using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BassNoteFinder;

namespace VisualReviewHarness;

internal static class Program
{
    private const int DefaultWidth = 1796;
    private const int DefaultHeight = 904;

    [STAThread]
    private static int Main(string[] args)
    {
        HarnessOptions options = HarnessOptions.Parse(args);
        Directory.CreateDirectory(options.OutputDirectory);

        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        var captures = new List<CaptureResult>();
        foreach (InitialViewMode mode in options.Modes)
        {
            captures.Add(Capture(mode, options));
        }

        WriteReviewPrompt(options.OutputDirectory, captures);
        app.Shutdown();

        Console.WriteLine($"Wrote {captures.Count} screenshot(s) to {options.OutputDirectory}");
        foreach (CaptureResult capture in captures)
        {
            Console.WriteLine($"{capture.Mode}: {capture.Path}");
        }

        return 0;
    }

    private static CaptureResult Capture(InitialViewMode mode, HarnessOptions options)
    {
        var window = new MainWindow(mode, enableRuntimeServices: false)
        {
            Width = options.Width,
            Height = options.Height,
            Left = -32000,
            Top = -32000,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        window.Show();
        PumpUi();
        window.UpdateLayout();
        PumpUi();

        string fileName = $"{mode.ToString().ToLowerInvariant()}-{options.Width}x{options.Height}.png";
        string path = Path.Combine(options.OutputDirectory, fileName);
        SaveWindowPng(window, path);
        window.Close();
        PumpUi();

        return new CaptureResult(mode, path);
    }

    private static void SaveWindowPng(Window window, string path)
    {
        int width = (int)Math.Ceiling(window.ActualWidth);
        int height = (int)Math.Ceiling(window.ActualHeight);

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private static void PumpUi()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    private static void WriteReviewPrompt(string outputDirectory, IReadOnlyList<CaptureResult> captures)
    {
        var prompt = new StringBuilder();
        prompt.AppendLine("# Bass Note Finder Visual Review");
        prompt.AppendLine();
        prompt.AppendLine("Use the generated screenshots to review UI/UX quality. Focus on:");
        prompt.AppendLine();
        prompt.AppendLine("- Alignment, spacing, and visual grouping");
        prompt.AppendLine("- Whether controls are named clearly for their behavior");
        prompt.AppendLine("- Whether Teacher and Student modes use the same layout language");
        prompt.AppendLine("- Whether the staff, fretboard, status, option strip, and footer compete for attention");
        prompt.AppendLine("- Any obvious accessibility concerns, including contrast and target size");
        prompt.AppendLine();
        prompt.AppendLine("Screenshots:");
        foreach (CaptureResult capture in captures)
        {
            prompt.AppendLine($"- {capture.Mode}: `{Path.GetFileName(capture.Path)}`");
        }
        prompt.AppendLine();
        prompt.AppendLine("Return prioritized findings and concrete implementation suggestions.");

        File.WriteAllText(Path.Combine(outputDirectory, "AI_REVIEW_PROMPT.md"), prompt.ToString());
    }

    private sealed record CaptureResult(InitialViewMode Mode, string Path);

    private sealed class HarnessOptions
    {
        public string OutputDirectory { get; private init; } = Path.Combine(".artifacts", "visual-review");
        public int Width { get; private init; } = DefaultWidth;
        public int Height { get; private init; } = DefaultHeight;
        public IReadOnlyList<InitialViewMode> Modes { get; private init; } =
            [InitialViewMode.Menu, InitialViewMode.Teacher, InitialViewMode.Student];

        public static HarnessOptions Parse(string[] args)
        {
            string outputDirectory = Path.Combine(".artifacts", "visual-review");
            int width = DefaultWidth;
            int height = DefaultHeight;
            IReadOnlyList<InitialViewMode> modes = [InitialViewMode.Menu, InitialViewMode.Teacher, InitialViewMode.Student];

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--output":
                    case "-o":
                        outputDirectory = RequireValue(args, ref i);
                        break;
                    case "--width":
                        width = int.Parse(RequireValue(args, ref i));
                        break;
                    case "--height":
                        height = int.Parse(RequireValue(args, ref i));
                        break;
                    case "--mode":
                    case "-m":
                        modes = ParseModes(RequireValue(args, ref i));
                        break;
                }
            }

            return new HarnessOptions
            {
                OutputDirectory = outputDirectory,
                Width = width,
                Height = height,
                Modes = modes
            };
        }

        private static string RequireValue(string[] args, ref int index)
        {
            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Missing value for {args[index]}.");
            }

            index++;
            return args[index];
        }

        private static IReadOnlyList<InitialViewMode> ParseModes(string value)
        {
            if (value.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                return [InitialViewMode.Menu, InitialViewMode.Teacher, InitialViewMode.Student];
            }

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(ParseMode)
                .ToArray();
        }

        private static InitialViewMode ParseMode(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "menu" => InitialViewMode.Menu,
                "teacher" => InitialViewMode.Teacher,
                "student" => InitialViewMode.Student,
                _ => throw new ArgumentException($"Unknown mode '{value}'. Use menu, teacher, student, or all.")
            };
        }
    }
}
