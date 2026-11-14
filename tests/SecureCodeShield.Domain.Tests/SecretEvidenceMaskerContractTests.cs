using System.Reflection;

namespace SecureCodeShield.Domain.Tests;

public sealed class SecretEvidenceMaskerContractTests
{
    [Fact]
    public void Mask_hides_sensitive_middle_characters_and_never_returns_the_original_value()
    {
        const string rawEvidence = "ghp_1234567890ABCDEFGHIJKLMN";

        var maskerType = Type.GetType(
            "SecureCodeShield.Domain.Security.SecretEvidenceMasker, SecureCodeShield.Domain");
        var maskMethod = maskerType?.GetMethod("Mask", BindingFlags.Public | BindingFlags.Static);

        Assert.True(maskMethod is not null, "The masking contract must expose SecretEvidenceMasker.Mask.");
        if (maskMethod is null)
        {
            return;
        }

        var maskedEvidence = Assert.IsType<string>(maskMethod.Invoke(null, [rawEvidence]));

        Assert.NotEqual(rawEvidence, maskedEvidence);
        Assert.StartsWith("ghp_", maskedEvidence, StringComparison.Ordinal);
        Assert.EndsWith("MN", maskedEvidence, StringComparison.Ordinal);
        Assert.DoesNotContain("1234567890ABCDEFGHIJKLM", maskedEvidence, StringComparison.Ordinal);
        Assert.Contains("•", maskedEvidence, StringComparison.Ordinal);
    }
}
