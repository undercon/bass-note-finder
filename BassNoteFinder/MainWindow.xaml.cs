using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;

namespace BassNoteFinder;

public partial class MainWindow : Window
{
    private const int RequiredStableDetections = 2;
    private const int RequiredLostDetections = 3;
    private const double StableCentsDriftTolerance = 35.0;
    private readonly NoteGenerator _generator = new(28, 67);
    private readonly StaffRenderer _staff = new();
    private readonly AudioCaptureService _audio;
    private readonly AppConfig _config;
    private Note _currentNote = new(40);
    private int _correct, _total;
    private int _cooldown;
    private int _candidateMidiNote = int.MinValue;
    private double _candidateCentsOff;
    private int _stableDetectionCount;
    private int _lostDetectionCount;
    private int? _lastScoredMidiNote;
    private bool _loadingConfig;

    public MainWindow()
    {
        InitializeComponent();
        _config = AppConfigStore.Load();
        _audio = new AudioCaptureService();
        _audio.PitchDetected += OnPitchDetected;
        _audio.PitchLost += OnPitchLost;
        _audio.ErrorOccurred += msg => Dispatcher.Invoke(() => StatusText.Text = msg);
        ApplyConfig();
        PopulateInputDevices();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ShowNote(_generator.RandomNote());
    }

    private void PopulateInputDevices()
    {
        _loadingConfig = true;
        InputCombo.Items.Clear();
        var devices = AudioCaptureService.GetInputDevices();
        foreach (var d in devices)
            InputCombo.Items.Add(d);

        if (InputCombo.Items.Count > 0)
        {
            int selectedIndex = 0;
            if (!string.IsNullOrWhiteSpace(_config.SelectedInputDevice))
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    if (devices[i] == _config.SelectedInputDevice)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }

            InputCombo.SelectedIndex = selectedIndex;
            ToggleMicBtn.IsEnabled = true;
        }
        else
        {
            ToggleMicBtn.IsEnabled = false;
            StatusText.Text = "No audio input devices found.";
        }

