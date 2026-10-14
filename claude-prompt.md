# Windows' Calculator in C#

Your job is to create an **exact working replica** of Windows' Calculator in C#.

## Steps

- [x] Turn this folder into a git repository.
- [x] Publish it to github.com/TimMurphy.
- [x] Create a Visual Studio solution containing:
  - a source project for the calculator engine,
  - a source project for the UI,
  - a test project covering the engine.
- [ ] Implement the calculator, starting with Standard mode.
- [ ] Keep the build green and the tests passing before every commit.

## Rules

- You may use the internet, but you **MUST NOT** read anyone else's source code related to `calc.exe`.
- You **MAY** decompile or disassemble `calc.exe` (and its resources) yourself and read the output. `calc.exe` is a native app, so disassembly, strings and resource inspection are expected and approved: do not stop to ask which approach to use.
- Use whatever you extract yourself (layout, strings, constants, behaviour) to make the replica exact, and keep going until the definition of done is met.
- **Do not stop, report back or ask me anything until there is an exact working copy of ALL modes** (Standard, Scientific, Graphing, Programmer, Date Calculation and every converter). Make reasonable decisions yourself and commit as you go. Only stop early if you are blocked by something only I can do (for example a permission or credential), and say precisely what.
- Keep a stopwatch on your work. The clock starts each time I say `implement @claude-prompt.md` and stops when you hand control back to me. Note the date and time (with timezone) of each start and stop, record the time taken per session in `README.md` under "Time spent", with a running total, and update it whenever you commit.
- You **MUST** record every web address you read, and the reason for reading it, in `GLOSSARY.md`.
- Do not copy any existing prompt or implementation of this task.

## Definition of done

- `dotnet build` and `dotnet test` succeed from the solution root.
- The app launches and behaves **exactly** like Windows' Calculator in **all** of its modes.
- `README.md` explains what the project is, how to run it and what is not yet implemented.
- `GLOSSARY.md` is complete and accurate.
- `README.md` has an up-to-date "Time spent" log.
