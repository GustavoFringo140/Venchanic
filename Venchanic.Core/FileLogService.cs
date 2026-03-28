using System.Text;

namespace Venchanic.Core;

public sealed class FileLogService
{
    public void Log(string area, string message)
    {
        try
        {
            RuntimePaths.EnsureRuntimeDirectories();
            var logPath = RuntimePaths.GetLogFilePath(DateTime.UtcNow);
            var line = $"{DateTime.UtcNow:O} [{area}] {message}{Environment.NewLine}";
            File.AppendAllText(logPath, line, Encoding.UTF8);
        }
        catch
        {
        }
    }
}