        _loadingConfig = false;
    }

    private void ToggleMic_Click(object sender, RoutedEventArgs e)
    {
        if (_audio.IsCapturing)
        {
            _audio.StopCapture();
            ToggleMicBtn.Content = "Start Mic";
            StatusText.Text = "Microphone stopped.";
        }
        else
        {
            if (_audio.StartCapture(InputCombo.SelectedIndex))
            {
                ToggleMicBtn.Content = "Stop Mic";
                StatusText.Text = "Listening... Play the note!";
            }
        }
    }

    private void InputCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        PersistSelectedInputDevice();

        if (_audio.IsCapturing)
        {
            _audio.StopCapture();
            if (!_audio.StartCapture(InputCombo.SelectedIndex))
            {
                ToggleMicBtn.Content = "Start Mic";
            }
        }
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        ShowNote(_generator.RandomNote());
    }

    private void ThresholdSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_audio == null)
        {
            return;
        }

        _audio.MinSignalLevel = (float)e.NewValue;
        UpdateThresholdDisplay(e.NewValue);

        if (_loadingConfig)
        {
            return;
        }

        _config.MinSignalLevel = (float)e.NewValue;
        AppConfigStore.Save(_config);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            ShowNote(_generator.RandomNote());
            e.Handled = true;
        }
    }

    private void ShowNote(Note note)
    {
        _currentNote = note;
        _cooldown = 3;

        _staff.StaffWidth = StaffCanvas.ActualWidth > 100 ? StaffCanvas.ActualWidth - 20 : 380;
        _staff.Render(StaffCanvas, note);

        var fretRenderer = new FretboardRenderer();
        fretRenderer.Render(FretboardCanvas, note);

        StatusText.Text = $"Find this note on your bass: {note.FullName}";
        DetectedNoteText.Text = "--";
        DetectedNoteText.Foreground = Brushes.White;
    }

    private void OnPitchDetected(double frequency)
    {
        Dispatcher.Invoke(() =>
        {
            _lostDetectionCount = 0;

            if (_cooldown > 0)
            {
                _cooldown--;
                if (_cooldown <= 0)
                    DetectedNoteText.Text = "--";
                return;
            }

            var centsOff = Note.CentsOffFromFrequency(frequency, out var detected);
            TrackCandidateDetection(detected, centsOff);

            bool isStable = _stableDetectionCount >= RequiredStableDetections;
            DetectedNoteText.Text = $"{detected.FullName} ({centsOff:F0}\u00A2)";
            DetectedNoteText.Foreground = isStable ? Brushes.White : Brushes.LightGoldenrodYellow;

            if (!isStable || _lastScoredMidiNote == detected.MidiNote)
            {
                return;
            }

            var tolerance = 30.0;
            if (detected.MidiNote == _currentNote.MidiNote && Math.Abs(centsOff) < tolerance)
            {
                _lastScoredMidiNote = detected.MidiNote;
                _correct++;
                DetectedNoteText.Foreground = Brushes.Lime;
                StatusText.Text = $"Correct! That was {_currentNote.FullName} \u2713";
                _total++;
                UpdateScore();
                ShowNote(_generator.RandomNote());
            }
            else if (detected.MidiNote != _currentNote.MidiNote && Math.Abs(centsOff) < tolerance)
            {
                _lastScoredMidiNote = detected.MidiNote;
                DetectedNoteText.Foreground = Brushes.OrangeRed;
                StatusText.Text = $"Not quite. You played {detected.FullName}, looking for {_currentNote.FullName}";
                _total++;
                UpdateScore();
            }
            else
            {
                DetectedNoteText.Foreground = Brushes.Yellow;
            }
        });
    }

    private void OnPitchLost()
    {
        Dispatcher.Invoke(() =>
        {
            _lostDetectionCount++;
            if (_lostDetectionCount < RequiredLostDetections)
            {
                return;
            }

            ResetDetectionTracking();

            if (_cooldown <= 0)
            {
                DetectedNoteText.Text = "--";
                DetectedNoteText.Foreground = Brushes.White;
            }
        });
    }

    private void UpdateScore()
    {
        ScoreText.Text = $"{_correct} / {_total}";
    }

    private void UpdateThresholdDisplay(double value)
    {
        ThresholdValueText.Text = value.ToString("0.000");
    }

    private void TrackCandidateDetection(Note detected, double centsOff)
    {
        if (detected.MidiNote == _candidateMidiNote && Math.Abs(centsOff - _candidateCentsOff) <= StableCentsDriftTolerance)
        {
            _stableDetectionCount++;
        }
        else
        {
            _candidateMidiNote = detected.MidiNote;
            _candidateCentsOff = centsOff;
            _stableDetectionCount = 1;
        }
    }

    private void ResetDetectionTracking()
    {
        _candidateMidiNote = int.MinValue;
        _candidateCentsOff = 0;
        _stableDetectionCount = 0;
        _lostDetectionCount = 0;
        _lastScoredMidiNote = null;
    }

    private void ApplyConfig()
    {
        _loadingConfig = true;
        ThresholdSlider.Value = Math.Clamp(_config.MinSignalLevel, (float)ThresholdSlider.Minimum, (float)ThresholdSlider.Maximum);
        UpdateThresholdDisplay(ThresholdSlider.Value);
        _audio.MinSignalLevel = (float)ThresholdSlider.Value;
        _loadingConfig = false;
    }

    private void PersistSelectedInputDevice()
    {
        if (_loadingConfig)
        {
            return;
        }

        _config.SelectedInputDevice = InputCombo.SelectedItem as string ?? string.Empty;
        AppConfigStore.Save(_config);
    }

    protected override void OnClosed(EventArgs e)
    {
        _audio.Dispose();
        base.OnClosed(e);
    }
}
