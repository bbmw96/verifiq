// VERIFIQ - Text-Format Report Builders
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Implements CSV, JSON, HTML, XML, Markdown, Text and BCF exports.
// Every format includes:
//   • VERIFIQ branded header
//   • Compliance score formulas with actual substituted values
//   • Data compliance results (all 20 check levels)
//   • Design code results (actual vs required with formula + result)
//   • Singapore agency breakdown
//   • Regulatory code references

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Reports;

public static class ReportFormatBuilders
{
    // ─── CSV ──────────────────────────────────────────────────────────────────

    public static async Task WriteCsvAsync(ValidationSession s, string path,
        CancellationToken ct, string app, string company, string founder, string website)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {app} | {company} | Developed by {founder} | {website}");
        sb.AppendLine($"# Generated: {s.StartedAt:dd MMMM yyyy HH:mm} UTC");
        sb.AppendLine($"# Country Mode: {s.CountryMode}  |  Session: {s.SessionId}");
        sb.AppendLine($"# DATA SCORE FORMULA: = ({s.PassedElements} / {s.TotalElements}) * 100 = {s.ComplianceScore:F1}%");
        if (s.DesignCode != null)
        {
            sb.AppendLine($"# DESIGN SCORE FORMULA: = ({s.DesignCode.PassedChecks} / {s.DesignCode.TotalChecks}) * 100 = {s.DesignCode.DesignComplianceScore:F1}%");
            sb.AppendLine($"# OVERALL SCORE FORMULA: = ({s.ComplianceScore:F1} + {s.DesignCode.DesignComplianceScore:F1}) / 2 = {s.OverallScore:F1}%");
        }
        sb.AppendLine("#");
        sb.AppendLine("# SECTION: DATA COMPLIANCE FINDINGS");
        sb.AppendLine("ReportType,ElementGUID,ElementName,IfcClass,StoreyName,CheckLevel,Severity,Country,AffectedAgency,Gateway,PropertySetName,PropertyName,ExpectedValue,ActualValue,Message,RemediationGuidance,RuleSource,CodeReference");

        foreach (var r in s.Results.OrderByDescending(x => x.Severity).ThenBy(x => x.ElementGuid))
            sb.AppendLine(string.Join(",", Q("DataCompliance"), Q(r.ElementGuid), Q(r.ElementName), Q(r.IfcClass), Q(r.StoreyName), Q(r.CheckLevel.ToString()), Q(r.Severity.ToString()), Q(r.Country.ToString()), Q(r.AffectedAgency.ToString()), Q(r.AffectedGateway.ToString()), Q(r.PropertySetName), Q(r.PropertyName), Q(r.ExpectedValue), Q(r.ActualValue), Q(r.Message), Q(r.RemediationGuidance), Q(r.RuleSource), Q(r.RuleReference)));

