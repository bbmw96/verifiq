// VERIFIQ - Professional Word Report Builder
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Generates a fully branded, professionally styled .docx compliance report
// with: cover page, TOC, executive summary with formulas, data compliance,
// design code compliance, per-agency breakdown, element schedule, appendices.

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Reports;

/// <summary>Local alignment enum - AlignmentType is not in DocumentFormat.OpenXml.</summary>
internal enum AlignmentType { Left, Center, Right }

public sealed class WordReportBuilder
{
    private readonly ValidationSession _s;
    private readonly string _app, _company, _founder, _website;

    // Brand colours (hex without #) - driven by selected template
    private string Navy  => ReportTemplates.Get(_template).PrimaryColour.TrimStart('#');
    private string Teal  => ReportTemplates.Get(_template).AccentColour.TrimStart('#');
    private const string White = "FFFFFF";
    private const string Light = "F4F6FA";
    private const string Grey  = "555555";
    private const string Green = "15803D";
    private const string Amber = "B45309";
    private const string Red   = "B91C1C";

    private readonly ReportTemplate _template;

    public WordReportBuilder(ValidationSession session,
        string appName, string company, string founder, string website = "bbmw0.com",
        ReportTemplate template = ReportTemplate.Professional)
    {
        _s = session; _app = appName; _company = company;
        _founder = founder; _website = website; _template = template;
    }

