using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.ComponentModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Venchanic.Core;
using Venchanic.UI.ViewModels;

namespace Venchanic.UI.Pages;

public sealed partial class HealthCheckPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public HealthCheckPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateStatusVisuals();
        UpdateAdvancedDiagnosticsVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.Status))
        {
            UpdateStatusVisuals();
        }

        if (e.PropertyName == nameof(DashboardViewModel.IsDebugMode))
        {
            UpdateAdvancedDiagnosticsVisibility();
        }
    }

    private void UpdateAdvancedDiagnosticsVisibility()
    {
        AdvancedDiagnosticsCard.Visibility = ViewModel.IsDebugMode ? Visibility.Visible : Visibility.Collapsed;
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

        StatusBadgeBorder.Background = new SolidColorBrush(background);
        StatusBadgeBorder.BorderBrush = new SolidColorBrush(border);
        StatusBadgeTextBlock.Foreground = new SolidColorBrush(foreground);
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
}
