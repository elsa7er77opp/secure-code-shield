using SecureCodeShield.Domain.Ledger;

namespace SecureCodeShield.Domain.Tests;

public sealed class LedgerChainContractTests
{
    private static readonly DateTimeOffset Moment = new(2026, 9, 10, 14, 2, 11, TimeSpan.Zero);

    private static List<LedgerEntry> BuildChain(int length)
    {
        var entries = new List<LedgerEntry>();
        LedgerEntry? previous = null;

        for (var index = 1; index <= length; index++)
        {
            previous = LedgerChain.Append(previous, Moment.AddMinutes(index), "review.completed", $"run {index}");
            entries.Add(previous);
        }

        return entries;
    }

    [Fact]
    public void The_first_entry_links_to_the_genesis_hash()
    {
        var entry = LedgerChain.Append(null, Moment, "review.started", "payments-api");

        Assert.Equal(1, entry.Sequence);
        Assert.Equal(LedgerChain.GenesisHash, entry.PreviousEntryHash);
    }

    [Fact]
    public void Each_entry_links_to_the_one_before_it()
    {
        var chain = BuildChain(4);

        for (var index = 1; index < chain.Count; index++)
        {
            Assert.Equal(chain[index - 1].EntryHash, chain[index].PreviousEntryHash);
        }
    }

    [Fact]
    public void The_same_content_always_hashes_the_same()
    {
        var first = LedgerChain.ComputeEntryHash(1, LedgerChain.GenesisHash, Moment, "review.completed", "payments-api");
        var second = LedgerChain.ComputeEntryHash(1, LedgerChain.GenesisHash, Moment, "review.completed", "payments-api");

        Assert.Equal(first, second);
    }

    [Fact]
    public void An_intact_chain_verifies()
    {
        var result = LedgerChain.Verify(BuildChain(5));

        Assert.True(result.IsIntact, result.Explanation);
        Assert.Equal(5, result.EntriesChecked);
        Assert.Null(result.BrokenAtSequence);
    }

    [Fact]
    public void Editing_an_entry_breaks_verification_at_that_entry()
    {
        var chain = BuildChain(5);
        chain[2] = chain[2] with { Detail = "run 3 (quietly changed)" };

        var result = LedgerChain.Verify(chain);

        Assert.False(result.IsIntact);
        Assert.Equal(3, result.BrokenAtSequence);
    }

    [Fact]
    public void Removing_an_entry_breaks_verification()
    {
        var chain = BuildChain(5);
        chain.RemoveAt(2);

        var result = LedgerChain.Verify(chain);

        Assert.False(result.IsIntact);
    }

    [Fact]
    public void Reordering_entries_breaks_verification()
    {
        var chain = BuildChain(4);
        (chain[1], chain[2]) = (chain[2], chain[1]);

        var result = LedgerChain.Verify(chain);

        Assert.False(result.IsIntact);
    }

    [Fact]
    public void An_empty_ledger_is_intact_and_says_so_plainly()
    {
        var result = LedgerChain.Verify([]);

        Assert.True(result.IsIntact);
        Assert.Equal("The ledger is empty.", result.Explanation);
    }
}
