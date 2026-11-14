Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class FolderPickerConfirmMouseNative {
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extraInfo);
}
'@

$picker = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        'Choose a project folder for review')))
if ($null -eq $picker) {
    throw 'The native project folder picker was not found.'
}

$selectFolder = $picker.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        '1')))
if ($null -eq $selectFolder) {
    throw 'The native Select Folder control was not found.'
}

$bounds = $selectFolder.Current.BoundingRectangle
[FolderPickerConfirmMouseNative]::SetForegroundWindow($picker.Current.NativeWindowHandle) | Out-Null
[FolderPickerConfirmMouseNative]::SetCursorPos([int]($bounds.Left + ($bounds.Width / 2)), [int]($bounds.Top + ($bounds.Height / 2))) | Out-Null
[FolderPickerConfirmMouseNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[FolderPickerConfirmMouseNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Seconds 2
