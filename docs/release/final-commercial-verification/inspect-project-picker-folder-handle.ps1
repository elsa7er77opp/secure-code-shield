param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Add-Type -AssemblyName UIAutomationClient

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

[pscustomobject]@{
    PickerHandle = $picker.Current.NativeWindowHandle
    FieldHandle = $field.Current.NativeWindowHandle
    FieldName = $field.Current.Name
    FieldControlType = $field.Current.ControlType.ProgrammaticName
    FieldAutomationId = $field.Current.AutomationId
    FieldBounds = $field.Current.BoundingRectangle.ToString()
    SupportsValuePattern = $field.TryGetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern, [ref]$null)
} | Format-List | Out-String | Set-Content -LiteralPath $OutputPath
