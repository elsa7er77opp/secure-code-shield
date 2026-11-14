namespace SecureCodeShield.Desktop.Tests;

public sealed class AppLifecycleContractTests
{
    [Fact]
    public void Authenticated_handoff_shows_the_workstation_before_closing_login()
    {
        var appSource = File.ReadAllText(FindProjectFile("src", "SecureCodeShield.Desktop", "App.axaml.cs"));

        var showWorkspace = appSource.IndexOf("workspace.Show();", StringComparison.Ordinal);
        var closeLogin = appSource.IndexOf("login.Close();", StringComparison.Ordinal);

        Assert.True(showWorkspace >= 0, "The authenticated handoff must show the workstation.");
        Assert.True(closeLogin >= 0, "The authenticated handoff must close the login window after replacement.");
        Assert.True(showWorkspace < closeLogin,
            "The workstation must be visible before the old Login window closes to keep the desktop application lifetime alive.");
    }

    private static string FindProjectFile(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SecureCodeShield.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory!.FullName, .. segments]);
    }
}
