using System.Security.Cryptography;
using System.Text;
using SecureCodeShield.Domain.Attestation;

namespace SecureCodeShield.Domain.Tests;

public sealed class ManifestVerificationContractTests
{
    private static readonly Dictionary<string, string> Delivered = new(StringComparer.Ordinal)
    {
        ["src/a.ts"] = "export const a = 1;",
        ["src/b.ts"] = "export const b = 2;"
    };

    private static FileDigest Digest(string path, string content) =>
        new(path, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content))), content.Length);

    private static ManifestVerification Verify(IReadOnlyList<FileDigest> manifest, IDictionary<string, string> onDisk) =>
        CertificateVerifier.VerifyManifest(
            manifest,
            @"C:\delivery",
            path => Encoding.UTF8.GetBytes(onDisk[path]),
            onDisk.Keys.ToArray());

    [Fact]
    public void An_untouched_delivery_matches_exactly()
    {
        var manifest = Delivered.Select(pair => Digest(pair.Key, pair.Value)).ToArray();

        var result = Verify(manifest, new Dictionary<string, string>(Delivered, StringComparer.Ordinal));

        Assert.True(result.IsExactMatch);
        Assert.Equal(2, result.MatchedFileCount);
    }

    [Fact]
    public void A_file_changed_after_the_review_is_named()
    {
        var manifest = Delivered.Select(pair => Digest(pair.Key, pair.Value)).ToArray();
        var onDisk = new Dictionary<string, string>(Delivered, StringComparer.Ordinal)
        {
            ["src/b.ts"] = "export const b = 999;"
        };

        var result = Verify(manifest, onDisk);

        Assert.False(result.IsExactMatch);
        Assert.Equal("src/b.ts", Assert.Single(result.ModifiedFiles));
    }

    [Fact]
    public void A_file_the_certificate_lists_but_is_not_delivered_is_named()
    {
        var manifest = Delivered.Select(pair => Digest(pair.Key, pair.Value)).ToArray();
        var onDisk = new Dictionary<string, string>(StringComparer.Ordinal) { ["src/a.ts"] = Delivered["src/a.ts"] };

        var result = Verify(manifest, onDisk);

        Assert.Equal("src/b.ts", Assert.Single(result.MissingFiles));
    }

    [Fact]
    public void A_delivered_file_the_review_never_covered_is_reported_not_ignored()
    {
        var manifest = new[] { Digest("src/a.ts", Delivered["src/a.ts"]) };
        var onDisk = new Dictionary<string, string>(Delivered, StringComparer.Ordinal)
        {
            ["src/slipped-in.ts"] = "export const c = 3;"
        };

        var result = Verify(manifest, onDisk);

        Assert.False(result.IsExactMatch);
        Assert.Contains("src/slipped-in.ts", result.UnreviewedFiles);
    }

    [Fact]
    public void Separator_style_does_not_affect_the_comparison()
    {
        var manifest = new[] { Digest(@"src\a.ts", Delivered["src/a.ts"]) };
        var onDisk = new Dictionary<string, string>(StringComparer.Ordinal) { ["src/a.ts"] = Delivered["src/a.ts"] };

        var result = Verify(manifest, onDisk);

        Assert.Equal(1, result.MatchedFileCount);
    }
}
