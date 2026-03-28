namespace Venchanic.Core;

public sealed class RepairOptions
{
    public bool ClearCacheBeforeRepair { get; init; }

    public RepairMode Mode { get; init; } = RepairMode.Patch;

    public bool TryCloseDiscordBeforeRepair { get; init; }

    public bool RetryAfterClosingDiscord { get; init; } = true;

    public bool UseFallbackMirror { get; init; } = true;
}
