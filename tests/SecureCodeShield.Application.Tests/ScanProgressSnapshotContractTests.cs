using System.Reflection;

namespace SecureCodeShield.Application.Tests;

public sealed class ScanProgressSnapshotContractTests
{
    [Fact]
    public void Create_reports_measurable_progress_and_a_safe_cancellation_affordance_for_running_scan()
    {
        var applicationAssembly = Assembly.Load("SecureCodeShield.Application");
        var domainAssembly = Assembly.Load("SecureCodeShield.Domain");
        var snapshotType = applicationAssembly.GetType("SecureCodeShield.Application.Scans.ScanProgressSnapshot");
        var scanStateType = domainAssembly.GetType("SecureCodeShield.Domain.Scans.ScanTaskState");
        var createMethod = snapshotType?.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        Assert.True(scanStateType is not null, "The domain scan state must be available to application progress reporting.");
        Assert.True(createMethod is not null, "The application contract must expose ScanProgressSnapshot.Create.");
        if (scanStateType is null || createMethod is null)
        {
            return;
        }

        var running = Enum.Parse(scanStateType, "Running");
        var snapshot = createMethod.Invoke(null, ["scan-42", running, 100, 42]);

        var percentComplete = Assert.IsType<int>(snapshot?.GetType().GetProperty("PercentComplete")?.GetValue(snapshot));
        var isCancellable = Assert.IsType<bool>(snapshot?.GetType().GetProperty("IsCancellable")?.GetValue(snapshot));
        var taskId = Assert.IsType<string>(snapshot?.GetType().GetProperty("TaskId")?.GetValue(snapshot));

        Assert.Equal("scan-42", taskId);
        Assert.Equal(42, percentComplete);
        Assert.True(isCancellable);
    }
}
