param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectFolder
)

$shell = New-Object -ComObject WScript.Shell
if (-not $shell.AppActivate('Choose a project folder for review')) {
    throw 'The native project folder picker is not active.'
}

Set-Clipboard -Value $ProjectFolder
$shell.SendKeys('^v')
Start-Sleep -Milliseconds 300
$shell.SendKeys('{TAB}')
$shell.SendKeys('{ENTER}')
Start-Sleep -Seconds 2
