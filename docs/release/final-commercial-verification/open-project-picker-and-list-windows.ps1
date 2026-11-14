param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Add-Type -AssemblyName UIAutomationClient

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
$window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
$buttonCondition = New-Object System.Windows.Automation.AndCondition @(
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)),
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        'Choose project')))
$button = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $buttonCondition)
if ($null -eq $button) {
    throw 'Choose project button was not found in the installed Workstation.'
}

$button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Start-Sleep -Seconds 2

[System.Windows.Automation.AutomationElement]::RootElement.FindAll(
    [System.Windows.Automation.TreeScope]::Children,
    [System.Windows.Automation.Condition]::TrueCondition) |
    Where-Object { $_.Current.ControlType -eq [System.Windows.Automation.ControlType]::Window } |
    ForEach-Object {
        [pscustomobject]@{
            Name = $_.Current.Name
            ProcessId = $_.Current.ProcessId
            IsEnabled = $_.Current.IsEnabled
            IsOffscreen = $_.Current.IsOffscreen
        }
    } |
    Format-Table -AutoSize |
    Out-String -Width 200 |
    Set-Content -LiteralPath $OutputPath
