<#
  Helpers for driving the installed Windows Calculator through UI Automation.
  Used to record golden data from the real app (see tools/Record-*.ps1).
#>
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$script:Window = $null

function Start-Calc {
    Get-Process CalculatorApp -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Process calc.exe
    $root = [Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::NameProperty, 'Calculator')
    $script:Window = $null
    for ($try = 0; $try -lt 20 -and -not $script:Window; $try++) {
        Start-Sleep -Milliseconds 800
        $script:Window = $root.FindFirst('Children', $cond)
    }
    if (-not $script:Window) { throw 'Calculator window not found' }
    Start-Sleep -Seconds 1
}

function Find-Element([string]$Id) {
    $cond = New-Object Windows.Automation.PropertyCondition([Windows.Automation.AutomationElement]::AutomationIdProperty, $Id)
    $script:Window.FindFirst('Descendants', $cond)
}

function Invoke-Element($Element) {
    $pattern = $null
    if ($Element.TryGetCurrentPattern([Windows.Automation.InvokePattern]::Pattern, [ref]$pattern)) {
        $pattern.Invoke()
    }
    elseif ($Element.TryGetCurrentPattern([Windows.Automation.TogglePattern]::Pattern, [ref]$pattern)) {
        $pattern.Toggle()
    }
    elseif ($Element.TryGetCurrentPattern([Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$pattern)) {
        $pattern.Expand()
    }
    elseif ($Element.TryGetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern, [ref]$pattern)) {
        $pattern.Select()
    }
    else {
        throw "No usable pattern on $($Element.Current.AutomationId)"
    }
}

function Select-Mode([string]$Id) {
    # Id is the navigation item id: Standard, Scientific, Graphing, Programmer, Date, Currency, ...
    $toggle = Find-Element 'TogglePaneButton'
    Invoke-Element $toggle
    Start-Sleep -Milliseconds 700
    $item = Find-Element $Id
    $item.GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern).Select()
    Start-Sleep -Milliseconds 900
}

function Get-Elements {
    $all = $script:Window.FindAll('Descendants', [Windows.Automation.Condition]::TrueCondition)
    foreach ($e in $all) {
        if ($e.Current.AutomationId) {
            [pscustomobject]@{
                Id      = $e.Current.AutomationId
                Name    = $e.Current.Name
                Type    = $e.Current.ControlType.ProgrammaticName -replace 'ControlType\.', ''
                Enabled = $e.Current.IsEnabled
            }
        }
    }
}

function Get-Text([string]$Id) {
    $e = Find-Element $Id
    if ($e) { $e.Current.Name }
}

Export-ModuleMember -Function Start-Calc, Find-Element, Invoke-Element, Select-Mode, Get-Elements, Get-Text
