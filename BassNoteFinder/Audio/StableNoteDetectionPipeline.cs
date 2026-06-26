using BassNoteFinder.MusicTheory;

namespace BassNoteFinder.Audio;

public sealed class StableNoteDetectionPipeline
{
    private const int AttackIgnoreFrames = 1;
    private const int PitchMedianWindowSize = 3;
    private const int RequiredStableDetections = 2;
    private const int RequiredLostDetections = 3;
    private const double StableCentsDriftTolerance = 35.0;
    private const double HarmonicJumpThreshold = 1.35;

    private readonly Queue<double> _recentFrequencies = new();
    private int _attackIgnoreFramesRemaining;
    private int _candidateMidiNote = int.MinValue;
    private double _candidateCentsOff;
    private int _stableDetectionCount;
    private int _lostDetectionCount;
    private int? _lastResolvedMidiNote;
    private double? _lastStableFrequency;
    private bool _signalLost = true;

    public bool UseHarmonicCorrection { get; set; } = true;

    public event Action<Note, double>? StableNoteDetected;
    public event Action? StableNoteLost;

    public void ProcessFrequency(double frequency)
    {
        if (_signalLost)
        {
            _signalLost = false;
            _attackIgnoreFramesRemaining = AttackIgnoreFrames;
            ClearRecentFrequencies();
        }

        _lostDetectionCount = 0;

        if (_attackIgnoreFramesRemaining > 0)
        {
            _attackIgnoreFramesRemaining--;
            return;
        }

        AddRecentFrequency(frequency);
        double filteredFrequency = GetMedianFrequency();
        filteredFrequency = ApplyHarmonicCorrection(filteredFrequency);

        var centsOff = Note.CentsOffFromFrequency(filteredFrequency, out var detected);
        TrackCandidateDetection(detected, centsOff);

        bool isStable = _stableDetectionCount >= RequiredStableDetections;
        if (!isStable)
        {
            return;
        }

        if (_lastResolvedMidiNote == detected.MidiNote)
        {
            return;
        }

        _lastResolvedMidiNote = detected.MidiNote;
        _lastStableFrequency = filteredFrequency;
        StableNoteDetected?.Invoke(detected, centsOff);
    }

    public void ProcessLost()
    {
        _lostDetectionCount++;
        if (_lostDetectionCount < RequiredLostDetections)
        {
            return;
        }

        Reset();
        StableNoteLost?.Invoke();
    }

    public void Reset()
    {
        _signalLost = true;
        _attackIgnoreFramesRemaining = 0;
        _candidateMidiNote = int.MinValue;
        _candidateCentsOff = 0;
        _stableDetectionCount = 0;
        _lostDetectionCount = 0;
        _lastResolvedMidiNote = null;
        _lastStableFrequency = null;
        ClearRecentFrequencies();
    }

    private void TrackCandidateDetection(Note detected, double centsOff)
    {
        if (detected.MidiNote == _candidateMidiNote && Math.Abs(centsOff - _candidateCentsOff) <= StableCentsDriftTolerance)
        {
            _stableDetectionCount++;
        }
        else
        {
            _candidateMidiNote = detected.MidiNote;
            _candidateCentsOff = centsOff;
            _stableDetectionCount = 1;
        }
    }

    private void AddRecentFrequency(double frequency)
    {
        _recentFrequencies.Enqueue(frequency);
        while (_recentFrequencies.Count > PitchMedianWindowSize)
        {
            _recentFrequencies.Dequeue();
        }
    }

    private double GetMedianFrequency()
    {
        if (_recentFrequencies.Count == 0)
        {
            return 0;
        }

        var ordered = _recentFrequencies.OrderBy(x => x).ToArray();
        return ordered[ordered.Length / 2];
    }

    private double ApplyHarmonicCorrection(double frequency)
    {
        if (!UseHarmonicCorrection || _lastStableFrequency is null)
        {
            return frequency;
        }

        double halfFrequency = frequency / 2.0;
        if (halfFrequency < 30)
        {
            return frequency;
        }

        double upwardJumpRatio = frequency / _lastStableFrequency.Value;
        double fullJumpDistance = Math.Abs(frequency - _lastStableFrequency.Value);
        double halfJumpDistance = Math.Abs(halfFrequency - _lastStableFrequency.Value);

        if (upwardJumpRatio >= HarmonicJumpThreshold && halfJumpDistance < fullJumpDistance)
        {
            return halfFrequency;
        }

        return frequency;
    }

    private void ClearRecentFrequencies()
    {
        _recentFrequencies.Clear();
    }
}
