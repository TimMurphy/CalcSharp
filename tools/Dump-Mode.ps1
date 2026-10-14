<#
.SYNOPSIS
  Lists the UI Automation ids, names, types and enabled state of the installed Calculator in a mode.
  Optionally presses ids first (for example to switch on the 2nd toggle) and lists new elements.
#>
param(
    [Parameter(Mandatory)][string]$Nav,
    [string[]]$Press = @(),
    [string]$Output = ''
)

Import-Module "$PSScriptRoot\CalcOracle.psm1" -Force
Start-Calc
Select-Mode $Nav
$before = @(Get-Elements | ForEach-Object Id)
foreach ($id in $Press) {
    Invoke-Element (Find-Element $id)
    Start-Sleep -Milliseconds 700
}

$rows = Get-Elements | Where-Object { $_.Type -in 'Button', 'Text', 'CheckBox', 'RadioButton', 'ComboBox', 'ListItem', 'Edit', 'Custom', 'Group', 'Pane' } |
    ForEach-Object { '{0} | {1} | {2} | enabled={3}' -f $_.Id, $_.Name, $_.Type, $_.Enabled }
if ($Output) { $rows | Set-Content $Output } else { $rows }
