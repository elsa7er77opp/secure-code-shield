param(
    [Parameter(Mandatory = $true)]
    [string]$ButtonName
)

Add-Type -AssemblyName UIAutomationClient

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
if ($null -eq $button -or -not $button.Current.IsEnabled) {
    throw "Workstation button '$ButtonName' is unavailable for UI Automation invocation."
}

$invokePattern = $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
if ($null -eq $invokePattern) {
    throw "Workstation button '$ButtonName' does not expose InvokePattern."
}

$invokePattern.Invoke()
Start-Sleep -Milliseconds 500
