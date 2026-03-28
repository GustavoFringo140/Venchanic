namespace Venchanic.Core;

public sealed class DiscordCacheService
{
    private static readonly string[] CacheDirectories =
    [
        "Cache",
        "Code Cache",
        "GPUCache",
        Path.Combine("Service Worker", "CacheStorage"),
        Path.Combine("Service Worker", "ScriptCache"),
        "DawnCache"
    ];

    public CacheCleanupResult ClearCaches(string? discordPath)
    {
        if (string.IsNullOrWhiteSpace(discordPath) || !Directory.Exists(discordPath))
        {
            return new CacheCleanupResult
            {
                Message = "Discord cache location was not found."
            };
        }

        var deletedCount = 0;
        var failedCount = 0;

        foreach (var relativePath in CacheDirectories)
        {
            var targetPath = Path.Combine(discordPath, relativePath);
            if (!Directory.Exists(targetPath))
            {
                continue;
            }

            try
            {
                Directory.Delete(targetPath, recursive: true);
                deletedCount++;
            }
            catch
            {
                failedCount++;
            }
        }

        return new CacheCleanupResult
        {
            DeletedDirectoryCount = deletedCount,
            FailedDirectoryCount = failedCount,
            Message = deletedCount == 0 && failedCount == 0
                ? "No Discord cache directories were found."
                : failedCount == 0
                    ? $"Cleared {deletedCount} Discord cache directories."
                    : $"Cleared {deletedCount} cache directories. {failedCount} directories could not be removed."
        };
    }
}
