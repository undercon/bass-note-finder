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
        int bestTau = -1;
        double bestPeak = double.MinValue;

        for (int tau = _minLag + 1; tau < maxLag; tau++)
        {
            if (nsdf[tau] <= cutoff)
            {
                continue;
            }

            if (nsdf[tau] >= nsdf[tau - 1] && nsdf[tau] > nsdf[tau + 1])
            {
                bestTau = tau;
                bestPeak = nsdf[tau];
                break;
            }
        }

        if (bestTau < 0)
        {
            for (int tau = _minLag; tau <= maxLag; tau++)
            {
                if (nsdf[tau] > bestPeak)
                {
                    bestPeak = nsdf[tau];
                    bestTau = tau;
                }
            }
        }

        if (bestTau <= 0)
        {
            return -1;
        }

        return RefineTau(nsdf, bestTau);
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
