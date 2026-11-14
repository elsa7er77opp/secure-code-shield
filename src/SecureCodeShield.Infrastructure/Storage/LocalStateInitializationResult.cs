namespace SecureCodeShield.Infrastructure.Storage;

public sealed record LocalStateInitializationResult(
    string DatabasePath,
    int SchemaVersion,
    bool RecoveredFromCorruption,
    string QuarantinedPath,
    string BackupPath);
