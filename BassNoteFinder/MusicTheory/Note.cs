namespace BassNoteFinder.MusicTheory;

public readonly record struct Note
{
    private static readonly string[] Names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    private static readonly string[] FlatNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };
    private static readonly int[] DiatonicMap = { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 };

    // Pitch classes that are sharps/naturals-with-a-sharp-name: C#, D#, F#, G#, A#
    private static readonly bool[] SharpPitchClasses = { false, true, false, true, false, false, true, false, true, false, true, false };

    public int MidiNote { get; }

    /// <summary>Returns false for the sentinel invalid note (MidiNote &lt; 0).</summary>
    public bool IsValid => MidiNote >= 0;

    public int Octave => MidiNote / 12 - 1;
    public int PitchClass => (MidiNote % 12 + 12) % 12;
    public int DiatonicClass => DiatonicMap[PitchClass];
    public double Frequency => 440.0 * Math.Pow(2, (MidiNote - 69) / 12.0);
    public string Name => Names[PitchClass];
    public string NameFlat => FlatNames[PitchClass];
    public string FullName => $"{Names[PitchClass]}{Octave}";
    public string FullNameFlat => $"{FlatNames[PitchClass]}{Octave}";

    /// <summary>True when this note's canonical name contains a sharp (C#, D#, F#, G#, A#).</summary>
    public bool IsSharp => SharpPitchClasses[PitchClass];

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

    /// <summary>
    /// Returns the octave equivalent of <paramref name="note"/> whose MIDI number
    /// is closest to <paramref name="reference"/>. Useful for resolving harmonic
    /// octave ambiguity relative to a known target.
    /// </summary>
    public static Note ClosestPitchClassToReference(Note note, Note reference)
    {
        // PitchClass is already normalised (0–11); no double-normalisation needed.
        int pc = note.PitchClass;
        // Find the integer n that minimises |reference.MidiNote - (pc + 12n)|.
        int n = (int)Math.Round((reference.MidiNote - pc) / 12.0);
        return new Note(pc + 12 * n);
    }

    public override string ToString() => FullName;
}
