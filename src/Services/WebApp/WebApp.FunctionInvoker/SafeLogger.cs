namespace WebApp.FunctionInvoker;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

public class SafeLogger
{
    private readonly int _maxMessagesPerSecond;
    private readonly int _maxBufferBytes;
    private readonly Queue<string> _buffer = new();
    private int _currentBytes = 0;
    private int _countThisSecond = 0;
    private readonly object _lock = new();
    private Timer? _resetTimer;

    public SafeLogger(int maxMessagesPerSecond = 50, int maxBufferBytes = 500_000)
    {
        _maxMessagesPerSecond = maxMessagesPerSecond;
        _maxBufferBytes = maxBufferBytes;

        _resetTimer = new Timer(_ => {
            lock (_lock) _countThisSecond = 0;
        }, null, 0, 1000);
    }

    public void Log(object? message)
    {
        lock (_lock)
        {
            if (_countThisSecond >= _maxMessagesPerSecond)
                return; // DROP

            _countThisSecond++;

            var msg = message?.ToString() ?? "null";
            var timestampedMsg = $"[{DateTime.UtcNow:O}] {msg}"; // ISO 8601 UTC
            var size = Encoding.UTF8.GetByteCount(timestampedMsg);

            if (_currentBytes + size > _maxBufferBytes)
                return; // DROP overflow

            _buffer.Enqueue(timestampedMsg);
            _currentBytes += size;
        }
    }

    public IReadOnlyCollection<string> Drain()
    {
        lock (_lock)
        {
            var result = _buffer.ToList();
            _buffer.Clear();
            _currentBytes = 0;
            return result;
        }
    }
    
    public void FlushToFile(string filePath)
    {
        lock (_lock)
        {
            if (_buffer.Count == 0)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.AppendAllLines(filePath, _buffer);

            _buffer.Clear();
            _currentBytes = 0;
        }
    }
}
