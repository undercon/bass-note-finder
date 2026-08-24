using System.Windows;

namespace BassNoteFinder.Localization;

public enum AppLanguage
{
    System,
    English,
    Greek
}

public static class LocalizationManager
{
    private const string MarkerKey = "LocalizationDictionaryMarker";
    private static readonly Lazy<ResourceDictionary> EnglishFallback = new(() => new ResourceDictionary
    {
        Source = new Uri("/BassNoteFinder;component/Resources/Strings.en.xaml", UriKind.Relative)
    });

    public static void Apply(AppLanguage language)
    {
        string culture = language switch
        {
            AppLanguage.Greek => "el",
            AppLanguage.English => "en",
            _ => System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "el" ? "el" : "en"
        };

        if (Application.Current is not { } application)
        {
            return;
        }

        var resources = application.Resources;
        ResourceDictionary? current = resources.MergedDictionaries
            .FirstOrDefault(dictionary => dictionary.Contains(MarkerKey));
        if (current != null)
        {
            resources.MergedDictionaries.Remove(current);
        }

        if (culture == "en")
        {
            return;
        }

        resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri($"/BassNoteFinder;component/Resources/Strings.{culture}.xaml", UriKind.Relative)
        });
    }

    public static string GetString(string key)
    {
        if (Application.Current?.Resources[key] is string localized)
        {
            return localized;
        }

        return EnglishFallback.Value[key] as string ?? key;
    }
}
