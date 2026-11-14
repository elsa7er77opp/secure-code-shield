param(
    [Parameter(Mandatory = $true)]
    [string]$UserName,
    [Parameter(Mandatory = $true)]
    [string]$Password
)

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class InstalledLoginNative {
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr handle, int command);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);
}
'@

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
if ($process.MainWindowHandle -eq 0) {
    throw 'Secure Code Shield does not have a native main window.'
}

[InstalledLoginNative]::ShowWindow($process.MainWindowHandle, 9) | Out-Null
[InstalledLoginNative]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 500

$shell = New-Object -ComObject WScript.Shell
if (-not $shell.AppActivate($process.Id)) {
    throw 'Unable to focus Secure Code Shield for local-login smoke.'
}

$shell.SendKeys($UserName)
$shell.SendKeys('{TAB}')
$shell.SendKeys($Password)
$shell.SendKeys('{TAB}')
$shell.SendKeys($Password)
$shell.SendKeys('{TAB}')
$shell.SendKeys('{ENTER}')
Start-Sleep -Seconds 2
