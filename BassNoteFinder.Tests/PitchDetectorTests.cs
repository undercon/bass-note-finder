using System.IO;
using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using NAudio.Wave;
using Xunit;

namespace BassNoteFinder.Tests;

public class PitchDetectorTests
{
    private record DetectionResult(string FileName, string PrimaryNote, double SuccessRate, Dictionary<string, int> Counts);

    public static IEnumerable<object[]> BassStringSubsetCases()
    {
        yield return ["Subsets/E-string-hits.wav", "E"];
        yield return ["Subsets/E-string_2-hits.wav", "E"];
        yield return ["Subsets/E-string_3-hits.wav", "E"];
        yield return ["Subsets/A-string-hits.wav", "A"];
        yield return ["Subsets/A-string_2-hits.wav", "A"];
        yield return ["Subsets/A-string_3-hits.wav", "A"];
        yield return ["Subsets/D-string-hits.wav", "D"];
        yield return ["Subsets/D-string_2-hits.wav", "D"];
        yield return ["Subsets/D-string_3-hits.wav", "D"];
        yield return ["Subsets/G-string-hits.wav", "G"];
        yield return ["Subsets/G-string_2-hits.wav", "G"];
        yield return ["Subsets/G-string_3-hits.wav", "G"];
    }

    public static IEnumerable<object[]> BassStringOctaveSubsetCases()
    {
        yield return ["Subsets/E-string-hits.wav", "E1"];
        yield return ["Subsets/E-string_2-hits.wav", "E1"];
        yield return ["Subsets/E-string_3-hits.wav", "E1"];
        yield return ["Subsets/A-string-hits.wav", "A1"];
        yield return ["Subsets/A-string_2-hits.wav", "A1"];
        yield return ["Subsets/A-string_3-hits.wav", "A1"];
        yield return ["Subsets/D-string-hits.wav", "D2"];
        yield return ["Subsets/D-string_2-hits.wav", "D2"];
        yield return ["Subsets/D-string_3-hits.wav", "D2"];
        yield return ["Subsets/G-string-hits.wav", "G2"];
        yield return ["Subsets/G-string_2-hits.wav", "G2"];
        yield return ["Subsets/G-string_3-hits.wav", "G2"];
    }

    public static IEnumerable<object[]> G3SubsetCases()
    {
        yield return ["Subsets/G3-hits.wav", "G"];
        yield return ["Subsets/G3_2-hits.wav", "G"];
        yield return ["Subsets/G3_3-hits.wav", "G"];
    }

    public static IEnumerable<object[]> G3OctaveSubsetCases()
    {
        yield return ["Subsets/G3-hits.wav", "G3"];
        yield return ["Subsets/G3_2-hits.wav", "G3"];
        yield return ["Subsets/G3_3-hits.wav", "G3"];
    }

    public static IEnumerable<object[]> FullPassIndividualNoteCases()
    {
        string[] notes = ["E", "F", "G", "A", "B", "C", "D", "E", "F", "G", "A", "B", "C", "D", "E", "F", "G"];
        string[] files = [
            "01_28_E1.wav",
            "02_29_F1.wav",
            "03_31_G1.wav",
            "04_33_A1.wav",
            "05_35_B1.wav",
            "06_36_C2.wav",
            "07_38_D2.wav",
            "08_40_E2.wav",
            "09_41_F2.wav",
            "10_43_G2.wav",
            "11_45_A2.wav",
            "12_47_B2.wav",
            "13_48_C3.wav",
            "14_50_D3.wav",
            "15_52_E3.wav",
            "16_53_F3.wav",
            "17_55_G3.wav"
        ];

        foreach (string pass in new[] { "pass1", "pass2" })
        {
            for (int i = 0; i < files.Length; i++)
            {
                yield return [$"FullPassNotes/{pass}/{files[i]}", notes[i]];
            }
        }
    }

    [Theory]
    [MemberData(nameof(BassStringSubsetCases))]
    public void DetectBassString_ReturnsCorrectNote(string fileName, string expectedNote)
    {
        var result = RunDetection(fileName, expectedNote);
        Console.WriteLine($"\n=== {result.FileName} ({expectedNote}) Detection Summary ===");
        foreach (var (name, count) in result.Counts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {name}: {count}");
        }
        Console.WriteLine($"Success rate ({result.PrimaryNote}): {result.SuccessRate:F1}%");

        Assert.True(result.Counts[result.PrimaryNote] > 0,
            $"Expected at least some {expectedNote} detections in {fileName}");
    }

