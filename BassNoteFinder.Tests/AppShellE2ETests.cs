using System.Windows.Controls;
using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Views;
using Xunit;

namespace BassNoteFinder.Tests;

public class AppNavigationTests
{
    private static MainWindow CreateWindow()
    {
        var w = new MainWindow();
        TestHelpers.InvokePrivate(w, "ShowMenu");
        return w;
    }

    [Fact]
    public void AfterShowMenu_MainContentIsMenuView()
    {
        var type = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var main = (ContentControl)w.FindName("MainContent");
            return main.Content?.GetType();
        });

        Assert.Equal(typeof(MenuView), type);
    }

    [Fact]
    public void TeacherModeSelected_NavigatesToTeacherModeView()
    {
        var type = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var main = (ContentControl)w.FindName("MainContent");
            TestHelpers.RaiseEvent((MenuView)main.Content!, "TeacherModeSelected");
            return main.Content?.GetType();
        });

        Assert.Equal(typeof(TeacherModeView), type);
    }

    [Fact]
    public void TeacherMode_BackRequested_ReturnsToMenuView()
    {
        var type = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var main = (ContentControl)w.FindName("MainContent");
            TestHelpers.RaiseEvent((MenuView)main.Content!, "TeacherModeSelected");
            TestHelpers.RaiseEvent((TeacherModeView)main.Content!, "BackToMenuRequested");
            return main.Content?.GetType();
        });

        Assert.Equal(typeof(MenuView), type);
    }

    [Fact]
    public void StudentModeSelected_NavigatesToStudentModeView()
    {
        var type = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var main = (ContentControl)w.FindName("MainContent");
            TestHelpers.RaiseEvent((MenuView)main.Content!, "StudentModeSelected");
            return main.Content?.GetType();
        });

        Assert.Equal(typeof(StudentModeView), type);
    }

    [Fact]
    public void StudentMode_BackRequested_ReturnsToMenuView()
    {
        var type = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var main = (ContentControl)w.FindName("MainContent");
            TestHelpers.RaiseEvent((MenuView)main.Content!, "StudentModeSelected");
            TestHelpers.RaiseEvent((StudentModeView)main.Content!, "BackToMenuRequested");
            return main.Content?.GetType();
        });

        Assert.Equal(typeof(MenuView), type);
    }

    [Fact]
    public void FullRoundTrip_TeacherBackStudentBack_EndsAtMenu()
    {
        var type = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var main = (ContentControl)w.FindName("MainContent");

            TestHelpers.RaiseEvent((MenuView)main.Content!, "TeacherModeSelected");
            TestHelpers.RaiseEvent((TeacherModeView)main.Content!, "BackToMenuRequested");
            TestHelpers.RaiseEvent((MenuView)main.Content!, "StudentModeSelected");
            TestHelpers.RaiseEvent((StudentModeView)main.Content!, "BackToMenuRequested");

            return main.Content?.GetType();
        });

        Assert.Equal(typeof(MenuView), type);
    }
}

public class FooterControlsE2ETests
{
    private static MainWindow CreateWindow()
    {
        var w = new MainWindow();
        TestHelpers.InvokePrivate(w, "ShowMenu");
        return w;
    }

    [Fact]
    public void ThresholdSlider_InitialDisplay_MatchesSliderValue()
    {
        TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var slider = (Slider)w.FindName("ThresholdSlider");
            var text = (TextBlock)w.FindName("ThresholdValueText");

            Assert.Equal(slider.Value.ToString("0.000"), text.Text);
            return 0;
        });
    }

    [Fact]
    public void ThresholdSlider_WhenValueChanges_DisplayUpdates()
    {
        string? display = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var slider = (Slider)w.FindName("ThresholdSlider");
            var text = (TextBlock)w.FindName("ThresholdValueText");

            slider.Value = 0.015;
            return text.Text;
        });

        Assert.Equal("0.015", display);
    }

    [Fact]
    public void HarmonicCorrectionCheckBox_MatchesPipelineSetting()
    {
        TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var cb = (CheckBox)w.FindName("HarmonicCorrectionCheckBox");
            var pipeline = TestHelpers.GetPrivateField<StableNoteDetectionPipeline>(w, "_detectionPipeline");

            Assert.NotNull(pipeline);
            Assert.Equal(cb.IsChecked == true, pipeline!.UseHarmonicCorrection);
            return 0;
        });
    }

    [Fact]
    public void HarmonicCorrectionCheckBox_WhenUnchecked_PipelineReflectsChange()
    {
        bool? pipelineValue = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var cb = (CheckBox)w.FindName("HarmonicCorrectionCheckBox");
            cb.IsChecked = false;
            var pipeline = TestHelpers.GetPrivateField<StableNoteDetectionPipeline>(w, "_detectionPipeline");
            return pipeline?.UseHarmonicCorrection;
        });

        Assert.False(pipelineValue);
    }

    [Fact]
    public void HarmonicCorrectionCheckBox_WhenRechecked_PipelineReflectsChange()
    {
        bool? pipelineValue = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var cb = (CheckBox)w.FindName("HarmonicCorrectionCheckBox");
            cb.IsChecked = false;
            cb.IsChecked = true;
            var pipeline = TestHelpers.GetPrivateField<StableNoteDetectionPipeline>(w, "_detectionPipeline");
            return pipeline?.UseHarmonicCorrection;
        });

        Assert.True(pipelineValue);
    }

    [Fact]
    public void ShowDeviationCheckBox_DefaultState_IsUnchecked()
    {
        bool? isChecked = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var cb = (CheckBox)w.FindName("ShowDeviationCheckBox");
            return cb.IsChecked;
        });

        Assert.False(isChecked);
    }

    [Fact]
    public void OnNoteDetected_WhenDeviationOff_DisplaysNoteNameOnly()
    {
        string? text = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            w.ShowDeviation = false;
            var note = new Note(43);
            TestHelpers.InvokePrivate(w, "OnStableNoteDetected", note, 5.0);
            return ((TextBlock)w.FindName("DetectedNoteText")).Text;
        });

        Assert.Equal("G2", text);
    }

    [Fact]
    public void OnNoteDetected_WhenDeviationOn_AppendsCentsOffset()
    {
        string? text = TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            w.ShowDeviation = true;
            var note = new Note(43);
            TestHelpers.InvokePrivate(w, "OnStableNoteDetected", note, -8.0);
            return ((TextBlock)w.FindName("DetectedNoteText")).Text;
        });

        Assert.NotNull(text);
        Assert.Contains("G2", text!);
        Assert.Contains("¢", text!);
        Assert.Contains("-8", text!);
    }

    [Fact]
    public void MicButton_IsEnabled_IfAndOnlyIfAudioDevicesAreAvailable()
    {
        TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var inputCombo = (ComboBox)w.FindName("InputCombo");
            var micBtn = (Button)w.FindName("ToggleMicBtn");

            bool hasDevices = inputCombo.Items.Count > 0;
            Assert.Equal(hasDevices, micBtn.IsEnabled);
            return 0;
        });
    }

    [Fact]
    public void WhenNoAudioDevices_DetectedNoteTextShowsNoDevices()
    {
        TestHelpers.RunOnSta(() =>
        {
            var w = CreateWindow();
            var inputCombo = (ComboBox)w.FindName("InputCombo");

            if (inputCombo.Items.Count > 0)
            {
                return 0;
            }

            var noteText = (TextBlock)w.FindName("DetectedNoteText");
            Assert.Equal("No devices", noteText.Text);
            return 0;
        });
    }
}
