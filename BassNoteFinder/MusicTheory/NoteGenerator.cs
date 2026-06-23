namespace BassNoteFinder.MusicTheory;

public class NoteGenerator
{
    private readonly Random _rng = new();
    private readonly int _minMidi;
    private readonly int _maxMidi;

    public NoteGenerator(int minMidi = 28, int maxMidi = 67)
    {
        _minMidi = minMidi;
        _maxMidi = maxMidi;
    }

    public Note RandomNote()
    {
        return new Note(_rng.Next(_minMidi, _maxMidi + 1));
    }

    public static int[] BassStringOpenNotes => [28, 33, 38, 43];
}
