using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BassNoteFinder.MusicTheory;

namespace BassNoteFinder.Rendering;

public class StaffRenderer
{
    private const double Ls = 20;
    private const double NoteW = 20;
    private const double NoteH = 14;
    private const double LedgerLen = 26;
    private const double StaffTop = 60;
    private const double StaffLeft = 30;
    private const double ColumnOffset = 60;

    private static readonly Brush StaffLineBrush = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly Brush ClefBrush = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
    private static readonly Brush NoteBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
    private static readonly Brush AccidentalBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));
    private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(0x0E, 0x63, 0x9C));
    private static readonly Brush NoteNameBrush = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD));
    private static readonly Brush PreviewBrush = new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7));

    private const int WrittenOctaveOffset = 12;
    private const int MinWrittenMidi = 40;
    private const int MaxWrittenMidi = 67;

    private static readonly int[] DiatonicToPitchClass = { 0, 2, 4, 5, 7, 9, 11 };
    private static readonly int[] PitchClassToDiatonic = { 0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6 };
    private static readonly string[] SharpNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
    private static readonly string[] FlatNames = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

    public double StaffWidth { get; set; } = 500;
    public bool ShowNoteNames { get; set; }
    public bool IncludeAccidentals { get; set; }

    private double CenterX => StaffWidth / 2;
    private double FlatX => CenterX - ColumnOffset;
    private double SharpX => CenterX + ColumnOffset;

    public enum AccidentalMode { Natural, Sharp, Flat }

    public static (Note note, AccidentalMode mode)? NoteFromPoint(double x, double y, bool includeAccidentals)
    {
        double pos = (StaffTop + 4 * Ls - y) / (Ls / 2.0);
        int posRounded = (int)Math.Round(pos);
        Note? natural = NoteFromStaffPosition(posRounded);
        if (natural == null) return null;

        double centerX = 250;
        double flatX = centerX - ColumnOffset;
        double sharpX = centerX + ColumnOffset;

        if (includeAccidentals)
        {
            double distCenter = Math.Abs(x - centerX);
            double distFlat = Math.Abs(x - flatX);
            double distSharp = Math.Abs(x - sharpX);

            if (distFlat <= distCenter && distFlat <= distSharp)
            {
                int flatMidi = natural.Value.MidiNote - 1;
                if (IsValidSoundingMidi(flatMidi)) return (new Note(flatMidi), AccidentalMode.Flat);
            }
            else if (distSharp <= distCenter && distSharp <= distFlat)
            {
                int sharpMidi = natural.Value.MidiNote + 1;
                if (IsValidSoundingMidi(sharpMidi)) return (new Note(sharpMidi), AccidentalMode.Sharp);
            }
        }

        return (natural.Value, AccidentalMode.Natural);
    }

    private static bool IsValidSoundingMidi(int midi) => midi >= 28 && midi <= 55;

    private static Note? NoteFromStaffPosition(int pos)
    {
        int temp = pos + 4;
        int diatonicClass = ((temp % 7) + 7) % 7;
        int octave = (int)Math.Floor((double)temp / 7) + 2;

        int pitchClass = DiatonicToPitchClass[diatonicClass];
        int writtenMidi = (octave + 1) * 12 + pitchClass;

        if (writtenMidi < MinWrittenMidi || writtenMidi > MaxWrittenMidi) return null;
        return new Note(writtenMidi - WrittenOctaveOffset);
    }

    private static int WrittenStaffPosition(Note note)
    {
        int writtenMidi = note.MidiNote + WrittenOctaveOffset;
        int octave = writtenMidi / 12 - 1;
        int pitchClass = (writtenMidi % 12 + 12) % 12;
        int diatonicClass = PitchClassToDiatonic[pitchClass];
        return (diatonicClass - 4) + 7 * (octave - 2);
    }

    private double GetNoteX(AccidentalMode mode)
    {
        return mode switch
        {
            AccidentalMode.Flat => FlatX,
            AccidentalMode.Sharp => SharpX,
            _ => CenterX
        };
    }

    public void Render(Canvas canvas, Note note, AccidentalMode mode = AccidentalMode.Natural)
    {
        canvas.Children.Clear();
        DrawStaff(canvas);
        DrawNote(canvas, note, mode);
    }

    public void RenderEmpty(Canvas canvas)
    {
        canvas.Children.Clear();
        DrawStaff(canvas);
    }

    public void RenderEmptyWithPreview(Canvas canvas, Note previewNote, AccidentalMode mode)
    {
        canvas.Children.Clear();
        DrawStaff(canvas);
        DrawPreviewNote(canvas, previewNote, mode);
    }

    public void RenderWithPreview(Canvas canvas, Note note, AccidentalMode noteMode, Note previewNote, AccidentalMode previewMode)
    {
        canvas.Children.Clear();
        DrawStaff(canvas);
        DrawNote(canvas, note, noteMode);
        DrawPreviewNote(canvas, previewNote, previewMode);
    }

    private void DrawPreviewNote(Canvas canvas, Note note, AccidentalMode mode)
    {
        double cx = GetNoteX(mode);
        DrawNoteAt(canvas, note, cx, mode, 0.3, true);
    }

    private void DrawNote(Canvas canvas, Note note, AccidentalMode mode)
    {
        double cx = GetNoteX(mode);
        DrawNoteAt(canvas, note, cx, mode, 1.0, false);
    }

    private void DrawNoteAt(Canvas canvas, Note note, double cx, AccidentalMode mode, double opacity, bool isPreview)
    {
        double top = StaffTop;
        int pos = WrittenStaffPosition(note);
        double noteY = top + 4 * Ls - pos * (Ls / 2.0);

        if (pos < 0)
        {
            int firstLedger = -2;
            int lastLedger = pos % 2 == 0 ? pos : pos - 1;
            for (int lp = firstLedger; lp >= lastLedger; lp -= 2)
            {
                double ly = top + 4 * Ls - lp * (Ls / 2.0);
                DrawLedger(canvas, cx, ly);
            }
        }
        else if (pos > 8)
        {
            int firstLedger = 10;
            int lastLedger = pos % 2 == 0 ? pos : pos + 1;
            for (int lp = firstLedger; lp <= lastLedger; lp += 2)
            {
                double ly = top + 4 * Ls - lp * (Ls / 2.0);
                DrawLedger(canvas, cx, ly);
            }
        }

        if (mode == AccidentalMode.Sharp)
        {
            var acc = new TextBlock
            {
                Text = "\u266F", FontSize = 18,
                FontWeight = FontWeights.Bold, Foreground = AccidentalBrush,
                Opacity = opacity
            };
            Canvas.SetLeft(acc, cx - 24);
            Canvas.SetTop(acc, noteY - 12);
            canvas.Children.Add(acc);
        }
        else if (mode == AccidentalMode.Flat)
        {
            var acc = new TextBlock
            {
                Text = "\u266D", FontSize = 18,
                FontWeight = FontWeights.Bold, Foreground = AccidentalBrush,
                Opacity = opacity
            };
            Canvas.SetLeft(acc, cx - 24);
            Canvas.SetTop(acc, noteY - 12);
            canvas.Children.Add(acc);
        }

        if (!isPreview)
        {
            var highlight = new Ellipse
            {
                Width = NoteW + 10, Height = NoteH + 10,
                Fill = HighlightBrush,
                Opacity = 0.4
            };
            Canvas.SetLeft(highlight, cx - (NoteW + 10) / 2);
            Canvas.SetTop(highlight, noteY - (NoteH + 10) / 2);
            canvas.Children.Add(highlight);
        }

        var ellipse = new Ellipse
        {
            Width = NoteW, Height = NoteH,
            Fill = isPreview ? PreviewBrush : NoteBrush,
            Stroke = isPreview ? PreviewBrush : NoteBrush,
            StrokeThickness = 1,
            Opacity = opacity
        };
        Canvas.SetLeft(ellipse, cx - NoteW / 2);
        Canvas.SetTop(ellipse, noteY - NoteH / 2);
        canvas.Children.Add(ellipse);

        if (ShowNoteNames && !isPreview)
        {
            var writtenNote = new Note(note.MidiNote + WrittenOctaveOffset);
            int pc = (writtenNote.MidiNote % 12 + 12) % 12;
            string name = mode switch
            {
                AccidentalMode.Flat => $"{FlatNames[pc]}{writtenNote.Octave}",
                AccidentalMode.Sharp => $"{SharpNames[pc]}{writtenNote.Octave}",
                _ => writtenNote.FullName
            };
            var nameTb = new TextBlock
            {
                Text = name,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = NoteNameBrush
            };
            Canvas.SetLeft(nameTb, cx + NoteW / 2 + 8);
            Canvas.SetTop(nameTb, noteY - 10);
            canvas.Children.Add(nameTb);
        }
    }

    private void DrawStaff(Canvas canvas)
    {
        double top = StaffTop;

        for (int i = 0; i < 5; i++)
            canvas.Children.Add(new Line
            {
                X1 = StaffLeft, Y1 = top + i * Ls,
                X2 = StaffWidth - StaffLeft, Y2 = top + i * Ls,
                Stroke = StaffLineBrush, StrokeThickness = 1
            });

        DrawBassClef(canvas, 32, top + 2 * Ls + 6);
    }

    private static void DrawBassClef(Canvas canvas, double x, double y)
    {
        var tb = new TextBlock
        {
            Text = "\U0001D122",
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontSize = 52,
            Foreground = ClefBrush
        };
        Canvas.SetLeft(tb, x - 28);
        Canvas.SetTop(tb, y - 42);
        canvas.Children.Add(tb);
    }

    private static void DrawLedger(Canvas canvas, double x, double y)
    {
        canvas.Children.Add(new Line
        {
            X1 = x - LedgerLen / 2, Y1 = y,
            X2 = x + LedgerLen / 2, Y2 = y,
            Stroke = StaffLineBrush, StrokeThickness = 1
        });
    }
}
