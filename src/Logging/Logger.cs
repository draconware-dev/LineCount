using System.Globalization;
using System.Text.Json;
using LineCount.Errors;

namespace LineCount.Logging;

public static class Logger
{
    static bool ColorStripe = false;
    static readonly Lock Lock = new Lock();
    
    public static void Log(string path, string value)
    {
        lock (Lock)
        {
            ColorStripe = !ColorStripe;
            int width = Console.BufferWidth;
            string workingDir = Environment.CurrentDirectory;

            if (path.StartsWith(workingDir, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Remove(0, workingDir.Length + 1);
                width -= path.Length + value.Length;
            }

            int length = Math.Max(width, 0);

            string buffer = new string('.', length);

            ConsoleColor color = Console.ForegroundColor;

            Console.ForegroundColor = ColorStripe ? ConsoleColor.Cyan : ConsoleColor.Blue;

            Console.WriteLine($"{path}{buffer}{value}");

            Console.ForegroundColor = color;
        }
    }

    public static void LogError<T>(T error) where T : IError
    {
        Console.Error.WriteLine($"\x1b[0;31m{error}\x1b[0m");
    }

    public static void LogReport(LineCountReport report, Format format)
    {
        switch(format)
        {
            case Format.Normal:
                LogNormalReport(report);
                break;
            case Format.Raw:
                LogRawReport(report);
                break;
            case Format.Json:
                LogJsonReport(report);
                break;
        }
    }

    static void LogNormalReport(LineCountReport report)
    {
        if (report.Files == 1)
        {
            Console.WriteLine($"{report.Lines} lines have been found.");
            return;
        }

        Console.WriteLine($"{report.Lines} lines have been found across {report.Files} files.");
    }

    static void LogRawReport(LineCountReport report)
    {
        Console.WriteLine(report.Lines);
    }
    
    static void LogJsonReport(LineCountReport report)
    {
        string json = JsonSerializer.Serialize(report, LineCountReportJsonContext.Default.LineCountReport);
        Console.WriteLine(json);
    }
}