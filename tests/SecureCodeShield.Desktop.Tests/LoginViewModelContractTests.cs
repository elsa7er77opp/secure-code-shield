using System.Reflection;
using SecureCodeShield.Desktop.ViewModels;

namespace SecureCodeShield.Desktop.Tests;

public sealed class LoginViewModelContractTests : IDisposable
{
    private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), $"secure-code-shield-login-{Guid.NewGuid():N}");

    [Fact]
    public void First_run_requires_password_confirmation_before_local_sign_in()
    {
        Directory.CreateDirectory(tempDirectory);
        var loginType = typeof(MainViewModel).Assembly.GetType("SecureCodeShield.Desktop.ViewModels.LoginViewModel");

        Assert.True(loginType is not null, "The desktop shell must expose LoginViewModel.");
        if (loginType is null)
        {
            return;
        }

        var login = Activator.CreateInstance(loginType, [tempDirectory]);
        var submit = loginType.GetMethod("Submit", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(submit is not null, "The login view model must submit local credentials.");
        if (login is null || submit is null)
        {
            return;
        }

        var mismatch = submit.Invoke(login, ["admin", "S3cureCodeShield!2026", "different"]);
        var created = submit.Invoke(login, ["admin", "S3cureCodeShield!2026", "S3cureCodeShield!2026"]);
        var signedIn = submit.Invoke(login, ["admin", "S3cureCodeShield!2026", string.Empty]);

        Assert.Equal("InvalidAccountDetails", mismatch?.GetType().GetProperty("Outcome")?.GetValue(mismatch)?.ToString());
        Assert.Equal("Success", created?.GetType().GetProperty("Outcome")?.GetValue(created)?.ToString());
        Assert.Equal("Success", signedIn?.GetType().GetProperty("Outcome")?.GetValue(signedIn)?.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
