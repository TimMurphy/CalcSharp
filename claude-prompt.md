# Windows' Calculator in C#

Your job is to create a copy of Windows' Calculator in C#.

## Goal

An exact replica of Windows' `calc.exe`: same look, layout, behaviour and keyboard shortcuts.

## Steps

1. Turn this folder into a git repository.
2. Publish it to github.com/TimMurphy.
3. Create a Visual Studio solution containing:
   - a source project for the calculator engine,
   - a source project for the UI,
   - a test project covering the engine.
4. Implement the calculator, starting with Standard mode.
5. Keep the build green and the tests passing before every commit.

## Rules

- You may use the internet, but you **MUST NOT** read any source code directly related to `calc.exe`.
- You **MUST** record every web address you read, and the reason for reading it, in `GLOSSARY.md`.
- Do not copy any existing prompt or implementation of this task.

## Definition of done

- `dotnet build` and `dotnet test` succeed from the solution root.
- The app launches and behaves like Windows' Calculator for the implemented modes.
- `README.md` explains what the project is, how to run it and what is not yet implemented.
- `GLOSSARY.md` is complete and accurate.
