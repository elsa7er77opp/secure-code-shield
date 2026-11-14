namespace SecureCodeShield.Domain.Security;

/// <summary>
/// Converts evidence that may contain a credential into a display-safe value.
/// The caller must discard the raw value after masking.
/// </summary>
public static class SecretEvidenceMasker
{
    private const char MaskCharacter = '•';
    private const int VisiblePrefixLength = 4;
    private const int VisibleSuffixLength = 2;

    public static string Mask(string evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(evidence);

        if (evidence.Length <= VisiblePrefixLength + VisibleSuffixLength)
        {
            return new string(MaskCharacter, evidence.Length);
        }

        var middleLength = evidence.Length - VisiblePrefixLength - VisibleSuffixLength;

        return string.Concat(
            evidence.AsSpan(0, VisiblePrefixLength),
            new string(MaskCharacter, middleLength),
            evidence.AsSpan(evidence.Length - VisibleSuffixLength, VisibleSuffixLength));
    }
}
