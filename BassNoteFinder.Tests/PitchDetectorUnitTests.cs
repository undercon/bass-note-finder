using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using Xunit;

namespace BassNoteFinder.Tests;

public class PitchDetectorUnitTests
{
    [Fact]
    public void DetectPitch_TooFewSamples_ReturnsNoPitch()
    {
        var detector = new PitchDetector();

        Assert.Equal(-1, detector.DetectPitch([0.1f, 0.2f, 0.3f]));
    }

    [Fact]
    public void DetectPitch_Silence_ReturnsNoPitch()
    {
        var detector = new PitchDetector();

        Assert.Equal(-1, detector.DetectPitch(new float[8192]));
    }

    [Fact]
    public void DetectPitch_StrongSecondHarmonicNearE1_ResolvesFundamental()
    {
        const int sampleRate = 44100;
        var detector = new PitchDetector(sampleRate, 8192)
        {
            PreferHigherOctave = false
        };

        double pitch = detector.DetectPitch(CreateSignal(
            sampleRate,
            8192,
            41.2034,
            (1.0, 1.00),
            (2.0, 1.45),
            (3.0, 0.30)));

        Note.CentsOffFromFrequency(pitch, out var note);
        Assert.Equal("E1", note.FullName);
    }

    [Fact]
    public void DetectPitch_A1Fundamental_RemainsA1()
    {
        const int sampleRate = 44100;
        var detector = new PitchDetector(sampleRate, 8192)
        {
            PreferHigherOctave = false
        };

        double pitch = detector.DetectPitch(CreateSignal(
            sampleRate,
            8192,
            55.0,
            (1.0, 1.00),
            (2.0, 0.40),
            (3.0, 0.20)));

        Note.CentsOffFromFrequency(pitch, out var note);
        Assert.Equal("A1", note.FullName);
    }

    private static float[] CreateSignal(
        int sampleRate,
        int sampleCount,
        double fundamental,
        params (double multiple, double amplitude)[] harmonics)
    {
        var samples = new float[sampleCount];
        for (int i = 0; i < samples.Length; i++)
        {
            double time = i / (double)sampleRate;
            double value = harmonics.Sum(harmonic =>
                harmonic.amplitude * Math.Sin(2 * Math.PI * fundamental * harmonic.multiple * time));
            samples[i] = (float)(value * 0.25);
        }

        return samples;
    }
}
