using System.Security.Cryptography;
using SecureCodeShield.Domain.Attestation;
using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Tests;

public sealed class ReviewCertificateContractTests
{
    private static readonly DateTimeOffset Issued = new(2026, 9, 10, 14, 2, 11, TimeSpan.Zero);

    private static ReviewCertificatePayload BuildPayload(params FileDigest[] files) => new(
        ReviewCertificatePayload.CurrentSchemaVersion,
        "8f6d1c2e-0000-4000-8000-000000000001",
        "payments-api",
        Issued,
        "a.elreefy",
        "AB12 CD34 EF56 7890",
        "CyberBank Secure Code Shield 1.1.0",
        new RulePackIdentity("cyberbank.builtin", "1.0", new string('a', 64), 21),
        new ScopeDeclaration(files.Length, 2, [new ExclusionSummary("excluded-directory", 1), new ExclusionSummary("file-too-large", 1)]),
        [new SeverityTally(Severity.Critical, 1), new SeverityTally(Severity.High, 2), new SeverityTally(Severity.Medium, 0), new SeverityTally(Severity.Note, 3)],
        files);

    private static FileDigest Digest(string path, string content) =>
        new(path, Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content))), content.Length);

    [Fact]
    public void The_same_review_always_produces_the_same_bytes_to_sign()
    {
        var payload = BuildPayload(Digest("src/a.ts", "one"), Digest("src/b.ts", "two"));

        Assert.Equal(
            CanonicalPayloadWriter.ToCanonicalBytes(payload),
            CanonicalPayloadWriter.ToCanonicalBytes(payload));
    }

    [Fact]
    public void File_order_in_memory_does_not_change_the_signed_bytes()
    {
        var a = Digest("src/a.ts", "one");
        var b = Digest("src/b.ts", "two");

        var forward = CanonicalPayloadWriter.ToCanonicalJson(BuildPayload(a, b));
        var reversed = CanonicalPayloadWriter.ToCanonicalJson(BuildPayload(b, a));

        Assert.Equal(forward, reversed);
    }

    [Fact]
    public void Windows_separators_are_normalised_so_a_certificate_verifies_anywhere()
    {
        Assert.Equal("src/services/auth.ts", CanonicalPayloadWriter.NormalizePath(@"src\services\auth.ts"));
        Assert.Equal("src/services/auth.ts", CanonicalPayloadWriter.NormalizePath(@".\src\services\auth.ts"));
    }

    [Fact]
    public void The_certificate_claims_only_what_was_checked_never_that_the_code_is_safe()
    {
        var json = CanonicalPayloadWriter.ToCanonicalJson(BuildPayload(Digest("src/a.ts", "one")));

        Assert.Contains("It is not a warranty that the code is secure.", json, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sealed_certificate_round_trips_and_verifies()
    {
        var payload = BuildPayload(Digest("src/a.ts", "one"), Digest("src/b.ts", "two"));
        var canonical = CanonicalPayloadWriter.ToCanonicalBytes(payload);

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var seal = new CertificateSeal(
            CertificateSeal.EcdsaP256Sha256,
            ReviewCertificateDocument.HashPayload(canonical),
            Convert.ToBase64String(key.SignData(canonical, HashAlgorithmName.SHA256)),
            Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
            "AB12 CD34 EF56 7890");

        var file = ReviewCertificateDocument.Render(canonical, seal);

        Assert.True(ReviewCertificateDocument.TryParse(file, out var parsedBytes, out var parsedSeal, out var parseError), parseError);
        Assert.Equal(canonical, parsedBytes);

        var verification = CertificateVerifier.VerifySeal(parsedBytes, parsedSeal!);
        Assert.True(verification.IsValid, verification.Explanation);

        Assert.True(CanonicalPayloadReader.TryRead(parsedBytes, out var reread, out var readError), readError);
        Assert.Equal(payload.CertificateId, reread!.CertificateId);
        Assert.Equal(payload.SubjectName, reread.SubjectName);
        Assert.Equal(2, reread.Files.Count);
    }

    [Fact]
    public void Altering_one_byte_of_the_payload_invalidates_the_seal()
    {
        var payload = BuildPayload(Digest("src/a.ts", "one"));
        var canonical = CanonicalPayloadWriter.ToCanonicalBytes(payload);

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var seal = new CertificateSeal(
            CertificateSeal.EcdsaP256Sha256,
            ReviewCertificateDocument.HashPayload(canonical),
            Convert.ToBase64String(key.SignData(canonical, HashAlgorithmName.SHA256)),
            Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()),
            "AB12 CD34 EF56 7890");

        var tampered = (byte[])canonical.Clone();
        tampered[^2] ^= 0x01;

        Assert.False(CertificateVerifier.VerifySeal(tampered, seal).IsValid);
    }

    [Fact]
    public void A_signature_from_a_different_key_is_rejected()
    {
        var payload = BuildPayload(Digest("src/a.ts", "one"));
        var canonical = CanonicalPayloadWriter.ToCanonicalBytes(payload);

        using var real = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        using var impostor = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var seal = new CertificateSeal(
            CertificateSeal.EcdsaP256Sha256,
            ReviewCertificateDocument.HashPayload(canonical),
            Convert.ToBase64String(impostor.SignData(canonical, HashAlgorithmName.SHA256)),
            Convert.ToBase64String(real.ExportSubjectPublicKeyInfo()),
            "AB12 CD34 EF56 7890");

        Assert.False(CertificateVerifier.VerifySeal(canonical, seal).IsValid);
    }
}