    public async Task BuildAsync(string path, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var main = doc.AddMainDocumentPart();
            AddStyles(main);
            AddNumbering(main);

            var body = new Body();
            main.Document = new Document(body);

            // ── 1. Cover Page ──────────────────────────────────────────────
            BuildCoverPage(body);
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // ── 2. Disclaimer ──────────────────────────────────────────────
            BuildDisclaimer(body);
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // ── 3. Executive Summary ───────────────────────────────────────
            BuildExecutiveSummary(body);
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // ── 4. Formulas and Methodology ───────────────────────────────
            BuildFormulaSection(body);
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // ── 5. Data Compliance Results ────────────────────────────────
            BuildDataComplianceSection(body);
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // ── 6. Design Code Compliance ─────────────────────────────────
            if (_s.DesignCode != null)
            {
                BuildDesignCodeSection(body, _s.DesignCode);
                body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
            }

            // ── 7. Singapore Agency Breakdown ─────────────────────────────
            if (_s.CountryMode is CountryMode.Singapore or CountryMode.Combined)
                BuildAgencySection(body);

            // ── 8. Critical Issues Schedule ───────────────────────────────
            BuildCriticalSchedule(body);
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // ── 9. All Findings ────────────────────────────────────────────
            BuildAllFindingsSection(body);
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));

            // ── 10. Appendix A - Code References ──────────────────────────
            BuildCodeReferences(body);

            // Footer
            AddHeaderFooter(main);

            main.Document.Save();
        }, ct);
    }

    // ─── COVER PAGE ───────────────────────────────────────────────────────────

    private void BuildCoverPage(Body body)
    {
        // Navy banner
        body.AppendChild(HeadingParagraph("VERIFIQ", 48, Navy, AlignmentType.Center, bold: true));
        body.AppendChild(HeadingParagraph("IFC Compliance Checker", 24, Teal, AlignmentType.Center));
        body.AppendChild(Spacer());

        // Country mode badge
        var modeColour = _s.CountryMode switch
        {
            CountryMode.Singapore => "1D4ED8",
            CountryMode.Malaysia  => "9B1C1C",
            _                     => Teal
        };
        var modeText = _s.CountryMode switch
        {
            CountryMode.Singapore => "Singapore: CORENET-X / IFC+SG",
            CountryMode.Malaysia  => "Malaysia: NBeS / UBBL 1984",
            _                     => "Singapore + Malaysia: Combined"
        };
        body.AppendChild(HeadingParagraph(modeText, 16, modeColour, AlignmentType.Center, bold: true));
        body.AppendChild(Spacer());

        // Title
        body.AppendChild(HeadingParagraph("IFC COMPLIANCE REPORT", 36, Navy,
            AlignmentType.Center, bold: true));
        body.AppendChild(Spacer());

        // Metadata table
        body.AppendChild(BuildCoverTable());
        body.AppendChild(Spacer());

        // Compliance score highlight
        string scoreColour = _s.ComplianceScore >= 95 ? Green
                           : _s.ComplianceScore >= 80 ? Amber : Red;

        body.AppendChild(HeadingParagraph("COMPLIANCE SCORE", 14, Grey,
            AlignmentType.Center, bold: true, spacing: 0));
        body.AppendChild(HeadingParagraph($"{_s.ComplianceScore:F1}%", 56,
            scoreColour, AlignmentType.Center, bold: true));
        body.AppendChild(Spacer());

        body.AppendChild(SeparatorLine(Navy));
        body.AppendChild(HeadingParagraph(
            $"{_company}  |  Developed by {_founder}  |  {_website}",
            10, Grey, AlignmentType.Center));
    }

    private Table BuildCoverTable()
    {
        var rows = new[]
        {
            ("Report Reference",   $"VERIFIQ-{_s.SessionId.ToString()[..8].ToUpperInvariant()}"),
            ("Report Date",        _s.StartedAt.ToString("dd MMMM yyyy")),
            ("Time",               _s.StartedAt.ToString("HH:mm:ss") + " UTC"),
            ("Country Mode",       _s.CountryMode.ToString()),
            ("Files Validated",    $"{_s.LoadedFiles.Count}"),
            ("Total Elements",     $"{_s.TotalElements:N0}"),
            ("Validation Duration",$"{_s.Duration.TotalSeconds:F1} seconds"),
            ("Generated By",       $"{_app} v1.0  |  {_company}"),
            ("Developed By",       _founder),
        };

        var tbl = CreateStyledTable(new[] { 3600, 5760 }, 100, Light, rows);
        return tbl;
    }

    // ─── DISCLAIMER ───────────────────────────────────────────────────────────

    private void BuildDisclaimer(Body body)
    {
        body.AppendChild(Heading1("Important Notice"));
        body.AppendChild(Para(
            "This report has been generated by VERIFIQ IFC Compliance Checker, " +
            "developed by Jia Wen Gan of BBMW0 Technologies. This report is provided " +
            "for quality assurance purposes only and does not constitute professional advice, " +
            "a regulatory approval, or a guarantee that the building project will be approved " +
            "by any government agency.", italic: true));
        body.AppendChild(Spacer());
        body.AppendChild(Para(
            "A passing VERIFIQ result means that the IFC model DATA complies with the " +
            "required property set structure and contains the required parameter values " +
            "as defined in the IFC+SG Industry Mapping (Singapore) or NBeS mapping (Malaysia). " +
            "It does not mean that the DESIGN itself is approved. The Qualified Person (QP) " +
            "remains responsible for ensuring full code compliance.", italic: true));
        body.AppendChild(Spacer());
        body.AppendChild(Para(
            "Design code checks included in this report check actual dimension and area values " +
            "extracted from the IFC model properties against published regulatory parameters. " +
            "These checks are indicative and should be reviewed by the QP before submission.", italic: true));
    }

    // ─── EXECUTIVE SUMMARY ────────────────────────────────────────────────────

    private void BuildExecutiveSummary(Body body)
    {
        body.AppendChild(Heading1("Executive Summary"));

        var dc = _s.DesignCode;
        var overallScore = dc != null
            ? $"{_s.OverallScore:F1}%  (Data: {_s.ComplianceScore:F1}%  +  Design: {dc.DesignComplianceScore:F1}%  ÷  2)"
            : $"{_s.ComplianceScore:F1}%";

        var summaryRows = new[]
        {
            ("Metric", "Value", "Description"),
            ("Total IFC Elements",    $"{_s.TotalElements:N0}",  "Physical elements parsed from loaded IFC files"),
            ("Fully Compliant",       $"{_s.PassedElements:N0}",  "Elements passing all 20 data compliance checks"),
            ("Warnings",              $"{_s.WarningElements:N0}", "Elements with non-critical data issues"),
            ("Errors",                $"{_s.ErrorElements:N0}",   "Elements with likely submission-rejection issues"),
            ("Critical Failures",     $"{_s.CriticalElements:N0}","Elements that will definitely cause rejection"),
            ("Proxy Elements",        $"{_s.ProxyElements:N0}",   "IfcBuildingElementProxy - unclassified elements (Critical)"),
        };

        if (dc != null)
        {
            var dsRows = new[]
            {
                ("Design Checks Run",    $"{dc.TotalChecks:N0}",  "Total design code parameter checks performed"),
                ("Design Checks Pass",   $"{dc.PassedChecks:N0}", "Design checks meeting code requirements"),
                ("Design Checks Fail",   $"{dc.FailedChecks:N0}", "Design checks below code minimums or above maximums"),
                ("Design Critical",      $"{dc.CriticalChecks:N0}","Design code failures rated Critical"),
            };
        }

        var allRows = summaryRows.Skip(1).ToArray();
        body.AppendChild(BuildThreeColumnTable(
            new[] { "Metric", "Value", "Description" },
            allRows, new[] { 2800, 1200, 5360 }));

        body.AppendChild(Spacer());

        // Score formula
        body.AppendChild(Heading2("Compliance Score Formula"));
        body.AppendChild(Para(
            "Data Compliance Score = (Compliant Elements ÷ Total Elements) × 100", bold: true));
        body.AppendChild(Para(
            $"= ({_s.PassedElements:N0} ÷ {_s.TotalElements:N0}) × 100 = {_s.ComplianceScore:F1}%"));

        if (dc != null)
        {
            body.AppendChild(Para(
                "Design Compliance Score = (Passed Design Checks ÷ Total Design Checks) × 100",
                bold: true));
            body.AppendChild(Para(
                $"= ({dc.PassedChecks:N0} ÷ {dc.TotalChecks:N0}) × 100 = {dc.DesignComplianceScore:F1}%"));
            body.AppendChild(Para(
                "Overall Score = (Data Score + Design Score) ÷ 2", bold: true));
            body.AppendChild(Para(
                $"= ({_s.ComplianceScore:F1} + {dc.DesignComplianceScore:F1}) ÷ 2 = {_s.OverallScore:F1}%"));
        }
    }

    // ─── FORMULA SECTION ──────────────────────────────────────────────────────

    private void BuildFormulaSection(Body body)
    {
        body.AppendChild(Heading1("Calculation Formulas and Methodology"));

        body.AppendChild(Heading2("Compliance Score Calculation"));
        body.AppendChild(BuildFormulaTable(new[]
        {
            ("Formula",      "Data Compliance Score = (N_pass ÷ N_total) × 100"),
            ("Where",        "N_pass = number of elements passing all 20 check levels"),
            ("",             "N_total = total physical elements in the IFC model"),
            ("Result",       $"{_s.PassedElements} ÷ {_s.TotalElements} × 100 = {_s.ComplianceScore:F1}%"),
            ("Interpretation",
                _s.ComplianceScore >= 95 ? "GREEN: Excellent. Model is submission-ready."
                : _s.ComplianceScore >= 80 ? "AMBER: Review errors before submission."
                : "RED: Significant issues. Do not submit until resolved."),
        }));

        body.AppendChild(Spacer());
        body.AppendChild(Heading2("Error Rate by Check Level"));
        body.AppendChild(Para(
            "The check-level error rate indicates which IFC+SG compliance areas have the most failures. " +
            "Formula: Check Error Rate = (Failures at Level N ÷ Total Elements) × 100"));

        if (_s.ErrorsByCheckLevel.Any())
        {
            var rows = _s.ErrorsByCheckLevel
                .OrderByDescending(k => k.Value)
                .Select(k => (
                    k.Key.ToString(),
                    k.Value.ToString(),
                    $"{(double)k.Value / _s.TotalElements * 100:F1}%",
                    "Error rate = failures ÷ total × 100"))
                .ToArray();

            body.AppendChild(BuildFourColumnTable(
                new[] { "Check Level", "Failures", "Error Rate", "Formula" },
                rows, new[] { 2400, 1200, 1200, 4560 }));
        }

        if (_s.DesignCode != null)
        {
            body.AppendChild(Spacer());
            body.AppendChild(Heading2("Design Code Check Formulas"));
            body.AppendChild(Para(
                "Each design code check applies one of the following formula types:"));

            body.AppendChild(BuildFormulaTable(new[]
            {
                ("Minimum Value Check",  "Actual Value ≥ Minimum Required  →  PASS / FAIL"),
                ("Maximum Value Check",  "Actual Value ≤ Maximum Permitted  →  PASS / FAIL"),
                ("Range Check",          "Min ≤ Actual Value ≤ Max  →  PASS / FAIL"),
                ("Ratio Check",          "Part Area ÷ Total Area × 100 ≤ Max %  →  PASS / FAIL"),
                ("Window-Area Ratio",    "Window Area ÷ Floor Area × 100 ≥ Min %  →  PASS / FAIL"),
                ("Ventilation Ratio",    "Vent Opening Area ÷ Floor Area × 100 ≥ Min %  →  PASS / FAIL"),
                ("Fire Rating Parse",    "Parsed FRR(minutes) from notation R/E/I or FDnn  →  ≥ Min minutes"),
                ("Slope/Gradient",       "1:N where N = 1 ÷ tan(θ°); gradient % = (1 ÷ N) × 100"),
            }));

            // Sample design check formulas
            body.AppendChild(Spacer());
            body.AppendChild(Heading2("Sample Design Code Calculations"));

            var sampleFails = _s.DesignCode.Results
                .Where(r => !r.Complies && !string.IsNullOrEmpty(r.FormulaResult))
                .Take(12)
                .ToList();

            if (sampleFails.Any())
            {
                var rows = sampleFails.Select(r => (
                    r.RuleId,
                    $"{r.ElementName} ({r.IfcClass})",
                    r.Formula,
                    r.FormulaResult
                )).ToArray();

                body.AppendChild(BuildFourColumnTable(
                    new[] { "Rule ID", "Element", "Formula", "Result" },
                    rows, new[] { 1400, 2400, 3200, 2360 }));
            }
        }
    }

    // ─── DATA COMPLIANCE SECTION ──────────────────────────────────────────────

    private void BuildDataComplianceSection(Body body)
    {
        body.AppendChild(Heading1("Data Compliance: 20 Check Levels"));
        body.AppendChild(Para(
            "The following table shows the number of failures at each of the 20 IFC data " +
            "compliance check levels. Each element is checked against all applicable levels."));

        body.AppendChild(Spacer());

        var checkLevelDescriptions = new Dictionary<CheckLevel, string>
        {
            { CheckLevel.IfcEntityClass,           "Is element a specific IFC class (not IfcBuildingElementProxy)?" },
            { CheckLevel.PredefinedType,            "Is PredefinedType specific and not NOTDEFINED?" },
            { CheckLevel.ObjectTypeUserDefined,     "When USERDEFINED, is ObjectType populated?" },
            { CheckLevel.ClassificationReference,   "Is IfcClassificationReference present and populated?" },
            { CheckLevel.ClassificationEdition,     "Is classification referencing the current edition?" },
            { CheckLevel.MandatoryPropertySets,     "Are all required Pset_ property sets present?" },
            { CheckLevel.SgPropertySets,            "Are all required SGPset_ property sets present? (SG only)" },
            { CheckLevel.PropertyValuesPopulated,   "Are all mandatory property values filled?" },
            { CheckLevel.PropertyValueDataType,     "Do values match required data types?" },
            { CheckLevel.PropertyValueEnumeration,  "Do enumerated values match the permitted list?" },
            { CheckLevel.SpatialContainment,        "Is every element contained within a storey?" },
            { CheckLevel.StoreyElevation,           "Are storey elevations consistent?" },
            { CheckLevel.Georeferencing,            "Is IfcMapConversion present and correct?" },
            { CheckLevel.SiteAndBuildingHierarchy,  "Is IfcSite/Building/Storey hierarchy correct?" },
            { CheckLevel.GuidUniqueness,            "Is every GlobalId unique?" },
            { CheckLevel.MaterialAssignment,        "Are structural/fire elements assigned materials?" },
            { CheckLevel.SpaceBoundaryIntegrity,    "Do spaces have Category and area?" },
            { CheckLevel.GeometryValidity,          "Is element geometry non-degenerate?" },
            { CheckLevel.IfcSchemaVersion,          "Is the correct IFC schema version used?" },
            { CheckLevel.FileHeaderCompleteness,    "Is the file header complete?" },
        };

        var rows = new List<(string, string, string, string)>();
        int level = 1;
        foreach (var kvp in checkLevelDescriptions)
        {
            _s.ErrorsByCheckLevel.TryGetValue(kvp.Key, out int failures);
            double rate = _s.TotalElements > 0
                ? (double)failures / _s.TotalElements * 100 : 0;
            rows.Add((
                $"Level {level:D2}: {kvp.Key}",
                failures.ToString(),
                $"{rate:F1}%",
                kvp.Value
            ));
            level++;
        }

        body.AppendChild(BuildFourColumnTable(
            new[] { "Check Level", "Failures", "Error Rate", "What Is Checked" },
            rows.ToArray(), new[] { 2600, 900, 900, 4960 }));
    }

    // ─── DESIGN CODE SECTION ──────────────────────────────────────────────────

    private void BuildDesignCodeSection(Body body, DesignCodeSession dc)
    {
        body.AppendChild(Heading1("Design Code Compliance"));
        body.AppendChild(Para(
            "This section reports the results of checking actual design values " +
            "(dimensions, areas, distances, ratios) against published regulatory requirements. " +
            "These checks go beyond data presence to verify design intent compliance."));
        body.AppendChild(Spacer());

        // Summary
        body.AppendChild(Heading2("Design Code Summary"));
        body.AppendChild(BuildThreeColumnTable(
            new[] { "Metric", "Value", "Formula" },
            new[]
            {
                ("Total Design Checks",   $"{dc.TotalChecks:N0}",          "All design code rules applied to all elements"),
                ("Passed",                $"{dc.PassedChecks:N0}",         $"= {dc.PassedChecks} ÷ {dc.TotalChecks} × 100 = {dc.DesignComplianceScore:F1}%"),
                ("Failed",                $"{dc.FailedChecks:N0}",         $"= {dc.FailedChecks} ÷ {dc.TotalChecks} × 100 = {100 - dc.DesignComplianceScore:F1}%"),
                ("Critical Failures",     $"{dc.CriticalChecks:N0}",       "Must fix before submission"),
                ("Design Score",          $"{dc.DesignComplianceScore:F1}%","= Passed ÷ Total × 100"),
            }, new[] { 2800, 1200, 5360 }));

        body.AppendChild(Spacer());

        // By category
        if (dc.FailuresByCategory.Any())
        {
            body.AppendChild(Heading2("Failures by Design Code Category"));
            var catRows = dc.FailuresByCategory
                .OrderByDescending(k => k.Value)
                .Select(k => (k.Key.ToString().Replace("AndUniversalDesign","").Replace("And","/ "), k.Value.ToString()))
                .ToArray();

            body.AppendChild(BuildTwoColumnTable(
                new[] { "Category", "Failure Count" },
                catRows, new[] { 7200, 2160 }));
        }

        body.AppendChild(Spacer());

        // Design code failures
        body.AppendChild(Heading2("Design Code Failures: Full List"));

        var failures = dc.Results
            .Where(r => !r.Complies)
            .OrderByDescending(r => r.Severity)
            .ThenBy(r => r.RuleId)
            .ToList();

        if (failures.Count == 0)
        {
            body.AppendChild(Para("No design code failures detected.", bold: true));
            return;
        }

        // Group by code reference
        var byCode = failures.GroupBy(f => f.CodeReference).ToList();
        foreach (var group in byCode.Take(20))
        {
            body.AppendChild(Heading3($"[{group.First().AffectedAgency}]  {group.Key}"));
            body.AppendChild(Para(group.First().RegulationText, italic: true));
            body.AppendChild(Spacer(120));

            var rows = group.Select(r => (
                r.RuleId,
                $"{r.ElementName}  ({r.IfcClass})",
                r.ActualDisplay,
                r.RequiredDisplay,
                r.FormulaResult
            )).ToArray();

            body.AppendChild(BuildFiveColumnTable(
                new[] { "Rule ID", "Element", "Actual", "Required", "Formula Result" },
                rows, new[] { 1200, 2800, 1400, 1600, 2360 }));

            body.AppendChild(Spacer());
        }
    }

    // ─── AGENCY SECTION ───────────────────────────────────────────────────────

    private void BuildAgencySection(Body body)
    {
        body.AppendChild(Heading1("Singapore: Errors by Regulatory Agency"));
        body.AppendChild(Para(
            "Singapore CORENET-X submissions are reviewed by 8 agencies. " +
            "The following table shows how errors are distributed across agencies, " +
            "which determines which submission gateway is most at risk of rejection."));
        body.AppendChild(Spacer());

        var sgAgencies = new[] { SgAgency.BCA, SgAgency.URA, SgAgency.SCDF,
                                  SgAgency.LTA, SgAgency.NEA, SgAgency.NParks,
                                  SgAgency.PUB, SgAgency.SLA };

        var agencyDesc = new Dictionary<SgAgency, string>
        {
            { SgAgency.BCA,    "Building and Construction Authority - Building control, accessibility, structural" },
            { SgAgency.URA,    "Urban Redevelopment Authority - Planning, GFA, land use, room sizes" },
            { SgAgency.SCDF,   "Singapore Civil Defence Force - Fire safety, escape routes, compartmentation" },
            { SgAgency.LTA,    "Land Transport Authority - Roads, parking, transport access" },
            { SgAgency.NEA,    "National Environment Agency - Environmental health, ventilation, drainage" },
            { SgAgency.NParks, "National Parks Board - Trees, greenery, parks" },
            { SgAgency.PUB,    "Public Utilities Board - Water supply, drainage, sewerage" },
            { SgAgency.SLA,    "Singapore Land Authority - Cadastral, georeferencing, SVY21" },
        };

        var rows = sgAgencies.Select(a =>
        {
            _s.ErrorsByAgency.TryGetValue(a, out int count);
            return (a.ToString(), agencyDesc.GetValueOrDefault(a, ""), count.ToString());
        }).ToArray();

        body.AppendChild(BuildThreeColumnTable(
            new[] { "Agency", "Responsibility", "Error Count" },
            rows, new[] { 1200, 6800, 1360 }));
    }

    // ─── CRITICAL SCHEDULE ────────────────────────────────────────────────────

    private void BuildCriticalSchedule(Body body)
    {
        body.AppendChild(Heading1("Critical Issues: Must Fix Before Submission"));

        var criticals = _s.Results.Where(r => r.Severity == Severity.Critical).ToList();
        body.AppendChild(Para(
            $"{criticals.Count} critical issues found. These must all be resolved before " +
            "submitting to CORENET-X or NBeS. Each critical issue will result in automatic rejection."));
        body.AppendChild(Spacer());

        if (!criticals.Any())
        {
            body.AppendChild(Para("✓  No critical issues found.", bold: true));
            return;
        }

        foreach (var r in criticals.Take(100))
        {
            body.AppendChild(BuildFindingBlock(r));
        }
    }

    // ─── ALL FINDINGS ─────────────────────────────────────────────────────────

    private void BuildAllFindingsSection(Body body)
    {
        body.AppendChild(Heading1("All Compliance Findings"));

        var findings = _s.Results.OrderByDescending(r => r.Severity).ToList();
        body.AppendChild(Para($"Total: {findings.Count} findings across {_s.TotalElements:N0} elements."));
        body.AppendChild(Spacer());

        var rows = findings.Take(2000).Select(r => (
            r.Severity.ToString(),
            r.CheckLevel.ToString(),
            $"{r.ElementName}\n{r.IfcClass}",
            r.ElementGuid.Length > 12 ? r.ElementGuid[..12] + "…" : r.ElementGuid,
            r.AffectedAgency != SgAgency.None ? r.AffectedAgency.ToString() : "-",
            string.IsNullOrEmpty(r.PropertySetName) ? "-" : $"{r.PropertySetName}\n.{r.PropertyName}",
            r.Message.Length > 120 ? r.Message[..117] + "…" : r.Message
        )).ToArray();

        body.AppendChild(BuildSevenColumnTable(
            new[] { "Severity", "Level", "Element", "GUID", "Agency", "Property", "Issue" },
            rows, new[] { 900, 1400, 2000, 1600, 800, 1600, 3060 }));
    }

    // ─── CODE REFERENCES APPENDIX ─────────────────────────────────────────────

    private void BuildCodeReferences(Body body)
    {
        body.AppendChild(Heading1("Appendix A: Regulatory Code References"));

        if (_s.CountryMode is CountryMode.Singapore or CountryMode.Combined)
        {
            body.AppendChild(Heading2("Singapore Regulations"));
            var sgRefs = new[]
            {
                ("BCA Building Control Act",          "Building Control Act (Cap 29, Rev 2000); Building Control Regulations 2003"),
                ("CORENET-X COP",                     "Code of Practice for BIM e-Submission (3rd Edition, September 2025)"),
                ("IFC+SG Industry Mapping",           "IFC+SG Industry Mapping 2025 (BCA/GovTech)"),
                ("BCA Code on Accessibility 2025",    "Code on Accessibility in the Built Environment 2025 (BCA)"),
                ("SCDF Fire Code",                    "Singapore Fire Code 2018 (2023 Edition) - SCDF"),
                ("BCA Green Mark 2021",               "BCA Green Mark for Residences 2021 / Green Mark for Non-Residences 2021"),
                ("BCA ETTV/RETV Code",                "Code on Envelope Thermal Performance for Buildings 2008 (Rev 2021)"),
                ("LTA Parking Code",                  "LTA Code of Practice for Vehicle Parking Provision in Development Proposals"),
                ("NEA Environmental Health Regs",     "Environmental Public Health Act / Third Schedule (Licensing)"),
                ("PUB Sewerage Regs",                 "Sewerage and Drainage Act; SS 636:2018 Code of Practice for Water Services"),
                ("URA Planning Handbook",             "URA Handbook on Singapore's Planning Parameters 2023 Edition"),
                ("URA DC Handbook: Landed Housing",  "URA Development Control Handbook  -  Landed Housing (Rev 2023)"),
                ("URA Balcony Circular",              "URA Circular - Guidelines on Balconies in New Private Residential Developments (Nov 2019)"),
                ("SVY21",                             "Singapore SVY21 Coordinate Reference System (EPSG:3414) - SLA"),
            };
            body.AppendChild(BuildTwoColumnTable(
                new[] { "Reference", "Full Citation" },
                sgRefs, new[] { 3200, 6160 }));
        }

        if (_s.CountryMode is CountryMode.Malaysia or CountryMode.Combined)
        {
            body.AppendChild(Spacer());
            body.AppendChild(Heading2("Malaysia Regulations"));
            var myRefs = new[]
            {
                ("UBBL 1984",      "Uniform Building By-Laws 1984 (G.N. 2571/1985 and all subsequent amendments) under the Street, Drainage and Building Act 1974 (Act 133)"),
                ("Act 133",        "Street, Drainage and Building Act 1974 (Act 133) as amended by Act A1286/2007"),
                ("MS 1184:2014",   "MS 1184:2014 Code of Practice on Access for Disabled Persons to Public Buildings (SIRIM)"),
                ("JBPM Fire Code", "Garis Panduan Persyaratan Keselamatan Kebakaran 2020 (JBPM - Jabatan Bomba dan Penyelamat Malaysia)"),
                ("NBeS",          "National BIM e-Submission (NBeS) - CIDB Malaysia; IFC mapping documentation 2024"),
                ("GBI Malaysia",  "Green Building Index (GBI) Non-Residential New Construction V1.0; GBI Residential New Construction V1.0"),
                ("MS EN 1992",    "MS EN 1992-1-1:2010 Eurocode 2 - Design of Concrete Structures (adopted from BS EN 1992)"),
                ("GDM2000",       "Malaysia Geodetic Datum 2000 (GDM2000) - DSMM Malaysia"),
            };
            body.AppendChild(BuildTwoColumnTable(
                new[] { "Reference", "Full Citation" },
                myRefs, new[] { 2000, 7360 }));
        }
    }

    // ─── TABLE BUILDERS ───────────────────────────────────────────────────────

    private Table CreateStyledTable(int[] widths, int spacing,
        string headerBg, (string k, string v)[] rows)
    {
        var tbl = new Table();
        tbl.PrependChild(TableBorderProperties());

        foreach (var (k, v) in rows)
        {
            var row = new TableRow();
            row.AppendChild(DataCell(k, widths[0], Light, bold: true));
            row.AppendChild(DataCell(v, widths[1], White));
            tbl.AppendChild(row);
        }
        return tbl;
    }

    private Table BuildTwoColumnTable(string[] headers, (string, string)[] rows, int[] widths)
    {
        var tbl = new Table();
        tbl.PrependChild(TableBorderProperties());
        tbl.AppendChild(HeaderRow(headers, widths));
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new TableRow();
            row.AppendChild(DataCell(rows[i].Item1, widths[0], i % 2 == 0 ? White : Light));
            row.AppendChild(DataCell(rows[i].Item2, widths[1], i % 2 == 0 ? White : Light));
            tbl.AppendChild(row);
        }
        return tbl;
    }

    private Table BuildThreeColumnTable(string[] headers, (string, string, string)[] rows, int[] widths)
    {
        var tbl = new Table();
        tbl.PrependChild(TableBorderProperties());
        tbl.AppendChild(HeaderRow(headers, widths));
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new TableRow();
            string bg = i % 2 == 0 ? White : Light;
            row.AppendChild(DataCell(rows[i].Item1, widths[0], bg, bold: true));
            row.AppendChild(DataCell(rows[i].Item2, widths[1], bg));
            row.AppendChild(DataCell(rows[i].Item3, widths[2], bg));
            tbl.AppendChild(row);
        }
        return tbl;
    }

    private Table BuildFourColumnTable(string[] headers,
        (string, string, string, string)[] rows, int[] widths)
    {
        var tbl = new Table();
        tbl.PrependChild(TableBorderProperties());
        tbl.AppendChild(HeaderRow(headers, widths));
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new TableRow();
            string bg = i % 2 == 0 ? White : Light;
            row.AppendChild(DataCell(rows[i].Item1, widths[0], bg));
            row.AppendChild(DataCell(rows[i].Item2, widths[1], bg));
            row.AppendChild(DataCell(rows[i].Item3, widths[2], bg));
            row.AppendChild(DataCell(rows[i].Item4, widths[3], bg));
            tbl.AppendChild(row);
        }
        return tbl;
    }

    private Table BuildFiveColumnTable(string[] headers,
        (string, string, string, string, string)[] rows, int[] widths)
    {
        var tbl = new Table();
        tbl.PrependChild(TableBorderProperties());
        tbl.AppendChild(HeaderRow(headers, widths));
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new TableRow();
            string bg = i % 2 == 0 ? White : Light;
            row.AppendChild(DataCell(rows[i].Item1, widths[0], bg));
            row.AppendChild(DataCell(rows[i].Item2, widths[1], bg));
            row.AppendChild(DataCell(rows[i].Item3, widths[2], bg));
            row.AppendChild(DataCell(rows[i].Item4, widths[3], bg));
            row.AppendChild(DataCell(rows[i].Item5, widths[4], bg));
            tbl.AppendChild(row);
        }
        return tbl;
    }

    private Table BuildSevenColumnTable(string[] headers,
        (string,string,string,string,string,string,string)[] rows, int[] widths)
    {
        var tbl = new Table();
        tbl.PrependChild(TableBorderProperties());
        tbl.AppendChild(HeaderRow(headers, widths));
        for (int i = 0; i < rows.Length; i++)
        {
            string bg = rows[i].Item1 == "Critical" ? "FEF2F2"
                      : rows[i].Item1 == "Error"    ? "FFFBEB"
                      : i % 2 == 0 ? White : Light;
            var row = new TableRow();
            row.AppendChild(DataCell(rows[i].Item1, widths[0], bg));
            row.AppendChild(DataCell(rows[i].Item2, widths[1], bg));
            row.AppendChild(DataCell(rows[i].Item3, widths[2], bg));
            row.AppendChild(DataCell(rows[i].Item4, widths[3], bg));
            row.AppendChild(DataCell(rows[i].Item5, widths[4], bg));
            row.AppendChild(DataCell(rows[i].Item6, widths[5], bg));
            row.AppendChild(DataCell(rows[i].Item7, widths[6], bg));
            tbl.AppendChild(row);
        }
        return tbl;
    }

    private Table BuildFormulaTable((string, string)[] rows)
    {
        var tbl = new Table();
        tbl.PrependChild(TableBorderProperties());
        foreach (var (label, formula) in rows)
        {
            var row = new TableRow();
            row.AppendChild(DataCell(label, 1800, Light, bold: true));
            row.AppendChild(DataCell(formula, 7560, "F0F9FF", mono: true));
            tbl.AppendChild(row);
        }
        return tbl;
    }

    private Paragraph BuildFindingBlock(ValidationResult r)
    {
        // In the full build each finding would be a mini-table
        // For now a structured paragraph
        var para = new Paragraph();
        var props = new ParagraphProperties(
            new SpacingBetweenLines { Before = "80", After = "40" });
        para.PrependChild(props);
        para.AppendChild(new Run(
            new RunProperties(new Bold(), new Color { Val = Red }),
            new Text($"[{r.Severity}]  {r.CheckLevel}: {r.ElementName}  ({r.IfcClass})")));
        return para;
    }

    // ─── BUILDING BLOCKS ──────────────────────────────────────────────────────

    private TableRow HeaderRow(string[] headers, int[] widths)
    {
        var row = new TableRow { TableRowProperties = new TableRowProperties(new TableHeader()) };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = widths[i].ToString() },
                    new Shading { Fill = Navy, Val = ShadingPatternValues.Clear }),
                new Paragraph(new ParagraphProperties(
                    new SpacingBetweenLines { Before = "80", After = "80" }),
                    new Run(
                        new RunProperties(new Bold(), new Color { Val = White },
                            new FontSize { Val = "18" }),
                        new Text(headers[i]))));
            row.AppendChild(cell);
        }
        return row;
    }

    private TableCell DataCell(string text, int width, string bg,
        bool bold = false, bool italic = false, bool mono = false)
    {
        var runProps = new RunProperties();
        if (bold)   runProps.AppendChild(new Bold());
        if (italic) runProps.AppendChild(new Italic());
        if (mono)   runProps.AppendChild(new RunFonts { Ascii = "Courier New", HighAnsi = "Courier New" });
        runProps.AppendChild(new FontSize { Val = "18" });

        return new TableCell(
            new TableCellProperties(
                new TableCellWidth { Type = TableWidthUnitValues.Dxa, Width = width.ToString() },
                new Shading { Fill = bg, Val = ShadingPatternValues.Clear },
                new TableCellMargin(
                    new TopMargin    { Width = "60",  Type = TableWidthUnitValues.Dxa },
                    new BottomMargin { Width = "60",  Type = TableWidthUnitValues.Dxa },
                    new LeftMargin   { Width = "120", Type = TableWidthUnitValues.Dxa },
                    new RightMargin  { Width = "80",  Type = TableWidthUnitValues.Dxa })),
            new Paragraph(
                new ParagraphProperties(
                    new SpacingBetweenLines { Before = "40", After = "40" }),
                new Run(runProps, new Text(text ?? string.Empty)
                {
                    Space = SpaceProcessingModeValues.Preserve
                })));
    }

    private TableProperties TableBorderProperties() =>
        new(new TableBorders(
            new TopBorder    { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new LeftBorder   { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new RightBorder  { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" },
            new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4, Color = "CCCCCC" }));

    // ─── PARAGRAPH HELPERS ────────────────────────────────────────────────────

    private Paragraph HeadingParagraph(string text, int fontSize, string colour,
        AlignmentType align, bool bold = false, int spacing = 200)
    {
            var runProps = new RunProperties(
                new Color { Val = colour },
                new FontSize { Val = (fontSize * 2).ToString() },
                new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
            if (bold) runProps.PrependChild(new Bold());
            return new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = align == AlignmentType.Center
                        ? JustificationValues.Center
                        : align == AlignmentType.Right
                            ? JustificationValues.Right
                            : JustificationValues.Left },
                    new SpacingBetweenLines { Before = spacing.ToString(), After = "80" }),
                new Run(runProps, new Text(text)));
    }

    private Paragraph Heading1(string text) => HeadingParagraph(text, 18, Navy, AlignmentType.Left, bold: true, spacing: 300);
    private Paragraph Heading2(string text) => HeadingParagraph(text, 14, Teal, AlignmentType.Left, bold: true, spacing: 240);
    private Paragraph Heading3(string text) => HeadingParagraph(text, 12, Navy, AlignmentType.Left, bold: true, spacing: 160);

    private Paragraph Para(string text, bool bold = false, bool italic = false)
    {
        var runProps = new RunProperties(new FontSize { Val = "20" });
        if (bold)   runProps.AppendChild(new Bold());
        if (italic) runProps.AppendChild(new Italic());
        runProps.AppendChild(new RunFonts { Ascii = "Arial", HighAnsi = "Arial" });
        return new Paragraph(
            new ParagraphProperties(
                new SpacingBetweenLines { Before = "60", After = "80", Line = "300", LineRule = LineSpacingRuleValues.Auto }),
            new Run(runProps, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }

    private Paragraph Spacer(int before = 160) =>
        new(new ParagraphProperties(new SpacingBetweenLines { Before = before.ToString(), After = "0" }),
            new Run(new Text("")));

    private Paragraph SeparatorLine(string colour) =>
        new(new ParagraphProperties(
            new ParagraphBorders(new BottomBorder
                { Val = BorderValues.Single, Size = 8, Color = colour, Space = 6 }),
            new SpacingBetweenLines { Before = "160", After = "160" }));

    // ─── HEADER / FOOTER ─────────────────────────────────────────────────────

    private void AddHeaderFooter(MainDocumentPart main)
    {
        var headerPart = main.AddNewPart<HeaderPart>();
        headerPart.Header = new Header(
            new Paragraph(
                new ParagraphProperties(
                    new ParagraphBorders(new BottomBorder
                        { Val = BorderValues.Single, Size = 6, Color = Teal, Space = 6 })),
                new Run(
                    new RunProperties(new Bold(), new Color { Val = Navy }),
                    new Text("VERIFIQ: IFC Compliance Report")),
                new Run(new Text("   |   " + _s.StartedAt.ToString("dd MMM yyyy")))));

        var footerPart = main.AddNewPart<FooterPart>();
        footerPart.Footer = new Footer(
            new Paragraph(
                new ParagraphProperties(
                    new ParagraphBorders(new TopBorder
                        { Val = BorderValues.Single, Size = 6, Color = Teal, Space = 6 })),
                new Run(
                    new RunProperties(new Color { Val = Grey }),
                    new Text($"{_company}  |  Developed by {_founder}  |  {_website}  |  Page "))));

        var docProps = main.Document.Body!
            .ChildElements.OfType<SectionProperties>().FirstOrDefault()
            ?? new SectionProperties();

        docProps.PrependChild(new HeaderReference
        {
            Type = HeaderFooterValues.Default,
            Id   = main.GetIdOfPart(headerPart)
        });
        docProps.PrependChild(new FooterReference
        {
            Type = HeaderFooterValues.Default,
            Id   = main.GetIdOfPart(footerPart)
        });

        if (!main.Document.Body!.ChildElements.OfType<SectionProperties>().Any())
            main.Document.Body!.AppendChild(docProps);
    }

    // ─── STYLES AND NUMBERING ─────────────────────────────────────────────────

    private void AddStyles(MainDocumentPart main)
    {
        var stylesPart = main.AddNewPart<StyleDefinitionsPart>();
        stylesPart.Styles = new Styles(
            new DocDefaults(
                new RunPropertiesDefault(
                    new RunPropertiesBaseStyle(
                        new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
                        new FontSize { Val = "20" })))); // 10pt default
    }

    private void AddNumbering(MainDocumentPart main)
    {
        var numPart = main.AddNewPart<NumberingDefinitionsPart>();
        numPart.Numbering = new Numbering();
    }
}
