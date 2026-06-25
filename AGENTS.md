# Agent Instructions

These instructions are for coding agents working in this repository (including OpenCode).

## Purpose

- `BassNoteFinder` is a WPF desktop trainer for bass note recognition.
- Core goal: stable note detection in the playable 4-string bass range (`E1` to `G3`, MIDI `28` to `55`) and clear Teacher/Student gameplay flows.

## Repo Map

- `BassNoteFinder/` - main app (`net10.0-windows`)
  - `Audio/` - capture and pitch detection (`AudioCaptureService`, `PitchDetector`)
  - `MusicTheory/` - note math/display (`Note`, `NoteDisplay`, `NoteGenerator`)
  - `Rendering/` - staff/fretboard rendering
  - `Views/` - `MenuView`, `TeacherModeView`, `StudentModeView`
  - `MainWindow.xaml.cs` - shell: mode routing + detection pipeline
- `BassNoteFinder.Tests/` - xUnit tests + audio fixtures in `Resources/`
- `PitchAnalyzer/` - CLI utility for inspecting pitch detection on recordings
- `docs/` - architecture and mode notes

## Local Dev Commands

Run from repo root:

```powershell
dotnet restore
dotnet build BassNoteFinder/BassNoteFinder.csproj
dotnet test BassNoteFinder.Tests/BassNoteFinder.Tests.csproj
dotnet run --project BassNoteFinder/BassNoteFinder.csproj
```

Useful targeted commands:

```powershell
dotnet test BassNoteFinder.Tests/BassNoteFinder.Tests.csproj --filter "FullyQualifiedName~PitchDetector"
dotnet run --project PitchAnalyzer/PitchAnalyzer.csproj -- "BassNoteFinder.Tests/Resources/E-string.m4a"
```

## Testing Expectations

- For pitch-related changes, run at least pitch detector tests (`PitchDetector*`) and one fixture check through `PitchAnalyzer`.
- Do not widen note generation/mapping outside MIDI `28..55` unless explicitly requested.
- If tests fail with file-lock errors, close any running `BassNoteFinder.exe` or `testhost.exe` and rerun.

## Working Rules For Agents

- Prefer focused changes; avoid broad refactors unless needed.
- Preserve existing UI/gameplay behavior unless task requires behavior changes.
- Keep docs and commands in sync with actual project paths.
- Do not commit generated `bin/` or `obj/` outputs.
