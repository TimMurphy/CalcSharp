<#
.SYNOPSIS
  Drives the installed Windows Calculator (Standard mode) through UI Automation and records the
  display and expression line after each key sequence. Output is a TSV used as golden data.

.DESCRIPTION
  Key alphabet: 0-9 digits, . decimal, + - * / operators, = equals, n negate, p percent,
  r reciprocal, s square root, q square, b backspace, e clear entry, c clear,
  m memory store, k recall, a memory add, u memory subtract, l memory clear.
  Writes: sequence <TAB> display <TAB> expression
#>
param(
    [string]$Output = "$PSScriptRoot\..\tests\CalcSharp.Core.Tests\GoldenData\standard.tsv",
    [int]$RandomCount = 250,
    [int]$Seed = 20261014,
    [string[]]$Probe = @()
)

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
$ids = @{
    '0' = 'num0Button'; '1' = 'num1Button'; '2' = 'num2Button'; '3' = 'num3Button'; '4' = 'num4Button'
    '5' = 'num5Button'; '6' = 'num6Button'; '7' = 'num7Button'; '8' = 'num8Button'; '9' = 'num9Button'
    '.' = 'decimalSeparatorButton'; '+' = 'plusButton'; '-' = 'minusButton'; '*' = 'multiplyButton'
    '/' = 'divideButton'; '=' = 'equalButton'; 'n' = 'negateButton'; 'p' = 'percentButton'
    'r' = 'invertButton'; 's' = 'squareRootButton'; 'q' = 'xpower2Button'; 'b' = 'backSpaceButton'
    'e' = 'clearEntryButton'; 'c' = 'clearButton'
    'm' = 'memButton'; 'k' = 'MemRecall'; 'a' = 'MemPlus'; 'u' = 'MemMinus'; 'l' = 'ClearMemoryButton'
}

Get-Process CalculatorApp -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Process calc.exe
Start-Sleep 4
$root = [Windows.Automation.AutomationElement]::RootElement
$nameProp = [Windows.Automation.AutomationElement]::NameProperty
$window = $root.FindFirst('Children', (New-Object Windows.Automation.PropertyCondition($nameProp, 'Calculator')))
if (-not $window) { throw 'Calculator window not found' }

$byId = [Windows.Automation.AutomationElement]::AutomationIdProperty
function Find($id) {
    $window.FindFirst('Descendants', (New-Object Windows.Automation.PropertyCondition($byId, $id)))
}
$buttons = @{}
foreach ($k in $ids.Keys) { $buttons[$k] = Find $ids[$k] }
$results = Find 'CalculatorResults'
$expression = Find 'CalculatorExpression'

function Press([string]$key) {
    $p = $buttons[$key].GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern)
    $p.Invoke()
}

function Record([string]$seq) {
    Press 'l'
    Press 'c'
    foreach ($ch in $seq.ToCharArray()) { Press ([string]$ch) }
    $d = $results.Current.Name -replace '^Display is ', ''
    $x = $expression.Current.Name -replace '^Expression is ?', ''
    "$seq`t$d`t$x"
}

$sequences = New-Object System.Collections.Generic.List[string]
if ($Probe.Count -gt 0) { $sequences.AddRange($Probe); $RandomCount = 0 } else {
# Hand-picked sequences covering documented behaviours.
$sequences.AddRange([string[]]@(
    '5', '12', '1234567', '0', '00', '1.5', '1..5', '.5', '5n', '5nn', 'n5', '5b', '123b', '12bb', '5bb',
    '5+3=', '5-3=', '5*3=', '5/3=', '5+3==', '5+3===', '5+=', '5*=', '5/=', '5-=', '2+3*4=', '2+3*', '5+-3=',
    '5+*3=', '5/0=', '0/0=', '5/0', '9s', '9ns', '7q', '4r', '0r', '200*5p', '200+10p=', '5p', '200+5p',
    '5+3e4=', '5+3c', '5+3e', '5+3=e', '5+3=7', '5+3=b', '5+3=n', '5+3=s', '5+3=q', '5+3=r', '5+s',
    '1/3=', '2/3=', '1/3*3=', '0.1+0.2=', '9999999999999999+1=', '99999999999999999', '1000000*1000000=',
    '0.0000001*0.0000001=', '5+3=+2=', '5+3=*2=', '2s', '2s*2s=', '3.14159265358979323846',
    '12345678901234567890', '1/7=', '10000000000000000*10=', '5q q', '5qq', '2qqqq', '3rr', '5nq', '5ns'
))
$rand = New-Object System.Random($Seed)
$alphabet = '0123456789012345678901234567890123456789.+-*/=npbqrsecmkaul'
for ($i = 0; $i -lt $RandomCount; $i++) {
    $len = $rand.Next(2, 9)
    $s = -join (1..$len | ForEach-Object { $alphabet[$rand.Next($alphabet.Length)] })
    $sequences.Add($s)
}

}
$lines = foreach ($s in ($sequences | Where-Object { $_ -notmatch ' ' })) { Record $s }
New-Item -ItemType Directory -Force (Split-Path $Output) | Out-Null
[IO.File]::WriteAllLines((Resolve-Path (Split-Path $Output)).Path + '\' + (Split-Path $Output -Leaf), $lines)
Press 'c'
"Recorded $($lines.Count) sequences to $Output"
