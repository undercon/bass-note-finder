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

    private readonly NoteGenerator _generator = new(28, 55);
    private readonly StaffRenderer _staff = new();
    private readonly FretboardRenderer _fretboardRenderer = new();
    private readonly DispatcherTimer _flashTimer;

    private Note? _currentNote;
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
        StatusText.Text = "Click the staff to place a note, or press Random.";
    }

    public void OnDeactivate()
    {
        _flashTimer.Stop();
    }

    public void OnNoteDetected(Note note, double centsOff)
    {
        if (_currentNote == null) return;
        Note target = _currentNote.Value;

        if (note.MidiNote == target.MidiNote)
        {
            SetFretboardState(FretboardState.CelebratingCorrect);
            StatusText.Text = $"Correct! That was {target.FullName} \u2713";
            _flashTimer.Start();
        }
        else
        {
            SetFretboardState(FretboardState.FlashingWrong, note);
            StatusText.Text = $"Not quite. You played {note.FullName}.";
            _flashTimer.Start();
        }
    }

    public void OnNoteLost() { }

    public void OnSpacePressed()
    {
        PickRandomNote();
    }

    private void TeacherModeView_Loaded(object sender, RoutedEventArgs e)
    {
        _staff.StaffWidth = StaffCanvas.ActualWidth > 100 ? StaffCanvas.ActualWidth - 20 : 380;
        _staff.RenderEmpty(StaffCanvas);
    }

    private void StaffCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        double y = e.GetPosition(StaffCanvas).Y;
        Note? note = StaffRenderer.NoteFromY(y);
        if (note != null)
        {
            SelectNote(note.Value);
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

    private void PickRandomNote()
    {
        SelectNote(_generator.RandomNote());
    }

    private void SelectNote(Note note)
    {
        _flashTimer.Stop();
        _currentNote = note;
        SetFretboardState(FretboardState.Hidden);

        _staff.StaffWidth = StaffCanvas.ActualWidth > 100 ? StaffCanvas.ActualWidth - 20 : 380;
        _staff.Render(StaffCanvas, note);

        StatusText.Text = $"Find this note on your bass: {note.FullName}";
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
                OverlayIcon.Text = "\u2713";
                OverlayIcon.Foreground = Brushes.LimeGreen;
                OverlayText.Text = "Correct!";
                OverlayText.Foreground = Brushes.LimeGreen;
                break;
        }
    }

    private void FlashTimer_Tick(object? sender, EventArgs e)
    {
        _flashTimer.Stop();
        SetFretboardState(FretboardState.Hidden);
    }
}
