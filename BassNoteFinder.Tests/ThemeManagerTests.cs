using System.Windows.Controls;
using System.Windows.Media;
using BassNoteFinder;
using Xunit;

namespace BassNoteFinder.Tests;

public class ThemeManagerTests
{
    [Fact]
    public void ApplyResources_LightThemeUpdatesSharedPaletteBrushes()
    {
        var resources = new System.Windows.ResourceDictionary
        {
            ["AppBackgroundBrush"] = new SolidColorBrush(Colors.Black),
            ["PanelHeaderBrush"] = new SolidColorBrush(Colors.White)
        };

        ThemeManager.ApplyResources(resources, AppTheme.Light);

        Assert.Equal(Color.FromRgb(0xEE, 0xF3, 0xF6), ((SolidColorBrush)resources["AppBackgroundBrush"]).Color);
        Assert.Equal(Color.FromRgb(0x17, 0x24, 0x32), ((SolidColorBrush)resources["PanelHeaderBrush"]).Color);
    }

    [Fact]
    public void ApplyResources_DarkThemeRestoresSharedPaletteBrushes()
    {
        var resources = new System.Windows.ResourceDictionary
        {
            ["AppBackgroundBrush"] = new SolidColorBrush(Colors.White)
        };

        ThemeManager.ApplyResources(resources, AppTheme.Dark);

        Assert.Equal(Color.FromRgb(0x0B, 0x11, 0x18), ((SolidColorBrush)resources["AppBackgroundBrush"]).Color);
    }

    [Fact]
    public void MainWindow_ThemePickerOffersAllChoicesAndAppliesLightTheme()
    {
        (AppTheme[] choices, Color background) = TestHelpers.RunOnSta(() =>
        {
            var window = new MainWindow(InitialViewMode.Menu, enableRuntimeServices: false);
            var themeCombo = (ComboBox)window.FindName("ThemeCombo");
            AppTheme[] items = themeCombo.Items.Cast<AppTheme>().ToArray();

            themeCombo.SelectedItem = AppTheme.Light;
            Color color = ((SolidColorBrush)window.Background).Color;
            window.Close();
            return (items, color);
        });

        Assert.Equal(new[] { AppTheme.System, AppTheme.Dark, AppTheme.Light }, choices);
        Assert.Equal(Color.FromRgb(0xEE, 0xF3, 0xF6), background);
    }
}
