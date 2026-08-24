using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BassNoteFinder.MusicTheory;

namespace BassNoteFinder.Rendering;

public class FretboardRenderer
{
    private const double StringSpacing = 42;
    private const double FretSpacing = 48;
    private const double NutWidth = 8;
    private const double MarkerSize = 10;
    private const int NumFrets = 12;
    private const double BoardLeft = 30;
    private const double BoardTop = 45;

    private static readonly int[] OpenNotes = { 43, 38, 33, 28 };

    public double TotalWidth => NutWidth + (NumFrets + 1) * FretSpacing + 30;
    public double TotalHeight => 5 * StringSpacing + 40;

    public void Render(Canvas canvas, Note? targetNote = null, Color? highlightColor = null, Note? staffReferenceNote = null)
    {
        canvas.Children.Clear();

        double x0 = BoardLeft;
        double y0 = BoardTop;

        double nutEnd = x0 + NutWidth;

        for (int s = 0; s < 4; s++)
        {
            double y = y0 + s * StringSpacing;
            var line = new Line
            {
                X1 = nutEnd, Y1 = y,
                X2 = nutEnd + NumFrets * FretSpacing, Y2 = y,
                Stroke = Brushes.Silver, StrokeThickness = 1.5
            };
            canvas.Children.Add(line);
        }

        var nut = new Line
        {
            X1 = nutEnd, Y1 = y0,
            X2 = nutEnd, Y2 = y0 + 3 * StringSpacing,
            Stroke = Brushes.White, StrokeThickness = NutWidth
        };
        canvas.Children.Add(nut);

        for (int f = 1; f <= NumFrets; f++)
        {
            double x = nutEnd + f * FretSpacing;
            var line = new Line
            {
                X1 = x, Y1 = y0,
                X2 = x, Y2 = y0 + 3 * StringSpacing,
                Stroke = Brushes.Gray, StrokeThickness = 1.5
            };
            canvas.Children.Add(line);
        }

        int[] markerFrets = { 3, 5, 7, 9 };
        foreach (int mf in markerFrets)
        {
            double mx = nutEnd + (mf - 0.5) * FretSpacing;
            double my = y0 + 1.5 * StringSpacing;
            canvas.Children.Add(new Ellipse
            {
                Width = MarkerSize, Height = MarkerSize,
                Fill = new SolidColorBrush(Color.FromRgb(0xD8, 0xB5, 0x78))
            });
            Canvas.SetLeft(canvas.Children[^1] as Ellipse, mx - MarkerSize / 2);
            Canvas.SetTop(canvas.Children[^1] as Ellipse, my - MarkerSize / 2);
        }

        double my12 = y0 + 0.75 * StringSpacing;
        double mx12 = nutEnd + 11.5 * FretSpacing;
        canvas.Children.Add(new Ellipse
        {
            Width = MarkerSize, Height = MarkerSize,
            Fill = new SolidColorBrush(Color.FromRgb(0xD8, 0xB5, 0x78))
        });
        Canvas.SetLeft(canvas.Children[^1] as Ellipse, mx12 - MarkerSize / 2);
        Canvas.SetTop(canvas.Children[^1] as Ellipse, my12 - MarkerSize / 2);

        my12 = y0 + 2.25 * StringSpacing;
        canvas.Children.Add(new Ellipse
        {
            Width = MarkerSize, Height = MarkerSize,
            Fill = new SolidColorBrush(Color.FromRgb(0xD8, 0xB5, 0x78))
        });
        Canvas.SetLeft(canvas.Children[^1] as Ellipse, mx12 - MarkerSize / 2);
        Canvas.SetTop(canvas.Children[^1] as Ellipse, my12 - MarkerSize / 2);

        var tb = new TextBlock
        {
            Text = "G", FontSize = 11, Foreground = Brushes.White
        };
        Canvas.SetLeft(tb, x0 - 18);
        Canvas.SetTop(tb, y0 - 8);
        canvas.Children.Add(tb);

        tb = new TextBlock
        {
            Text = "D", FontSize = 11, Foreground = Brushes.White
        };
        Canvas.SetLeft(tb, x0 - 18);
        Canvas.SetTop(tb, y0 + StringSpacing - 8);
        canvas.Children.Add(tb);

        tb = new TextBlock
        {
            Text = "A", FontSize = 11, Foreground = Brushes.White
        };
        Canvas.SetLeft(tb, x0 - 18);
        Canvas.SetTop(tb, y0 + 2 * StringSpacing - 8);
        canvas.Children.Add(tb);

        tb = new TextBlock
        {
            Text = "E", FontSize = 11, Foreground = Brushes.White
        };
        Canvas.SetLeft(tb, x0 - 18);
        Canvas.SetTop(tb, y0 + 3 * StringSpacing - 8);
        canvas.Children.Add(tb);

        if (targetNote.HasValue)
        {
            DrawNoteMarker(canvas, targetNote.Value, highlightColor ?? Color.FromRgb(0, 180, 255), staffReferenceNote, "✓");
        }
    }

