namespace Venchanic.Core;

public sealed class HealthCheckResult
{
    public VencordHealthState State { get; init; }

    public string? Branch { get; init; }

    public string? Reason { get; init; }

    public string? DiscordPath { get; init; }

    public string? AppFolderPath { get; init; }

    public string? DiscordVersion { get; init; }

    public string? ResourcesPath { get; init; }

    public bool AppAsarPresent { get; init; }

    public bool MarkerPresent { get; init; }

    public string? RuntimeRootPath { get; init; }

    public string? StateFilePath { get; init; }

    public string? InstallerCliPath { get; init; }

    public DateTime? LastCheckTime { get; init; }

    public DateTime? LastRepairTime { get; init; }

    public string? LastRepairResult { get; init; }

    public string? LastRepairMessage { get; init; }
}
