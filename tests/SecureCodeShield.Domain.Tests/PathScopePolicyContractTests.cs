using System.Reflection;

namespace SecureCodeShield.Domain.Tests;

public sealed class PathScopePolicyContractTests
{
    [Fact]
    public void IsWithinRoot_accepts_a_descendant_path_of_the_authorized_root()
    {
        var policyType = Type.GetType(
            "SecureCodeShield.Domain.Projects.PathScopePolicy, SecureCodeShield.Domain");
        var method = policyType?.GetMethod("IsWithinRoot", BindingFlags.Public | BindingFlags.Static);

        Assert.True(method is not null, "The path scope contract must expose PathScopePolicy.IsWithinRoot.");
        if (method is null)
        {
            return;
        }

        var isWithinRoot = Assert.IsType<bool>(
            method.Invoke(null, [@"C:\Projects\Shield", @"C:\Projects\Shield\src\Program.cs"]));

        Assert.True(isWithinRoot);
    }

    [Fact]
    public void IsWithinRoot_rejects_a_sibling_that_only_shares_the_root_prefix()
    {
        var policyType = Type.GetType(
            "SecureCodeShield.Domain.Projects.PathScopePolicy, SecureCodeShield.Domain");
        var method = policyType?.GetMethod("IsWithinRoot", BindingFlags.Public | BindingFlags.Static);

        Assert.True(method is not null, "The path scope contract must expose PathScopePolicy.IsWithinRoot.");
        if (method is null)
        {
            return;
        }

        var isWithinRoot = Assert.IsType<bool>(
            method.Invoke(null, [@"C:\Projects\Shield", @"C:\Projects\Shield-Archive\secrets.env"]));

        Assert.False(isWithinRoot);
    }
}
