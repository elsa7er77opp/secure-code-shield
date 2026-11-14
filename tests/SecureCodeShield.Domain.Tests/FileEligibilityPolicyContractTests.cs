using System.Reflection;

namespace SecureCodeShield.Domain.Tests;

public sealed class FileEligibilityPolicyContractTests
{
    [Fact]
    public void Evaluate_skips_a_file_under_node_modules_with_an_explicit_reason()
    {
        var domainAssembly = Assembly.Load("SecureCodeShield.Domain");
        var policyType = domainAssembly.GetType("SecureCodeShield.Domain.Projects.FileEligibilityPolicy");
        var evaluateMethod = policyType?.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static);

        Assert.True(evaluateMethod is not null, "The file policy contract must expose FileEligibilityPolicy.Evaluate.");
        if (evaluateMethod is null)
        {
            return;
        }

        var decision = evaluateMethod.Invoke(null, ["node_modules/library/index.js", 1024L]);
        var isEligible = Assert.IsType<bool>(decision?.GetType().GetProperty("IsEligible")?.GetValue(decision));
        var reason = Assert.IsType<string>(decision?.GetType().GetProperty("Reason")?.GetValue(decision));

        Assert.False(isEligible);
        Assert.Equal("excluded-directory", reason);
    }

    [Fact]
    public void Evaluate_allows_a_small_environment_file_for_redaction_first_analysis()
    {
        var domainAssembly = Assembly.Load("SecureCodeShield.Domain");
        var policyType = domainAssembly.GetType("SecureCodeShield.Domain.Projects.FileEligibilityPolicy");
        var evaluateMethod = policyType?.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static);

        Assert.True(evaluateMethod is not null, "The file policy contract must expose FileEligibilityPolicy.Evaluate.");
        if (evaluateMethod is null)
        {
            return;
        }

        var decision = evaluateMethod.Invoke(null, ["config/.env", 1024L]);
        var isEligible = Assert.IsType<bool>(decision?.GetType().GetProperty("IsEligible")?.GetValue(decision));
        var reason = Assert.IsType<string>(decision?.GetType().GetProperty("Reason")?.GetValue(decision));

        Assert.True(isEligible);
        Assert.Equal("eligible", reason);
    }
}
