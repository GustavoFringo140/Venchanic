using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Venchanic.Core;

namespace Venchanic.UI.ViewModels;

public enum InstallerToolState { InstallerReady, InstallerMissing, InstallerDownloading, InstallerDownloadFailed }
public enum ToastKind { Info, Success, Warning, Error }

public sealed class DashboardViewModel : ViewModelBase
{
    public const string DashboardTab = "Dashboard";
    public const string HealthCheckTab = "Health Check";
    public const string RepairTab = "Repair";
    public const string SettingsTab = "Settings";

    private readonly VencordService _vencordService = new();
    private readonly RelayCommand _checkCommand;
    private AppState _state;
    private AppSettings _settings;
    private RepairOptions _pendingRepairOptions = new();
    private string _appFolderPath = "Not detected", _appVersionText = "v0.6.0", _closeDiscordRetryMessage = string.Empty, _discordBranch = "Not detected", _discordPath = "Not detected", _discordVersion = "Not detected", _installerCliPath = RuntimePaths.InstallerCliPath, _installerDialogMessage = string.Empty, _installerDialogPrimaryButtonText = "Download", _installerDialogSecondaryButtonText = "Cancel", _installerDialogStatusText = string.Empty, _installerDialogTitle = string.Empty, _lastOperationMessage = "Run Repair to patch the current Discord install.", _lastOperationTitle = "No repair recorded yet", _latestAvailableVersion = "Unknown", _reason = "Press Check to inspect Discord.", _repairDialogMessage = "Choose how Venchanic should prepare and run the repair flow.", _repairDialogTitle = "Repair options", _repairError = string.Empty, _repairMessage = string.Empty, _repairOutput = string.Empty, _reportsFolderPath = RuntimePaths.ReportsDirectory, _resourcesPath = "Not detected", _runtimeRootPath = RuntimePaths.RootDirectory, _selectedTab = DashboardTab, _stateFilePath = RuntimePaths.StateFilePath, _status = "Unknown", _statusBarText = "Ready", _toastMessage = string.Empty, _toastTitle = string.Empty, _updateReleaseUrl = "https://github.com/GustavoFringo140/Venchanic/releases/latest", _updateStatusText = "Update status not checked yet.", _logsFolderPath = RuntimePaths.LogsDirectory;
    private bool _canRetryAfterClose, _isAppAsarPresent, _isBusy, _isDebugMode, _isInstallerDialogOpen, _isInstallerDialogPrimaryVisible = true, _isInstallerDialogSecondaryVisible = true, _isInstallerDownloadInProgress, _isMarkerPresent, _isProgressVisible, _isRepairOptionsDialogOpen, _isRepairRunning, _isToastVisible, _isPatchModeSelected = true, _isDeepModeSelected, _repairOptionsClearCache, _repairOptionsTryCloseDiscord, _showResetStatePrompt, _updateAvailable, _pendingInstallerDownloadContinueToRepair, _pendingInstallerRedownload;
    private InstallerToolState _installerFlowState;
    private DateTime? _lastCheckTime, _lastRepairTime, _lastUpdateCheckTime;
    private ToastKind _toastKind = ToastKind.Info;

    public DashboardViewModel()
    {
        _state = _vencordService.LoadState();
        _settings = _state.Settings ?? new AppSettings();
        _state.Settings = _settings;
        _isDebugMode = _state.IsDebugMode ?? _settings.ShowDebugDiagnostics;
        _settings.ShowDebugDiagnostics = _isDebugMode;
        ApplySavedState(_state);
        SyncInstallerState();
        _checkCommand = new RelayCommand(OnCheck, () => !IsBusy);
        CheckCommand = _checkCommand;
        RepairCommand = new AsyncRelayCommand(OnRepairAsync, () => !IsBusy, OnBusyStateChanged);
        FixEverythingCommand = new AsyncRelayCommand(OnFixEverythingAsync, () => !IsBusy, OnBusyStateChanged);
        RetryCloseDiscordCommand = new AsyncRelayCommand(OnCloseDiscordAndRetryAsync, () => !IsBusy, OnBusyStateChanged);
        CheckForUpdatesCommand = new AsyncRelayCommand(OnCheckForUpdatesAsync, () => !IsBusy, OnBusyStateChanged);
        if (AutoCheckOnStartup) { OnCheck(); } else { RefreshRuntimeMetadata(); }
        if (CheckForUpdatesOnStartup) { _ = CheckForUpdatesAsync(false); }
    }

