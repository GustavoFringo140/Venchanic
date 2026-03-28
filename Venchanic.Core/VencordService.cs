namespace Venchanic.Core;

public sealed class VencordService
{
    private readonly DiscordLocator _discordLocator = new();
    private readonly RepairService _repairService = new();
    private readonly StorageService _storageService = new();
    private readonly FileLogService _logService = new();
    private readonly DiagnosticsService _diagnosticsService = new();
    private readonly UpdateService _updateService = new();
    private readonly DiscordProcessService _discordProcessService = new();
    private readonly DiscordCacheService _discordCacheService = new();

    public AppState LoadState()
    {
        return _storageService.Load();
    }

    public void SaveState(AppState state)
    {
        _storageService.Save(state);
    }

    public bool HasInstaller()
    {
        return _repairService.HasInstaller();
    }

    public bool HasPrimaryInstaller()
    {
        return _repairService.HasPrimaryInstaller();
    }

    public async Task<bool> DownloadInstallerAsync(bool forceRedownload = false, bool useFallbackMirror = true)
    {
        _logService.Log("installer", $"Download requested. forceRedownload={forceRedownload}, useFallbackMirror={useFallbackMirror}");
        var success = await _repairService.DownloadInstallerAsync(forceRedownload, useFallbackMirror);
        _logService.Log("installer", success
            ? $"Installer ready at {RuntimePaths.InstallerCliPath}"
            : "Installer download failed.");

        var state = _storageService.Load();
        state.LastInstallerCliPath = RuntimePaths.InstallerCliPath;
        _storageService.Save(state);

        return success;
    }

