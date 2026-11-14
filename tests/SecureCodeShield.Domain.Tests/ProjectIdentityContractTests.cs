using System.Reflection;

namespace SecureCodeShield.Domain.Tests;

public sealed class ProjectIdentityContractTests
{
    [Fact]
    public void Create_normalizes_display_name_and_preserves_the_canonical_authorized_root()
    {
        var identityType = Type.GetType(
            "SecureCodeShield.Domain.Projects.LocalProjectIdentity, SecureCodeShield.Domain");
        var create = identityType?.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        Assert.True(create is not null, "The project identity contract must expose LocalProjectIdentity.Create.");
        if (create is null)
        {
            return;
        }

        var identity = create.Invoke(null, ["  Payment API  ", @"C:\Projects\PaymentApi\"]);

        Assert.NotNull(identity);
        Assert.Equal("Payment API", identityType!.GetProperty("DisplayName")?.GetValue(identity));
        Assert.Equal(@"C:\Projects\PaymentApi", identityType.GetProperty("CanonicalRoot")?.GetValue(identity));
        Assert.NotEqual(Guid.Empty, identityType.GetProperty("Id")?.GetValue(identity));
    }

    [Fact]
    public void Create_rejects_a_blank_display_name()
    {
        var identityType = Type.GetType(
            "SecureCodeShield.Domain.Projects.LocalProjectIdentity, SecureCodeShield.Domain");
        var create = identityType?.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        Assert.True(create is not null, "The project identity contract must expose LocalProjectIdentity.Create.");
        if (create is null)
        {
            return;
        }

        var exception = Assert.Throws<TargetInvocationException>(
            () => create.Invoke(null, ["   ", @"C:\Projects\PaymentApi"]));

        Assert.IsType<ArgumentException>(exception.InnerException);
    }
}
