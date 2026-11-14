using System.Reflection;
using SecureCodeShield.Desktop.ViewModels;

namespace SecureCodeShield.Desktop.Tests;

public sealed class SplashExperienceContractTests
{
    [Fact]
    public void Splash_state_exposes_determinate_progress_and_respects_reduced_motion()
    {
        var splashType = typeof(MainViewModel).Assembly.GetType("SecureCodeShield.Desktop.ViewModels.SplashViewModel");

        Assert.True(splashType is not null, "The desktop shell must expose a dedicated SplashViewModel.");
        if (splashType is null)
        {
            return;
        }

        var normal = Activator.CreateInstance(splashType, new object[] { false });
        var reduced = Activator.CreateInstance(splashType, new object[] { true });
        var progress = splashType.GetProperty("ProgressPercent", BindingFlags.Public | BindingFlags.Instance);
        var shouldRotate = splashType.GetProperty("ShouldRotateGlobe", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(progress is not null, "The splash must expose determinate loading progress.");
        Assert.True(shouldRotate is not null, "The splash must expose whether the globe can animate.");
        if (normal is null || reduced is null || progress is null || shouldRotate is null)
        {
            return;
        }

        Assert.Equal(72, progress.GetValue(normal));
        Assert.Equal(true, shouldRotate.GetValue(normal));
        Assert.Equal(false, shouldRotate.GetValue(reduced));
    }
}
