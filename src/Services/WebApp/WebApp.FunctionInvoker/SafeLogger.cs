using System.Text.Json;

namespace WebApp.FunctionInvoker;

public class SafeLogger
{
    private readonly int _maxMessagesPerSecond;
    private int _countThisSecond = 0;
    private readonly object _lock = new();
    private Timer _resetTimer;

    public SafeLogger(int maxMessagesPerSecond = 50)
    {
        _maxMessagesPerSecond = maxMessagesPerSecond;
        
        _resetTimer = new Timer(_ => {
            lock (_lock) _countThisSecond = 0;
        }, null, 0, 1000);
    }

    private bool Allow()
    {
        lock (_lock)
        {
            if (_countThisSecond >= _maxMessagesPerSecond)
                return false;

            _countThisSecond++;
            return true;
        }
    }

    public void Info(object? message) => Log(message, "INFO");
    public void Warn(object? message) => Log(message, "WARN");
    public void Error(object? message) => Log(message, "ERROR");

    private void Log(object? message, string level)
    {
        if (!Allow() || message is null)
            return;

        var record = new LogRecord(DateTime.UtcNow, message.ToString() ?? "null", level);

        var json = JsonSerializer.Serialize(record);
        Console.WriteLine(json); // Fluentd capture
    }
}