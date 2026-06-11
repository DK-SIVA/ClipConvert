using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace ClipConvert.Services;

/// <summary>
/// Information about an available update from a GitHub release.
/// </summary>
public class UpdateInfo
{
    public required Version Version { get; init; }
    public required string SetupDownloadUrl { get; init; }
    public required string SetupFileName { get; init; }
}

/// <summary>
/// Checks GitHub Releases for a newer version and downloads/launches the setup installer.
/// </summary>
public class UpdateService
{
    private const string LatestReleaseApiUrl =
        "https://api.github.com/repos/DK-SIVA/ClipConvert/releases/latest";

    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ClipConvert-Updater");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public static Version CurrentVersion =>
        Normalize(Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0));

    /// <summary>
    /// Queries the latest GitHub release. Returns update info if a newer version
    /// with a setup installer asset exists, otherwise null.
    /// </summary>
    public async Task<UpdateInfo?> CheckForUpdateAsync()
    {
        using var response = await Http.GetAsync(LatestReleaseApiUrl);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        string? tag = root.GetProperty("tag_name").GetString();
        var latest = ParseVersionTag(tag);
        if (latest == null || latest <= CurrentVersion)
            return null;

        if (!root.TryGetProperty("assets", out var assets))
            return null;

        foreach (var asset in assets.EnumerateArray())
        {
            string? name = asset.GetProperty("name").GetString();
            string? url = asset.GetProperty("browser_download_url").GetString();

            if (name != null && url != null &&
                name.StartsWith("ClipConvert_Setup", StringComparison.OrdinalIgnoreCase) &&
                name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateInfo
                {
                    Version = latest,
                    SetupDownloadUrl = url,
                    SetupFileName = name
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Downloads the setup installer to %TEMP% and launches it silently.
    /// The caller must shut down the application afterwards so the installer
    /// can replace the running executable.
    /// </summary>
    public async Task DownloadAndInstallAsync(UpdateInfo update)
    {
        string setupPath = Path.Combine(Path.GetTempPath(), update.SetupFileName);

        await using (var download = await Http.GetStreamAsync(update.SetupDownloadUrl))
        await using (var file = File.Create(setupPath))
        {
            await download.CopyToAsync(file);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = setupPath,
            Arguments = "/SP- /SILENT /NORESTART",
            UseShellExecute = true
        });
    }

    private static Version? ParseVersionTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        tag = tag.TrimStart('v', 'V');
        return Version.TryParse(tag, out var version) ? Normalize(version) : null;
    }

    // Normalize to Major.Minor.Build so "v1.1" and assembly version "1.1.0.0" compare correctly.
    private static Version Normalize(Version v) =>
        new(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0));
}
