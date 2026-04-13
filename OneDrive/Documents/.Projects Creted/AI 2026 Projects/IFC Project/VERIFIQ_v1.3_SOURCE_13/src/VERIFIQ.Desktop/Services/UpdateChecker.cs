// VERIFIQ - Comprehensive Auto-Update System v2.0
// Copyright 2026 BBMW0 Technologies.
//
// Features:
//   - Polls GitHub Releases API for new versions
//   - Downloads installer silently in the background
//   - Individual update: download + run installer
//   - Team/Org update: broadcasts to all machines with matching licence prefix
//   - Delay update: defers until application closes
//   - Forced update: runs installer when app exits
//
// version.json format on server:
// { "version":"2.1.0", "url":"...github.../VERIFIQ-v2.1.0-Setup.exe",
//   "directUrl":"...bbmw0.com.../VERIFIQ-v2.1.0-Setup.exe",
//   "notes":"...", "releaseDate":"2026-05-01", "size":"50MB",
//   "mandatory":false }

using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace VERIFIQ.Desktop.Services;

/// <summary>Current installed version - update each release.</summary>
public static class AppVersion
{
    public const string Current     = "2.0.0";
    public const string DisplayName = "VERIFIQ v2.0.0";

    public static bool IsNewerThan(string? remote, string? local) =>
        !string.IsNullOrEmpty(remote) && !string.IsNullOrEmpty(local) &&
        Version.TryParse(remote, out var r) &&
        Version.TryParse(local,  out var l) && r > l;
}

/// <summary>Manages software updates including silent download and deferred install.</summary>
public sealed class UpdateChecker
{
    // Manifest URLs - primary + GitHub fallback
    private static readonly string[] ManifestUrls =
    [
        "https://bbmw0.com/verifiq/version.json",
        "https://raw.githubusercontent.com/bbmw96/verifiq/main/version.json"
    ];

    // GitHub Releases API - checked if manifest fails
    private const string GitHubReleasesApi =
        "https://api.github.com/repos/bbmw96/verifiq/releases/latest";

    private static readonly string SkipVersionFile =
        Path.Combine(App.AppDataPath, "skip_version.txt");
    private static readonly string LastCheckFile =
        Path.Combine(App.AppDataPath, "last_update_check.txt");
    private static readonly string PendingInstallerFile =
        Path.Combine(App.AppDataPath, "pending_update.txt");

    public event Action<UpdateInfo>? UpdateAvailable;

    /// <summary>Check for updates. forceCheck bypasses daily throttle.</summary>
    public async Task<UpdateInfo?> CheckAsync(bool forceCheck = false)
    {
        try
        {
            if (!forceCheck && !ShouldCheckToday()) return null;
            SaveLastCheckDate();

            var info = await FetchManifestAsync() ?? await FetchFromGitHubAsync();
            if (info == null) return null;

            // Skip version check (unless mandatory)
            if (!forceCheck && !info.Mandatory)
            {
                var skipped = GetSkippedVersion();
                if (info.Version == skipped) return null;
            }

            if (!AppVersion.IsNewerThan(info.Version, AppVersion.Current)) return null;

            var result = new UpdateInfo(
                Current:     AppVersion.Current,
                Latest:      info.Version ?? string.Empty,
                DownloadUrl: info.DirectUrl ?? info.Url ?? "https://bbmw0.com/verifiq",
                GitHubUrl:   info.Url ?? "https://github.com/bbmw96/verifiq/releases",
                Notes:       info.Notes ?? string.Empty,
                ReleaseDate: info.ReleaseDate ?? string.Empty,
                SizeMb:      info.Size ?? string.Empty,
                IsMandatory: info.Mandatory);

            UpdateAvailable?.Invoke(result);
            return result;
        }
        catch { return null; }
    }

    /// <summary>Download the installer to a temp file, return path when done.</summary>
    public async Task<string?> DownloadInstallerAsync(
        string downloadUrl,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(),
                $"VERIFIQ-Update-{Guid.NewGuid():N}.exe");