        if (s.DesignCode != null)
        {
            sb.AppendLine();
            sb.AppendLine("# SECTION: DESIGN CODE CHECKS (actual value checks vs regulatory requirements)");
            sb.AppendLine("ReportType,RuleID,RuleName,CodeReference,ElementGUID,ElementName,IFCClass,SpaceCategory,CheckParameter,CheckUnit,ActualValue,ActualDisplay,RequiredMinimum,RequiredMaximum,RequiredDisplay,Formula,FormulaResult,Severity,Complies,Message,RemediationGuidance");

            foreach (var d in s.DesignCode.Results.OrderByDescending(x => x.Severity))
                sb.AppendLine(string.Join(",", Q("DesignCode"), Q(d.RuleId), Q(d.RuleName), Q(d.CodeReference), Q(d.ElementGuid), Q(d.ElementName), Q(d.IfcClass), Q(d.SpaceCategory), Q(d.CheckParameter), Q(d.CheckUnit), d.ActualValue.ToString("F4"), Q(d.ActualDisplay), d.RequiredMinimum.ToString("F4"), d.RequiredMaximum?.ToString("F4") ?? "", Q(d.RequiredDisplay), Q(d.Formula), Q(d.FormulaResult), Q(d.Severity.ToString()), d.Complies.ToString(), Q(d.Message), Q(d.RemediationGuidance)));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, ct);
    }

    // ─── JSON ─────────────────────────────────────────────────────────────────

    public static async Task WriteJsonAsync(ValidationSession s, string path,
        CancellationToken ct, string app, string company, string founder,
        string website, string contact, string version)
    {
        var dc = s.DesignCode;
        var report = new
        {
            meta = new { application = app, company, founder, website, contact, version,
                sessionId = s.SessionId, generatedAt = s.StartedAt,
                countryMode = s.CountryMode.ToString(), durationSeconds = s.Duration.TotalSeconds },
            formulas = new
            {
                dataScore = new { formula = "(Passed / Total) * 100",
                    passed = s.PassedElements, total = s.TotalElements, result = $"{s.ComplianceScore:F1}%" },
                designScore = dc == null ? null : (object)new { formula = "(PassedChecks / TotalChecks) * 100",
                    passed = dc.PassedChecks, total = dc.TotalChecks, result = $"{dc.DesignComplianceScore:F1}%" },
                overallScore = dc == null ? null : (object)new { formula = "(DataScore + DesignScore) / 2", result = $"{s.OverallScore:F1}%" }
            },
            summary = new
            {
                totalElements = s.TotalElements, passed = s.PassedElements, warnings = s.WarningElements,
                errors = s.ErrorElements, critical = s.CriticalElements, proxies = s.ProxyElements,
                complianceScore = s.ComplianceScore, designScore = dc?.DesignComplianceScore, overallScore = s.OverallScore,
                errorsByAgency = s.ErrorsByAgency.ToDictionary(k => k.Key.ToString(), k => k.Value),
                errorsByCheckLevel = s.ErrorsByCheckLevel.ToDictionary(k => k.Key.ToString(), k => k.Value),
                designFailsByCategory = dc?.FailuresByCategory.ToDictionary(k => k.Key.ToString(), k => k.Value)
            },
            files = s.LoadedFiles.Select(f => new { f.FileName, schema = f.Schema.ToString(), f.TotalElementCount, f.ProxyElementCount, f.ParsedAt }),
            dataFindings = s.Results.OrderByDescending(r => r.Severity).Select(r => new
            {
                r.ElementGuid, r.ElementName, r.IfcClass, storey = r.StoreyName,
                checkLevel = r.CheckLevel.ToString(), severity = r.Severity.ToString(),
                country = r.Country.ToString(), agency = r.AffectedAgency.ToString(),
                gateway = r.AffectedGateway.ToString(), r.PropertySetName, r.PropertyName,
                r.ExpectedValue, r.ActualValue, r.Message, remediation = r.RemediationGuidance,
                r.RuleSource, codeReference = r.RuleReference
            }),
            designCodeFindings = dc?.Results.OrderByDescending(r => r.Severity).Select(d => new
            {
                d.RuleId, d.RuleName, d.CodeReference, d.RegulationText, category = d.Category.ToString(),
                d.ElementGuid, d.ElementName, d.IfcClass, d.SpaceCategory, d.CheckParameter, d.CheckUnit,
                d.ActualValue, d.ActualDisplay, d.RequiredMinimum, d.RequiredMaximum, d.RequiredDisplay,
                d.Formula, d.FormulaResult, severity = d.Severity.ToString(), d.Complies, d.Message, remediation = d.RemediationGuidance
            })
        };

        var opts = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report, opts), Encoding.UTF8, ct);
    }

    // ─── HTML ─────────────────────────────────────────────────────────────────

    public static async Task WriteHtmlAsync(ValidationSession s, string path,
        CancellationToken ct, string app, string company, string founder, string website,
        ReportTemplate template = ReportTemplate.Professional)
    {
        var dc   = s.DesignCode;
        var tmpl = ReportTemplates.Get(template);
        var sb   = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
        sb.AppendLine($"<title>VERIFIQ Compliance Report - {s.StartedAt:yyyy-MM-dd} [{tmpl.Name}]</title>");
        sb.AppendLine($"<style>{EmbeddedCss(template)}</style></head><body>");

        // Header
        sb.AppendLine($"<header class=\"hdr\"><div class=\"hdr-logo\"><div class=\"logo-mark\">VQ</div><div><div class=\"logo-name\">VERIFIQ</div><div class=\"logo-sub\">IFC Compliance Checker</div></div></div><div class=\"hdr-right\"><span class=\"mode-pill\">{H(ModeLabel(s.CountryMode))}</span><span>{s.StartedAt:dd MMM yyyy HH:mm} UTC</span></div></header>");
        sb.AppendLine("<div class=\"wrap\">");

        // Formula box
        sb.AppendLine("<div class=\"card\"><h2>Compliance Score Formulas</h2><div class=\"fbox\">");
        sb.AppendLine($"<b>Data Score</b> = (Passed ÷ Total) × 100 = ({s.PassedElements} ÷ {s.TotalElements}) × 100 = <b class=\"{SC(s.ComplianceScore)}\">{s.ComplianceScore:F1}%</b><br>");
        if (dc != null)
        {
            sb.AppendLine($"<b>Design Score</b> = (Passed Checks ÷ Total Checks) × 100 = ({dc.PassedChecks} ÷ {dc.TotalChecks}) × 100 = <b class=\"{SC(dc.DesignComplianceScore)}\">{dc.DesignComplianceScore:F1}%</b><br>");
            sb.AppendLine($"<b>Overall Score</b> = (Data + Design) ÷ 2 = ({s.ComplianceScore:F1} + {dc.DesignComplianceScore:F1}) ÷ 2 = <b class=\"{SC(s.OverallScore)}\">{s.OverallScore:F1}%</b>");
        }
        sb.AppendLine("</div></div>");

        // Stat cards
        sb.AppendLine("<div class=\"cards\">");
        void Card(string v, string l, string c = "") => sb.AppendLine($"<div class=\"stat\"><div class=\"sval {c}\">{v}</div><div class=\"slbl\">{l}</div></div>");
        Card($"{s.ComplianceScore:F1}%", "Data Score", SC(s.ComplianceScore));
        Card($"{s.TotalElements:N0}", "Total Elements");
        Card($"{s.PassedElements:N0}", "Compliant", "grn");
        Card($"{s.WarningElements:N0}", "Warnings", "amb");
        Card($"{s.ErrorElements:N0}", "Errors", "amb");
        Card($"{s.CriticalElements:N0}", "Critical", "red");
        Card($"{s.ProxyElements:N0}", "Proxy");
        if (dc != null) Card($"{dc.DesignComplianceScore:F1}%", "Design Score", SC(dc.DesignComplianceScore));
        sb.AppendLine("</div>");

        // Agency table (SG)
        if (s.ErrorsByAgency.Any() && s.CountryMode != CountryMode.Malaysia)
        {
            sb.AppendLine("<div class=\"card\"><h2>Singapore: Errors by Agency</h2><table><thead><tr><th>Agency</th><th>Errors</th><th>% of Errors</th><th>Formula</th></tr></thead><tbody>");
            int tot = s.Results.Count(r => r.Severity >= Severity.Error);
            foreach (var kvp in s.ErrorsByAgency.OrderByDescending(k => k.Value))
            {
                double p = tot > 0 ? (double)kvp.Value / tot * 100 : 0;
                sb.AppendLine($"<tr><td><span class=\"ab ab-{kvp.Key}\">{kvp.Key}</span></td><td class=\"n\">{kvp.Value}</td><td class=\"n\">{p:F1}%</td><td class=\"mono\">{kvp.Value} ÷ {tot} × 100 = {p:F1}%</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Design code failures
        if (dc != null && dc.FailedChecks > 0)
        {
            sb.AppendLine("<div class=\"card\"><h2>Design Code Failures: Actual Values vs Code Requirements</h2>");
            sb.AppendLine("<div class=\"fbox\" style=\"margin-bottom:12px\">");
            sb.AppendLine($"Checks actual dimensions, areas, and distances from the IFC model against published code requirements.<br>");
            sb.AppendLine($"Score = ({dc.PassedChecks} ÷ {dc.TotalChecks}) × 100 = <b class=\"{SC(dc.DesignComplianceScore)}\">{dc.DesignComplianceScore:F1}%</b>  |  {dc.FailedChecks} failures, {dc.CriticalChecks} critical");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"fb\"><select id=\"dc-s\" onchange=\"fDC()\"><option value=\"\">All Severities</option><option>Critical</option><option>Error</option><option>Warning</option></select><input id=\"dc-q\" placeholder=\"Search rule, element...\" oninput=\"fDC()\"/></div>");
            sb.AppendLine("<table id=\"dct\"><thead><tr><th>Severity</th><th>Rule ID</th><th>Rule Name</th><th>Code Ref</th><th>Element</th><th>Actual</th><th>Required</th><th>Formula</th><th>Result</th><th>Fix</th></tr></thead><tbody>");
            foreach (var d in dc.Results.Where(r => !r.Complies).OrderByDescending(r => r.Severity))
            {
                sb.AppendLine($"<tr class=\"r{d.Severity.ToString().ToLower()}\" data-sev=\"{d.Severity}\" data-q=\"{H((d.RuleId + d.ElementName + d.CodeReference).ToLower())}\">");
                sb.AppendLine($"<td>{SB(d.Severity)}</td><td class=\"mono sml\">{H(d.RuleId)}</td><td>{H(d.RuleName)}</td><td class=\"sml\">{H(d.CodeReference)}</td>");
                sb.AppendLine($"<td><b>{H(d.ElementName)}</b><br><small class=\"grey\">{H(d.IfcClass)}</small></td>");
                sb.AppendLine($"<td class=\"mono fail\">{H(d.ActualDisplay)}</td><td class=\"mono\">{H(d.RequiredDisplay)}</td>");
                sb.AppendLine($"<td class=\"mono sml\">{H(d.Formula)}</td><td class=\"mono sml\">{H(d.FormulaResult)}</td>");
                sb.AppendLine($"<td class=\"fix sml\">{H(d.RemediationGuidance)}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // All data findings
        sb.AppendLine("<div class=\"card\"><h2>All Data Compliance Findings</h2>");
        sb.AppendLine("<div class=\"fb\"><select id=\"fs\" onchange=\"fM()\"><option value=\"\">All Severities</option><option>Critical</option><option>Error</option><option>Warning</option><option>Pass</option></select>");
        sb.AppendLine("<select id=\"fa\" onchange=\"fM()\"><option value=\"\">All Agencies</option>");
        foreach (var a in s.ErrorsByAgency.Keys.OrderBy(k => k.ToString())) sb.AppendLine($"<option>{a}</option>");
        sb.AppendLine("</select><input id=\"fq\" placeholder=\"GUID, name, message...\" oninput=\"fM()\"/><button class=\"btn\" onclick=\"cF()\">Clear</button></div>");
        sb.AppendLine("<table id=\"mt\"><thead><tr><th>Severity</th><th>Level</th><th>Element</th><th>GUID</th><th>Storey</th><th>Agency</th><th>Property</th><th>Issue</th><th>Fix</th></tr></thead><tbody>");

        foreach (var r in s.Results.OrderByDescending(x => x.Severity))
        {
            sb.AppendLine($"<tr class=\"r{r.Severity.ToString().ToLower()}\" data-sev=\"{r.Severity}\" data-ag=\"{r.AffectedAgency}\" data-q=\"{H((r.ElementGuid + r.ElementName + r.Message).ToLower())}\">");
            sb.AppendLine($"<td>{SB(r.Severity)}</td><td class=\"sml\">{H(r.CheckLevel.ToString())}</td>");
            sb.AppendLine($"<td><b>{H(r.ElementName)}</b><br><small class=\"grey\">{H(r.IfcClass)}</small></td>");
            sb.AppendLine($"<td class=\"mono sml\">{H(r.ElementGuid.Length > 12 ? r.ElementGuid[..12] + "…" : r.ElementGuid)}</td>");
            sb.AppendLine($"<td class=\"sml\">{H(r.StoreyName)}</td>");
            sb.AppendLine($"<td>{(r.AffectedAgency != SgAgency.None ? $"<span class=\"ab ab-{r.AffectedAgency}\">{r.AffectedAgency}</span>" : "-")}</td>");
            sb.AppendLine($"<td class=\"mono sml\">{H(r.PropertySetName)}<br>{H(r.PropertyName)}</td>");
            sb.AppendLine($"<td>{H(r.Message)}</td><td class=\"fix sml\">{H(r.RemediationGuidance)}</td></tr>");
        }
        sb.AppendLine("</tbody></table></div>");

        sb.AppendLine($"<footer>{H(tmpl.FooterText)} | {H(website)} | {s.StartedAt:dd MMM yyyy HH:mm} UTC</footer>");
        sb.AppendLine("</div>");
        sb.AppendLine($"<script>{EmbeddedJs()}</script></body></html>");

        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, ct);
    }

    // ─── XML ──────────────────────────────────────────────────────────────────

    public static async Task WriteXmlAsync(ValidationSession s, string path,
        CancellationToken ct, string app, string company, string founder, string website)
    {
        var dc = s.DesignCode;
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<!-- {app} | {company} | Developed by {founder} | {website} -->");
        sb.AppendLine("<VERIFIQReport xmlns=\"https://bbmw0.com/verifiq/report/v1\" version=\"1.0\">");
        sb.AppendLine($"<Meta sessionId=\"{s.SessionId}\" generatedAt=\"{s.StartedAt:O}\" countryMode=\"{s.CountryMode}\" duration=\"{s.Duration.TotalSeconds:F1}\"/>");
        sb.AppendLine("<Formulas>");
        sb.AppendLine($"  <DataScore formula=\"(Passed/Total)*100\" passed=\"{s.PassedElements}\" total=\"{s.TotalElements}\" result=\"{s.ComplianceScore:F1}\"/>");
        if (dc != null)
        {
            sb.AppendLine($"  <DesignScore formula=\"(PassedChecks/TotalChecks)*100\" passed=\"{dc.PassedChecks}\" total=\"{dc.TotalChecks}\" result=\"{dc.DesignComplianceScore:F1}\"/>");
            sb.AppendLine($"  <OverallScore formula=\"(DataScore+DesignScore)/2\" result=\"{s.OverallScore:F1}\"/>");
        }
        sb.AppendLine("</Formulas>");
        sb.AppendLine($"<Summary total=\"{s.TotalElements}\" passed=\"{s.PassedElements}\" warnings=\"{s.WarningElements}\" errors=\"{s.ErrorElements}\" critical=\"{s.CriticalElements}\" proxy=\"{s.ProxyElements}\" dataScore=\"{s.ComplianceScore:F1}\" designScore=\"{dc?.DesignComplianceScore:F1}\" overallScore=\"{s.OverallScore:F1}\"/>");
        sb.AppendLine("<DataFindings>");
        foreach (var r in s.Results.OrderByDescending(x => x.Severity))
            sb.AppendLine($"  <Finding guid=\"{X(r.ElementGuid)}\" name=\"{X(r.ElementName)}\" class=\"{X(r.IfcClass)}\" storey=\"{X(r.StoreyName)}\" check=\"{r.CheckLevel}\" severity=\"{r.Severity}\" country=\"{r.Country}\" agency=\"{r.AffectedAgency}\" pset=\"{X(r.PropertySetName)}\" prop=\"{X(r.PropertyName)}\" expected=\"{X(r.ExpectedValue)}\" actual=\"{X(r.ActualValue)}\" message=\"{X(r.Message)}\" fix=\"{X(r.RemediationGuidance)}\"/>");
        sb.AppendLine("</DataFindings>");
        if (dc != null)
        {
            sb.AppendLine("<DesignCodeFindings>");
            foreach (var d in dc.Results.OrderByDescending(x => x.Severity))
                sb.AppendLine($"  <Check ruleId=\"{X(d.RuleId)}\" ruleName=\"{X(d.RuleName)}\" code=\"{X(d.CodeReference)}\" guid=\"{X(d.ElementGuid)}\" name=\"{X(d.ElementName)}\" param=\"{X(d.CheckParameter)}\" unit=\"{X(d.CheckUnit)}\" actual=\"{d.ActualValue:F4}\" actualDisplay=\"{X(d.ActualDisplay)}\" min=\"{d.RequiredMinimum:F4}\" max=\"{(d.RequiredMaximum.HasValue ? d.RequiredMaximum.Value.ToString("F4") : "")}\" required=\"{X(d.RequiredDisplay)}\" formula=\"{X(d.Formula)}\" result=\"{X(d.FormulaResult)}\" severity=\"{d.Severity}\" complies=\"{d.Complies.ToString().ToLower()}\" message=\"{X(d.Message)}\" fix=\"{X(d.RemediationGuidance)}\"/>");
            sb.AppendLine("</DesignCodeFindings>");
        }
        sb.AppendLine("</VERIFIQReport>");
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, ct);
    }

    // ─── MARKDOWN ─────────────────────────────────────────────────────────────

    public static async Task WriteMarkdownAsync(ValidationSession s, string path,
        CancellationToken ct, string app, string company, string founder, string website)
    {
        var dc = s.DesignCode;
        var sb = new StringBuilder();
        sb.AppendLine($"# VERIFIQ: IFC Compliance Report\n");
        sb.AppendLine($"> **{app}** | {company} | Developed by {founder} | {website}\n");
        sb.AppendLine($"| Field | Value |\n|---|---|\n| Generated | {s.StartedAt:dd MMMM yyyy HH:mm} UTC |\n| Country Mode | {s.CountryMode} |\n| Session | `{s.SessionId}` |\n");
        sb.AppendLine("## Score Formulas\n\n```");
        sb.AppendLine($"Data Score  = ({s.PassedElements} / {s.TotalElements}) * 100 = {s.ComplianceScore:F1}%");
        if (dc != null) { sb.AppendLine($"Design Score = ({dc.PassedChecks} / {dc.TotalChecks}) * 100 = {dc.DesignComplianceScore:F1}%"); sb.AppendLine($"Overall     = ({s.ComplianceScore:F1} + {dc.DesignComplianceScore:F1}) / 2 = {s.OverallScore:F1}%"); }
        sb.AppendLine("```\n");
        sb.AppendLine($"## Summary\n\n| Metric | Count |\n|---|---|\n| Total | {s.TotalElements:N0} |\n| Compliant | {s.PassedElements:N0} |\n| Warnings | {s.WarningElements:N0} |\n| Errors | {s.ErrorElements:N0} |\n| **Critical** | **{s.CriticalElements:N0}** |\n| Proxy | {s.ProxyElements:N0} |\n");
        if (dc != null && dc.FailedChecks > 0)
        {
            sb.AppendLine("## Design Code Failures\n");
            sb.AppendLine("| Severity | Rule ID | Element | Actual | Required | Formula Result |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var d in dc.Results.Where(r => !r.Complies).OrderByDescending(r => r.Severity).Take(40))
                sb.AppendLine($"| **{d.Severity}** | `{d.RuleId}` | {d.ElementName} | `{d.ActualDisplay}` | `{d.RequiredDisplay}` | `{d.FormulaResult}` |");
            sb.AppendLine();
        }
        sb.AppendLine("## Data Compliance Findings\n");
        sb.AppendLine("| Severity | Level | Element | Agency | Issue |");
        sb.AppendLine("|---|---|---|---|---|");
        foreach (var r in s.Results.OrderByDescending(x => x.Severity).Take(300))
            sb.AppendLine($"| **{r.Severity}** | {r.CheckLevel} | {r.ElementName} | {r.AffectedAgency} | {r.Message.Replace("|", "\\|")} |");
        sb.AppendLine($"\n---\n*{app} | {company} | Developed by {founder} | {website}*");
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, ct);
    }

    // ─── TEXT ─────────────────────────────────────────────────────────────────

    public static async Task WriteTextAsync(ValidationSession s, string path,
        CancellationToken ct, string app, string company, string founder, string website)
    {
        var dc = s.DesignCode;
        string L = new('=', 80), D = new('-', 80);
        var sb = new StringBuilder();
        sb.AppendLine(L); sb.AppendLine($"  VERIFIQ: IFC COMPLIANCE REPORT"); sb.AppendLine($"  {company}  |  Developed by {founder}  |  {website}"); sb.AppendLine(L);
        sb.AppendLine($"  Country Mode : {s.CountryMode}");
        sb.AppendLine($"  Generated    : {s.StartedAt:dd MMMM yyyy HH:mm} UTC  |  Session: {s.SessionId}");
        sb.AppendLine(D); sb.AppendLine("  SCORE FORMULAS");
        sb.AppendLine($"  Data Score   = ({s.PassedElements} / {s.TotalElements}) * 100 = {s.ComplianceScore:F1}%");
        if (dc != null) { sb.AppendLine($"  Design Score = ({dc.PassedChecks} / {dc.TotalChecks}) * 100 = {dc.DesignComplianceScore:F1}%"); sb.AppendLine($"  Overall      = ({s.ComplianceScore:F1} + {dc.DesignComplianceScore:F1}) / 2 = {s.OverallScore:F1}%"); }
        sb.AppendLine(D); sb.AppendLine($"  Total: {s.TotalElements,6:N0}  |  Pass: {s.PassedElements,6:N0}  |  Warn: {s.WarningElements,6:N0}  |  Error: {s.ErrorElements,6:N0}  |  Critical: {s.CriticalElements,6:N0}");
        if (dc != null && dc.FailedChecks > 0)
        {
            sb.AppendLine(D); sb.AppendLine("  DESIGN CODE FAILURES");
            int n = 1;
            foreach (var d in dc.Results.Where(r => !r.Complies).OrderByDescending(r => r.Severity).Take(50))
            {
                sb.AppendLine($"  #{n++,4}  [{d.Severity,-8}]  {d.RuleId}");
                sb.AppendLine($"        {d.RuleName}  |  {d.CodeReference}");
                sb.AppendLine($"        Element : {d.ElementName} ({d.IfcClass})");
                sb.AppendLine($"        Actual  : {d.ActualDisplay}  |  Required: {d.RequiredDisplay}");
                sb.AppendLine($"        Formula : {d.Formula}");
                sb.AppendLine($"        Result  : {d.FormulaResult}");
                sb.AppendLine($"        Fix     : {d.RemediationGuidance}"); sb.AppendLine();
            }
        }
        sb.AppendLine(D); sb.AppendLine("  ALL DATA COMPLIANCE FINDINGS");
        int i = 1;
        foreach (var r in s.Results.OrderByDescending(x => x.Severity))
        {
            sb.AppendLine($"  #{i++,4}  [{r.Severity,-8}]  {r.CheckLevel}");
            sb.AppendLine($"        {r.ElementName} ({r.IfcClass})  GUID: {r.ElementGuid}");
            if (r.AffectedAgency != SgAgency.None) sb.AppendLine($"        Agency: {r.AffectedAgency}");
            sb.AppendLine($"        Issue: {r.Message}");
            if (!string.IsNullOrEmpty(r.RemediationGuidance)) sb.AppendLine($"        Fix  : {r.RemediationGuidance}");
            sb.AppendLine();
        }
        sb.AppendLine(L); sb.AppendLine($"  END OF REPORT - {app}"); sb.AppendLine(L);
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8, ct);
    }

    // ─── BCF ──────────────────────────────────────────────────────────────────

    public static async Task WriteBcfAsync(ValidationSession s, string path,
        CancellationToken ct, string company)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "VERIFIQ_BCF_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "bcf.version"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Version VersionId=\"2.1\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"/>", ct);

            // Data findings
            foreach (var r in s.Results.Where(x => x.Severity >= Severity.Error).Take(100))
            {
                var id = Guid.NewGuid().ToString();
                Directory.CreateDirectory(Path.Combine(tempDir, id));
                await File.WriteAllTextAsync(Path.Combine(tempDir, id, "markup.bcf"),
                    $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Markup><Topic Guid=\"{id}\" TopicStatus=\"Open\" TopicType=\"Issue\">" +
                    $"<Title>[{r.Severity}] {X(r.CheckLevel.ToString())}: {X(r.ElementName)}</Title>" +
                    $"<Priority>{(r.Severity == Severity.Critical ? "Critical" : "Normal")}</Priority>" +
                    $"<Description>{X(r.Message)}&#10;Fix: {X(r.RemediationGuidance)}</Description>" +
                    $"<CreationDate>{s.StartedAt:O}</CreationDate><CreationAuthor>VERIFIQ / {X(company)}</CreationAuthor></Topic>" +
                    $"<Viewpoints><ViewPoint Guid=\"{Guid.NewGuid()}\"><Components><IfcGuid>{X(r.ElementGuid)}</IfcGuid></Components></ViewPoint></Viewpoints></Markup>", ct);
            }

            // Design code failures
            if (s.DesignCode != null)
                foreach (var d in s.DesignCode.Results.Where(x => !x.Complies && x.Severity >= Severity.Error).Take(100))
                {
                    var id = Guid.NewGuid().ToString();
                    Directory.CreateDirectory(Path.Combine(tempDir, id));
                    await File.WriteAllTextAsync(Path.Combine(tempDir, id, "markup.bcf"),
                        $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<Markup><Topic Guid=\"{id}\" TopicStatus=\"Open\" TopicType=\"Issue\">" +
                        $"<Title>[DesignCode/{d.Severity}] {X(d.RuleId)}: {X(d.ElementName)}</Title>" +
                        $"<Priority>{(d.Severity == Severity.Critical ? "Critical" : "Normal")}</Priority>" +
                        $"<Description>{X(d.RuleName)}&#10;{X(d.CodeReference)}&#10;Actual: {X(d.ActualDisplay)}&#10;Required: {X(d.RequiredDisplay)}&#10;Formula: {X(d.Formula)}&#10;Result: {X(d.FormulaResult)}&#10;Fix: {X(d.RemediationGuidance)}</Description>" +
                        $"<CreationDate>{s.StartedAt:O}</CreationDate><CreationAuthor>VERIFIQ / {X(company)}</CreationAuthor></Topic>" +
                        $"<Viewpoints><ViewPoint Guid=\"{Guid.NewGuid()}\"><Components><IfcGuid>{X(d.ElementGuid)}</IfcGuid></Components></ViewPoint></Viewpoints></Markup>", ct);
                }

            if (File.Exists(path)) File.Delete(path);
            ZipFile.CreateFromDirectory(tempDir, path);
        }
        finally { if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true); }
    }

    // ─── INTERNAL STYLE / JS ─────────────────────────────────────────────────

    private static string EmbeddedCss(ReportTemplate template = ReportTemplate.Professional)
    {
        var t  = ReportTemplates.Get(template);
        var nv = t.PrimaryColour.TrimStart('#');
        var tl = t.AccentColour.TrimStart('#');
        var ff = t.FontFamily;
        return $":root{{--nv:#{nv};--tl:#{tl};--li:#F4F6FA;--bd:#E2E8F0;--rd:#B91C1C;--am:#B45309;--gn:#15803D}}" +
            $"*{{box-sizing:border-box;margin:0;padding:0}}body{{font-family:{ff};font-size:13px;color:#1a1a1a;background:var(--li)}}" +
            ".wrap{max-width:1400px;margin:0 auto;padding:20px 28px}" +
            ".card{background:#fff;border-radius:8px;border:1px solid var(--bd);padding:18px;margin-bottom:16px;box-shadow:0 1px 3px rgba(0,0,0,.06)}" +
            ".hdr{background:var(--nv);color:#fff;padding:14px 28px;display:flex;align-items:center;justify-content:space-between}" +
            ".hdr-logo{display:flex;align-items:center;gap:12px}" +
            ".logo-mark{background:var(--tl);color:#fff;width:42px;height:42px;border-radius:8px;display:flex;align-items:center;justify-content:center;font-size:17px;font-weight:900}" +
            ".logo-name{font-size:22px;font-weight:900}.logo-sub{font-size:11px;color:#93C5FD}" +
            ".hdr-right{display:flex;gap:12px;font-size:11px;color:#CBD5E1;align-items:center}" +
            ".mode-pill{padding:3px 9px;border-radius:4px;background:var(--tl);color:#fff;font-weight:700;font-size:11px}" +
            "h2{font-size:16px;font-weight:700;color:var(--nv);border-bottom:2px solid var(--tl);padding-bottom:6px;margin:0 0 12px}" +
            "h3{font-size:13px;font-weight:700;color:var(--tl);margin:16px 0 8px}" +
            ".fbox{background:#EFF6FF;border-left:4px solid var(--tl);border-radius:6px;padding:12px 16px;font-size:12px;line-height:1.7}" +
            ".kpi-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(110px,1fr));gap:10px;margin:14px 0}" +
            ".kpi{background:#fff;border:1px solid var(--bd);border-radius:8px;padding:12px;text-align:center}" +
            ".kpi-val{font-size:26px;font-weight:700;line-height:1.1}" +
            ".kpi-lbl{font-size:10px;color:#64748B;text-transform:uppercase;letter-spacing:.5px;margin-top:4px}" +
            ".kpi-val.gn{color:var(--gn)}.kpi-val.am{color:var(--am)}.kpi-val.rd{color:var(--rd)}.kpi-val.tl{color:var(--tl)}" +
            "table{width:100%;border-collapse:collapse;background:#fff;font-size:12px}" +
            "thead th{background:var(--nv);color:#fff;padding:8px 10px;text-align:left;font-size:11px;font-weight:700;white-space:nowrap}" +
            "tbody td{padding:6px 10px;border-bottom:1px solid #F1F5F9;vertical-align:top;line-height:1.4}" +
            "tbody tr:hover td{background:#F8FAFC}" +
            ".row-crit td{background:#FFF5F5}.row-err td{background:#FFFCF0}" +
            ".badge{display:inline-block;padding:2px 7px;border-radius:4px;font-size:11px;font-weight:700}" +
            ".b-crit{background:#FEF2F2;color:#B91C1C;border:1px solid #FECACA}" +
            ".b-err{background:#FFFBEB;color:#B45309;border:1px solid #FCD34D}" +
            ".b-warn{background:#FEF9C3;color:#854D0E;border:1px solid #FDE68A}" +
            ".b-pass{background:#F0FDF4;color:#15803D;border:1px solid #86EFAC}" +
            ".filter-bar{display:flex;gap:8px;margin-bottom:12px;flex-wrap:wrap;align-items:center}" +
            ".filter-bar select,.filter-bar input{padding:5px 9px;border:1px solid var(--bd);border-radius:5px;font-family:inherit;font-size:12px;background:#fff;color:#1a1a1a}" +
            ".filter-bar input{min-width:200px}" +
            ".ag-sg{background:#EFF6FF;color:#1D4ED8;border:1px solid #BFDBFE}" +
            ".ag-my{background:#FEF2F2;color:#B91C1C;border:1px solid #FECACA}" +
            ".pill{padding:2px 7px;border-radius:4px;font-size:11px;font-weight:600;display:inline-block}" +
            "footer{border-top:1px solid var(--bd);padding:12px 28px;font-size:10px;color:#94A3B8;text-align:center;margin-top:24px}" +
            "@media print{.filter-bar,.btn,.no-print{display:none!important}body{font-size:10px}thead th{font-size:9px}}" +
            "::-webkit-scrollbar{width:5px}::-webkit-scrollbar-thumb{background:#CBD5E1;border-radius:3px}";
    }


    private static string EmbeddedJs() =>
        "function fM(){var s=document.getElementById('fs').value.toLowerCase(),a=document.getElementById('fa').value.toLowerCase(),q=document.getElementById('fq').value.toLowerCase();" +
        "document.querySelectorAll('#mt tbody tr').forEach(function(r){var ok=(!s||(r.dataset.sev||'').toLowerCase()===s)&&(!a||(r.dataset.ag||'').toLowerCase()===a)&&(!q||(r.dataset.q||'').includes(q));r.style.display=ok?'':'none';});}" +
        "function fDC(){var s=document.getElementById('dc-s').value.toLowerCase(),q=document.getElementById('dc-q').value.toLowerCase();" +
        "document.querySelectorAll('#dct tbody tr').forEach(function(r){var ok=(!s||(r.dataset.sev||'').toLowerCase()===s)&&(!q||(r.dataset.q||'').includes(q));r.style.display=ok?'':'none';});}" +
        "function cF(){['fs','fa','fq'].forEach(function(id){var e=document.getElementById(id);if(e)e.value='';});document.querySelectorAll('#mt tbody tr').forEach(function(r){r.style.display='';})}";

    private static string SB(Severity s) => $"<span class=\"badge badge{s.ToString().ToLower()}\">{s}</span>";
    private static string SC(double s) => s >= 95 ? "grn" : s >= 80 ? "amb" : "red";
    private static string H(string s) => (s ?? "").Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;");
    private static string X(string s) => H(s).Replace("'","&apos;");
    private static string Q(string s) => $"\"{(s ?? "").Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", "")}\"";
    private static string ModeLabel(CountryMode m) => m switch
    {
        CountryMode.Singapore => "Singapore: CORENET-X / IFC+SG",
        CountryMode.Malaysia  => "Malaysia: NBeS / UBBL 1984",
        _                     => "Singapore + Malaysia"
    };
}
