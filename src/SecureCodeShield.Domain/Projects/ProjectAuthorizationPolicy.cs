namespace SecureCodeShield.Domain.Projects;

/// <summary>
/// Defines the minimum domain gate required before any project scan can begin.
/// Filesystem canonicalization and containment are infrastructure responsibilities.
/// </summary>
public static class ProjectAuthorizationPolicy
{
    public static bool CanAuthorize(string? projectRoot, bool explicitConsent)
    {
        return explicitConsent && !string.IsNullOrWhiteSpace(projectRoot);
    }
}
