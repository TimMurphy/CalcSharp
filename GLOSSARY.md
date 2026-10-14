# Glossary

## Web addresses consulted

| Address | Reason for reading |
| --- | --- |
| _None_ | No web pages were read while building this project. |

## Local files inspected

Allowed by the brief: these are read from the locally installed Windows Calculator package
(self-obtained, not anyone else's source code).

| File | Reason for reading |
| --- | --- |
| `C:\Program Files\WindowsApps\Microsoft.WindowsCalculator_11.2607.0.0_x64__8wekyb3d8bbwe\resources.pri` | Extracted UI strings (error messages, tooltips with keyboard shortcuts, unit names, graphing and date-mode strings) and control names, to match the real app's wording, shortcuts and button sets. Copied to a scratch folder and dumped with a small string extractor. |
| `...\AppxManifest.xml` | Copied alongside `resources.pri`; not otherwise used. |

The running `calc.exe` is also driven through UI Automation (`tools/CalcOracle.psm1`) and its
on-screen display and expression text are recorded as golden test data. No source code is read.

Nothing from `CalculatorApp.dll`, `CalcViewModel.dll`, `GraphControl.dll` or `GraphingImpl.dll`
has been disassembled yet.

## Constraint

The project brief forbids reading anyone else's source code related to `calc.exe`, but allows
reading code this project decompiled from `calc.exe` itself. Behaviour not covered by the
extracted strings was implemented from general knowledge of how these calculators work, and is
therefore **approximate**. If a web address is consulted in future it **must** be added to the
table above together with the reason it was read.

## Terms

| Term | Meaning |
| --- | --- |
| CE | Clear Entry: clears the number currently being typed. |
| C | Clear: resets the whole calculation. |
| MC / MR / MS / M+ / M− | Memory clear / recall / store / add / subtract. |
| Operand | A number an operator acts on. |
| QWORD / DWORD / WORD / BYTE | 64 / 32 / 16 / 8-bit word sizes in Programmer mode. |
| RoL / RoR / RoLC / RoRC | Rotate left / right, optionally through the carry flag. |
