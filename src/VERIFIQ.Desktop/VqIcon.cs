// VERIFIQ: IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// ─── LOGO / ICON SYSTEM ───────────────────────────────────────────────────────
//
// VqIcon generates the VERIFIQ application icon programmatically at runtime
// using WPF drawing primitives. No external image file is required to run.
//
// HOW TO CHANGE THE LOGO IN FUTURE
// ─────────────────────────────────
// Option A — Replace the programmatic icon with an image file:
//   1. Create folder:  VERIFIQ.Desktop / Assets /
//   2. Add files:      icon-256.png  (256 x 256, shown in About page)
//                      icon-32.png   (32 x 32,   window title bar + taskbar)
//                      icon.ico      (multi-resolution .ico for the exe file)
//   3. In Visual Studio, right-click each file → Properties → Build Action = Resource
//   4. In VqIcon.cs, replace the Get() body with:
//        var uri = new Uri("pack://application:,,,/VERIFIQ.Desktop;component/Assets/icon-32.png");
//        var img = new System.Windows.Media.Imaging.BitmapImage(uri);
//        img.Freeze();
//        return img;
//   5. In VERIFIQ.Desktop.csproj add inside <PropertyGroup>:
//        <ApplicationIcon>Assets\icon.ico</ApplicationIcon>
//
// Option B — Edit the programmatic drawing below to change shape or colour.
//   The brand palette colours are defined as const strings near the top of Render().
//
// ─────────────────────────────────────────────────────────────────────────────

using System.Globalization;

// UseWindowsForms=true brings System.Drawing into scope which conflicts with
// System.Windows.Media and System.Windows types. All ambiguous names are
// resolved here with explicit aliases so the WPF versions are used throughout.
using WpfBrushes       = System.Windows.Media.Brushes;
using WpfBrush         = System.Windows.Media.Brush;
using WpfSolidBrush    = System.Windows.Media.SolidColorBrush;
using WpfColor         = System.Windows.Media.Color;
using WpfFontFamily    = System.Windows.Media.FontFamily;
using WpfFontStyles    = System.Windows.FontStyles;
using WpfFontWeights   = System.Windows.FontWeights;
using WpfFontStretches = System.Windows.FontStretches;
using WpfTypeface      = System.Windows.Media.Typeface;
using WpfFormattedText = System.Windows.Media.FormattedText;
using WpfFlowDirection = System.Windows.FlowDirection;
using WpfPoint         = System.Windows.Point;
using WpfRect          = System.Windows.Rect;
using WpfPixelFormats  = System.Windows.Media.PixelFormats;
using WpfDrawingVisual = System.Windows.Media.DrawingVisual;
using WpfBitmapSource  = System.Windows.Media.Imaging.BitmapSource;
using WpfRtb           = System.Windows.Media.Imaging.RenderTargetBitmap;

namespace VERIFIQ.Desktop;

/// <summary>
/// Generates and caches the VERIFIQ VQ icon as a WPF BitmapSource.
/// Applied as the Window.Icon property on every window in the application
/// so the teal VQ logo appears consistently in the title bar and taskbar.
/// </summary>
internal static class VqIcon
{
    // ── Cached icons at different sizes ───────────────────────────────────────
    private static WpfBitmapSource? _icon16;
    private static WpfBitmapSource? _icon32;
    private static WpfBitmapSource? _icon64;
    private static WpfBitmapSource? _icon256;

    // Primary — window title bars and taskbar (32 px)
    internal static WpfBitmapSource Get()     => _icon32  ??= Render(32);

    // Small — compact UI contexts (16 px)
    internal static WpfBitmapSource Get16()   => _icon16  ??= Render(16);

    // Medium — dialogs (64 px)
    internal static WpfBitmapSource Get64()   => _icon64  ??= Render(64);

    // Large — About page (256 px)
    internal static WpfBitmapSource Get256()  => _icon256 ??= Render(256);

    /// <summary>
    /// Renders the VQ logo to a <see cref="WpfBitmapSource"/> at the given pixel size.
    /// The logo is a teal rounded square with "VQ" in white bold Arial, centred.
    /// The result is frozen (immutable, thread-safe) before being returned.
    /// </summary>
    private static WpfBitmapSource Render(int size)
    {
        // ── Brand palette ─────────────────────────────────────────────────────
        // Edit these strings to change the icon colours.
        const string TealHex = "#0E7C86";   // VERIFIQ teal  (matches --teal in app.css)

        var teal  = new WpfSolidBrush(
            (WpfColor)System.Windows.Media.ColorConverter.ConvertFromString(TealHex)!);
        var white = WpfBrushes.White;

        // ── Layout ────────────────────────────────────────────────────────────
        double radius   = size * 0.22;      // Rounded corner radius
        double margin   = size * 0.06;      // Inset from edge
        double fontSize = size * 0.40;      // "VQ" text size relative to icon
        double s        = size;             // Shorthand

        // ── Draw ──────────────────────────────────────────────────────────────
        var visual = new WpfDrawingVisual();
        using (var ctx = visual.RenderOpen())
        {
            // Teal rounded rectangle background
            ctx.DrawRoundedRectangle(
                teal,
                null,
                new WpfRect(margin, margin, s - margin * 2, s - margin * 2),
                radius, radius);

            // "VQ" white bold text, centred
            var typeface = new WpfTypeface(
                new WpfFontFamily("Arial"),
                WpfFontStyles.Normal,
                WpfFontWeights.Bold,
                WpfFontStretches.Normal);

            var ft = new WpfFormattedText(
                "VQ",
                CultureInfo.InvariantCulture,
                WpfFlowDirection.LeftToRight,
                typeface,
                fontSize,
                white,
                1.0);    // pixelsPerDip — 1.0 = 96 dpi baseline, avoids requiring a visual tree

            ctx.DrawText(ft, new WpfPoint(
                (s - ft.Width)  / 2.0,
                (s - ft.Height) / 2.0));
        }

        // ── Rasterise and freeze ──────────────────────────────────────────────
        var rtb = new WpfRtb(size, size, 96, 96, WpfPixelFormats.Pbgra32);
        rtb.Render(visual);
        rtb.Freeze();       // Immutable + thread-safe; safe to share across threads
        return rtb;
    }
}
