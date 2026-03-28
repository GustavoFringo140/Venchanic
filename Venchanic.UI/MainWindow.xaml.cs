using System.ComponentModel;
using System;
using System.IO;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using WinRT.Interop;
using Venchanic.UI.Pages;
using Venchanic.UI.ViewModels;

namespace Venchanic.UI;

public sealed partial class MainWindow : Window
{
    public DashboardViewModel ViewModel { get; } = new();
    private readonly DashboardPage _dashboardPage;
    private readonly HealthCheckPage _healthCheckPage;
    private readonly RepairPage _repairPage;
    private readonly SettingsPage _settingsPage;

    public MainWindow()
    {
        InitializeComponent();
        Title = "Venchanic";
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarDragRegion);
        _dashboardPage = new DashboardPage(ViewModel);
        _healthCheckPage = new HealthCheckPage(ViewModel);
        _repairPage = new RepairPage(ViewModel);
        _settingsPage = new SettingsPage(ViewModel);
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ConfigureWindow();
        NavigateToSelectedTab();
        UpdateProgressBarVisibility();
        UpdateNavigationState();
        UpdateInstallerDialogVisibility();
        UpdateRepairDialogVisibility();
        UpdateToastVisibility();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DashboardViewModel.IsProgressVisible))
        {
            UpdateProgressBarVisibility();
        }

        if (e.PropertyName == nameof(DashboardViewModel.SelectedTab))
        {
            NavigateToSelectedTab();
            UpdateNavigationState();
        }

        if (e.PropertyName is nameof(DashboardViewModel.IsInstallerDialogOpen)
            or nameof(DashboardViewModel.IsInstallerDownloadInProgress)
            or nameof(DashboardViewModel.InstallerDialogTitle)
            or nameof(DashboardViewModel.InstallerDialogMessage)
            or nameof(DashboardViewModel.InstallerDialogStatusText)
            or nameof(DashboardViewModel.InstallerDialogPrimaryButtonText)
            or nameof(DashboardViewModel.InstallerDialogSecondaryButtonText)
            or nameof(DashboardViewModel.IsInstallerDialogPrimaryVisible)
            or nameof(DashboardViewModel.IsInstallerDialogSecondaryVisible))
        {
            UpdateInstallerDialogVisibility();
        }

        if (e.PropertyName is nameof(DashboardViewModel.IsRepairOptionsDialogOpen)
            or nameof(DashboardViewModel.RepairDialogTitle)
            or nameof(DashboardViewModel.RepairDialogMessage))
        {
            UpdateRepairDialogVisibility();
        }

        if (e.PropertyName is nameof(DashboardViewModel.IsToastVisible)
            or nameof(DashboardViewModel.ToastTitle)
            or nameof(DashboardViewModel.ToastMessage)
            or nameof(DashboardViewModel.ToastKind))
        {
            UpdateToastVisibility();
        }
    }

    private void UpdateProgressBarVisibility()
    {
        StatusProgressBar.Visibility = ViewModel.IsProgressVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateInstallerDialogVisibility()
    {
        InstallerOverlay.Visibility = ViewModel.IsInstallerDialogOpen ? Visibility.Visible : Visibility.Collapsed;
        InstallerProgressBar.Visibility = ViewModel.IsInstallerDownloadInProgress ? Visibility.Visible : Visibility.Collapsed;
        InstallerDialogButtons.Visibility =
            ViewModel.IsInstallerDialogPrimaryVisible || ViewModel.IsInstallerDialogSecondaryVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        InstallerDialogTitleText.Text = ViewModel.InstallerDialogTitle;
        InstallerDialogMessageText.Text = ViewModel.InstallerDialogMessage;
        InstallerDialogStatusText.Text = ViewModel.InstallerDialogStatusText;
        InstallerDialogStatusText.Visibility = string.IsNullOrWhiteSpace(ViewModel.InstallerDialogStatusText)
            ? Visibility.Collapsed
            : Visibility.Visible;
        InstallerNoButton.Content = ViewModel.InstallerDialogSecondaryButtonText;
        InstallerYesButton.Content = ViewModel.InstallerDialogPrimaryButtonText;
        InstallerNoButton.Visibility = ViewModel.IsInstallerDialogSecondaryVisible ? Visibility.Visible : Visibility.Collapsed;
        InstallerYesButton.Visibility = ViewModel.IsInstallerDialogPrimaryVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateRepairDialogVisibility()
    {
        RepairOptionsOverlay.Visibility = ViewModel.IsRepairOptionsDialogOpen ? Visibility.Visible : Visibility.Collapsed;
        RepairDialogTitleText.Text = ViewModel.RepairDialogTitle;
        RepairDialogMessageText.Text = ViewModel.RepairDialogMessage;
    }

    private void UpdateToastVisibility()
    {
        ToastBorder.Visibility = ViewModel.IsToastVisible ? Visibility.Visible : Visibility.Collapsed;
        ToastTitleText.Text = ViewModel.ToastTitle;
        ToastMessageText.Text = ViewModel.ToastMessage;

        var background = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x20, 0x22, 0x26);
        var border = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x35, 0x38, 0x40);

        switch (ViewModel.ToastKind)
        {
            case ToastKind.Success:
                background = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x1D, 0x2D, 0x25);
                border = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x43, 0x9B, 0x69);
                break;
            case ToastKind.Warning:
                background = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x2F, 0x27, 0x1A);
                border = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xD4, 0x9A, 0x36);
                break;
            case ToastKind.Error:
                background = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0x31, 0x22, 0x20);
                border = Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xD0, 0x6A, 0x4A);
                break;
        }

        ToastBorder.Background = new SolidColorBrush(background);
        ToastBorder.BorderBrush = new SolidColorBrush(border);
    }

    private void UpdateNavigationState()
    {
        ApplyTabStyle(DashboardTabButton, ViewModel.SelectedTab == DashboardViewModel.DashboardTab);
        ApplyTabStyle(HealthCheckTabButton, ViewModel.SelectedTab == DashboardViewModel.HealthCheckTab);
        ApplyTabStyle(RepairTabButton, ViewModel.SelectedTab == DashboardViewModel.RepairTab);
        ApplyTabStyle(SettingsTabButton, ViewModel.SelectedTab == DashboardViewModel.SettingsTab);
    }

    private void NavigateToSelectedTab()
    {
        DashboardHost.Content = ViewModel.SelectedTab switch
        {
            DashboardViewModel.HealthCheckTab => _healthCheckPage,
            DashboardViewModel.RepairTab => _repairPage,
            DashboardViewModel.SettingsTab => _settingsPage,
            _ => _dashboardPage
        };
    }

    private static void ApplyTabStyle(Button button, bool isActive)
    {
        button.Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(
            isActive ? (byte)0x26 : (byte)0x08,
            isActive ? (byte)0x00 : (byte)0x00,
            isActive ? (byte)0x7A : (byte)0x00,
            isActive ? (byte)0xCC : (byte)0x00));
        button.BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(
            isActive ? (byte)0x32 : (byte)0x16,
            isActive ? (byte)0x00 : (byte)0x00,
            isActive ? (byte)0x7A : (byte)0x00,
            isActive ? (byte)0xCC : (byte)0x00));
        button.Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(
            0xFF,
            isActive ? (byte)0xF4 : (byte)0xD8,
            isActive ? (byte)0xF8 : (byte)0xDC,
            isActive ? (byte)0xFC : (byte)0xE1));
        button.FontWeight = isActive ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal;
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new SizeInt32(1240, 820));

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = appWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            appWindow.SetIcon(iconPath);
        }
    }

    private void DashboardTabButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = DashboardViewModel.DashboardTab;
    }

    private void HealthCheckTabButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = DashboardViewModel.HealthCheckTab;
    }

    private void RepairTabButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = DashboardViewModel.RepairTab;
    }

    private void SettingsTabButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedTab = DashboardViewModel.SettingsTab;
    }

    private void InstallerNoButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelInstallerDialogFlow();
    }

    private async void InstallerYesButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ConfirmInstallerDownloadAsync();
    }

    private void ToastCloseButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.HideToast();
    }

    private void RepairOptionsCancelButton_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelRepairOptionsDialog();
    }

    private async void RepairOptionsConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.ConfirmRepairOptionsAsync();
    }
}
