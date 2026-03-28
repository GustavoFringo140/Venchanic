namespace Venchanic.Core;

public sealed class DiscordCloseResult
{
    public bool Success { get; init; }

    public bool AnyProcessFound { get; init; }

    public bool ForcedKillUsed { get; init; }

    public int ClosedProcessCount { get; init; }

    public string Message { get; init; } = string.Empty;
}
