param(
    [Parameter(Mandatory = $true)]
    [string]$UserName,
    [Parameter(Mandatory = $true)]
    [string]$Password
)

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class InstalledSetupNative {
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr handle, int command);
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr handle, out RECT rectangle);
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);
}
'@

function Set-InstalledTextField([int]$x, [int]$y, [string]$value, $shell) {
    [InstalledSetupNative]::SetCursorPos($x, $y) | Out-Null
    [InstalledSetupNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [InstalledSetupNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 150
    $shell.SendKeys('^a')
    Set-Clipboard -Value $value
    $shell.SendKeys('^v')
    Start-Sleep -Milliseconds 150
}

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
if ($process.MainWindowHandle -eq 0) {
    throw 'Secure Code Shield does not have a native main window.'
}

[InstalledSetupNative]::ShowWindow($process.MainWindowHandle, 9) | Out-Null
[InstalledSetupNative]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 500

$rectangle = [InstalledSetupNative+RECT]::new()
if (-not [InstalledSetupNative]::GetWindowRect($process.MainWindowHandle, [ref]$rectangle)) {
    throw 'Unable to obtain Secure Code Shield window bounds.'
}

$shell = New-Object -ComObject WScript.Shell
Set-InstalledTextField ($rectangle.Left + 400) ($rectangle.Top + 478) $UserName $shell
Set-InstalledTextField ($rectangle.Left + 400) ($rectangle.Top + 581) $Password $shell
Set-InstalledTextField ($rectangle.Left + 400) ($rectangle.Top + 683) $Password $shell
$shell.SendKeys('{TAB}')
$shell.SendKeys('{ENTER}')
Start-Sleep -Seconds 3
