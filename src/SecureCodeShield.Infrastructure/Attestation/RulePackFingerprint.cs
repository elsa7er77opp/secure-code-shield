using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SecureCodeShield.Domain.Attestation;
using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Infrastructure.Attestation;

/// <summary>
/// Identifies the rule set a certificate was produced with.
/// <para>
/// Pinning the rules by hash is what keeps the certificate honest. A clean result means "clean against
/// these rules", and a later, larger rule pack would legitimately find more. Without the hash, an old
/// certificate could be mistaken for a current one.
/// </para>
/// </summary>
public static class RulePackFingerprint
{
    public const string PackId = "cyberbank.builtin";
    public const string PackVersion = "1.0";

    public static RulePackIdentity Current()
    {
        var builder = new StringBuilder();

        foreach (var rule in RuleCatalog.All.OrderBy(rule => rule.Id, StringComparer.Ordinal))
        {
            builder.Append(CultureInfo.InvariantCulture, $"{rule.Id}|{rule.Severity}|{rule.Confidence}|{rule.Cwe}|{rule.Owasp}\n");
        }

        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));

        return new RulePackIdentity(PackId, PackVersion, digest, RuleCatalog.All.Count);
    }
}
