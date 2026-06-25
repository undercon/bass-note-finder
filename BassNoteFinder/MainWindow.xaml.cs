using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BassNoteFinder.Audio;
using BassNoteFinder.Gameplay;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Views;

namespace BassNoteFinder;

public partial class MainWindow : Window
{
    private const int AttackIgnoreFrames = 1;
    private const int PitchMedianWindowSize = 3;
    private const int RequiredStableDetections = 2;
    private const int RequiredLostDetections = 3;
    private const double StableCentsDriftTolerance = 35.0;
    private const double HarmonicJumpThreshold = 1.35;

    private readonly AudioCaptureService _audio;
    private readonly AppConfig _config;
    private readonly Queue<double> _recentFrequencies = new();

    private IGameMode? _activeMode;
    private int _attackIgnoreFramesRemaining;
    private int _candidateMidiNote = int.MinValue;
    private double _candidateCentsOff;
    private int _stableDetectionCount;
    private int _lostDetectionCount;
    private int? _lastResolvedMidiNote;
    private double? _lastStableFrequency;
    private bool _signalLost = true;
    private bool _loadingConfig;

    public bool ShowDeviation { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        _config = AppConfigStore.Load();
        _audio = new AudioCaptureService();
        _audio.PitchDetected += OnPitchDetected;
        _audio.PitchLost += OnPitchLost;
        _audio.ErrorOccurred += msg => Dispatcher.Invoke(() => DetectedNoteText.Text = msg);
        ApplyConfig();
        PopulateInputDevices();
        Loaded += OnLoaded;
        LocationChanged += WindowBoundsChanged;
        SizeChanged += WindowBoundsChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ShowMenu();
    }

    private void ShowMenu()
    {
        if (_activeMode != null)
        {
            _activeMode.OnDeactivate();
            _activeMode = null;
        }

        ResetDetectionTracking();
        var menu = new MenuView();
        menu.TeacherModeSelected += ShowTeacherMode;
        menu.StudentModeSelected += ShowStudentMode;
        MainContent.Content = menu;
    }

    private void ShowTeacherMode()
    {
        var view = new TeacherModeView();
        view.BackToMenuRequested += ShowMenu;
        view.IncludeOctavesChanged += (includeOctaves) => _audio.PreferHigherOctave = includeOctaves;
        _audio.PreferHigherOctave = view.IncludeOctaves;
        MainContent.Content = view;
        _activeMode = view;
        _activeMode.OnActivate();
    }

    private void ShowStudentMode()
    {
        var view = new StudentModeView();
        view.BackToMenuRequested += ShowMenu;
        view.IncludeOctavesChanged += (includeOctaves) => _audio.PreferHigherOctave = includeOctaves;
        _audio.PreferHigherOctave = view.IncludeOctaves;
        MainContent.Content = view;
        _activeMode = view;
        _activeMode.OnActivate();
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
            DetectedNoteText.Text = "No devices";
        }

