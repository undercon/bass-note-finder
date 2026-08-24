using BassNoteFinder.Localization;
using System.Windows;
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

    [Fact]
    public void Apply_WithApplicationResources_SwitchesBetweenGreekAndEnglish()
    {
        TestHelpers.RunOnSta(() =>
        {
            var application = new Application();
            application.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/BassNoteFinder;component/Resources/Strings.en.xaml", UriKind.Relative)
            });

            try
            {
                LocalizationManager.Apply(AppLanguage.Greek);
                Assert.Equal("Έναρξη μικροφώνου", LocalizationManager.GetString("StartMic"));

                LocalizationManager.Apply(AppLanguage.English);
                Assert.Equal("Start Mic", LocalizationManager.GetString("StartMic"));
            }
            finally
            {
                application.Shutdown();
            }
        });
    }
}
