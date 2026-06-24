# Note Range

## Standard 4-String Bass Tuning

| String | Open Note | MIDI | Frequency |
|--------|-----------|------|-----------|
| E (lowest) | E1 | 28 | ~41.2 Hz |
| A | A1 | 33 | ~55.0 Hz |
| D | D2 | 38 | ~73.4 Hz |
| G (highest) | G2 | 43 | ~98.0 Hz |

## Playable Range (12 Frets)

- **Lowest note**: E1 (MIDI 28) — open E string
- **Highest note**: G3 (MIDI 55) — 12th fret of G string

## Bug History

The original `NoteGenerator` was constructed with range `(28, 67)`, which included notes up to G4 (MIDI 67). Notes above MIDI 55 are unplayable on a standard 4-string bass with 12 frets. The `FretboardRenderer.FindNotePosition` method returned `(-1, 99)` for these notes, causing them to silently disappear from the fretboard.

**Fix**: Changed the range to `(28, 55)` so all generated notes are playable.
