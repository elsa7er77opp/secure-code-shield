param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Add-Type -AssemblyName UIAutomationClient

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
