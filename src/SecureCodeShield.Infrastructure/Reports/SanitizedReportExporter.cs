using System.Text;
using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Infrastructure.Reports;

/// <summary>
/// Produces a local review report from display-safe findings only. This API intentionally has no project-root parameter.
/// </summary>
public static class SanitizedReportExporter
{
    public static ReportExportResult Export(
        string outputDirectory,
        string projectDisplayName,
        IEnumerable<SecretFinding> findings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectDisplayName);
        ArgumentNullException.ThrowIfNull(findings);

        var safeFindings = findings.ToArray();
        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var reportName = "secure-code-shield-report-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")
                         + "-" + Guid.NewGuid().ToString("N")[..8] + ".md";
        var reportPath = Path.Combine(fullOutputDirectory, reportName);
        var temporaryPath = reportPath + ".tmp";

        File.WriteAllText(temporaryPath, Render(projectDisplayName, safeFindings), new UTF8Encoding(false));
        File.Move(temporaryPath, reportPath);

        return new ReportExportResult(reportPath, safeFindings.Length);
    }

    private static string Render(string projectDisplayName, IReadOnlyList<SecretFinding> findings)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Secure Code Shield — Sanitized Local Report");
        builder.AppendLine();
        builder.AppendLine("**Project:** " + EscapeMarkdown(projectDisplayName));
        builder.AppendLine("**Finding count:** " + findings.Count);
        builder.AppendLine("**Evidence policy:** Values are redacted before report creation. This report does not include an absolute project path or source-file content.");
        builder.AppendLine();
        builder.AppendLine("| Rule | Relative location | Line | Masked evidence |");
        builder.AppendLine("|---|---|---:|---|");

        foreach (var finding in findings)
        {
            builder.Append("| ")
                .Append(EscapeMarkdown(finding.RuleId))
                .Append(" | ")
                .Append(EscapeMarkdown(finding.RelativePath))
                .Append(" | ")
                .Append(finding.LineNumber)
                .Append(" | ")
                .Append(EscapeMarkdown(finding.MaskedEvidence))
                .AppendLine(" |");
        }

        return builder.ToString();
    }

    private static string EscapeMarkdown(string value)
    {
        return value
            .Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
    }
}
