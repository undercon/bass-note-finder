using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using NAudio.Wave;

if (args.Length == 0)
{
    Console.WriteLine("Usage: PitchAnalyzer <audio-file>");
    Console.WriteLine("       PitchAnalyzer --full-pass-metrics [FullPassNotes-directory]");
    return;
}

if (args[0] == "--full-pass-metrics")
{
    string root = args.Length > 1 ? args[1] : Path.Combine("BassNoteFinder.Tests", "Resources", "FullPassNotes");
    root = Path.GetFullPath(root);

    if (!Directory.Exists(root))
    {
        Console.WriteLine($"Directory not found: {root}");
        return;
    }

    Console.WriteLine("pass,file,expected,total_frames,pitched_frames,correct_frames,accuracy,primary,primary_frames,avg_cents_off");

    foreach (string file in Directory.GetFiles(root, "*.wav", SearchOption.AllDirectories).OrderBy(Path.GetDirectoryName).ThenBy(Path.GetFileName))
    {
        string pass = Path.GetFileName(Path.GetDirectoryName(file)) ?? "unknown";
        string expected = GetExpectedNote(file);
        var result = AnalyzeFile(file, expected);

        Console.WriteLine(string.Join(',',
            pass,
            Path.GetFileName(file),
            expected,
            result.TotalFrames,
            result.PitchedFrames,
            result.CorrectFrames,
            result.Accuracy.ToString("F1"),
            result.PrimaryNote,
            result.PrimaryFrames,
            result.AverageCentsOff.ToString("F1")));
    }

    return;
}

string path = args[0];
if (!File.Exists(path))
{
    Console.WriteLine($"File not found: {path}");
    return;
}

var samples = LoadAudioMono(path, out int sampleRate);
Console.WriteLine($"Loaded {samples.Length} samples at {sampleRate} Hz ({samples.Length / (double)sampleRate:F2}s)");

var detector = new PitchDetector(sampleRate, 8192);
int bufferSize = 8192;
int hopSize = bufferSize / 4;

var counts = new Dictionary<string, int>();

for (int offset = 0; offset + bufferSize <= samples.Length; offset += hopSize)
{
    float[] buffer = new float[bufferSize];
    Array.Copy(samples, offset, buffer, 0, bufferSize);

    double rms = Math.Sqrt(buffer.Average(x => x * x));
    if (rms < 0.005f)
    {
        continue;
    }

    double pitch = detector.DetectPitch(buffer);
    if (pitch <= 0)
    {
        Console.WriteLine($"Offset {offset / (double)sampleRate,6:F2}s: NO PITCH (rms={rms:F4})");
        continue;
    }

    var centsOff = Note.CentsOffFromFrequency(pitch, out var note);
    string name = note.FullName;

    counts.TryGetValue(name, out int count);
    counts[name] = count + 1;

    Console.WriteLine($"Offset {offset / (double)sampleRate,6:F2}s: {pitch,8:F2} Hz -> {name} ({centsOff:F0}c)  rms={rms:F4}");
}

Console.WriteLine("\n=== Summary ===");
foreach (var (name, count) in counts.OrderByDescending(x => x.Value))
{
    Console.WriteLine($"  {name}: {count}");
}

static DetectionMetrics AnalyzeFile(string path, string expectedNote)
{
    var samples = LoadAudioMono(path, out int sampleRate);
    var detector = new PitchDetector(sampleRate, 8192);
    int bufferSize = 8192;
    int hopSize = bufferSize / 4;
    int totalFrames = 0;
    int pitchedFrames = 0;
    int correctFrames = 0;
    double centsTotal = 0;
    var counts = new Dictionary<string, int>();

    for (int offset = 0; offset + bufferSize <= samples.Length; offset += hopSize)
    {
        float[] buffer = new float[bufferSize];
        Array.Copy(samples, offset, buffer, 0, bufferSize);

        double rms = Math.Sqrt(buffer.Average(x => x * x));
        if (rms < 0.005f)
        {
            continue;
        }

        totalFrames++;
        double pitch = detector.DetectPitch(buffer);
        if (pitch <= 0)
        {
            continue;
        }

        pitchedFrames++;
        double centsOff = Note.CentsOffFromFrequency(pitch, out var note);
        string name = note.FullName;

        counts.TryGetValue(name, out int count);
        counts[name] = count + 1;

        if (name == expectedNote)
        {
            correctFrames++;
            centsTotal += Math.Abs(centsOff);
        }
    }

    var primary = counts.Count == 0 ? ("", 0) : counts.OrderByDescending(x => x.Value).Select(x => (x.Key, x.Value)).First();
    double accuracy = pitchedFrames == 0 ? 0 : correctFrames / (double)pitchedFrames * 100.0;
    double averageCentsOff = correctFrames == 0 ? 0 : centsTotal / correctFrames;

    return new DetectionMetrics(totalFrames, pitchedFrames, correctFrames, accuracy, primary.Item1, primary.Item2, averageCentsOff);
}

static string GetExpectedNote(string path)
{
    string name = Path.GetFileNameWithoutExtension(path);
    string[] parts = name.Split('_');
    return parts.Length >= 3 ? parts[2] : name;
}

static float[] LoadAudioMono(string path, out int sampleRate)
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

internal readonly record struct DetectionMetrics(
    int TotalFrames,
    int PitchedFrames,
    int CorrectFrames,
    double Accuracy,
    string PrimaryNote,
    int PrimaryFrames,
    double AverageCentsOff);
