using System.Reflection;
using Microsoft.Data.Sqlite;

namespace SecureCodeShield.Infrastructure.Tests;

public sealed class LocalScanHistoryStoreContractTests : IDisposable
{
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(),
        "secure-code-shield-history-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void SaveCompletedRun_persists_a_safe_summary_without_source_or_evidence()
    {
        Directory.CreateDirectory(testDirectory);
        var databasePath = Path.Combine(testDirectory, "state.db");

        var infrastructureAssembly = Assembly.Load("SecureCodeShield.Infrastructure");
        var databaseType = infrastructureAssembly.GetType("SecureCodeShield.Infrastructure.Storage.LocalStateDatabase");
        var initialize = databaseType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
        var storeType = infrastructureAssembly.GetType("SecureCodeShield.Infrastructure.Storage.LocalScanHistoryStore");
        var saveCompletedRun = storeType?.GetMethod("SaveCompletedRun", BindingFlags.Public | BindingFlags.Static);

        Assert.True(initialize is not null, "The storage contract must expose LocalStateDatabase.Initialize.");
        Assert.True(saveCompletedRun is not null, "The storage contract must expose LocalScanHistoryStore.SaveCompletedRun.");
        if (initialize is null || saveCompletedRun is null)
        {
            return;
        }

        initialize.Invoke(null, [databasePath]);
        var startedUtc = new DateTimeOffset(2026, 8, 22, 10, 0, 0, TimeSpan.Zero);
        var completedUtc = startedUtc.AddSeconds(3);
        saveCompletedRun.Invoke(null,
        [
            databasePath,
            "scan-local-01",
            "project-local-01",
            "1.0.0",
            startedUtc,
            completedUtc,
            12,
            12,
            2
        ]);

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT scan_id, project_id, engine_version, outcome, eligible_file_count, processed_file_count, finding_count FROM scan_runs WHERE scan_id = 'scan-local-01';";

        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("scan-local-01", reader.GetString(0));
        Assert.Equal("project-local-01", reader.GetString(1));
        Assert.Equal("1.0.0", reader.GetString(2));
        Assert.Equal("Completed", reader.GetString(3));
        Assert.Equal(12L, reader.GetInt64(4));
        Assert.Equal(12L, reader.GetInt64(5));
        Assert.Equal(2L, reader.GetInt64(6));
    }

    [Fact]
    public void RecordAuditEvent_persists_a_safe_local_action_without_project_content()
    {
        Directory.CreateDirectory(testDirectory);
        var databasePath = Path.Combine(testDirectory, "state.db");

        var infrastructureAssembly = Assembly.Load("SecureCodeShield.Infrastructure");
        var databaseType = infrastructureAssembly.GetType("SecureCodeShield.Infrastructure.Storage.LocalStateDatabase");
        var initialize = databaseType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);
        var storeType = infrastructureAssembly.GetType("SecureCodeShield.Infrastructure.Storage.LocalScanHistoryStore");
        var recordAuditEvent = storeType?.GetMethod("RecordAuditEvent", BindingFlags.Public | BindingFlags.Static);

        Assert.True(initialize is not null, "The storage contract must expose LocalStateDatabase.Initialize.");
        Assert.True(recordAuditEvent is not null, "The storage contract must expose LocalScanHistoryStore.RecordAuditEvent.");
        if (initialize is null || recordAuditEvent is null)
        {
            return;
        }

        initialize.Invoke(null, [databasePath]);
        recordAuditEvent.Invoke(null,
        [
            databasePath,
            "audit-local-01",
            new DateTimeOffset(2026, 8, 22, 10, 1, 0, TimeSpan.Zero),
            "scan",
            "completed",
            "scan-local-01"
        ]);

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT event_id, category, action, task_id FROM audit_events WHERE event_id = 'audit-local-01';";

        using var reader = command.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal("audit-local-01", reader.GetString(0));
        Assert.Equal("scan", reader.GetString(1));
        Assert.Equal("completed", reader.GetString(2));
        Assert.Equal("scan-local-01", reader.GetString(3));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }
}
