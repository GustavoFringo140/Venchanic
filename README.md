# Venchanic

> Smart Windows companion for checking, repairing and maintaining Vencord installs on Discord.

![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-0f172a?style=flat-square)
![Framework](https://img.shields.io/badge/UI-WinUI%203-1f2937?style=flat-square)
![Runtime](https://img.shields.io/badge/.NET-8-111827?style=flat-square)

Venchanic is a packaged WinUI 3 utility focused on one job: making Vencord repair on Discord cleaner, faster and less annoying.

It is not a mod manager, not a launcher, and not a bloated dashboard app. It is a practical repair tool with diagnostics, automation and a predictable Windows-native UI.

## What it does

- 🔎 Runs health checks for your current Discord install
- 🩹 Repairs Vencord using the official installer CLI
- 📦 Auto-downloads the installer when it is missing
- 🧠 Stores runtime state, last check and last repair results
- 🧰 Exports diagnostics for GitHub issues
- 📝 Writes logs into `%LOCALAPPDATA%\\Venchanic`
- 🔁 Supports guided repair and retry after closing Discord
- 🚦 Checks for newer Venchanic releases

## UI overview

- `Dashboard`  
  General overview, current status and recommended next action.

- `Health Check`  
  Discord detection details, quick diagnostics actions and install summary.

- `Repair`  
  Installer status, repair actions, guided flow and advanced diagnostics output.

- `Settings`  
  Automation toggles, debug mode, runtime folders, update actions and project links.

## Features

### Health and diagnostics

- Detects Discord install path, branch and version
- Checks `app.asar`, resources and Vencord markers
- Persists `LastCheckTime`, `LastRepairTime`, last result and message
- Exports diagnostics reports as:
  - `txt`
  - `json`

### Repair flow

- Standard patch repair
- Deep reinstall flow:
  - uninstall
  - install
- Optional cache cleanup before repair
- Retry flow when Discord is still running
- Optional automatic Discord close attempt before repair

### Runtime storage

All runtime data is stored under:

```text
%LOCALAPPDATA%\Venchanic\
```

Including:

- `tools`
- `logs`
- `reports`
- `state.json`

## Screenshots

Add screenshots here after publish:

- `Dashboard`
- `Health Check`
- `Repair`
- `Settings`

## Installation

### Packaged app

Use the packaged WinUI build from Releases when it is published.

### Development build

Requirements:

- Windows 10/11
- .NET 8 SDK
- Windows App SDK / WinUI 3 development environment
- Visual Studio 2022 or compatible MSBuild tooling

Build:

```powershell
dotnet build .\Venchanic.UI\Venchanic.UI.csproj -p:Platform=x86
```

Run packaged app after registration:

```powershell
$appId = 'shell:AppsFolder\Venchanic.UI_xmkfxnxv2eh86!App'
Start-Process explorer.exe $appId
```

## Installer CLI

Venchanic relies on `VencordInstallerCli.exe`.

Download flow:

1. Official Vencord source
2. Fallback mirror from this repository root

Expected filename:

```text
VencordInstallerCli.exe
```

Runtime location:

```text
%LOCALAPPDATA%\Venchanic\tools\VencordInstallerCli.exe
```

## Diagnostics and logs

Venchanic is designed to be issue-friendly.

Useful output locations:

- Logs: `%LOCALAPPDATA%\Venchanic\logs`
- Reports: `%LOCALAPPDATA%\Venchanic\reports`
- State: `%LOCALAPPDATA%\Venchanic\state.json`

If you open an issue, include:

- current Venchanic version
- current Discord version
- health state
- exported diagnostics report
- relevant log file

## Status

Current focus:

- stable packaged app behavior
- predictable repair UX
- practical diagnostics
- clean Windows-native presentation

## Project links

- GitHub: `GustavoFringo140`
- Telegram: `@wojiras`
- Discord: `q_w.z`
- Website: https://zeozcb.ru

## Disclaimer

- Venchanic is an unofficial utility.
- It does not replace the official Vencord installer.
- Use it only if you understand what patching Discord means on your system.
