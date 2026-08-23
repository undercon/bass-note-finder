using System.Diagnostics;
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
    private readonly AdaptivePracticeEngine _adaptivePractice = new();

    private Note? _currentNote;
    private StaffRenderer.AccidentalMode _currentMode = StaffRenderer.AccidentalMode.Natural;
    private bool _loadingSettings;
    private long _targetStartedTimestamp;
    private int _mistakesOnCurrentTarget;
    private bool _targetResultRecorded = true;

    public event Action? BackToMenuRequested;
    public event Action<bool>? IncludeOctavesChanged;
    public event Action<StudentModeSettings>? SettingsChanged;
    public bool IncludeOctaves => IncludeOctavesCheckBox.IsChecked == true;

    public StudentModeView() : this(new StudentModeSettings())
    {
    }

    public StudentModeView(StudentModeSettings settings)
    {
        InitializeComponent();
        _nextNoteTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _nextNoteTimer.Tick += NextNoteTimer_Tick;
        ApplySettings(settings);
    }

    public void OnActivate()
    {
        _nextNoteTimer.Stop();
        PickRandomNote(recordSkippedTarget: false);
    }

    public void OnDeactivate()
    {
        _nextNoteTimer.Stop();
    }

    public void RefreshTheme()
    {
        RerenderStaff();
        SyncNextNoteDelayUi();
    }

    public void OnNoteDetected(Note note, double centsOff)
    {
        if (_currentNote == null) return;
        Note target = _currentNote.Value;
        bool includeOctave = IncludeOctavesCheckBox.IsChecked == true;
        Note evaluatedNote = EvaluateDetectedNoteAgainstTarget(note, target, includeOctave);
        string playedDisplay = NoteDisplay.Format(note, ToDisplayAccidental(_currentMode), includeOctave);

        if (evaluatedNote.MidiNote == target.MidiNote)
        {
            RecordAdaptiveResult(target);
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
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "CorrectBrush");
        }
        else
        {
            if (IsAdaptivePracticeEnabled && !_targetResultRecorded)
            {
                _mistakesOnCurrentTarget++;
            }
            SetFretboardState(FretboardState.FlashingWrong, note);
            StatusText.Text = $"Not quite. You played {playedDisplay}.";
            StatusText.FontSize = 16;
            StatusText.FontWeight = FontWeights.SemiBold;
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "ErrorBrush");
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
        _staff.StaffWidth = StaffCanvas.ActualWidth > 100 ? StaffCanvas.ActualWidth : StaffCanvas.Width;
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
        NotifySettingsChanged();
        UpdateStatusText();
        RerenderStaff();
    }

    private void IncludeAccidentalsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _staff.IncludeAccidentals = IncludeAccidentalsCheckBox.IsChecked == true;
        NotifySettingsChanged();
        if (!_staff.IncludeAccidentals && _currentMode != StaffRenderer.AccidentalMode.Natural)
        {
            SyncAvailableNoteCheckBoxStates();
            PickRandomNote(recordSkippedTarget: false);
            return;
        }
        SyncAvailableNoteCheckBoxStates();
        RerenderStaff();
    }

    private void IncludeOctavesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        bool includeOctaves = IncludeOctavesCheckBox.IsChecked == true;
        _staff.IncludeOctaves = includeOctaves;
        IncludeOctavesChanged?.Invoke(includeOctaves);
        NotifySettingsChanged();
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
        NotifySettingsChanged();
    }

    private void AdaptivePracticeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings)
        {
            return;
        }

        if (IsAdaptivePracticeEnabled && _currentNote.HasValue)
        {
            BeginTrackingCurrentTarget();
        }
        else
        {
            _targetResultRecorded = true;
        }

        NotifySettingsChanged();
    }

    private void NextNoteDelaySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        SyncNextNoteDelayUi();
        NotifySettingsChanged();
    }

    private void AvailableNoteCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingSettings)
        {
            return;
        }

        var selectedPitchClasses = GetAvailablePitchClasses();
        if (selectedPitchClasses.Count == 0 && sender is CheckBox changed)
        {
            changed.IsChecked = true;
            return;
        }

        NotifySettingsChanged();
        PickRandomNote(recordSkippedTarget: false);
    }

    private void PickRandomNote(bool recordSkippedTarget = true)
    {
        _nextNoteTimer.Stop();
        if (recordSkippedTarget)
        {
            RecordSkippedAdaptiveTarget();
        }

        HashSet<int> availablePitchClasses = GetPlayablePitchClasses();
        IReadOnlySet<int> targetPitchClasses = availablePitchClasses;
        if (IsAdaptivePracticeEnabled)
        {
            targetPitchClasses = new HashSet<int>
            {
                _adaptivePractice.ChooseNext(availablePitchClasses)
            };
        }

        if (_staff.IncludeAccidentals)
        {
            var (note, mode) = _generator.RandomNoteWithAccidental(targetPitchClasses);
            SelectNote(note, mode);
        }
        else
        {
            SelectNote(_generator.RandomNote(targetPitchClasses), StaffRenderer.AccidentalMode.Natural);
        }
    }

    private void SelectNote(Note note, StaffRenderer.AccidentalMode mode)
    {
        _currentNote = note;
        _currentMode = mode;
        BeginTrackingCurrentTarget();
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
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
            return;
        }

        if (ShowNoteNamesCheckBox.IsChecked == true)
        {
            StatusText.Text = $"Play: {NoteDisplay.Format(_currentNote.Value, ToDisplayAccidental(_currentMode), IncludeOctavesCheckBox.IsChecked == true)}";
            StatusText.FontSize = 16;
            StatusText.FontWeight = FontWeights.Bold;
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "PanelHeaderBrush");
        }
        else
        {
            StatusText.Text = "Play this note on your bass.";
            StatusText.FontSize = 14;
            StatusText.FontWeight = FontWeights.Normal;
            StatusText.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
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
                OverlayIcon.SetResourceReference(TextBlock.ForegroundProperty, "SecondaryBrush");
                OverlayText.Text = "Play the note to reveal";
                OverlayText.SetResourceReference(TextBlock.ForegroundProperty, "SubtleTextBrush");
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
                    OverlayIcon.Text = NoteDisplay.Format(studentNote.Value, ToDisplayAccidental(_currentMode), IncludeOctavesCheckBox.IsChecked == true);
                    OverlayIcon.FontSize = 36;
                    OverlayIcon.SetResourceReference(TextBlock.ForegroundProperty, "CorrectBrush");
                    OverlayText.Text = "Correct!";
                    OverlayText.SetResourceReference(TextBlock.ForegroundProperty, "CorrectBrush");
                    _fretboardRenderer.Render(FretboardCanvas, studentNote.Value, Color.FromRgb(0xFF, 0x32, 0x32));
                }
                else
                {
                    OverlayIcon.Text = "\u2713";
                    OverlayIcon.FontSize = 48;
                    OverlayIcon.SetResourceReference(TextBlock.ForegroundProperty, "CorrectBrush");
                    OverlayText.Text = "Correct!";
                    OverlayText.SetResourceReference(TextBlock.ForegroundProperty, "CorrectBrush");
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
    private bool IsAdaptivePracticeEnabled => AdaptivePracticeCheckBox.IsChecked == true;

    private void SyncNextNoteDelayUi()
    {
        if (NextNoteDelaySlider == null || NextNoteDelayValueText == null || AutoAdvanceCheckBox == null)
        {
            return;
        }

        int seconds = (int)Math.Round(NextNoteDelaySlider.Value);
        NextNoteDelayValueText.Text = $"{seconds}s";
        NextNoteDelaySlider.IsEnabled = IsAutoAdvanceEnabled;
        NextNoteDelayValueText.SetResourceReference(
            TextBlock.ForegroundProperty,
            IsAutoAdvanceEnabled ? "PanelHeaderBrush" : "SubtleTextBrush");
    }

    private void ApplySettings(StudentModeSettings settings)
    {
        _loadingSettings = true;
        ShowNoteNamesCheckBox.IsChecked = settings.ShowNoteLabels;
        IncludeAccidentalsCheckBox.IsChecked = settings.IncludeAccidentals;
        IncludeOctavesCheckBox.IsChecked = settings.MatchOctave;
        AdaptivePracticeCheckBox.IsChecked = settings.AdaptivePractice;
        AutoAdvanceCheckBox.IsChecked = settings.AutoAdvance;
        NextNoteDelaySlider.Value = Math.Clamp(settings.NextNoteDelaySeconds, NextNoteDelaySlider.Minimum, NextNoteDelaySlider.Maximum);
        ApplyAvailablePitchClasses(settings.AvailablePitchClasses);

        _staff.ShowNoteNames = settings.ShowNoteLabels;
        _staff.IncludeAccidentals = settings.IncludeAccidentals;
        _staff.IncludeOctaves = settings.MatchOctave;
        _loadingSettings = false;
        SyncNextNoteDelayUi();
        SyncAvailableNoteCheckBoxStates();
    }

    private void NotifySettingsChanged()
    {
        if (_loadingSettings)
        {
            return;
        }

        SettingsChanged?.Invoke(new StudentModeSettings
        {
            ShowNoteLabels = ShowNoteNamesCheckBox.IsChecked == true,
            IncludeAccidentals = IncludeAccidentalsCheckBox.IsChecked == true,
            MatchOctave = IncludeOctavesCheckBox.IsChecked == true,
            AdaptivePractice = IsAdaptivePracticeEnabled,
            AutoAdvance = AutoAdvanceCheckBox.IsChecked == true,
            NextNoteDelaySeconds = NextNoteDelaySlider.Value,
            AvailablePitchClasses = GetAvailablePitchClasses().Order().ToArray()
        });
    }

    private HashSet<int> GetAvailablePitchClasses()
    {
        return GetAvailableNoteCheckBoxes()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => int.Parse(checkBox.Tag?.ToString() ?? "0"))
            .ToHashSet();
    }

    private HashSet<int> GetPlayablePitchClasses()
    {
        HashSet<int> selected = GetAvailablePitchClasses();
        if (_staff.IncludeAccidentals)
        {
            return selected;
        }

        selected.RemoveWhere(pitchClass => !IsNaturalPitchClass(pitchClass));
        return selected.Count > 0
            ? selected
            : new HashSet<int> { 0, 2, 4, 5, 7, 9, 11 };
    }

    private void BeginTrackingCurrentTarget()
    {
        _targetStartedTimestamp = Stopwatch.GetTimestamp();
        _mistakesOnCurrentTarget = 0;
        _targetResultRecorded = !IsAdaptivePracticeEnabled;
    }

    private void RecordAdaptiveResult(Note target)
    {
        if (!IsAdaptivePracticeEnabled || _targetResultRecorded)
        {
            return;
        }

        _adaptivePractice.RecordResult(
            target.PitchClass,
            _mistakesOnCurrentTarget,
            Stopwatch.GetElapsedTime(_targetStartedTimestamp));
        _targetResultRecorded = true;
    }

    private void RecordSkippedAdaptiveTarget()
    {
        if (!IsAdaptivePracticeEnabled || _targetResultRecorded || !_currentNote.HasValue)
        {
            return;
        }

        _adaptivePractice.RecordResult(
            _currentNote.Value.PitchClass,
            Math.Max(1, _mistakesOnCurrentTarget),
            Stopwatch.GetElapsedTime(_targetStartedTimestamp));
        _targetResultRecorded = true;
    }

    private void ApplyAvailablePitchClasses(IEnumerable<int>? pitchClasses)
    {
        var available = (pitchClasses ?? Enumerable.Range(0, 12)).ToHashSet();
        if (available.Count == 0)
        {
            available = Enumerable.Range(0, 12).ToHashSet();
        }

        foreach (var checkBox in GetAvailableNoteCheckBoxes())
        {
            int pitchClass = int.Parse(checkBox.Tag?.ToString() ?? "0");
            checkBox.IsChecked = available.Contains(pitchClass);
        }
    }

    private IEnumerable<CheckBox> GetAvailableNoteCheckBoxes()
    {
        yield return NoteCCheckBox;
        yield return NoteCsCheckBox;
        yield return NoteDCheckBox;
        yield return NoteDsCheckBox;
        yield return NoteECheckBox;
        yield return NoteFCheckBox;
        yield return NoteFsCheckBox;
        yield return NoteGCheckBox;
        yield return NoteGsCheckBox;
        yield return NoteACheckBox;
        yield return NoteAsCheckBox;
        yield return NoteBCheckBox;
    }

    private void SyncAvailableNoteCheckBoxStates()
    {
        bool includeAccidentals = IncludeAccidentalsCheckBox.IsChecked == true;
        foreach (var checkBox in GetAvailableNoteCheckBoxes())
        {
            int pitchClass = int.Parse(checkBox.Tag?.ToString() ?? "0");
            checkBox.IsEnabled = includeAccidentals || IsNaturalPitchClass(pitchClass);
        }
    }

    private static bool IsNaturalPitchClass(int pitchClass)
    {
        return pitchClass is 0 or 2 or 4 or 5 or 7 or 9 or 11;
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

    private static Note EvaluateDetectedNoteAgainstTarget(Note detected, Note target, bool includeOctaves)
    {
        // Wrong pitch class entirely — report as-is
        if (detected.PitchClass != target.PitchClass)
            return detected;

        // Exact match
        if (detected.MidiNote == target.MidiNote)
            return detected;

        // When octaves are displayed/enforced, treat octave mismatches as wrong answers.
        if (includeOctaves)
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
