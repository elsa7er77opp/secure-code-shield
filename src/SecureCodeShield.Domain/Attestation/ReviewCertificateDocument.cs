using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SecureCodeShield.Domain.Attestation;

/// <summary>
/// The on-disk certificate: the canonical payload carried verbatim as base64url, plus the seal.
/// <para>
/// The payload is stored encoded rather than as nested JSON on purpose. A verifier must hash the
/// exact bytes that were signed, and re-serializing parsed JSON would risk changing a single byte —
/// a different key order, a re-escaped character, a trailing zero — and turn a valid certificate
/// into a failed one. Carrying the bytes verbatim removes that whole class of problem.
/// </para>
/// </summary>
public static class ReviewCertificateDocument
{
    public const string FileExtension = ".scscert.json";

    /// <summary>Renders the certificate file content. The result is what gets written to disk.</summary>
    public static string Render(byte[] canonicalPayload, CertificateSeal seal)
    {
        ArgumentNullException.ThrowIfNull(canonicalPayload);
        ArgumentNullException.ThrowIfNull(seal);

        var builder = new StringBuilder(canonicalPayload.Length + 512);
        builder.Append("{\n");
        builder.Append("  \"format\": \"securecodeshield.review-certificate\",\n");
        builder.Append("  \"payload\": \"").Append(ToBase64Url(canonicalPayload)).Append("\",\n");
        builder.Append("  \"seal\": {\n");
        builder.Append("    \"algorithm\": \"").Append(seal.Algorithm).Append("\",\n");
        builder.Append("    \"payloadSha256\": \"").Append(seal.PayloadSha256).Append("\",\n");
        builder.Append("    \"signature\": \"").Append(seal.Signature).Append("\",\n");
        builder.Append("    \"publicKey\": \"").Append(seal.PublicKey).Append("\",\n");
        builder.Append("    \"keyFingerprint\": \"").Append(seal.KeyFingerprint).Append("\"\n");
        builder.Append("  }\n");
        builder.Append("}\n");

        return builder.ToString();
    }

    /// <summary>
    /// Pulls the payload bytes and the seal back out of a certificate file.
    /// Parsing is deliberately narrow: anything unexpected is a refusal, never a guess.
    /// </summary>
    public static bool TryParse(string content, out byte[] payloadBytes, out CertificateSeal? seal, out string error)
    {
        payloadBytes = [];
        seal = null;

        if (string.IsNullOrWhiteSpace(content))
        {
            error = "The certificate file is empty.";
            return false;
        }

        var payloadEncoded = ReadField(content, "payload");
        if (payloadEncoded is null)
        {
            error = "The certificate has no payload.";
            return false;
        }

        try
        {
            payloadBytes = FromBase64Url(payloadEncoded);
        }
        catch (FormatException)
        {
            error = "The certificate payload is not readable. The file has been altered or truncated.";
            return false;
        }

        var algorithm = ReadField(content, "algorithm");
        var payloadSha256 = ReadField(content, "payloadSha256");
        var signature = ReadField(content, "signature");
        var publicKey = ReadField(content, "publicKey");
        var fingerprint = ReadField(content, "keyFingerprint");

        if (algorithm is null || payloadSha256 is null || signature is null || publicKey is null || fingerprint is null)
        {
            error = "The certificate seal is incomplete.";
            return false;
        }

        seal = new CertificateSeal(algorithm, payloadSha256, signature, publicKey, fingerprint);
        error = string.Empty;
        return true;
    }

    /// <summary>Hash of the exact payload bytes, in lowercase hex.</summary>
    public static string HashPayload(byte[] canonicalPayload)
    {
        ArgumentNullException.ThrowIfNull(canonicalPayload);

        return Convert.ToHexStringLower(SHA256.HashData(canonicalPayload));
    }

    public static string ToBase64Url(byte[] value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static byte[] FromBase64Url(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => throw new FormatException("The encoded payload has an invalid length.")
        };

        return Convert.FromBase64String(padded);
    }

    /// <summary>
    /// Reads one flat string field. The certificate layout is fixed and written by <see cref="Render"/>,
    /// so a small reader is enough and avoids a JSON dependency in the standalone verifier.
    /// </summary>
    private static string? ReadField(string content, string name)
    {
        var marker = string.Create(CultureInfo.InvariantCulture, $"\"{name}\"");
        var markerIndex = content.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        var colonIndex = content.IndexOf(':', markerIndex + marker.Length);
        if (colonIndex < 0)
        {
            return null;
        }

        var openQuote = content.IndexOf('"', colonIndex + 1);
        if (openQuote < 0)
        {
            return null;
        }

        var closeQuote = content.IndexOf('"', openQuote + 1);
        if (closeQuote < 0)
        {
            return null;
        }

        return content[(openQuote + 1)..closeQuote];
    }
}