        _loadingConfig = false;
    }

    private void ToggleMic_Click(object sender, RoutedEventArgs e)
    {
        if (_audio.IsCapturing)
        {
            _audio.StopCapture();
            ToggleMicBtn.Content = "Start Mic";
            DetectedNoteText.Text = "--";
        }
        else
        {
            if (_audio.StartCapture(InputCombo.SelectedIndex))
            {
                ToggleMicBtn.Content = "Stop Mic";
                DetectedNoteText.Text = "Listening...";
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

    private void HarmonicCorrectionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_loadingConfig)
        {
            return;
        }

        _config.UseHarmonicCorrection = HarmonicCorrectionCheckBox.IsChecked == true;
        AppConfigStore.Save(_config);
    }

    private void ShowDeviationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        ShowDeviation = ShowDeviationCheckBox.IsChecked == true;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _activeMode?.OnSpacePressed();
            e.Handled = true;
        }
    }

    private void OnPitchDetected(double frequency)
    {
        Dispatcher.Invoke(() =>
        {
            if (_signalLost)
            {
                _signalLost = false;
                _attackIgnoreFramesRemaining = AttackIgnoreFrames;
                ClearRecentFrequencies();
            }

            _lostDetectionCount = 0;

            if (_attackIgnoreFramesRemaining > 0)
            {
                _attackIgnoreFramesRemaining--;
                return;
            }

            AddRecentFrequency(frequency);
            double filteredFrequency = GetMedianFrequency();
            filteredFrequency = ApplyHarmonicCorrection(filteredFrequency);

            var centsOff = Note.CentsOffFromFrequency(filteredFrequency, out var detected);
            TrackCandidateDetection(detected, centsOff);

            bool isStable = _stableDetectionCount >= RequiredStableDetections;
            if (!isStable)
            {
                return;
            }

            if (_lastResolvedMidiNote == detected.MidiNote)
            {
                return;
            }

            _lastResolvedMidiNote = detected.MidiNote;
            _lastStableFrequency = filteredFrequency;

            DetectedNoteText.Text = ShowDeviation
                ? $"{detected.FullName} ({centsOff:F0}\u00A2)"
                : detected.FullName;
            DetectedNoteText.Foreground = Brushes.White;

            _activeMode?.OnNoteDetected(detected, centsOff);
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
            _signalLost = true;
            _activeMode?.OnNoteLost();
        });
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
        _attackIgnoreFramesRemaining = 0;
        _candidateMidiNote = int.MinValue;
        _candidateCentsOff = 0;
        _stableDetectionCount = 0;
        _lostDetectionCount = 0;
        _lastResolvedMidiNote = null;
        _lastStableFrequency = null;
        ClearRecentFrequencies();
    }

    private void ApplyConfig()
    {
        _loadingConfig = true;
        Width = Math.Max(_config.WindowWidth, MinWidth > 0 ? MinWidth : 1050);
        Height = Math.Max(_config.WindowHeight, MinHeight > 0 ? MinHeight : 750);

        if (_config.WindowLeft.HasValue && _config.WindowTop.HasValue)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = _config.WindowLeft.Value;
            Top = _config.WindowTop.Value;
        }

        ThresholdSlider.Value = Math.Clamp(_config.MinSignalLevel, (float)ThresholdSlider.Minimum, (float)ThresholdSlider.Maximum);
        HarmonicCorrectionCheckBox.IsChecked = _config.UseHarmonicCorrection;
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

    private void AddRecentFrequency(double frequency)
    {
        _recentFrequencies.Enqueue(frequency);
        while (_recentFrequencies.Count > PitchMedianWindowSize)
        {
            _recentFrequencies.Dequeue();
        }
    }

    private double GetMedianFrequency()
    {
        if (_recentFrequencies.Count == 0)
        {
            return 0;
        }

        var ordered = _recentFrequencies.OrderBy(x => x).ToArray();
        return ordered[ordered.Length / 2];
    }

    private double ApplyHarmonicCorrection(double frequency)
    {
        if (HarmonicCorrectionCheckBox.IsChecked != true || _lastStableFrequency is null)
        {
            return frequency;
        }

        double halfFrequency = frequency / 2.0;
        if (halfFrequency < 30)
        {
            return frequency;
        }

        double upwardJumpRatio = frequency / _lastStableFrequency.Value;
        double fullJumpDistance = Math.Abs(frequency - _lastStableFrequency.Value);
        double halfJumpDistance = Math.Abs(halfFrequency - _lastStableFrequency.Value);

        if (upwardJumpRatio >= HarmonicJumpThreshold && halfJumpDistance < fullJumpDistance)
        {
            return halfFrequency;
        }

        return frequency;
    }

    private void ClearRecentFrequencies()
    {
        _recentFrequencies.Clear();
    }

    private void WindowBoundsChanged(object? sender, EventArgs e)
    {
        if (_loadingConfig || !IsLoaded || WindowState != WindowState.Normal)
        {
            return;
        }

        _config.WindowWidth = Width;
        _config.WindowHeight = Height;
        _config.WindowLeft = Left;
        _config.WindowTop = Top;
        AppConfigStore.Save(_config);
    }

    protected override void OnClosed(EventArgs e)
    {
        _audio.Dispose();
        base.OnClosed(e);
    }
}
