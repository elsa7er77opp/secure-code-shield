Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class PickerErrorDismissMouseNative {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);
}
'@

$errorDialog = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        'Open File')))
if ($null -eq $errorDialog) {
    throw 'The native folder-picker error dialog was not found.'
}

$ok = $errorDialog.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        'OK')))
if ($null -eq $ok) {
    throw 'The native folder-picker error OK control was not found.'
}

$bounds = $ok.Current.BoundingRectangle
[PickerErrorDismissMouseNative]::SetForegroundWindow($errorDialog.Current.NativeWindowHandle) | Out-Null
[PickerErrorDismissMouseNative]::SetCursorPos([int]($bounds.Left + ($bounds.Width / 2)), [int]($bounds.Top + ($bounds.Height / 2))) | Out-Null
[PickerErrorDismissMouseNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[PickerErrorDismissMouseNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 500
