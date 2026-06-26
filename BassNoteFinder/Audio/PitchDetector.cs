namespace BassNoteFinder.Audio;

public class PitchDetector
{
    private const double MinClarity = 0.78;
    private const double PeakCutoffRatio = 0.90;
    private const double HarmonicFreqThreshold = 220.0;
    private const double BassFundamentalCeiling = 115.0;
    private const double BassFundamentalFloor = 35.0;
    private const double OctaveRatioTolerance = 0.15;
    private const double LowBassStrengthRatio = 0.96;
    private const double InputEqCrossover = 105.0;
    private const double InputEqLowGain = 0.75;
    private const double InputEqHighGain = 2.35;
    private readonly int _sampleRate;
    private readonly int _minLag;
    private readonly int _maxLag;
    public bool PreferHigherOctave { get; set; } = true;

    public PitchDetector(int sampleRate = 44100, int bufferSize = 8192)
    {
        _sampleRate = sampleRate;
        _minLag = Math.Max(8, (int)(sampleRate / 210.0));
        _maxLag = Math.Min(bufferSize / 2, (int)(sampleRate / 40.0));
    }

    public double DetectPitch(float[] samples)
    {
        if (samples.Length < 4)
        {
            return -1;
        }

        float[] prepared = PrepareSamples(samples);
        var period = FindPeriod(prepared);
        if (period <= 0) return -1;

        return _sampleRate / period;
    }

    private double FindPeriod(float[] samples)
    {
        int maxLag = Math.Min(_maxLag, samples.Length / 2);
        if (maxLag <= _minLag)
        {
            return -1;
        }

        var nsdf = new double[maxLag + 1];
        double strongestPeak = 0;

        for (int tau = _minLag; tau <= maxLag; tau++)
        {
            double acf = 0;
            double divisor = 0;
            int limit = samples.Length - tau;

            for (int i = 0; i < limit; i++)
            {
                double a = samples[i];
                double b = samples[i + tau];
                acf += a * b;
                divisor += a * a + b * b;
            }

            if (divisor <= 0)
            {
                continue;
            }

            double value = 2.0 * acf / divisor;
            nsdf[tau] = value;
            if (value > strongestPeak)
            {
                strongestPeak = value;
            }
        }

        if (strongestPeak < MinClarity)
        {
            return -1;
        }

        double cutoff = Math.Max(MinClarity, strongestPeak * PeakCutoffRatio);

        var peaks = new List<(int tau, double value)>();
        for (int tau = _minLag + 1; tau < maxLag; tau++)
        {
            if (nsdf[tau] <= cutoff)
            {
                continue;
            }

            if (nsdf[tau] >= nsdf[tau - 1] && nsdf[tau] > nsdf[tau + 1])
            {
                peaks.Add((tau, nsdf[tau]));
            }
        }

        if (peaks.Count == 0)
        {
            int bestTau = _minLag;
            double bestPeak = nsdf[_minLag];
            for (int tau = _minLag + 1; tau <= maxLag; tau++)
            {
                if (nsdf[tau] > bestPeak)
                {
                    bestPeak = nsdf[tau];
                    bestTau = tau;
                }
            }
            return RefineTau(nsdf, bestTau);
        }

        var strongest = peaks.Aggregate((a, b) => a.value > b.value ? a : b);
        int selectedTau = strongest.tau;
        double selectedFreq = (double)_sampleRate / selectedTau;

        var lowerOctavePeak = FindOctavePeak(peaks, selectedTau, octaveRatio: 2.0);
        if (lowerOctavePeak is { } lowerCandidate)
        {
            double lowerFreq = (double)_sampleRate / lowerCandidate.tau;
            bool likelyBassHarmonic =
                selectedFreq >= BassFundamentalFloor * 2.0 &&
                selectedFreq <= BassFundamentalCeiling &&
                lowerFreq >= BassFundamentalFloor;

            if (likelyBassHarmonic && lowerCandidate.value >= strongest.value * LowBassStrengthRatio)
            {
                selectedTau = lowerCandidate.tau;
                selectedFreq = lowerFreq;
            }
        }

        if (PreferHigherOctave && selectedFreq < HarmonicFreqThreshold)
        {
            foreach (var (tau, value) in peaks)
            {
                double ratio = (double)selectedTau / tau;
                if (Math.Abs(ratio - 2.0) < OctaveRatioTolerance && tau < selectedTau)
                {
                    double candidateFreq = (double)_sampleRate / tau;
                    if (candidateFreq > HarmonicFreqThreshold && value >= strongest.value * 0.98)
                    {
                        selectedTau = tau;
                        selectedFreq = candidateFreq;
                        break;
                    }
                }
            }
        }

        return RefineTau(nsdf, selectedTau);
    }

    private float[] PrepareSamples(float[] samples)
    {
        float[] equalized = ApplyInputEq(samples);
        float[] prepared = new float[samples.Length];
        double mean = 0;

        for (int i = 0; i < equalized.Length; i++)
        {
            mean += equalized[i];
        }

        mean /= equalized.Length;

        for (int i = 0; i < equalized.Length; i++)
        {
            double window = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (equalized.Length - 1));
            prepared[i] = (float)((equalized[i] - mean) * window);
        }

        return prepared;
    }

    private float[] ApplyInputEq(float[] samples)
    {
        float[] equalized = new float[samples.Length];
        double rc = 1.0 / (2.0 * Math.PI * InputEqCrossover);
        double dt = 1.0 / _sampleRate;
        double alpha = dt / (rc + dt);
        double low = samples[0];

        for (int i = 0; i < samples.Length; i++)
        {
            double sample = samples[i];
            low += alpha * (sample - low);
            double high = sample - low;
            equalized[i] = (float)(low * InputEqLowGain + high * InputEqHighGain);
        }

        return equalized;
    }

    private static double RefineTau(double[] curve, int tauIndex)
    {
        if (tauIndex <= 0 || tauIndex >= curve.Length - 1)
        {
            return tauIndex;
        }

        double left = curve[tauIndex - 1];
        double center = curve[tauIndex];
        double right = curve[tauIndex + 1];
        double denominator = left - 2 * center + right;

        if (Math.Abs(denominator) < 1e-9)
        {
            return tauIndex;
        }

        double offset = 0.5 * (left - right) / denominator;
        return tauIndex + Math.Clamp(offset, -0.5, 0.5);
    }

    private static (int tau, double value)? FindOctavePeak(List<(int tau, double value)> peaks, int referenceTau, double octaveRatio)
    {
        (int tau, double value)? best = null;
        double smallestError = double.MaxValue;

        foreach (var peak in peaks)
        {
            double ratio = (double)peak.tau / referenceTau;
            double error = Math.Abs(ratio - octaveRatio);
            if (error > OctaveRatioTolerance)
            {
                continue;
            }

            if (error < smallestError)
            {
                smallestError = error;
                best = peak;
            }
        }

        return best;
    }
}
