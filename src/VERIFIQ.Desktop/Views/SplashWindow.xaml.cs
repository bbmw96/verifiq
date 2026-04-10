// VERIFIQ — Splash + Licence Windows
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.

using System.IO;
using System.Windows;
using VERIFIQ.Security;
// UseWindowsForms=true brings System.Windows.Forms into scope — disambiguate WPF types explicitly.
using Application            = System.Windows.Application;
using HorizontalAlignment    = System.Windows.HorizontalAlignment;
using VerticalAlignment      = System.Windows.VerticalAlignment;
using MessageBox             = System.Windows.MessageBox;
using MessageBoxButton       = System.Windows.MessageBoxButton;
using MessageBoxImage        = System.Windows.MessageBoxImage;
using MessageBoxResult       = System.Windows.MessageBoxResult;

namespace VERIFIQ.Desktop;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
        // Set icon so it appears in the taskbar while the splash is visible.
        Icon = VqIcon.Get();
    }
}

// ─── LICENCE WINDOW XAML-LIKE CODE ───────────────────────────────────────────

public class LicenceWindow : Window
{
    private System.Windows.Controls.TextBox? _keyBox;

    public LicenceWindow()
    {
        Title  = "VERIFIQ: Licence Activation";
        Width  = 520;
        Height = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode            = ResizeMode.NoResize;
        Background            = System.Windows.Media.Brushes.White;

        // Set VQ icon — appears in the title bar, taskbar button, and Alt+Tab.
        Icon = VqIcon.Get();

        // Taskbar entry description shown when hovering over the taskbar button.
        TaskbarItemInfo = new System.Windows.Shell.TaskbarItemInfo
        {
            Description = "VERIFIQ: Licence Activation"
        };

        BuildLayout();
    }

    private void BuildLayout()
    {
        var grid = new System.Windows.Controls.Grid();
        grid.Margin = new Thickness(32);
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = System.Windows.GridLength.Auto });

        // Logo + Title
        var logoPanel = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 20) };
        var logoBorder = new System.Windows.Controls.Border
        {
            Width = 48, Height = 48, CornerRadius = new CornerRadius(8),
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(11, 31, 69)),
            Margin = new Thickness(0, 0, 14, 0)
        };
        var logoText = new System.Windows.Controls.TextBlock
        {
            Text = "VQ", Foreground = System.Windows.Media.Brushes.White,
            FontWeight = FontWeights.Bold, FontSize = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        logoBorder.Child = logoText;
        logoPanel.Children.Add(logoBorder);

        var titleStack = new System.Windows.Controls.StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "Activate VERIFIQ", FontSize = 20, FontWeight = FontWeights.Bold,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(11, 31, 69))
        });
        titleStack.Children.Add(new System.Windows.Controls.TextBlock
        {
            Text = "IFC Compliance Checker: Singapore and Malaysia",
            FontSize = 12, Foreground = System.Windows.Media.Brushes.Gray
        });
        logoPanel.Children.Add(titleStack);
        System.Windows.Controls.Grid.SetRow(logoPanel, 0);
        grid.Children.Add(logoPanel);

        // Instruction
        var instruction = new System.Windows.Controls.TextBlock
        {
            Text = "Enter your licence key below.\n" +
                   "Format: VRFQ-[TIER]-[NNNN]-0000-[CHECKSUM]\n" +
                   "To purchase: bbmw0@hotmail.com  |  bbmw0.com",
            TextWrapping = TextWrapping.Wrap, FontSize = 12,
            Foreground = System.Windows.Media.Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 16)
        };
        System.Windows.Controls.Grid.SetRow(instruction, 1);
        grid.Children.Add(instruction);

        // Key input
        _keyBox = new System.Windows.Controls.TextBox
        {
            FontFamily = new System.Windows.Media.FontFamily("Courier New"),
            FontSize = 14, Height = 40,
            Padding = new Thickness(10, 8, 10, 8),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            Text = "VRFQ-",
            MaxLength = 35,
            Margin = new Thickness(0, 0, 0, 16)
        };
        System.Windows.Controls.Grid.SetRow(_keyBox, 2);
        grid.Children.Add(_keyBox);

        // Trial note
        var trialNote = new System.Windows.Controls.TextBlock
        {
            Text = "Trial mode: 10 elements per run.\n" +
                   "  Trial key:      VRFQ-TRIAL-DEMO0-0000-00000001\n" +
                   "  Individual key: VRFQ-IND1-0001-0000-6C6A84BB",
            TextWrapping = TextWrapping.Wrap, FontSize = 11,
            Foreground = System.Windows.Media.Brushes.DarkBlue,
            Margin = new Thickness(0, 0, 0, 0)
        };
        System.Windows.Controls.Grid.SetRow(trialNote, 3);
        grid.Children.Add(trialNote);

        // Button row
        var buttonPanel = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 12, 0, 0)
        };

        var skipBtn = new System.Windows.Controls.Button
        {
            Content = "Skip (Trial Mode)",
            Width = 140, Height = 36, Margin = new Thickness(0, 0, 10, 0),
            Background = System.Windows.Media.Brushes.Transparent,
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand
        };
        skipBtn.Click += (s, e) => OpenMainWindowAsTrial();

        var activateBtn = new System.Windows.Controls.Button
        {
            Content = "Activate →",
            Width = 100, Height = 36,
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(11, 31, 69)),
            Foreground = System.Windows.Media.Brushes.White,
            BorderThickness = new Thickness(0),
            FontWeight = FontWeights.Bold,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        activateBtn.Click += (s, e) => TryActivate();

        buttonPanel.Children.Add(skipBtn);
        buttonPanel.Children.Add(activateBtn);
        System.Windows.Controls.Grid.SetRow(buttonPanel, 5);
        grid.Children.Add(buttonPanel);

        Content = grid;
    }

    private void TryActivate()
    {
        var key       = _keyBox?.Text?.Trim() ?? string.Empty;
        var validator = new LicenceValidator();
        var licence   = validator.Validate(key);

        if (!licence.IsValid)
        {
            MessageBox.Show(
                $"Licence activation failed:\n\n{licence.InvalidReason}\n\n" +
                "Please check the key and try again. Contact bbmw0@hotmail.com for assistance.",
                "VERIFIQ: Invalid Licence Key",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Save the validated key so it loads automatically on next launch.
        try
        {
            var settingsPath = Path.Combine(App.AppDataPath, "settings.json");
            var settings     = new Core.Models.AppSettings { LicenceKey = key };
            File.WriteAllText(settingsPath,
                System.Text.Json.JsonSerializer.Serialize(settings));
        }
        catch { /* Key save failure is non-critical; activation still succeeds. */ }

        try
        {
            App.CurrentLicence = licence;
            var main = new MainWindow();
            Application.Current.MainWindow = main; // Set BEFORE Show.
            main.Show();
            Close();                               // Close AFTER main is shown.
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"VERIFIQ could not open the main window.\n\nError: {ex.Message}",
                "VERIFIQ: Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenMainWindowAsTrial()
    {
        try
        {
            App.CurrentLicence = new LicenceInfo
            {
                IsValid     = true,
                Tier        = LicenceTier.Trial,
                LicencedTo  = "Trial User",
                MaxUsers    = 1,
                IsPerpetual = false,
                ExpiresAt   = DateTime.MaxValue
            };

            var main = new MainWindow();
            Application.Current.MainWindow = main; // Set BEFORE Show.
            main.Show();
            Close();                               // Close AFTER main is shown.
            Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"VERIFIQ could not open in trial mode.\n\nError: {ex.Message}",
                "VERIFIQ: Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
