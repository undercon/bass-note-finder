namespace BassNoteFinder.Audio;

public class PitchDetector
{
    private const double MinClarity = 0.78;
    private const double PeakCutoffRatio = 0.90;
    private readonly int _sampleRate;
    private readonly int _minLag;
    private readonly int _maxLag;

    public PitchDetector(int sampleRate = 44100, int bufferSize = 8192)
    {
        _sampleRate = sampleRate;
        _minLag = Math.Max(8, sampleRate / 350);
        _maxLag = Math.Min(bufferSize / 2, sampleRate / 30);
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

        int selectedTau = peaks[^1].tau;
        double selectedValue = peaks[^1].value;

        foreach (var (tau, value) in peaks)
        {
            int doubleTau = tau * 2;
            if (doubleTau <= maxLag && nsdf[doubleTau] >= MinClarity)
            {
                bool isDoublePeak = false;
                for (int d = doubleTau - 2; d <= doubleTau + 2 && !isDoublePeak; d++)
                {
                    if (d > tau && d <= maxLag && nsdf[d] >= MinClarity)
                    {
                        bool isLocalMax = true;
                        for (int dd = Math.Max(_minLag, d - 1); dd <= Math.Min(maxLag, d + 1); dd++)
                        {
                            if (dd != d && nsdf[dd] > nsdf[d])
                            {
                                isLocalMax = false;
                                break;
                            }
                        }
                        if (isLocalMax) isDoublePeak = true;
                    }
                }

                if (isDoublePeak && tau > selectedTau)
                {
                    selectedTau = doubleTau;
                    selectedValue = nsdf[doubleTau];
                }
            }
        }

        return RefineTau(nsdf, selectedTau);
    }

    private static float[] PrepareSamples(float[] samples)
    {
        float[] prepared = new float[samples.Length];
        double mean = 0;

        for (int i = 0; i < samples.Length; i++)
        {
            mean += samples[i];
        }

        mean /= samples.Length;

        for (int i = 0; i < samples.Length; i++)
        {
            double window = 0.5 - 0.5 * Math.Cos(2.0 * Math.PI * i / (samples.Length - 1));
            prepared[i] = (float)((samples[i] - mean) * window);
        }

        return prepared;
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
}
