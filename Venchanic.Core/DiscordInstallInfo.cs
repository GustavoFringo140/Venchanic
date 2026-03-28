namespace Venchanic.Core;

public sealed class DiscordInstallInfo
{
    public required string Branch { get; init; }

    public required string DiscordPath { get; init; }

    public required string AppFolderPath { get; init; }

    public required string DiscordVersion { get; init; }

    public required string ResourcesPath { get; init; }

    public required string AppAsarPath { get; init; }

    public required string VencordMarkerPath { get; init; }
}
