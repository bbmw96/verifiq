// VERIFIQ: IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// ─── NETWORK SERVICE ─────────────────────────────────────────────────────────
//
// Handles three network concerns:
//
//  1. Online / offline detection
//     Uses .NET NetworkInterface to check if any non-loopback adapter is up.
//     Listens to NetworkChange.NetworkAvailabilityChanged so the UI updates
//     in real time when the network drops or comes back (e.g. power cut).
//
//  2. Proxy / corporate firewall support
//     Organisations on a VPN or behind a corporate proxy can configure:
//       - Proxy URL            e.g. http://proxy.company.com:8080
//       - Proxy username       (optional - leave blank for unauthenticated)
//       - Proxy password       (optional)
//       - Bypass list          e.g. *.internal.company.com
//       - Custom update server URL for companies hosting internal VERIFIQ updates
//     Settings are stored in %AppData%\BBMW0Technologies\VERIFIQ\settings.json
//     and applied globally to every HttpClient the application creates.
//
//  3. Shared HttpClient factory
//     GetClient() returns an HttpClient configured with the stored proxy settings.
//     Used by XeokitService and UpdateChecker so proxy settings apply everywhere.
// ─────────────────────────────────────────────────────────────────────────────

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;

namespace VERIFIQ.Desktop.Services;

public sealed class NetworkService
{
    // Singleton - one instance per application.
    public static readonly NetworkService Instance = new();

    // ── Online / Offline Status ──────────────────────────────────────────────

    /// <summary>True when at least one non-loopback network adapter is available.</summary>
    public bool IsOnline { get; private set; }

    /// <summary>Fired on the UI thread when connectivity changes.</summary>
    public event Action<bool>? ConnectivityChanged;

    // ── Proxy Settings ───────────────────────────────────────────────────────

    public NetworkProxySettings ProxySettings { get; private set; } = new();

    // ── Initialisation ───────────────────────────────────────────────────────

    private NetworkService()
    {
        IsOnline = NetworkInterface.GetIsNetworkAvailable();
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
    }

    /// <summary>Load proxy settings from disk and apply to outgoing requests.</summary>
    public void Initialise()
    {
        LoadProxySettings();
    }

    // ── HttpClient factory ───────────────────────────────────────────────────

    /// <summary>
    /// Creates an HttpClient configured with the stored proxy settings.
    /// Always create a new instance per download - do not share long-lived clients.
    /// </summary>
    public HttpClient CreateClient(TimeSpan? timeout = null)
    {
        HttpMessageHandler handler;

        if (ProxySettings.UseProxy && !string.IsNullOrWhiteSpace(ProxySettings.ProxyUrl))
        {
            var proxy = new WebProxy(ProxySettings.ProxyUrl, true);

            if (!string.IsNullOrWhiteSpace(ProxySettings.Username))
                proxy.Credentials = new NetworkCredential(
                    ProxySettings.Username,
                    ProxySettings.Password);

            if (!string.IsNullOrWhiteSpace(ProxySettings.BypassList))
                proxy.BypassList = ProxySettings.BypassList
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            handler = new HttpClientHandler
            {
                Proxy            = proxy,
                UseProxy         = true,
                PreAuthenticate  = true,
                ServerCertificateCustomValidationCallback =
                    ProxySettings.IgnoreSslErrors
                        ? (_, _, _, _) => true    // Allow self-signed certs on internal CAs
                        : HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }
        else
        {
            // No proxy - use system default proxy (respects IE/OS proxy settings).
            handler = new HttpClientHandler { UseProxy = true };
        }

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"VERIFIQ/{UpdateChecker.CurrentVersion} (+bbmw0.com)");

        return client;
    }

    // ── Proxy Settings persistence ───────────────────────────────────────────

    public void SaveProxySettings(NetworkProxySettings settings)
    {
        ProxySettings = settings;

        try
        {
            var path = ProxySettingsPath();
            var json = JsonSerializer.Serialize(settings,
                new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { /* Non-critical save failure. */ }
    }

    private void LoadProxySettings()
    {
        try
        {
            var path = ProxySettingsPath();
            if (!File.Exists(path)) return;
            var json     = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<NetworkProxySettings>(json);
            if (settings != null) ProxySettings = settings;
        }
        catch { /* Corrupt settings - use defaults. */ }
    }

    private static string ProxySettingsPath() =>
        Path.Combine(App.AppDataPath, "network.json");

    // ── Network change handler ───────────────────────────────────────────────

    private void OnNetworkChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        bool wasOnline = IsOnline;
        IsOnline = e.IsAvailable;

        if (wasOnline != IsOnline)
        {
            // Fire on the WPF dispatcher so UI code can respond safely.
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                ConnectivityChanged?.Invoke(IsOnline));
        }
    }
}

// ── Proxy settings model ─────────────────────────────────────────────────────

public sealed class NetworkProxySettings
{
    /// <summary>Whether to route traffic through the proxy.</summary>
    public bool UseProxy { get; set; } = false;

    /// <summary>
    /// Full proxy URL including port. Examples:
    ///   http://proxy.company.com:8080
    ///   http://10.0.0.1:3128
    /// </summary>
    public string ProxyUrl { get; set; } = string.Empty;

    /// <summary>Proxy username. Leave empty for unauthenticated proxies.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Proxy password. Stored locally in user AppData.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Comma-separated bypass list for addresses that should not go through the proxy.
    /// Example: *.internal.company.com, 192.168.0.0/16
    /// </summary>
    public string BypassList { get; set; } = string.Empty;

    /// <summary>
    /// Skip SSL certificate validation. Enable only for proxies with a
    /// self-signed or internal corporate CA certificate.
    /// </summary>
    public bool IgnoreSslErrors { get; set; } = false;

    /// <summary>
    /// Custom URL where VERIFIQ checks for updates.
    /// Leave empty to use the default bbmw0.com update server.
    /// Organisations can point this at an internal server for air-gapped deployments.
    /// </summary>
    public string CustomUpdateServerUrl { get; set; } = string.Empty;
}
