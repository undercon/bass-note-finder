# Teacher Mode

## Summary

A teacher-led mode where the teacher selects notes on the staff and the student plays them on bass. The fretboard is hidden until the student plays, then reacts to correct/incorrect attempts.

## Flow

1. Teacher places a note on the staff (click to place) or presses **Random** for a random note in range.
2. The note stays highlighted on the staff until the teacher selects another.
3. Student plays the note on their bass.
4. The application detects the pitch and reacts:
   - **Correct**: happy animation on the fretboard area. Fretboard does not reveal the correct position (student already found it).
   - **Wrong**: mistake animation on the fretboard area + briefly flash where the student played (not the correct position). The flash hides and the student can retry.
5. No auto-advance. The teacher controls progression by selecting the next note.

## Key Behaviors

- **No auto-advance**: the round only ends when the teacher picks a new note. There is no scoring.
- **Retry on wrong**: after a wrong answer, the fretboard hides again and the student can retry the same note.
- **Fretboard reveal on wrong**: shows the student's played position briefly (flash and hide), never the correct position.
- **Fretboard on correct**: happy animation only, no fret position reveal.
- **Note persistence**: the selected note remains highlighted on the staff until the next one is chosen.

## Note Selection

- **Click on staff**: teacher clicks a vertical position on the staff canvas. The note snaps to the nearest valid position in the playable range (E1-G3, MIDI 28-55). Any chromatic note in range is valid.
- **Random button**: picks a random note in the E1-G3 range.

## Fretboard States

| State | Description |
|-------|-------------|
| Hidden | Default. Fretboard area shows a placeholder. |
| Flashing (wrong) | Briefly shows the student's played position in red, then returns to Hidden. |
| Celebrating (correct) | Shows a happy animation (green pulse/bounce), then returns to Hidden. |

## Teacher Interaction with Camera

The teacher shows mistakes to the camera as part of the lesson. The flash-and-hide behavior lets the teacher briefly display where the student played without revealing the answer.
