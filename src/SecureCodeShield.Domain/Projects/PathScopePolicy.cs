namespace SecureCodeShield.Domain.Projects;

/// <summary>
/// Compares canonical Windows-style paths after infrastructure has resolved reparse points and absolute paths.
/// </summary>
public static class PathScopePolicy
{
    public static bool IsWithinRoot(string authorizedRoot, string candidatePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizedRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidatePath);

        var normalizedRoot = Normalize(authorizedRoot);
        var normalizedCandidate = Normalize(candidatePath);

        return normalizedCandidate.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
               || normalizedCandidate.StartsWith(normalizedRoot + "\\", StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path)
    {
        return path.Replace('/', '\\').TrimEnd('\\');
    }
}