    public HealthCheckResult Check()
    {
        RuntimePaths.EnsureRuntimeDirectories();
        _logService.Log("check", "Health check started.");

        var previousState = _storageService.Load();
        var discord = _discordLocator.FindDiscord();

        if (discord is null)
        {
            var notFoundResult = new HealthCheckResult
            {
                State = VencordHealthState.DiscordNotFound,
                Reason = "Discord installation was not found.",
                AppAsarPresent = false,
                MarkerPresent = false,
                RuntimeRootPath = RuntimePaths.RootDirectory,
                StateFilePath = RuntimePaths.StateFilePath,
                InstallerCliPath = RuntimePaths.InstallerCliPath,
                LastCheckTime = previousState.LastCheckTime,
                LastRepairTime = previousState.LastRepairTime,
                LastRepairResult = previousState.LastRepairResult,
                LastRepairMessage = previousState.LastRepairMessage
            };

            SaveState(previousState, notFoundResult, DateTime.UtcNow);
            _logService.Log("check", "Discord installation was not found.");
            return notFoundResult;
        }

        var isDiscordUpdated =
            !string.IsNullOrWhiteSpace(previousState.LastAppFolderPath) &&
            !string.IsNullOrWhiteSpace(discord.AppFolderPath) &&
            !string.Equals(previousState.LastAppFolderPath, discord.AppFolderPath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(previousState.LastDiscordPath, discord.DiscordPath, StringComparison.OrdinalIgnoreCase);

        HealthCheckResult result;

        if (isDiscordUpdated)
        {
            result = CreateHealthResult(
                previousState,
                discord,
                VencordHealthState.DiscordUpdated,
                "Discord was updated. Repair is recommended.",
                appAsarPresent: File.Exists(discord.AppAsarPath),
                markerPresent: File.Exists(discord.VencordMarkerPath));
        }
        else if (!File.Exists(discord.AppAsarPath))
        {
            result = CreateHealthResult(
                previousState,
                discord,
                VencordHealthState.BrokenInstall,
                "Discord resources are missing app.asar.",
                appAsarPresent: false,
                markerPresent: File.Exists(discord.VencordMarkerPath));
        }
        else if (!File.Exists(discord.VencordMarkerPath))
        {
            result = CreateHealthResult(
                previousState,
                discord,
                VencordHealthState.VencordNotDetected,
                "Vencord patch markers were not found.",
                appAsarPresent: true,
                markerPresent: false);
        }
        else
        {
            result = CreateHealthResult(
                previousState,
                discord,
                VencordHealthState.Healthy,
                "Vencord markers detected.",
                appAsarPresent: true,
                markerPresent: true);
        }

        SaveState(previousState, result, DateTime.UtcNow);
        _logService.Log("check", $"Health result: {result.State}");
        return result;
    }

    public async Task<RepairResult> RepairAsync(RepairOptions options)
    {
        RuntimePaths.EnsureRuntimeDirectories();
        _logService.Log("repair", $"Repair started. mode={options.Mode}, clearCache={options.ClearCacheBeforeRepair}, tryCloseDiscord={options.TryCloseDiscordBeforeRepair}");

        var health = Check();
        var closeResult = new DiscordCloseResult { Message = "Discord close was not requested." };
        var cacheResult = new CacheCleanupResult { Message = "Cache cleanup was not requested." };

        if (options.TryCloseDiscordBeforeRepair)
        {
            closeResult = await _discordProcessService.TryCloseDiscordFamilyAsync();
            _logService.Log("repair", $"Discord close result: {closeResult.Message}");
        }

        if (options.ClearCacheBeforeRepair)
        {
            cacheResult = _discordCacheService.ClearCaches(health.DiscordPath);
            _logService.Log("repair", $"Cache cleanup result: {cacheResult.Message}");
        }

        var repairResult = await _repairService.RepairAsync(health.Branch, health.DiscordPath, options.Mode);
        repairResult = AppendExecutionMetadata(repairResult, options, closeResult, cacheResult);

        if (!repairResult.Success &&
            repairResult.CanRetryAfterClose &&
            options.RetryAfterClosingDiscord &&
            !options.TryCloseDiscordBeforeRepair)
        {
            var retryCloseResult = await _discordProcessService.TryCloseDiscordFamilyAsync();
            _logService.Log("repair", $"Retry close result: {retryCloseResult.Message}");

            if (retryCloseResult.Success)
            {
                var retryOptions = new RepairOptions
                {
                    ClearCacheBeforeRepair = false,
                    Mode = options.Mode,
                    RetryAfterClosingDiscord = false,
                    TryCloseDiscordBeforeRepair = false,
                    UseFallbackMirror = options.UseFallbackMirror
                };

                var retryResult = await _repairService.RepairAsync(health.Branch, health.DiscordPath, retryOptions.Mode);
                repairResult = AppendExecutionMetadata(retryResult, retryOptions, retryCloseResult, cacheResult);
            }
            else
            {
                repairResult = new RepairResult
                {
                    Success = repairResult.Success,
                    ExitCode = repairResult.ExitCode,
                    StandardOutput = repairResult.StandardOutput,
                    StandardError = repairResult.StandardError,
                    Message = repairResult.Message,
                    InstallerMissing = repairResult.InstallerMissing,
                    DiscordRunning = repairResult.DiscordRunning,
                    FilesLocked = repairResult.FilesLocked,
                    CanRetryAfterClose = repairResult.CanRetryAfterClose,
                    DownloadFailed = repairResult.DownloadFailed,
                    TimedOut = repairResult.TimedOut,
                    DeepRepair = repairResult.DeepRepair,
                    CacheCleanupRequested = repairResult.CacheCleanupRequested,
                    CacheCleanupAttempted = repairResult.CacheCleanupAttempted,
                    CacheCleanupHadErrors = repairResult.CacheCleanupHadErrors,
                    DiscordCloseAttempted = true,
                    DiscordCloseSucceeded = false
                };
            }
        }

        PersistRepairState(repairResult);
        _logService.Log("repair", $"Repair completed. success={repairResult.Success}, message={repairResult.Message}");

        if (repairResult.Success)
        {
            Check();
        }

        return repairResult;
    }

    public Task<DiscordCloseResult> TryCloseDiscordFamilyAsync()
    {
        return _discordProcessService.TryCloseDiscordFamilyAsync();
    }

    public string BuildDiagnosticsText(string appVersion, bool debugModeEnabled, string installerStatus)
    {
        var snapshot = CreateDiagnosticsSnapshot(appVersion, debugModeEnabled, installerStatus);
        return _diagnosticsService.BuildTextReport(snapshot);
    }

    public string BuildDiagnosticsJson(string appVersion, bool debugModeEnabled, string installerStatus)
    {
        var snapshot = CreateDiagnosticsSnapshot(appVersion, debugModeEnabled, installerStatus);
        return _diagnosticsService.BuildJsonReport(snapshot);
    }

    public (string TextPath, string JsonPath) ExportDiagnostics(string appVersion, bool debugModeEnabled, string installerStatus)
    {
        var snapshot = CreateDiagnosticsSnapshot(appVersion, debugModeEnabled, installerStatus);
        var result = _diagnosticsService.ExportReports(snapshot);
        _logService.Log("diagnostics", $"Reports exported: {result.TextPath}, {result.JsonPath}");
        return result;
    }

    public Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion)
    {
        _logService.Log("updates", $"Checking for updates from version {currentVersion}.");
        return _updateService.CheckForUpdatesAsync(currentVersion);
    }

