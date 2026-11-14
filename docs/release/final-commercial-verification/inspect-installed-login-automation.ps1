Add-Type -AssemblyName UIAutomationClient

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
$window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
if ($null -eq $window) {
    throw 'Unable to acquire Secure Code Shield UI Automation window.'
}

$window.FindAll(
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
    Format-Table -AutoSize
