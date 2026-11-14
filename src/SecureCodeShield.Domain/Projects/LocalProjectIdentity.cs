using System;
using System.IO;

namespace SecureCodeShield.Domain.Projects;

public sealed record LocalProjectIdentity(Guid Id, string DisplayName, string CanonicalRoot)
{
    public static LocalProjectIdentity Create(string displayName, string authorizedRoot)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A project display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(authorizedRoot))
        {
            throw new ArgumentException("An authorized project root is required.", nameof(authorizedRoot));
        }

        var normalizedName = displayName.Trim();
        var canonicalRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(authorizedRoot.Trim()));
        return new LocalProjectIdentity(Guid.NewGuid(), normalizedName, canonicalRoot);
    }
}
