using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Venchanic.Core;
using Venchanic.UI.ViewModels;

namespace Venchanic.UI.Pages;

public sealed partial class SettingsPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public SettingsPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateResetPromptVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.ShowResetStatePrompt))
        {
            UpdateResetPromptVisibility();
        }
    }

    private void UpdateResetPromptVisibility()
    {
        ResetStatePrompt.Visibility = ViewModel.ShowResetStatePrompt ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OpenRuntimeFolderButton_Click(object sender, RoutedEventArgs e)
    {
        RuntimePaths.EnsureRuntimeDirectories();
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{RuntimePaths.RootDirectory}\"",
            UseShellExecute = true
        });
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

    private void ResetLocalStateButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleResetStatePrompt();
    }

    private void ConfirmResetStateButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ResetLocalState();
    }

    private void CancelResetStateButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.ToggleResetStatePrompt();
    }

    private void WebsiteLinkButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalLink("https://zeozcb.ru");
    }

    private void GitHubLinkButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalLink("https://github.com/GustavoFringo140");
    }

    private void TelegramLinkButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalLink("https://t.me/wojiras");
    }

    private void OpenReleasePageButton_Click(object sender, RoutedEventArgs e)
    {
        OpenExternalLink(ViewModel.UpdateReleaseUrl);
    }

    private static void OpenExternalLink(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
}
