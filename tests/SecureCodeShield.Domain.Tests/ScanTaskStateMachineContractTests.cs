using System.Reflection;

namespace SecureCodeShield.Domain.Tests;

public sealed class ScanTaskStateMachineContractTests
{
    [Fact]
    public void CanTransition_requires_a_running_scan_to_enter_cancelling_before_cancelled()
    {
        var domainAssembly = Assembly.Load("SecureCodeShield.Domain");
        var stateType = domainAssembly.GetType("SecureCodeShield.Domain.Scans.ScanTaskState");
        var machineType = domainAssembly.GetType("SecureCodeShield.Domain.Scans.ScanTaskStateMachine");
        var canTransitionMethod = machineType?.GetMethod("CanTransition", BindingFlags.Public | BindingFlags.Static);

        Assert.True(stateType is not null, "The scan task contract must expose ScanTaskState.");
        Assert.True(canTransitionMethod is not null, "The scan task contract must expose ScanTaskStateMachine.CanTransition.");
        if (stateType is null || canTransitionMethod is null)
        {
            return;
        }

        var running = Enum.Parse(stateType, "Running");
        var cancelling = Enum.Parse(stateType, "Cancelling");
        var cancelled = Enum.Parse(stateType, "Cancelled");

        var canRequestCancellation = Assert.IsType<bool>(canTransitionMethod.Invoke(null, [running, cancelling]));
        var canSkipCancellationProtocol = Assert.IsType<bool>(canTransitionMethod.Invoke(null, [running, cancelled]));

        Assert.True(canRequestCancellation);
        Assert.False(canSkipCancellationProtocol);
    }
}
