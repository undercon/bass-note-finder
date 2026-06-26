using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BassNoteFinder.Gameplay;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;

namespace BassNoteFinder.Views;

public partial class StudentModeView : UserControl, IGameMode
{
    private enum FretboardState { Hidden, FlashingWrong, CelebratingCorrect }

    private readonly NoteGenerator _generator = new(28, 48);
    private readonly StaffRenderer _staff = new();
    private readonly FretboardRenderer _fretboardRenderer = new();
    private readonly DispatcherTimer _nextNoteTimer;

    private Note? _currentNote;
    private StaffRenderer.AccidentalMode _currentMode = StaffRenderer.AccidentalMode.Natural;

    public event Action? BackToMenuRequested;
    public event Action<bool>? IncludeOctavesChanged;
    public bool IncludeOctaves => IncludeOctavesCheckBox.IsChecked == true;

    public StudentModeView()
    {
        InitializeComponent();
        _nextNoteTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _nextNoteTimer.Tick += NextNoteTimer_Tick;
    }

    public void OnActivate()
    {
        _nextNoteTimer.Stop();
        PickRandomNote();
    }

    public void OnDeactivate()
    {
        _nextNoteTimer.Stop();
    }

    public void OnNoteDetected(Note note, double centsOff)
    {
        if (_currentNote == null) return;
        Note target = _currentNote.Value;
        Note evaluatedNote = EvaluateDetectedNoteAgainstTarget(note, target);
        string playedDisplay = NoteDisplay.Format(note, ToDisplayAccidental(_currentMode), includeOctave: true);

        if (evaluatedNote.MidiNote == target.MidiNote)
        {
            SetFretboardState(FretboardState.CelebratingCorrect, target);
            if (IsAutoAdvanceEnabled)
            {
                int seconds = (int)Math.Round(NextNoteDelaySlider.Value);
                StatusText.Text = $"Correct! You played {playedDisplay}. Next note in {seconds}s...";
                _nextNoteTimer.Interval = TimeSpan.FromSeconds(seconds);
                _nextNoteTimer.Stop();
                _nextNoteTimer.Start();
            }
            else
            {
                StatusText.Text = $"Correct! You played {playedDisplay}.";
                _nextNoteTimer.Stop();
            }
            StatusText.FontSize = 18;
            StatusText.FontWeight = FontWeights.SemiBold;
            StatusText.Foreground = Brushes.LimeGreen;
        }
        else
        {
            SetFretboardState(FretboardState.FlashingWrong, note);
            StatusText.Text = $"Not quite. You played {playedDisplay}.";
            StatusText.FontSize = 16;
            StatusText.FontWeight = FontWeights.SemiBold;
            StatusText.Foreground = Brushes.OrangeRed;
        }
    }

    public void OnNoteLost() { }

    public void OnSpacePressed()
    {
        PickRandomNote();
    }

    private void StudentModeView_Loaded(object sender, RoutedEventArgs e)
    {
        _staff.IncludeOctaves = IncludeOctavesCheckBox.IsChecked == true;
        SyncNextNoteDelayUi();
        UpdateStaffWidth();
        RerenderStaff();
    }

    private void UpdateStaffWidth()
    {
        _staff.StaffWidth = StaffCanvas.ActualWidth > 100 ? StaffCanvas.ActualWidth - 20 : 500;
    }

    private void RandomBtn_Click(object sender, RoutedEventArgs e)
    {
        PickRandomNote();
    }

    private void BackToModeSelectionBtn_Click(object sender, RoutedEventArgs e)
    {
        BackToMenuRequested?.Invoke();
    }

