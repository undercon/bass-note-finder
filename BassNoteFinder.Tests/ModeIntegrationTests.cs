using System.Reflection;
using System.Windows.Controls;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;
using BassNoteFinder.Views;
using Xunit;

namespace BassNoteFinder.Tests;

public class ModeIntegrationTests
{
    [Fact]
    public void StudentMode_WhenOctavesOff_StatusOmitsOctaveNumber()
    {
        string status = RunOnSta(() =>
        {
            var view = new StudentModeView();
            SetIncludeOctaves(view, false);
            SelectTarget(view, new Note(43), StaffRenderer.AccidentalMode.Natural);

            view.OnNoteDetected(new Note(43), 0);
            return GetStatusText(view);
        });

        Assert.Contains("You played G", status);
        Assert.DoesNotContain("G2", status);
    }

    [Fact]
    public void TeacherMode_WhenOctavesOff_StatusOmitsOctaveNumber()
    {
        string status = RunOnSta(() =>
        {
            var view = new TeacherModeView();
            SetIncludeOctaves(view, false);
            SelectTarget(view, new Note(43), StaffRenderer.AccidentalMode.Natural);

            view.OnNoteDetected(new Note(43), 0);
            return GetStatusText(view);
        });

        Assert.Contains("That was G", status);
        Assert.DoesNotContain("G2", status);
    }

    [Fact]
    public void StudentMode_WhenOctavesOn_OctaveMismatchIsIncorrect()
    {
        string status = RunOnSta(() =>
        {
            var view = new StudentModeView();
            SetIncludeOctaves(view, true);
            SelectTarget(view, new Note(43), StaffRenderer.AccidentalMode.Natural); // G2

            view.OnNoteDetected(new Note(31), 0); // G1
            return GetStatusText(view);
        });

        Assert.StartsWith("Not quite.", status);
    }

    [Fact]
    public void TeacherMode_WhenOctavesOn_OctaveMismatchIsIncorrect()
    {
        string status = RunOnSta(() =>
        {
            var view = new TeacherModeView();
            SetIncludeOctaves(view, true);
            SelectTarget(view, new Note(43), StaffRenderer.AccidentalMode.Natural); // G2

            view.OnNoteDetected(new Note(31), 0); // G1
            return GetStatusText(view);
        });

        Assert.StartsWith("Not quite.", status);
    }

