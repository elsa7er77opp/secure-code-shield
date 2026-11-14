using System.Globalization;
using System.Text.Json;
using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Attestation;

/// <summary>
/// Reads a canonical payload back into the model.
/// <para>
/// Reading is separate from writing on purpose. Only the writer has to be byte-deterministic; the
/// reader just has to be strict. Nothing read here is ever re-serialized and re-signed — the original
/// bytes stay the authority.
/// </para>
/// </summary>
public static class CanonicalPayloadReader
{
    public static bool TryRead(byte[] canonicalPayload, out ReviewCertificatePayload? payload, out string error)
    {
        ArgumentNullException.ThrowIfNull(canonicalPayload);
        payload = null;

        try
        {
            using var document = JsonDocument.Parse(canonicalPayload);
            var root = document.RootElement;

            var schemaVersion = root.GetProperty("schemaVersion").GetInt32();
            if (schemaVersion > ReviewCertificatePayload.CurrentSchemaVersion)
            {
                error = $"This certificate uses format version {schemaVersion}; this verifier understands up to {ReviewCertificatePayload.CurrentSchemaVersion}. Use a newer verifier rather than trusting a partial reading.";
                return false;
            }

            var rulePack = root.GetProperty("rulePack");
            var scope = root.GetProperty("scope");

            var exclusions = scope.GetProperty("exclusions").EnumerateArray()
                .Select(item => new ExclusionSummary(
                    item.GetProperty("reason").GetString() ?? string.Empty,
                    item.GetProperty("fileCount").GetInt32()))
                .ToArray();

            var tallies = root.GetProperty("tallies").EnumerateArray()
                .Select(item => new SeverityTally(
                    Enum.Parse<Severity>(item.GetProperty("severity").GetString() ?? nameof(Severity.Note)),
                    item.GetProperty("count").GetInt32()))
                .ToArray();

            var files = root.GetProperty("files").EnumerateArray()
                .Select(item => new FileDigest(
                    item.GetProperty("path").GetString() ?? string.Empty,
                    item.GetProperty("sha256").GetString() ?? string.Empty,
                    item.GetProperty("sizeBytes").GetInt64()))
                .ToArray();

            payload = new ReviewCertificatePayload(
                schemaVersion,
                root.GetProperty("certificateId").GetString() ?? string.Empty,
                root.GetProperty("subject").GetString() ?? string.Empty,
                DateTimeOffset.Parse(root.GetProperty("issuedAtUtc").GetString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                root.GetProperty("issuer").GetString() ?? string.Empty,
                root.GetProperty("issuerKeyFingerprint").GetString() ?? string.Empty,
                root.GetProperty("tool").GetString() ?? string.Empty,
                new RulePackIdentity(
                    rulePack.GetProperty("id").GetString() ?? string.Empty,
                    rulePack.GetProperty("version").GetString() ?? string.Empty,
                    rulePack.GetProperty("sha256").GetString() ?? string.Empty,
                    rulePack.GetProperty("ruleCount").GetInt32()),
                new ScopeDeclaration(
                    scope.GetProperty("reviewedFileCount").GetInt32(),
                    scope.GetProperty("excludedFileCount").GetInt32(),
                    exclusions,
                    scope.GetProperty("note").GetString() ?? string.Empty),
                tallies,
                files);

            error = string.Empty;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentException)
        {
            error = "The certificate payload could not be read. The file has been altered.";
            return false;
        }
    }
}
