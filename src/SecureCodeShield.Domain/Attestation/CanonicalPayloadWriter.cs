using System.Globalization;
using System.Text;

namespace SecureCodeShield.Domain.Attestation;

/// <summary>
/// Serializes a payload to bytes deterministically.
/// <para>
/// The signature covers these exact bytes, so the writer must never depend on reflection order,
/// culture, locale, line endings or dictionary iteration. Every field is written by hand in a fixed
/// order for that reason: a certificate signed on one machine has to verify byte-for-byte on another.
/// </para>
/// </summary>
public static class CanonicalPayloadWriter
{
    /// <summary>UTF-8 without a byte order mark. A BOM would change the bytes that get signed.</summary>
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static byte[] ToCanonicalBytes(ReviewCertificatePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return Utf8NoBom.GetBytes(ToCanonicalJson(payload));
    }

    public static string ToCanonicalJson(ReviewCertificatePayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var builder = new StringBuilder(4096);

        builder.Append('{');
        WriteNumber(builder, "schemaVersion", payload.SchemaVersion, first: true);
        WriteString(builder, "certificateId", payload.CertificateId);
        WriteString(builder, "subject", payload.SubjectName);
        WriteString(builder, "issuedAtUtc", payload.IssuedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        WriteString(builder, "issuer", payload.IssuerName);
        WriteString(builder, "issuerKeyFingerprint", payload.IssuerKeyFingerprint);
        WriteString(builder, "tool", payload.Tool);
        WriteString(builder, "assertion", ReviewCertificatePayload.Assertion);

        builder.Append(",\"rulePack\":{");
        WriteString(builder, "id", payload.RulePack.Id, first: true);
        WriteString(builder, "version", payload.RulePack.Version);
        WriteString(builder, "sha256", payload.RulePack.Sha256);
        WriteNumber(builder, "ruleCount", payload.RulePack.RuleCount);
        builder.Append('}');

        builder.Append(",\"scope\":{");
        WriteNumber(builder, "reviewedFileCount", payload.Scope.ReviewedFileCount, first: true);
        WriteNumber(builder, "excludedFileCount", payload.Scope.ExcludedFileCount);
        WriteString(builder, "note", payload.Scope.Note);
        builder.Append(",\"exclusions\":[");

        // Ordered by reason so two runs over the same project always emit the same bytes.
        var exclusions = payload.Scope.Exclusions
            .OrderBy(exclusion => exclusion.Reason, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < exclusions.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append('{');
            WriteString(builder, "reason", exclusions[index].Reason, first: true);
            WriteNumber(builder, "fileCount", exclusions[index].FileCount);
            builder.Append('}');
        }

        builder.Append("]}");

        builder.Append(",\"tallies\":[");

        // Highest severity first, which is also the order a reader expects.
        var tallies = payload.Tallies
            .OrderByDescending(tally => tally.Severity)
            .ToArray();

        for (var index = 0; index < tallies.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append('{');
            WriteString(builder, "severity", tallies[index].Severity.ToString(), first: true);
            WriteNumber(builder, "count", tallies[index].Count);
            builder.Append('}');
        }

        builder.Append("],\"files\":[");

        // Ordinal path order: culture-aware sorting would differ between machines.
        var files = payload.Files
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();

        for (var index = 0; index < files.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append('{');
            WriteString(builder, "path", NormalizePath(files[index].RelativePath), first: true);
            WriteString(builder, "sha256", files[index].Sha256);
            WriteNumber(builder, "sizeBytes", files[index].SizeBytes);
            builder.Append('}');
        }

        builder.Append("]}");

        return builder.ToString();
    }

    /// <summary>
    /// Windows and Unix disagree about separators. The manifest always stores forward slashes so a
    /// certificate written on Windows verifies unchanged on another platform.
    /// </summary>
    public static string NormalizePath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        return relativePath.Replace('\\', '/').TrimStart('.', '/');
    }

    private static void WriteNumber(StringBuilder builder, string name, long value, bool first = false)
    {
        if (!first)
        {
            builder.Append(',');
        }

        builder.Append('"').Append(name).Append("\":").Append(value.ToString(CultureInfo.InvariantCulture));
    }

    private static void WriteString(StringBuilder builder, string name, string value, bool first = false)
    {
        if (!first)
        {
            builder.Append(',');
        }

        builder.Append('"').Append(name).Append("\":");
        AppendEscaped(builder, value);
    }

    private static void AppendEscaped(StringBuilder builder, string value)
    {
        builder.Append('"');

        foreach (var character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < ' ')
                    {
                        builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }
}
