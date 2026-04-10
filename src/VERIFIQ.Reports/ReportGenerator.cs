using VERIFIQ.Reports;
// VERIFIQ — Report Generator (Master Dispatcher)
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Routes each export format to its dedicated branded builder.
// All builders produce full VERIFIQ branded layout, formulas, and design code results.

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Reports;

public sealed class ReportGenerator
{
    private const string AppName    = "VERIFIQ: IFC Compliance Checker";
    private const string Company    = "BBMW0 Technologies";
    private const string Founder    = "Jia Wen Gan";
    private const string Website    = "bbmw0.com";
    private const string Contact    = "bbmw0@hotmail.com";
    private const string AppVersion = "1.0.0";

    public async Task ExportAsync(
        ValidationSession session,
        ExportFormat format,
        string outputPath,
        ReportTemplate template = ReportTemplate.Professional,
        CancellationToken ct = default)
    {
        switch (format)
        {
            case ExportFormat.Word:
                await new WordReportBuilder(session, AppName, Company, Founder, Website, template)
                    .BuildAsync(outputPath, ct);
                break;
            case ExportFormat.PDF:
                await new PdfReportBuilderFull(session, AppName, Company, Founder, Website, template)
                    .BuildAsync(outputPath, ct);
                break;
            case ExportFormat.Excel:
                await new ExcelReportBuilderFull(session, AppName, Company, Founder)
                    .BuildAsync(outputPath, ct);
                break;
            case ExportFormat.CSV:
                await ExportCsvAsync(session, outputPath, ct);
                break;
            case ExportFormat.JSON:
                await ExportJsonAsync(session, outputPath, ct);
                break;
            case ExportFormat.HTML:
                await ExportHtmlAsync(session, outputPath, ct);
                break;
            case ExportFormat.XML:
                await ExportXmlAsync(session, outputPath, ct);
                break;
            case ExportFormat.Markdown:
                await ExportMarkdownAsync(session, outputPath, ct);
                break;
            case ExportFormat.Text:
                await ExportTextAsync(session, outputPath, ct);
                break;
            case ExportFormat.BCF:
                await ExportBcfAsync(session, outputPath, ct);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    public string GetDefaultFileName(ValidationSession session, ExportFormat format,
        ReportTemplate template = ReportTemplate.Professional)
    {
        var country = session.CountryMode switch
        {
            CountryMode.Singapore => "SG",
            CountryMode.Malaysia  => "MY",
            _                     => "SGMY"
        };
        var date = session.StartedAt.ToString("yyyyMMdd_HHmm");
        var ext = format switch
        {
            ExportFormat.Word     => "docx",
            ExportFormat.PDF      => "pdf",
            ExportFormat.Excel    => "xlsx",
            ExportFormat.CSV      => "csv",
            ExportFormat.JSON     => "json",
            ExportFormat.HTML     => "html",
            ExportFormat.XML      => "xml",
            ExportFormat.Markdown => "md",
            ExportFormat.Text     => "txt",
            ExportFormat.BCF      => "bcf",
            _                     => "txt"
        };
        var tmplAbbr = template switch
        {
            ReportTemplate.ExecutiveSummary => "_Exec",
            ReportTemplate.BcaFormat        => "_BCA",
            ReportTemplate.ScdfFormat       => "_SCDF",
            ReportTemplate.NbesFormat       => "_NBeS",
            ReportTemplate.Minimal          => "_Print",
            ReportTemplate.Technical        => "_Tech",
            ReportTemplate.AuditTrail       => "_Audit",
            _                               => "",
        };
        return $"VERIFIQ_Compliance_{country}_{date}{tmplAbbr}.{ext}";
    }

    // All format implementations are in ReportFormatBuilders.cs
    private Task ExportCsvAsync(ValidationSession s, string path, CancellationToken ct)
        => ReportFormatBuilders.WriteCsvAsync(s, path, ct, AppName, Company, Founder, Website);
    private Task ExportJsonAsync(ValidationSession s, string path, CancellationToken ct)
        => ReportFormatBuilders.WriteJsonAsync(s, path, ct, AppName, Company, Founder, Website, Contact, AppVersion);
    private Task ExportHtmlAsync(ValidationSession s, string path, CancellationToken ct)
        => ReportFormatBuilders.WriteHtmlAsync(s, path, ct, AppName, Company, Founder, Website);
    private Task ExportXmlAsync(ValidationSession s, string path, CancellationToken ct)
        => ReportFormatBuilders.WriteXmlAsync(s, path, ct, AppName, Company, Founder, Website);
    private Task ExportMarkdownAsync(ValidationSession s, string path, CancellationToken ct)
        => ReportFormatBuilders.WriteMarkdownAsync(s, path, ct, AppName, Company, Founder, Website);
    private Task ExportTextAsync(ValidationSession s, string path, CancellationToken ct)
        => ReportFormatBuilders.WriteTextAsync(s, path, ct, AppName, Company, Founder, Website);
    private Task ExportBcfAsync(ValidationSession s, string path, CancellationToken ct)
        => ReportFormatBuilders.WriteBcfAsync(s, path, ct, Company);
}

// ─── PDF BUILDER FULL ────────────────────────────────────────────────────────

public sealed class PdfReportBuilderFull
{
    private readonly ValidationSession _s;
    private readonly string _app, _company, _founder, _website;
    private readonly ReportTemplate _template;

    public PdfReportBuilderFull(ValidationSession s,
        string app, string company, string founder, string website,
        ReportTemplate template = ReportTemplate.Professional)
    { _s = s; _app = app; _company = company; _founder = founder; _website = website; _template = template; }

    public async Task BuildAsync(string path, CancellationToken ct)
    {
        var htmlPath = System.IO.Path.ChangeExtension(path, ".html");
        await ReportFormatBuilders.WriteHtmlAsync(_s, htmlPath, ct, _app, _company, _founder, _website, _template);

        // Inject print-media CSS
        var html = await File.ReadAllTextAsync(htmlPath, ct);
        html = html.Replace("</style>",
            "@media print{.filter-bar,.btn{display:none!important}" +
            "body{font-size:10px}thead th{font-size:9px}}" +
            "</style>");
        await File.WriteAllTextAsync(htmlPath, html, ct);

        await File.WriteAllTextAsync(path,
            $"VERIFIQ PDF Report  -  Generated {_s.StartedAt:dd MMM yyyy HH:mm} UTC\n\n" +
            $"A print-optimised HTML is saved as:\n{htmlPath}\n\n" +
            $"Open in your browser and press Ctrl+P > Save as PDF.\n\n" +
            $"Score: {_s.ComplianceScore:F1}% | Critical: {_s.CriticalElements} | Errors: {_s.ErrorElements}\n\n" +
            $"Native PdfSharpCore PDF generation will be added in VERIFIQ v1.1.", ct);
    }
}
