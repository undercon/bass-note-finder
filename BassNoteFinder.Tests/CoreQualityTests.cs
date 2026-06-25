using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;
using BassNoteFinder.Audio;
using Xunit;

namespace BassNoteFinder.Tests;

public class NoteDisplayTests
{
    [Theory]
    [InlineData(28, 40)]
    [InlineData(48, 60)]
    [InlineData(55, 67)]
    public void ToWrittenMidi_ShiftsByBassOctave(int soundingMidi, int expectedWrittenMidi)
    {
        Assert.Equal(expectedWrittenMidi, NoteDisplay.ToWrittenMidi(soundingMidi));
        Assert.Equal(soundingMidi, NoteDisplay.ToSoundingMidi(expectedWrittenMidi));
    }

    [Theory]
    [InlineData(48, NoteDisplay.AccidentalDisplay.Natural, true, "C3")]
    [InlineData(48, NoteDisplay.AccidentalDisplay.Natural, false, "C")]
    [InlineData(49, NoteDisplay.AccidentalDisplay.Sharp, true, "C#3")]
    [InlineData(49, NoteDisplay.AccidentalDisplay.Flat, true, "Db3")]
    [InlineData(49, NoteDisplay.AccidentalDisplay.Flat, false, "Db")]
    public void Format_UsesRequestedAccidentalAndOctave(int midi, NoteDisplay.AccidentalDisplay accidental, bool includeOctave, string expected)
    {
        string actual = NoteDisplay.Format(new Note(midi), accidental, includeOctave);
        Assert.Equal(expected, actual);
    }
}

public class NoteMathTests
{
    [Fact]
    public void ClosestPitchClassToReference_MapsEquivalentPitchNearReference()
    {
        Note detectedHarmonic = new(31); // G1
        Note target = new(43); // G2

        Note resolved = Note.ClosestPitchClassToReference(detectedHarmonic, target);

        Assert.Equal(43, resolved.MidiNote);
    }

    [Theory]
    [InlineData(1)]   // C#
    [InlineData(3)]   // D#
    [InlineData(6)]   // F#
    [InlineData(8)]   // G#
    [InlineData(10)]  // A#
    public void IsSharp_ReturnsTrue_ForSharpPitchClasses(int pitchClass)
    {
        Assert.True(new Note(pitchClass).IsSharp);
    }

