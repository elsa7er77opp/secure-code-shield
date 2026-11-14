using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SecureCodeShield.Domain.Ledger;

/// <summary>
/// The chain maths for the review ledger. Pure and storage-free: the same inputs always produce the
/// same hashes on any machine, which is what allows a chain to be verified somewhere it was not written.
/// </summary>
public static class LedgerChain
{
    /// <summary>The value a first entry uses in place of a previous hash.</summary>
    public const string GenesisHash = "0000000000000000000000000000000000000000000000000000000000000000";

    private const char FieldSeparator = '\u001F';

    /// <summary>
    /// Hashes the content of an entry. The separator is a character that cannot appear in the fields,
    /// so no combination of values can be rearranged into a different entry with the same hash.
    /// </summary>
    public static string ComputeEntryHash(
        long sequence,
        string previousEntryHash,
        DateTimeOffset occurredAtUtc,
        string action,
        string detail)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequence);
        ArgumentException.ThrowIfNullOrWhiteSpace(previousEntryHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(detail);

        var canonical = string.Create(CultureInfo.InvariantCulture, $"{sequence}{FieldSeparator}{previousEntryHash}{FieldSeparator}{occurredAtUtc.ToUniversalTime():O}{FieldSeparator}{action}{FieldSeparator}{detail}");

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    /// <summary>Builds the next entry for a chain whose current tail is <paramref name="previous"/>.</summary>
    public static LedgerEntry Append(
        LedgerEntry? previous,
        DateTimeOffset occurredAtUtc,
        string action,
        string detail)
    {
        var sequence = (previous?.Sequence ?? 0) + 1;
        var previousHash = previous?.EntryHash ?? GenesisHash;
        var entryHash = ComputeEntryHash(sequence, previousHash, occurredAtUtc, action, detail);

        return new LedgerEntry(sequence, previousHash, occurredAtUtc, action, detail, entryHash);
    }

    /// <summary>
    /// Walks a whole chain and reports the first entry that does not hold.
    /// A chain is intact when the sequence is unbroken, every link points at the real previous hash,
    /// and every entry hash still matches its own content.
    /// </summary>
    public static LedgerVerification Verify(IReadOnlyList<LedgerEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var expectedPrevious = GenesisHash;

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var expectedSequence = index + 1;

            if (entry.Sequence != expectedSequence)
            {
                return LedgerVerification.Broken(expectedSequence, $"Entry {index + 1} claims sequence {entry.Sequence}; the chain expects {expectedSequence}. An entry was removed or reordered.");
            }

            if (!string.Equals(entry.PreviousEntryHash, expectedPrevious, StringComparison.Ordinal))
            {
                return LedgerVerification.Broken(entry.Sequence, $"Entry {entry.Sequence} does not link to the entry before it. An earlier entry was changed or removed.");
            }

            var recomputed = ComputeEntryHash(entry.Sequence, entry.PreviousEntryHash, entry.OccurredAtUtc, entry.Action, entry.Detail);
            if (!string.Equals(entry.EntryHash, recomputed, StringComparison.Ordinal))
            {
                return LedgerVerification.Broken(entry.Sequence, $"Entry {entry.Sequence} has been edited since it was recorded.");
            }

            expectedPrevious = entry.EntryHash;
        }

        return LedgerVerification.Intact(entries.Count);
    }
}

/// <summary>The outcome of verifying a ledger, written so the message can be shown to a non-technical reader.</summary>
/// <param name="IsIntact">True when every entry holds.</param>
/// <param name="EntriesChecked">How many entries were walked before the result was decided.</param>
/// <param name="BrokenAtSequence">The first sequence that failed, or null when the chain is intact.</param>
/// <param name="Explanation">Plain description of what failed, or a confirmation when intact.</param>
public sealed record LedgerVerification(
    bool IsIntact,
    int EntriesChecked,
    long? BrokenAtSequence,
    string Explanation)
{
    public static LedgerVerification Intact(int entriesChecked) =>
        new(true, entriesChecked, null, entriesChecked == 0
            ? "The ledger is empty."
            : $"All {entriesChecked} entries are intact and in order.");

    public static LedgerVerification Broken(long sequence, string explanation) =>
        new(false, (int)Math.Max(sequence - 1, 0), sequence, explanation);
}
