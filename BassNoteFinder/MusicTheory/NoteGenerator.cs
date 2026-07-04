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

    public Note RandomNote(IReadOnlySet<int>? allowedPitchClasses = null)
    {
        var naturals = GetNaturalNotes(allowedPitchClasses);
        if (naturals.Count == 0)
        {
            naturals = GetNaturalNotes();
        }

        return naturals[_rng.Next(naturals.Count)];
    }

    public (Note note, StaffRenderer.AccidentalMode mode) RandomNoteWithAccidental(IReadOnlySet<int>? allowedPitchClasses = null)
    {
        var naturals = GetNaturalNotes(allowedPitchClasses);
        var accidentals = GetAccidentalNotes(allowedPitchClasses);

        if (_rng.Next(3) == 0)
        {
            if (naturals.Count > 0)
            {
                var note = naturals[_rng.Next(naturals.Count)];
                return (note, StaffRenderer.AccidentalMode.Natural);
            }
        }

        if (accidentals.Count > 0)
        {
            var (accNote, accMode) = accidentals[_rng.Next(accidentals.Count)];
            return (accNote, accMode);
        }

        if (naturals.Count == 0)
        {
            naturals = GetNaturalNotes();
        }

        return (naturals[_rng.Next(naturals.Count)], StaffRenderer.AccidentalMode.Natural);
    }

    private List<Note> GetNaturalNotes(IReadOnlySet<int>? allowedPitchClasses = null)
    {
        var result = new List<Note>();
        for (int midi = _minMidi; midi <= _maxMidi; midi++)
        {
            int pc = (midi % 12 + 12) % 12;
            if (NaturalPitchClasses.Contains(pc) && IsAllowed(pc, allowedPitchClasses))
            {
                result.Add(new Note(midi));
            }
        }
        return result;
    }

    private List<(Note note, StaffRenderer.AccidentalMode mode)> GetAccidentalNotes(IReadOnlySet<int>? allowedPitchClasses = null)
    {
        var result = new List<(Note, StaffRenderer.AccidentalMode)>();
        for (int midi = _minMidi; midi <= _maxMidi; midi++)
        {
            int pc = (midi % 12 + 12) % 12;
            if (!NaturalPitchClasses.Contains(pc) && IsAllowed(pc, allowedPitchClasses))
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

    private static bool IsAllowed(int pitchClass, IReadOnlySet<int>? allowedPitchClasses)
    {
        return allowedPitchClasses == null || allowedPitchClasses.Count == 0 || allowedPitchClasses.Contains(pitchClass);
    }

    public static int[] BassStringOpenNotes => [28, 33, 38, 43];
}
