using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using SecureCodeShield.Desktop.ViewModels;
using System;

namespace SecureCodeShield.Desktop.Views;

public partial class SplashWindow : Window
{
    private readonly DispatcherTimer? rotationTimer;
    private readonly Image? rotatingGlobe;
    private double rotationAngle;

    public SplashWindow()
        : this(new SplashViewModel(prefersReducedMotion: false))
    {
    }

    public SplashWindow(SplashViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        rotatingGlobe = this.FindControl<Image>("RotatingGlobe");

        if (viewModel.ShouldRotateGlobe && rotatingGlobe is not null)
        {
            rotationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(42) };
            rotationTimer.Tick += (_, _) => RotateGlobe();
            rotationTimer.Start();
        }

        Closed += (_, _) => rotationTimer?.Stop();
    }

    private void RotateGlobe()
    {
        rotationAngle = (rotationAngle + 0.42) % 360;

        if (rotatingGlobe?.RenderTransform is RotateTransform rotation)
        {
            rotation.Angle = rotationAngle;
        }
    }
}
