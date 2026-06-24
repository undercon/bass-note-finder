namespace BassNoteFinder;

public class AppConfig
{
    public float MinSignalLevel { get; set; } = 0.01f;
    public string SelectedInputDevice { get; set; } = string.Empty;
    public bool UseHarmonicCorrection { get; set; } = true;
    public double WindowWidth { get; set; } = 1050;
    public double WindowHeight { get; set; } = 750;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
}
