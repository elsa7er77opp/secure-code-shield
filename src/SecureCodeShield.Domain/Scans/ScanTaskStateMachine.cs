namespace SecureCodeShield.Domain.Scans;

/// <summary>
/// Restricts scan-task progression to transitions that can be shown and recovered safely in the desktop workspace.
/// </summary>
public static class ScanTaskStateMachine
{
    public static bool CanTransition(ScanTaskState current, ScanTaskState next)
    {
        return (current, next) switch
        {
            (ScanTaskState.Queued, ScanTaskState.Running) => true,
            (ScanTaskState.Queued, ScanTaskState.Cancelled) => true,
            (ScanTaskState.Running, ScanTaskState.Cancelling) => true,
            (ScanTaskState.Running, ScanTaskState.Completed) => true,
            (ScanTaskState.Running, ScanTaskState.Failed) => true,
            (ScanTaskState.Cancelling, ScanTaskState.Cancelled) => true,
            (ScanTaskState.Cancelling, ScanTaskState.Failed) => true,
            _ => false
        };
    }
}
