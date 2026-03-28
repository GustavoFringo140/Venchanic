namespace Venchanic.Core;

public sealed class AppState
{
    public bool? IsDebugMode { get; set; }

    public AppSettings Settings { get; set; } = new();

    public string? LastDiscordPath { get; set; }

    public string? LastAppFolderPath { get; set; }

    public string? LastDiscordVersion { get; set; }

    public string? LastHealthState { get; set; }

    public string? LastInstallerCliPath { get; set; }

    public DateTime? LastCheckTime { get; set; }

    public DateTime? LastRepairTime { get; set; }

    public string? LastRepairResult { get; set; }

    public string? LastRepairMessage { get; set; }

    public string? LastUpdateCheckResult { get; set; }

    public DateTime? LastUpdateCheckTime { get; set; }
}