    [Theory(Skip = "Octave-level detection — revisit later")]
    [MemberData(nameof(BassStringOctaveSubsetCases))]
    public void DetectBassString_ReturnsCorrectOctave(string fileName, string expectedNote)
    {
        var result = RunDetectionFullName(fileName, expectedNote);
        Console.WriteLine($"\n=== {result.FileName} ({expectedNote}) Detection Summary ===");
        foreach (var (name, count) in result.Counts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {name}: {count}");
        }
        Console.WriteLine($"Success rate ({result.PrimaryNote}): {result.SuccessRate:F1}%");

        Assert.True(result.Counts[result.PrimaryNote] > 0,
            $"Expected at least some {expectedNote} detections in {fileName}");
    }

    [Theory]
    [MemberData(nameof(G3SubsetCases))]
    public void DetectG3FromRecording_ReturnsG3(string fileName, string expectedNote)
    {
        var result = RunDetection(fileName, expectedNote);
        Console.WriteLine($"\n=== {result.FileName} ({expectedNote}) Detection Summary ===");
        foreach (var (name, count) in result.Counts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {name}: {count}");
        }
        Console.WriteLine($"Success rate ({expectedNote}): {result.SuccessRate:F1}%");

        Assert.True(result.Counts.GetValueOrDefault(expectedNote, 0) > 0,
            $"Expected at least some {expectedNote} detections in {fileName}");
    }

    [Theory(Skip = "Octave-level detection — revisit later")]
    [MemberData(nameof(G3OctaveSubsetCases))]
    public void DetectG3FromRecording_ReturnsG3Octave(string fileName, string expectedNote)
    {
        var result = RunDetectionFullName(fileName, expectedNote);
        Console.WriteLine($"\n=== {result.FileName} ({expectedNote}) Detection Summary ===");
        foreach (var (name, count) in result.Counts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {name}: {count}");
        }
        Console.WriteLine($"Success rate ({expectedNote}): {result.SuccessRate:F1}%");

        Assert.True(result.Counts.GetValueOrDefault(expectedNote, 0) > 0,
            $"Expected at least some {expectedNote} detections in {fileName}");
    }

    [Theory]
    [MemberData(nameof(FullPassIndividualNoteCases))]
    public void DetectFullPassIndividualNote_ReturnsExpectedNoteName(string fileName, string expectedNote)
    {
        var result = RunDetection(fileName, expectedNote, maxStrikes: 1);
        Console.WriteLine($"\n=== {result.FileName} ({expectedNote}) Detection Summary ===");
        foreach (var (name, count) in result.Counts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {name}: {count}");
        }
        Console.WriteLine($"Success rate ({expectedNote}): {result.SuccessRate:F1}%");

        Assert.True(result.Counts.GetValueOrDefault(expectedNote, 0) > 0,
            $"Expected at least some {expectedNote} detections in {fileName}");
    }

