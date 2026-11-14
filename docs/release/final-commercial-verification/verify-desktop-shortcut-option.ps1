$ErrorActionPreference = 'Stop'

$projectRoot = 'C:\Users\ahmed\Desktop\cyberbank\secure-code-shield'
$installer = Join-Path $projectRoot 'DELIVERY\INSTALLER\CyberBank-Secure-Code-Shield-Final-Setup-win-x64.exe'
$target = 'C:\Users\ahmed\AppData\Local\Programs\CyberBank\Secure Code Shield\SecureCodeShield.Desktop.exe'
$desktop = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktop 'CyberBank Secure Code Shield.lnk'
$evidencePath = Join-Path $projectRoot 'docs\release\final-commercial-verification\desktop-shortcut-option.log'

Get-Process SecureCodeShield.Desktop -ErrorAction SilentlyContinue | Stop-Process -Force
if (Test-Path -LiteralPath $shortcutPath) {
    Remove-Item -LiteralPath $shortcutPath -Force
}

$installerProcess = Start-Process -FilePath $installer -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS="desktopicon"' -PassThru
$installerProcess.WaitForExit()

$deadline = [DateTime]::UtcNow.AddSeconds(20)
while (-not (Test-Path -LiteralPath $shortcutPath) -and [DateTime]::UtcNow -lt $deadline) {
    Start-Sleep -Milliseconds 250
}

if (-not (Test-Path -LiteralPath $shortcutPath)) {
    throw 'The selected desktopicon installer task did not create a Desktop shortcut.'
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
if ($shortcut.TargetPath -ne $target) {
    throw "Desktop shortcut target mismatch. Actual: $($shortcut.TargetPath)"
}

[pscustomobject]@{
    ShortcutPath = $shortcutPath
    TargetPath = $shortcut.TargetPath
    IconLocation = $shortcut.IconLocation
    InstallerTask = 'desktopicon'
    Result = 'PASS'
} | Format-List | Out-String | Set-Content -LiteralPath $evidencePath
