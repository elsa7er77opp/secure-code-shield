param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectFolder
)

$shell = New-Object -ComObject WScript.Shell
if (-not $shell.AppActivate('Choose a project folder for review')) {
    throw 'The native project folder picker is not active.'
}

$shell.SendKeys($ProjectFolder)
Start-Sleep -Seconds 1
