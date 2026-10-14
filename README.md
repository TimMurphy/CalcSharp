# CalcSharp

## The Goal

Create a C# / WPF replica of the Windows Calculator entirely with Claude.

## Inspiration

Dave's Garage video https://youtu.be/KC31K-CUC5Y?si=ZCpKs0jT3eQy3FkL. I have deliberately not copied Dave's prompt.

## Features

All modes in the navigation menu are implemented. Behaviour is verified against the real Calculator
by replaying recorded key sequences (see "How exactness is checked"); modes marked *unverified* have
not been compared yet.

| Mode | Status |
| --- | --- |
| Standard | Verified: display and expression line match 1,300+ recorded sequences, including memory keys |
| Scientific | Verified on 158 recorded sequences; 32-digit arithmetic. Not yet verified: toggles (2nd, hyp, DEG/RAD/GRAD, F-E), inverse/hyperbolic names, number-format thresholds |
| Graphing | *Unverified*: `y=`, `x=` and implicit equations, colour-coded list, zoom and pan |
| Programmer | *Unverified*: HEX/DEC/OCT/BIN, QWORD..BYTE, bitwise ops, shifts and rotates, bit-flip panel |
| Date calculation | *Unverified*: difference between dates, add or subtract years/months/days |
| Converters | *Unverified*: Currency (offline rates), Volume, Length, Weight and mass, Temperature, Energy, Area, Speed, Time, Power, Data, Pressure, Angle |

### How exactness is checked

`tools/CalcOracle.psm1` and `tools/Record-Oracle.ps1` drive the installed Windows Calculator through
UI Automation, press a key sequence from a map in `tools/oracle/`, and record the display and
expression line to `tests/CalcSharp.Core.Tests/GoldenData/*.tsv`. The golden tests replay each
sequence against the C# engine. This needs an interactive (unlocked, connected) desktop session.

### Not yet exact

- Layout and visuals are approximate (no Mica/Fluent styling; some combo boxes are not dark themed).
- Programmer, Date calculation, Graphing and the converters have not been compared with the real app.
- Converter unit order, "fun" units and rounding, and Currency rates, are best recollection.
- Graphing has no tracing, analysis, settings or share.
- Single memory slot; no history or memory list pane.

## Requirements

- Windows (WPF)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Layout

- `src/CalcSharp.Core` – calculator engine (no UI dependencies)
- `src/CalcSharp.App` – WPF front end
- `tests/CalcSharp.Core.Tests` – xUnit tests for the engine

## Run

```
dotnet test
dotnet run --project src/CalcSharp.App
```

## Time spent

Wall-clock time (UTC+11). Each session starts when the prompt is given with `implement @claude-prompt.md` and stops when control is handed back.

| Session | Start | Stop | Elapsed | Notes |
| --- | --- | --- | --- | --- |
| 2 | 2026-10-14 17:54 | 2026-10-14 18:53 | 59m | Navigation shell; parallel build of all modes; UI Automation oracle; Standard matched to real app (1,300+ sequences); Scientific rewritten on 32-digit BigNum and matched (158 sequences) |
| | | | **59m** | **Total** (session 1 predates the clock rule and is not counted) |

## Ground rules

No one else's `calc.exe` source code is read. Code decompiled from `calc.exe` by this project itself may be read. See [GLOSSARY.md](GLOSSARY.md) for the web addresses consulted and why.
