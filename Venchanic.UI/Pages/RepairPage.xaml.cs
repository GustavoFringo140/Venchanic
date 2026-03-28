using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Venchanic.Core;
using Venchanic.UI.ViewModels;

namespace Venchanic.UI.Pages;

public sealed partial class RepairPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public RepairPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateAdvancedDiagnosticsVisibility();
        UpdateRepairActivityVisibility();
        UpdateButtonState();
        UpdateStatusVisuals();
        UpdateInstallerButtonsVisibility();
        UpdateRetryButtonVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(DashboardViewModel.IsDebugMode) or nameof(DashboardViewModel.HasRepairDetails) or nameof(DashboardViewModel.RepairOutput) or nameof(DashboardViewModel.RepairError))
        {
            UpdateAdvancedDiagnosticsVisibility();
        }

        if (e.PropertyName == nameof(DashboardViewModel.IsRepairRunning))
        {
            UpdateRepairActivityVisibility();
            UpdateButtonState();
        }

        if (e.PropertyName is nameof(DashboardViewModel.Status) or nameof(DashboardViewModel.InstallerFlowState))
        {
            UpdateStatusVisuals();
            UpdateInstallerButtonsVisibility();
        }

        if (e.PropertyName == nameof(DashboardViewModel.CanRetryAfterClose))
        {
            UpdateRetryButtonVisibility();
        }
    }

    private void UpdateAdvancedDiagnosticsVisibility()
    {
        var showCard = ViewModel.IsDebugMode;
        var hasOutput = !string.IsNullOrWhiteSpace(ViewModel.RepairOutput);
        var hasError = !string.IsNullOrWhiteSpace(ViewModel.RepairError);

        AdvancedDiagnosticsCard.Visibility = showCard ? Visibility.Visible : Visibility.Collapsed;
        RepairOutputSection.Visibility = hasOutput ? Visibility.Visible : Visibility.Collapsed;
        RepairErrorSection.Visibility = hasError ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRepairActivityVisibility()
    {
        RepairActivityPanel.Visibility = ViewModel.IsRepairRunning ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateButtonState()
    {
        var isEnabled = !ViewModel.IsRepairRunning && !ViewModel.IsInstallerDownloadInProgress;
        CheckButton.IsEnabled = isEnabled;
        RepairButton.IsEnabled = isEnabled;
        DownloadInstallerButton.IsEnabled = isEnabled;
        RedownloadInstallerButton.IsEnabled = isEnabled;
        FixEverythingButton.IsEnabled = isEnabled;
        RetryCloseDiscordButton.IsEnabled = isEnabled;
    }

    private void UpdateInstallerButtonsVisibility()
    {
        var hasPrimaryInstaller = ViewModel.HasPrimaryInstaller();
        DownloadInstallerButton.Visibility = hasPrimaryInstaller ? Visibility.Collapsed : Visibility.Visible;
        RedownloadInstallerButton.Visibility = hasPrimaryInstaller ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRetryButtonVisibility()
    {
        RetryCloseDiscordButton.Visibility = ViewModel.CanRetryAfterClose ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateStatusVisuals()
    {
        var background = Microsoft.UI.ColorHelper.FromArgb(0x12, 0x5A, 0x62, 0x6E);
        var border = Microsoft.UI.ColorHelper.FromArgb(0x22, 0x5A, 0x62, 0x6E);
        var foreground = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xE5, 0xEA, 0xF0);

        switch (ViewModel.Status)
        {
            case nameof(VencordHealthState.Healthy):
                background = Microsoft.UI.ColorHelper.FromArgb(0x18, 0x43, 0x9B, 0x69);
                border = Microsoft.UI.ColorHelper.FromArgb(0x36, 0x5A, 0xB9, 0x7F);
                foreground = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xE7, 0xF5, 0xEC);
                break;
            case nameof(VencordHealthState.DiscordUpdated):
                background = Microsoft.UI.ColorHelper.FromArgb(0x18, 0xD4, 0x9A, 0x36);
                border = Microsoft.UI.ColorHelper.FromArgb(0x36, 0xE3, 0xAB, 0x4C);
                foreground = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFB, 0xF1, 0xDF);
                break;
            case nameof(VencordHealthState.BrokenInstall):
            case nameof(VencordHealthState.VencordNotDetected):
                background = Microsoft.UI.ColorHelper.FromArgb(0x18, 0xD0, 0x6A, 0x4A);
                border = Microsoft.UI.ColorHelper.FromArgb(0x36, 0xDD, 0x7B, 0x60);
                foreground = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFB, 0xEA, 0xE5);
                break;
            case nameof(VencordHealthState.DiscordNotFound):
                background = Microsoft.UI.ColorHelper.FromArgb(0x14, 0x7A, 0x86, 0x93);
                border = Microsoft.UI.ColorHelper.FromArgb(0x2A, 0x90, 0x9D, 0xAA);
                foreground = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xEA, 0xEE, 0xF2);
                break;
        }

        RepairStatusBadgeBorder.Background = new SolidColorBrush(background);
        RepairStatusBadgeBorder.BorderBrush = new SolidColorBrush(border);
        RepairStatusBadgeTextBlock.Foreground = new SolidColorBrush(foreground);
    }

    private void DownloadInstallerButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenInstallerDownloadDialog(continueToRepair: false, forceRedownload: false);
    }

    private void RedownloadInstallerButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenInstallerDownloadDialog(continueToRepair: false, forceRedownload: true);
    }

    private void OpenToolsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimePaths.EnsureRuntimeDirectories();
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{RuntimePaths.ToolsDirectory}\"",
            UseShellExecute = true
        });
    }

    private void OpenLogsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimePaths.EnsureRuntimeDirectories();
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{RuntimePaths.LogsDirectory}\"",
            UseShellExecute = true
        });
    }

    private void OpenReportsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimePaths.EnsureRuntimeDirectories();
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{RuntimePaths.ReportsDirectory}\"",
            UseShellExecute = true
        });
    }

    private void CopyDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        var package = new DataPackage();
        package.SetText(ViewModel.GetDiagnosticsText());
        Clipboard.SetContent(package);
    }

    private void ExportDiagnosticsButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ExportDiagnostics();
    }
}