    [Fact]
    public void StudentMode_IncludeOctavesToggle_RaisesSettingEvent()
    {
        bool? eventValue = null;

        RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.IncludeOctavesChanged += value => eventValue = value;
            SetIncludeOctaves(view, true);
            return 0;
        });

        Assert.True(eventValue);
    }

    [Fact]
    public void TeacherMode_IncludeOctavesToggle_RaisesSettingEvent()
    {
        bool? eventValue = null;

        RunOnSta(() =>
        {
            var view = new TeacherModeView();
            view.IncludeOctavesChanged += value => eventValue = value;
            SetIncludeOctaves(view, true);
            return 0;
        });

        Assert.True(eventValue);
    }

    [Fact]
    public void StudentMode_AutoAdvanceToggle_DisablesDelaySlider()
    {
        bool sliderEnabled = true;

        RunOnSta(() =>
        {
            var view = new StudentModeView();
            var autoAdvance = (CheckBox?)view.FindName("AutoAdvanceCheckBox");
            var delaySlider = (Slider?)view.FindName("NextNoteDelaySlider");
            Assert.NotNull(autoAdvance);
            Assert.NotNull(delaySlider);

            autoAdvance!.IsChecked = false;
            sliderEnabled = delaySlider!.IsEnabled;
            return 0;
        });

        Assert.False(sliderEnabled);
    }

    [Fact]
    public void TeacherMode_ConstructedWithSettings_AppliesSavedOptions()
    {
        RunOnSta(() =>
        {
            var view = new TeacherModeView(new TeacherModeSettings
            {
                ShowNoteLabels = true,
                IncludeAccidentals = true,
                MatchOctave = true
            });

            Assert.True(((CheckBox)view.FindName("ShowNoteNamesCheckBox")).IsChecked);
            Assert.True(((CheckBox)view.FindName("IncludeAccidentalsCheckBox")).IsChecked);
            Assert.True(((CheckBox)view.FindName("IncludeOctavesCheckBox")).IsChecked);
            return 0;
        });
    }

    [Fact]
    public void StudentMode_ConstructedWithSettings_AppliesSavedOptionsAndNotes()
    {
        RunOnSta(() =>
        {
            var view = new StudentModeView(new StudentModeSettings
            {
                ShowNoteLabels = true,
                IncludeAccidentals = true,
                MatchOctave = true,
                AdaptivePractice = true,
                AutoAdvance = false,
                NextNoteDelaySeconds = 7,
                AvailablePitchClasses = [4, 7]
            });

            Assert.True(((CheckBox)view.FindName("ShowNoteNamesCheckBox")).IsChecked);
            Assert.True(((CheckBox)view.FindName("IncludeAccidentalsCheckBox")).IsChecked);
            Assert.True(((CheckBox)view.FindName("IncludeOctavesCheckBox")).IsChecked);
            Assert.True(((CheckBox)view.FindName("AdaptivePracticeCheckBox")).IsChecked);
            Assert.False(((CheckBox)view.FindName("AutoAdvanceCheckBox")).IsChecked);
            Assert.Equal(7, ((Slider)view.FindName("NextNoteDelaySlider")).Value);
            Assert.True(((CheckBox)view.FindName("NoteECheckBox")).IsChecked);
            Assert.True(((CheckBox)view.FindName("NoteGCheckBox")).IsChecked);
            Assert.False(((CheckBox)view.FindName("NoteCCheckBox")).IsChecked);
            return 0;
        });
    }

    [Fact]
    public void StudentMode_AvailableNoteToggle_RaisesSettingsChanged()
    {
        int[]? pitchClasses = null;

        RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.SettingsChanged += settings => pitchClasses = settings.AvailablePitchClasses;

            var cSharp = (CheckBox)view.FindName("NoteCsCheckBox");
            cSharp.IsChecked = false;
            return 0;
        });

        Assert.NotNull(pitchClasses);
        Assert.DoesNotContain(1, pitchClasses!);
    }

    [Fact]
    public void StudentMode_SetNotation_UpdatesAvailableNoteNames()
    {
        RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.SetNotation(NoteDisplay.NamingConvention.Solfege);

            Assert.Equal("Do", ((CheckBox)view.FindName("NoteCCheckBox")).Content);
            Assert.Equal("Do♯", ((CheckBox)view.FindName("NoteCsCheckBox")).Content);
            Assert.Equal("Si", ((CheckBox)view.FindName("NoteBCheckBox")).Content);
            return 0;
        });
    }

    [Fact]
    public void StudentMode_AdaptiveCoachToggle_RaisesSettingsChanged()
    {
        bool? adaptivePractice = null;

        RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.SettingsChanged += settings => adaptivePractice = settings.AdaptivePractice;

            ((CheckBox)view.FindName("AdaptivePracticeCheckBox")).IsChecked = true;
            return 0;
        });

        Assert.True(adaptivePractice);
    }

    [Fact]
    public void StudentMode_AccidentalNoteOptions_DisableWhenAccidentalsAreOff()
    {
        RunOnSta(() =>
        {
            var view = new StudentModeView(new StudentModeSettings { IncludeAccidentals = false });
            var cSharp = (CheckBox)view.FindName("NoteCsCheckBox");
            var e = (CheckBox)view.FindName("NoteECheckBox");

            Assert.False(cSharp.IsEnabled);
            Assert.True(e.IsEnabled);

            var includeAccidentals = (CheckBox)view.FindName("IncludeAccidentalsCheckBox");
            includeAccidentals.IsChecked = true;
            Assert.True(cSharp.IsEnabled);
            return 0;
        });
    }

    [Fact]
    public void TeacherMode_DisablingAccidentals_ReplacesAnAccidentalTargetWithANatural()
    {
        RunOnSta(() =>
        {
            var view = new TeacherModeView(new TeacherModeSettings { IncludeAccidentals = true });
            SelectTarget(view, new Note(37), StaffRenderer.AccidentalMode.Sharp); // C#2

            ((CheckBox)view.FindName("IncludeAccidentalsCheckBox")).IsChecked = false;

            var currentNote = (Note?)view.GetType()
                .GetField("_currentNote", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view);
            var currentMode = (StaffRenderer.AccidentalMode)view.GetType()
                .GetField("_currentMode", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(view)!;

            Assert.True(currentNote.HasValue);
            Assert.Contains(currentNote.Value.PitchClass, new[] { 0, 2, 4, 5, 7, 9, 11 });
            Assert.Equal(StaffRenderer.AccidentalMode.Natural, currentMode);
            return 0;
        });
    }

    private static void SelectTarget(object view, Note note, StaffRenderer.AccidentalMode mode)
    {
        MethodInfo? select = view.GetType().GetMethod("SelectNote", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(select);
        select!.Invoke(view, [note, mode]);
    }

    private static void SetIncludeOctaves(UserControl view, bool enabled)
    {
        var includeOctaves = (CheckBox?)view.FindName("IncludeOctavesCheckBox");
        Assert.NotNull(includeOctaves);
        includeOctaves!.IsChecked = enabled;
    }

    private static string GetStatusText(UserControl view)
    {
        var status = (TextBlock?)view.FindName("StatusText");
        Assert.NotNull(status);
        return status!.Text;
    }

    private static T RunOnSta<T>(Func<T> action)
    {
        T? result = default;
        Exception? ex = null;

        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception e)
            {
                ex = e;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (ex != null)
        {
            throw new TargetInvocationException(ex);
        }

        return result!;
    }
}
