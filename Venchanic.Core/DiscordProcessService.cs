using System.Diagnostics;

namespace Venchanic.Core;

public sealed class DiscordProcessService
{
    private static readonly string[] DiscordProcessNames =
    [
        "Discord",
        "DiscordPTB",
        "DiscordCanary"
    ];

    public async Task<DiscordCloseResult> TryCloseDiscordFamilyAsync()
    {
        var matchingProcesses = Process
            .GetProcesses()
            .Where(process => DiscordProcessNames.Contains(process.ProcessName, StringComparer.OrdinalIgnoreCase))
            .ToArray();

        if (matchingProcesses.Length == 0)
        {
            return new DiscordCloseResult
            {
                Success = true,
                Message = "Discord was not running."
            };
        }

        var forcedKillUsed = false;
        var closedCount = 0;

        foreach (var process in matchingProcesses)
        {
            try
            {
                if (process.HasExited)
                {
                    closedCount++;
                    continue;
                }

                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    process.CloseMainWindow();
                    await Task.Delay(400);
                }

                if (!process.HasExited)
                {
                    await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
                }

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    forcedKillUsed = true;
                    await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(3));
                }

                if (process.HasExited)
                {
                    closedCount++;
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        var success = closedCount == matchingProcesses.Length;
        return new DiscordCloseResult
        {
            Success = success,
            AnyProcessFound = true,
            ForcedKillUsed = forcedKillUsed,
            ClosedProcessCount = closedCount,
            Message = success
                ? "Discord was closed successfully."
                : "Venchanic could not close every Discord process."
        };
    }
}
