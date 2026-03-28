namespace Venchanic.Core;

public sealed class DiscordLocator
{
    private static readonly (string FolderName, string Branch)[] DiscordInstalls =
    [
        ("Discord", "stable"),
        ("DiscordPTB", "ptb"),
        ("DiscordCanary", "canary")
    ];

    public DiscordInstallInfo? FindDiscord()
    {
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localAppDataPath))
        {
            return null;
        }

        foreach (var discordInstall in DiscordInstalls)
        {
            var discordPath = Path.Combine(localAppDataPath, discordInstall.FolderName);
            if (!Directory.Exists(discordPath))
            {
                continue;
            }

            var appFolderPath = GetLatestAppFolder(discordPath);
            if (appFolderPath is null)
            {
                continue;
            }

            var discordVersion = GetDiscordVersion(appFolderPath);
            var resourcesPath = Path.Combine(appFolderPath, "resources");

            return new DiscordInstallInfo
            {
                Branch = discordInstall.Branch,
                DiscordPath = discordPath,
                AppFolderPath = appFolderPath,
                DiscordVersion = discordVersion,
                ResourcesPath = resourcesPath,
                AppAsarPath = Path.Combine(resourcesPath, "app.asar"),
                VencordMarkerPath = Path.Combine(resourcesPath, "_app.asar")
            };
        }

        return null;
    }

    private static string? GetLatestAppFolder(string discordPath)
    {
        var appDirectories = Directory
            .GetDirectories(discordPath, "app-*", SearchOption.TopDirectoryOnly)
            .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return appDirectories.FirstOrDefault();
    }

    private static string GetDiscordVersion(string appFolderPath)
    {
        var folderName = Path.GetFileName(appFolderPath);
        const string prefix = "app-";

        if (folderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return folderName[prefix.Length..];
        }

        return "Not detected";
    }
}
