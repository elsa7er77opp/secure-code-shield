using System.Reflection;
using SecureCodeShield.Infrastructure.Storage;

namespace SecureCodeShield.Infrastructure.Tests;

public sealed class LocalAccountServiceContractTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"secure-code-shield-auth-{Guid.NewGuid():N}");

    [Fact]
    public void First_administrator_can_sign_in_without_persisting_the_raw_password()
    {
        Directory.CreateDirectory(tempDirectory);
        var serviceType = typeof(LocalStateDatabase).Assembly.GetType("SecureCodeShield.Infrastructure.Security.LocalAccountService");

        Assert.True(serviceType is not null, "Local account authentication must be implemented in Infrastructure.");
        if (serviceType is null)
        {
            return;
        }

        var service = Activator.CreateInstance(serviceType, tempDirectory);
        var create = serviceType.GetMethod("CreateFirstAdministrator", BindingFlags.Public | BindingFlags.Instance);
        var authenticate = serviceType.GetMethod("Authenticate", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(create is not null, "The local account service must create the first administrator.");
        Assert.True(authenticate is not null, "The local account service must authenticate a local administrator.");
        if (service is null || create is null || authenticate is null)
        {
            return;
        }

        var createResult = create.Invoke(service, ["admin", "S3cureCodeShield!2026"]);
        var authenticationResult = authenticate.Invoke(service, ["admin", "S3cureCodeShield!2026"]);
        var stateFile = Path.Combine(tempDirectory, "local-account.json");

        Assert.Equal("Success", createResult?.GetType().GetProperty("Outcome")?.GetValue(createResult)?.ToString());
        Assert.Equal("Success", authenticationResult?.GetType().GetProperty("Outcome")?.GetValue(authenticationResult)?.ToString());
        Assert.True(File.Exists(stateFile));
        Assert.DoesNotContain("S3cureCodeShield!2026", File.ReadAllText(stateFile));
    }

    [Fact]
    public void Five_failed_passwords_temporarily_lock_the_local_account()
    {
        Directory.CreateDirectory(tempDirectory);
        var serviceType = typeof(LocalStateDatabase).Assembly.GetType("SecureCodeShield.Infrastructure.Security.LocalAccountService");

        Assert.True(serviceType is not null, "Local account authentication must be implemented in Infrastructure.");
        if (serviceType is null)
        {
            return;
        }

        var service = Activator.CreateInstance(serviceType, tempDirectory);
        var create = serviceType.GetMethod("CreateFirstAdministrator", BindingFlags.Public | BindingFlags.Instance);
        var authenticate = serviceType.GetMethod("Authenticate", BindingFlags.Public | BindingFlags.Instance);
        if (service is null || create is null || authenticate is null)
        {
            return;
        }

        create.Invoke(service, ["admin", "S3cureCodeShield!2026"]);
        object? result = null;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            result = authenticate.Invoke(service, ["admin", "wrong-password"]);
        }

        Assert.Equal("Locked", result?.GetType().GetProperty("Outcome")?.GetValue(result)?.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
