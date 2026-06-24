using BassNoteFinder.Rendering;

namespace BassNoteFinder.MusicTheory;

public class NoteGenerator
{
    private readonly Random _rng = new();
    private readonly int _minMidi;
    private readonly int _maxMidi;

    private static readonly int[] NaturalPitchClasses = { 0, 2, 4, 5, 7, 9, 11 };

    public NoteGenerator(int minMidi = 28, int maxMidi = 48)
    {
        _minMidi = minMidi;
        _maxMidi = maxMidi;
    }

    public Note RandomNote()
    {
        var naturals = GetNaturalNotes();
        return naturals[_rng.Next(naturals.Count)];
    }

    public (Note note, StaffRenderer.AccidentalMode mode) RandomNoteWithAccidental()
    {
        if (_rng.Next(3) == 0)
        {
            var naturals = GetNaturalNotes();
            var note = naturals[_rng.Next(naturals.Count)];
            return (note, StaffRenderer.AccidentalMode.Natural);
        }

        var accidentals = GetAccidentalNotes();
        if (accidentals.Count == 0)
        {
            var naturals = GetNaturalNotes();
            return (naturals[_rng.Next(naturals.Count)], StaffRenderer.AccidentalMode.Natural);
        }

        var (accNote, accMode) = accidentals[_rng.Next(accidentals.Count)];
        return (accNote, accMode);
    }

    private List<Note> GetNaturalNotes()
    {
        var result = new List<Note>();
        for (int midi = _minMidi; midi <= _maxMidi; midi++)
        {
            int pc = (midi % 12 + 12) % 12;
            if (NaturalPitchClasses.Contains(pc))
            {
                result.Add(new Note(midi));
            }
        }
        return result;
    }

    private List<(Note note, StaffRenderer.AccidentalMode mode)> GetAccidentalNotes()
    {
        var result = new List<(Note, StaffRenderer.AccidentalMode)>();
        for (int midi = _minMidi; midi <= _maxMidi; midi++)
        {
            int pc = (midi % 12 + 12) % 12;
            if (!NaturalPitchClasses.Contains(pc))
            {
                if (midi > _minMidi)
                {
                    result.Add((new Note(midi), StaffRenderer.AccidentalMode.Sharp));
                }
                if (midi < _maxMidi)
                {
                    result.Add((new Note(midi), StaffRenderer.AccidentalMode.Flat));
                }
            }
        }
        return result;
    }

    public static int[] BassStringOpenNotes => [28, 33, 38, 43];
}
