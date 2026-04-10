// VERIFIQ: IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// xeokit offline bundle download service.

using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
// UseWindowsForms=true brings System.Windows.Forms into scope — disambiguate WPF types explicitly.
using MessageBox         = System.Windows.MessageBox;
using MessageBoxButton   = System.Windows.MessageBoxButton;
using MessageBoxImage    = System.Windows.MessageBoxImage;
using MessageBoxResult   = System.Windows.MessageBoxResult;

namespace VERIFIQ.Desktop.Services;

/// <summary>
/// Manages the offline xeokit WebGL IFC rendering library.
/// On first launch, offers to download it so the 3D Viewer works without internet.
/// Important for Singapore and Malaysia sites where power cuts can interrupt connectivity.
/// </summary>
public sealed class XeokitService
{
    private const string DownloadUrl    = "https://cdn.jsdelivr.net/npm/@xeokit/xeokit-sdk@2.6.0/dist/xeokit-sdk.es.js";
    private const string BundleVersion  = "2.6.0";
    private const long   ApproxBytes    = 3_400_000;

    public static string VendorDir  => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "js", "vendor", "xeokit");
    public static string BundlePath => Path.Combine(VendorDir, "xeokit-sdk.es.js");
    public static bool   IsDownloaded => File.Exists(BundlePath) && new FileInfo(BundlePath).Length > 1_000_000;

    private static bool _prompted = false;

    public static void CheckAndOfferDownload(Window owner)
    {
        if (IsDownloaded || _prompted) return;
        _prompted = true;
        _ = Task.Run(() => owner.Dispatcher.Invoke(() => PromptAndDownload(owner)));
    }

    /// <summary>
    /// Manually offer the xeokit download from the Settings or 3D Viewer pages.
    /// Unlike CheckAndOfferDownload, this bypasses the one-time prompt guard so
    /// the user can retry after a failed download or after declining initially.
    /// </summary>
    public static void OfferDownload(Window owner)
    {
        _ = Task.Run(() => owner.Dispatcher.Invoke(() => PromptAndDownload(owner)));
    }

    private static void PromptAndDownload(Window owner)
    {
        var result = MessageBox.Show(
            $"VERIFIQ 3D Viewer — Offline Library\n\n" +
            $"The xeokit WebGL library (v{BundleVersion}, ~{ApproxBytes / 1_000_000.0:F1} MB) is not installed.\n" +
            $"Without it the 3D Viewer uses a basic canvas renderer.\n\n" +
            $"Download it now for full offline 3D IFC viewing?\n" +
            $"  Source:  cdn.jsdelivr.net (jsDelivr CDN)\n" +
            $"  Saved to: {VendorDir}\n\n" +
            $"After download the 3D Viewer works even when the network is down.\n" +
            $"This prompt will not appear again.",
            "VERIFIQ: Download 3D Viewer Library",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
            _ = DownloadAsync(owner);
    }

    private static async Task DownloadAsync(Window owner)
    {
        var prog = new XeokitProgressWindow(owner);
        prog.Show();
        var cts = new CancellationTokenSource();
        prog.CancelRequested += () => cts.Cancel();

        try
        {
            Directory.CreateDirectory(VendorDir);
            var tmp = BundlePath + ".tmp";

            using var http = NetworkService.Instance.CreateClient(TimeSpan.FromMinutes(10));
            // UserAgent is set by NetworkService.CreateClient

            using var resp = await http.GetAsync(DownloadUrl,
                HttpCompletionOption.ResponseHeadersRead, cts.Token);
            resp.EnsureSuccessStatusCode();

            var total    = resp.Content.Headers.ContentLength ?? ApproxBytes;
            var buf      = new byte[81_920];
            long done    = 0;

            await using var src  = await resp.Content.ReadAsStreamAsync(cts.Token);
            await using var dst  = File.Create(tmp);

            int n;
            while ((n = await src.ReadAsync(buf, 0, buf.Length, cts.Token)) > 0)
            {
                await dst.WriteAsync(buf, 0, n, cts.Token);
                done += n;
                int pct = (int)(done * 100 / total);
                prog.Dispatcher.Invoke(() => prog.Set(pct,
                    $"Downloading... {done / 1_048_576.0:F1} MB / {total / 1_048_576.0:F1} MB"));
            }
            dst.Close();

            if (File.Exists(BundlePath)) File.Delete(BundlePath);
            File.Move(tmp, BundlePath);

            prog.Dispatcher.Invoke(() =>
            {
                prog.Close();
                MessageBox.Show(
                    "xeokit downloaded successfully.\n\nNavigate to the 3D Viewer page — it is now fully offline-capable.",
                    "VERIFIQ: 3D Viewer Ready",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
        catch (OperationCanceledException)
        {
            prog.Dispatcher.Invoke(() => prog.Close());
            if (File.Exists(BundlePath + ".tmp")) File.Delete(BundlePath + ".tmp");
        }
        catch (Exception ex)
        {
            prog.Dispatcher.Invoke(() =>
            {
                prog.Close();
                MessageBox.Show(
                    $"Download failed: {ex.Message}\n\nThe 3D Viewer will use the basic canvas renderer.\nTry again from the 3D Viewer page when a stable connection is available.",
                    "VERIFIQ: Download Failed",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            });
        }
    }
}

// ── PROGRESS WINDOW ──────────────────────────────────────────────────────────
internal sealed class XeokitProgressWindow : Window
{
    private readonly System.Windows.Controls.TextBlock _txt;
    private readonly System.Windows.Controls.ProgressBar _bar;
    public event Action? CancelRequested;

    internal XeokitProgressWindow(Window owner)
    {
        Owner = owner; Title = "VERIFIQ: Downloading xeokit";
        Width = 460; Height = 160; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = System.Windows.Media.Brushes.White;
        Icon = VqIcon.Get();

        var sp = new System.Windows.Controls.StackPanel { Margin = new Thickness(24) };
        sp.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Downloading xeokit WebGL library for offline 3D IFC viewing...",
            FontSize = 13, FontFamily = new System.Windows.Media.FontFamily("Arial"),
            Margin = new Thickness(0, 0, 0, 12),
            Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(11, 31, 69))
        });
        _bar = new System.Windows.Controls.ProgressBar
        {
            Height = 8, Minimum = 0, Maximum = 100, Margin = new Thickness(0, 0, 0, 8),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 124, 134)),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240))
        };
        sp.Children.Add(_bar);
        _txt = new System.Windows.Controls.TextBlock
        {
            Text = "Connecting...", FontSize = 11, FontFamily = new System.Windows.Media.FontFamily("Arial"),
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139))
        };
        sp.Children.Add(_txt);

        var btn = new System.Windows.Controls.Button
        {
            Content = "Cancel", Width = 80, Padding = new Thickness(0, 6, 0, 6),
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Background = System.Windows.Media.Brushes.Transparent, FontSize = 12,
            FontFamily = new System.Windows.Media.FontFamily("Arial"),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btn.Click += (_, _) => CancelRequested?.Invoke();
        sp.Children.Add(btn);
        Content = sp;
    }

    internal void Set(int pct, string status) { _bar.Value = pct; _txt.Text = status; }
}
