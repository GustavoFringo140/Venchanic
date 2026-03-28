namespace Venchanic.Core;

public sealed class DiagnosticsSnapshot
{
    public string AppVersion { get; init; } = string.Empty;

    public bool DebugModeEnabled { get; init; }

    public string InstallerStatus { get; init; } = string.Empty;

    public AppState State { get; init; } = new();

    public HealthCheckResult Health { get; init; } = new();
}
