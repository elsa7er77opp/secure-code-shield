param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$startTime = (Get-Date).AddMinutes(-30)
$events = Get-WinEvent -FilterHashtable @{ LogName = 'Application'; StartTime = $startTime } -ErrorAction SilentlyContinue |
    Where-Object {
        $_.ProviderName -match 'Application Error|Windows Error Reporting|\.NET Runtime' -or
        $_.Message -match 'SecureCodeShield'
    } |
    Select-Object -First 12 TimeCreated, ProviderName, Id, LevelDisplayName, Message

'# Application events' | Set-Content -LiteralPath $OutputPath
$events | Format-List | Out-String -Width 240 | Add-Content -LiteralPath $OutputPath
'# Local artifact names only' | Add-Content -LiteralPath $OutputPath
Get-ChildItem -LiteralPath "$env:LOCALAPPDATA\CyberBank\SecureCodeShield" -Recurse -File -ErrorAction SilentlyContinue |
    Select-Object FullName, Length, LastWriteTime |
    Format-Table -AutoSize |
    Out-String -Width 240 |
    Add-Content -LiteralPath $OutputPath
