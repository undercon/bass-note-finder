using System.Windows;
using System.Windows.Media;

namespace BassNoteFinder;

public enum AppTheme
{
    System,
    Dark,
    Light
}

public static class ThemeManager
{
    private static readonly IReadOnlyDictionary<string, Color> DarkPalette = CreateDarkPalette();
    private static readonly IReadOnlyDictionary<string, Color> LightPalette = CreateLightPalette();

    public static AppTheme Resolve(AppTheme selection)
    {
        if (selection != AppTheme.System)
        {
            return selection;
        }

        Color systemBackground = SystemColors.WindowColor;
        double luminance = ((0.2126 * systemBackground.R) +
                            (0.7152 * systemBackground.G) +
                            (0.0722 * systemBackground.B)) / 255;
        return luminance >= 0.5 ? AppTheme.Light : AppTheme.Dark;
    }

    public static AppTheme Apply(AppTheme selection, params FrameworkElement[] roots)
    {
        AppTheme resolved = Resolve(selection);
        if (Application.Current != null)
        {
            ApplyResources(Application.Current.Resources, resolved);
        }

        foreach (FrameworkElement root in roots)
        {
            ApplyResources(root.Resources, resolved);
        }

        return resolved;
    }

    public static void ApplyResources(ResourceDictionary resources, AppTheme theme)
    {
        IReadOnlyDictionary<string, Color> palette = Resolve(theme) == AppTheme.Light
            ? LightPalette
            : DarkPalette;

        foreach (ResourceDictionary mergedDictionary in resources.MergedDictionaries)
        {
            ApplyResources(mergedDictionary, theme);
        }

        foreach ((string resourceKey, Color color) in palette)
        {
            if (resources.Contains(resourceKey))
            {
                resources[resourceKey] = new SolidColorBrush(color);
            }
        }
    }

    private static IReadOnlyDictionary<string, Color> CreateDarkPalette()
    {
        return new Dictionary<string, Color>
        {
            ["AppBackgroundBrush"] = Parse("#0B1118"),
            ["PanelBackgroundBrush"] = Parse("#141E29"),
            ["PanelBorderBrush"] = Parse("#2B3A4A"),
            ["PanelHeaderBrush"] = Parse("#F2F6F8"),
            ["MutedTextBrush"] = Parse("#AAB8C5"),
            ["SubtleTextBrush"] = Parse("#718397"),
            ["PrimaryBrush"] = Parse("#159B9A"),
            ["SuccessBrush"] = Parse("#D99A36"),
            ["SecondaryBrush"] = Parse("#334659"),
            ["FocusBorderBrush"] = Parse("#65E0D5"),
            ["AccentBrush"] = Parse("#65E0D5"),
            ["ModeHeaderBorderBrush"] = Parse("#35566A"),
            ["StatusPillBackgroundBrush"] = Parse("#101923"),
            ["CheckBoxGlyphBrush"] = Parse("#172432"),
            ["CheckBoxBorderBrush"] = Parse("#52657A"),
            ["CheckBoxHoverBrush"] = Parse("#203344"),
            ["CheckMarkBrush"] = Parse("#071114"),
            ["ButtonHoverBorderBrush"] = Parse("#8DF5EC"),
            ["ButtonTextBrush"] = Parse("#FFFFFF"),
            ["TeacherCardBackgroundBrush"] = Parse("#172532"),
            ["TeacherCardBorderBrush"] = Parse("#38667A"),
            ["StudentCardBackgroundBrush"] = Parse("#1C2630"),
            ["StudentCardBorderBrush"] = Parse("#5A5138"),
            ["WarmAccentBrush"] = Parse("#F0BE67"),
            ["CorrectBrush"] = Parse("#32CD32"),
            ["ErrorBrush"] = Parse("#FF4500"),
            ["DarkComboForegroundBrush"] = Parse("#FFFFFF"),
            ["DarkComboBackgroundBrush"] = Parse("#333333"),
            ["DarkComboPopupBrush"] = Parse("#2A2A2A"),
            ["DarkComboBorderBrush"] = Parse("#555555"),
            ["DarkComboHoverBrush"] = Parse("#3F3F46"),
            ["DarkComboPressedBrush"] = Parse("#0E639C"),
            ["DarkComboSelectedBrush"] = Parse("#0E639C")
        };
    }

    private static IReadOnlyDictionary<string, Color> CreateLightPalette()
    {
        return new Dictionary<string, Color>
        {
            ["AppBackgroundBrush"] = Parse("#EEF3F6"),
            ["PanelBackgroundBrush"] = Parse("#FFFFFF"),
            ["PanelBorderBrush"] = Parse("#CBD7E1"),
            ["PanelHeaderBrush"] = Parse("#172432"),
            ["MutedTextBrush"] = Parse("#53677A"),
            ["SubtleTextBrush"] = Parse("#77899A"),
            ["PrimaryBrush"] = Parse("#087F80"),
            ["SuccessBrush"] = Parse("#B97812"),
            ["SecondaryBrush"] = Parse("#526C83"),
            ["FocusBorderBrush"] = Parse("#087F80"),
            ["AccentBrush"] = Parse("#087F80"),
            ["ModeHeaderBorderBrush"] = Parse("#8CBAC4"),
            ["StatusPillBackgroundBrush"] = Parse("#EDF2F6"),
            ["CheckBoxGlyphBrush"] = Parse("#F7F9FB"),
            ["CheckBoxBorderBrush"] = Parse("#8496A8"),
            ["CheckBoxHoverBrush"] = Parse("#E4EDF3"),
            ["CheckMarkBrush"] = Parse("#FFFFFF"),
            ["ButtonHoverBorderBrush"] = Parse("#075F60"),
            ["ButtonTextBrush"] = Parse("#FFFFFF"),
            ["TeacherCardBackgroundBrush"] = Parse("#E6F4F4"),
            ["TeacherCardBorderBrush"] = Parse("#76B9B8"),
            ["StudentCardBackgroundBrush"] = Parse("#FFF3DF"),
            ["StudentCardBorderBrush"] = Parse("#D6A55B"),
            ["WarmAccentBrush"] = Parse("#98610B"),
            ["CorrectBrush"] = Parse("#218739"),
            ["ErrorBrush"] = Parse("#C53F32"),
            ["DarkComboForegroundBrush"] = Parse("#172432"),
            ["DarkComboBackgroundBrush"] = Parse("#FFFFFF"),
            ["DarkComboPopupBrush"] = Parse("#FFFFFF"),
            ["DarkComboBorderBrush"] = Parse("#A8B7C5"),
            ["DarkComboHoverBrush"] = Parse("#E6EDF3"),
            ["DarkComboPressedBrush"] = Parse("#CFE7E6"),
            ["DarkComboSelectedBrush"] = Parse("#B9DDDC")
        };
    }

    private static Color Parse(string value) => (Color)ColorConverter.ConvertFromString(value);
}
