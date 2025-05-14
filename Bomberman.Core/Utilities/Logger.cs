namespace Bomberman.Core.Utilities;

public static class Logger
{
    private enum LogLevel
    {
        Information = 5,
        Warning = 6,
    }

    private const LogLevel Level =
#if DEBUG
    LogLevel.Information;
#else
    LogLevel.Warning;
#endif

    public static void Information(string message)
    {
        if (Level > LogLevel.Information)
            return;

        Log(nameof(Information), message);
    }

    public static void Warning(string message)
    {
        if (Level > LogLevel.Warning)
            return;

        Log(nameof(Warning), message);
    }

    private static void Log(string logLevel, string message)
    {
        Console.WriteLine($"{DateTime.Now} [{logLevel}] {message}");
    }
}
