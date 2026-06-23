using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using BassNoteFinder.Audio;
using BassNoteFinder.MusicTheory;
using BassNoteFinder.Rendering;

namespace BassNoteFinder;

public partial class MainWindow : Window
{
    private readonly NoteGenerator _generator = new(28, 67);
    private readonly StaffRenderer _staff = new();
    private readonly AudioCaptureService _audio;
    private Note _currentNote = new(40);
    private int _correct, _total;
    private int _cooldown;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        _audio = new AudioCaptureService();
        _audio.PitchDetected += OnPitchDetected;
        _audio.ErrorOccurred += msg => Dispatcher.Invoke(() => StatusText.Text = msg);
        PopulateInputDevices();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ShowNote(_generator.RandomNote());
    }

    private void PopulateInputDevices()
    {
        InputCombo.Items.Clear();
        var devices = AudioCaptureService.GetInputDevices();
        foreach (var d in devices)
            InputCombo.Items.Add(d);
        if (InputCombo.Items.Count > 0)
            InputCombo.SelectedIndex = 0;
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
            _audio.StartCapture(InputCombo.SelectedIndex);
            ToggleMicBtn.Content = "Stop Mic";
            StatusText.Text = "Listening... Play the note!";
        }
    }

    private void InputCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_audio.IsCapturing)
        {
            _audio.StopCapture();
            _audio.StartCapture(InputCombo.SelectedIndex);
        }
    }

    private void NewNote_Click(object sender, RoutedEventArgs e)
    {
        ShowNote(_generator.RandomNote());
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
            if (_cooldown > 0)
            {
                _cooldown--;
                if (_cooldown <= 0)
                    DetectedNoteText.Text = "--";
                return;
            }

            var centsOff = Note.CentsOffFromFrequency(frequency, out var detected);
            DetectedNoteText.Text = $"{detected.FullName} ({centsOff:F0}\u00A2)";

            var tolerance = 30.0;
            if (detected.MidiNote == _currentNote.MidiNote && Math.Abs(centsOff) < tolerance)
            {
                _correct++;
                DetectedNoteText.Foreground = Brushes.Lime;
                StatusText.Text = $"Correct! That was {_currentNote.FullName} \u2713";
                _total++;
                UpdateScore();
                ShowNote(_generator.RandomNote());
            }
            else if (detected.MidiNote != _currentNote.MidiNote && Math.Abs(centsOff) < tolerance)
            {
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

    private void UpdateScore()
    {
        ScoreText.Text = $"{_correct} / {_total}";
    }

    protected override void OnClosed(EventArgs e)
    {
        _audio.Dispose();
        base.OnClosed(e);
    }
}
