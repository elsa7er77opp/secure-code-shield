Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class FolderPickerPositionNative {
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
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

[FolderPickerPositionNative]::SetWindowPos(
    $picker.Current.NativeWindowHandle,
    [IntPtr]::Zero,
    10,
    10,
    0,
    0,
    0x0001 -bor 0x0004 -bor 0x0040) | Out-Null
