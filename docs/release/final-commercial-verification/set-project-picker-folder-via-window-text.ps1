param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectFolder,
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Add-Type -AssemblyName UIAutomationClient
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class FolderPickerTextNative {
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, string lParam);
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

[FolderPickerTextNative]::SendMessage(
    [IntPtr]$field.Current.NativeWindowHandle,
    0x000C,
    [IntPtr]::Zero,
    $ProjectFolder) | Out-Null
Start-Sleep -Milliseconds 300

[pscustomobject]@{
    FieldNameAfterSet = $field.Current.Name
    ExpectedPath = $ProjectFolder
} | Format-List | Out-String | Set-Content -LiteralPath $OutputPath
