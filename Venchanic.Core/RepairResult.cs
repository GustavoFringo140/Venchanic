namespace Venchanic.Core;

public sealed class RepairResult
{
    public bool Success { get; init; }

    public int ExitCode { get; init; }

    public string StandardOutput { get; init; } = string.Empty;

    public string StandardError { get; init; } = string.Empty;

    public string? Message { get; init; }

    public bool InstallerMissing { get; init; }

    public bool DiscordRunning { get; init; }

    public bool FilesLocked { get; init; }

    public bool CanRetryAfterClose { get; init; }

    public bool DownloadFailed { get; init; }

    public bool TimedOut { get; init; }

    public bool DeepRepair { get; init; }

    public bool CacheCleanupRequested { get; init; }

    public bool CacheCleanupAttempted { get; init; }

    public bool CacheCleanupHadErrors { get; init; }

    public bool DiscordCloseAttempted { get; init; }

    public bool DiscordCloseSucceeded { get; init; }
}
