using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;

namespace Venchanic.Core;

public sealed class RepairService
{
    private const string InstallerFileName = "VencordInstallerCli.exe";
    private const string InstallerDownloadUrl = "https://github.com/Vencord/Installer/releases/latest/download/VencordInstallerCli.exe";
    private const string InstallerFallbackMirrorUrl = "https://raw.githubusercontent.com/GustavoFringo140/Venchanic/main/VencordInstallerCli.exe";
    private static readonly TimeSpan RepairTimeout = TimeSpan.FromSeconds(90);
    private const string ExitPrompt = "Press Enter to exit";
    private static readonly HttpClient HttpClient = new();

    public bool HasInstaller()
    {
        return !string.IsNullOrWhiteSpace(FindInstallerPath());
    }

    public bool HasPrimaryInstaller()
    {
        return File.Exists(RuntimePaths.InstallerCliPath);
    }

    public async Task<bool> DownloadInstallerAsync(bool forceRedownload = false, bool useFallbackMirror = true)
    {
        RuntimePaths.EnsureRuntimeDirectories();

        if (forceRedownload && File.Exists(RuntimePaths.InstallerCliPath))
        {
            try
            {
                File.Delete(RuntimePaths.InstallerCliPath);
            }
            catch
            {
                return false;
            }
        }

        if (!forceRedownload && File.Exists(RuntimePaths.InstallerCliPath))
        {
            return true;
        }

        var downloadUrls = new List<string>
        {
            InstallerDownloadUrl
        };

        if (useFallbackMirror)
        {
            downloadUrls.Add(InstallerFallbackMirrorUrl);
        }

        foreach (var url in downloadUrls)
        {
            try
            {
                await using var responseStream = await HttpClient.GetStreamAsync(url);
                await using var fileStream = new FileStream(RuntimePaths.InstallerCliPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await responseStream.CopyToAsync(fileStream);
                return true;
            }
            catch
            {
            }
        }

        return false;
    }

    public async Task<RepairResult> RepairAsync(string? branch, string? location, RepairMode mode)
    {
        RuntimePaths.EnsureRuntimeDirectories();

        var targetArguments = BuildTargetArguments(branch, location);
        if (targetArguments is null)
        {
            return new RepairResult
            {
                Success = false,
                Message = "Discord install target was not found."
            };
        }

        var installerPath = FindInstallerPath();
        if (installerPath is null)
        {
            return new RepairResult
            {
                Success = false,
                InstallerMissing = true,
                Message = "VencordInstallerCli.exe not found."
            };
        }

        if (mode == RepairMode.DeepReinstall)
        {
            return await RunDeepRepairAsync(installerPath, targetArguments);
        }

        return await RunInstallerActionAsync(installerPath, "-repair", targetArguments, deepRepair: false);
    }

    private async Task<RepairResult> RunDeepRepairAsync(string installerPath, string targetArguments)
    {
        var uninstallResult = await RunInstallerActionAsync(installerPath, "-uninstall", targetArguments, deepRepair: true);
        var installResult = await RunInstallerActionAsync(installerPath, "-install", targetArguments, deepRepair: true);

        return new RepairResult
        {
            Success = installResult.Success,
            ExitCode = installResult.ExitCode,
            StandardOutput = CombineSections(
                "Uninstall Output",
                uninstallResult.StandardOutput,
                "Install Output",
                installResult.StandardOutput),
            StandardError = CombineSections(
                "Uninstall Error",
                uninstallResult.StandardError,
                "Install Error",
                installResult.StandardError),
            Message = installResult.Success
                ? "Deep reinstall completed successfully."
                : installResult.Message,
            InstallerMissing = installResult.InstallerMissing,
            DiscordRunning = uninstallResult.DiscordRunning || installResult.DiscordRunning,
            FilesLocked = uninstallResult.FilesLocked || installResult.FilesLocked,
            CanRetryAfterClose = uninstallResult.CanRetryAfterClose || installResult.CanRetryAfterClose,
            DownloadFailed = installResult.DownloadFailed,
            TimedOut = uninstallResult.TimedOut || installResult.TimedOut,
            DeepRepair = true
        };
    }

    private async Task<RepairResult> RunInstallerActionAsync(string installerPath, string actionFlag, string targetArguments, bool deepRepair)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = installerPath,
            Arguments = $"{actionFlag} {targetArguments}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        var standardOutputBuilder = new StringBuilder();
        var standardErrorBuilder = new StringBuilder();

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            return new RepairResult
            {
                Success = false,
                Message = "Failed to start VencordInstallerCli.exe.",
                DeepRepair = deepRepair
            };
        }

