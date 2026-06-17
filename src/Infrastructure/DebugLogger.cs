using System.Runtime.CompilerServices;

namespace SshConfigTui.Infrastructure;

public class DebugLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public DebugLogger(bool enabled = false, string path = "debug.log")
    {
        if (!enabled)
        {
            _writer = StreamWriter.Null;
            return;
        }

        try
        {
            _writer = new StreamWriter(path, append: false) { AutoFlush = true };
            Write("=== SSH Config TUI Debug Log ===");
        }
        catch
        {
            _writer = StreamWriter.Null;
        }
    }

    public void Write(
        string message,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        lock (_lock)
        {
            var fileName = Path.GetFileName(file);
            _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{fileName}:{line} {member}] {message}");
        }
    }

    public void WriteError(string message, Exception? ex = null)
    {
        lock (_lock)
        {
            _writer.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [ERROR] {message}");
            if (ex != null)
                _writer.WriteLine($"  {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            Write("=== End ===");
            _writer.Flush();
            _writer.Dispose();
        }
    }
}
