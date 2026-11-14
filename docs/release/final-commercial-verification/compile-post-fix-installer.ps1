$projectRoot = 'C:\Users\ahmed\Desktop\cyberbank\secure-code-shield'
$compiler = Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'
$script = Join-Path $projectRoot 'installer\SecureCodeShield.iss'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "Inno Setup compiler was not found at $compiler."
}

& $compiler $script
exit $LASTEXITCODE
