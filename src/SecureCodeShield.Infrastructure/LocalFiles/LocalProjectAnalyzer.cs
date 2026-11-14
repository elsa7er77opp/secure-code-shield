using SecureCodeShield.Domain.Findings;
using SecureCodeShield.Domain.Projects;

namespace SecureCodeShield.Infrastructure.LocalFiles;

/// <summary>
/// Reads an explicitly authorized local project as bounded text and never executes any project content.
/// Raw source lines stay inside the read loop; only redacted findings leave this class.
///
/// This replaces the single-rule <see cref="LocalProjectScanner"/> path. That type is kept so existing
/// callers and tests keep compiling while the desktop moves across.
/// </summary>
public static class LocalProjectAnalyzer
{
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint
    };

    public static AnalysisResult Analyze(string projectRoot, bool explicitAuthorization) =>
        Analyze(projectRoot, explicitAuthorization, CancellationToken.None);

    public static AnalysisResult Analyze(
        string projectRoot,
        bool explicitAuthorization,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        var canonicalRoot = Path.GetFullPath(projectRoot);
        if (!ProjectAuthorizationPolicy.CanAuthorize(canonicalRoot, explicitAuthorization))
        {
            throw new UnauthorizedAccessException("Explicit authorization is required before analyzing a local project.");
        }

        if (!Directory.Exists(canonicalRoot))
        {
            throw new DirectoryNotFoundException("The authorized project root no longer exists.");
        }

        var findings = new List<Finding>();
        var eligibleFiles = 0;
        var excludedFiles = 0;

        foreach (var fullPath in Directory.EnumerateFiles(canonicalRoot, "*", EnumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PathScopePolicy.IsWithinRoot(canonicalRoot, fullPath))
            {
                excludedFiles++;
                continue;
            }

            var relativePath = Path.GetRelativePath(canonicalRoot, fullPath);
            var fileSize = new FileInfo(fullPath).Length;

            if (!FileEligibilityPolicy.Evaluate(relativePath, fileSize).IsEligible)
            {
                excludedFiles++;
                continue;
            }

            eligibleFiles++;

            var lineNumber = 0;
            foreach (var sourceLine in File.ReadLines(fullPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                lineNumber++;
                findings.AddRange(LineAnalyzer.Analyze(relativePath, lineNumber, sourceLine));
            }
        }

        var ordered = findings
            .OrderByDescending(finding => finding.Rule.Severity)
            .ThenBy(finding => finding.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.LineNumber)
            .ToArray();

        return new AnalysisResult(ordered, eligibleFiles, excludedFiles);
    }
}

/// <summary>
/// The outcome of one local analysis run: what was found, and how much of the project it covered.
/// The counts matter for the report — a reviewer needs to know what was not looked at.
/// </summary>
public sealed record AnalysisResult(
    IReadOnlyList<Finding> Findings,
    int EligibleFiles,
    int ExcludedFiles)
{
    public int CountOf(Severity severity) => Findings.Count(finding => finding.Rule.Severity == severity);
}
