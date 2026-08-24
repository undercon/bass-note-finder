using System.Reflection;
using BassNoteFinder.Audio;
using NAudio.Wave;
using Xunit;

namespace BassNoteFinder.Tests;

public class AudioCaptureServiceTests
{
    [Fact]
    public void MinSignalLevel_IsClampedToSupportedRange()
    {
        using var service = new AudioCaptureService();

        service.MinSignalLevel = 0.001f;
        Assert.Equal(0.005f, service.MinSignalLevel);

        service.MinSignalLevel = 0.5f;
        Assert.Equal(0.02f, service.MinSignalLevel);
    }

    [Fact]
    public void StopCapture_EmitsZeroSignalLevel()
    {
        using var service = new AudioCaptureService();
        var levels = new List<float>();
        service.SignalLevelMeasured += levels.Add;

        service.StopCapture();

        Assert.False(service.IsCapturing);
        Assert.Equal([0f], levels);
    }

    [Fact]
    public void StartCapture_InvalidDevice_ReportsAnErrorWithoutCapturing()
    {
        using var service = new AudioCaptureService();
        string? error = null;
        service.ErrorOccurred += message => error = message;

        bool started = service.StartCapture(-1);

        Assert.False(started);
        Assert.False(service.IsCapturing);
        Assert.NotNull(error);
    }

    [Fact]
    public void ProcessBuffer_Silence_MeasuresSilenceAndSignalsLostPitch()
    {
        using var service = new AudioCaptureService(bufferSize: 4096);
        var levels = new List<float>();
        int lostCount = 0;
        service.SignalLevelMeasured += levels.Add;
        service.PitchLost += () => lostCount++;
        SetBuffer(service, new float[4096]);

        TestHelpers.InvokePrivate(service, "ProcessBuffer");

        Assert.Equal([0f], levels);
        Assert.Equal(1, lostCount);
    }

    [Fact]
    public void OnDataAvailable_FillsBufferAndProcessesCompletedFrame()
    {
        using var service = new AudioCaptureService(bufferSize: 4);
        var levels = new List<float>();
        int lostCount = 0;
        service.SignalLevelMeasured += levels.Add;
        service.PitchLost += () => lostCount++;
        SetBuffer(service, new float[4]);

        var audio = new WaveInEventArgs(new byte[8], 8);
        TestHelpers.InvokePrivate(service, "OnDataAvailable", null, audio);

        Assert.Equal([0f], levels);
        Assert.Equal(1, lostCount);
    }

    [Fact]
    public void GetInputDevices_ReturnsAUsableCollection()
    {
        IReadOnlyList<string> devices = AudioCaptureService.GetInputDevices();

        Assert.NotNull(devices);
        Assert.All(devices, device => Assert.False(string.IsNullOrWhiteSpace(device)));
    }

    [Fact]
    public void PreferHigherOctave_ProxiesDetectorPreference()
    {
        using var service = new AudioCaptureService();

        service.PreferHigherOctave = false;

        Assert.False(service.PreferHigherOctave);
    }

    [Fact]
    public void ProcessBuffer_WithoutAllocatedBuffer_DoesNothing()
    {
        using var service = new AudioCaptureService();
        int signalEvents = 0;
        service.SignalLevelMeasured += _ => signalEvents++;

        TestHelpers.InvokePrivate(service, "ProcessBuffer");

        Assert.Equal(0, signalEvents);
    }

    [Fact]
    public void ProcessBuffer_AudiblePeriodicSignal_ReportsSignalAndPitch()
    {
        const int sampleRate = 44100;
        using var service = new AudioCaptureService(sampleRate, 8192);
        float? level = null;
        double? pitch = null;
        service.SignalLevelMeasured += value => level = value;
        service.PitchDetected += value => pitch = value;
        SetBuffer(service, CreateSineWave(sampleRate, 8192, 110));

        TestHelpers.InvokePrivate(service, "ProcessBuffer");

        Assert.NotNull(level);
        Assert.True(level > service.MinSignalLevel);
        Assert.InRange(pitch!.Value, 100, 120);
    }

    private static void SetBuffer(AudioCaptureService service, float[] samples)
    {
        var field = typeof(AudioCaptureService).GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(service, samples);
    }

    private static float[] CreateSineWave(int sampleRate, int sampleCount, double frequency)
    {
        var samples = new float[sampleCount];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)(0.25 * Math.Sin(2 * Math.PI * frequency * i / sampleRate));
        }

        return samples;
    }
}
