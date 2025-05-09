using System;
public static class Logging {
    public static void LogColor(ConsoleColor color, string message) {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
