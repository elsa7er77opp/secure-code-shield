using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SecureCodeShield.Desktop.ViewModels;
using SecureCodeShield.Desktop.Views;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SecureCodeShield.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var workspace = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
            var splash = new SplashWindow(new SplashViewModel(prefersReducedMotion: false));
            var login = new LoginWindow(new LoginViewModel(GetLocalStateDirectory()));

            desktop.MainWindow = login;
            login.Authenticated += (_, _) =>
            {
                desktop.MainWindow = workspace;
                workspace.Show();
                login.Close();
            };
            splash.Show();
            _ = ShowLoginAfterInitializationAsync(splash, login);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task ShowLoginAfterInitializationAsync(SplashWindow splash, LoginWindow login)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(2500));
        splash.Close();
        login.Show();
    }

    private static string GetLocalStateDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CyberBank",
            "SecureCodeShield");
}
