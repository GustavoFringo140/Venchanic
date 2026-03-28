[CmdletBinding()]
param(
    [ValidateSet("x64", "x86")]
    [string]$Platform = "x64",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [switch]$SkipLaunch
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$uiProjectPath = Join-Path $repoRoot "Venchanic.UI\Venchanic.UI.csproj"
$targetFramework = "net8.0-windows10.0.19041.0"
$runtimeFolder = if ($Platform -eq "x64") { "win-x64" } else { "win-x86" }
$manifestPath = Join-Path $repoRoot "Venchanic.UI\bin\$Platform\$Configuration\$targetFramework\$runtimeFolder\AppxManifest.xml"
$packageName = "Venchanic.UI"

Write-Host "==> Building Venchanic ($Platform $Configuration)" -ForegroundColor Cyan
$env:DOTNET_CLI_HOME = Join-Path $env:USERPROFILE ".codex\memories"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
dotnet build $uiProjectPath -p:Platform=$Platform -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

if (-not (Test-Path $manifestPath)) {
    throw "AppxManifest.xml was not found at: $manifestPath"
}

Write-Host "==> Stopping running Venchanic.UI processes" -ForegroundColor Cyan
Get-Process -Name "Venchanic.UI" -ErrorAction SilentlyContinue | Stop-Process -Force

Write-Host "==> Removing installed Venchanic.UI packages" -ForegroundColor Cyan
$installedPackages = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue
foreach ($package in $installedPackages) {
    Write-Host "Removing $($package.PackageFullName)" -ForegroundColor Yellow
    Remove-AppxPackage -Package $package.PackageFullName
}

Write-Host "==> Registering package manifest" -ForegroundColor Cyan
Add-AppxPackage -Register $manifestPath -ForceApplicationShutdown

$registeredPackage = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue |
    Sort-Object -Property Architecture -Descending |
    Select-Object -First 1

if (-not $registeredPackage) {
    throw "Package registration completed, but Venchanic.UI was not found afterwards."
}

Write-Host "Registered: $($registeredPackage.PackageFullName)" -ForegroundColor Green

if (-not $SkipLaunch) {
    $appId = "shell:AppsFolder\$($registeredPackage.PackageFamilyName)!App"
    Write-Host "==> Launching packaged app" -ForegroundColor Cyan
    Start-Process explorer.exe $appId
}
