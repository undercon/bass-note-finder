namespace BassNoteFinder.MusicTheory;

public readonly record struct Note
{
    private static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    private static readonly string[] FlatNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
    private static readonly int[] DiatonicMap = { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 };

    public int MidiNote { get; }
    public int Octave => MidiNote / 12 - 1;
    public int PitchClass => (MidiNote % 12 + 12) % 12;
    public int DiatonicClass => DiatonicMap[PitchClass];
    public double Frequency => 440.0 * Math.Pow(2, (MidiNote - 69) / 12.0);
    public string Name => Names[PitchClass];
    public string NameFlat => FlatNames[PitchClass];
    public string FullName => $"{Names[PitchClass]}{Octave}";
    public string FullNameFlat => $"{FlatNames[PitchClass]}{Octave}";

    public Note(int midiNote)
    {
        MidiNote = midiNote;
    }

    public static Note FromFrequency(double freq)
    {
        if (freq <= 0) return new Note(-1);
        var midi = (int)Math.Round(12 * Math.Log2(freq / 440.0) + 69);
        return new Note(midi);
    }

    public static double CentsOffFromFrequency(double freq, out Note nearest)
    {
        if (freq <= 0) { nearest = new Note(-1); return 0; }
        var midiFloat = 12 * Math.Log2(freq / 440.0) + 69;
        var midiRounded = (int)Math.Round(midiFloat);
        nearest = new Note(midiRounded);
        return (midiFloat - midiRounded) * 100;
    }

    public int StaffPosition()
    {
        return (DiatonicClass - 4) + 7 * (Octave - 2);
    }

    public bool IsSharp => Name.Contains('#');
    public override string ToString() => FullName;
}
