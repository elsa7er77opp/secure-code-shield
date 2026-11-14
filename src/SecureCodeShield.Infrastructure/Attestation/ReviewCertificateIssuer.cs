using System.Runtime.Versioning;
using System.Security.Cryptography;
using SecureCodeShield.Domain.Attestation;
using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Infrastructure.Attestation;

/// <summary>
/// Turns a completed local review into a sealed certificate.
/// The manifest is built by re-reading each reviewed file, so the digests describe the bytes on disk
/// at the moment of issue rather than whatever was cached during the review.
/// </summary>
public sealed class ReviewCertificateIssuer
{
    private readonly IssuerKeyStore _keyStore;
    private readonly string _toolVersion;

    public ReviewCertificateIssuer(IssuerKeyStore keyStore, string toolVersion)
    {
        ArgumentNullException.ThrowIfNull(keyStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolVersion);

        _keyStore = keyStore;
        _toolVersion = toolVersion;
    }

    /// <summary>
    /// Builds and seals a certificate for a completed review.
    /// </summary>
    /// <param name="projectRoot">The authorized root the review ran against.</param>
    /// <param name="subjectName">The name to record for the reviewed body of code.</param>
    /// <param name="issuerName">The local operator account that ran the review.</param>
    /// <param name="reviewedRelativePaths">Every file the review actually read.</param>
    /// <param name="findings">The findings produced.</param>
    /// <param name="exclusions">Why files inside the root were not reviewed.</param>
    /// <param name="issuedAtUtc">Issue time; passed in so the result is testable.</param>
    [SupportedOSPlatform("windows")]
    public IssuedCertificate Issue(
        string projectRoot,
        string subjectName,
        string issuerName,
        IReadOnlyList<string> reviewedRelativePaths,
        IReadOnlyList<Finding> findings,
        IReadOnlyList<ExclusionSummary> exclusions,
        DateTimeOffset issuedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(issuerName);
        ArgumentNullException.ThrowIfNull(reviewedRelativePaths);
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(exclusions);

        var canonicalRoot = Path.GetFullPath(projectRoot);
        var files = BuildManifest(canonicalRoot, reviewedRelativePaths);

        using var key = _keyStore.LoadOrCreate();
        var fingerprint = IssuerKeyStore.ComputeFingerprint(key);

        var payload = new ReviewCertificatePayload(
            ReviewCertificatePayload.CurrentSchemaVersion,
            Guid.NewGuid().ToString("D"),
            subjectName,
            issuedAtUtc,
            issuerName,
            fingerprint,
            $"CyberBank Secure Code Shield {_toolVersion}",
            RulePackFingerprint.Current(),
            new ScopeDeclaration(files.Count, exclusions.Sum(exclusion => exclusion.FileCount), exclusions),
            BuildTallies(findings),
            files);

        var canonicalPayload = CanonicalPayloadWriter.ToCanonicalBytes(payload);

        var seal = new CertificateSeal(
            CertificateSeal.EcdsaP256Sha256,
            ReviewCertificateDocument.HashPayload(canonicalPayload),
            Convert.ToBase64String(key.SignData(canonicalPayload, HashAlgorithmName.SHA256)),
            Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
            fingerprint);

        return new IssuedCertificate(payload, ReviewCertificateDocument.Render(canonicalPayload, seal));
    }

    private static List<FileDigest> BuildManifest(string canonicalRoot, IReadOnlyList<string> reviewedRelativePaths)
    {
        var files = new List<FileDigest>(reviewedRelativePaths.Count);

        foreach (var relativePath in reviewedRelativePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var fullPath = Path.Combine(canonicalRoot, relativePath);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            using var stream = File.OpenRead(fullPath);
            var digest = Convert.ToHexStringLower(SHA256.HashData(stream));

            files.Add(new FileDigest(
                CanonicalPayloadWriter.NormalizePath(relativePath),
                digest,
                new FileInfo(fullPath).Length));
        }

        return files;
    }

    /// <summary>
    /// Always reports every severity, including the ones with no findings.
    /// A missing row reads as an omission; an explicit zero reads as a result.
    /// </summary>
    private static SeverityTally[] BuildTallies(IReadOnlyList<Finding> findings) =>
        Enum.GetValues<Severity>()
            .OrderByDescending(severity => severity)
            .Select(severity => new SeverityTally(severity, findings.Count(finding => finding.Rule.Severity == severity)))
            .ToArray();
}

/// <param name="Payload">What the certificate asserts.</param>
/// <param name="FileContent">The certificate file content, ready to write to disk.</param>
public sealed record IssuedCertificate(ReviewCertificatePayload Payload, string FileContent);
