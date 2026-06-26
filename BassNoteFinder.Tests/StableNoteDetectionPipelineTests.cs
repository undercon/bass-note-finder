using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using Xunit;

namespace BassNoteFinder.Tests;

public class StableNoteDetectionPipelineTests
{
    [Fact]
    public void Pipeline_EmitsStableNote_AfterAttackIgnoreAndStabilityWindow()
    {
        var pipeline = new StableNoteDetectionPipeline();
        Note? detected = null;
        int detections = 0;
        pipeline.StableNoteDetected += (note, cents) =>
        {
            detected = note;
            detections++;
        };

        pipeline.ProcessFrequency(98.0);
        pipeline.ProcessFrequency(98.0);
        pipeline.ProcessFrequency(98.0);

        Assert.Equal(1, detections);
        Assert.Equal("G2", detected!.Value.FullName);
    }

    [Fact]
    public void Pipeline_AfterStableLoss_CanEmitSameNoteAgain()
    {
        var pipeline = new StableNoteDetectionPipeline();
        int detections = 0;
        int losses = 0;
        pipeline.StableNoteDetected += (_, _) => detections++;
        pipeline.StableNoteLost += () => losses++;

        pipeline.ProcessFrequency(98.0);
        pipeline.ProcessFrequency(98.0);
        pipeline.ProcessFrequency(98.0);

        pipeline.ProcessLost();
        pipeline.ProcessLost();
        pipeline.ProcessLost();

        pipeline.ProcessFrequency(98.0);
        pipeline.ProcessFrequency(98.0);
        pipeline.ProcessFrequency(98.0);

        Assert.Equal(2, detections);
        Assert.Equal(1, losses);
    }

    [Fact]
    public void Pipeline_HarmonicCorrectionToggle_ChangesResolvedNote()
    {
        var corrected = RunA1ThenJumpToA2(useCorrection: true);
        var uncorrected = RunA1ThenJumpToA2(useCorrection: false);

        Assert.DoesNotContain(corrected, n => n.FullName == "A2");
        Assert.Contains(uncorrected, n => n.FullName == "A2");
    }

    private static List<Note> RunA1ThenJumpToA2(bool useCorrection)
    {
        var pipeline = new StableNoteDetectionPipeline { UseHarmonicCorrection = useCorrection };
        var notes = new List<Note>();
        pipeline.StableNoteDetected += (note, _) => notes.Add(note);

        pipeline.ProcessFrequency(55.0);
        pipeline.ProcessFrequency(55.0);
        pipeline.ProcessFrequency(55.0);

        pipeline.ProcessFrequency(110.0);
        pipeline.ProcessFrequency(110.0);
        pipeline.ProcessFrequency(110.0);

        return notes;
    }
}
