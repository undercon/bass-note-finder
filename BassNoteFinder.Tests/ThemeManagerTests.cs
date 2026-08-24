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
        (string[] choices, Color background) = TestHelpers.RunOnSta(() =>
        {
            var window = new MainWindow(InitialViewMode.Menu, enableRuntimeServices: false);
            var themeCombo = (ComboBox)window.FindName("ThemeCombo");
            string[] items = themeCombo.Items.Cast<object>().Select(item => item.ToString()!).ToArray();

            themeCombo.SelectedItem = themeCombo.Items.Cast<object>().Single(item => item.ToString() == "Light");
            Color color = ((SolidColorBrush)window.Background).Color;
            window.Close();
            return (items, color);
        });

        Assert.Equal(new[] { "System", "Dark", "Light" }, choices);
        Assert.Equal(Color.FromRgb(0xEE, 0xF3, 0xF6), background);
    }

    [Fact]
    public void MainWindow_NotationPickerOffersStandardAndSolfege()
    {
        var choices = TestHelpers.RunOnSta(() =>
        {
            var window = new MainWindow(InitialViewMode.Menu, enableRuntimeServices: false);
            var notationCombo = (ComboBox)window.FindName("NotationCombo");
            var items = notationCombo.Items.Cast<object>().Select(item => item.ToString()).ToArray();
            window.Close();
            return items;
        });

        Assert.Equal(
            new[] { "Standard", "Solfège" },
            choices);
    }

    [Fact]
    public void MainWindow_LanguagePickerOffersGreekAndKeepsTheSelectedLanguage()
    {
        string[] choices = TestHelpers.RunOnSta(() =>
        {
            var window = new MainWindow(InitialViewMode.Menu, enableRuntimeServices: false);
            var languageCombo = (ComboBox)window.FindName("LanguageCombo");
            var items = languageCombo.Items.Cast<object>().ToArray();

            languageCombo.SelectedItem = items.Single(item => item.ToString() == "Ελληνικά");
            string[] names = items.Select(item => item.ToString()!).ToArray();

            Assert.Equal("Ελληνικά", languageCombo.SelectedItem?.ToString());
            window.Close();
            return names;
        });

        Assert.Equal(new[] { "System", "English", "Ελληνικά" }, choices);
    }
}
