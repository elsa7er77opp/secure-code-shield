using System.Security.Cryptography;

namespace SecureCodeShield.Domain.Attestation;

/// <summary>
/// Checks a certificate without needing the product that issued it.
/// Two independent questions are answered separately, because they fail for different reasons and
/// a recipient needs to know which one broke:
/// the seal (was the certificate itself altered?) and the manifest (are these the files it describes?).
/// </summary>
public static class CertificateVerifier
{
    /// <summary>
    /// Confirms the payload has not been altered and the seal was produced by the key it carries.
    /// <para>
    /// This proves integrity and that later certificates come from the same issuer key. It does not
    /// prove who that issuer is: the key is self-generated, not vouched for by an authority. Identity
    /// requires a code-signing certificate, which this build does not use.
    /// </para>
    /// </summary>
    public static SealVerification VerifySeal(byte[] canonicalPayload, CertificateSeal seal)
    {
        ArgumentNullException.ThrowIfNull(canonicalPayload);
        ArgumentNullException.ThrowIfNull(seal);

        if (!string.Equals(seal.Algorithm, CertificateSeal.EcdsaP256Sha256, StringComparison.Ordinal))
        {
            return new SealVerification(false, $"Unsupported signature algorithm '{seal.Algorithm}'.");
        }

        var actualHash = ReviewCertificateDocument.HashPayload(canonicalPayload);
        if (!CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(actualHash),
                System.Text.Encoding.ASCII.GetBytes(seal.PayloadSha256)))
        {
            return new SealVerification(false, "The certificate contents do not match the hash it was sealed with. The certificate has been altered.");
        }

        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(seal.PublicKey), out _);

            var isValid = key.VerifyData(
                canonicalPayload,
                Convert.FromBase64String(seal.Signature),
                HashAlgorithmName.SHA256);

            return isValid
                ? new SealVerification(true, "The seal is valid and the certificate has not been altered.")
                : new SealVerification(false, "The signature does not match the certificate contents.");
        }
        catch (Exception exception) when (exception is CryptographicException or FormatException)
        {
            return new SealVerification(false, "The seal could not be read. The certificate has been altered or was produced by a different tool.");
        }
    }

    /// <summary>
    /// Compares the manifest against a folder on disk and reports each file's state.
    /// A file the certificate does not mention is reported rather than ignored: the recipient needs to
    /// know that part of what they received was never reviewed.
    /// </summary>
    public static ManifestVerification VerifyManifest(
        IReadOnlyList<FileDigest> manifest,
        string rootFolder,
        Func<string, byte[]> readFile,
        IReadOnlyCollection<string> presentRelativePaths)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootFolder);
        ArgumentNullException.ThrowIfNull(readFile);
        ArgumentNullException.ThrowIfNull(presentRelativePaths);

        var matched = new List<string>();
        var modified = new List<string>();
        var missing = new List<string>();

        var present = new HashSet<string>(
            presentRelativePaths.Select(CanonicalPayloadWriter.NormalizePath),
            StringComparer.Ordinal);

        foreach (var entry in manifest)
        {
            var path = CanonicalPayloadWriter.NormalizePath(entry.RelativePath);

            if (!present.Contains(path))
            {
                missing.Add(path);
                continue;
            }

            present.Remove(path);

            string actual;
            try
            {
                actual = Convert.ToHexStringLower(SHA256.HashData(readFile(path)));
            }
            catch (IOException)
            {
                missing.Add(path);
                continue;
            }

            if (string.Equals(actual, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                matched.Add(path);
            }
            else
            {
                modified.Add(path);
            }
        }

        // Whatever is left was delivered but never reviewed.
        var unreviewed = present.OrderBy(path => path, StringComparer.Ordinal).ToArray();

        return new ManifestVerification(
            matched.Count,
            modified.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            missing.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
            unreviewed);
    }
}

/// <param name="IsValid">True when the certificate is intact and correctly signed.</param>
/// <param name="Explanation">Plain description a recipient can act on.</param>
public sealed record SealVerification(bool IsValid, string Explanation);

/// <param name="MatchedFileCount">Files whose bytes are exactly what was reviewed.</param>
/// <param name="ModifiedFiles">Files that changed since the review.</param>
/// <param name="MissingFiles">Files the certificate lists that are not present.</param>
/// <param name="UnreviewedFiles">Files present that the certificate does not cover.</param>
public sealed record ManifestVerification(
    int MatchedFileCount,
    IReadOnlyList<string> ModifiedFiles,
    IReadOnlyList<string> MissingFiles,
    IReadOnlyList<string> UnreviewedFiles)
{
    public bool IsExactMatch => ModifiedFiles.Count == 0 && MissingFiles.Count == 0 && UnreviewedFiles.Count == 0;
}
