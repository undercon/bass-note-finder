using NAudio.Wave;

namespace BassNoteFinder.Audio;

public class AudioCaptureService : IDisposable
{
    private float _minSignalLevel = 0.02f;
    private WaveInEvent? _waveIn;
    private readonly PitchDetector _pitchDetector;
    private float[]? _buffer;
    private int _bufferPos;

    public int SampleRate { get; }
    public int BufferSize { get; }
    public bool IsCapturing => _isCapturing;
    public float MinSignalLevel
    {
        get => _minSignalLevel;
        set => _minSignalLevel = Math.Clamp(value, 0f, 0.25f);
    }

    private bool _isCapturing;

    public event Action<double>? PitchDetected;
    public event Action<string>? ErrorOccurred;

    public AudioCaptureService(int sampleRate = 44100, int bufferSize = 4096)
    {
        SampleRate = sampleRate;
        BufferSize = bufferSize;
        _pitchDetector = new PitchDetector(sampleRate, bufferSize);
    }

    public static IReadOnlyList<string> GetInputDevices()
    {
        var devices = new List<string>();
        for (int i = 0; i < WaveInEvent.DeviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            devices.Add(caps.ProductName);
        }
        return devices;
    }

    public bool StartCapture(int deviceIndex = 0)
    {
        StopCapture();

        int deviceCount = WaveInEvent.DeviceCount;
        if (deviceCount <= 0)
        {
            ErrorOccurred?.Invoke("No audio input devices found.");
            return false;
        }

        if (deviceIndex < 0 || deviceIndex >= deviceCount)
        {
            ErrorOccurred?.Invoke("Select a valid audio input device.");
            return false;
        }

        try
        {
            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceIndex,
                WaveFormat = new WaveFormat(SampleRate, 16, 1),
                BufferMilliseconds = 50
            };

            _buffer = new float[BufferSize];
            _bufferPos = 0;

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;
            _waveIn.StartRecording();
            _isCapturing = true;
            return true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Failed to start capture: {ex.Message}");
            _waveIn?.Dispose();
            _waveIn = null;
            _isCapturing = false;
            return false;
        }
    }

    public void StopCapture()
    {
        _isCapturing = false;
        if (_waveIn != null)
        {
            try { _waveIn.StopRecording(); } catch { }
            _waveIn.Dispose();
            _waveIn = null;
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_buffer == null) return;

        int samples = e.BytesRecorded / 2;

        for (int i = 0; i < samples; i++)
        {
            short sample = (short)(e.Buffer[i * 2] | (e.Buffer[i * 2 + 1] << 8));
            _buffer[_bufferPos++] = sample / 32768f;

            if (_bufferPos >= _buffer.Length)
            {
                _bufferPos = 0;
                ProcessBuffer();
            }
        }
    }

    private void ProcessBuffer()
    {
        if (_buffer == null) return;

        try
        {
            float[] copy = new float[_buffer.Length];
            Buffer.BlockCopy(_buffer, 0, copy, 0, _buffer.Length * 4);

            if (GetRootMeanSquare(copy) < MinSignalLevel)
            {
                return;
            }

            double pitch = _pitchDetector.DetectPitch(copy);
            if (pitch > 0)
            {
                PitchDetected?.Invoke(pitch);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Pitch detection failed: {ex.Message}");
        }
    }

    private static float GetRootMeanSquare(float[] samples)
    {
        double sum = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i];
        }

        return (float)Math.Sqrt(sum / samples.Length);
    }

    private static void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
    }

    public void Dispose()
    {
        StopCapture();
        GC.SuppressFinalize(this);
    }
}
