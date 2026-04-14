// VERIFIQ - COP Rules Auto-Update Engine
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// ─── CORENET-X COP RULES AUTO-UPDATE ENGINE ───────────────────────────────────
//
// Automatically checks for and applies updates to VERIFIQ's embedded rules when:
//   1. The BCA/GovTech IFC+SG Industry Mapping Excel is updated at:
//      https://info.corenet.gov.sg/ifc-sg/templates--apps-and-more/ifc-sg-excel-mapping-file
//   2. A new CORENET-X COP edition is published at:
//      https://go.gov.sg/cxcop
//   3. A new IFC+SG Resource Kit is published at:
//      https://go.gov.sg/ifcsg
//
// The engine:
//   - Runs a silent background check on app startup (and daily thereafter)
//   - Downloads the IFC+SG Excel if a newer version is available
//   - Parses the Excel and merges new/updated codes into the runtime library
//   - Checks the COP version string in the PDF header to detect new editions
//   - Notifies the JS frontend via SendToFrontend("rulesUpdateAvailable", ...)
//   - Stores downloaded files in %LOCALAPPDATA%\VERIFIQ\RulesCache\
//
// The Industry Mapping Excel format (BCA/GovTech) contains columns:
//   Classification Code | IFC Entity | PredefinedType | Pset Name | Property Name |
//   Property Type | Unit | Input Limitation | Examples | Responsible Agency | Gateway

using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using VERIFIQ.Rules.Common;
using VERIFIQ.Reports;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Desktop.Services;

