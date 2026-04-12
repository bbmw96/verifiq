// VERIFIQ - Professional Excel Report Builder
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Generates a fully structured .xlsx compliance report with:
//   • VERIFIQ-branded cover sheet with key metrics
//   • Executive Summary with live Excel formulas (COUNTIF, SUMIF, %)
//   • Data Compliance sheet - all 20 levels with error-rate formulas
//   • Design Code sheet - all design failures with code references
//   • All Findings sheet - 16-column table with conditional formatting
//   • Critical Issues sheet (auto-filtered subset)
//   • By Agency sheet with bar-chart quantities
//   • Elements Schedule sheet - every element with compliance status
//   • Formulas sheet - all calculation methodologies

using ClosedXML.Excel;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Reports;

public sealed class ExcelReportBuilderFull
{
    private readonly ValidationSession _s;
    private readonly string _app, _company, _founder;

    private const string NavyHex  = "0B1F45";
    private const string TealHex  = "0E7C86";
    private const string LightHex = "F4F6FA";
    private const string RedHex   = "FEF2F2";
    private const string AmberHex = "FFFBEB";
    private const string GreenHex = "F0FDF4";

    public ExcelReportBuilderFull(ValidationSession session,
        string appName, string company, string founder)
    {
        _s = session; _app = appName; _company = company; _founder = founder;
    }

