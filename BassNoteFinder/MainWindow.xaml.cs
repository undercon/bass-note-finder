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
    private const double SignalIndicatorCeiling = 0.06;

    private readonly AudioCaptureService _audio;
    private readonly StableNoteDetectionPipeline _detectionPipeline;
    private readonly AppConfig _config;

    private IGameMode? _activeMode;
    private bool _loadingConfig;

    public bool ShowDeviation { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        _config = AppConfigStore.Load();
        _audio = new AudioCaptureService();
        _detectionPipeline = new StableNoteDetectionPipeline();
        _audio.PitchDetected += OnPitchDetected;
        _audio.PitchLost += OnPitchLost;
        _audio.SignalLevelMeasured += OnSignalLevelMeasured;
        _audio.ErrorOccurred += msg => Dispatcher.Invoke(() => DetectedNoteText.Text = msg);
        _detectionPipeline.StableNoteDetected += OnStableNoteDetected;
        _detectionPipeline.StableNoteLost += OnStableNoteLost;
        ApplyConfig();
        PopulateInputDevices();
        Loaded += OnLoaded;
        LocationChanged += WindowBoundsChanged;
        SizeChanged += WindowBoundsChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ShowMenu();
        RestoreMicCaptureState();
    }

    private void ShowMenu()
    {
        if (_activeMode != null)
        {
            _activeMode.OnDeactivate();
            _activeMode = null;
        }

        _detectionPipeline.Reset();
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
            SetMicUiState(false);
            PersistMicStartupPreference(false);
        }
        else
        {
            if (_audio.StartCapture(InputCombo.SelectedIndex))
            {
                SetMicUiState(true);
                PersistMicStartupPreference(true);
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
                SetMicUiState(false);
                PersistMicStartupPreference(false);
            }
            else
            {
                SetMicUiState(true);
            }
        }
    }

    private void RestoreMicCaptureState()
    {
        if (!_config.StartMicOnLaunch || InputCombo.Items.Count == 0)
        {
            SetMicUiState(false);
            return;
        }

        if (_audio.StartCapture(InputCombo.SelectedIndex))
        {
            SetMicUiState(true);
        }
        else
        {
            SetMicUiState(false);
            PersistMicStartupPreference(false);
        }
    }

    private void SetMicUiState(bool isCapturing)
    {
        ToggleMicBtn.Content = isCapturing ? "Stop Mic" : "Start Mic";
        if (!isCapturing)
        {
            DetectedNoteText.Text = "--";
            _detectionPipeline.Reset();
            UpdateSignalIndicator(0);
        }
        else if (DetectedNoteText.Text == "--")
        {
            DetectedNoteText.Text = "Listening...";
        }
    }

    private void PersistMicStartupPreference(bool startMicOnLaunch)
    {
        _config.StartMicOnLaunch = startMicOnLaunch;
        AppConfigStore.Save(_config);
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
        _detectionPipeline.UseHarmonicCorrection = HarmonicCorrectionCheckBox.IsChecked == true;
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
        _detectionPipeline.ProcessFrequency(frequency);
    }

    private void OnPitchLost()
    {
        _detectionPipeline.ProcessLost();
    }

    private void OnStableNoteDetected(Note detected, double centsOff)
    {
        Dispatcher.Invoke(() =>
        {
            DetectedNoteText.Text = ShowDeviation
                ? $"{detected.FullName} ({centsOff:F0}\u00A2)"
                : detected.FullName;
            DetectedNoteText.Foreground = Brushes.White;
            _activeMode?.OnNoteDetected(detected, centsOff);
        });
    }

    private void OnStableNoteLost()
    {
        Dispatcher.Invoke(() => _activeMode?.OnNoteLost());
    }

    private void OnSignalLevelMeasured(float rms)
    {
        Dispatcher.Invoke(() => UpdateSignalIndicator(rms));
    }

    private void UpdateThresholdDisplay(double value)
    {
        ThresholdValueText.Text = value.ToString("0.000");
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
        _detectionPipeline.UseHarmonicCorrection = _config.UseHarmonicCorrection;
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

    private void UpdateSignalIndicator(double rms)
    {
        if (SignalDot == null)
        {
            return;
        }

        if (!_audio.IsCapturing || rms <= 0.0005)
        {
            SignalDot.Fill = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
            SignalDot.Stroke = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
            SignalDot.Opacity = 0.55;
            return;
        }

        double level = Math.Clamp(rms / SignalIndicatorCeiling, 0.0, 1.0);
        byte red = (byte)(0x66 + level * (0x4F));
        byte green = (byte)(0x66 + level * (0x99));
        byte blue = (byte)(0x66 + level * (0x2B));

        SignalDot.Fill = new SolidColorBrush(Color.FromRgb(red, green, blue));
        SignalDot.Stroke = new SolidColorBrush(Color.FromRgb(0xC8, 0xD4, 0xCC));
        SignalDot.Opacity = 0.55 + 0.45 * level;
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
        _config.StartMicOnLaunch = _audio.IsCapturing;
        AppConfigStore.Save(_config);
        _audio.Dispose();
        base.OnClosed(e);
    }
}
