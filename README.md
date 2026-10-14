# CalcSharp

## The Goal

Create a C# / WPF replica of the Windows Calculator entirely with Claude.

## Inspiration

Dave's Garage video https://youtu.be/KC31K-CUC5Y?si=ZCpKs0jT3eQy3FkL. I have deliberately not copied Dave's prompt.

## Features

All modes in the navigation menu are implemented. **None has yet been verified as exact** against
the real app; see "Not yet exact" below.

| Mode | Status |
| --- | --- |
| Standard | Complete keypad, memory, expression line, errors, keyboard shortcuts |
| Scientific | Precedence, parentheses, trig/hyp/inverse, DEG/RAD/GRAD, F-E, logs, powers, factorial |
| Graphing | `y=`, `x=` and implicit equations, colour-coded list, zoom and pan |
| Programmer | HEX/DEC/OCT/BIN, QWORD..BYTE, bitwise ops, shifts and rotates, bit-flip panel |
| Date calculation | Difference between dates, add or subtract years/months/days |
| Converters | Currency (offline rates), Volume, Length, Weight and mass, Temperature, Energy, Area, Speed, Time, Power, Data, Pressure, Angle |

### Not yet exact

- Layout and visuals are approximate (no Mica/Fluent styling; some combo boxes are not dark themed).
- Scientific uses `double` rounded to 16 digits, not the real app's arbitrary precision.
- Converter unit order, "fun" units and rounding, and Currency rates, are best recollection.
- Graphing has no tracing, analysis, settings or share.
- Some shortcuts and edge-case behaviour were written from memory rather than extracted.
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
| 2 | 2026-10-14 17:54 | 2026-10-14 18:06 | 12m | Navigation shell, parallel build of Scientific, Programmer, Graphing, Date calculation and converters; merged; 394 tests |
| | | | **12m** | **Total** (session 1 predates the clock rule and is not counted) |

## Ground rules

No one else's `calc.exe` source code is read. Code decompiled from `calc.exe` by this project itself may be read. See [GLOSSARY.md](GLOSSARY.md) for the web addresses consulted and why.