    public string Status { get => _status; set { if (SetProperty(ref _status, value)) { RaisePropertyChanged(nameof(StatusTitle)); RaisePropertyChanged(nameof(StatusSubtitle)); RaisePropertyChanged(nameof(RecommendedActionText)); RaisePropertyChanged(nameof(StatusBadgeText)); RaisePropertyChanged(nameof(FooterSummaryText)); } } }
    public string Reason { get => _reason; set => SetProperty(ref _reason, value); }
    public string StatusTitle => Status switch { nameof(VencordHealthState.Healthy) => "Vencord is healthy", nameof(VencordHealthState.DiscordUpdated) => "Discord was updated", nameof(VencordHealthState.VencordNotDetected) => "Vencord was not detected", nameof(VencordHealthState.BrokenInstall) => "Discord install looks incomplete", nameof(VencordHealthState.DiscordNotFound) => "Discord was not found", _ => "Check your Discord health" };
    public string StatusSubtitle => Status switch { nameof(VencordHealthState.Healthy) => "Your Discord install appears to be patched and ready.", nameof(VencordHealthState.DiscordUpdated) => "Your Discord app folder changed since the last known state.", nameof(VencordHealthState.VencordNotDetected) => "Discord was found, but Vencord markers were not detected.", nameof(VencordHealthState.BrokenInstall) => "Discord resources are missing required files.", nameof(VencordHealthState.DiscordNotFound) => "Venchanic could not locate a supported Discord installation.", _ => string.IsNullOrWhiteSpace(Reason) ? "Run a health check to inspect the current install." : Reason };
    public string RecommendedActionText => CanRetryAfterClose ? "Close Discord and retry." : InstallerFlowState != InstallerToolState.InstallerReady && Status is nameof(VencordHealthState.DiscordUpdated) or nameof(VencordHealthState.VencordNotDetected) or nameof(VencordHealthState.BrokenInstall) ? "Installer required before Repair." : Status switch { nameof(VencordHealthState.Healthy) => "No action needed.", nameof(VencordHealthState.DiscordUpdated) => "Repair recommended.", nameof(VencordHealthState.VencordNotDetected) => "Patch recommended.", nameof(VencordHealthState.BrokenInstall) => "Repair recommended.", nameof(VencordHealthState.DiscordNotFound) => "Install Discord or verify the install location.", _ => "Run Check to inspect the current install." };
    public string StatusBadgeText => Status switch { nameof(VencordHealthState.Healthy) => "Healthy", nameof(VencordHealthState.DiscordUpdated) => "Update detected", nameof(VencordHealthState.VencordNotDetected) => "Not detected", nameof(VencordHealthState.BrokenInstall) => "Broken", nameof(VencordHealthState.DiscordNotFound) => "Not found", _ => "Unknown" };
    public string InstallerStatusTitle => InstallerFlowState switch { InstallerToolState.InstallerReady => "Ready", InstallerToolState.InstallerDownloading => "Downloading", InstallerToolState.InstallerDownloadFailed => "Failed", _ => "Missing" };
    public string InstallerStatusText => InstallerFlowState switch { InstallerToolState.InstallerReady => "VencordInstallerCli.exe is available in runtime tools storage.", InstallerToolState.InstallerDownloading => "Downloading VencordInstallerCli.exe into runtime tools storage.", InstallerToolState.InstallerDownloadFailed => "Installer download failed. Check your internet connection and try again.", _ => "Installer is not available." };
    public string AppAsarStatusText => IsAppAsarPresent ? "Present" : "Missing";
    public string MarkerStatusText => IsMarkerPresent ? "Present" : "Missing";
    public string LastCheckTimeText => FormatDate(LastCheckTime);
    public string LastRepairTimeText => FormatDate(LastRepairTime);
    public string LastUpdateCheckTimeText => FormatDate(LastUpdateCheckTime);
    public string FooterSummaryText => $"{StatusBadgeText} | Installer {InstallerStatusTitle}";
    public string LastOperationSummaryText => $"{LastOperationTitle}. {LastOperationMessage}";
    public string LogsFolderPath { get => _logsFolderPath; set => SetProperty(ref _logsFolderPath, value); }
    public string ReportsFolderPath { get => _reportsFolderPath; set => SetProperty(ref _reportsFolderPath, value); }
    public string SelectedTab { get => _selectedTab; set => SetProperty(ref _selectedTab, value); }
    public string DiscordPath { get => _discordPath; set => SetProperty(ref _discordPath, value); }
    public string DiscordVersion { get => _discordVersion; set => SetProperty(ref _discordVersion, value); }
    public string DiscordBranch { get => _discordBranch; set => SetProperty(ref _discordBranch, value); }
    public string AppFolderPath { get => _appFolderPath; set => SetProperty(ref _appFolderPath, value); }
    public bool IsInstallerDialogOpen { get => _isInstallerDialogOpen; set => SetProperty(ref _isInstallerDialogOpen, value); }
    public bool IsInstallerDownloadInProgress { get => _isInstallerDownloadInProgress; set => SetProperty(ref _isInstallerDownloadInProgress, value); }
    public bool IsInstallerDialogPrimaryVisible { get => _isInstallerDialogPrimaryVisible; set => SetProperty(ref _isInstallerDialogPrimaryVisible, value); }
    public bool IsInstallerDialogSecondaryVisible { get => _isInstallerDialogSecondaryVisible; set => SetProperty(ref _isInstallerDialogSecondaryVisible, value); }
    public string InstallerDialogTitle { get => _installerDialogTitle; set => SetProperty(ref _installerDialogTitle, value); }
    public string InstallerDialogMessage { get => _installerDialogMessage; set => SetProperty(ref _installerDialogMessage, value); }
    public string InstallerDialogStatusText { get => _installerDialogStatusText; set => SetProperty(ref _installerDialogStatusText, value); }
    public string InstallerDialogPrimaryButtonText { get => _installerDialogPrimaryButtonText; set => SetProperty(ref _installerDialogPrimaryButtonText, value); }
    public string InstallerDialogSecondaryButtonText { get => _installerDialogSecondaryButtonText; set => SetProperty(ref _installerDialogSecondaryButtonText, value); }
    public InstallerToolState InstallerFlowState { get => _installerFlowState; set { if (SetProperty(ref _installerFlowState, value)) { RaisePropertyChanged(nameof(InstallerStatusTitle)); RaisePropertyChanged(nameof(InstallerStatusText)); RaisePropertyChanged(nameof(FooterSummaryText)); RaisePropertyChanged(nameof(RecommendedActionText)); } } }
    public bool IsRepairOptionsDialogOpen { get => _isRepairOptionsDialogOpen; set => SetProperty(ref _isRepairOptionsDialogOpen, value); }
    public string RepairDialogTitle { get => _repairDialogTitle; set => SetProperty(ref _repairDialogTitle, value); }
    public string RepairDialogMessage { get => _repairDialogMessage; set => SetProperty(ref _repairDialogMessage, value); }
    public bool RepairOptionsClearCache { get => _repairOptionsClearCache; set => SetProperty(ref _repairOptionsClearCache, value); }
    public bool RepairOptionsTryCloseDiscord { get => _repairOptionsTryCloseDiscord; set => SetProperty(ref _repairOptionsTryCloseDiscord, value); }
    public bool IsPatchModeSelected { get => _isPatchModeSelected; set { if (SetProperty(ref _isPatchModeSelected, value) && value) { IsDeepModeSelected = false; } } }
    public bool IsDeepModeSelected { get => _isDeepModeSelected; set { if (SetProperty(ref _isDeepModeSelected, value) && value) { IsPatchModeSelected = false; } } }
    public string RuntimeRootPath { get => _runtimeRootPath; set => SetProperty(ref _runtimeRootPath, value); }
    public string StateFilePath { get => _stateFilePath; set => SetProperty(ref _stateFilePath, value); }
    public string InstallerCliPath { get => _installerCliPath; set => SetProperty(ref _installerCliPath, value); }
    public string ResourcesPath { get => _resourcesPath; set => SetProperty(ref _resourcesPath, value); }
    public bool IsAppAsarPresent { get => _isAppAsarPresent; set { if (SetProperty(ref _isAppAsarPresent, value)) { RaisePropertyChanged(nameof(AppAsarStatusText)); } } }
    public bool IsMarkerPresent { get => _isMarkerPresent; set { if (SetProperty(ref _isMarkerPresent, value)) { RaisePropertyChanged(nameof(MarkerStatusText)); } } }
    public string RepairMessage { get => _repairMessage; set => SetProperty(ref _repairMessage, value); }
    public string RepairOutput { get => _repairOutput; set { if (SetProperty(ref _repairOutput, value)) { RaisePropertyChanged(nameof(HasRepairDetails)); } } }
    public string RepairError { get => _repairError; set { if (SetProperty(ref _repairError, value)) { RaisePropertyChanged(nameof(HasRepairDetails)); } } }
    public string LastOperationTitle { get => _lastOperationTitle; set { if (SetProperty(ref _lastOperationTitle, value)) { RaisePropertyChanged(nameof(LastOperationSummaryText)); } } }
    public string LastOperationMessage { get => _lastOperationMessage; set { if (SetProperty(ref _lastOperationMessage, value)) { RaisePropertyChanged(nameof(LastOperationSummaryText)); } } }
    public DateTime? LastCheckTime { get => _lastCheckTime; set { if (SetProperty(ref _lastCheckTime, value)) { RaisePropertyChanged(nameof(LastCheckTimeText)); } } }
    public DateTime? LastRepairTime { get => _lastRepairTime; set { if (SetProperty(ref _lastRepairTime, value)) { RaisePropertyChanged(nameof(LastRepairTimeText)); } } }
    public DateTime? LastUpdateCheckTime { get => _lastUpdateCheckTime; set { if (SetProperty(ref _lastUpdateCheckTime, value)) { RaisePropertyChanged(nameof(LastUpdateCheckTimeText)); } } }
    public bool HasRepairDetails => !string.IsNullOrWhiteSpace(RepairOutput) || !string.IsNullOrWhiteSpace(RepairError);
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
    public bool IsDebugMode { get => _isDebugMode; set { if (SetProperty(ref _isDebugMode, value)) { _settings.ShowDebugDiagnostics = value; _state.IsDebugMode = value; PersistState(); RaisePropertyChanged(nameof(DebugModeText)); RaisePropertyChanged(nameof(HasRepairDetails)); } } }
    public bool AutoCheckOnStartup { get => _settings.AutoCheckOnStartup; set { if (_settings.AutoCheckOnStartup != value) { _settings.AutoCheckOnStartup = value; RaisePropertyChanged(); PersistState(); } } }
    public bool CheckForUpdatesOnStartup { get => _settings.CheckForUpdatesOnStartup; set { if (_settings.CheckForUpdatesOnStartup != value) { _settings.CheckForUpdatesOnStartup = value; RaisePropertyChanged(); PersistState(); } } }
    public bool AutoDownloadInstallerWhenRepairStarts { get => _settings.AutoDownloadInstallerWhenRepairStarts; set { if (_settings.AutoDownloadInstallerWhenRepairStarts != value) { _settings.AutoDownloadInstallerWhenRepairStarts = value; RaisePropertyChanged(); PersistState(); } } }
    public bool ClearCacheBeforeRepairByDefault { get => _settings.ClearCacheBeforeRepairByDefault; set { if (_settings.ClearCacheBeforeRepairByDefault != value) { _settings.ClearCacheBeforeRepairByDefault = value; RaisePropertyChanged(); PersistState(); } } }
    public bool TryCloseDiscordAutomaticallyBeforeRepair { get => _settings.TryCloseDiscordAutomaticallyBeforeRepair; set { if (_settings.TryCloseDiscordAutomaticallyBeforeRepair != value) { _settings.TryCloseDiscordAutomaticallyBeforeRepair = value; RaisePropertyChanged(); PersistState(); } } }
    public bool ExportDiagnosticsAfterFailedRepair { get => _settings.ExportDiagnosticsAfterFailedRepair; set { if (_settings.ExportDiagnosticsAfterFailedRepair != value) { _settings.ExportDiagnosticsAfterFailedRepair = value; RaisePropertyChanged(); PersistState(); } } }
    public bool UseFallbackMirrorIfOfficialInstallerDownloadFails { get => _settings.UseFallbackMirrorIfOfficialInstallerDownloadFails; set { if (_settings.UseFallbackMirrorIfOfficialInstallerDownloadFails != value) { _settings.UseFallbackMirrorIfOfficialInstallerDownloadFails = value; RaisePropertyChanged(); PersistState(); } } }
    public string DebugModeText => IsDebugMode ? "Debug mode on" : "Debug mode off";
    public string StatusBarText { get => _statusBarText; set => SetProperty(ref _statusBarText, value); }
    public bool IsProgressVisible { get => _isProgressVisible; set => SetProperty(ref _isProgressVisible, value); }
    public bool IsRepairRunning { get => _isRepairRunning; set => SetProperty(ref _isRepairRunning, value); }
    public bool ShowResetStatePrompt { get => _showResetStatePrompt; set => SetProperty(ref _showResetStatePrompt, value); }
    public bool CanRetryAfterClose { get => _canRetryAfterClose; set { if (SetProperty(ref _canRetryAfterClose, value)) { RaisePropertyChanged(nameof(RecommendedActionText)); } } }
    public string CloseDiscordRetryMessage { get => _closeDiscordRetryMessage; set => SetProperty(ref _closeDiscordRetryMessage, value); }
    public bool IsToastVisible { get => _isToastVisible; set => SetProperty(ref _isToastVisible, value); }
    public string ToastTitle { get => _toastTitle; set => SetProperty(ref _toastTitle, value); }
    public string ToastMessage { get => _toastMessage; set => SetProperty(ref _toastMessage, value); }
    public ToastKind ToastKind { get => _toastKind; set => SetProperty(ref _toastKind, value); }
    public string UpdateStatusText { get => _updateStatusText; set => SetProperty(ref _updateStatusText, value); }
    public string LatestAvailableVersion { get => _latestAvailableVersion; set => SetProperty(ref _latestAvailableVersion, value); }
    public bool UpdateAvailable { get => _updateAvailable; set => SetProperty(ref _updateAvailable, value); }
    public string UpdateReleaseUrl { get => _updateReleaseUrl; set => SetProperty(ref _updateReleaseUrl, value); }
    public string AppVersionText { get => _appVersionText; set => SetProperty(ref _appVersionText, value); }
    public ICommand CheckCommand { get; }
    public ICommand RepairCommand { get; }
    public ICommand FixEverythingCommand { get; }
    public ICommand RetryCloseDiscordCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }
    public bool HasInstaller() => _vencordService.HasInstaller();
    public bool HasPrimaryInstaller() => _vencordService.HasPrimaryInstaller();

    public void OpenRepairOptionsDialog(bool fixEverything) { RepairDialogTitle = fixEverything ? "Fix everything" : "Repair options"; RepairDialogMessage = fixEverything ? "Venchanic can close Discord, clear cache, ensure the installer exists, run repair and re-check the install." : "Choose how Venchanic should prepare and run the repair flow."; RepairOptionsClearCache = fixEverything || ClearCacheBeforeRepairByDefault; RepairOptionsTryCloseDiscord = fixEverything || TryCloseDiscordAutomaticallyBeforeRepair; IsPatchModeSelected = true; IsDeepModeSelected = false; IsRepairOptionsDialogOpen = true; }
    public void CancelRepairOptionsDialog() { IsRepairOptionsDialogOpen = false; }
    public async Task ConfirmRepairOptionsAsync() { IsRepairOptionsDialogOpen = false; _pendingRepairOptions = new RepairOptions { ClearCacheBeforeRepair = RepairOptionsClearCache, Mode = IsDeepModeSelected ? RepairMode.DeepReinstall : RepairMode.Patch, TryCloseDiscordBeforeRepair = RepairOptionsTryCloseDiscord, RetryAfterClosingDiscord = true, UseFallbackMirror = UseFallbackMirrorIfOfficialInstallerDownloadFails }; if (!HasPrimaryInstaller()) { if (AutoDownloadInstallerWhenRepairStarts) { OpenInstallerDownloadDialog(true, false); await ConfirmInstallerDownloadAsync(); } else { OpenInstallerDownloadDialog(true, false); } return; } await RunRepairAsync(_pendingRepairOptions); }
    public void OpenInstallerDownloadDialog(bool continueToRepair, bool forceRedownload) { _pendingInstallerDownloadContinueToRepair = continueToRepair; _pendingInstallerRedownload = forceRedownload; SyncInstallerState(); InstallerDialogTitle = forceRedownload ? "Re-download installer" : "Installer required"; InstallerDialogMessage = forceRedownload ? "Venchanic can download a fresh copy of VencordInstallerCli.exe into runtime tools storage." : "VencordInstallerCli.exe was not found. Venchanic can download it automatically."; InstallerDialogStatusText = forceRedownload ? "The installer will be saved to the runtime tools folder." : "Repair needs the official Vencord installer."; InstallerDialogPrimaryButtonText = forceRedownload ? "Redownload" : "Download"; InstallerDialogSecondaryButtonText = "Cancel"; IsInstallerDialogPrimaryVisible = true; IsInstallerDialogSecondaryVisible = true; IsInstallerDownloadInProgress = false; IsInstallerDialogOpen = true; }
    public void CancelInstallerDialogFlow() { IsInstallerDialogOpen = false; IsInstallerDownloadInProgress = false; RepairMessage = _pendingInstallerDownloadContinueToRepair ? "Repair cancelled." : "Installer action cancelled."; }
    public async Task ConfirmInstallerDownloadAsync()
    {
        InstallerFlowState = InstallerToolState.InstallerDownloading; InstallerDialogTitle = "Downloading installer"; InstallerDialogMessage = "Downloading VencordInstallerCli.exe..."; InstallerDialogStatusText = "Venchanic is saving the installer into runtime tools storage."; IsInstallerDownloadInProgress = true; IsInstallerDialogPrimaryVisible = false; IsInstallerDialogSecondaryVisible = false;
        var ok = await _vencordService.DownloadInstallerAsync(_pendingInstallerRedownload, UseFallbackMirrorIfOfficialInstallerDownloadFails);
        if (!ok) { InstallerFlowState = InstallerToolState.InstallerDownloadFailed; IsInstallerDownloadInProgress = false; InstallerDialogTitle = "Download failed"; InstallerDialogMessage = "Failed to download VencordInstallerCli.exe."; InstallerDialogStatusText = "Check your internet connection and try again."; InstallerDialogPrimaryButtonText = "Retry"; InstallerDialogSecondaryButtonText = "Cancel"; IsInstallerDialogPrimaryVisible = true; IsInstallerDialogSecondaryVisible = true; RepairMessage = "Installer download failed. Check your internet connection and try again."; LastOperationTitle = "Installer download failed"; LastOperationMessage = "Venchanic could not download the official Vencord installer."; ShowToast("Installer download failed", "Check your internet connection and try again.", ToastKind.Error); RefreshRuntimeMetadata(); return; }
        SyncInstallerState(); RefreshRuntimeMetadata(); IsInstallerDialogOpen = false; IsInstallerDownloadInProgress = false; ShowToast("Installer downloaded", "The official Vencord installer is ready.", ToastKind.Success);
        if (_pendingInstallerDownloadContinueToRepair) { await RunRepairAsync(_pendingRepairOptions); return; }
        RepairMessage = "Installer downloaded successfully."; LastOperationTitle = "Installer downloaded"; LastOperationMessage = "The official Vencord installer is ready in runtime tools storage.";
    }

    public void ToggleResetStatePrompt() { ShowResetStatePrompt = !ShowResetStatePrompt; }
    public void ResetLocalState() { _state = new AppState { IsDebugMode = IsDebugMode, Settings = _settings, LastInstallerCliPath = RuntimePaths.InstallerCliPath }; _vencordService.SaveState(_state); ShowResetStatePrompt = false; Status = "Unknown"; Reason = "Local state reset. Run health check to rebuild diagnostics."; DiscordPath = "Not detected"; DiscordVersion = "Not detected"; DiscordBranch = "Not detected"; AppFolderPath = "Not detected"; ResourcesPath = "Not detected"; IsAppAsarPresent = false; IsMarkerPresent = false; RepairMessage = "Local state reset."; RepairOutput = string.Empty; RepairError = string.Empty; LastCheckTime = null; LastRepairTime = null; LastOperationTitle = "Local state reset"; LastOperationMessage = "Saved Venchanic state and detection history were cleared."; CanRetryAfterClose = false; CloseDiscordRetryMessage = string.Empty; RefreshRuntimeMetadata(); SyncInstallerState(); ShowToast("Local state reset", "Saved Venchanic state and detection history were cleared.", ToastKind.Info); }
    public string GetDiagnosticsText() => _vencordService.BuildDiagnosticsText(AppVersionText, IsDebugMode, InstallerStatusTitle);
    public string GetDiagnosticsJson() => _vencordService.BuildDiagnosticsJson(AppVersionText, IsDebugMode, InstallerStatusTitle);
    public (string TextPath, string JsonPath) ExportDiagnostics() { var r = _vencordService.ExportDiagnostics(AppVersionText, IsDebugMode, InstallerStatusTitle); RefreshRuntimeMetadata(); ShowToast("Diagnostics exported", "Reports were saved to the reports folder.", ToastKind.Success); return r; }
    public void HideToast() { IsToastVisible = false; }

    private void OnCheck()
    {
        StatusBarText = "Checking..."; IsProgressVisible = true;
        try
        {
            var r = _vencordService.Check();
            Status = r.State.ToString(); Reason = r.Reason ?? string.Empty; DiscordPath = r.DiscordPath ?? "Not found"; DiscordVersion = r.DiscordVersion ?? "Not detected"; DiscordBranch = r.Branch ?? "Not detected"; AppFolderPath = r.AppFolderPath ?? "Not found"; ResourcesPath = r.ResourcesPath ?? "Not found"; RuntimeRootPath = r.RuntimeRootPath ?? RuntimePaths.RootDirectory; StateFilePath = r.StateFilePath ?? RuntimePaths.StateFilePath; InstallerCliPath = r.InstallerCliPath ?? RuntimePaths.InstallerCliPath; IsAppAsarPresent = r.AppAsarPresent; IsMarkerPresent = r.MarkerPresent; LastCheckTime = r.LastCheckTime ?? DateTime.UtcNow; LastRepairTime = r.LastRepairTime; if (!string.IsNullOrWhiteSpace(r.LastRepairMessage)) { LastOperationTitle = r.LastRepairResult == "Success" ? "Last repair succeeded" : "Last repair recorded"; LastOperationMessage = r.LastRepairMessage; } SyncInstallerState(); RefreshRuntimeMetadata();
        }
        finally { StatusBarText = "Ready"; IsProgressVisible = false; }
    }

    private Task OnRepairAsync() { OpenRepairOptionsDialog(false); return Task.CompletedTask; }
    private Task OnFixEverythingAsync() { OpenRepairOptionsDialog(true); return Task.CompletedTask; }
    private async Task OnCloseDiscordAndRetryAsync() { if (_pendingRepairOptions is null) { _pendingRepairOptions = new RepairOptions { Mode = RepairMode.Patch, UseFallbackMirror = UseFallbackMirrorIfOfficialInstallerDownloadFails }; } await RunRepairAsync(new RepairOptions { ClearCacheBeforeRepair = false, Mode = _pendingRepairOptions.Mode, RetryAfterClosingDiscord = false, TryCloseDiscordBeforeRepair = true, UseFallbackMirror = _pendingRepairOptions.UseFallbackMirror }); }
    private Task OnCheckForUpdatesAsync() => CheckForUpdatesAsync(true);
    private async Task CheckForUpdatesAsync(bool toast)
    {
        StatusBarText = "Checking for updates..."; IsProgressVisible = true;
        try
        {
            var r = await _vencordService.CheckForUpdatesAsync(AppVersionText); UpdateStatusText = r.Message; LatestAvailableVersion = r.LatestVersion; UpdateAvailable = r.UpdateAvailable; UpdateReleaseUrl = r.ReleaseUrl; LastUpdateCheckTime = DateTime.UtcNow; _state.LastUpdateCheckTime = LastUpdateCheckTime; _state.LastUpdateCheckResult = r.Message; PersistState(); if (toast) { ShowToast(r.UpdateAvailable ? "Update available" : "Update check completed", r.Message, r.UpdateAvailable ? ToastKind.Warning : ToastKind.Info); }
        }
        finally { StatusBarText = "Ready"; IsProgressVisible = false; }
    }

    private async Task RunRepairAsync(RepairOptions options)
    {
        _pendingRepairOptions = options; StatusBarText = "Repairing..."; IsProgressVisible = true; IsRepairRunning = true; RepairMessage = "Running Vencord repair..."; RepairOutput = string.Empty; RepairError = string.Empty; CanRetryAfterClose = false; CloseDiscordRetryMessage = string.Empty;
        try
        {
            var r = await _vencordService.RepairAsync(options); var message = GetHumanReadableRepairMessage(r); RepairMessage = message; RepairOutput = r.StandardOutput; RepairError = r.StandardError; LastOperationTitle = r.Success ? "Last repair succeeded" : "Last repair failed"; LastOperationMessage = message; CanRetryAfterClose = !r.Success && r.CanRetryAfterClose; CloseDiscordRetryMessage = r.CanRetryAfterClose ? "Discord is still running. Close Discord and retry." : string.Empty; _state = _vencordService.LoadState(); ApplySavedState(_state); RefreshRuntimeMetadata(); SyncInstallerState();
            if (r.Success) { OnCheck(); ShowToast("Repair succeeded", message, ToastKind.Success); } else { if (ExportDiagnosticsAfterFailedRepair) { ExportDiagnostics(); } ShowToast(r.CanRetryAfterClose ? "Discord is still running" : "Repair failed", message, r.CanRetryAfterClose ? ToastKind.Warning : ToastKind.Error); }
        }
        finally { IsRepairRunning = false; StatusBarText = "Ready"; IsProgressVisible = false; }
    }

    private string GetHumanReadableRepairMessage(RepairResult r) => r.Success ? (r.DeepRepair ? "Deep reinstall completed successfully." : "Repair completed successfully.") : r.DiscordRunning || r.FilesLocked ? "Discord appears to be running. Close Discord completely and try Repair again." : r.InstallerMissing ? "Installer is not available." : r.DownloadFailed ? "Installer download failed. Check your internet connection and try again." : r.TimedOut || r.ExitCode == -1 ? "Repair timed out." : "Repair failed. Review diagnostics and try again.";
    private void ApplySavedState(AppState state) { LastCheckTime = state.LastCheckTime; LastRepairTime = state.LastRepairTime; LastUpdateCheckTime = state.LastUpdateCheckTime; InstallerCliPath = state.LastInstallerCliPath ?? RuntimePaths.InstallerCliPath; if (!string.IsNullOrWhiteSpace(state.LastRepairMessage)) { LastOperationTitle = state.LastRepairResult == "Success" ? "Last repair succeeded" : "Last repair failed"; LastOperationMessage = state.LastRepairMessage; } if (!string.IsNullOrWhiteSpace(state.LastUpdateCheckResult)) { UpdateStatusText = state.LastUpdateCheckResult; } }
    private void RefreshRuntimeMetadata() { RuntimePaths.EnsureRuntimeDirectories(); RuntimeRootPath = RuntimePaths.RootDirectory; StateFilePath = RuntimePaths.StateFilePath; InstallerCliPath = RuntimePaths.InstallerCliPath; LogsFolderPath = RuntimePaths.LogsDirectory; ReportsFolderPath = RuntimePaths.ReportsDirectory; _state = _vencordService.LoadState(); LastCheckTime = _state.LastCheckTime; LastRepairTime = _state.LastRepairTime; LastUpdateCheckTime = _state.LastUpdateCheckTime; }
    private void SyncInstallerState() { InstallerFlowState = HasPrimaryInstaller() ? InstallerToolState.InstallerReady : InstallerToolState.InstallerMissing; }
    private void PersistState() { _state.IsDebugMode = IsDebugMode; _state.Settings = _settings; _state.LastInstallerCliPath = InstallerCliPath; _vencordService.SaveState(_state); }
    private async void ShowToast(string title, string message, ToastKind kind) { ToastTitle = title; ToastMessage = message; ToastKind = kind; IsToastVisible = true; await Task.Delay(3200); if (ToastTitle == title && ToastMessage == message) { IsToastVisible = false; } }
    private void OnBusyStateChanged(bool isBusy) { IsBusy = isBusy; _checkCommand.RaiseCanExecuteChanged(); }
    private static string FormatDate(DateTime? value) => value.HasValue ? value.Value.ToLocalTime().ToString("g") : "Not recorded";
}
