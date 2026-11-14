using SecureCodeShield.Domain.Findings;
using SecureCodeShield.Domain.Projects;

namespace SecureCodeShield.Infrastructure.LocalFiles;

/// <summary>
/// Reads an explicitly authorized local project as bounded text and never executes any project content.
/// Raw source lines stay inside the scan loop; only redacted findings leave this class.
/// </summary>
public static class LocalProjectScanner
{
    private static readonly EnumerationOptions EnumerationOptions = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint
    };

    public static IReadOnlyList<SecretFinding> Scan(string projectRoot, bool explicitAuthorization)
    {
        return ScanCancellable(projectRoot, explicitAuthorization, CancellationToken.None);
    }

    public static IReadOnlyList<SecretFinding> ScanCancellable(
        string projectRoot,
        bool explicitAuthorization,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);

        var canonicalRoot = Path.GetFullPath(projectRoot);
        if (!ProjectAuthorizationPolicy.CanAuthorize(canonicalRoot, explicitAuthorization))
        {
            throw new UnauthorizedAccessException("Explicit authorization is required before scanning a local project.");
        }

        if (!Directory.Exists(canonicalRoot))
        {
            throw new DirectoryNotFoundException("The authorized project root no longer exists.");
        }

        var findings = new List<SecretFinding>();

        foreach (var fullPath in Directory.EnumerateFiles(canonicalRoot, "*", EnumerationOptions))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!PathScopePolicy.IsWithinRoot(canonicalRoot, fullPath))
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(canonicalRoot, fullPath);
            var fileSize = new FileInfo(fullPath).Length;
            if (!FileEligibilityPolicy.Evaluate(relativePath, fileSize).IsEligible)
            {
                continue;
            }

            var lineNumber = 0;
            foreach (var sourceLine in File.ReadLines(fullPath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                lineNumber++;
                findings.AddRange(SecretAssignmentDetector.ScanLine(relativePath, lineNumber, sourceLine));
            }
        }

        return findings;
    }
}
