[CmdletBinding()]
param(
    [ValidateSet("x64", "x86")]
    [string]$Platform = "x64",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$buildPackageScript = Join-Path $PSScriptRoot "build-release-package.ps1"
$payloadScriptSource = Join-Path $PSScriptRoot "installer\Install-VenchanicPackage.ps1"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$packageRoot = Join-Path $artifactsRoot "packages\$Platform\$Configuration"
$certPath = Join-Path $artifactsRoot "certificates\Venchanic.Dev.cer"
$installerRoot = Join-Path $artifactsRoot "installer\$Platform\$Configuration"
$payloadRoot = Join-Path $installerRoot "payload"
$sedPath = Join-Path $installerRoot "VenchanicInstaller.sed"
$outputExe = Join-Path $installerRoot "Venchanic-Setup-$Platform.exe"

New-Item -ItemType Directory -Force -Path $installerRoot | Out-Null
New-Item -ItemType Directory -Force -Path $payloadRoot | Out-Null

& $buildPackageScript -Platform $Platform -Configuration $Configuration

$msix = Get-ChildItem $packageRoot -Recurse -Filter *.msix | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if (-not $msix) {
    throw "MSIX package was not found in $packageRoot"
}

if (-not (Test-Path $certPath)) {
    throw "Certificate file was not found: $certPath"
}

if (-not (Test-Path $payloadScriptSource)) {
    throw "Installer payload script was not found: $payloadScriptSource"
}

Copy-Item $msix.FullName (Join-Path $payloadRoot $msix.Name) -Force
Copy-Item $certPath (Join-Path $payloadRoot "Venchanic.Dev.cer") -Force
Copy-Item $payloadScriptSource (Join-Path $payloadRoot "Install-VenchanicPackage.ps1") -Force

$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=0
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=Venchanic installation has finished.
TargetName=$outputExe
FriendlyName=Venchanic Setup
AppLaunched=powershell.exe -ExecutionPolicy Bypass -File Install-VenchanicPackage.ps1
PostInstallCmd=<None>
AdminQuietInstCmd=powershell.exe -ExecutionPolicy Bypass -File Install-VenchanicPackage.ps1
UserQuietInstCmd=powershell.exe -ExecutionPolicy Bypass -File Install-VenchanicPackage.ps1
SourceFiles=SourceFiles
[SourceFiles]
SourceFiles0=$payloadRoot
[SourceFiles0]
%FILE0%= 
%FILE1%= 
%FILE2%= 
[Strings]
FILE0=$(Split-Path -Leaf $msix.FullName)
FILE1=Venchanic.Dev.cer
FILE2=Install-VenchanicPackage.ps1
"@

Set-Content -Path $sedPath -Value $sed -Encoding ASCII

& iexpress.exe /N $sedPath | Out-Null

if (-not (Test-Path $outputExe)) {
    throw "Installer EXE was not generated: $outputExe"
}

$notesPath = Join-Path $installerRoot "INSTALLER.txt"
$notes = @"
Venchanic installer EXE

Installer:
$outputExe

Underlying package:
$($msix.FullName)

Certificate:
$certPath
"@
Set-Content -Path $notesPath -Value $notes -Encoding UTF8

Write-Host "Installer EXE: $outputExe" -ForegroundColor Green
Write-Host "MSIX: $($msix.FullName)" -ForegroundColor Green
Write-Host "Certificate: $certPath" -ForegroundColor Green
