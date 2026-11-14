using SecureCodeShield.Domain.Attestation;

namespace SecureCodeShield.Verifier;

/// <summary>
/// The free, standalone checker a recipient runs against code they were given.
/// <para>
/// It deliberately needs no licence, no account and no network. Its whole value is that the person
/// checking does not have to trust the person who sent the certificate — or the tool that made it.
/// It answers two questions and keeps them apart: has the certificate been altered, and are these the
/// files it describes.
/// </para>
/// </summary>
internal static class Program
{
    private const int ExitVerified = 0;
    private const int ExitMismatch = 1;
    private const int ExitUsage = 2;

    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length is 0 or > 2 || args[0] is "-h" or "--help")
        {
            WriteUsage();
            return ExitUsage;
        }

        var certificatePath = Path.GetFullPath(args[0]);
        var folder = Path.GetFullPath(args.Length == 2 ? args[1] : Path.GetDirectoryName(certificatePath) ?? ".");

        if (!File.Exists(certificatePath))
        {
            Console.Error.WriteLine($"Certificate not found: {certificatePath}");
            return ExitUsage;
        }

        if (!Directory.Exists(folder))
        {
            Console.Error.WriteLine($"Folder not found: {folder}");
            return ExitUsage;
        }

        if (!ReviewCertificateDocument.TryParse(File.ReadAllText(certificatePath), out var payloadBytes, out var seal, out var parseError))
        {
            Fail(parseError);
            return ExitMismatch;
        }

        var sealResult = CertificateVerifier.VerifySeal(payloadBytes, seal!);
        if (!sealResult.IsValid)
        {
            Fail(sealResult.Explanation);
            return ExitMismatch;
        }

        if (!CanonicalPayloadReader.TryRead(payloadBytes, out var payload, out var readError))
        {
            Fail(readError);
            return ExitMismatch;
        }

        WriteCertificateSummary(payload!);

        var manifest = CertificateVerifier.VerifyManifest(
            payload!.Files,
            folder,
            relativePath => File.ReadAllBytes(Path.Combine(folder, relativePath)),
            EnumerateRelativePaths(folder));

        WriteManifestResult(manifest, payload.Files.Count);

        if (!manifest.IsExactMatch)
        {
            return ExitMismatch;
        }

        Console.WriteLine();
        Console.WriteLine("VERIFIED — every file in this folder is exactly what the certificate says was reviewed.");
        Console.WriteLine();
        Console.WriteLine("What this does and does not mean:");
        Console.WriteLine("  It means these bytes are the bytes that were reviewed, and the certificate was not altered.");
        Console.WriteLine("  It does not mean the code is secure, and it does not prove who the issuer is —");
        Console.WriteLine("  the signing key is self-generated. Compare the key fingerprint above against one");
        Console.WriteLine("  you have seen from the same issuer before.");

        return ExitVerified;
    }

    private static void WriteCertificateSummary(ReviewCertificatePayload payload)
    {
        Console.WriteLine();
        Console.WriteLine("REVIEW CERTIFICATE");
        Console.WriteLine(new string('-', 62));
        Console.WriteLine($"  Subject          {payload.SubjectName}");
        Console.WriteLine($"  Issued           {payload.IssuedAtUtc:yyyy-MM-dd HH:mm} UTC");
        Console.WriteLine($"  Issuer           {payload.IssuerName}");
        Console.WriteLine($"  Key fingerprint  {payload.IssuerKeyFingerprint}");
        Console.WriteLine($"  Tool             {payload.Tool}");
        Console.WriteLine($"  Rule pack        {payload.RulePack.Id} {payload.RulePack.Version} " +
                          $"({payload.RulePack.RuleCount} rules, {payload.RulePack.Sha256[..12]}…)");
        Console.WriteLine();
        Console.WriteLine("  Result");

        foreach (var tally in payload.Tallies)
        {
            Console.WriteLine($"    {tally.Severity,-9} {tally.Count}");
        }

        Console.WriteLine();
        Console.WriteLine("  Coverage");
        Console.WriteLine($"    Reviewed  {payload.Scope.ReviewedFileCount} files");
        Console.WriteLine($"    Excluded  {payload.Scope.ExcludedFileCount} files");

        foreach (var exclusion in payload.Scope.Exclusions)
        {
            Console.WriteLine($"      {exclusion.Reason,-22} {exclusion.FileCount}");
        }

        Console.WriteLine();
        Console.WriteLine("  " + payload.Scope.Note);
    }

    private static void WriteManifestResult(ManifestVerification manifest, int expectedFileCount)
    {
        Console.WriteLine();
        Console.WriteLine("FILE CHECK");
        Console.WriteLine(new string('-', 62));
        Console.WriteLine($"  Matched      {manifest.MatchedFileCount} of {expectedFileCount}");

        WriteList("Changed since the review", manifest.ModifiedFiles);
        WriteList("Listed but not found here", manifest.MissingFiles);
        WriteList("Present but never reviewed", manifest.UnreviewedFiles);
    }

    private static void WriteList(string heading, IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"  {heading} ({paths.Count})");

        // A wall of paths hides the point; the count above already carries the scale.
        foreach (var path in paths.Take(20))
        {
            Console.WriteLine($"    {path}");
        }

        if (paths.Count > 20)
        {
            Console.WriteLine($"    … and {paths.Count - 20} more");
        }
    }

    private static string[] EnumerateRelativePaths(string folder) =>
        Directory.EnumerateFiles(folder, "*", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            })
            .Select(fullPath => CanonicalPayloadWriter.NormalizePath(Path.GetRelativePath(folder, fullPath)))
            .Where(relativePath => !relativePath.EndsWith(ReviewCertificateDocument.FileExtension, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    private static void Fail(string explanation)
    {
        Console.WriteLine();
        Console.WriteLine("NOT VERIFIED");
        Console.WriteLine(new string('-', 62));
        Console.WriteLine($"  {explanation}");
    }

    private static void WriteUsage()
    {
        Console.WriteLine("scs-verify — checks a Secure Code Shield review certificate.");
        Console.WriteLine();
        Console.WriteLine("  scs-verify <certificate" + ReviewCertificateDocument.FileExtension + "> [folder]");
        Console.WriteLine();
        Console.WriteLine("  certificate  The certificate you were given.");
        Console.WriteLine("  folder       The code to check it against. Defaults to the certificate's folder.");
        Console.WriteLine();
        Console.WriteLine("Exit codes: 0 verified, 1 not verified, 2 usage error.");
        Console.WriteLine("No account, licence or network connection is required.");
    }
}
