// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// ─── IFC+SG INDUSTRY MAPPING EXCEL IMPORTER ──────────────────────────────────
//
// Reads the official BCA/GovTech IFC+SG Industry Mapping Excel file
// (downloadable from go.gov.sg/ifcsg, IFC+SG Resource Kit) and merges the classification
// codes and their required property sets into VERIFIQ's runtime library.
//
// The Industry Mapping Excel defines for every building element type:
//   - The IFC entity class and PredefinedType
//   - The IFC+SG classification code
//   - Every required Pset_ and SGPset_ property set
//   - Every required property within each set
//   - The regulatory agency responsible
//
// After import, the runtime ClassificationCodeLibrary is updated with
// the exact code-to-property mappings from the official BCA document.
// This makes VERIFIQ 100% aligned with the submitted Industry Mapping
// version, rather than relying solely on the embedded codes.
//
// Supported Excel formats:
//   • IFC+SG Industry Mapping 2025 (COP3) - primary format
//   • IFC+SG Industry Mapping 2023 (COP2) - backward compatible
//
// Column conventions detected automatically by header name scanning.

using ClosedXML.Excel;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;
using VERIFIQ.Rules.Common;

namespace VERIFIQ.Reports;

public sealed class IndustryMappingImportResult
{
    public bool    Success            { get; set; }
    public int     CodesImported      { get; set; }
    public int     CodesUpdated       { get; set; }
    public int     RulesImported      { get; set; }
    public string  SourceFile         { get; set; } = string.Empty;
    public string  DetectedVersion    { get; set; } = string.Empty;
    public List<string> Errors        { get; set; } = new();
    public List<string> Warnings      { get; set; } = new();
    public List<string> ImportedCodes { get; set; } = new();
}

public sealed class IndustryMappingImporter
{
    // ─── PUBLIC ENTRY POINT ──────────────────────────────────────────────────

    public async Task<IndustryMappingImportResult> ImportAsync(
        string excelPath,
        CancellationToken ct = default)
    {
        var result = new IndustryMappingImportResult { SourceFile = excelPath };

        if (!File.Exists(excelPath))
        {
            result.Errors.Add($"File not found: {excelPath}");
            return result;
        }

        var ext = Path.GetExtension(excelPath).ToLowerInvariant();
        if (ext != ".xlsx" && ext != ".xls")
        {
            result.Errors.Add("Only .xlsx and .xls files are supported.");
            return result;
        }

        try
        {
            await Task.Run(() => ProcessWorkbook(excelPath, result), ct);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Import failed: {ex.Message}");
        }

        return result;
    }

    // ─── WORKBOOK PROCESSING ─────────────────────────────────────────────────

