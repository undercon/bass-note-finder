using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BassNoteFinder.Gameplay;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;

namespace BassNoteFinder.Views;

public partial class TeacherModeView : UserControl, IGameMode
{
    private enum FretboardState { Hidden, FlashingWrong, CelebratingCorrect }

    private readonly NoteGenerator _generator = new(28, 48);
    private readonly StaffRenderer _staff = new();
    private readonly FretboardRenderer _fretboardRenderer = new();
    private readonly DispatcherTimer _flashTimer;

    private Note? _currentNote;
    private StaffRenderer.AccidentalMode _currentMode = StaffRenderer.AccidentalMode.Natural;
    private Note? _hoverNote;
    private StaffRenderer.AccidentalMode _hoverMode = StaffRenderer.AccidentalMode.Natural;
    private FretboardState _fretboardState = FretboardState.Hidden;

    public event Action? BackToMenuRequested;

    public TeacherModeView()
    {
        InitializeComponent();
        _flashTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _flashTimer.Tick += FlashTimer_Tick;
    }

    public void OnActivate()
    {
        _currentNote = null;
        SetFretboardState(FretboardState.Hidden);
        UpdateStatusText();
    }

    public void OnDeactivate()
    {
        _flashTimer.Stop();
    }

    public void OnNoteDetected(Note note, double centsOff)
    {
        if (_currentNote == null) return;
        Note target = _currentNote.Value;

        var writtenTarget = new Note(target.MidiNote + 12);
        var writtenPlayed = new Note(note.MidiNote + 12);

        if (note.MidiNote == target.MidiNote)
        {
            SetFretboardState(FretboardState.CelebratingCorrect, target);
            StatusText.Text = $"Correct! That was {writtenTarget.FullName} \u2713";
            StatusText.FontSize = 20;
            StatusText.FontWeight = FontWeights.Bold;
            StatusText.Foreground = Brushes.LimeGreen;
        }
        else
        {
            SetFretboardState(FretboardState.FlashingWrong, note);
            StatusText.Text = $"Not quite. You played {writtenPlayed.FullName}.";
            StatusText.FontSize = 16;
            StatusText.FontWeight = FontWeights.Bold;
            StatusText.Foreground = Brushes.OrangeRed;
        }
    }

    public void OnNoteLost() { }

    public void OnSpacePressed()
    {
        PickRandomNote();
    }

