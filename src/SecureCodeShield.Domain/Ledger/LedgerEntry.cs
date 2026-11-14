namespace SecureCodeShield.Domain.Ledger;

/// <summary>
/// One recorded event in the local review ledger.
/// <para>
/// Every entry carries the hash of the entry before it, so the entries form a chain. Editing or
/// removing any earlier entry changes its hash and breaks every entry after it, which is what makes
/// the record tamper-evident rather than merely append-only.
/// </para>
/// </summary>
/// <param name="Sequence">One-based position in the chain.</param>
/// <param name="PreviousEntryHash">Hash of the previous entry, or the genesis hash for the first.</param>
/// <param name="OccurredAtUtc">When the event happened, in UTC.</param>
/// <param name="Action">Stable machine action name, for example "review.completed".</param>
/// <param name="Detail">Short, non-sensitive description. Never a secret, a source line or a path outside the project.</param>
/// <param name="EntryHash">Hash over the fields above; recomputed on verification.</param>
public sealed record LedgerEntry(
    long Sequence,
    string PreviousEntryHash,
    DateTimeOffset OccurredAtUtc,
    string Action,
    string Detail,
    string EntryHash);