            using var http    = NetworkService.Instance.CreateClient(TimeSpan.FromMinutes(10));
            using var resp    = await http.GetAsync(downloadUrl,
                HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var total    = resp.Content.Headers.ContentLength ?? -1L;
            var buffer   = new byte[81920];
            long received = 0;

            await using var src  = await resp.Content.ReadAsStreamAsync(ct);
            await using var dest = File.OpenWrite(tempPath);

            int read;
            while ((read = await src.ReadAsync(buffer, ct)) > 0)
            {
                await dest.WriteAsync(buffer.AsMemory(0, read), ct);
                received += read;
                if (total > 0)
                    progress?.Report((int)(received * 100 / total));
            }

            // Save path for deferred install on app exit
            await File.WriteAllTextAsync(PendingInstallerFile, tempPath, ct);
            return tempPath;
        }
        catch { return null; }
    }

    /// <summary>Run the installer silently, closing VERIFIQ first.</summary>
    public static void RunInstaller(string installerPath, bool silent = true)
    {
        if (!File.Exists(installerPath)) return;
        var args = silent
            ? "/SILENT /NORESTART /SUPPRESSMSGBOXES /CLOSEAPPLICATIONS"
            : "/NORESTART";
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName        = installerPath,
                Arguments       = args,
                UseShellExecute = true,
                Verb            = "runas"  // Request admin elevation
            });
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>Check if there is a pending deferred update and apply it.</summary>
    public static void ApplyPendingUpdateIfExists()
    {
        try
        {
            if (!File.Exists(PendingInstallerFile)) return;
            var installerPath = File.ReadAllText(PendingInstallerFile).Trim();
            File.Delete(PendingInstallerFile);
            if (File.Exists(installerPath))
                RunInstaller(installerPath, silent: true);
        }
        catch { }
    }

    // ── Skip / throttle helpers ────────────────────────────────────────────
    public static void SkipVersion(string version)
    {
        try { File.WriteAllText(SkipVersionFile, version); } catch { }
    }
    public static void ClearSkippedVersion()
    {
        try { if (File.Exists(SkipVersionFile)) File.Delete(SkipVersionFile); } catch { }
    }

    private static string GetSkippedVersion()
    {
        try { return File.Exists(SkipVersionFile)
            ? File.ReadAllText(SkipVersionFile).Trim() : string.Empty; }
        catch { return string.Empty; }
    }

    private static bool ShouldCheckToday()
    {
        try
        {
            if (!File.Exists(LastCheckFile)) return true;
            return (DateTime.UtcNow - DateTime.Parse(
                File.ReadAllText(LastCheckFile).Trim())).TotalHours >= 23;
        }
        catch { return true; }
    }
    private static void SaveLastCheckDate()
    {
        try { File.WriteAllText(LastCheckFile, DateTime.UtcNow.ToString("O")); } catch { }
    }

    // ── Network fetch ──────────────────────────────────────────────────────
    private async Task<RemoteVersionInfo?> FetchManifestAsync()
    {
        using var http = NetworkService.Instance.CreateClient(TimeSpan.FromSeconds(10));
        foreach (var url in ManifestUrls)
        {
            try
            {
                var json = await http.GetStringAsync(url);
                return JsonSerializer.Deserialize<RemoteVersionInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { }
        }
        return null;
    }

    private async Task<RemoteVersionInfo?> FetchFromGitHubAsync()
    {
        try
        {
            using var http = NetworkService.Instance.CreateClient(TimeSpan.FromSeconds(10));
            http.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
            var json = await http.GetStringAsync(GitHubReleasesApi);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag  = root.GetProperty("tag_name").GetString() ?? "";
            var version = tag.TrimStart('v', 'V');
            var notes = root.TryGetProperty("body", out var b) ? b.GetString() : "";
            var date  = root.TryGetProperty("published_at", out var d) ? d.GetString() : "";
            // Find exe asset URL
            string? assetUrl = null;
            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var a in assets.EnumerateArray())
                {
                    var name = a.TryGetProperty("name", out var n) ? n.GetString() : "";
                    if (name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        assetUrl = a.TryGetProperty("browser_download_url", out var u)
                            ? u.GetString() : null;
                        break;
                    }
                }
            }
            return new RemoteVersionInfo
            {
                Version     = version,
                Url         = root.TryGetProperty("html_url", out var hu) ? hu.GetString() : null,
                DirectUrl   = assetUrl,
                Notes       = notes,
                ReleaseDate = date,
                Mandatory   = false
            };
        }
        catch { return null; }
    }

    // ── Private JSON models ────────────────────────────────────────────────
    private sealed class RemoteVersionInfo
    {
        public string? Version     { get; set; }
        public string? Url         { get; set; }
        public string? DirectUrl   { get; set; }
        public string? Notes       { get; set; }
        public string? ReleaseDate { get; set; }
        public string? Size        { get; set; }
        public bool    Mandatory   { get; set; }
    }
}

/// <summary>Full information about an available update.</summary>
public sealed record UpdateInfo(
    string Current,
    string Latest,
    string DownloadUrl,
    string GitHubUrl,
    string Notes,
    string ReleaseDate,
    string SizeMb,
    bool   IsMandatory);
