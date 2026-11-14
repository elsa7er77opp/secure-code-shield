using Avalonia.Controls;
using Avalonia.Interactivity;
using SecureCodeShield.Desktop.ViewModels;
using System;
using System.IO;

namespace SecureCodeShield.Desktop.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
        : this(new LoginViewModel(GetDefaultStateDirectory()))
    {
    }

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public event EventHandler? Authenticated;

    private void ContinueClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is not LoginViewModel viewModel)
        {
            return;
        }

        var result = viewModel.Submit(
            UsernameBox.Text ?? string.Empty,
            PasswordBox.Text ?? string.Empty,
            ConfirmationBox.Text ?? string.Empty);

        PasswordBox.Text = string.Empty;
        ConfirmationBox.Text = string.Empty;

        if (result.Outcome == SecureCodeShield.Infrastructure.Security.LocalAuthenticationOutcome.Success)
        {
            Authenticated?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string GetDefaultStateDirectory()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CyberBank",
            "SecureCodeShield");
}
