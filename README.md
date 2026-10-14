# CalcSharp

## The Goal

Create a C# / WPF replica of the Windows Calculator entirely with Claude.

## Inspiration

Dave's Garage video https://youtu.be/KC31K-CUC5Y?si=ZCpKs0jT3eQy3FkL. I have deliberately not copied Dave's prompt.

## Features

Standard mode only, for now:

- `+ − × ÷`, `=` (repeat equals), `%`, `√`, `x²`, `1/x`, `±`
- `CE`, `C`, backspace
- Memory: `MC`, `MR`, `M+`, `M−`, `MS`
- Expression line, 16-digit limit, "Cannot divide by zero" / "Result is undefined" errors
- Keyboard input

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

## Ground rules

No one else's `calc.exe` source code is read. Code decompiled from `calc.exe` by this project itself may be read. See [GLOSSARY.md](GLOSSARY.md) for the web addresses consulted and why.
