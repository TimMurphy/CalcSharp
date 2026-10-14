# CalcSharp

A C# / WPF replica of the Windows Calculator (Standard mode).

- `src/CalcSharp.Core` – calculator engine (no UI dependencies)
- `src/CalcSharp.App` – WPF front end
- `tests/CalcSharp.Core.Tests` – xUnit tests for the engine

```
dotnet test
dotnet run --project src/CalcSharp.App
```

See [GLOSSARY.md](GLOSSARY.md) for the web addresses consulted and why.