    public void Log(string area, string message)
    {
        _logService.Log(area, message);
    }

    private DiagnosticsSnapshot CreateDiagnosticsSnapshot(string appVersion, bool debugModeEnabled, string installerStatus)
    {
        return new DiagnosticsSnapshot
        {
            AppVersion = appVersion,
            DebugModeEnabled = debugModeEnabled,
            InstallerStatus = installerStatus,
            State = _storageService.Load(),
            Health = Check()
        };
    }

    private static RepairResult AppendExecutionMetadata(
        RepairResult repairResult,
        RepairOptions options,
        DiscordCloseResult closeResult,
        CacheCleanupResult cacheResult)
    {
        return new RepairResult
        {
            Success = repairResult.Success,
            ExitCode = repairResult.ExitCode,
            StandardOutput = repairResult.StandardOutput,
            StandardError = repairResult.StandardError,
            Message = repairResult.Message,
            InstallerMissing = repairResult.InstallerMissing,
            DiscordRunning = repairResult.DiscordRunning,
            FilesLocked = repairResult.FilesLocked,
            CanRetryAfterClose = repairResult.CanRetryAfterClose,
            DownloadFailed = repairResult.DownloadFailed,
            TimedOut = repairResult.TimedOut,
            DeepRepair = options.Mode == RepairMode.DeepReinstall,
            CacheCleanupRequested = options.ClearCacheBeforeRepair,
            CacheCleanupAttempted = options.ClearCacheBeforeRepair,
            CacheCleanupHadErrors = cacheResult.FailedDirectoryCount > 0,
            DiscordCloseAttempted = options.TryCloseDiscordBeforeRepair || closeResult.AnyProcessFound,
            DiscordCloseSucceeded = closeResult.Success
        };
    }

    private void PersistRepairState(RepairResult repairResult)
    {
        var state = _storageService.Load();
        state.LastRepairTime = DateTime.UtcNow;
        state.LastRepairResult = repairResult.Success ? "Success" : "Failed";
        state.LastRepairMessage = repairResult.Message;
        state.LastInstallerCliPath = RuntimePaths.InstallerCliPath;
        _storageService.Save(state);
    }

    private static HealthCheckResult CreateHealthResult(
        AppState previousState,
        DiscordInstallInfo discord,
        VencordHealthState state,
        string reason,
        bool appAsarPresent,
        bool markerPresent)
    {
        return new HealthCheckResult
        {
            State = state,
            Branch = discord.Branch,
            Reason = reason,
            DiscordPath = discord.DiscordPath,
            AppFolderPath = discord.AppFolderPath,
            DiscordVersion = discord.DiscordVersion,
            ResourcesPath = discord.ResourcesPath,
            AppAsarPresent = appAsarPresent,
            MarkerPresent = markerPresent,
            RuntimeRootPath = RuntimePaths.RootDirectory,
            StateFilePath = RuntimePaths.StateFilePath,
            InstallerCliPath = RuntimePaths.InstallerCliPath,
            LastCheckTime = previousState.LastCheckTime,
            LastRepairTime = previousState.LastRepairTime,
            LastRepairResult = previousState.LastRepairResult,
            LastRepairMessage = previousState.LastRepairMessage
        };
    }

    private void SaveState(AppState previousState, HealthCheckResult result, DateTime checkTimeUtc)
    {
        previousState.LastDiscordPath = result.DiscordPath;
        previousState.LastAppFolderPath = result.AppFolderPath;
        previousState.LastDiscordVersion = result.DiscordVersion;
        previousState.LastHealthState = result.State.ToString();
        previousState.LastCheckTime = checkTimeUtc;
        previousState.LastInstallerCliPath = RuntimePaths.InstallerCliPath;
        _storageService.Save(previousState);
    }
}