    private void TeacherModeView_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateStaffWidth();
        _staff.RenderEmpty(StaffCanvas);
    }

    private void UpdateStaffWidth()
    {
        _staff.StaffWidth = StaffCanvas.ActualWidth > 100 ? StaffCanvas.ActualWidth - 20 : 500;
    }

    private void StaffCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(StaffCanvas);
        var result = StaffRenderer.NoteFromPoint(pos.X, pos.Y, _staff.IncludeAccidentals);
        if (result != null)
        {
            var (note, mode) = result.Value;
            SelectNote(note, mode);
        }
    }

    private void StaffCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(StaffCanvas);
        var result = StaffRenderer.NoteFromPoint(pos.X, pos.Y, _staff.IncludeAccidentals);
        if (result == null)
        {
            if (_hoverNote != null)
            {
                _hoverNote = null;
                RerenderStaff();
            }
            return;
        }

        var (note, mode) = result.Value;
        if (_hoverNote == null || note.MidiNote != _hoverNote.Value.MidiNote || mode != _hoverMode)
        {
            _hoverNote = note;
            _hoverMode = mode;
            RerenderStaff();
        }
    }

    private void StaffCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_hoverNote != null)
        {
            _hoverNote = null;
            RerenderStaff();
        }
    }

    private void RandomBtn_Click(object sender, RoutedEventArgs e)
    {
        PickRandomNote();
    }

    private void BackBtn_Click(object sender, RoutedEventArgs e)
    {
        BackToMenuRequested?.Invoke();
    }

    private void ShowNoteNamesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _staff.ShowNoteNames = ShowNoteNamesCheckBox.IsChecked == true;
        RerenderStaff();
    }

    private void IncludeAccidentalsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _staff.IncludeAccidentals = IncludeAccidentalsCheckBox.IsChecked == true;
        if (!_staff.IncludeAccidentals && _currentMode != StaffRenderer.AccidentalMode.Natural)
        {
            _currentMode = StaffRenderer.AccidentalMode.Natural;
            _hoverMode = StaffRenderer.AccidentalMode.Natural;
        }
        RerenderStaff();
    }

    private void ShowStatusNoteNameCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (_currentNote.HasValue)
        {
            if (ShowStatusNoteNameCheckBox.IsChecked == true)
            {
                var writtenNote = new Note(_currentNote.Value.MidiNote + 12);
                StatusText.Text = $"Looking for: {writtenNote.FullName}";
                StatusText.FontSize = 20;
                StatusText.FontWeight = FontWeights.Bold;
                StatusText.Foreground = Brushes.White;
            }
            else
            {
                StatusText.Text = "Find this note on your bass.";
                StatusText.FontSize = 13;
                StatusText.FontWeight = FontWeights.Normal;
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            }
        }
        else
        {
            StatusText.Text = "Click the staff to place a note, or press Random.";
            StatusText.FontSize = 13;
            StatusText.FontWeight = FontWeights.Normal;
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
        }
    }

    private void PickRandomNote()
    {
        if (_staff.IncludeAccidentals)
        {
            var (note, mode) = _generator.RandomNoteWithAccidental();
            SelectNote(note, mode);
        }
        else
        {
            SelectNote(_generator.RandomNote(), StaffRenderer.AccidentalMode.Natural);
        }
    }

    private void SelectNote(Note note, StaffRenderer.AccidentalMode mode)
    {
        _flashTimer.Stop();
        _currentNote = note;
        _currentMode = mode;
        _hoverNote = null;
        SetFretboardState(FretboardState.Hidden);

        UpdateStaffWidth();
        _staff.Render(StaffCanvas, note, mode);
        UpdateStatusText();
    }

    private void RerenderStaff()
    {
        UpdateStaffWidth();

        if (_currentNote.HasValue && _hoverNote.HasValue)
        {
            _staff.RenderWithPreview(StaffCanvas, _currentNote.Value, _currentMode, _hoverNote.Value, _hoverMode);
        }
        else if (_currentNote.HasValue)
        {
            _staff.Render(StaffCanvas, _currentNote.Value, _currentMode);
        }
        else if (_hoverNote.HasValue)
        {
            _staff.RenderEmptyWithPreview(StaffCanvas, _hoverNote.Value, _hoverMode);
        }
        else
        {
            _staff.RenderEmpty(StaffCanvas);
        }
    }

    private void SetFretboardState(FretboardState state, Note? studentNote = null)
    {
        _fretboardState = state;

        switch (state)
        {
            case FretboardState.Hidden:
                FretboardPanel.Visibility = Visibility.Hidden;
                OverlayPanel.Visibility = Visibility.Visible;
                OverlayIcon.Text = "?";
                OverlayIcon.FontSize = 48;
                OverlayIcon.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
                OverlayText.Text = "Play the note to reveal";
                OverlayText.Foreground = new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));
                break;

            case FretboardState.FlashingWrong:
                OverlayPanel.Visibility = Visibility.Hidden;
                FretboardPanel.Visibility = Visibility.Visible;
                if (studentNote.HasValue)
                {
                    _fretboardRenderer.Render(FretboardCanvas, studentNote.Value,
                        Color.FromRgb(0xFF, 0x32, 0x32));
                }
                break;

            case FretboardState.CelebratingCorrect:
                FretboardPanel.Visibility = Visibility.Hidden;
                OverlayPanel.Visibility = Visibility.Visible;
                if (studentNote.HasValue)
                {
                    var written = new Note(studentNote.Value.MidiNote + 12);
                    OverlayIcon.Text = written.FullName;
                    OverlayIcon.FontSize = 36;
                    OverlayIcon.Foreground = Brushes.LimeGreen;
                    OverlayText.Text = "Correct!";
                    OverlayText.Foreground = Brushes.LimeGreen;
                }
                else
                {
                    OverlayIcon.Text = "\u2713";
                    OverlayIcon.FontSize = 48;
                    OverlayIcon.Foreground = Brushes.LimeGreen;
                    OverlayText.Text = "Correct!";
                    OverlayText.Foreground = Brushes.LimeGreen;
                }
                break;
        }
    }

    private void FlashTimer_Tick(object? sender, EventArgs e)
    {
        _flashTimer.Stop();
    }
}
