﻿using LineCount.Errors;

namespace LineCount;

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

    public static void LogError<T>(T error)
    {
        Console.Error.WriteLine(error);
    }
}