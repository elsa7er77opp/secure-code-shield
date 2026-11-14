using CommunityToolkit.Mvvm.ComponentModel;
using SecureCodeShield.Infrastructure.Security;
using System;

namespace SecureCodeShield.Desktop.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly LocalAccountService localAccountService;

    public LoginViewModel(string stateDirectory)
    {
        localAccountService = new LocalAccountService(stateDirectory);
        StatusMessage = IsFirstRun
            ? "Create the first local administrator to protect this workstation."
            : "Sign in to unlock the local analysis workstation.";
    }

    public bool IsFirstRun => !localAccountService.IsConfigured;

    [ObservableProperty]
    public partial string StatusMessage { get; set; }

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    public LocalAuthenticationResult Submit(string username, string password, string passwordConfirmation)
    {
        LocalAuthenticationResult result;
        if (IsFirstRun)
        {
            if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
            {
                result = new(LocalAuthenticationOutcome.InvalidAccountDetails, "Password confirmation does not match.");
            }
            else
            {
                result = localAccountService.CreateFirstAdministrator(username, password);
            }
        }
        else
        {
            result = localAccountService.Authenticate(username, password);
        }

        IsAuthenticated = result.Outcome == LocalAuthenticationOutcome.Success;
        StatusMessage = result.Message;
        OnPropertyChanged(nameof(IsFirstRun));
        return result;
    }
}