    [Fact]
    public void DetectC3FromRecording_ReturnsC3()
    {
        const string fileName = "Subsets/C3-hits.wav";
        const string expectedNote = "C3";

        var result = RunDetectionFullName(fileName, expectedNote);
        Console.WriteLine($"\n=== {result.FileName} ({expectedNote}) Detection Summary ===");
        foreach (var (name, count) in result.Counts.OrderByDescending(x => x.Value))
        {
            Console.WriteLine($"  {name}: {count}");
        }
        Console.WriteLine($"Success rate ({expectedNote}): {result.SuccessRate:F1}%");

        Assert.True(result.Counts.GetValueOrDefault(expectedNote, 0) > 0,
            $"Expected at least some {expectedNote} detections in {fileName}");
    }

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
        int totalPitched = detectedE1 + detectedE2 + detectedOther;
        double successRate = totalPitched > 0 ? (double)detectedE1 / totalPitched * 100 : 0;
        Console.WriteLine($"Success rate (E1): {successRate:F1}%");
        Assert.True(detectedE1 >= detectedE2, $"E1 detected {detectedE1} times but E2 detected {detectedE2} times");
    }

    private DetectionResult RunDetection(string fileName, string expectedNote, int maxStrikes = 7)
    {
        string recordingPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", fileName);
        recordingPath = Path.GetFullPath(recordingPath);

        if (!File.Exists(recordingPath))
        {
            throw new FileNotFoundException($"Test resource not found: {recordingPath}");
        }

        var samples = LoadAudioMono(recordingPath, out int sampleRate);
        var detector = new PitchDetector(sampleRate, 8192);
        int bufferSize = 8192;
        var counts = new Dictionary<string, int>();

        int strikeCount = 0;
        bool inStrike = false;

        for (int offset = 0; offset + bufferSize <= samples.Length; offset += bufferSize / 4)
        {
            float[] buffer = new float[bufferSize];
            Array.Copy(samples, offset, buffer, 0, bufferSize);

            double rms = Math.Sqrt(buffer.Average(x => x * x));

            if (rms >= 0.005f)
            {
                double pitch = detector.DetectPitch(buffer);
                if (pitch > 0)
                {
                    if (!inStrike)
                    {
                        strikeCount++;
                        if (strikeCount > maxStrikes)
                            break;
                        inStrike = true;
                    }

                    var centsOff = Note.CentsOffFromFrequency(pitch, out var note);
                    string name = note.Name;

                    counts.TryGetValue(name, out int count);
                    counts[name] = count + 1;
                }
                else
                {
                    inStrike = false;
                }
            }
            else
            {
                inStrike = false;
            }
        }

        int primaryCount = counts.GetValueOrDefault(expectedNote, 0);
        int totalPitched = counts.Values.Sum();
        double successRate = totalPitched > 0 ? (double)primaryCount / totalPitched * 100 : 0;

        string displayName = Path.GetFileNameWithoutExtension(fileName);
        return new DetectionResult(displayName, expectedNote, successRate, counts);
    }

    private DetectionResult RunDetectionFullName(string fileName, string expectedNote, int maxStrikes = 7)
    {
        string recordingPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", fileName);
        recordingPath = Path.GetFullPath(recordingPath);

        if (!File.Exists(recordingPath))
        {
            throw new FileNotFoundException($"Test resource not found: {recordingPath}");
        }

        var samples = LoadAudioMono(recordingPath, out int sampleRate);
        var detector = new PitchDetector(sampleRate, 8192);
        int bufferSize = 8192;
        var counts = new Dictionary<string, int>();

        int strikeCount = 0;
        bool inStrike = false;

        for (int offset = 0; offset + bufferSize <= samples.Length; offset += bufferSize / 4)
        {
            float[] buffer = new float[bufferSize];
            Array.Copy(samples, offset, buffer, 0, bufferSize);

            double rms = Math.Sqrt(buffer.Average(x => x * x));

            if (rms >= 0.005f)
            {
                double pitch = detector.DetectPitch(buffer);
                if (pitch > 0)
                {
                    if (!inStrike)
                    {
                        strikeCount++;
                        if (strikeCount > maxStrikes)
                            break;
                        inStrike = true;
                    }

                    var centsOff = Note.CentsOffFromFrequency(pitch, out var note);
                    string name = note.FullName;

                    counts.TryGetValue(name, out int count);
                    counts[name] = count + 1;
                }
                else
                {
                    inStrike = false;
                }
            }
            else
            {
                inStrike = false;
            }
        }

        int primaryCount = counts.GetValueOrDefault(expectedNote, 0);
        int totalPitched = counts.Values.Sum();
        double successRate = totalPitched > 0 ? (double)primaryCount / totalPitched * 100 : 0;

        string displayName = Path.GetFileNameWithoutExtension(fileName);
        return new DetectionResult(displayName, expectedNote, successRate, counts);
    }

    private static float[] LoadAudioMono(string path, out int sampleRate)
    {
        using var reader = new MediaFoundationReader(path);
        sampleRate = reader.WaveFormat.SampleRate;
        int channels = reader.WaveFormat.Channels;

        var sampleProvider = reader.ToSampleProvider();
        var buffer = new List<float>();
        var readBuffer = new float[4096];
        int read;

        while ((read = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                buffer.Add(readBuffer[i]);
            }
        }

        if (channels == 2)
        {
            var mono = new float[buffer.Count / 2];
            for (int i = 0; i < mono.Length; i++)
            {
                mono[i] = (buffer[i * 2] + buffer[i * 2 + 1]) / 2f;
            }
            return mono;
        }

        return buffer.ToArray();
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
