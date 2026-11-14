using System.Reflection;

namespace SecureCodeShield.Application.Tests;

public sealed class ScanTaskCoordinatorContractTests
{
    [Fact]
    public void Register_creates_a_queued_safe_progress_snapshot()
    {
        var applicationAssembly = Assembly.Load("SecureCodeShield.Application");
        var coordinatorType = applicationAssembly.GetType("SecureCodeShield.Application.Scans.ScanTaskCoordinator");
        var register = coordinatorType?.GetMethod("Register", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(coordinatorType is not null, "The application layer must expose ScanTaskCoordinator.");
        Assert.True(register is not null, "The task coordinator contract must expose Register.");
        if (coordinatorType is null || register is null)
        {
            return;
        }

        var coordinator = Activator.CreateInstance(coordinatorType);
        var snapshot = register.Invoke(coordinator, ["task-local-01", 27]);

        Assert.Equal("task-local-01", snapshot?.GetType().GetProperty("TaskId")?.GetValue(snapshot));
        Assert.Equal(0, snapshot?.GetType().GetProperty("ProcessedFileCount")?.GetValue(snapshot));
        Assert.Equal(0, snapshot?.GetType().GetProperty("PercentComplete")?.GetValue(snapshot));
        Assert.Equal("Queued", snapshot?.GetType().GetProperty("State")?.GetValue(snapshot)?.ToString());
        Assert.False(Assert.IsType<bool>(snapshot?.GetType().GetProperty("IsCancellable")?.GetValue(snapshot)));
    }

    [Fact]
    public void Start_moves_a_registered_task_to_running_and_makes_it_cancellable()
    {
        var applicationAssembly = Assembly.Load("SecureCodeShield.Application");
        var coordinatorType = applicationAssembly.GetType("SecureCodeShield.Application.Scans.ScanTaskCoordinator");
        var register = coordinatorType?.GetMethod("Register", BindingFlags.Public | BindingFlags.Instance);
        var start = coordinatorType?.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(register is not null, "The task coordinator contract must expose Register.");
        Assert.True(start is not null, "The task coordinator contract must expose Start.");
        if (coordinatorType is null || register is null || start is null)
        {
            return;
        }

        var coordinator = Activator.CreateInstance(coordinatorType);
        register.Invoke(coordinator, ["task-local-02", 5]);
        var snapshot = start.Invoke(coordinator, ["task-local-02"]);

        Assert.Equal("Running", snapshot?.GetType().GetProperty("State")?.GetValue(snapshot)?.ToString());
        Assert.True(Assert.IsType<bool>(snapshot?.GetType().GetProperty("IsCancellable")?.GetValue(snapshot)));
    }

    [Fact]
    public void Cancel_moves_a_running_task_through_cancelling_to_cancelled()
    {
        var applicationAssembly = Assembly.Load("SecureCodeShield.Application");
        var coordinatorType = applicationAssembly.GetType("SecureCodeShield.Application.Scans.ScanTaskCoordinator");
        var register = coordinatorType?.GetMethod("Register", BindingFlags.Public | BindingFlags.Instance);
        var start = coordinatorType?.GetMethod("Start", BindingFlags.Public | BindingFlags.Instance);
        var requestCancellation = coordinatorType?.GetMethod("RequestCancellation", BindingFlags.Public | BindingFlags.Instance);
        var confirmCancellation = coordinatorType?.GetMethod("ConfirmCancellation", BindingFlags.Public | BindingFlags.Instance);

        Assert.True(register is not null, "The task coordinator contract must expose Register.");
        Assert.True(start is not null, "The task coordinator contract must expose Start.");
        Assert.True(requestCancellation is not null, "The task coordinator contract must expose RequestCancellation.");
        Assert.True(confirmCancellation is not null, "The task coordinator contract must expose ConfirmCancellation.");
        if (coordinatorType is null || register is null || start is null || requestCancellation is null || confirmCancellation is null)
        {
            return;
        }

        var coordinator = Activator.CreateInstance(coordinatorType);
        register.Invoke(coordinator, ["task-local-03", 8]);
        start.Invoke(coordinator, ["task-local-03"]);

        var cancelling = requestCancellation.Invoke(coordinator, ["task-local-03"]);
        var cancelled = confirmCancellation.Invoke(coordinator, ["task-local-03"]);

        Assert.Equal("Cancelling", cancelling?.GetType().GetProperty("State")?.GetValue(cancelling)?.ToString());
        Assert.Equal("Cancelled", cancelled?.GetType().GetProperty("State")?.GetValue(cancelled)?.ToString());
        Assert.False(Assert.IsType<bool>(cancelled?.GetType().GetProperty("IsCancellable")?.GetValue(cancelled)));
    }
}
