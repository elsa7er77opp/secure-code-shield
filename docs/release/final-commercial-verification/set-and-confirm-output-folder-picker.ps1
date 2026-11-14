param(
    [Parameter(Mandatory = $true)]
    [string]$DialogTitle,
    [Parameter(Mandatory = $true)]
    [string]$OutputFolder
)

Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class OutputFolderPickerNative {
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, string lParam);
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
        $DialogTitle)))
if ($null -eq $picker) {
    throw "The native output picker '$DialogTitle' was not found."
}

$field = $picker.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        '1152')))
$selectFolder = $picker.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        '1')))
if ($null -eq $field -or $null -eq $selectFolder) {
    throw 'The native output picker does not expose its Folder or Select Folder controls.'
}

[OutputFolderPickerNative]::SendMessage([IntPtr]$field.Current.NativeWindowHandle, 0x000C, [IntPtr]::Zero, $OutputFolder) | Out-Null
Start-Sleep -Milliseconds 250
if ($field.Current.Name -ne $OutputFolder) {
    throw "The native output picker did not accept the intended folder. Actual: $($field.Current.Name)"
}

$bounds = $selectFolder.Current.BoundingRectangle
[OutputFolderPickerNative]::SetForegroundWindow($picker.Current.NativeWindowHandle) | Out-Null
[OutputFolderPickerNative]::SetCursorPos([int]($bounds.Left + ($bounds.Width / 2)), [int]($bounds.Top + ($bounds.Height / 2))) | Out-Null
[OutputFolderPickerNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
[OutputFolderPickerNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
Start-Sleep -Seconds 2