    public void RenderComparison(
        Canvas canvas,
        Note targetNote,
        Color targetColor,
        Note playedNote,
        Color playedColor,
        Note? staffReferenceNote = null)
    {
        Render(canvas);
        DrawNoteMarker(canvas, targetNote, targetColor, staffReferenceNote, "T");
        DrawNoteMarker(canvas, playedNote, playedColor, staffReferenceNote, "×");
    }

    private static void DrawNoteMarker(Canvas canvas, Note note, Color color, Note? staffReferenceNote, string markerText)
    {
        var (str, fret) = FindNotePosition(note, staffReferenceNote);
        if (str < 0)
        {
            return;
        }

        double nutEnd = BoardLeft + NutWidth;
        double sx = nutEnd + (fret - 0.5) * FretSpacing;
        double sy = BoardTop + str * StringSpacing;
        if (fret == 0)
        {
            sx = nutEnd + 0.18 * FretSpacing;
        }

        const double markerDiameter = 30;
        canvas.Children.Add(new Ellipse
        {
            Width = markerDiameter,
            Height = markerDiameter,
            Fill = new SolidColorBrush(color),
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Opacity = 0.96
        });
        Canvas.SetLeft(canvas.Children[^1] as Ellipse, sx - (markerDiameter / 2));
        Canvas.SetTop(canvas.Children[^1] as Ellipse, sy - (markerDiameter / 2));

        var markerLabel = new TextBlock
        {
            Text = markerText,
            FontSize = markerText == "×" ? 20 : 15,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            Width = markerDiameter,
            TextAlignment = TextAlignment.Center
        };
        Canvas.SetLeft(markerLabel, sx - (markerDiameter / 2));
        Canvas.SetTop(markerLabel, sy - 11);
        canvas.Children.Add(markerLabel);

        var fretLabel = new TextBlock
        {
            Text = fret > 0 ? fret.ToString() : "open",
            FontSize = fret > 0 ? 10 : 9,
            Foreground = fret > 0 ? Brushes.White : Brushes.LightGray
        };
        Canvas.SetLeft(fretLabel, fret > 0 ? sx - 5 : sx - 13);
        Canvas.SetTop(fretLabel, sy + 18);
        canvas.Children.Add(fretLabel);
    }

    public static (int stringIndex, int fret) FindNotePosition(Note note, Note? staffReferenceNote = null)
    {
        int midi = note.MidiNote;
        if (staffReferenceNote.HasValue)
        {
            midi = Note.ClosestPitchClassToReference(note, staffReferenceNote.Value).MidiNote;
        }

        var candidates = new List<(int stringIndex, int fret)>();

        for (int s = 0; s < 4; s++)
        {
            int fret = midi - OpenNotes[s];
            if (fret >= 0 && fret <= NumFrets)
            {
                candidates.Add((s, fret));
            }
        }

        if (candidates.Count == 0)
        {
            return (-1, 99);
        }

        if (candidates.Any(c => c.fret == 5))
        {
            return candidates.First(c => c.fret == 5);
        }

        return candidates
            .OrderBy(c => c.fret)
            .ThenByDescending(c => c.stringIndex)
            .First();
    }
}
