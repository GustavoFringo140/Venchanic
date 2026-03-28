namespace Venchanic.Core;

public static class RuntimePaths
{
    private static string LocalAppDataRoot
    {
        get
        {
            var specialFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(specialFolderPath))
            {
                return specialFolderPath;
            }

            return Environment.GetEnvironmentVariable("LOCALAPPDATA") ??
                AppContext.BaseDirectory;
        }
    }

    public static string RootDirectory =>
        Path.Combine(LocalAppDataRoot, "Venchanic");

    public static string ToolsDirectory =>
        Path.Combine(RootDirectory, "tools");

    public static string StateFilePath =>
        Path.Combine(RootDirectory, "state.json");

    public static string LogsDirectory =>
        Path.Combine(RootDirectory, "logs");

    public static string ReportsDirectory =>
        Path.Combine(RootDirectory, "reports");

    public static string InstallerCliPath =>
        Path.Combine(ToolsDirectory, "VencordInstallerCli.exe");

    public static void EnsureRuntimeDirectories()
    {
        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ToolsDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ReportsDirectory);
    }

    public static string GetLogFilePath(DateTime utcDateTime)
    {
        return Path.Combine(LogsDirectory, $"venchanic-{utcDateTime:yyyyMMdd}.log");
    }
}
