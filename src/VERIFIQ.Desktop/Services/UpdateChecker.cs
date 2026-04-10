// VERIFIQ: IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Software update checker — silent background check, non-blocking notification.

using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace VERIFIQ.Desktop.Services;

/// <summary>
/// Checks for VERIFIQ updates in the background. If a newer version is found,
/// a non-intrusive notification is shown. The check respects offline conditions
/// by using a short timeout and never blocking the UI thread.
/// The update manifest is a simple JSON file served from bbmw0.com.
/// </summary>
public sealed class UpdateChecker
{
    // Version file format: { "version":"1.1.0", "url":"https://bbmw0.com/verifiq/download", "notes":"..." }
    private static string ManifestUrl =>
        !string.IsNullOrWhiteSpace(NetworkService.Instance.ProxySettings.CustomUpdateServerUrl)
            ? NetworkService.Instance.ProxySettings.CustomUpdateServerUrl
            : "https://bbmw0.com/verifiq/version.json";

    // The current installed version — matches AssemblyVersion in the csproj.
    public const string CurrentVersion = "1.0.0";

    // Callback fired on the calling thread when an update is found.
    public event Action<UpdateInfo>? UpdateAvailable;

    /// <summary>
    /// Run the update check on a background thread. Returns within 8 seconds
    /// regardless of network conditions. Never throws or blocks the UI.
    /// </summary>
    public async Task CheckAsync()
    {
        try
        {
            using var http = NetworkService.Instance.CreateClient(TimeSpan.FromSeconds(8));
            // UserAgent is set by NetworkService.CreateClient — no need to set it here.

            var json = await http.GetStringAsync(ManifestUrl);
            var info = JsonSerializer.Deserialize<RemoteVersionInfo>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (info == null || string.IsNullOrWhiteSpace(info.Version)) return;

            if (IsNewer(info.Version, CurrentVersion))
            {
                UpdateAvailable?.Invoke(new UpdateInfo(
                    Current:  CurrentVersion,
                    Latest:   info.Version,
                    Url:      info.Url ?? "https://bbmw0.com",
                    Notes:    info.Notes ?? string.Empty));
            }
        }
        catch
        {
            // Network unavailable, server unreachable, JSON malformed — silently ignore.
            // The update check is a background enhancement and must never disrupt work.
        }
    }

    private static bool IsNewer(string remote, string local)
    {
        return Version.TryParse(remote, out var r)
            && Version.TryParse(local,  out var l)
            && r > l;
    }

    private sealed class RemoteVersionInfo
    {
        public string? Version { get; set; }
        public string? Url     { get; set; }
        public string? Notes   { get; set; }
    }
}

public sealed record UpdateInfo(
    string Current,
    string Latest,
    string Url,
    string Notes);
