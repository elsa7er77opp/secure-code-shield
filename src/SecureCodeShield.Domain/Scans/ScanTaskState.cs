namespace SecureCodeShield.Domain.Scans;

public enum ScanTaskState
{
    Queued,
    Running,
    Cancelling,
    Cancelled,
    Completed,
    Failed
}
