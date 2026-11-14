Add-Type -AssemblyName UIAutomationClient

$process = Get-Process SecureCodeShield.Desktop -ErrorAction Stop | Select-Object -First 1
$window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
if ($null -eq $window) {
    throw 'Secure Code Shield does not have a native window to close.'
}

$windowPattern = $window.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern)
if ($null -eq $windowPattern) {
    throw 'Secure Code Shield does not expose a normal WindowPattern close operation.'
}

$windowPattern.Close()
Start-Sleep -Seconds 2
