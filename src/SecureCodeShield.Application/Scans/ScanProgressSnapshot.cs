using SecureCodeShield.Domain.Scans;

namespace SecureCodeShield.Application.Scans;

/// <summary>
/// A UI-safe projection of scan progress. It carries counters and state, never file contents or raw evidence.
/// </summary>
public sealed record ScanProgressSnapshot(
    string TaskId,
    ScanTaskState State,
    int EligibleFileCount,
    int ProcessedFileCount,
    int PercentComplete,
    bool IsCancellable)
{
    public static ScanProgressSnapshot Create(
        string taskId,
        ScanTaskState state,
        int eligibleFileCount,
        int processedFileCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentOutOfRangeException.ThrowIfNegative(eligibleFileCount);
        ArgumentOutOfRangeException.ThrowIfNegative(processedFileCount);

        if (processedFileCount > eligibleFileCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processedFileCount),
                "Processed files cannot exceed eligible files.");
        }

        var percentComplete = eligibleFileCount == 0
            ? 0
            : (int)Math.Floor(processedFileCount * 100d / eligibleFileCount);

        return new ScanProgressSnapshot(
            taskId,
            state,
            eligibleFileCount,
            processedFileCount,
            percentComplete,
            state == ScanTaskState.Running);
    }
}
