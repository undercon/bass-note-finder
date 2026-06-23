namespace BassNoteFinder.Audio;

public class PitchDetector
{
    private const double MinConfidence = 0.70;
    private readonly int _sampleRate;
    private readonly int _bufferSize;

    public PitchDetector(int sampleRate = 44100, int bufferSize = 4096)
    {
        _sampleRate = sampleRate;
        _bufferSize = bufferSize;
    }

    public double DetectPitch(float[] samples)
    {
        var period = Yin(samples);
        if (period <= 0) return -1;

        var confidence = YinConfidence(samples, period);
        if (confidence < MinConfidence) return -1;

        return _sampleRate / period;
    }

    private double Yin(float[] samples)
    {
        int length = samples.Length;
        var diff = new double[length / 2];
        double tau = -1;

        for (int t = 0; t < diff.Length; t++)
        {
            diff[t] = 0;
            for (int i = 0; i < diff.Length; i++)
            {
                double delta = samples[i] - samples[i + t];
                diff[t] += delta * delta;
            }
        }

        double runningSum = 0;
        for (int t = 1; t < diff.Length; t++)
        {
            runningSum += diff[t];
            diff[t] = diff[t] * t / runningSum;

            if (t > 20 && diff[t] < 0.20)
            {
                double betterTau = t - 1 + (diff[t - 1] - 0.20) / (diff[t - 1] - diff[t]);
                tau = betterTau;
                break;
            }
        }

        if (tau < 0 && diff.Length > 0)
        {
            tau = 1;
            for (int t = 2; t < diff.Length; t++)
            {
                if (diff[t] < diff[(int)tau])
                    tau = t;
            }
        }

        if (tau <= 0) return -1;

        return tau;
    }

    private static double YinConfidence(float[] samples, double period)
    {
        int halfLen = samples.Length / 2;
        int periodInt = (int)Math.Round(period);
        if (periodInt <= 0 || periodInt >= halfLen) return 0;

        double numer = 0, denom = 0;
        for (int i = 0; i < halfLen; i++)
        {
            double d = samples[i] - samples[i + periodInt];
            numer += d * d;
            denom += samples[i] * samples[i] + samples[i + periodInt] * samples[i + periodInt];
        }

        if (denom <= 0) return 0;
        return 1.0 - numer / denom;
    }
}
