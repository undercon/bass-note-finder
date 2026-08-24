using System.Text.Json.Serialization;
using BassNoteFinder.Localization;
using BassNoteFinder.MusicTheory;

namespace BassNoteFinder;

public class AppConfig
{
    [JsonConverter(typeof(JsonStringEnumConverter<AppLanguage>))]
    public AppLanguage Language { get; set; } = AppLanguage.System;
    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public AppTheme Theme { get; set; } = AppTheme.System;
    [JsonConverter(typeof(JsonStringEnumConverter<NoteDisplay.NamingConvention>))]
    public NoteDisplay.NamingConvention Notation { get; set; } = NoteDisplay.NamingConvention.Standard;
    public float MinSignalLevel { get; set; } = 0.01f;
    public string SelectedInputDevice { get; set; } = string.Empty;
    public bool StartMicOnLaunch { get; set; } = false;
    public bool ShowDeviation { get; set; } = false;
    public TeacherModeSettings TeacherMode { get; set; } = new();
    public StudentModeSettings StudentMode { get; set; } = new();
    public double WindowWidth { get; set; } = 1200;
    public double WindowHeight { get; set; } = 750;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
}

public class TeacherModeSettings
{
    public bool ShowNoteLabels { get; set; }
    public bool IncludeAccidentals { get; set; }
    public bool MatchOctave { get; set; }
    public bool RevealTargetOnMiss { get; set; }
}

public class StudentModeSettings : TeacherModeSettings
{
    public StudentModeSettings()
    {
        MatchOctave = true;
    }

    public bool AdaptivePractice { get; set; }
    public bool AutoAdvance { get; set; } = true;
    public double NextNoteDelaySeconds { get; set; } = 3;
    public int[] AvailablePitchClasses { get; set; } = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11];
}