        var standardOutputTask = ReadStandardOutputAsync(process, standardOutputBuilder);
        var standardErrorTask = ReadStandardErrorAsync(process.StandardError, standardErrorBuilder);

        using var timeoutCts = new CancellationTokenSource(RepairTimeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            await Task.WhenAll(standardOutputTask, standardErrorTask);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
            }

            await Task.WhenAll(standardOutputTask, standardErrorTask);

            return new RepairResult
            {
                Success = false,
                ExitCode = -1,
                StandardOutput = standardOutputBuilder.ToString(),
                StandardError = standardErrorBuilder.ToString(),
                Message = "Repair timed out.",
                TimedOut = true,
                DeepRepair = deepRepair
            };
        }

        var standardOutput = standardOutputBuilder.ToString();
        var standardError = standardErrorBuilder.ToString();
        var discordRunning = ContainsDiscordRunningError(standardOutput, standardError);
        var filesLocked = ContainsFilesLockedError(standardOutput, standardError);

        return new RepairResult
        {
            Success = process.ExitCode == 0,
            ExitCode = process.ExitCode,
            StandardOutput = standardOutput,
            StandardError = standardError,
            DiscordRunning = discordRunning,
            FilesLocked = filesLocked,
            CanRetryAfterClose = discordRunning || filesLocked,
            Message = process.ExitCode == 0
                ? (deepRepair ? "Deep reinstall completed successfully." : "Repair succeeded.")
                : discordRunning || filesLocked
                    ? "Discord is still running. Close Discord and try again."
                    : $"Repair failed with exit code {process.ExitCode}.",
            DeepRepair = deepRepair
        };
    }

    private static async Task ReadStandardOutputAsync(Process process, StringBuilder outputBuilder)
    {
        var buffer = new char[256];
        var promptHandled = false;

        while (true)
        {
            var read = await process.StandardOutput.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            outputBuilder.Append(buffer, 0, read);

            if (!promptHandled && outputBuilder.ToString().Contains(ExitPrompt, StringComparison.OrdinalIgnoreCase))
            {
                promptHandled = true;

                try
                {
                    await process.StandardInput.WriteLineAsync();
                    await process.StandardInput.FlushAsync();
                }
                catch
                {
                }
            }
        }
    }

    private static async Task ReadStandardErrorAsync(StreamReader reader, StringBuilder errorBuilder)
    {
        var buffer = new char[256];

        while (true)
        {
            var read = await reader.ReadAsync(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            errorBuilder.Append(buffer, 0, read);
        }
    }

    private static string? BuildTargetArguments(string? branch, string? location)
    {
        if (!string.IsNullOrWhiteSpace(location))
        {
            return $"-location \"{location}\"";
        }

        if (!string.IsNullOrWhiteSpace(branch))
        {
            return $"-branch {branch}";
        }

        return null;
    }

    private static bool ContainsDiscordRunningError(string standardOutput, string standardError)
    {
        return ContainsAny(
            standardOutput,
            standardError,
            "files are used by a different process",
            "Make sure you close Discord before trying to patch",
            "Failed to kill Discord",
            "TerminateProcess",
            "close Discord",
            "Discord is running");
    }

    private static bool ContainsFilesLockedError(string standardOutput, string standardError)
    {
        return ContainsAny(
            standardOutput,
            standardError,
            "files are used by a different process",
            "Access is denied",
            "TerminateProcess",
            "locked",
            "in use");
    }

    private static bool ContainsAny(string standardOutput, string standardError, params string[] values)
    {
        foreach (var value in values)
        {
            if (standardOutput.Contains(value, StringComparison.OrdinalIgnoreCase) ||
                standardError.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string CombineSections(string firstTitle, string firstValue, string secondTitle, string secondValue)
    {
        var builder = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(firstValue))
        {
            builder.AppendLine(firstTitle);
            builder.AppendLine(firstValue.Trim());
        }

        if (!string.IsNullOrWhiteSpace(secondValue))
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine(secondTitle);
            builder.AppendLine(secondValue.Trim());
        }

        return builder.ToString();
    }

    private static string? FindInstallerPath()
    {
        foreach (var candidate in GetCandidatePaths())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetCandidatePaths()
    {
        yield return RuntimePaths.InstallerCliPath;
        yield return Path.Combine(AppContext.BaseDirectory, "tools", InstallerFileName);
        yield return Path.Combine(AppContext.BaseDirectory, InstallerFileName);
        yield return @"C:\Users\zeo\Venchanic\VencordInstallerCli.exe";
        yield return @"C:\Users\zeo\Venchanic\Venchanic.UI\VencordInstallerCli.exe";
    }
}
