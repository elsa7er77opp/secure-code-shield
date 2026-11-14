param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectFolder
)

Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class FolderPickerFieldMouseNative {
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

$field = $picker.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        '1152')))
if ($null -eq $field) {
    throw 'The native Folder input field was not found.'
}

$bounds = $field.Current.BoundingRectangle
[FolderPickerFieldMouseNative]::SetForegroundWindow($picker.Current.NativeWindowHandle) | Out-Null
[FolderPickerFieldMouseNative]::SetCursorPos([int]($bounds.Left + 12), [int]($bounds.Top + ($bounds.Height / 2))) | Out-Null
[FolderPickerFieldMouseNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[FolderPickerFieldMouseNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Milliseconds 200

$shell = New-Object -ComObject WScript.Shell
Set-Clipboard -Value $ProjectFolder
$shell.SendKeys('^a')
$shell.SendKeys('^v')
Start-Sleep -Seconds 1
