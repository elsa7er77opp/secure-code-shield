using Microsoft.Data.Sqlite;

namespace SecureCodeShield.Infrastructure.Storage;

/// <summary>
/// Writes safe scan outcomes only. Source text, paths, raw evidence, and credentials are deliberately absent from this API.
/// </summary>
public static class LocalScanHistoryStore
{
    public static void SaveCompletedRun(
        string databasePath,
        string scanId,
        string projectId,
        string engineVersion,
        DateTimeOffset startedUtc,
        DateTimeOffset completedUtc,
        int eligibleFileCount,
        int processedFileCount,
        int findingCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(scanId);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(engineVersion);
        ArgumentOutOfRangeException.ThrowIfNegative(eligibleFileCount);
        ArgumentOutOfRangeException.ThrowIfNegative(processedFileCount);
        ArgumentOutOfRangeException.ThrowIfNegative(findingCount);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        }.ToString());
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO scan_runs (
                scan_id, project_id, engine_version, started_utc, completed_utc, outcome,
                eligible_file_count, processed_file_count, finding_count, error_category)
            VALUES (
                $scanId, $projectId, $engineVersion, $startedUtc, $completedUtc, 'Completed',
                $eligibleFileCount, $processedFileCount, $findingCount, NULL);
            """;
        command.Parameters.AddWithValue("$scanId", scanId);
        command.Parameters.AddWithValue("$projectId", projectId);
        command.Parameters.AddWithValue("$engineVersion", engineVersion);
        command.Parameters.AddWithValue("$startedUtc", startedUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$completedUtc", completedUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$eligibleFileCount", eligibleFileCount);
        command.Parameters.AddWithValue("$processedFileCount", processedFileCount);
        command.Parameters.AddWithValue("$findingCount", findingCount);
        command.ExecuteNonQuery();
        transaction.Commit();
    }

    public static void RecordAuditEvent(
        string databasePath,
        string eventId,
        DateTimeOffset occurredUtc,
        string category,
        string action,
        string? taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        }.ToString());
        connection.Open();

        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO audit_events (event_id, occurred_utc, category, action, task_id)
            VALUES ($eventId, $occurredUtc, $category, $action, $taskId);
            """;
        command.Parameters.AddWithValue("$eventId", eventId);
        command.Parameters.AddWithValue("$occurredUtc", occurredUtc.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$category", category);
        command.Parameters.AddWithValue("$action", action);
        command.Parameters.AddWithValue("$taskId", (object?)taskId ?? DBNull.Value);
        command.ExecuteNonQuery();
        transaction.Commit();
    }
}
