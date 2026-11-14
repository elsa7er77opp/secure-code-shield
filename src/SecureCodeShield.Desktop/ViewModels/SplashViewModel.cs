using CommunityToolkit.Mvvm.ComponentModel;

namespace SecureCodeShield.Desktop.ViewModels;

public sealed partial class SplashViewModel : ObservableObject
{
    public SplashViewModel(bool prefersReducedMotion)
    {
        ShouldRotateGlobe = !prefersReducedMotion;
    }

    public int ProgressPercent => 72;

    public bool ShouldRotateGlobe { get; }

    public string InitializationStatus => "INITIALIZING SECURE CODE SHIELD...";

    public string InitializationDetail => "Preparing analysis engine and security modules";
}
