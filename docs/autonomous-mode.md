# Student Mode

## Status

Implemented.

## Summary

A self-study mode where the application generates targets and can auto-advance after correct notes. Students can restrict the available pitch classes and choose how the app picks targets.

## Features

- **Auto-advance**: after a correct note, the app automatically presents a new note.
- **Configurable options**: available notes, accidentals, octave matching, labels, and timing.
- **Random** uses the original generator and allows repeats.
- **Gentle** plays each enabled pitch class once per round. A note played correctly after a mistake moves earlier in the next round. Response time and skips carry no penalty.
- **Adaptive** revisits harder notes based on mistakes, skips, and response time. It limits immediate repetition and favors familiar musical intervals between targets.
- **Shuffle** plays each enabled pitch class once per round in random order, avoiding repeats between rounds when more than one pitch class is available.
- **Fretboard feedback**: reveals the played position after an attempt.

## Distinction from Teacher Mode

| Aspect | Teacher Mode | Autonomous Mode |
|--------|-------------|-----------------|
| Note selection | Teacher picks manually | App generates automatically |
| Auto-advance | No | Yes |
| Learning model | No | Four coach choices, with session-based feedback in Gentle and Adaptive |
| Fretboard reveal | Student's position only (wrong) | Played position and correct feedback |
| Settings | Shared footer | Shared footer + mode-specific options |
