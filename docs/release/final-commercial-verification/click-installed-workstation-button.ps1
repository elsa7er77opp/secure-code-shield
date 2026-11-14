param(
    [Parameter(Mandatory = $true)]
    [string]$ButtonName
)

Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class WorkstationButtonMouseNative {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);
}
'@

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
$window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
$button = $window.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.AndCondition @(
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Button)),
        (New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            $ButtonName)))))
if ($null -eq $button -or -not $button.Current.IsEnabled -or $button.Current.IsOffscreen) {
    throw "Workstation button '$ButtonName' is unavailable for native mouse selection."
}

$bounds = $button.Current.BoundingRectangle
[WorkstationButtonMouseNative]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
[WorkstationButtonMouseNative]::SetCursorPos([int]($bounds.Left + ($bounds.Width / 2)), [int]($bounds.Top + ($bounds.Height / 2))) | Out-Null
[WorkstationButtonMouseNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[WorkstationButtonMouseNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 500
