using System;

namespace ConfluenceMd;

public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}

public class ConsoleLogger : ILogger
{
    private readonly bool _verbose;

    public ConsoleLogger(bool verbose = false)
    {
        _verbose = verbose;
    }

    public void Debug(string message)
    {
        if (_verbose)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }

    public void Info(string message)
    {
        Console.WriteLine(message);
    }

    public void Warning(string message)
    {
        Console.WriteLine($"[WARNING] {message}");
    }

    public void Error(string message)
    {
        Console.Error.WriteLine($"[ERROR] {message}");
    }
}
