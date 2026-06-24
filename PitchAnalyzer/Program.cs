using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using NAudio.Wave;

if (args.Length == 0)
{
    Console.WriteLine("Usage: PitchAnalyzer <audio-file>");
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
