using System.Diagnostics;

namespace Bomberman.Core.Utilities;

public static class Logger
{
    public static void Information(string message)
    {
        Log(nameof(Information), message);
    }

    public static void Warning(string message)
    {
        Log(nameof(Warning), message);
    }

    private static void Log(string logLevel, string message)
    {
        Debug.WriteLine($"{DateTime.Now} [{logLevel}] {message}");
    }
}
