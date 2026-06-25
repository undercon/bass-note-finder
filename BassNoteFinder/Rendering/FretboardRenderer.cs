using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BassNoteFinder.MusicTheory;

namespace BassNoteFinder.Rendering;

public class FretboardRenderer
{
    private const double StringSpacing = 34;
    private const double FretSpacing = 48;
    private const double NutWidth = 8;
    private const double MarkerSize = 10;
    private const int NumFrets = 12;

    private static readonly int[] OpenNotes = { 43, 38, 33, 28 };

    public double TotalWidth => NutWidth + (NumFrets + 1) * FretSpacing + 30;
    public double TotalHeight => 5 * StringSpacing + 40;

    public void Render(Canvas canvas, Note? targetNote = null, Color? highlightColor = null, Note? staffReferenceNote = null)
    {
        canvas.Children.Clear();

        double x0 = 30;
        double y0 = 30;

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
                Fill = Brushes.DimGray
            });
            Canvas.SetLeft(canvas.Children[^1] as Ellipse, mx - MarkerSize / 2);
            Canvas.SetTop(canvas.Children[^1] as Ellipse, my - MarkerSize / 2);
        }

        double my12 = y0 + 0.75 * StringSpacing;
        double mx12 = nutEnd + 11.5 * FretSpacing;
        canvas.Children.Add(new Ellipse
        {
            Width = MarkerSize, Height = MarkerSize,
            Fill = Brushes.DimGray
        });
        Canvas.SetLeft(canvas.Children[^1] as Ellipse, mx12 - MarkerSize / 2);
        Canvas.SetTop(canvas.Children[^1] as Ellipse, my12 - MarkerSize / 2);

        my12 = y0 + 2.25 * StringSpacing;
        canvas.Children.Add(new Ellipse
        {
            Width = MarkerSize, Height = MarkerSize,
            Fill = Brushes.DimGray
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
            var (str, fret) = FindNotePosition(targetNote.Value, staffReferenceNote);
            if (str >= 0)
            {
                double sx = nutEnd + (fret - 0.5) * FretSpacing;
                double sy = y0 + str * StringSpacing;
                if (fret == 0)
                {
                    sx = nutEnd + 0.18 * FretSpacing;
                }

                var color = highlightColor ?? Color.FromRgb(0, 180, 255);
                canvas.Children.Add(new Ellipse
                {
                    Width = 20, Height = 20,
                    Fill = new SolidColorBrush(color),
                    Opacity = 0.8
                });
                Canvas.SetLeft(canvas.Children[^1] as Ellipse, sx - 10);
                Canvas.SetTop(canvas.Children[^1] as Ellipse, sy - 10);

                if (fret > 0)
                {
                    var ftb = new TextBlock
                    {
                        Text = fret.ToString(),
                        FontSize = 10,
                        Foreground = Brushes.White
                    };
                    Canvas.SetLeft(ftb, sx - 5);
                    Canvas.SetTop(ftb, sy + 12);
                    canvas.Children.Add(ftb);
                }
                else
                {
                    var ftb = new TextBlock
                    {
                        Text = "open",
                        FontSize = 9,
                        Foreground = Brushes.LightGray
                    };
                    Canvas.SetLeft(ftb, sx - 13);
                    Canvas.SetTop(ftb, sy + 12);
                    canvas.Children.Add(ftb);
                }
            }
        }
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
