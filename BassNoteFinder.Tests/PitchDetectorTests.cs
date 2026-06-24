using System.IO;
using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;

namespace BassNoteFinder.Tests;

public class PitchDetectorTests
{
    [Fact]
    public void DetectE1FromRecording_ReturnsE1NotE2()
    {
        string recordingPath = Path.Combine(AppContext.BaseDirectory, "test_e1.wav");
        if (!File.Exists(recordingPath))
        {
            return;
        }

        var samples = LoadWavMono(recordingPath, out int sampleRate);
        Assert.True(samples.Length > 0);

        var detector = new PitchDetector(sampleRate, 8192);
        int bufferSize = 8192;
        int detectedE2 = 0;
        int detectedE1 = 0;
        int detectedOther = 0;

        for (int offset = 0; offset + bufferSize <= samples.Length; offset += bufferSize / 4)
        {
            float[] buffer = new float[bufferSize];
            Array.Copy(samples, offset, buffer, 0, bufferSize);

            double pitch = detector.DetectPitch(buffer);
            if (pitch <= 0) continue;

            var centsOff = Note.CentsOffFromFrequency(pitch, out var note);
            string name = note.FullName;

            if (name == "E1") detectedE1++;
            else if (name == "E2") detectedE2++;
            else detectedOther++;

            Console.WriteLine($"Offset {offset,6}: {pitch,8:F2} Hz -> {name} ({centsOff:F0}c)");
        }

        Console.WriteLine($"\nSummary: E1={detectedE1}, E2={detectedE2}, Other={detectedOther}");
        Assert.True(detectedE1 >= detectedE2, $"E1 detected {detectedE1} times but E2 detected {detectedE2} times");
    }

    private static float[] LoadWavMono(string path, out int sampleRate)
    {
        using var reader = new BinaryReader(File.OpenRead(path));

        reader.ReadBytes(12);
        int formatChunkSize = reader.ReadInt32();
        short audioFormat = reader.ReadInt16();
        short numChannels = reader.ReadInt16();
        sampleRate = reader.ReadInt32();
        reader.ReadInt32();
        reader.ReadInt16();
        short bitsPerSample = reader.ReadInt16();

        if (formatChunkSize > 16)
        {
            reader.ReadBytes(formatChunkSize - 16);
        }

        string chunkId = new string(reader.ReadChars(4));
        while (chunkId != "data")
        {
            int skip = reader.ReadInt32();
            reader.ReadBytes(skip);
            chunkId = new string(reader.ReadChars(4));
        }

        int dataSize = reader.ReadInt32();
        int bytesPerSample = bitsPerSample / 8;
        int numSamples = dataSize / bytesPerSample;

        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            if (bitsPerSample == 16)
            {
                short sample = reader.ReadInt16();
                samples[i] = sample / 32768f;
                if (numChannels == 2)
                {
                    reader.ReadInt16();
                }
            }
            else if (bitsPerSample == 32)
            {
                samples[i] = reader.ReadSingle();
                if (numChannels == 2)
                {
                    reader.ReadSingle();
                }
            }
        }

        return samples;
    }
}
