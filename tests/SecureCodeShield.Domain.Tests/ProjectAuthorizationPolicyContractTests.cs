using System.Reflection;

namespace SecureCodeShield.Domain.Tests;

public sealed class ProjectAuthorizationPolicyContractTests
{
    [Fact]
    public void CanAuthorize_rejects_a_project_when_explicit_consent_is_absent()
    {
        var policyType = Type.GetType(
            "SecureCodeShield.Domain.Projects.ProjectAuthorizationPolicy, SecureCodeShield.Domain");
        var canAuthorizeMethod = policyType?.GetMethod("CanAuthorize", BindingFlags.Public | BindingFlags.Static);

        Assert.True(canAuthorizeMethod is not null, "The authorization contract must expose ProjectAuthorizationPolicy.CanAuthorize.");
        if (canAuthorizeMethod is null)
        {
            return;
        }

        var canAuthorize = Assert.IsType<bool>(
            canAuthorizeMethod.Invoke(null, [@"C:\AuthorizedProject", false]));

        Assert.False(canAuthorize);
    }

    [Fact]
    public void CanAuthorize_accepts_a_nonempty_project_root_only_after_explicit_consent()
    {
        var policyType = Type.GetType(
            "SecureCodeShield.Domain.Projects.ProjectAuthorizationPolicy, SecureCodeShield.Domain");
        var canAuthorizeMethod = policyType?.GetMethod("CanAuthorize", BindingFlags.Public | BindingFlags.Static);

        Assert.True(canAuthorizeMethod is not null, "The authorization contract must expose ProjectAuthorizationPolicy.CanAuthorize.");
        if (canAuthorizeMethod is null)
        {
            return;
        }

        var canAuthorize = Assert.IsType<bool>(
            canAuthorizeMethod.Invoke(null, [@"C:\AuthorizedProject", true]));

        Assert.True(canAuthorize);
    }
}