    [Theory]
    [InlineData(0)]   // C
    [InlineData(2)]   // D
    [InlineData(4)]   // E
    [InlineData(5)]   // F
    [InlineData(7)]   // G
    [InlineData(9)]   // A
    [InlineData(11)]  // B
    public void IsSharp_ReturnsFalse_ForNaturalPitchClasses(int pitchClass)
    {
        Assert.False(new Note(pitchClass).IsSharp);
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForSentinelNote()
    {
        Assert.False(new Note(-1).IsValid);
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForNormalNote()
    {
        Assert.True(new Note(40).IsValid);
    }
}

public class StaffRendererMappingTests
{
    [Fact]
    public void NoteFromPoint_CenterColumn_MapsToNaturalNote()
    {
        var result = StaffRenderer.NoteFromPoint(250, 40, includeAccidentals: false);

        Assert.True(result.HasValue);
        Assert.Equal(StaffRenderer.AccidentalMode.Natural, result.Value.mode);
        Assert.Equal("C3", result.Value.note.FullName);
    }

    [Fact]
    public void NoteFromPoint_AccidentalColumns_MapToFlatAndSharp()
    {
        var flat = StaffRenderer.NoteFromPoint(190, 40, includeAccidentals: true);
        var sharp = StaffRenderer.NoteFromPoint(310, 40, includeAccidentals: true);

        Assert.True(flat.HasValue);
        Assert.True(sharp.HasValue);

        Assert.Equal(StaffRenderer.AccidentalMode.Flat, flat.Value.mode);
        Assert.Equal("B2", flat.Value.note.FullName);

        Assert.Equal(StaffRenderer.AccidentalMode.Sharp, sharp.Value.mode);
        Assert.Equal("C#3", sharp.Value.note.FullName);
    }

    [Fact]
    public void NoteFromPoint_OutOfRange_ReturnsNull()
    {
        var result = StaffRenderer.NoteFromPoint(250, -200, includeAccidentals: true);
        Assert.False(result.HasValue);
    }
}

public class FretboardRendererTests
{
    [Theory]
    [InlineData(28, 3, 0)]
    [InlineData(33, 3, 5)]
    [InlineData(38, 2, 5)]
    [InlineData(43, 1, 5)]
    [InlineData(40, 1, 2)]
    public void FindNotePosition_ReturnsExpectedStringAndFret(int midi, int expectedStringIndex, int expectedFret)
    {
        var (stringIndex, fret) = FretboardRenderer.FindNotePosition(new Note(midi));

        Assert.Equal(expectedStringIndex, stringIndex);
        Assert.Equal(expectedFret, fret);
    }

    [Fact]
    public void FindNotePosition_WhenUnplayable_ReturnsNoPosition()
    {
        var (stringIndex, fret) = FretboardRenderer.FindNotePosition(new Note(10));

        Assert.Equal(-1, stringIndex);
        Assert.Equal(99, fret);
    }

    [Fact]
    public void FindNotePosition_WithStaffHint_UsesClosestEquivalentPitchToTarget()
    {
        var (stringIndex, fret) = FretboardRenderer.FindNotePosition(
            note: new Note(45),
            staffReferenceNote: new Note(33));

        Assert.Equal(3, stringIndex);
        Assert.Equal(5, fret);
    }
}

public class NoteGeneratorTests
{
    [Fact]
    public void RandomNote_StaysWithinRangeAndNaturalPitchClasses()
    {
        var generator = new NoteGenerator(28, 48);

        for (int i = 0; i < 200; i++)
        {
            Note note = generator.RandomNote();
            int pitchClass = (note.MidiNote % 12 + 12) % 12;

            Assert.InRange(note.MidiNote, 28, 48);
            Assert.Contains(pitchClass, new[] { 0, 2, 4, 5, 7, 9, 11 });
        }
    }

    [Fact]
    public void RandomNoteWithAccidental_StaysWithinRange()
    {
        var generator = new NoteGenerator(28, 48);

        for (int i = 0; i < 300; i++)
        {
            var (note, mode) = generator.RandomNoteWithAccidental();

            Assert.InRange(note.MidiNote, 28, 48);
            Assert.Contains(mode, new[]
            {
                StaffRenderer.AccidentalMode.Natural,
                StaffRenderer.AccidentalMode.Sharp,
                StaffRenderer.AccidentalMode.Flat
            });
        }
    }
}

public class PitchDetectorOctaveTests
{
    [Fact]
    public void DetectPitch_StrongSecondHarmonicNearE1_ResolvesToE1()
    {
        const int sampleRate = 44100;
        const int bufferSize = 8192;

        float[] samples = GenerateSignal(
            sampleRate,
            bufferSize,
            41.2034,
            (1.0, 1.00),
            (2.0, 1.45),
            (3.0, 0.30));

        var detector = new PitchDetector(sampleRate, bufferSize)
        {
            PreferHigherOctave = false
        };

        double pitch = detector.DetectPitch(samples);
        Assert.True(pitch > 0);

        Note.CentsOffFromFrequency(pitch, out var note);
        Assert.Equal("E1", note.FullName);
    }

    [Fact]
    public void DetectPitch_A1Fundamental_RemainsA1()
    {
        const int sampleRate = 44100;
        const int bufferSize = 8192;

        float[] samples = GenerateSignal(
            sampleRate,
            bufferSize,
            55.0,
            (1.0, 1.00),
            (2.0, 0.40),
            (3.0, 0.20));

        var detector = new PitchDetector(sampleRate, bufferSize)
        {
            PreferHigherOctave = false
        };

        double pitch = detector.DetectPitch(samples);
        Assert.True(pitch > 0);

        Note.CentsOffFromFrequency(pitch, out var note);
        Assert.Equal("A1", note.FullName);
    }

    private static float[] GenerateSignal(int sampleRate, int sampleCount, double baseFrequency, params (double multiple, double amplitude)[] harmonics)
    {
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            double time = i / (double)sampleRate;
            double value = 0;

            foreach (var (multiple, amplitude) in harmonics)
            {
                value += amplitude * Math.Sin(2.0 * Math.PI * baseFrequency * multiple * time);
            }

            samples[i] = (float)(value * 0.25);
        }

        return samples;
    }
}
