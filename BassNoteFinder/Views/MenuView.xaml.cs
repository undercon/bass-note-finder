using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace BassNoteFinder.Views;

public partial class MenuView : UserControl
{
    public event Action? TeacherModeSelected;
    public event Action? StudentModeSelected;

    public MenuView()
    {
        InitializeComponent();
        VersionText.Text = $"v{GetDisplayVersion()}";
    }

    private void TeacherModeButton_Click(object sender, RoutedEventArgs e)
    {
        TeacherModeSelected?.Invoke();
    }

    private void StudentModeButton_Click(object sender, RoutedEventArgs e)
    {
        StudentModeSelected?.Invoke();
    }

    private static string GetDisplayVersion()
    {
        var assembly = typeof(MenuView).Assembly;
        var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(infoVersion))
        {
            int plusIndex = infoVersion.IndexOf('+');
            return plusIndex > 0 ? infoVersion[..plusIndex] : infoVersion;
        }

        return assembly.GetName().Version?.ToString() ?? "0.0.0";
    }
}
