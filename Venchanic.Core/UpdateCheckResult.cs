namespace Venchanic.Core;

public sealed class UpdateCheckResult
{
    public bool Success { get; init; }

    public bool UpdateAvailable { get; init; }

    public string CurrentVersion { get; init; } = string.Empty;

    public string LatestVersion { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string ReleaseUrl { get; init; } = "https://github.com/GustavoFringo140/Venchanic/releases/latest";
}
