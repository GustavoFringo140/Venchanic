using System.Text;
using System.Text.Json;

namespace Venchanic.Core;

public sealed class DiagnosticsService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public string BuildTextReport(DiagnosticsSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Venchanic Diagnostics");
        builder.AppendLine("=====================");
        builder.AppendLine($"App version: {snapshot.AppVersion}");
        builder.AppendLine($"Debug mode: {snapshot.DebugModeEnabled}");
        builder.AppendLine($"Health state: {snapshot.Health.State}");
        builder.AppendLine($"Reason: {snapshot.Health.Reason ?? "n/a"}");
        builder.AppendLine($"Branch: {snapshot.Health.Branch ?? "n/a"}");
        builder.AppendLine($"Discord path: {snapshot.Health.DiscordPath ?? "n/a"}");
        builder.AppendLine($"Discord version: {snapshot.Health.DiscordVersion ?? "n/a"}");
        builder.AppendLine($"App folder: {snapshot.Health.AppFolderPath ?? "n/a"}");
        builder.AppendLine($"Resources path: {snapshot.Health.ResourcesPath ?? "n/a"}");
        builder.AppendLine($"App.Asar present: {snapshot.Health.AppAsarPresent}");
        builder.AppendLine($"Vencord marker present: {snapshot.Health.MarkerPresent}");
        builder.AppendLine($"Runtime root: {snapshot.Health.RuntimeRootPath ?? RuntimePaths.RootDirectory}");
        builder.AppendLine($"Installer CLI path: {snapshot.Health.InstallerCliPath ?? RuntimePaths.InstallerCliPath}");
        builder.AppendLine($"Installer status: {snapshot.InstallerStatus}");
        builder.AppendLine($"Last check: {FormatDate(snapshot.State.LastCheckTime)}");
        builder.AppendLine($"Last repair: {FormatDate(snapshot.State.LastRepairTime)}");
        builder.AppendLine($"Last repair result: {snapshot.State.LastRepairResult ?? "n/a"}");
        builder.AppendLine($"Last repair message: {snapshot.State.LastRepairMessage ?? "n/a"}");
        builder.AppendLine($"State file: {RuntimePaths.StateFilePath}");
        builder.AppendLine($"Logs folder: {RuntimePaths.LogsDirectory}");
        builder.AppendLine($"Reports folder: {RuntimePaths.ReportsDirectory}");
        return builder.ToString();
    }

    public string BuildJsonReport(DiagnosticsSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, JsonSerializerOptions);
    }

    public (string TextPath, string JsonPath) ExportReports(DiagnosticsSnapshot snapshot)
    {
        RuntimePaths.EnsureRuntimeDirectories();

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var textPath = Path.Combine(RuntimePaths.ReportsDirectory, $"diagnostics-{timestamp}.txt");
        var jsonPath = Path.Combine(RuntimePaths.ReportsDirectory, $"diagnostics-{timestamp}.json");

        File.WriteAllText(textPath, BuildTextReport(snapshot));
        File.WriteAllText(jsonPath, BuildJsonReport(snapshot));

        return (textPath, jsonPath);
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToLocalTime().ToString("O")
            : "n/a";
    }
}
