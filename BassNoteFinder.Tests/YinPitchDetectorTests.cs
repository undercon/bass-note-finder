using BassNoteFinder.Audio;
using Xunit;

namespace BassNoteFinder.Tests;

public class YinPitchDetectorTests
{
    [Theory]
    [InlineData(55.0)]
    [InlineData(110.0)]
    [InlineData(220.0)]
    public void DetectPitch_CleanSineWave_ReturnsDetectedPitch(double frequency)
    {
        const int sampleRate = 44100;
        var detector = new YinPitchDetector(sampleRate);

        double detected = detector.DetectPitch(CreateSineWave(sampleRate, 4096, frequency));

        Assert.True(detected > 0, "A clean periodic signal should produce a pitch candidate.");
    }

    [Fact]
    public void DetectPitch_Silence_ReturnsNoPitch()
    {
        var detector = new YinPitchDetector();

        double detected = detector.DetectPitch(new float[4096]);

        Assert.Equal(-1, detected);
    }

    [Fact]
    public void DetectPitch_InsufficientSamples_ReturnsNoPitch()
    {
        var detector = new YinPitchDetector();

        double detected = detector.DetectPitch([0.1f]);

        Assert.Equal(-1, detected);
    }

    private static float[] CreateSineWave(int sampleRate, int sampleCount, double frequency)
    {
        var samples = new float[sampleCount];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)Math.Sin(2 * Math.PI * frequency * i / sampleRate);
        }

        return samples;
    }
}
