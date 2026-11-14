using Microsoft.Data.Sqlite;

namespace SecureCodeShield.Infrastructure.Storage;

/// <summary>
/// Owns the local SQLite lifecycle. Source files and raw secret evidence are never written to this database.
/// </summary>
public static class LocalStateDatabase
{
    private const int CurrentSchemaVersion = 2;

    public static LocalStateInitializationResult Initialize(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullDatabasePath = Path.GetFullPath(databasePath);
        var databaseDirectory = Path.GetDirectoryName(fullDatabasePath)
                                ?? throw new InvalidOperationException("A database directory is required.");
        Directory.CreateDirectory(databaseDirectory);

        var recoveredFromCorruption = false;
        var quarantinedPath = string.Empty;
        var backupPath = string.Empty;

        if (File.Exists(fullDatabasePath) && !HasPassingIntegrityCheck(fullDatabasePath))
        {
            recoveredFromCorruption = true;
            quarantinedPath = fullDatabasePath + ".corrupt-" + Guid.NewGuid().ToString("N");
            SqliteConnection.ClearAllPools();
            File.Move(fullDatabasePath, quarantinedPath);
        }
        else if (File.Exists(fullDatabasePath))
        {
            var existingSchemaVersion = GetExistingSchemaVersion(fullDatabasePath);
            if (existingSchemaVersion > 0 && existingSchemaVersion < CurrentSchemaVersion)
            {
                backupPath = fullDatabasePath + ".backup-v" + existingSchemaVersion + "-" + Guid.NewGuid().ToString("N");
                File.Copy(fullDatabasePath, backupPath, overwrite: false);
            }
        }

        using var connection = Open(fullDatabasePath);
        connection.Open();

        ExecuteNonQuery(connection, "PRAGMA journal_mode = WAL;");
        ExecuteNonQuery(connection, "PRAGMA foreign_keys = ON;");
        ExecuteNonQuery(connection, "PRAGMA synchronous = NORMAL;");
        ExecuteNonQuery(
            connection,
            """
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version INTEGER PRIMARY KEY,
                applied_utc TEXT NOT NULL
            );
            """);
        ExecuteNonQuery(
            connection,
            """
            INSERT OR IGNORE INTO schema_migrations (version, applied_utc)
            VALUES (1, CURRENT_TIMESTAMP);
            """);
        ExecuteNonQuery(
            connection,
            """
            CREATE TABLE IF NOT EXISTS scan_runs (
                scan_id TEXT PRIMARY KEY,
                project_id TEXT NOT NULL,
                engine_version TEXT NOT NULL,
                started_utc TEXT NOT NULL,
                completed_utc TEXT,
                outcome TEXT NOT NULL,
                eligible_file_count INTEGER NOT NULL,
                processed_file_count INTEGER NOT NULL,
                finding_count INTEGER NOT NULL,
                error_category TEXT
            );
            """);
        ExecuteNonQuery(
            connection,
            """
            CREATE TABLE IF NOT EXISTS audit_events (
                event_id TEXT PRIMARY KEY,
                occurred_utc TEXT NOT NULL,
                category TEXT NOT NULL,
                action TEXT NOT NULL,
                task_id TEXT
            );
            """);
        ExecuteNonQuery(
            connection,
            """
            INSERT OR IGNORE INTO schema_migrations (version, applied_utc)
            VALUES (2, CURRENT_TIMESTAMP);
            """);

        return new LocalStateInitializationResult(
            fullDatabasePath,
            CurrentSchemaVersion,
            recoveredFromCorruption,
            quarantinedPath,
            backupPath);
    }

    private static int GetExistingSchemaVersion(string databasePath)
    {
        using var connection = Open(databasePath);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COALESCE(MAX(version), 0) FROM sqlite_master JOIN schema_migrations ON 1 = 1 WHERE sqlite_master.type = 'table' AND sqlite_master.name = 'schema_migrations';";

        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static bool HasPassingIntegrityCheck(string databasePath)
    {
        try
        {
            using var connection = Open(databasePath);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA integrity_check;";

            return string.Equals(command.ExecuteScalar()?.ToString(), "ok", StringComparison.OrdinalIgnoreCase);
        }
        catch (SqliteException)
        {
            return false;
        }
    }

    private static SqliteConnection Open(string databasePath)
    {
        return new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = false
        }.ToString());
    }

    private static void ExecuteNonQuery(SqliteConnection connection, string commandText)
    {
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        command.ExecuteNonQuery();
    }
}
