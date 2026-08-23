using BassNoteFinder.Gameplay;
using Xunit;

namespace BassNoteFinder.Tests;

public class AdaptivePracticeEngineTests
{
    private static readonly int[] NaturalPitchClasses = [0, 2, 4, 5, 7, 9, 11];

    [Fact]
    public void ChooseNext_WithSeveralNotes_DoesNotRepeatEitherOfTheLastTwoNotes()
    {
        var engine = new AdaptivePracticeEngine(new Random(17));
        var sequence = Enumerable.Range(0, 40)
            .Select(_ => engine.ChooseNext(NaturalPitchClasses))
            .ToArray();

        for (int i = 1; i < sequence.Length; i++)
        {
            Assert.NotEqual(sequence[i - 1], sequence[i]);
            if (i >= 2)
            {
                Assert.NotEqual(sequence[i - 2], sequence[i]);
            }
        }
    }

    [Fact]
    public void ChooseNext_RevisitsAStrugglingNoteMoreOftenWithoutHammeringIt()
    {
        var engine = new AdaptivePracticeEngine(new Random(23));
        foreach (int pitchClass in NaturalPitchClasses)
        {
            engine.RecordResult(pitchClass, mistakes: 0, TimeSpan.FromSeconds(2));
        }

        for (int i = 0; i < 4; i++)
        {
            engine.RecordResult(4, mistakes: 2, TimeSpan.FromSeconds(9)); // E
        }

        int[] sequence = Enumerable.Range(0, 60)
            .Select(_ => engine.ChooseNext(NaturalPitchClasses))
            .ToArray();
        var counts = sequence.GroupBy(pitchClass => pitchClass)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.True(counts[4] > counts.Where(pair => pair.Key != 4).Average(pair => pair.Value));
        Assert.DoesNotContain(sequence.Zip(sequence.Skip(1)), pair => pair.First == pair.Second);
    }

    [Fact]
    public void RecordResult_SlowCorrectAnswersIncreaseSelectionPriority()
    {
        var engine = new AdaptivePracticeEngine(new Random(31));
        foreach (int pitchClass in NaturalPitchClasses)
        {
            TimeSpan response = pitchClass == 9
                ? TimeSpan.FromSeconds(11)
                : TimeSpan.FromSeconds(2);
            engine.RecordResult(pitchClass, mistakes: 0, response);
        }

        int[] sequence = Enumerable.Range(0, 60)
            .Select(_ => engine.ChooseNext(NaturalPitchClasses))
            .ToArray();
        var counts = sequence.GroupBy(pitchClass => pitchClass)
            .ToDictionary(group => group.Key, group => group.Count());

        Assert.True(counts[9] > counts.Where(pair => pair.Key != 9).Average(pair => pair.Value));
    }

    [Fact]
    public void ChooseNext_WithOnlyOneNote_AllowsThatNoteToRepeat()
    {
        var engine = new AdaptivePracticeEngine(new Random(5));

        Assert.Equal(7, engine.ChooseNext([7]));
        Assert.Equal(7, engine.ChooseNext([7]));
    }
}
