using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;
using BassNoteFinder.Views;
using Xunit;

namespace BassNoteFinder.Tests;

public class TeacherModeE2ETests
{
    private static TeacherModeView CreateActivated()
    {
        var view = new TeacherModeView();
        view.OnActivate();
        return view;
    }

    private static string StatusText(TeacherModeView view) =>
        ((TextBlock)view.FindName("StatusText")).Text;

    private static Brush StatusForeground(TeacherModeView view) =>
        ((TextBlock)view.FindName("StatusText")).Foreground;

    private static string OverlayIconText(TeacherModeView view) =>
        ((TextBlock)view.FindName("OverlayIcon")).Text;

    private static string OverlayBodyText(TeacherModeView view) =>
        ((TextBlock)view.FindName("OverlayText")).Text;

    private static Visibility OverlayPanelVisibility(TeacherModeView view) =>
        ((FrameworkElement)view.FindName("OverlayPanel")).Visibility;

    [Fact]
    public void OnActivate_StatusShowsPlacementInstruction()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = CreateActivated();
            return StatusText(view);
        });

        Assert.Equal("Click the staff to place a note, or press Random.", status);
    }

    [Fact]
    public void OnActivate_OverlayShowsQuestionMark()
    {
        string icon = TestHelpers.RunOnSta(() =>
        {
            var view = CreateActivated();
            return OverlayIconText(view);
        });

        Assert.Equal("?", icon);
    }

    [Fact]
    public void OnActivate_OverlayTextShowsRevealHint()
    {
        string body = TestHelpers.RunOnSta(() =>
        {
            var view = CreateActivated();
            return OverlayBodyText(view);
        });

        Assert.Equal("Play the note to reveal", body);
    }

    [Fact]
    public void AfterSelectNote_StatusShowsFindInstruction()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            return StatusText(view);
        });

        Assert.Equal("Find this note on your bass.", status);
    }

    [Fact]
    public void AfterSelectNote_OverlayStillShowsQuestionMark()
    {
        string icon = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            return OverlayIconText(view);
        });

        Assert.Equal("?", icon);
    }

    [Fact]
    public void OnSpacePressed_SelectsANote_StatusChangesFromPlaceholder()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = CreateActivated();
            view.OnSpacePressed();
            return StatusText(view);
        });

        Assert.NotEqual("Click the staff to place a note, or press Random.", status);
    }

    [Fact]
    public void CorrectNote_StatusStartsWithCorrect()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(43), 0);
            return StatusText(view);
        });

        Assert.StartsWith("Correct! That was", status);
    }

    [Fact]
    public void CorrectNote_StatusForegroundIsGreen()
    {
        Brush fg = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(43), 0);
            return StatusForeground(view);
        });

        Assert.Equal(Brushes.LimeGreen, fg);
    }

    [Fact]
    public void CorrectNote_OverlayTextShowsCorrect()
    {
        string body = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(43), 0);
            return OverlayBodyText(view);
        });

        Assert.Equal("Correct!", body);
    }

    [Fact]
    public void CorrectNote_OverlayPanelRemainsVisible()
    {
        Visibility vis = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(43), 0);
            return OverlayPanelVisibility(view);
        });

        Assert.Equal(Visibility.Visible, vis);
    }

    [Fact]
    public void WrongNote_StatusStartsWithNotQuite()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(40), 0);
            return StatusText(view);
        });

        Assert.StartsWith("Not quite.", status);
    }

    [Fact]
    public void WrongNote_StatusForegroundIsOrangeRed()
    {
        Brush fg = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(40), 0);
            return StatusForeground(view);
        });

        Assert.Equal(Brushes.OrangeRed, fg);
    }

    [Fact]
    public void WrongNote_OverlayPanelIsHidden()
    {
        Visibility vis = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(40), 0);
            return OverlayPanelVisibility(view);
        });

        Assert.Equal(Visibility.Hidden, vis);
    }

    [Fact]
    public void ShowNoteNames_Checked_StatusRevealsPitch()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            var cb = (CheckBox)view.FindName("ShowNoteNamesCheckBox");
            cb.IsChecked = true;
            return StatusText(view);
        });

        Assert.StartsWith("Looking for:", status);
    }

    [Fact]
    public void HarmonicOctave_OneOctaveAboveTarget_WithOctavesOff_TreatedAsCorrect()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            var includeOctaves = (CheckBox)view.FindName("IncludeOctavesCheckBox");
            includeOctaves.IsChecked = false;
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(55), 0);
            return StatusText(view);
        });

        Assert.StartsWith("Correct!", status);
    }

    [Fact]
    public void HarmonicOctave_TwoOctavesAboveTarget_WithOctavesOff_TreatedAsWrong()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new TeacherModeView();
            var includeOctaves = (CheckBox)view.FindName("IncludeOctavesCheckBox");
            includeOctaves.IsChecked = false;
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(67), 0);
            return StatusText(view);
        });

        Assert.StartsWith("Not quite.", status);
    }
}

