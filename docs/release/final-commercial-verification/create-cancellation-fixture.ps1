$fixtureRoot = 'C:\Users\ahmed\Desktop\cyberbank\secure-code-shield\docs\release\final-commercial-verification\release-fixture\cancellation'
New-Item -ItemType Directory -Force -Path $fixtureRoot | Out-Null

$line = 'public static class LocalCancellationFixture { public const string SafeText = "Local cancellation verification only"; }'
for ($fileIndex = 1; $fileIndex -le 24; $fileIndex++) {
    $path = Join-Path $fixtureRoot ("SafeFixture{0:D2}.cs" -f $fileIndex)
    $writer = [System.IO.StreamWriter]::new($path, $false)
    try {
        while ($writer.BaseStream.Length -lt 900000) {
            $writer.WriteLine($line)
        }
    }
    finally {
        $writer.Dispose()
    }
}
