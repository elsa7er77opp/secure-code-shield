using System.Reflection;
using Microsoft.Data.Sqlite;

namespace SecureCodeShield.Infrastructure.Tests;

public sealed class LocalStateDatabaseContractTests : IDisposable
{
    private readonly string testDirectory = Path.Combine(
        Path.GetTempPath(),
        "secure-code-shield-state-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Initialize_creates_a_versioned_local_database()
    {
        Directory.CreateDirectory(testDirectory);
        var databasePath = Path.Combine(testDirectory, "state.db");

        var databaseType = Type.GetType(
            "SecureCodeShield.Infrastructure.Storage.LocalStateDatabase, SecureCodeShield.Infrastructure");
        var initializeMethod = databaseType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);

        Assert.True(initializeMethod is not null, "The storage contract must expose LocalStateDatabase.Initialize.");
        if (initializeMethod is null)
        {
            return;
        }

        var result = initializeMethod.Invoke(null, [databasePath]);
        var schemaVersion = Assert.IsType<int>(result?.GetType().GetProperty("SchemaVersion")?.GetValue(result));
        var recoveredFromCorruption = Assert.IsType<bool>(
            result?.GetType().GetProperty("RecoveredFromCorruption")?.GetValue(result));

        Assert.True(File.Exists(databasePath));
        Assert.Equal(2, schemaVersion);
        Assert.False(recoveredFromCorruption);
    }

    [Fact]
    public void Initialize_quarantines_a_corrupt_database_and_recovers_a_clean_local_store()
    {
        Directory.CreateDirectory(testDirectory);
        var databasePath = Path.Combine(testDirectory, "state.db");
        File.WriteAllBytes(databasePath, [0x00, 0x13, 0x37, 0xFF]);

        var databaseType = Type.GetType(
            "SecureCodeShield.Infrastructure.Storage.LocalStateDatabase, SecureCodeShield.Infrastructure");
        var initializeMethod = databaseType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);

        Assert.True(initializeMethod is not null, "The storage contract must expose LocalStateDatabase.Initialize.");
        if (initializeMethod is null)
        {
            return;
        }

        var result = initializeMethod.Invoke(null, [databasePath]);
        var recoveredFromCorruption = Assert.IsType<bool>(
            result?.GetType().GetProperty("RecoveredFromCorruption")?.GetValue(result));
        var quarantinedPath = Assert.IsType<string>(result?.GetType().GetProperty("QuarantinedPath")?.GetValue(result));

        Assert.True(recoveredFromCorruption);
        Assert.True(File.Exists(quarantinedPath));
        Assert.True(File.Exists(databasePath));
    }

    [Fact]
    public void Initialize_applies_the_safe_scan_and_audit_schema_upgrade()
    {
        Directory.CreateDirectory(testDirectory);
        var databasePath = Path.Combine(testDirectory, "state.db");

        var databaseType = Type.GetType(
            "SecureCodeShield.Infrastructure.Storage.LocalStateDatabase, SecureCodeShield.Infrastructure");
        var initializeMethod = databaseType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);

        Assert.True(initializeMethod is not null, "The storage contract must expose LocalStateDatabase.Initialize.");
        if (initializeMethod is null)
        {
            return;
        }

        var result = initializeMethod.Invoke(null, [databasePath]);
        var schemaVersion = Assert.IsType<int>(result?.GetType().GetProperty("SchemaVersion")?.GetValue(result));

        Assert.Equal(2, schemaVersion);

        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name IN ('scan_runs', 'audit_events');";

        Assert.Equal(2L, Assert.IsType<long>(command.ExecuteScalar()));
    }

    [Fact]
    public void Initialize_creates_a_backup_before_upgrading_an_existing_local_store()
    {
        Directory.CreateDirectory(testDirectory);
        var databasePath = Path.Combine(testDirectory, "state.db");

        using (var existingStore = new SqliteConnection($"Data Source={databasePath};Pooling=False"))
        {
            existingStore.Open();
            using var createSchema = existingStore.CreateCommand();
            createSchema.CommandText =
                "CREATE TABLE schema_migrations (version INTEGER PRIMARY KEY, applied_utc TEXT NOT NULL); INSERT INTO schema_migrations (version, applied_utc) VALUES (1, CURRENT_TIMESTAMP);";
            createSchema.ExecuteNonQuery();
        }

        var databaseType = Type.GetType(
            "SecureCodeShield.Infrastructure.Storage.LocalStateDatabase, SecureCodeShield.Infrastructure");
        var initializeMethod = databaseType?.GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static);

        Assert.True(initializeMethod is not null, "The storage contract must expose LocalStateDatabase.Initialize.");
        if (initializeMethod is null)
        {
            return;
        }

        var result = initializeMethod.Invoke(null, [databasePath]);
        var backupPath = result?.GetType().GetProperty("BackupPath")?.GetValue(result) as string;

        Assert.False(string.IsNullOrWhiteSpace(backupPath));
        Assert.True(File.Exists(backupPath));
        Assert.NotEqual(Path.GetFullPath(databasePath), Path.GetFullPath(backupPath));
        Assert.Equal(2, Assert.IsType<int>(result?.GetType().GetProperty("SchemaVersion")?.GetValue(result)));
    }

    public void Dispose()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }
}
