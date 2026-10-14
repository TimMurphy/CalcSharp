<#
.SYNOPSIS
  Records golden data from the installed Windows Calculator for any mode.

.DESCRIPTION
  A map file (tools/oracle/<mode>.json) describes how one character maps to the UI Automation
  element ids to invoke, the nav id of the mode and the elements to read after each sequence.
  Output is TSV: sequence <TAB> read1 <TAB> read2 ...

  Map file:
    {
      "nav": "Scientific",
      "reset": ["clearButton"],
      "keys": { "7": ["num7Button"], "i": ["trigButton", "sinButton"] },
      "reads": ["CalculatorResults", "CalculatorExpression"],
      "strip": ["^Display is ", "^Expression is ?"],
      "alphabet": "0123456789+-*/=",
      "probes": ["1+2="]
    }
#>
param(
    [Parameter(Mandatory)][string]$Map,
    [Parameter(Mandatory)][string]$Output,
    [int]$RandomCount = 200,
    [int]$MinLength = 2,
    [int]$MaxLength = 8,
    [int]$Seed = 1,
    [string[]]$Probe = @()
)

Import-Module "$PSScriptRoot\CalcOracle.psm1" -Force
$spec = Get-Content $Map -Raw | ConvertFrom-Json

# Case-sensitive key table.
$keys = New-Object 'System.Collections.Generic.Dictionary[string,string[]]'
foreach ($p in $spec.keys.PSObject.Properties) { $keys[$p.Name] = [string[]]@($p.Value) }

Start-Calc
Select-Mode $spec.nav

function Get-Ui([string]$ElementId) {
    for ($try = 0; $try -lt 8; $try++) {
        $found = Find-Element $ElementId
        if ($null -ne $found) { return $found }
        Start-Sleep -Milliseconds 250
    }
    throw "element not found: $ElementId"
}

function Press([string[]]$ids) {
    foreach ($id in $ids) {
        # Elements inside flyouts only exist while open, so look them up each time.
        Invoke-Element (Get-Ui $id)
    }
}

function Record-One([string]$seq) {
    Press $spec.reset
    foreach ($ch in $seq.ToCharArray()) {
        $k = [string]$ch
        if (-not $keys.ContainsKey($k)) { throw "no key '$k' in map" }
        Press $keys[$k]
    }
    $cols = for ($i = 0; $i -lt $spec.reads.Count; $i++) {
        $e = Find-Element $spec.reads[$i]
        $t = if ($e) { $e.Current.Name } else { '<missing>' }
        $t -replace $spec.strip[$i], ''
    }
    # Undo persistent toggles so sequences stay independent.
    foreach ($id in @($spec.untoggle)) {
        if (-not $id) { continue }
        $t = Find-Element $id
        $pat = $null
        if ($t -and $t.TryGetCurrentPattern([Windows.Automation.TogglePattern]::Pattern, [ref]$pat)) {
            if ($pat.Current.ToggleState -eq 'On') { $pat.Toggle() }
        }
    }
    (@($seq) + @($cols)) -join "`t"
}

function Record([string]$seq) {
    try { Record-One $seq }
    catch {
        # The app is in an unexpected state (for example a flyout is open): restart it.
        Write-Warning "sequence '$seq' failed: $($_.Exception.Message)"
        Start-Calc
        Select-Mode $spec.nav
        $null
    }
}
$sequences = New-Object System.Collections.Generic.List[string]
if ($Probe.Count -gt 0) {
    $sequences.AddRange($Probe)
}
else {
    if ($spec.probes) { $sequences.AddRange([string[]]$spec.probes) }
    $rand = New-Object System.Random($Seed)
    $alphabet = $spec.alphabet
    for ($i = 0; $i -lt $RandomCount; $i++) {
        $len = $rand.Next($MinLength, $MaxLength + 1)
        $sequences.Add((-join (1..$len | ForEach-Object { $alphabet[$rand.Next($alphabet.Length)] })))
    }
}

$lines = @(foreach ($s in $sequences) { Record $s }) | Where-Object { $_ -and $_.Trim() }
$dir = Split-Path $Output
if ($dir) { New-Item -ItemType Directory -Force $dir | Out-Null }
[IO.File]::WriteAllLines([IO.Path]::GetFullPath($Output), $lines)
try { Press $spec.reset } catch { }
"Recorded $($lines.Count) sequences to $Output"
