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
