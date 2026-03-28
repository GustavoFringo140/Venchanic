[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$packageName = "Venchanic.UI"

$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
$isAdministrator = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdministrator) {
    Start-Process powershell.exe -Verb RunAs -ArgumentList @(
        "-ExecutionPolicy", "Bypass",
        "-File", ('"{0}"' -f $MyInvocation.MyCommand.Path)
    )
    exit
}

$certPath = Join-Path $scriptRoot "Venchanic.Dev.cer"
$msix = Get-ChildItem $scriptRoot -Filter *.msix -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if (-not (Test-Path $certPath)) {
    throw "Certificate file was not found beside the installer payload."
}

if (-not $msix) {
    throw "MSIX package was not found beside the installer payload."
}

Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\TrustedPeople" | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\CurrentUser\Root" | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\LocalMachine\TrustedPeople" | Out-Null
Import-Certificate -FilePath $certPath -CertStoreLocation "Cert:\LocalMachine\Root" | Out-Null

Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue | ForEach-Object {
    Remove-AppxPackage -Package $_.PackageFullName
}

Add-AppxPackage -Path $msix.FullName

$installedPackage = Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $installedPackage) {
    throw "Package installation completed, but Venchanic.UI is not installed."
}

$appId = "shell:AppsFolder\$($installedPackage.PackageFamilyName)!App"
Start-Process explorer.exe $appId
