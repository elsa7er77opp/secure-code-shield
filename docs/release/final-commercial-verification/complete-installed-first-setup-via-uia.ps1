param(
    [Parameter(Mandatory = $true)]
    [string]$UserName,
    [Parameter(Mandatory = $true)]
    [string]$Password
)

Add-Type -AssemblyName UIAutomationClient

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
$window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
if ($null -eq $window) {
    throw 'Unable to acquire Secure Code Shield UI Automation window.'
}

$editCondition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::Edit)
$edits = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $editCondition)
if ($edits.Count -ne 3) {
    throw "Expected exactly three local-login edit controls; found $($edits.Count)."
}

for ($index = 0; $index -lt $edits.Count; $index++) {
    $valuePattern = $edits[$index].GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    if ($null -eq $valuePattern) {
        throw "Login edit control $index does not expose ValuePattern."
    }

    $value = if ($index -eq 0) { $UserName } else { $Password }
    $valuePattern.SetValue($value)
}

$buttonCondition = New-Object System.Windows.Automation.AndCondition @(
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)),
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        'Continue securely')))
$button = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
if ($null -eq $button) {
    throw 'Unable to find the local-login continuation button.'
}

$invokePattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
if ($null -eq $invokePattern) {
    throw 'The local-login continuation button does not expose InvokePattern.'
}

$invokePattern.Invoke()
Start-Sleep -Seconds 3
