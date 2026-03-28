namespace Venchanic.Core;

public sealed class AppSettings
{
    public bool AutoCheckOnStartup { get; set; } = true;

    public bool CheckForUpdatesOnStartup { get; set; } = true;

    public bool AutoDownloadInstallerWhenRepairStarts { get; set; } = true;

    public bool ShowDebugDiagnostics { get; set; }

    public bool ClearCacheBeforeRepairByDefault { get; set; }

    public bool TryCloseDiscordAutomaticallyBeforeRepair { get; set; }

    public bool ExportDiagnosticsAfterFailedRepair { get; set; }

    public bool UseFallbackMirrorIfOfficialInstallerDownloadFails { get; set; } = true;
}