    private void ShowNoteNamesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _staff.ShowNoteNames = ShowNoteNamesCheckBox.IsChecked == true;
        UpdateStatusText();
        RerenderStaff();
    }

    private void IncludeAccidentalsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _staff.IncludeAccidentals = IncludeAccidentalsCheckBox.IsChecked == true;
        if (!_staff.IncludeAccidentals && _currentMode != StaffRenderer.AccidentalMode.Natural)
        {
            PickRandomNote();
            return;
        }
        RerenderStaff();
    }

    private void IncludeOctavesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        bool includeOctaves = IncludeOctavesCheckBox.IsChecked == true;
        _staff.IncludeOctaves = includeOctaves;
        IncludeOctavesChanged?.Invoke(includeOctaves);
        UpdateStatusText();
        RerenderStaff();
    }

    private void AutoAdvanceCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsAutoAdvanceEnabled)
        {
            _nextNoteTimer.Stop();
        }

        SyncNextNoteDelayUi();
    }

    private void NextNoteDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SyncNextNoteDelayUi();
    }

    private void PickRandomNote()
    {
        _nextNoteTimer.Stop();
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
        _currentNote = note;
        _currentMode = mode;
        SetFretboardState(FretboardState.Hidden);
        UpdateStatusText();
        RerenderStaff();
    }

    private void RerenderStaff()
    {
        UpdateStaffWidth();
        if (_currentNote.HasValue)
        {
            _staff.Render(StaffCanvas, _currentNote.Value, _currentMode);
        }
        else
        {
            _staff.RenderEmpty(StaffCanvas);
        }
    }

    private void UpdateStatusText()
    {
        if (!_currentNote.HasValue)
        {
            StatusText.Text = "Play the shown note.";
            StatusText.FontSize = 14;
            StatusText.FontWeight = FontWeights.Normal;
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
            return;
        }

        if (ShowNoteNamesCheckBox.IsChecked == true)
        {
            StatusText.Text = $"Play: {NoteDisplay.Format(_currentNote.Value, ToDisplayAccidental(_currentMode), IncludeOctavesCheckBox.IsChecked == true)}";
            StatusText.FontSize = 16;
            StatusText.FontWeight = FontWeights.Bold;
            StatusText.Foreground = Brushes.White;
        }
        else
        {
            StatusText.Text = "Play this note on your bass.";
            StatusText.FontSize = 14;
            StatusText.FontWeight = FontWeights.Normal;
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
        }
    }

    private void SetFretboardState(FretboardState state, Note? studentNote = null)
    {
        switch (state)
        {
            case FretboardState.Hidden:
                FretboardPanel.Visibility = Visibility.Visible;
                OverlayPanel.Visibility = Visibility.Visible;
                _fretboardRenderer.Render(FretboardCanvas);
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
                    _fretboardRenderer.Render(FretboardCanvas, studentNote.Value, Color.FromRgb(0xFF, 0x32, 0x32));
                }
                break;

            case FretboardState.CelebratingCorrect:
                OverlayPanel.Visibility = Visibility.Visible;
                FretboardPanel.Visibility = Visibility.Visible;
                if (studentNote.HasValue)
                {
                    OverlayIcon.Text = NoteDisplay.Format(studentNote.Value, ToDisplayAccidental(_currentMode), includeOctave: true);
                    OverlayIcon.FontSize = 36;
                    OverlayIcon.Foreground = Brushes.LimeGreen;
                    OverlayText.Text = "Correct!";
                    OverlayText.Foreground = Brushes.LimeGreen;
                    _fretboardRenderer.Render(FretboardCanvas, studentNote.Value, Color.FromRgb(0xFF, 0x32, 0x32));
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

    private void NextNoteTimer_Tick(object? sender, EventArgs e)
    {
        _nextNoteTimer.Stop();
        PickRandomNote();
    }

    private bool IsAutoAdvanceEnabled => AutoAdvanceCheckBox.IsChecked == true;

    private void SyncNextNoteDelayUi()
    {
        if (NextNoteDelaySlider == null || NextNoteDelayValueText == null || AutoAdvanceCheckBox == null)
        {
            return;
        }

        int seconds = (int)Math.Round(NextNoteDelaySlider.Value);
        NextNoteDelayValueText.Text = $"{seconds}s";
        NextNoteDelaySlider.IsEnabled = IsAutoAdvanceEnabled;
        NextNoteDelayValueText.Foreground = IsAutoAdvanceEnabled
            ? new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC))
            : new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77));
    }

    private static NoteDisplay.AccidentalDisplay ToDisplayAccidental(StaffRenderer.AccidentalMode mode)
    {
        return mode switch
        {
            StaffRenderer.AccidentalMode.Flat => NoteDisplay.AccidentalDisplay.Flat,
            StaffRenderer.AccidentalMode.Sharp => NoteDisplay.AccidentalDisplay.Sharp,
            _ => NoteDisplay.AccidentalDisplay.Natural
        };
    }

    private static Note EvaluateDetectedNoteAgainstTarget(Note detected, Note target)
    {
        // Wrong pitch class entirely — report as-is
        if (detected.PitchClass != target.PitchClass)
            return detected;

        // Exact match
        if (detected.MidiNote == target.MidiNote)
            return detected;

        // Accept harmonic correction only for exactly one octave off (±12 semitones).
        // This is the only physically plausible single-harmonic detection error:
        // detector picks up the 2nd harmonic (octave above) or sub-octave (octave below).
        // Two or more octaves off means the player genuinely played the wrong octave.
        if (Math.Abs(detected.MidiNote - target.MidiNote) == 12)
            return target;

        return detected;
    }
}
