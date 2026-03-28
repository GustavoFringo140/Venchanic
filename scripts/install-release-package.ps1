[CmdletBinding()]
param(
    [ValidateSet("x64", "x86")]
    [string]$Platform = "x64",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipLaunch
)

$ErrorActionPreference = "Stop"

$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
$isAdministrator = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdministrator) {
    $argumentList = @(
        "-ExecutionPolicy", "Bypass",
        "-File", ('"{0}"' -f $PSCommandPath),
        "-Platform", $Platform,
        "-Configuration", $Configuration
    )

    if ($SkipLaunch) {
        $argumentList += "-SkipLaunch"
    }

    Start-Process powershell.exe -Verb RunAs -ArgumentList $argumentList
    exit
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$packageRoot = Join-Path $repoRoot "artifacts\packages\$Platform\$Configuration"
$certPath = Join-Path $repoRoot "artifacts\certificates\Venchanic.Dev.cer"
$packageName = "Venchanic.UI"

$msix = Get-ChildItem $packageRoot -Recurse -Include *.msix,*.appx -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (-not $msix) {
    throw "No release package was found in $packageRoot. Run build-release-package.ps1 first."
}

if (-not (Test-Path $certPath)) {
    throw "Certificate file was not found: $certPath"
}

Write-Host "==> Trusting development certificate" -ForegroundColor Cyan
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null

Write-Host "==> Removing installed Venchanic.UI packages" -ForegroundColor Cyan
Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Removing $($_.PackageFullName)" -ForegroundColor Yellow
    Remove-AppxPackage -Package $_.PackageFullName
}

Write-Host "==> Installing package" -ForegroundColor Cyan
Add-AppxPackage -Path $msix.FullName

$package = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $package) {
    throw "Package installation completed, but Venchanic.UI is not installed."
}

Write-Host "Installed: $($package.PackageFullName)" -ForegroundColor Green

if (-not $SkipLaunch) {
    $appId = "shell:AppsFolder\$($package.PackageFamilyName)!App"
    Write-Host "==> Launching packaged app" -ForegroundColor Cyan
    Start-Process explorer.exe $appId
}
