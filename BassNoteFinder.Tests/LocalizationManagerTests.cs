using BassNoteFinder.Localization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace BassNoteFinder.Tests;

public class LocalizationManagerTests
{
    [Fact]
    public void GetString_WithoutApplicationResources_UsesEnglishFallback()
    {
        Assert.Equal("Start Mic", LocalizationManager.GetString("StartMic"));
    }

    [Fact]
    public void GetString_UnknownKey_ReturnsKeyForVisibleDiagnostics()
    {
        Assert.Equal("MissingKey", LocalizationManager.GetString("MissingKey"));
    }

    [Fact]
    public void Apply_WithoutWpfApplication_IsSafe()
    {
        LocalizationManager.Apply(AppLanguage.Greek);
    }

    [Theory]
    [InlineData("\"Greek\"", "Greek")]
    [InlineData("2", "Greek")]
    [InlineData("\"pack:nl\"", "pack:nl")]
    public void LanguagePreference_RoundTripsBuiltInAndPackIds(string json, string expected)
    {
        var config = JsonSerializer.Deserialize<AppConfig>($$"""{"Language":{{json}}}""")!;
        Assert.Equal(expected, config.Language);
        Assert.Equal(expected, JsonSerializer.Deserialize<AppConfig>(JsonSerializer.Serialize(config))!.Language);
    }

    [Fact]
    public void LanguagePicker_SwitchesBetweenBuiltInLanguagesAndPartialJsonPacks()
    {
        TestHelpers.RunOnSta(() =>
        {
            var application = new Application();
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/BassNoteFinder;component/Resources/Strings.en.xaml", UriKind.Relative)
            });

            string directory = Path.Combine(AppContext.BaseDirectory, "Languages");
            Directory.CreateDirectory(directory);
            string name = $"Test language {Guid.NewGuid():N}";
            string packPath = Path.Combine(directory, $"{name}.json");
            string invalidPath = Path.Combine(directory, $"{name}-invalid.json");
            try
            {
                File.WriteAllText(packPath, $$"""
                    {
                      "name": "{{name}}",
                      "strings": {
                        "AppTitle": "Basnoten",
                        "StartMic": "Start microfoon",
                        "StopMic": " ",
                        "CorrectStudentNext": "Incorrect placeholder {2}",
                        "AppBackgroundBrush": "Unrelated resource"
                      }
                    }
                    """);
                File.WriteAllText(invalidPath, "{ invalid json");

                LocalizationManager.Apply(AppLanguage.Greek);
                Assert.Equal("Έναρξη μικροφώνου", LocalizationManager.GetString("StartMic"));

                var window = new MainWindow(InitialViewMode.Menu, enableRuntimeServices: false);
                var picker = (ComboBox)window.FindName("LanguageCombo");
                picker.SelectedItem = picker.Items.Cast<object>().Single(item => item.ToString() == name);

                Assert.Equal(name, picker.SelectedItem?.ToString());
                Assert.Equal("Basnoten", window.Title);
                Assert.Equal("Start microfoon", LocalizationManager.GetString("StartMic"));
                Assert.Equal("Stop Mic", LocalizationManager.GetString("StopMic"));
                Assert.Equal("Staff", LocalizationManager.GetString("Staff"));
                Assert.Equal("Correct! You played {0}. Next note in {1}s...", LocalizationManager.GetString("CorrectStudentNext"));
                Assert.False(application.Resources.Contains("AppBackgroundBrush"));

                picker.SelectedItem = picker.Items.Cast<object>().Single(item => item.ToString() == "Ελληνικά");
                Assert.Equal("Έναρξη μικροφώνου", LocalizationManager.GetString("StartMic"));
                picker.SelectedItem = picker.Items.Cast<object>().Single(item => item.ToString() == "English");
                Assert.Equal("Start Mic", LocalizationManager.GetString("StartMic"));
                window.Close();
            }
            finally
            {
                application.Shutdown();
                File.Delete(packPath);
                File.Delete(invalidPath);
            }
        });
    }
}
