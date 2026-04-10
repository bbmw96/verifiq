// VERIFIQ: IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.

using System.IO;
using System.Threading.Tasks;
using System.Windows;
using VERIFIQ.Security;

// UseWindowsForms=true brings System.Windows.Forms.Application into scope.
// Alias WPF types explicitly so every name is unambiguous throughout this file.
using Application         = System.Windows.Application;
using MessageBoxButton    = System.Windows.MessageBoxButton;
using MessageBoxImage     = System.Windows.MessageBoxImage;
using MessageBoxResult    = System.Windows.MessageBoxResult;
using MessageBox          = System.Windows.MessageBox;

namespace VERIFIQ.Desktop;

public partial class App : Application
{
    public static LicenceInfo? CurrentLicence { get; internal set; }
    public static string AppDataPath { get; internal set; } = string.Empty;
    public static string DataPath    { get; internal set; } = string.Empty;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Prevent WPF from auto-exiting when no windows are open during the
        // startup transition (splash closes, new window not yet shown).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Initialise network service early — loads proxy settings from disk and
        // starts the connectivity monitor before any HTTP calls are made.
        Services.NetworkService.Instance.Initialise();

        // Global UI-thread exception handler — catches any unhandled WPF exceptions.
        DispatcherUnhandledException += (sender, ex) =>
        {
            ex.Handled = true;
            MessageBox.Show(
                $"An unexpected error occurred:\n\n{ex.Exception.Message}\n\n" +
                $"{ex.Exception.StackTrace}",
                "VERIFIQ: Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        // Set up application data paths.
        AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BBMW0Technologies", "VERIFIQ");
        Directory.CreateDirectory(AppDataPath);

        DataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        Directory.CreateDirectory(DataPath);

        // Show splash immediately on the UI thread.
        var splash = new SplashWindow();
        splash.Show();
        MainWindow = splash;

        // Run the startup sequence on a background thread so the splash renders.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400); // Allow splash to paint fully.

                // ── Integrity check ───────────────────────────────────────────
                var checker   = new IntegrityChecker();
                var integrity = checker.CheckApplicationIntegrity(
                    AppDomain.CurrentDomain.BaseDirectory);

                if (!integrity.IsValid)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(
                            $"Application integrity check failed.\n\n{integrity.Message}\n\n" +
                            "Please reinstall VERIFIQ from bbmw0.com",
                            "VERIFIQ: Integrity Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Current.Shutdown(1);
                    });
                    return;
                }

                // ── Load saved licence key ────────────────────────────────────
                string savedKey = string.Empty;
                try
                {
                    var settingsPath = Path.Combine(AppDataPath, "settings.json");
                    if (File.Exists(settingsPath))
                    {
                        var json     = File.ReadAllText(settingsPath);
                        var settings = System.Text.Json.JsonSerializer
                            .Deserialize<Core.Models.AppSettings>(json);
                        savedKey = settings?.LicenceKey ?? string.Empty;
                    }
                }
                catch { /* Corrupt settings file - start without saved key. */ }

                // ── Validate licence ──────────────────────────────────────────
                var validator = new LicenceValidator();
                var licence   = validator.Validate(savedKey);

                // ── Open the correct next window on the UI thread ─────────────
                await Dispatcher.InvokeAsync(() =>
                {
                    CurrentLicence = licence;

                    try
                    {
                        if (!licence.IsValid)
                        {
                            // No valid licence: show the activation window.
                            var licenceWin = new LicenceWindow();
                            MainWindow = licenceWin;   // Set BEFORE Show to keep
                            licenceWin.Show();          // ShutdownMode happy.
                        }
                        else
                        {
                            // Valid licence: open the main application window.
                            var main = new MainWindow();
                            MainWindow = main;
                            main.Show();
                        }

                        // Close splash AFTER the next window is safely shown.
                        splash.Close();

                        // Now that the real window is open, enable normal shutdown.
                        ShutdownMode = ShutdownMode.OnLastWindowClose;
                    }
                    catch (Exception innerEx)
                    {
                        // New window failed to open. Show the error and let the
                        // user decide whether to retry or exit.
                        MessageBox.Show(
                            $"VERIFIQ could not open the main window.\n\n" +
                            $"Error: {innerEx.Message}\n\n" +
                            $"Please reinstall VERIFIQ if this persists.",
                            "VERIFIQ: Startup Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Current.Shutdown(1);
                    }
                });
            }
            catch (Exception ex)
            {
                // Something failed on the background thread before reaching the UI.
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"VERIFIQ could not start.\n\n" +
                        $"Error: {ex.Message}\n\n" +
                        $"{ex.StackTrace}",
                        "VERIFIQ: Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Current.Shutdown(1);
                });
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }
}
