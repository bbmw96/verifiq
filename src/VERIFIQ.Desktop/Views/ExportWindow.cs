// VERIFIQ — IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.

using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;
using VERIFIQ.Reports;
// UseWindowsForms=true brings System.Windows.Forms into scope — disambiguate WPF types explicitly.
using Application         = System.Windows.Application;
using MessageBox          = System.Windows.MessageBox;
using MessageBoxButton    = System.Windows.MessageBoxButton;
using MessageBoxImage     = System.Windows.MessageBoxImage;
using MessageBoxResult    = System.Windows.MessageBoxResult;

namespace VERIFIQ.Desktop;

public partial class ExportWindow : Window
{
    private readonly ValidationSession _session;
    private readonly ReportGenerator   _reporter;

    public ExportWindow(ValidationSession session, ReportGenerator reporter)
    {
        _session  = session;
        _reporter = reporter;
        InitializeComponent();
        Title = "VERIFIQ: Export Compliance Report";
        Icon  = VqIcon.Get();    // VQ logo in title bar and taskbar
    }

    /// <summary>Returns the selected report template from the radio button group.</summary>
    private VERIFIQ.Reports.ReportTemplate SelectedTemplate
    {
        get
        {
            foreach (var rb in new[] { TmplProfessional, TmplExecutive, TmplBca, TmplScdf,
                                       TmplNbes, TmplMinimal, TmplTechnical, TmplAudit })
            {
                if (rb?.IsChecked == true &&
                    rb.Tag is string tag &&
                    Enum.TryParse<VERIFIQ.Reports.ReportTemplate>(tag, out var t))
                    return t;
            }
            return VERIFIQ.Reports.ReportTemplate.Professional;
        }
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var template  = SelectedTemplate;
        var formats   = new List<ExportFormat>();
        if (ChkWord.IsChecked     == true) formats.Add(ExportFormat.Word);
        if (ChkPdf.IsChecked      == true) formats.Add(ExportFormat.PDF);
        if (ChkExcel.IsChecked    == true) formats.Add(ExportFormat.Excel);
        if (ChkCsv.IsChecked      == true) formats.Add(ExportFormat.CSV);
        if (ChkJson.IsChecked     == true) formats.Add(ExportFormat.JSON);
        if (ChkHtml.IsChecked     == true) formats.Add(ExportFormat.HTML);
        if (ChkXml.IsChecked      == true) formats.Add(ExportFormat.XML);
        if (ChkMarkdown.IsChecked == true) formats.Add(ExportFormat.Markdown);
        if (ChkText.IsChecked     == true) formats.Add(ExportFormat.Text);
        if (ChkBcf.IsChecked      == true) formats.Add(ExportFormat.BCF);

        if (formats.Count == 0)
        {
            MessageBox.Show("Please select at least one export format.",
                "VERIFIQ: Export", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Choose output folder
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select output folder for VERIFIQ reports"
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

        var outputDir = dlg.SelectedPath;
        var exported  = new List<string>();
        var errors    = new List<string>();

        ExportProgress.Visibility = Visibility.Visible;
        ExportProgress.Maximum    = formats.Count;
        ExportProgress.Value      = 0;
        BtnExport.IsEnabled       = false;

        foreach (var format in formats)
        {
            try
            {
                var fileName = _reporter.GetDefaultFileName(_session, format, template);
                var fullPath = Path.Combine(outputDir, fileName);

                ExportStatusLabel.Text = $"Exporting {format} ({VERIFIQ.Reports.ReportTemplates.Get(template).Name})...";
                await _reporter.ExportAsync(_session, format, fullPath, template);

                exported.Add(fileName);
                ExportProgress.Value++;
            }
            catch (Exception ex)
            {
                errors.Add($"{format}: {ex.Message}");
            }
        }

        BtnExport.IsEnabled = true;
        ExportProgress.Visibility = Visibility.Collapsed;

        var sb = new System.Text.StringBuilder();
        if (exported.Count > 0)
        {
            sb.AppendLine($"Successfully exported {exported.Count} report(s) to:");
            sb.AppendLine(outputDir);
            sb.AppendLine();
            foreach (var f in exported) sb.AppendLine($"  ✓  {f}");
        }
        if (errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Errors:");
            foreach (var err in errors) sb.AppendLine($"  ✗  {err}");
        }

        MessageBox.Show(sb.ToString(), "VERIFIQ: Export Complete",
            MessageBoxButton.OK,
            errors.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);

        if (exported.Count > 0 && errors.Count == 0)
            Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();


    private void Template_Checked(object sender, RoutedEventArgs e)
    {
        if (TemplateDesc == null) return;
        var tmpl = VERIFIQ.Reports.ReportTemplates.Get(SelectedTemplate);
        TemplateDesc.Text = tmpl.Description;
    }

    private void CheckAll_Click(object sender, RoutedEventArgs e)
    {
        ChkWord.IsChecked = ChkPdf.IsChecked = ChkExcel.IsChecked = ChkCsv.IsChecked =
        ChkJson.IsChecked = ChkHtml.IsChecked = ChkXml.IsChecked = ChkMarkdown.IsChecked =
        ChkText.IsChecked = ChkBcf.IsChecked = true;
    }
}
