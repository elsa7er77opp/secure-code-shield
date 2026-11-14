param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Add-Type -AssemblyName UIAutomationClient

$pickerCondition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty,
    'Choose a project folder for review')
$picker = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    $pickerCondition)
if ($null -eq $picker) {
    throw 'The native project folder picker was not found in the UI Automation tree.'
}

$picker.FindAll(
    [System.Windows.Automation.TreeScope]::Descendants,
    [System.Windows.Automation.Condition]::TrueCondition) |
    ForEach-Object {
        [pscustomobject]@{
            ControlType = $_.Current.ControlType.ProgrammaticName
            Name = $_.Current.Name
            AutomationId = $_.Current.AutomationId
            IsEnabled = $_.Current.IsEnabled
            IsOffscreen = $_.Current.IsOffscreen
        }
    } |
    Format-Table -AutoSize |
    Out-String -Width 240 |
    Set-Content -LiteralPath $OutputPath
