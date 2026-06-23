namespace BassNoteFinder;

public class AppConfig
{
    public float MinSignalLevel { get; set; } = 0.01f;
    public string SelectedInputDevice { get; set; } = string.Empty;
}
