$ErrorActionPreference = 'Stop'

$shortcutPath = 'C:\Users\ahmed\Desktop\CyberBank Secure Code Shield.lnk'
$evidencePath = 'C:\Users\ahmed\Desktop\cyberbank\secure-code-shield\docs\release\final-commercial-verification\desktop-shortcut-inspection.log'

if (-not (Test-Path -LiteralPath $shortcutPath)) {
    throw 'The expected CyberBank Desktop shortcut does not exist.'
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
[pscustomobject]@{
    ShortcutPath = $shortcutPath
    TargetPath = $shortcut.TargetPath
    IconLocation = $shortcut.IconLocation
    WorkingDirectory = $shortcut.WorkingDirectory
} | Format-List | Out-String | Set-Content -LiteralPath $evidencePath
