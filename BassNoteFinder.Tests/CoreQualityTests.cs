using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;
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
    [InlineData(33, 2, 0)]
    [InlineData(38, 1, 0)]
    [InlineData(43, 0, 0)]
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
