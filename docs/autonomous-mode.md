# Student Mode

## Status

Implemented.

## Summary

A self-study mode where the application generates targets and can auto-advance after correct notes. Students can restrict the available pitch classes and optionally enable an adaptive coach.

## Features

- **Auto-advance**: after a correct note, the app automatically presents a new note.
- **Configurable options**: available notes, accidentals, octave matching, labels, and timing.
- **Adaptive coach**: tracks mistakes, skips, and response time during the current session.
- **Smooth reinforcement**: prioritizes weak notes while preventing immediate repetition and favoring step, fourth/fifth, and chord-tone movement between targets.
- **Fretboard feedback**: reveals the played position after an attempt.

## Distinction from Teacher Mode

| Aspect | Teacher Mode | Autonomous Mode |
|--------|-------------|-----------------|
| Note selection | Teacher picks manually | App generates automatically |
| Auto-advance | No | Yes |
| Learning model | No | Optional session-based adaptive coach |
| Fretboard reveal | Student's position only (wrong) | Played position and correct feedback |
| Settings | Shared footer | Shared footer + mode-specific options |
