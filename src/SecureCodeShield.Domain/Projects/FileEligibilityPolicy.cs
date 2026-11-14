namespace SecureCodeShield.Domain.Projects;

/// <summary>
/// A conservative, pure policy for deciding whether a local text file may be handed to the analysis pipeline.
/// Widening this list is what lets the engine produce results on a real project rather than only on .env files.
/// </summary>
public static class FileEligibilityPolicy
{
    public const long DefaultMaximumFileBytes = 1_048_576;

    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".idea",
        ".next",
        ".nuxt",
        ".svn",
        ".venv",
        ".vs",
        "bin",
        "build",
        "coverage",
        "dist",
        "node_modules",
        "obj",
        "out",
        "packages",
        "target",
        "vendor",
        "venv"
    };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // C-family and .NET
        ".c", ".cc", ".cpp", ".cs", ".h", ".hpp",
        // Web and scripting
        ".js", ".jsx", ".mjs", ".cjs", ".ts", ".tsx", ".vue", ".svelte",
        ".php", ".py", ".rb", ".go", ".java", ".kt", ".kts", ".scala", ".rs", ".swift",
        ".pl", ".ps1", ".psm1", ".sh", ".bash", ".zsh",
        // Data and configuration
        ".json", ".yaml", ".yml", ".toml", ".ini", ".cfg", ".conf", ".properties", ".xml",
        ".env", ".tf", ".tfvars", ".gradle", ".sql",
        // Project manifests
        ".csproj", ".props", ".targets"
    };

    /// <summary>Extensionless or dotted file names that are always worth reading.</summary>
    private static readonly HashSet<string> SupportedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".env",
        ".npmrc",
        ".netrc",
        "dockerfile",
        "docker-compose.yml",
        "docker-compose.yaml",
        "makefile",
        "procfile"
    };

    public static FileEligibilityDecision Evaluate(string relativePath, long fileSizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        if (fileSizeBytes < 0)
        {
            return new FileEligibilityDecision(false, "invalid-size");
        }

        if (fileSizeBytes > DefaultMaximumFileBytes)
        {
            return new FileEligibilityDecision(false, "file-too-large");
        }

        var segments = relativePath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Any(segment => ExcludedDirectories.Contains(segment)))
        {
            return new FileEligibilityDecision(false, "excluded-directory");
        }

        var fileName = segments.LastOrDefault() ?? string.Empty;

        if (SupportedFileNames.Contains(fileName))
        {
            return new FileEligibilityDecision(true, "eligible");
        }

        // `.env.production`, `.env.local` and friends carry the same risk as `.env` itself.
        if (fileName.StartsWith(".env", StringComparison.OrdinalIgnoreCase))
        {
            return new FileEligibilityDecision(true, "eligible");
        }

        var extension = Path.GetExtension(fileName);

        return SupportedExtensions.Contains(extension)
            ? new FileEligibilityDecision(true, "eligible")
            : new FileEligibilityDecision(false, "unsupported-extension");
    }
}
