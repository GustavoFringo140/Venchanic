namespace Venchanic.Core;

public sealed class CacheCleanupResult
{
    public int DeletedDirectoryCount { get; init; }

    public int FailedDirectoryCount { get; init; }

    public string Message { get; init; } = string.Empty;
}
