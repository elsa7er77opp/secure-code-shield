param(
    [Parameter(Mandatory = $true)]
    [string]$UserName,
    [Parameter(Mandatory = $true)]
    [string]$Password
)

Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class InstalledSigninMouseNative {
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

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
$window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
$editCondition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Edit)
$edits = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCondition)
if ($edits.Count -ne 2) {
    throw "Expected exactly two sign-in edit controls; found $($edits.Count)."
}

for ($index = 0; $index -lt $edits.Count; $index++) {
    $valuePattern = $edits[$index].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $value = if ($index -eq 0) { $UserName } else { $Password }
    $valuePattern.SetValue($value)
}

[InstalledSigninMouseNative]::ShowWindow($process.MainWindowHandle, 9) | Out-Null
[InstalledSigninMouseNative]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
$rectangle = [InstalledSigninMouseNative+RECT]::new()
if (-not [InstalledSigninMouseNative]::GetWindowRect($process.MainWindowHandle, [ref]$rectangle)) {
    throw 'Unable to obtain Secure Code Shield window bounds.'
}

[InstalledSigninMouseNative]::SetCursorPos($rectangle.Left + 400, $rectangle.Top + 755) | Out-Null
[InstalledSigninMouseNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[InstalledSigninMouseNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Seconds 3