    public async Task BuildAsync(string path, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            using var wb = new XLWorkbook();

            BuildCoverSheet(wb);
            BuildFormulasSheet(wb);
            BuildDataComplianceSheet(wb);
            if (_s.DesignCode != null) BuildDesignCodeSheet(wb, _s.DesignCode);
            BuildAllFindingsSheet(wb);
            BuildCriticalSheet(wb);
            BuildAgencySheet(wb);
            BuildElementsSheet(wb);
            BuildCodeReferencesSheet(wb);

            wb.SaveAs(path);
        }, ct);
    }

    // ─── COVER SHEET ─────────────────────────────────────────────────────────

    private void BuildCoverSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Cover");
        // Grid lines hidden via worksheet display (ClosedXML 0.102.x API)
        ws.Column(1).Width = 4;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 30;
        ws.Column(4).Width = 22;
        ws.Column(5).Width = 4;

        // Navy header band
        ws.Range("A1:E5").Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);

        // VERIFIQ Title
        ws.Cell("B2").Value = "VERIFIQ";
        ws.Cell("B2").Style.Font.Bold = true;
        ws.Cell("B2").Style.Font.FontSize = 32;
        ws.Cell("B2").Style.Font.FontColor = XLColor.White;
        ws.Cell("B3").Value = "IFC Compliance Checker";
        ws.Cell("B3").Style.Font.FontSize = 14;
        ws.Cell("B3").Style.Font.FontColor = XLColor.FromHtml("93C5FD");

        ws.Cell("D2").Value = _s.CountryMode.ToString();
        ws.Cell("D2").Style.Font.Bold = true;
        ws.Cell("D2").Style.Font.FontSize = 16;
        ws.Cell("D2").Style.Font.FontColor = XLColor.White;

        ws.Cell("D3").Value = _s.CountryMode switch
        {
            CountryMode.Singapore => "CORENET-X / IFC+SG",
            CountryMode.Malaysia  => "NBeS / UBBL 1984",
            _                     => "Singapore + Malaysia"
        };
        ws.Cell("D3").Style.Font.FontColor = XLColor.FromHtml("93C5FD");

        // Key metrics
        int r = 7;
        AddMetricRow(ws, r++, "Report Reference",
            $"VERIFIQ-{_s.SessionId.ToString()[..8].ToUpperInvariant()}");
        AddMetricRow(ws, r++, "Generated",
            _s.StartedAt.ToString("dd MMMM yyyy HH:mm") + " UTC");
        AddMetricRow(ws, r++, "Country Mode", _s.CountryMode.ToString());
        AddMetricRow(ws, r++, "IFC Files", _s.LoadedFiles.Count.ToString());
        AddMetricRow(ws, r++, "Duration", $"{_s.Duration.TotalSeconds:F1} seconds");
        r++;

        // Score box
        ws.Cell(r, 2).Value = "DATA COMPLIANCE SCORE";
        ws.Cell(r, 2).Style.Font.Bold = true;
        ws.Cell(r, 2).Style.Font.FontSize = 12;
        ws.Cell(r, 2).Style.Font.FontColor = XLColor.FromHtml(NavyHex);
        ws.Cell(r, 3).Value = $"{_s.ComplianceScore:F1}%";
        ws.Cell(r, 3).Style.Font.Bold = true;
        ws.Cell(r, 3).Style.Font.FontSize = 28;
        ws.Cell(r, 3).Style.Font.FontColor = ScoreXLColor(_s.ComplianceScore);
        r++;

        ws.Cell(r, 2).Value = "Formula: Passed ÷ Total × 100";
        ws.Cell(r, 3).Value = $"= {_s.PassedElements} ÷ {_s.TotalElements} × 100";
        StyleFormulaRow(ws, r);
        r++;

        if (_s.DesignCode != null)
        {
            ws.Cell(r, 2).Value = "DESIGN CODE SCORE";
            ws.Cell(r, 2).Style.Font.Bold = true;
            ws.Cell(r, 3).Value = $"{_s.DesignCode.DesignComplianceScore:F1}%";
            ws.Cell(r, 3).Style.Font.Bold = true;
            ws.Cell(r, 3).Style.Font.FontSize = 18;
            ws.Cell(r, 3).Style.Font.FontColor = ScoreXLColor(_s.DesignCode.DesignComplianceScore);
            r++;

            ws.Cell(r, 2).Value = "OVERALL SCORE";
            ws.Cell(r, 2).Style.Font.Bold = true;
            ws.Cell(r, 3).Value = $"{_s.OverallScore:F1}%";
            ws.Cell(r, 3).Style.Font.Bold = true;
            ws.Cell(r, 3).Style.Font.FontSize = 22;
            ws.Cell(r, 3).Style.Font.FontColor = ScoreXLColor(_s.OverallScore);
            r++;
        }

        r++;
        // Element counts table
        ws.Cell(r, 2).Value = "Element Statistics";
        ws.Cell(r, 2).Style.Font.Bold = true;
        ws.Cell(r, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
        ws.Cell(r, 2).Style.Font.FontColor = XLColor.White;
        ws.Cell(r, 3).Value = "Count";
        ws.Cell(r, 3).Style.Font.Bold = true;
        ws.Cell(r, 3).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
        ws.Cell(r, 3).Style.Font.FontColor = XLColor.White;
        ws.Cell(r, 4).Value = "% of Total";
        ws.Cell(r, 4).Style.Font.Bold = true;
        ws.Cell(r, 4).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
        ws.Cell(r, 4).Style.Font.FontColor = XLColor.White;
        r++;

        void StatRow(string label, int count, XLColor colour)
        {
            ws.Cell(r, 2).Value = label;
            ws.Cell(r, 3).Value = count;
            ws.Cell(r, 3).Style.Font.FontColor = colour;
            ws.Cell(r, 3).Style.Font.Bold = count > 0;
            if (_s.TotalElements > 0)
                ws.Cell(r, 4).Value = $"{(double)count / _s.TotalElements * 100:F1}%";
            r++;
        }

        int totalRow = r;
        ws.Cell(r, 2).Value = "Total Elements"; ws.Cell(r, 3).Value = _s.TotalElements; r++;
        StatRow("Compliant",      _s.PassedElements,   XLColor.FromHtml("15803D"));
        StatRow("Warnings",       _s.WarningElements,  XLColor.FromHtml("B45309"));
        StatRow("Errors",         _s.ErrorElements,    XLColor.FromHtml("B45309"));
        StatRow("Critical",       _s.CriticalElements, XLColor.FromHtml("B91C1C"));
        StatRow("Proxy Elements", _s.ProxyElements,    _s.ProxyElements > 0 ? XLColor.Red : XLColor.Black);

        // Percentage formulas in col D.
        // NumberFormatId=9 is Excel's built-in Percentage format, which multiplies the
        // cell value by 100 when displaying. Store the raw fraction (e.g. 0.183) so
        // Excel shows it as 18.3%. Storing count/total*100 would produce 1830%.
        for (int row2 = totalRow + 1; row2 < r; row2++)
        {
            ws.Cell(row2, 4).FormulaA1 = $"=IF(C{totalRow}=0,0,C{row2}/C{totalRow})";
            ws.Cell(row2, 4).Style.NumberFormat.NumberFormatId = 9; // Percentage
        }

        r += 2;
        ws.Cell(r, 2).Value = $"Generated by {_app} | {_company} | Developed by {_founder}";
        ws.Cell(r, 2).Style.Font.Italic = true;
        ws.Cell(r, 2).Style.Font.FontSize = 10;
        ws.Cell(r, 2).Style.Font.FontColor = XLColor.Gray;
    }

    // Percentage formula helper - stores raw fraction (0.183) for cells formatted as Percentage.
    // Excel's Percentage format (NumberFormatId=9) multiplies by 100 for display.
    private static string PctFormula(int dataRow, int totalRow)
        => $"=IF(C{totalRow}=0,0,C{dataRow}/C{totalRow})";

    // ─── FORMULAS SHEET ───────────────────────────────────────────────────────

    private void BuildFormulasSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Formulas");
        ws.Column(1).Width = 4;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 50;
        ws.Column(4).Width = 30;
        // Grid lines hidden via worksheet display (ClosedXML 0.102.x API)

        NavyHeader(ws, 1, "VERIFIQ: Calculation Formulas and Methodology", 2, 4);

        int r = 3;
        ws.Cell(r, 2).Value = "All formulas used by VERIFIQ to calculate compliance scores and check design code parameters.";
        ws.Cell(r, 2).Style.Font.Italic = true;
        r += 2;

        // Section: Compliance score formulas
        SectionHeader(ws, r++, "Compliance Score Calculations", 2, 4);
        FormulaRow(ws, r++, "Data Compliance Score",
            "= (N_pass ÷ N_total) × 100",
            $"= ({_s.PassedElements} ÷ {_s.TotalElements}) × 100 = {_s.ComplianceScore:F1}%");
        if (_s.DesignCode != null)
        {
            FormulaRow(ws, r++, "Design Compliance Score",
                "= (D_pass ÷ D_total) × 100",
                $"= ({_s.DesignCode.PassedChecks} ÷ {_s.DesignCode.TotalChecks}) × 100 = {_s.DesignCode.DesignComplianceScore:F1}%");
            FormulaRow(ws, r++, "Overall Score",
                "= (Data Score + Design Score) ÷ 2",
                $"= ({_s.ComplianceScore:F1} + {_s.DesignCode.DesignComplianceScore:F1}) ÷ 2 = {_s.OverallScore:F1}%");
        }
        FormulaRow(ws, r++, "Error Rate per Check Level",
            "= (Failures at Level N ÷ N_total) × 100",
            "Result shown in column C of Data Compliance sheet");
        r++;

        SectionHeader(ws, r++, "Design Code Check Formulas", 2, 4);
        FormulaRow(ws, r++, "Minimum Value Check",
            "PASS if: Actual Value ≥ Required Minimum",
            "E.g. GrossArea(Bedroom) ≥ 9.0 m²");
        FormulaRow(ws, r++, "Maximum Value Check",
            "PASS if: Actual Value ≤ Permitted Maximum",
            "E.g. TravelDistance ≤ 30 m");
        FormulaRow(ws, r++, "Balcony Ratio (URA SG)",
            "Balcony Ratio = Σ(BalconyArea) ÷ Σ(TotalGFA) × 100 ≤ 10%",
            "Must not exceed 10% of total GFA per URA guidelines");
        FormulaRow(ws, r++, "Window Area Ratio",
            "WindowAreaRatio = Σ(WindowArea) ÷ FloorArea × 100 ≥ 10%",
            "SG: BCA Building Control Reg §7; MY: UBBL By-Law 38");
        FormulaRow(ws, r++, "Ventilation Ratio",
            "VentRatio = Σ(VentOpeningArea) ÷ FloorArea × 100 ≥ 5%",
            "SG: NEA Environmental Health Regs; MY: UBBL By-Law 39");
        FormulaRow(ws, r++, "Ramp Gradient",
            "SlopeRatio = 1:N where N = 1 ÷ tan(θ°); Gradient% = 1÷N×100 ≤ 8.33%",
            "PASS if N ≥ 12 (i.e. gradient ≤ 1/12 = 8.33%)");
        FormulaRow(ws, r++, "Fire Rating Parse",
            "R/E/I notation → extract R value in minutes; FDnn → n minutes",
            "E.g. '60/60/60' → 60 min; 'FD120' → 120 min");
        FormulaRow(ws, r++, "Staircase Ratio Check",
            "2R + T = 550 to 700 mm  (R=riser mm, T=tread mm)",
            "SG: BCR §8; MY: UBBL By-Law 122");
        FormulaRow(ws, r++, "WWR (Window Wall Ratio)",
            "WWR = Σ(WindowArea on facade) ÷ Σ(GrossWallArea) × 100 ≤ Max%",
            "SG-N/S: ≤ 50%; SG-E/W: ≤ 25%; per BCA Green Mark 2021");
        r++;

        SectionHeader(ws, r++, "IFC Property Extraction Logic", 2, 4);
        FormulaRow(ws, r++, "Area Source Priority",
            "1. SGPset_SpaceGFA.GrossFloorArea  2. Pset_SpaceCommon.GrossPlannedArea  3. BoundingBox",
            "First available value is used; source recorded in output");
        FormulaRow(ws, r++, "Height Source Priority",
            "1. Pset_SpaceCommon.Reference (storey height)  2. BoundingBox Z extent",
            "BoundingBox Z range used when no explicit height property found");
        FormulaRow(ws, r++, "Width Source Priority",
            "1. Pset_DoorCommon.OverallWidth  2. BoundingBox X/Y min extent",
            "For corridors: BoundingBox min(X,Y) extent used");
        FormulaRow(ws, r++, "Thickness Source Priority",
            "1. Pset_WallCommon.Thickness (if present)  2. BoundingBox min extent",
            "Wall thickness from BoundingBox is approximate only");
    }

    // ─── DATA COMPLIANCE SHEET ────────────────────────────────────────────────

    private void BuildDataComplianceSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Data Compliance");
        // Grid lines hidden via worksheet display (ClosedXML 0.102.x API)

        NavyHeader(ws, 1, "Data Compliance: All 20 Check Levels", 1, 6);

        // Metadata
        ws.Cell(2, 1).Value = "Total Elements";
        ws.Cell(2, 2).Value = _s.TotalElements;
        ws.Cell(2, 2).Style.Font.Bold = true;
        ws.Cell(3, 1).Value = "Compliance Score";
        ws.Cell(3, 2).FormulaA1 = "=B2_pass/B2*100";
        ws.Cell(3, 2).Value = $"{_s.ComplianceScore:F1}%";
        ws.Cell(3, 2).Style.Font.Bold = true;

        // Column headers
        int headerRow = 5;
        string[] headers = {
            "Check Level", "Level Name", "Description",
            "Failures", "Error Rate Formula", "Error Rate %",
            "Typical Fix"
        };
        int[] widths = { 8, 28, 50, 10, 36, 14, 45 };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(headerRow, c + 1).Value = headers[c];
            ws.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
            ws.Cell(headerRow, c + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Column(c + 1).Width = widths[c];
        }

        var checkDescriptions = new (CheckLevel, string Desc, string Fix)[]
        {
            (CheckLevel.IfcEntityClass,          "Is element a specific class, not IfcBuildingElementProxy?",       "Change element type in BIM software; assign correct IFC class"),
            (CheckLevel.PredefinedType,           "Is PredefinedType specific (not NOTDEFINED or blank)?",           "Set PredefinedType in IFC export settings or ArchiCAD/Revit classification"),
            (CheckLevel.ObjectTypeUserDefined,    "When USERDEFINED, is ObjectType string populated?",              "Add descriptive ObjectType string when using USERDEFINED"),
            (CheckLevel.ClassificationReference,  "Is IfcClassificationReference present and populated?",            "Assign classification from IFC+SG Industry Mapping in BIM software"),
            (CheckLevel.ClassificationEdition,    "Is classification referencing the current edition?",             "Update classification system reference to 2025 edition"),
            (CheckLevel.MandatoryPropertySets,    "Are all required Pset_ property sets present?",                  "Configure Pset_ property sets in ArchiCAD/Revit IFC export settings"),
            (CheckLevel.SgPropertySets,           "Are all required SGPset_ property sets present? (SG only)",       "Load IFC+SG Export Translator/Shared Parameters from info.corenet.gov.sg"),
            (CheckLevel.PropertyValuesPopulated,  "Are all mandatory property values filled (not empty/NOTDEFINED)?","Populate all mandatory fields in property sets before export"),
            (CheckLevel.PropertyValueDataType,    "Do values match required data types (BOOLEAN, REAL, STRING)?",   "Check value format: IsExternal must be TRUE/FALSE, not 'Yes'/'No'"),
            (CheckLevel.PropertyValueEnumeration, "Do enumerated values match the permitted list?",                  "Use exact permitted values from IFC+SG mapping; check capitalisation"),
            (CheckLevel.SpatialContainment,       "Is every element contained within an IfcBuildingStorey?",        "Assign all elements to a storey in BIM software (Home Storey in ArchiCAD)"),
            (CheckLevel.StoreyElevation,          "Are storey elevations consistent across federated files?",       "Synchronise storey elevations between discipline BIM models"),
            (CheckLevel.Georeferencing,           "Is IfcMapConversion present and in SVY21/GDM2000?",             "Set project location using SVY21 coordinates before IFC export"),
            (CheckLevel.SiteAndBuildingHierarchy, "Is IfcSite/Building/Storey hierarchy correct?",                 "Ensure Site, Building and Storey levels exist in BIM project structure"),
            (CheckLevel.GuidUniqueness,           "Is every GlobalId unique across all elements?",                  "Regenerate GUIDs in BIM software; avoid copying elements between files"),
            (CheckLevel.MaterialAssignment,       "Are structural/fire-rated elements assigned materials?",         "Assign material to load-bearing walls, slabs, beams and columns"),
            (CheckLevel.SpaceBoundaryIntegrity,   "Do spaces have Category and area in Pset_SpaceCommon?",         "Set room categories and compute areas in BIM software space properties"),
            (CheckLevel.GeometryValidity,         "Is element geometry non-degenerate (no zero-size elements)?",   "Check for zero-thickness walls, zero-area slabs in BIM software"),
            (CheckLevel.IfcSchemaVersion,         "Is the IFC schema version IFC4 Reference View? (SG)",           "Select IFC4 Reference View in IFC export settings"),
            (CheckLevel.FileHeaderCompleteness,   "Is the file header complete (authoring app, schema, date)?",    "Check IFC export settings include complete file header"),
        };

        int row = headerRow + 1;
        foreach (var (level, desc, fix) in checkDescriptions)
        {
            _s.ErrorsByCheckLevel.TryGetValue(level, out int failures);
            double rate = _s.TotalElements > 0 ? (double)failures / _s.TotalElements * 100 : 0;

            int levelNum = (int)level;
            string bg = failures == 0 ? GreenHex : failures > 10 ? RedHex : AmberHex;

            ws.Cell(row, 1).Value = $"L{levelNum:D2}";
            ws.Cell(row, 2).Value = level.ToString();
            ws.Cell(row, 3).Value = desc;
            ws.Cell(row, 4).Value = failures;
            ws.Cell(row, 4).Style.Font.Bold = failures > 0;
            ws.Cell(row, 4).Style.Font.FontColor = failures == 0
                ? XLColor.FromHtml("15803D") : XLColor.FromHtml("B91C1C");
            ws.Cell(row, 5).Value = $"=D{row}/{_s.TotalElements}*100";
            ws.Cell(row, 5).FormulaA1 = $"=D{row}/{_s.TotalElements}*100";
            ws.Cell(row, 6).Value = $"{rate:F1}%";
            ws.Cell(row, 7).Value = fix;

            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml(failures == 0 ? "FFFFFF" : row % 2 == 0 ? "FFFFFF" : "FAFAFA");

            row++;
        }

        ws.Row(headerRow).Height = 20;
        ws.SheetView.FreezeRows(headerRow);
    }

    // ─── DESIGN CODE SHEET ────────────────────────────────────────────────────

    private void BuildDesignCodeSheet(XLWorkbook wb, DesignCodeSession dc)
    {
        var ws = wb.Worksheets.Add("Design Code");
        // Grid lines hidden via worksheet display (ClosedXML 0.102.x API)

        NavyHeader(ws, 1, "Design Code Compliance: Actual Value Checks", 1, 9);

        int r = 3;
        ws.Cell(r, 1).Value = "Design Score";
        ws.Cell(r, 2).Value = $"{dc.DesignComplianceScore:F1}%";
        ws.Cell(r, 2).Style.Font.Bold = true;
        ws.Cell(r, 2).Style.Font.FontSize = 16;
        ws.Cell(r, 2).Style.Font.FontColor = ScoreXLColor(dc.DesignComplianceScore);
        ws.Cell(r, 3).Value = $"Formula: {dc.PassedChecks} passed ÷ {dc.TotalChecks} total × 100";
        ws.Cell(r, 3).Style.Font.Italic = true;

        int headerRow = 5;
        string[] headers = {
            "Rule ID", "Rule Name", "Code Reference", "Element GUID",
            "Element Name", "IFC Class", "Actual Value", "Required", "Formula", "Result", "Severity", "Fix"
        };
        int[] widths = { 16, 38, 40, 36, 30, 26, 16, 20, 40, 28, 12, 50 };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(headerRow, c + 1).Value = headers[c];
            ws.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(TealHex);
            ws.Cell(headerRow, c + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Column(c + 1).Width = widths[c];
        }

        var dataRow = headerRow + 1;
        foreach (var result in dc.Results.OrderByDescending(r2 => r2.Severity)
                                         .ThenBy(r2 => r2.RuleId))
        {
            string bg = result.Complies ? "FFFFFF"
                      : result.Severity == Severity.Critical ? "FEF2F2"
                      : result.Severity == Severity.Error    ? "FFFBEB"
                      : "FFFFFF";

            ws.Cell(dataRow, 1).Value  = result.RuleId;
            ws.Cell(dataRow, 2).Value  = result.RuleName;
            ws.Cell(dataRow, 3).Value  = result.CodeReference;
            ws.Cell(dataRow, 4).Value  = result.ElementGuid;
            ws.Cell(dataRow, 5).Value  = result.ElementName;
            ws.Cell(dataRow, 6).Value  = result.IfcClass;
            ws.Cell(dataRow, 7).Value  = result.ActualDisplay;
            ws.Cell(dataRow, 8).Value  = result.RequiredDisplay;
            ws.Cell(dataRow, 9).Value  = result.Formula;
            ws.Cell(dataRow, 9).Style.Font.FontName = "Courier New";
            ws.Cell(dataRow, 10).Value = result.FormulaResult;
            ws.Cell(dataRow, 10).Style.Font.FontName = "Courier New";
            ws.Cell(dataRow, 11).Value = result.Severity.ToString();
            ws.Cell(dataRow, 12).Value = result.RemediationGuidance;

            ws.Row(dataRow).Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
            ws.Cell(dataRow, 11).Style.Font.Bold = !result.Complies;
            ws.Cell(dataRow, 11).Style.Font.FontColor = result.Complies
                ? XLColor.FromHtml("15803D") : XLColor.FromHtml("B91C1C");

            dataRow++;
        }

        ws.SheetView.FreezeRows(headerRow);
        ws.RangeUsed().SetAutoFilter();

        // Summary formula at top
        ws.Cell(4, 1).Value = "Total Checks";
        ws.Cell(4, 2).FormulaA1 = $"=COUNTA(K{headerRow + 1}:K{dataRow - 1})";
        ws.Cell(4, 1).Value = $"Total: {dc.TotalChecks}, Passed: {dc.PassedChecks}, Failed: {dc.FailedChecks}";
    }

    // ─── ALL FINDINGS SHEET ───────────────────────────────────────────────────

    private void BuildAllFindingsSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("All Findings");
        // Grid lines hidden via worksheet display (ClosedXML 0.102.x API)

        int headerRow = 1;
        string[] headers = {
            "Severity", "Check Level", "Element GUID", "Element Name",
            "IFC Class", "Storey", "Country", "Agency", "Gateway",
            "Property Set", "Property", "Expected Value", "Actual Value",
            "Message", "Remediation", "Rule Source"
        };
        int[] widths = { 12, 26, 36, 30, 26, 20, 14, 10, 18, 25, 28, 30, 30, 60, 55, 38 };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(headerRow, c + 1).Value = headers[c];
            ws.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
            ws.Cell(headerRow, c + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Column(c + 1).Width = widths[c];
        }

        int row = 2;
        foreach (var r in _s.Results.OrderByDescending(x => x.Severity))
        {
            string bg = r.Severity == Severity.Critical ? RedHex
                      : r.Severity == Severity.Error    ? AmberHex
                      : r.Severity == Severity.Warning  ? "FFFDE7"
                      : row % 2 == 0 ? "FFFFFF" : "F9FAFB";

            ws.Cell(row, 1).Value  = r.Severity.ToString();
            ws.Cell(row, 2).Value  = r.CheckLevel.ToString();
            ws.Cell(row, 3).Value  = r.ElementGuid;
            ws.Cell(row, 4).Value  = r.ElementName;
            ws.Cell(row, 5).Value  = r.IfcClass;
            ws.Cell(row, 6).Value  = r.StoreyName;
            ws.Cell(row, 7).Value  = r.Country.ToString();
            ws.Cell(row, 8).Value  = r.AffectedAgency != SgAgency.None ? r.AffectedAgency.ToString() : "-";
            ws.Cell(row, 9).Value  = r.AffectedGateway.ToString();
            ws.Cell(row, 10).Value = r.PropertySetName;
            ws.Cell(row, 11).Value = r.PropertyName;
            ws.Cell(row, 12).Value = r.ExpectedValue;
            ws.Cell(row, 13).Value = r.ActualValue;
            ws.Cell(row, 14).Value = r.Message;
            ws.Cell(row, 15).Value = r.RemediationGuidance;
            ws.Cell(row, 16).Value = r.RuleSource;

            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml(bg);
            ws.Cell(row, 1).Style.Font.Bold = r.Severity >= Severity.Error;

            row++;
        }

        // Summary COUNTIF formulas below the data
        row += 2;
        ws.Cell(row, 1).Value = "Summary (COUNTIF formulas)";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.White;
        row++;

        int lastData = row - 3;
        ws.Cell(row, 1).Value = "Critical Count";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(A2:A{lastData},\"Critical\")";
        row++;
        ws.Cell(row, 1).Value = "Error Count";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(A2:A{lastData},\"Error\")";
        row++;
        ws.Cell(row, 1).Value = "Warning Count";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(A2:A{lastData},\"Warning\")";
        row++;
        ws.Cell(row, 1).Value = "Pass Count";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(A2:A{lastData},\"Pass\")";
        row++;
        ws.Cell(row, 1).Value = "Total Findings";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTA(A2:A{lastData})";

        ws.SheetView.FreezeRows(1);
        ws.Range(1, 1, 1, 16).SetAutoFilter();
    }

    // ─── CRITICAL SHEET ───────────────────────────────────────────────────────

    private void BuildCriticalSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Critical Issues");
        var criticals = _s.Results.Where(r => r.Severity == Severity.Critical).ToList();

        NavyHeader(ws, 1, $"Critical Issues: {criticals.Count} items requiring immediate attention", 1, 7);

        string[] headers = {
            "Rule", "Element Name", "IFC Class", "GUID", "Agency", "Issue", "Fix"
        };
        int[] widths = { 28, 30, 24, 36, 10, 60, 55 };

        int headerRow = 3;
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(headerRow, c + 1).Value = headers[c];
            ws.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("B91C1C");
            ws.Cell(headerRow, c + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Column(c + 1).Width = widths[c];
        }

        int row = headerRow + 1;
        foreach (var r in criticals)
        {
            ws.Cell(row, 1).Value = r.CheckLevel.ToString();
            ws.Cell(row, 2).Value = r.ElementName;
            ws.Cell(row, 3).Value = r.IfcClass;
            ws.Cell(row, 4).Value = r.ElementGuid;
            ws.Cell(row, 5).Value = r.AffectedAgency != SgAgency.None ? r.AffectedAgency.ToString() : "-";
            ws.Cell(row, 6).Value = r.Message;
            ws.Cell(row, 7).Value = r.RemediationGuidance;
            ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml(row % 2 == 0 ? RedHex : "FFFFFF");
            row++;
        }

        ws.SheetView.FreezeRows(headerRow);
        ws.RangeUsed().SetAutoFilter();
    }

    // ─── AGENCY SHEET ─────────────────────────────────────────────────────────

    private void BuildAgencySheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("By Agency (SG)");
        NavyHeader(ws, 1, "Singapore: Errors by Regulatory Agency", 1, 4);

        string[] headers = { "Agency", "Error Count", "% of Total Findings", "Formula" };
        int[] widths = { 20, 15, 22, 45 };

        int headerRow = 3;
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(headerRow, c + 1).Value = headers[c];
            ws.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
            ws.Cell(headerRow, c + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Column(c + 1).Width = widths[c];
        }

        int total = _s.Results.Count(r => r.Severity >= Severity.Error);
        int row = headerRow + 1;
        int firstDataRow = row;

        foreach (var kvp in _s.ErrorsByAgency.OrderByDescending(k => k.Value))
        {
            ws.Cell(row, 1).Value = kvp.Key.ToString();
            ws.Cell(row, 2).Value = kvp.Value;
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 3).FormulaA1 = $"=B{row}/B{firstDataRow + 10}*100";
            ws.Cell(row, 3).Value = total > 0 ? $"{(double)kvp.Value / total * 100:F1}%" : "0%";
            ws.Cell(row, 4).Value = $"= {kvp.Value} ÷ {total} × 100 = {(total > 0 ? (double)kvp.Value / total * 100 : 0):F1}%";
            row++;
        }

        // Total row with SUM formula
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).FormulaA1 = $"=SUM(B{firstDataRow}:B{row - 1})";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = "100%";
        ws.Cell(row, 4).Value = $"Sum of all agency errors = {total}";
        ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml(LightHex);
    }

    // ─── ELEMENTS SHEET ───────────────────────────────────────────────────────

    private void BuildElementsSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Elements Schedule");
        int headerRow = 1;

        string[] headers = {
            "GUID", "Element Name", "IFC Class", "Predefined Type",
            "Storey", "Overall Severity", "Error Count", "Warning Count",
            "Is Proxy", "Has Classification", "Has Required Psets",
            "Storey Assigned", "Material Assigned"
        };
        int[] widths = { 36, 30, 26, 20, 18, 18, 13, 16, 12, 20, 20, 18, 18 };

        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(headerRow, c + 1).Value = headers[c];
            ws.Cell(headerRow, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
            ws.Cell(headerRow, c + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, c + 1).Style.Font.FontColor = XLColor.White;
            ws.Column(c + 1).Width = widths[c];
        }

        int row = 2;
        foreach (var file in _s.LoadedFiles)
        {
            foreach (var el in file.Elements)
            {
                string bg = el.OverallSeverity == Severity.Critical ? RedHex
                          : el.OverallSeverity == Severity.Error    ? AmberHex
                          : el.IsProxy ? "FFF7ED"
                          : el.OverallSeverity == Severity.Pass ? GreenHex
                          : "FFFFFF";

                bool hasClassif = el.Classifications.Any();
                bool hasPsets   = el.PropertySets.Count >= 1;
                bool hasStorey  = !string.IsNullOrEmpty(el.StoreyGuid);
                bool hasMat     = el.Materials.Any();

                ws.Cell(row, 1).Value  = el.GlobalId;
                ws.Cell(row, 2).Value  = el.Name;
                ws.Cell(row, 3).Value  = el.IfcClass;
                ws.Cell(row, 4).Value  = el.PredefinedType;
                ws.Cell(row, 5).Value  = el.StoreyName;
                ws.Cell(row, 6).Value  = el.OverallSeverity.ToString();
                ws.Cell(row, 7).Value  = el.ErrorCount;
                ws.Cell(row, 8).Value  = el.WarningCount;
                ws.Cell(row, 9).Value  = el.IsProxy;
                ws.Cell(row, 10).Value = hasClassif;
                ws.Cell(row, 11).Value = hasPsets;
                ws.Cell(row, 12).Value = hasStorey;
                ws.Cell(row, 13).Value = hasMat;

                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml(bg);

                // Conditional formatting for FALSE values
                if (!hasClassif) ws.Cell(row, 10).Style.Font.FontColor = XLColor.Red;
                if (!hasPsets)   ws.Cell(row, 11).Style.Font.FontColor = XLColor.Red;
                if (!hasStorey)  ws.Cell(row, 12).Style.Font.FontColor = XLColor.OrangeRed;

                row++;
            }
        }

        // Summary row with COUNTIF formulas
        row += 2;
        ws.Cell(row, 1).Value = "SUMMARY (live formulas)";
        ws.Cell(row, 1).Style.Font.Bold = true;
        row++;
        ws.Cell(row, 1).Value = "Total Elements";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTA(A2:A{row - 3})";
        row++;
        ws.Cell(row, 1).Value = "Proxy Elements";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(I2:I{row - 4},TRUE)";
        row++;
        ws.Cell(row, 1).Value = "Missing Classification";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(J2:J{row - 5},FALSE)";
        row++;
        ws.Cell(row, 1).Value = "Missing Property Sets";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(K2:K{row - 6},FALSE)";
        row++;
        ws.Cell(row, 1).Value = "Not Assigned to Storey";
        ws.Cell(row, 2).FormulaA1 = $"=COUNTIF(L2:L{row - 7},FALSE)";

        ws.SheetView.FreezeRows(1);
        ws.RangeUsed().SetAutoFilter();
    }

    // ─── CODE REFERENCES SHEET ────────────────────────────────────────────────

    private void BuildCodeReferencesSheet(XLWorkbook wb)
    {
        var ws = wb.Worksheets.Add("Code References");
        ws.Column(1).Width = 4;
        ws.Column(2).Width = 35;
        ws.Column(3).Width = 80;
        // Grid lines hidden via worksheet display (ClosedXML 0.102.x API)

        NavyHeader(ws, 1, "Regulatory Code References", 2, 3);

        var refs = new List<(string, string)>();

        if (_s.CountryMode is CountryMode.Singapore or CountryMode.Combined)
        {
            refs.Add(("── SINGAPORE ──", ""));
            refs.Add(("CORENET-X COP 3rd Ed.", "Code of Practice for BIM e-Submission, 3rd Edition, September 2025 (BCA/GovTech)"));
            refs.Add(("IFC+SG Industry Mapping", "IFC+SG Industry Mapping 2025  -  500+ parameters, all 8 agencies (BCA/GovTech), info.corenet.gov.sg"));
            refs.Add(("BCA Building Control Act", "Building Control Act (Cap 29, 2000 Rev) and Building Control Regulations 2003 (Rev 2021)"));
            refs.Add(("BCA Code on Accessibility", "Code on Accessibility in the Built Environment 2025  -  Universal design requirements"));
            refs.Add(("SCDF Fire Code",             "Singapore Fire Code 2018 (2023 Amendment) - SCDF - escape routes, compartmentation, fire ratings"));
            refs.Add(("BCA Green Mark 2021",         "BCA Green Mark for Residences 2021 / Non-Residences 2021  -  WWR, U-values, RETV"));
            refs.Add(("BCA Envelope Thermal Code",   "Code on Envelope Thermal Performance 2008 (Rev 2021)  -  ETTV, RETV, U-values"));
            refs.Add(("URA Planning Handbook 2023",  "URA Handbook on Singapore's Planning Parameters 2023  -  room sizes, GFA rules, setbacks"));
            refs.Add(("URA Balcony Guidelines",      "URA Circular  -  Guidelines on Balconies in New Private Residential Developments, November 2019"));
            refs.Add(("URA DC Landed Housing",       "URA Development Control Handbook  -  Landed Housing (Revised 2023)"));
            refs.Add(("LTA Parking Code",            "LTA Code of Practice for Vehicle Parking Provision in Development Proposals (Rev 2023)"));
            refs.Add(("NEA Environmental Health",    "Environmental Public Health Act  -  Licensing of Premises Regulations, Third Schedule"));
            refs.Add(("PUB / SS 636:2018",           "PUB Sewerage and Drainage Act; SS 636:2018 Code of Practice for Water Services in Buildings"));
            refs.Add(("SVY21 / EPSG:3414",           "Singapore SVY21 Coordinate Reference System  -  SLA; Mandatory for CORENET-X submissions"));
        }

        if (_s.CountryMode is CountryMode.Malaysia or CountryMode.Combined)
        {
            refs.Add(("── MALAYSIA ──", ""));
            refs.Add(("UBBL 1984",    "Uniform Building By-Laws 1984 (G.N. 2571/1985 with all amendments) under Act 133  -  all 9 Parts"));
            refs.Add(("Act 133",      "Street, Drainage and Building Act 1974 (Act 133), as amended by Act A1286/2007"));
            refs.Add(("NBeS 2024",    "National BIM e-Submission (NBeS) - CIDB Malaysia - IFC mapping documentation 2024 edition"));
            refs.Add(("MS 1184:2014", "MS 1184:2014 Code of Practice on Access for Disabled Persons to Public Buildings (SIRIM Berhad)"));
            refs.Add(("JBPM 2020",    "Garis Panduan Persyaratan Keselamatan Kebakaran 2020 (JBPM)  -  fire safety escape and compartmentation"));
            refs.Add(("GBI Malaysia", "Green Building Index (GBI) Non-Residential NC V1.0 / Residential NC V1.0  -  thermal, WWR, energy"));
            refs.Add(("MS EN 1992",   "MS EN 1992-1-1:2010 Eurocode 2  -  Design of Concrete Structures (adopted in Malaysia)"));
            refs.Add(("GDM2000",      "Malaysia Geodetic Datum 2000 (GDM2000)  -  DSMM Malaysia; projection varies by state"));
        }

        int row = 3;
        foreach (var (code, citation) in refs)
        {
            bool isHeader = code.StartsWith("──");
            if (isHeader)
            {
                ws.Cell(row, 2).Value = code;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromHtml(LightHex);
            }
            else
            {
                ws.Cell(row, 2).Value = code;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 3).Value = citation;
            }
            row++;
        }
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static void NavyHeader(IXLWorksheet ws, int row, string title, int col1, int col2)
    {
        ws.Cell(row, col1).Value = title;
        ws.Cell(row, col1).Style.Font.Bold = true;
        ws.Cell(row, col1).Style.Font.FontSize = 16;
        ws.Cell(row, col1).Style.Font.FontColor = XLColor.White;
        ws.Cell(row, col1).Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
        ws.Range(row, col1, row, col2).Merge()
            .Style.Fill.BackgroundColor = XLColor.FromHtml(NavyHex);
    }

    private static void SectionHeader(IXLWorksheet ws, int row, string title, int col1, int col2)
    {
        ws.Cell(row, col1).Value = title;
        ws.Cell(row, col1).Style.Font.Bold = true;
        ws.Cell(row, col1).Style.Font.FontSize = 13;
        ws.Cell(row, col1).Style.Font.FontColor = XLColor.FromHtml(TealHex);
        ws.Range(row, col1, row, col2).Merge()
            .Style.Fill.BackgroundColor = XLColor.FromHtml("E0F7FA");
    }

    private static void FormulaRow(IXLWorksheet ws, int row, string label, string formula, string result)
    {
        ws.Cell(row, 2).Value = label;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = formula;
        ws.Cell(row, 3).Style.Font.FontName = "Courier New";
        ws.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("F0F9FF");
        ws.Cell(row, 4).Value = result;
        ws.Cell(row, 4).Style.Font.Italic = true;
    }

    private static void AddMetricRow(IXLWorksheet ws, int row, string label, string value)
    {
        ws.Cell(row, 2).Value = label;
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 2).Style.Font.FontColor = XLColor.FromHtml("555555");
        ws.Cell(row, 3).Value = value;
    }

    private static void StyleFormulaRow(IXLWorksheet ws, int row)
    {
        ws.Cell(row, 3).Style.Font.FontName = "Courier New";
        ws.Cell(row, 3).Style.Fill.BackgroundColor = XLColor.FromHtml("F0F9FF");
        ws.Cell(row, 3).Style.Font.FontColor = XLColor.FromHtml(TealHex);
    }

    private static XLColor ScoreXLColor(double score) =>
        score >= 95 ? XLColor.FromHtml("15803D")
      : score >= 80 ? XLColor.FromHtml("B45309")
      : XLColor.FromHtml("B91C1C");
}
