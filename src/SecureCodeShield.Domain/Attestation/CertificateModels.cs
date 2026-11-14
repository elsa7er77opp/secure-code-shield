using SecureCodeShield.Domain.Findings;

namespace SecureCodeShield.Domain.Attestation;

/// <summary>
/// One reviewed file, identified by content rather than by name. This is what lets a recipient
/// prove that the bytes they received are the bytes that were reviewed.
/// </summary>
public sealed record FileDigest(string RelativePath, string Sha256, long SizeBytes);

/// <summary>
/// A category of file that was deliberately not reviewed, and how many fell into it.
/// An audit reader looks at this before the findings: what was skipped decides what the review is worth.
/// </summary>
public sealed record ExclusionSummary(string Reason, int FileCount);

/// <summary>
/// The honest boundary of the review. Everything here is a statement about coverage, not about safety.
/// </summary>
/// <param name="ReviewedFileCount">Files that were read and analysed.</param>
/// <param name="ExcludedFileCount">Files inside the root that were not analysed.</param>
/// <param name="Exclusions">Why files were excluded, grouped by reason.</param>
/// <param name="Note">Fixed statement of what the review does and does not cover.</param>
public sealed record ScopeDeclaration(
    int ReviewedFileCount,
    int ExcludedFileCount,
    IReadOnlyList<ExclusionSummary> Exclusions,
    string Note = "Static local review of eligible text files only. No code was executed, no dependency was resolved, and no file left the reviewing machine.");

/// <summary>
/// The exact rule set the review ran with. Pinning it by hash is what stops a certificate from
/// being read as a stronger claim than it is: a later, larger rule set would find more.
/// </summary>
public sealed record RulePackIdentity(string Id, string Version, string Sha256, int RuleCount);

/// <summary>How many findings of one severity the review produced.</summary>
public sealed record SeverityTally(Severity Severity, int Count);

/// <summary>
/// Everything the certificate asserts. This record is the thing that gets signed.
/// It deliberately carries no source line, no secret value and no absolute path.
/// </summary>
/// <param name="SchemaVersion">Format version, so an old verifier can refuse a newer certificate honestly.</param>
/// <param name="CertificateId">Random identifier for referencing this certificate in correspondence.</param>
/// <param name="SubjectName">The name the reviewer gave the reviewed body of code.</param>
/// <param name="IssuedAtUtc">When the certificate was produced.</param>
/// <param name="IssuerName">The operator account that ran the review.</param>
/// <param name="IssuerKeyFingerprint">Stable fingerprint of the signing key, for trust on first use.</param>
/// <param name="Tool">Product and version that produced the certificate.</param>
/// <param name="RulePack">The pinned rule set.</param>
/// <param name="Scope">What was and was not reviewed.</param>
/// <param name="Tallies">Finding counts by severity.</param>
/// <param name="Files">Digest manifest of every reviewed file, ordered by path.</param>
public sealed record ReviewCertificatePayload(
    int SchemaVersion,
    string CertificateId,
    string SubjectName,
    DateTimeOffset IssuedAtUtc,
    string IssuerName,
    string IssuerKeyFingerprint,
    string Tool,
    RulePackIdentity RulePack,
    ScopeDeclaration Scope,
    IReadOnlyList<SeverityTally> Tallies,
    IReadOnlyList<FileDigest> Files)
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// The one sentence the certificate is allowed to claim. It is deliberately about the review,
    /// never about the code: "reviewed against this rule set" is defensible, "this code is secure" is not.
    /// </summary>
    public const string Assertion =
        "The files listed in this certificate were reviewed on the issuing machine against the rule pack named above, with the result recorded below. This is a statement about what was checked. It is not a warranty that the code is secure.";
}

/// <summary>
/// The seal over a payload: the hash that was signed, the signature, and the public key needed to check it.
/// </summary>
/// <param name="Algorithm">Signature algorithm identifier.</param>
/// <param name="PayloadSha256">Hash of the exact canonical payload bytes.</param>
/// <param name="Signature">Base64 signature over those bytes.</param>
/// <param name="PublicKey">Base64 SubjectPublicKeyInfo of the issuer key.</param>
/// <param name="KeyFingerprint">SHA-256 of the public key, shown to humans.</param>
public sealed record CertificateSeal(
    string Algorithm,
    string PayloadSha256,
    string Signature,
    string PublicKey,
    string KeyFingerprint)
{
    public const string EcdsaP256Sha256 = "ECDSA-P256-SHA256";
}