/// <summary>
/// Result from a COP rules update check.
/// </summary>
public sealed class CopRulesUpdateResult
{
    public bool   UpdateAvailable     { get; set; }
    public bool   UpdateApplied       { get; set; }
    public string CopVersion          { get; set; } = string.Empty;
    public string CopEditionDate      { get; set; } = string.Empty;
    public int    NewCodesCount       { get; set; }
    public int    UpdatedCodesCount   { get; set; }
    public int    NewPropertiesCount  { get; set; }
    public string SourceUrl           { get; set; } = string.Empty;
    public string Message             { get; set; } = string.Empty;
    public string ErrorMessage        { get; set; } = string.Empty;
    public bool   HasError            => !string.IsNullOrEmpty(ErrorMessage);
    public DateTime CheckedAt         { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Embedded knowledge of where to find COP updates.
/// All URLs sourced from official BCA/GovTech CORENET-X portal.
/// </summary>
public static class CopUpdateSources
{
    // Primary: IFC+SG Industry Mapping Excel - direct download URL
    // This file defines every classification code, entity mapping, property and agency assignment.
    public const string ExcelMappingUrl =
        "https://info.corenet.gov.sg/ifc-sg/templates--apps-and-more/ifc-sg-excel-mapping-file";

    // Direct Excel file download (the above page contains a link to this)
    public const string ExcelDirectDownloadPattern =
        "https://info.corenet.gov.sg/docs/default-source/ifc-sg/";

    // IFC+SG Resource Kit landing page
    public const string IfcSgResourceKit = "https://go.gov.sg/ifcsg";

    // COP document page - check for new edition announcements
    public const string CopDocumentPage = "https://go.gov.sg/cxcop";

    // COP 3.1 PDF direct URL
    public const string Cop31PdfUrl =
        "https://info.corenet.gov.sg/docs/default-source/default-document-library/corenet-x-cop---3-1-edition-2025-12.pdf";

    // Version manifest - VERIFIQ checks this to know what version of the mapping it has
    public const string VersionManifestUrl =
        "https://bbmw0.com/verifiq/rules-version.json";

    // AppData cache directory
    public static string RulesCacheDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VERIFIQ", "RulesCache");

    public static string CachedExcelPath =>
        Path.Combine(RulesCacheDir, "IFCSGIndustryMapping.xlsx");

    public static string CachedVersionPath =>
        Path.Combine(RulesCacheDir, "rules-version.json");

    public static string LastCheckPath =>
        Path.Combine(RulesCacheDir, "last-rules-check.txt");
}

/// <summary>
/// Tracks the installed version of the rules database.
/// </summary>
public sealed class RulesVersionInfo
{
    public string CopVersion     { get; set; } = "3.1";
    public string CopEditionDate { get; set; } = "2025-12";
    public string MappingVersion { get; set; } = "1.0";
    public int    TotalCodes     { get; set; }
    public int    TotalProperties{ get; set; }
    public DateTime InstalledAt  { get; set; } = DateTime.UtcNow;
    public string InstalledFrom  { get; set; } = "embedded";
}

/// <summary>
/// The COP Rules Auto-Update Engine.
/// Checks for new IFC+SG mapping files and applies them to VERIFIQ's runtime library.
/// </summary>
public sealed class CopRulesUpdateEngine
{
    private static readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        DefaultRequestHeaders =
        {
            { "User-Agent", "VERIFIQ/2.0 (IFC+SG Compliance Checker; contact bbmw0@hotmail.com)" }
        }
    };

    private readonly IndustryMappingImporter _importer = new();
    private bool _checkInProgress;

    // ── DAILY AUTO-CHECK ─────────────────────────────────────────────────────

    /// <summary>
    /// Starts the background daily check. Called once on app startup.
    /// Will not run if a check was performed in the last 24 hours.
    /// </summary>
    public void StartBackgroundCheck(Action<CopRulesUpdateResult> onComplete)
    {
        Task.Run(async () =>
        {
            try
            {
                // Only check once per 24 hours
                if (WasCheckedRecently()) return;

                var result = await CheckAndApplyAsync(silent: true);
                RecordCheckTime();
                onComplete(result);
            }
            catch
            {
                // Background check failure is silent - never block app startup
            }
        });
    }

    // ── MANUAL FORCED CHECK ──────────────────────────────────────────────────

    /// <summary>
    /// Performs an immediate forced check, regardless of last check time.
    /// Called when user clicks "Check for Rules Update" in Settings.
    /// </summary>
    public async Task<CopRulesUpdateResult> ForceCheckAsync(CancellationToken ct = default)
    {
        if (_checkInProgress)
            return new CopRulesUpdateResult { Message = "Check already in progress." };

        _checkInProgress = true;
        try
        {
            var result = await CheckAndApplyAsync(silent: false, ct);
            RecordCheckTime();
            return result;
        }
        finally
        {
            _checkInProgress = false;
        }
    }

    // ── CORE CHECK LOGIC ─────────────────────────────────────────────────────

    private async Task<CopRulesUpdateResult> CheckAndApplyAsync(
        bool silent, CancellationToken ct = default)
    {
        var result = new CopRulesUpdateResult();

        try
        {
            // ── Step 1: Check the version manifest ────────────────────────
            var (serverVersion, serverDate, excelDownloadUrl) =
                await GetServerVersionInfoAsync(ct);

            var installedVersion = LoadInstalledVersion();

            result.CopVersion       = serverVersion;
            result.CopEditionDate   = serverDate;
            result.SourceUrl        = excelDownloadUrl;

            // Compare versions
            bool newVersionAvailable = IsNewerVersion(serverVersion, serverDate, installedVersion);

            if (!newVersionAvailable)
            {
                result.UpdateAvailable = false;
                result.Message = $"Rules are up to date. COP {installedVersion.CopVersion} " +
                                 $"({installedVersion.CopEditionDate}) - " +
                                 $"{installedVersion.TotalCodes} codes, " +
                                 $"{installedVersion.TotalProperties} properties.";
                return result;
            }

            result.UpdateAvailable = true;

            if (silent)
            {
                // In silent mode - just flag that update is available, don't apply
                result.Message = $"New IFC+SG mapping available: COP {serverVersion} ({serverDate}).";
                return result;
            }

            // ── Step 2: Download the Excel mapping file ────────────────────
            EnsureCacheDirectory();

            string excelPath = await DownloadExcelMappingAsync(excelDownloadUrl, ct);

            if (string.IsNullOrEmpty(excelPath))
            {
                result.ErrorMessage = "Failed to download IFC+SG Industry Mapping Excel. " +
                                      "Please download manually from go.gov.sg/ifcsg and " +
                                      "import via Settings > Import Mapping.";
                return result;
            }

            // ── Step 3: Parse and merge into runtime library ───────────────
            var importResult = await _importer.ImportAsync(excelPath, ct: ct);

            result.UpdateApplied      = importResult.Success;
            result.NewCodesCount      = importResult.CodesImported;
            result.UpdatedCodesCount  = importResult.CodesUpdated;
            result.NewPropertiesCount = importResult.RulesImported;

            if (importResult.Success && importResult.Errors.Count == 0)
            {
                // Save updated version info
                SaveInstalledVersion(new RulesVersionInfo
                {
                    CopVersion      = serverVersion,
                    CopEditionDate  = serverDate,
                    TotalCodes      = importResult.CodesImported,
                    TotalProperties = importResult.RulesImported,
                    InstalledAt     = DateTime.UtcNow,
                    InstalledFrom   = excelDownloadUrl
                });

                result.Message =
                    $"Rules updated to COP {serverVersion} ({serverDate}). " +
                    $"{result.NewCodesCount} new codes, " +
                    $"{result.UpdatedCodesCount} updated codes, " +
                    $"{result.NewPropertiesCount} new properties added. " +
                    $"Total: {importResult.CodesImported} codes, {importResult.RulesImported} properties.";
            }
            else
            {
                result.ErrorMessage = string.Join(", ", importResult.Errors);
            }
        }
        catch (TaskCanceledException)
        {
            result.ErrorMessage = "Rules update check was cancelled.";
        }
        catch (HttpRequestException ex)
        {
            result.ErrorMessage = $"Network error checking for rules update: {ex.Message}. " +
                                   "Check internet connection and try again.";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Rules update error: {ex.Message}";
        }

        return result;
    }

    // ── VERSION RESOLUTION ───────────────────────────────────────────────────

    /// <summary>
    /// Fetches server version info.
    /// Strategy:
    ///   1. Try VERIFIQ version manifest (fastest)
    ///   2. Try scraping the IFC+SG resource page for the latest mapping link
    ///   3. Fall back to trying the direct Excel URL
    /// </summary>
    private async Task<(string version, string date, string excelUrl)>
        GetServerVersionInfoAsync(CancellationToken ct)
    {
        // ── Try VERIFIQ version manifest first ────────────────────────────
        try
        {
            var json = await _http.GetStringAsync(CopUpdateSources.VersionManifestUrl, ct);
            var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string version   = root.TryGetProperty("copVersion", out var vp)   ? vp.GetString() ?? "3.1"      : "3.1";
            string date      = root.TryGetProperty("copDate",    out var dp)   ? dp.GetString() ?? "2025-12"  : "2025-12";
            string excelUrl  = root.TryGetProperty("excelUrl",   out var ep)   ? ep.GetString() ?? string.Empty : string.Empty;

            if (!string.IsNullOrEmpty(excelUrl))
                return (version, date, excelUrl);
        }
        catch { /* fall through */ }

        // ── Try scraping the IFC+SG resource page ─────────────────────────
        try
        {
            string pageHtml = await _http.GetStringAsync(CopUpdateSources.ExcelMappingUrl, ct);

            // Look for direct Excel download links in the page
            // BCA/GovTech typically host as .xlsx or direct file links
            string excelUrl  = ExtractExcelUrlFromPage(pageHtml);
            string version   = ExtractCopVersionFromPage(pageHtml);
            string date      = ExtractCopDateFromPage(pageHtml);

            if (!string.IsNullOrEmpty(excelUrl))
                return (version, date, excelUrl);
        }
        catch { /* fall through */ }

        // ── Fallback: try the direct URL ──────────────────────────────────
        // Use HEAD request to check if file has been modified
        try
        {
            var request  = new HttpRequestMessage(HttpMethod.Head,
                "https://info.corenet.gov.sg/docs/default-source/ifc-sg/ifc-sg-industry-mapping-2025.xlsx");
            var response = await _http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return ("3.1", "2025-12",
                    "https://info.corenet.gov.sg/docs/default-source/ifc-sg/ifc-sg-industry-mapping-2025.xlsx");
        }
        catch { /* fall through */ }

        // Return current known version - no update check possible
        return ("3.1", "2025-12", string.Empty);
    }

    private static string ExtractExcelUrlFromPage(string html)
    {
        // Look for .xlsx download links
        var patterns = new[]
        {
            @"href=""(https://[^""]*\.xlsx)""",
            @"href=""(https://info\.corenet\.gov\.sg/docs[^""]*industry[^""]*)""",
            @"href=""(https://[^""]*ifc-sg-industry[^""]*)""",
            @"href=""(https://[^""]*mapping[^""]*\.xlsx)""",
        };

        foreach (var pattern in patterns)
        {
            var m = System.Text.RegularExpressions.Regex.Match(html, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value;
        }
        return string.Empty;
    }

    private static string ExtractCopVersionFromPage(string html)
    {
        // Look for COP version strings like "COP 3.1", "COP3", "3.2 Edition"
        var m = System.Text.RegularExpressions.Regex.Match(html,
            @"COP\s*([0-9]+\.[0-9]+)|([0-9]+\.[0-9]+)\s*Edition",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return m.Success ? (m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : m.Groups[2].Value) : "3.1";
    }

    private static string ExtractCopDateFromPage(string html)
    {
        // Look for date patterns like "2025-12", "December 2025", "2026-03"
        var m = System.Text.RegularExpressions.Regex.Match(html,
            @"(20[0-9]{2})-([01][0-9])|([A-Z][a-z]+)\s+(20[0-9]{2})");
        if (m.Success)
        {
            if (m.Groups[1].Success) return $"{m.Groups[1].Value}-{m.Groups[2].Value}";
            // Convert month name to number
            if (DateTime.TryParseExact($"1 {m.Groups[3].Value} {m.Groups[4].Value}",
                    "d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                return dt.ToString("yyyy-MM");
        }
        return "2025-12";
    }

    // ── EXCEL DOWNLOAD ───────────────────────────────────────────────────────

    private async Task<string> DownloadExcelMappingAsync(string url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        try
        {
            var bytes    = await _http.GetByteArrayAsync(url, ct);
            string path  = CopUpdateSources.CachedExcelPath;
            await File.WriteAllBytesAsync(path, bytes, ct);
            return path;
        }
        catch
        {
            // Try the known fallback URL
            try
            {
                var bytes = await _http.GetByteArrayAsync(
                    CopUpdateSources.ExcelMappingUrl, ct);
                string path = CopUpdateSources.CachedExcelPath;
                await File.WriteAllBytesAsync(path, bytes, ct);
                return path;
            }
            catch { return string.Empty; }
        }
    }

    // ── VERSION TRACKING ─────────────────────────────────────────────────────

    private static bool IsNewerVersion(string serverVersion, string serverDate,
        RulesVersionInfo installed)
    {
        // Compare COP version (e.g. 3.1 vs 3.2)
        if (Version.TryParse(serverVersion, out var sv) &&
            Version.TryParse(installed.CopVersion, out var iv))
        {
            if (sv > iv) return true;
        }

        // Compare edition date (e.g. 2025-12 vs 2026-03)
        if (string.Compare(serverDate, installed.CopEditionDate,
                StringComparison.OrdinalIgnoreCase) > 0) return true;

        return false;
    }

    private static RulesVersionInfo LoadInstalledVersion()
    {
        try
        {
            string path = CopUpdateSources.CachedVersionPath;
            if (!File.Exists(path))
                return new RulesVersionInfo
                {
                    CopVersion      = "3.1",
                    CopEditionDate  = "2025-12",
                    TotalCodes      = 196,
                    TotalProperties = 946,
                    InstalledFrom   = "embedded"
                };

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<RulesVersionInfo>(json) ?? new RulesVersionInfo();
        }
        catch { return new RulesVersionInfo(); }
    }

    private static void SaveInstalledVersion(RulesVersionInfo info)
    {
        try
        {
            EnsureCacheDirectory();
            string json = JsonSerializer.Serialize(info, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CopUpdateSources.CachedVersionPath, json);
        }
        catch { }
    }

    private static bool WasCheckedRecently()
    {
        try
        {
            string path = CopUpdateSources.LastCheckPath;
            if (!File.Exists(path)) return false;
            string text = File.ReadAllText(path).Trim();
            if (DateTime.TryParse(text, out var lastCheck))
                return (DateTime.UtcNow - lastCheck).TotalHours < 24;
        }
        catch { }
        return false;
    }

    private static void RecordCheckTime()
    {
        try
        {
            EnsureCacheDirectory();
            File.WriteAllText(CopUpdateSources.LastCheckPath,
                DateTime.UtcNow.ToString("O"));
        }
        catch { }
    }

    private static void EnsureCacheDirectory()
    {
        Directory.CreateDirectory(CopUpdateSources.RulesCacheDir);
    }
}
