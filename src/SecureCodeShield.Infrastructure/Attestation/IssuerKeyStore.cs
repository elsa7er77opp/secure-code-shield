using System.Runtime.Versioning;
using System.Security.Cryptography;
using SecureCodeShield.Domain.Attestation;

namespace SecureCodeShield.Infrastructure.Attestation;

/// <summary>
/// Holds the key this installation signs certificates with.
/// <para>
/// The key is generated once, on this machine, and never leaves it. The private key is written to
/// disk protected by DPAPI under the current Windows user, so another account on the same machine
/// cannot read it and a copied file is useless elsewhere. On any other platform the store refuses to
/// save rather than writing an unprotected key — an unprotected signing key is worse than no signing.
/// </para>
/// </summary>
public sealed class IssuerKeyStore
{
    private const string KeyFileName = "issuer-key.dpapi";

    private readonly string _keyFilePath;

    public IssuerKeyStore(string stateDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateDirectory);

        _keyFilePath = Path.Combine(stateDirectory, KeyFileName);
    }

    /// <summary>True when this platform can protect a private key at rest.</summary>
    public static bool IsSupported => OperatingSystem.IsWindows();

    /// <summary>
    /// Returns the installation's signing key, creating it on first use.
    /// The caller owns the returned instance and must dispose it.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public ECDsa LoadOrCreate()
    {
        if (!IsSupported)
        {
            throw new PlatformNotSupportedException("Certificate signing requires Windows, where the private key can be protected with DPAPI.");
        }

        var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        if (File.Exists(_keyFilePath))
        {
            try
            {
                var unprotected = ProtectedData.Unprotect(
                    File.ReadAllBytes(_keyFilePath),
                    optionalEntropy: null,
                    DataProtectionScope.CurrentUser);

                key.ImportECPrivateKey(unprotected, out _);
                CryptographicOperations.ZeroMemory(unprotected);
                return key;
            }
            catch (Exception exception) when (exception is CryptographicException or IOException)
            {
                // A key that cannot be read is replaced rather than silently reused. Certificates
                // already issued stay verifiable: each carries its own public key.
                key.Dispose();
                key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            }
        }

        Save(key);
        return key;
    }

    [SupportedOSPlatform("windows")]
    private void Save(ECDsa key)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_keyFilePath)!);

        var privateKey = key.ExportECPrivateKey();

        try
        {
            var protectedKey = ProtectedData.Protect(privateKey, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_keyFilePath, protectedKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(privateKey);
        }
    }

    /// <summary>
    /// A short, stable name for a public key, shown to a recipient so they can confirm that a later
    /// certificate came from the same issuer as an earlier one.
    /// </summary>
    public static string ComputeFingerprint(ECDsa key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var digest = SHA256.HashData(key.ExportSubjectPublicKeyInfo());

        return string.Join(' ', Convert.ToHexString(digest)[..16].Chunk(4).Select(chunk => new string(chunk)));
    }
}
