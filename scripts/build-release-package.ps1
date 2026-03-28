[CmdletBinding()]
param(
    [ValidateSet("x64", "x86")]
    [string]$Platform = "x64",

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$uiProjectPath = Join-Path $repoRoot "Venchanic.UI\Venchanic.UI.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$packageRoot = Join-Path $artifactsRoot "packages\$Platform\$Configuration"
$certRoot = Join-Path $artifactsRoot "certificates"
$certCerPath = Join-Path $certRoot "Venchanic.Dev.cer"
$certPfxPath = Join-Path $certRoot "Venchanic.Dev.pfx"
$certPasswordPlain = "venchanic-dev"
$certPassword = ConvertTo-SecureString -String $certPasswordPlain -AsPlainText -Force
$certSubject = "CN=Venchanic"

New-Item -ItemType Directory -Force -Path $packageRoot | Out-Null
New-Item -ItemType Directory -Force -Path $certRoot | Out-Null

if (-not (Test-Path $certPfxPath) -or -not (Test-Path $certCerPath)) {
    Write-Host "==> Creating self-signed development certificate" -ForegroundColor Cyan
    $certificate = New-SelfSignedCertificate `
        -Type CodeSigningCert `
        -Subject $certSubject `
        -CertStoreLocation "Cert:\CurrentUser\My" `
        -Provider "Microsoft Enhanced RSA and AES Cryptographic Provider" `
        -KeySpec Signature `
        -KeyExportPolicy Exportable `
        -NotAfter (Get-Date).AddYears(3)

    Export-PfxCertificate `
        -Cert $certificate `
        -FilePath $certPfxPath `
        -Password $certPassword | Out-Null

    Export-Certificate `
        -Cert $certificate `
        -FilePath $certCerPath | Out-Null
}

$signingCertificate = Get-ChildItem "Cert:\CurrentUser\My" |
    Where-Object { $_.Subject -eq $certSubject } |
    Sort-Object NotAfter -Descending |
    Select-Object -First 1

if (-not $signingCertificate) {
    throw "Signing certificate '$certSubject' was not found in Cert:\CurrentUser\My"
}

Write-Host "==> Building signed MSIX package ($Platform $Configuration)" -ForegroundColor Cyan
$env:DOTNET_CLI_HOME = Join-Path $env:USERPROFILE ".codex\memories"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

dotnet publish $uiProjectPath `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxBundle=Never `
    -p:UapAppxPackageBuildMode=SideloadOnly `
    -p:AppxPackageDir="$packageRoot\" `
    -p:PackageCertificateThumbprint=$($signingCertificate.Thumbprint) `
    -p:AppxPackageSigningEnabled=true

if ($LASTEXITCODE -ne 0) {
    throw "MSIX package build failed."
}

$msix = Get-ChildItem $packageRoot -Recurse -Include *.msix,*.appx | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if (-not $msix) {
    throw "Package build finished, but no .msix or .appx artifact was found in $packageRoot"
}

$readmePath = Join-Path $packageRoot "INSTALL.txt"
$readmeContent = @"
Venchanic packaged release artifact

Package:
$($msix.FullName)

Certificate:
$certCerPath

Install:
1. Install the certificate into Current User / Trusted People.
2. Run:
   Add-AppxPackage -Path "$($msix.FullName)"
"@
Set-Content -Path $readmePath -Value $readmeContent -Encoding UTF8

Write-Host "Package: $($msix.FullName)" -ForegroundColor Green
Write-Host "Certificate: $certCerPath" -ForegroundColor Green
Write-Host "Install notes: $readmePath" -ForegroundColor Green
