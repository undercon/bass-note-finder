using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace BassNoteFinder.Localization;

public static class AppLanguage
{
    public const string System = "System";
    public const string English = "English";
    public const string Greek = "Greek";
}

internal sealed class LanguagePreferenceConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out int legacyValue))
        {
            return legacyValue switch
            {
                1 => AppLanguage.English,
                2 => AppLanguage.Greek,
                _ => AppLanguage.System
            };
        }

        return reader.GetString() ?? AppLanguage.System;
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value);
}

public static class LocalizationManager
{
    private const string MarkerKey = "LocalizationDictionaryMarker";
    internal static string PacksDirectory => Path.Combine(AppContext.BaseDirectory, "Languages");

    internal sealed record LanguagePack(string Id, string Name, IReadOnlyDictionary<string, string?> Strings);
    private sealed record PackFile(string? Name, Dictionary<string, string?>? Strings);

    private static readonly Lazy<ResourceDictionary> EnglishFallback = new(() => new ResourceDictionary
    {
        Source = new Uri("/BassNoteFinder;component/Resources/Strings.en.xaml", UriKind.Relative)
    });

    internal static IReadOnlyList<LanguagePack> LoadPacks()
    {
        string[] paths;
        try
        {
            paths = Directory.GetFiles(PacksDirectory, "*.json");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }

        var packs = new List<LanguagePack>();
        foreach (string path in paths)
        {
            try
            {
                var file = JsonSerializer.Deserialize<PackFile>(File.ReadAllText(path), JsonSerializerOptions.Web);
                if (file?.Strings != null && !string.IsNullOrWhiteSpace(file.Name))
                {
                    packs.Add(new LanguagePack($"pack:{Path.GetFileNameWithoutExtension(path)}", file.Name.Trim(), file.Strings));
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                continue;
            }
        }

        return packs.OrderBy(pack => pack.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    public static void Apply(string language) => Apply(language, pack: null);

    internal static void Apply(string language, LanguagePack? pack)
    {
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

        if (pack != null)
        {
            resources.MergedDictionaries.Add(CreatePackResources(pack));
        }
        else if (language == AppLanguage.Greek ||
                 (language == AppLanguage.System && System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "el"))
        {
            resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/BassNoteFinder;component/Resources/Strings.el.xaml", UriKind.Relative)
            });
        }
    }

    private static ResourceDictionary CreatePackResources(LanguagePack pack)
    {
        var resources = new ResourceDictionary { [MarkerKey] = true };
        foreach (var (key, translation) in pack.Strings)
        {
            if (!string.IsNullOrWhiteSpace(translation) &&
                EnglishFallback.Value[key] is string english &&
                HasCompatiblePlaceholders(english, translation))
            {
                resources[key] = translation;
            }
        }
        return resources;
    }

    private static bool HasCompatiblePlaceholders(string english, string translation)
    {
        if (!english.Contains('{'))
        {
            return true;
        }

        try
        {
            return CompositeFormat.Parse(translation).MinimumArgumentCount ==
                   CompositeFormat.Parse(english).MinimumArgumentCount;
        }
        catch (FormatException)
        {
            return false;
        }
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