    private static void ProcessWorkbook(string path, IndustryMappingImportResult result)
    {
        using var wb = new XLWorkbook(path);

        // Detect version from workbook properties or title sheet
        result.DetectedVersion = DetectVersion(wb);

        // Try all worksheets - Industry Mapping Excel often has multiple tabs
        // (one per discipline or one combined sheet)
        int sheetsProcessed = 0;
        foreach (var ws in wb.Worksheets)
        {
            // Skip obviously non-data sheets
            if (ws.Name.StartsWith("Cover", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.StartsWith("Instruction", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.StartsWith("Legend", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.StartsWith("Change", StringComparison.OrdinalIgnoreCase) ||
                ws.Name.StartsWith("ReadMe", StringComparison.OrdinalIgnoreCase))
                continue;

            var colMap = DetectColumns(ws);
            if (!colMap.IsValid)
            {
                result.Warnings.Add($"Sheet '{ws.Name}': could not identify required columns - skipped.");
                continue;
            }

            ProcessSheet(ws, colMap, result);
            sheetsProcessed++;
        }

        if (sheetsProcessed == 0)
        {
            result.Errors.Add("No valid data sheets found. Expected columns: IFC Class, Classification Code, PropertySet, PropertyName.");
            return;
        }

        result.Success = result.CodesImported > 0 || result.CodesUpdated > 0;
    }

    private static void ProcessSheet(IXLWorksheet ws, ColumnMap cols, IndustryMappingImportResult result)
    {
        // Group by classification code.
        // COP3.1 BCA format: "Identified Component" is used as the key (e.g. "Beam", "Wall").
        // Each row represents one property rule for that component under a given agency.
        // ArchiCAD note: elements are classified by classification code, not native type.
        // A slab assigned classification "Wall" exports as IfcWall in ArchiCAD.
        // VERIFIQ validates based on the IFC entity + classification combination.
        var codeEntries = new Dictionary<string, ClassificationCodeEntry>(StringComparer.OrdinalIgnoreCase);

        int lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = cols.DataStartRow; row <= lastRow; row++)
        {
            var classCode  = GetCell(ws, row, cols.ClassificationCode)?.Trim();
            var ifcClass   = GetCell(ws, row, cols.IfcClass)?.Trim().ToUpperInvariant();
            var predType   = GetCell(ws, row, cols.PredefinedType)?.Trim().ToUpperInvariant() ?? "";
            var psetName   = GetCell(ws, row, cols.PropertySetName)?.Trim();
            var propName   = GetCell(ws, row, cols.PropertyName)?.Trim();
            var agencyStr  = GetCell(ws, row, cols.Agency)?.Trim();
            var required   = GetCell(ws, row, cols.IsRequired)?.Trim();
            var expected   = GetCell(ws, row, cols.ExpectedValue)?.Trim() ?? "";
            var desc       = GetCell(ws, row, cols.Description)?.Trim() ?? "";
            var regRef     = GetCell(ws, row, cols.Regulation)?.Trim() ?? "";
            var codeName   = GetCell(ws, row, cols.CodeName)?.Trim() ?? "";

            // Skip blank or header rows
            if (string.IsNullOrWhiteSpace(classCode) || string.IsNullOrWhiteSpace(ifcClass))
                continue;
            if (classCode.Equals("Classification Code", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(psetName) || string.IsNullOrWhiteSpace(propName))
                continue;

            // Get or create entry for this code
            if (!codeEntries.TryGetValue(classCode, out var entry))
            {
                entry = new ClassificationCodeEntry
                {
                    Code           = classCode,
                    Name           = codeName,
                    IfcClass       = ifcClass,
                    PredefinedType = predType,
                    Discipline     = classCode.Split('-').FirstOrDefault() ?? "A",
                    PrimaryAgency  = ParseAgency(agencyStr)
                };
                codeEntries[classCode] = entry;
            }

            // Add the property rule
            entry.Rules.Add(new CodePropertyRule
            {
                PropertySetName = psetName,
                PropertyName    = propName,
                IsRequired      = ParseRequired(required),
                Agency          = ParseAgency(agencyStr),
                Gateway         = InferGateway(psetName, propName),
                ExpectedValue   = expected,
                Description     = desc,
                Regulation      = regRef
            });
        }

        // Merge into the runtime library
        int imported = 0, updated = 0;
        foreach (var kvp in codeEntries)
        {
            var existing = ClassificationCodeLibrary.Find(kvp.Key);
            if (existing == null)
            {
                imported++;
                result.ImportedCodes.Add($"NEW  {kvp.Key} - {kvp.Value.Name} ({kvp.Value.Rules.Count} rules)");
            }
            else
            {
                updated++;
                result.ImportedCodes.Add($"UPDT {kvp.Key} - {kvp.Value.Name} ({kvp.Value.Rules.Count} rules)");
            }
            result.RulesImported += kvp.Value.Rules.Count;
        }

        ClassificationCodeLibrary.MergeImported(codeEntries.Values);
        result.CodesImported += imported;
        result.CodesUpdated  += updated;
    }

    // ─── COLUMN DETECTION ────────────────────────────────────────────────────

    private sealed class ColumnMap
    {
        public int ClassificationCode { get; set; } = -1;
        public int CodeName           { get; set; } = -1;
        public int IfcClass           { get; set; } = -1;
        public int PredefinedType     { get; set; } = -1;
        public int PropertySetName    { get; set; } = -1;
        public int PropertyName       { get; set; } = -1;
        public int Agency             { get; set; } = -1;
        public int IsRequired         { get; set; } = -1;
        public int ExpectedValue      { get; set; } = -1;
        public int Description        { get; set; } = -1;
        public int Regulation         { get; set; } = -1;
        public int DataStartRow       { get; set; } = 2;

        public bool IsValid => ClassificationCode > 0 && IfcClass > 0
                            && PropertySetName > 0 && PropertyName > 0;
    }

    private static ColumnMap DetectColumns(IXLWorksheet ws)
    {
        var map = new ColumnMap();
        int lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 30;

        // Scan first 10 rows for header row
        for (int headerRow = 1; headerRow <= 10; headerRow++)
        {
            bool foundAny = false;
            for (int col = 1; col <= lastCol; col++)
            {
                var header = ws.Cell(headerRow, col).GetString().Trim().ToUpperInvariant()
                               .Replace("\n", " ").Replace("\r", "").Replace("\t", " ");
                if (string.IsNullOrWhiteSpace(header)) continue;

                // === BCA IFC+SG Dec 2025 COP3.1 EXACT COLUMN HEADERS ===
                // Col 1:  S/N
                // Col 2:  Agency
                // Col 3:  Identified Component
                // Col 4:  Identified parameters
                // Col 5:  Suggested Revit Representation
                // Col 6:  Suggested Archicad Representation
                // Col 7:  Suggested Tekla Structures Representation
                // Col 8:  Suggested Bentley OpenBuildings Representation
                // Col 9:  Suggested Discipline
                // Col 10: IFC4 Entities
                // Col 11: IFC Sub Types (* = USERDEFINED)
                // Col 12: Property Set
                // Col 13: Property Name
                // Col 14: Property Type
                // Col 15: Property Unit
                // Col 16: IFC4 Material Set
                // Col 17: Accepted Values (for parameters with Input Limitations)
                // Col 18: Sample Value for Reference

                if (header == "AGENCY" || header.Contains("AGENCY"))
                    map.Agency = col;

                if (header.Contains("IDENTIFIED COMPONENT") || header.Contains("IDENTIFIED PARAM"))
                {
                    map.CodeName = col; foundAny = true;
                    // In BCA COP3.1 format, component name IS the classification name
                }

                if (header.Contains("IFC4") && header.Contains("ENTIT") ||
                    header.Contains("IFC ENTIT") || header == "IFC4 ENTITIES" || header == "ENTITY")
                { map.IfcClass = col; foundAny = true; }

                if (header.Contains("IFC SUB TYPE") || header.Contains("SUBTYPE") || header.Contains("SUB TYPE"))
                    map.PredefinedType = col;

                if (header.Contains("PROPERTY SET") || header.Contains("PROPERTYSET") || header == "PSET")
                { map.PropertySetName = col; foundAny = true; }

                if (header.Contains("PROPERTY NAME") || (header == "PROPERTY" && map.PropertyName < 0))
                { map.PropertyName = col; foundAny = true; }

                if (header.Contains("PROPERTY TYPE") && !header.Contains("NAME"))
                    map.IsRequired = col;  // Reuse for type detection

                if (header.Contains("ACCEPTED VALUE") || header.Contains("INPUT LIMITATION"))
                    map.ExpectedValue = col;

                if (header.Contains("SAMPLE VALUE"))
                    map.Description = col;

                // Also handle older format columns (for backward compat)
                if (header.Contains("CLASSIFICATION") && header.Contains("CODE") && map.ClassificationCode < 0)
                { map.ClassificationCode = col; foundAny = true; }

                if ((header.Contains("IFC") && header.Contains("CLASS")) && map.IfcClass < 0)
                    map.IfcClass = col;
            }

            // In COP3.1 format: IFC entity + property set + property name is sufficient
            if (foundAny && map.IfcClass > 0 && map.PropertySetName > 0 && map.PropertyName > 0)
            {
                map.DataStartRow = headerRow + 1;
                // In COP3.1 format: classification code is derived from component name + index
                if (map.ClassificationCode < 0) map.ClassificationCode = map.CodeName;
                break;
            }
        }

        return map;
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static string? GetCell(IXLWorksheet ws, int row, int col)
    {
        if (col < 1) return null;
        var v = ws.Cell(row, col).GetString();
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }

    private static bool ParseRequired(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return true; // default to required
        var v = val.Trim().ToUpperInvariant();
        return v is "YES" or "Y" or "TRUE" or "MANDATORY" or "M" or "1" or "REQUIRED";
    }

    private static SgAgency ParseAgency(string? agencyStr)
    {
        if (string.IsNullOrWhiteSpace(agencyStr)) return SgAgency.BCA;
        var v = agencyStr.Trim().ToUpperInvariant();
        return v switch
        {
            "BCA"     => SgAgency.BCA,
            "URA"     => SgAgency.URA,
            "SCDF"    => SgAgency.SCDF,
            "LTA"     => SgAgency.LTA,
            "NEA"     => SgAgency.NEA,
            "PUB"     => SgAgency.PUB,
            "NPARKS"  => SgAgency.NParks,
            "SLA"     => SgAgency.SLA,
            "CIDB"    => SgAgency.CIDB,
            "JBPM"    => SgAgency.JBPM,
            _         => SgAgency.BCA
        };
    }

    private static CorenetGateway InferGateway(string psetName, string propName)
    {
        // Piling-specific properties → Piling gateway
        if (psetName.Contains("Pile", StringComparison.OrdinalIgnoreCase) ||
            propName.Contains("Pile", StringComparison.OrdinalIgnoreCase))
            return CorenetGateway.Piling;

        // Space/GFA/category properties → Design gateway
        if (psetName.Contains("GFA", StringComparison.OrdinalIgnoreCase) ||
            propName.Contains("Category", StringComparison.OrdinalIgnoreCase) ||
            propName.Contains("GrossPlanned", StringComparison.OrdinalIgnoreCase))
            return CorenetGateway.Design;

        // Default to Construction gateway
        return CorenetGateway.Construction;
    }

    private static string DetectVersion(XLWorkbook wb)
    {
        // Look for version hints in properties or first sheet title cell
        try
        {
            var firstSheet = wb.Worksheets.First();
            for (int row = 1; row <= 5; row++)
            {
                var cell = firstSheet.Cell(row, 1).GetString();
                if (cell.Contains("2025", StringComparison.OrdinalIgnoreCase))
                    return "IFC+SG Industry Mapping 2025 (COP3)";
                if (cell.Contains("2023", StringComparison.OrdinalIgnoreCase))
                    return "IFC+SG Industry Mapping 2023 (COP2)";
                if (cell.Contains("NBeS", StringComparison.OrdinalIgnoreCase))
                    return "NBeS Industry Mapping 2024";
            }
        }
        catch { }
        return "IFC+SG Industry Mapping (version unknown)";
    }
}
