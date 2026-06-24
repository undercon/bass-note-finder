namespace BassNoteFinder.MusicTheory;

public static class NoteDisplay
{
    public const int BassWrittenOctaveOffset = 12;

    private static readonly string[] SharpNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    private static readonly string[] FlatNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

    public enum AccidentalDisplay
    {
        Natural,
        Sharp,
        Flat
    }

    public static int ToWrittenMidi(int soundingMidi) => soundingMidi + BassWrittenOctaveOffset;

    public static int ToSoundingMidi(int writtenMidi) => writtenMidi - BassWrittenOctaveOffset;

    public static Note ToWritten(Note sounding) => new(ToWrittenMidi(sounding.MidiNote));

    public static string Format(Note soundingNote, AccidentalDisplay accidental, bool includeOctave)
    {
        int pitchClass = (soundingNote.MidiNote % 12 + 12) % 12;
        string baseName = accidental switch
        {
            AccidentalDisplay.Flat => FlatNames[pitchClass],
            AccidentalDisplay.Sharp => SharpNames[pitchClass],
            _ => soundingNote.Name
        };

        return includeOctave ? $"{baseName}{soundingNote.Octave}" : baseName;
    }
}
