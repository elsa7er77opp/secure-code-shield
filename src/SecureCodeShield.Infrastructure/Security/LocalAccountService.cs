using System.Security.Cryptography;
using System.Text.Json;

namespace SecureCodeShield.Infrastructure.Security;

public enum LocalAuthenticationOutcome
{
    Success,
    InvalidCredentials,
    Locked,
    NotConfigured,
    AlreadyConfigured,
    InvalidAccountDetails
}

public sealed record LocalAuthenticationResult(LocalAuthenticationOutcome Outcome, string Message);

public sealed class LocalAccountService
{
    private const int Pbkdf2Iterations = 600_000;
    private const int SaltLength = 32;
    private const int HashLength = 32;
    private const int MaximumFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);
    private readonly string stateFilePath;

    public LocalAccountService(string stateDirectory)
    {
        if (string.IsNullOrWhiteSpace(stateDirectory))
        {
            throw new ArgumentException("A local state directory is required.", nameof(stateDirectory));
        }

        Directory.CreateDirectory(stateDirectory);
        stateFilePath = Path.Combine(stateDirectory, "local-account.json");
    }

    public bool IsConfigured => File.Exists(stateFilePath);

    public LocalAuthenticationResult CreateFirstAdministrator(string username, string password)
    {
        if (IsConfigured)
        {
            return new(LocalAuthenticationOutcome.AlreadyConfigured, "A local administrator is already configured.");
        }

        if (!IsValidUsername(username) || !IsStrongPassword(password))
        {
            return new(LocalAuthenticationOutcome.InvalidAccountDetails, "Use a 3–64 character username and a strong password of at least 12 characters.");
        }

        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var record = new LocalAccountRecord(
            username.Trim(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(DerivePasswordHash(password, salt)),
            Pbkdf2Iterations,
            0,
            null);

        WriteAtomically(record);
        return new(LocalAuthenticationOutcome.Success, "Local administrator created.");
    }

    public LocalAuthenticationResult Authenticate(string username, string password)
    {
        if (!TryRead(out var record))
        {
            return new(LocalAuthenticationOutcome.NotConfigured, "Create the first local administrator before signing in.");
        }

        var now = DateTimeOffset.UtcNow;
        if (record.LockoutUntilUtc is { } lockoutUntil && lockoutUntil > now)
        {
            return new(LocalAuthenticationOutcome.Locked, "Too many failed attempts. Try again later.");
        }

        var suppliedHash = DerivePasswordHash(password, Convert.FromBase64String(record.PasswordSalt), record.Iterations);
        var usernameMatches = string.Equals(record.Username, username.Trim(), StringComparison.Ordinal);
        var passwordMatches = CryptographicOperations.FixedTimeEquals(suppliedHash, Convert.FromBase64String(record.PasswordHash));

        if (!usernameMatches || !passwordMatches)
        {
            var failedAttempts = record.FailedAttempts + 1;
            var locked = failedAttempts >= MaximumFailedAttempts;
            var updated = record with
            {
                FailedAttempts = failedAttempts,
                LockoutUntilUtc = locked ? now.Add(LockoutDuration) : null
            };
            WriteAtomically(updated);

            return locked
                ? new(LocalAuthenticationOutcome.Locked, "Too many failed attempts. The account is temporarily locked.")
                : new(LocalAuthenticationOutcome.InvalidCredentials, "Incorrect username or password.");
        }

        if (record.FailedAttempts != 0 || record.LockoutUntilUtc is not null)
        {
            WriteAtomically(record with { FailedAttempts = 0, LockoutUntilUtc = null });
        }

        return new(LocalAuthenticationOutcome.Success, "Signed in locally.");
    }

    private bool TryRead(out LocalAccountRecord record)
    {
        record = default!;
        if (!File.Exists(stateFilePath))
        {
            return false;
        }

        try
        {
            record = JsonSerializer.Deserialize<LocalAccountRecord>(File.ReadAllText(stateFilePath))!;
            return record is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private void WriteAtomically(LocalAccountRecord record)
    {
        var temporaryPath = $"{stateFilePath}.{Guid.NewGuid():N}.tmp";
        var payload = JsonSerializer.Serialize(record);
        File.WriteAllText(temporaryPath, payload);
        File.Move(temporaryPath, stateFilePath, overwrite: true);
    }

    private static byte[] DerivePasswordHash(string password, byte[] salt, int iterations = Pbkdf2Iterations)
        => Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA512, HashLength);

    private static bool IsValidUsername(string username)
        => !string.IsNullOrWhiteSpace(username) && username.Trim().Length is >= 3 and <= 64;

    private static bool IsStrongPassword(string password)
        => password.Length >= 12
           && password.Any(char.IsUpper)
           && password.Any(char.IsLower)
           && password.Any(char.IsDigit)
           && password.Any(character => !char.IsLetterOrDigit(character));

    private sealed record LocalAccountRecord(
        string Username,
        string PasswordSalt,
        string PasswordHash,
        int Iterations,
        int FailedAttempts,
        DateTimeOffset? LockoutUntilUtc);
}