public class StudentModeE2ETests
{
    private static string StatusText(StudentModeView view) =>
        ((TextBlock)view.FindName("StatusText")).Text;

    private static Brush StatusForeground(StudentModeView view) =>
        ((TextBlock)view.FindName("StatusText")).Foreground;

    private static string OverlayBodyText(StudentModeView view) =>
        ((TextBlock)view.FindName("OverlayText")).Text;

    [Fact]
    public void OnActivate_AutoPicksANote_StatusShowsPlayInstruction()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.OnActivate();
            return StatusText(view);
        });

        Assert.Equal("Play this note on your bass.", status);
    }

    [Fact]
    public void CorrectNote_AutoAdvanceOn_StatusShowsCountdown()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.OnActivate();

            var autoAdvance = (CheckBox)view.FindName("AutoAdvanceCheckBox");
            autoAdvance.IsChecked = true;

            Note? current = TestHelpers.GetPrivateField<Note?>(view, "_currentNote");
            Assert.NotNull(current);
            view.OnNoteDetected(current!.Value, 0);
            return StatusText(view);
        });

        Assert.Contains("Next note in", status);
    }

    [Fact]
    public void CorrectNote_AutoAdvanceOff_StatusShowsCorrectWithNoCountdown()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.OnActivate();

            var autoAdvance = (CheckBox)view.FindName("AutoAdvanceCheckBox");
            autoAdvance.IsChecked = false;

            Note? current = TestHelpers.GetPrivateField<Note?>(view, "_currentNote");
            Assert.NotNull(current);
            view.OnNoteDetected(current!.Value, 0);
            return StatusText(view);
        });

        Assert.StartsWith("Correct!", status);
        Assert.DoesNotContain("Next note in", status);
    }

    [Fact]
    public void CorrectNote_StatusForegroundIsGreen()
    {
        Brush fg = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.OnActivate();

            Note? current = TestHelpers.GetPrivateField<Note?>(view, "_currentNote");
            Assert.NotNull(current);
            view.OnNoteDetected(current!.Value, 0);
            return StatusForeground(view);
        });

        Assert.Equal(Brushes.LimeGreen, fg);
    }

    [Fact]
    public void CorrectNote_OverlayTextShowsCorrect()
    {
        string body = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.OnActivate();

            Note? current = TestHelpers.GetPrivateField<Note?>(view, "_currentNote");
            Assert.NotNull(current);
            view.OnNoteDetected(current!.Value, 0);
            return OverlayBodyText(view);
        });

        Assert.Equal("Correct!", body);
    }

    [Fact]
    public void WrongNote_StatusStartsWithNotQuite()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(40), 0);
            return StatusText(view);
        });

        Assert.StartsWith("Not quite.", status);
    }

    [Fact]
    public void WrongNote_StatusForegroundIsOrangeRed()
    {
        Brush fg = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            view.OnNoteDetected(new Note(40), 0);
            return StatusForeground(view);
        });

        Assert.Equal(Brushes.OrangeRed, fg);
    }

    [Fact]
    public void ShowNoteNames_Checked_StatusRevealsPitch()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            TestHelpers.InvokeSelectNote(view, new Note(43), StaffRenderer.AccidentalMode.Natural);
            var cb = (CheckBox)view.FindName("ShowNoteNamesCheckBox");
            cb.IsChecked = true;
            return StatusText(view);
        });

        Assert.StartsWith("Play:", status);
    }

    [Fact]
    public void DelaySlider_WhenValueChanges_UpdatesDisplayText()
    {
        string? text = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            var slider = (Slider)view.FindName("NextNoteDelaySlider");
            var label = (TextBlock)view.FindName("NextNoteDelayValueText");
            slider.Value = 7.0;
            return label.Text;
        });

        Assert.Equal("7s", text);
    }

    [Fact]
    public void OnSpacePressed_KeepsANoteSelected_StatusNotEmpty()
    {
        string status = TestHelpers.RunOnSta(() =>
        {
            var view = new StudentModeView();
            view.OnActivate();
            view.OnSpacePressed();
            return StatusText(view);
        });

        Assert.NotEqual("Play the shown note.", status);
    }
}
