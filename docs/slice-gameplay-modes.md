# Slice: Gameplay Modes Architecture

## Status

Complete — architecture in place, Teacher Mode implemented, ready for iterative refinement.

## Branch

`feature/gameplay-modes` (off `POC_2` / commit `12f7de5`)

## What was done

### Architecture restructure
- Converted `MainWindow` from a single-screen app into a **shell** hosting a `ContentControl`
- Shell retains: audio capture, pitch detection plumbing, config persistence, footer controls, dark theme
- Each game mode is a `UserControl` implementing `IGameMode`
- Shell routes resolved (stable) notes to the active mode via `OnNoteDetected`

### New files
- `Gameplay/IGameMode.cs` — interface for all game modes
- `Views/MenuView.xaml` / `.xaml.cs` — mode selection screen
- `Views/TeacherModeView.xaml` / `.xaml.cs` — Teacher Mode gameplay screen
- `docs/modes-overview.md` — architecture documentation
- `docs/teacher-mode.md` — Teacher Mode spec
- `docs/autonomous-mode.md` — future Autonomous Mode placeholder
- `docs/note-range.md` — note range documentation and bug history
- `docs/slice-gameplay-modes.md` — this file

### Modified files
- `MainWindow.xaml` — shell layout with `ContentControl` + footer
- `MainWindow.xaml.cs` — shell logic, view navigation, pitch detection routing
- `MusicTheory/NoteGenerator.cs` — fixed range from `(28, 67)` to `(28, 55)`
- `Rendering/StaffRenderer.cs` — dark-theme-visible colors, `RenderEmpty()`, `NoteFromY()` for click-to-place
- `Rendering/FretboardRenderer.cs` — added `highlightColor` parameter

### Bug fixes
- **Note range**: notes above MIDI 55 (G3) were unplayable on a 4-string bass. Fixed to E1-G3.
- **Staff visibility**: staff lines, clef, and notes were black on dark background. Now use visible colors.

### Teacher Mode behavior
- Teacher clicks the staff to place a note (snaps to nearest valid position in E1-G3)
- "Random" button (also Space key) picks a random note in range
- Note stays highlighted on the staff until another is selected
- Fretboard is hidden by default ("?" placeholder)
- On wrong answer: fretboard flashes student's played position in red for 1.5s, then hides
- On correct answer: green checkmark celebration for 1.5s, then hides
- No auto-advance — teacher controls progression
- Student can retry the same note after a wrong answer
- "Back to Menu" button returns to mode selection

## What was NOT done (deferred)

### Fretboard animations
- Current reactions are simple overlays (checkmark, red flash). No elaborate animations yet.
- Future: WPF storyboards for scale bounce, color pulse, shake effects.

### Staff click refinement
- Click snaps to nearest diatonic position. No visual feedback during hover.
- Future: show a preview note at the hovered position before clicking.

### Fretboard reveal semantics
- Currently shows student's played position on wrong answers only.
- No "show correct position" option yet (by design for Teacher Mode).

### Autonomous Mode
- Documented in `docs/autonomous-mode.md` but not implemented.
- Will be a separate slice.

## Plans for this slice (next steps)

1. **Test the full flow** — launch the app, enter Teacher Mode, place notes, play notes, verify reactions
2. **Staff click UX** — add hover preview, ensure clicking outside valid range gives feedback
3. **Fretboard flash polish** — the current red flash could be more visually distinct (border, text label with note name)
4. **Status text improvements** — ensure status text is clear in all states (no note selected, waiting for student, correct, wrong)
5. **Cleanup** — remove any dead code from the old single-screen implementation
6. **Commit and push** the branch for review

## Technical notes

- The shell's pitch detection pipeline (attack ignore, median smoothing, stability tracking, harmonic correction) is unchanged from POC_2. Only the routing target changed — instead of updating local UI, it calls `_activeMode.OnNoteDetected()`.
- The `_lastResolvedMidiNote` guard in the shell prevents the same sustained note from repeatedly triggering the mode. This resets on pitch loss.
- The `TeacherModeView` uses a `DispatcherTimer` for the flash duration (1.5s) and returns to the hidden state automatically.
- Config persistence is unchanged — window size/position, gate, input device, harmonic correction all still save immediately.
