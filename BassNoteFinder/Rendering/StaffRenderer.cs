using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BassNoteFinder.MusicTheory;

namespace BassNoteFinder.Rendering;

public class StaffRenderer
{
    private const double Ls = 14;
    private const double NoteW = 14;
    private const double NoteH = 10;
    private const double LedgerLen = 18;

    public double StaffWidth { get; set; } = 400;

    public void Render(Canvas canvas, Note note)
    {
        canvas.Children.Clear();

        double cx = StaffWidth / 2;
        double top = 50;

        for (int i = 0; i < 5; i++)
            canvas.Children.Add(new Line
            {
                X1 = 30, Y1 = top + i * Ls,
                X2 = StaffWidth - 30, Y2 = top + i * Ls,
                Stroke = Brushes.Black, StrokeThickness = 1
            });

        DrawBassClef(canvas, 32, top + 2 * Ls + 6);

        int pos = note.StaffPosition();

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

        if (note.IsSharp)
        {
            var acc = new TextBlock
            {
                Text = "\u266F", FontSize = 18,
                FontWeight = FontWeights.Bold, Foreground = Brushes.Black
            };
            Canvas.SetLeft(acc, cx - 24);
            Canvas.SetTop(acc, noteY - 12);
            canvas.Children.Add(acc);
        }

        var ellipse = new Ellipse
        {
            Width = NoteW, Height = NoteH,
            Fill = Brushes.Black,
            Stroke = Brushes.Black, StrokeThickness = 1
        };
        Canvas.SetLeft(ellipse, cx - NoteW / 2);
        Canvas.SetTop(ellipse, noteY - NoteH / 2);
        canvas.Children.Add(ellipse);
    }

    private static void DrawBassClef(Canvas canvas, double x, double y)
    {
        var tb = new TextBlock
        {
            Text = "\U0001D122",
            FontFamily = new FontFamily("Segoe UI Symbol"),
            FontSize = 52,
            Foreground = Brushes.Black
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
            Stroke = Brushes.Black, StrokeThickness = 1
        });
    }
}
