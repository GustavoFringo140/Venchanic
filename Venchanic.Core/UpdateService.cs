using System.Net.Http.Headers;
using System.Text.Json;

namespace Venchanic.Core;

public sealed class UpdateService
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/GustavoFringo140/Venchanic/releases/latest";
    private const string ReleasePageUrl = "https://github.com/GustavoFringo140/Venchanic/releases/latest";
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion)
    {
        try
        {
            var json = await HttpClient.GetStringAsync(ReleasesApiUrl);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var tagName = root.TryGetProperty("tag_name", out var tagElement)
                ? tagElement.GetString() ?? string.Empty
                : string.Empty;
            var htmlUrl = root.TryGetProperty("html_url", out var htmlElement)
                ? htmlElement.GetString() ?? ReleasePageUrl
                : ReleasePageUrl;

            var normalizedLatest = NormalizeVersion(tagName);
            var normalizedCurrent = NormalizeVersion(currentVersion);
            var latestVersion = Version.TryParse(normalizedLatest, out var latest)
                ? latest
                : null;
            var current = Version.TryParse(normalizedCurrent, out var currentParsed)
                ? currentParsed
                : null;

            var updateAvailable = latest is not null &&
                current is not null &&
                latest > current;

            return new UpdateCheckResult
            {
                Success = true,
                UpdateAvailable = updateAvailable,
                CurrentVersion = currentVersion,
                LatestVersion = string.IsNullOrWhiteSpace(tagName) ? currentVersion : tagName,
                Message = updateAvailable
                    ? $"Update available: {currentVersion} -> {tagName}"
                    : "Venchanic is up to date.",
                ReleaseUrl = htmlUrl
            };
        }
        catch
        {
            return new UpdateCheckResult
            {
                Success = false,
                CurrentVersion = currentVersion,
                LatestVersion = currentVersion,
                Message = "Could not check for updates.",
                ReleaseUrl = ReleasePageUrl
            };
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Venchanic", "1.0"));
        return client;
    }

    private static string NormalizeVersion(string value)
    {
        return value.Trim().TrimStart('v', 'V');
    }
}
