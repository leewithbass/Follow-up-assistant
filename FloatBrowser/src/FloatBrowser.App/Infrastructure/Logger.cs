using System.IO;
using System.Text;

namespace FloatBrowser.App.Infrastructure;

public interface ILogger
{
    Task LogErrorAsync(string message, Exception ex);
    Task LogInfoAsync(string message);
}

public class FileLogger : ILogger
{
    private readonly string _logPath;

    public FileLogger(string appDataPath)
    {
        Directory.CreateDirectory(appDataPath);
        _logPath = Path.Combine(appDataPath, "floatbrowser.log");
    }

    public Task LogErrorAsync(string message, Exception ex) => WriteAsync($"[ERROR] {message}\n{ex}");

    public Task LogInfoAsync(string message) => WriteAsync($"[INFO] {message}");

    private Task WriteAsync(string line)
    {
        var text = $"{DateTime.Now:O} {line}{Environment.NewLine}";
        return File.AppendAllTextAsync(_logPath, text, Encoding.UTF8);
    }
}
