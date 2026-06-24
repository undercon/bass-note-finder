# Bass Note Finder — Modes Overview

## Architecture

The application uses a shell-based architecture where `MainWindow` hosts a `ContentControl` that swaps between views. Each game mode is a self-contained `UserControl` implementing `IGameMode`.

- **Shell (MainWindow)**: owns audio capture, config persistence, and the always-present footer (input selector, gate, harmonic correction, mic toggle, detected-note readout).
- **Menu View**: mode selection screen shown on startup.
- **Mode Views**: each mode implements `IGameMode` and receives resolved note events from the shell.

## IGameMode Interface

```csharp
public interface IGameMode
{
    void OnActivate();
    void OnDeactivate();
    void OnNoteDetected(Note note, double centsOff);
    void OnNoteLost();
}
```

The shell handles all pitch detection plumbing (attack ignore, median smoothing, stability tracking, harmonic correction). Modes only receive stable, resolved notes.

## Current Modes

### Teacher Mode
Active. See [teacher-mode.md](teacher-mode.md).

## Planned Modes

### Autonomous Mode
Not yet implemented. See [autonomous-mode.md](autonomous-mode.md).

## Shared Settings (always present in footer)

- Input device selector
- Gate threshold (RMS signal level)
- Bass harmonics correction toggle
- Mic start/stop toggle
- Detected note readout

These controls are available in every mode so the teacher/student can adjust sensitivity live without leaving the mode.
