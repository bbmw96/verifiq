// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// ─── IFC+SG CLASSIFICATION CODE LIBRARY ──────────────────────────────────────
//
// Comprehensive embedded library of IFC+SG classification codes based on:
//   • CORENET-X COP 3.1 Edition (December 2025)
//   • IFC+SG Industry Mapping 2025 (BCA/GovTech Singapore)
//   • Singapore Standard IFC4 Reference View ADD2 TC1
//
// Code format: {Discipline}-{ElementType}-{SubType}-{Sequence}
//   A = Architectural   S = Structural   C = Civil
//   M = Mechanical      E = Electrical   P = Plumbing/Sanitary
//   F = Foundation      L = Landscape    T = Transport
//
// For each code the library defines:
//   - The IFC entity class and PredefinedType it maps to
//   - All required property sets (Pset_ and SGPset_)
//   - All required properties within each pset
//   - Which regulatory agency requires each property
//   - The CORENET-X gateway at which it becomes mandatory
//
// When GetPropertiesForClassification() is called, it first does an EXACT
// lookup in this library. If not found, it falls back to pattern matching.
// When BCA provides an updated Industry Mapping Excel, IndustryMappingImporter
// will merge additional codes into the runtime dictionary.

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Rules.Common;

/// <summary>
/// A single IFC+SG classification code entry with all its required property rules.
/// </summary>
public sealed class ClassificationCodeEntry
{
    public string Code              { get; set; } = string.Empty;
    public string Name              { get; set; } = string.Empty;
    public string IfcClass          { get; set; } = string.Empty;
    public string PredefinedType    { get; set; } = string.Empty;
    public string Discipline        { get; set; } = string.Empty;  // A/S/C/M/E/P/F/L
    public SgAgency PrimaryAgency   { get; set; } = SgAgency.BCA;
    public List<CodePropertyRule>   Rules { get; set; } = new();
}

public sealed class CodePropertyRule
{
    public string PropertySetName   { get; set; } = string.Empty;
    public string PropertyName      { get; set; } = string.Empty;
    public bool   IsRequired        { get; set; }
    public SgAgency Agency          { get; set; }
    public CorenetGateway Gateway   { get; set; } = CorenetGateway.Construction;
    public string ExpectedValue     { get; set; } = string.Empty;  // e.g. "TRUE" for booleans
    public string Description       { get; set; } = string.Empty;
    public string Regulation        { get; set; } = string.Empty;
}

/// <summary>
/// Static library of all known IFC+SG classification codes.
/// Source: IFC+SG Industry Mapping 2025 (CORENET-X COP3, BCA/GovTech Singapore).
/// Extended from all publicly available CORENET-X documentation.
/// </summary>
public static class ClassificationCodeLibrary
{
    // Master lookup: code (upper) → entry
    private static readonly Dictionary<string, ClassificationCodeEntry> _library =
        BuildLibrary();

    public static ClassificationCodeEntry? Find(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        // Try exact match first, then prefix match (first 3 segments)
        var upper = code.Trim().ToUpperInvariant();
        if (_library.TryGetValue(upper, out var entry)) return entry;
        // Match on first 3 dash-segments: A-WAL-EXW
        var parts = upper.Split('-');
        if (parts.Length >= 3)
        {
            var prefix = $"{parts[0]}-{parts[1]}-{parts[2]}";
            if (_library.TryGetValue(prefix, out var prefixEntry)) return prefixEntry;
        }
        return null;
    }

    public static IEnumerable<ClassificationCodeEntry> All => _library.Values;

    /// <summary>
    /// Merges entries imported from the BCA Industry Mapping Excel into the runtime library.
    /// New codes are added; existing codes are updated if the Excel entry is more specific.
    /// </summary>
    public static void MergeImported(IEnumerable<ClassificationCodeEntry> imported)
    {
        foreach (var entry in imported)
        {
            var key = entry.Code.Trim().ToUpperInvariant();
            _library[key] = entry;  // overwrite or add
        }
    }

    // ─── LIBRARY BUILD ───────────────────────────────────────────────────────

    private static Dictionary<string, ClassificationCodeEntry> BuildLibrary()
    {
        var d = new Dictionary<string, ClassificationCodeEntry>(StringComparer.OrdinalIgnoreCase);

        void Add(ClassificationCodeEntry e) => d[e.Code.ToUpperInvariant()] = e;

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - WALLS (A-WAL-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-WAL-EXW", "External Wall", "IFCWALL", "SOLIDWALL", "A", SgAgency.BCA, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "Mark as external wall",              "IFC+SG COP3"),
            R("Pset_WallCommon",       "LoadBearing",          false, SgAgency.BCA,  GCon, "",      "TRUE if load-bearing",               "BC 2:2021"),
            R("SGPset_WallThermal",    "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "<=0.50","U-value W/m²K, max 0.50 per Green Mark 2021", "BCA Green Mark 2021 §3.4"),
            R("SGPset_WallThermal",    "SolarAbsorptance",     false, SgAgency.BCA,  GCon, "0.0-1.0","Solar absorptance for ETTV",        "BCA Green Mark 2021 §3.4"),
            R("SGPset_WallAcoustic",   "AcousticRating",       false, SgAgency.BCA,  GCon, "",      "STC rating if required",             "BCA Green Mark 2021"),
        }));

        Add(E("A-WAL-INW", "Internal Non-Load-Bearing Wall", "IFCWALL", "SOLIDWALL", "A", SgAgency.BCA, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Mark as internal wall",              "IFC+SG COP3"),
            R("Pset_WallCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "FALSE", "Non-load-bearing",                   "BC 2:2021"),
        }));

        Add(E("A-WAL-LBW", "Load-Bearing Internal Wall", "IFCWALL", "SOLIDWALL", "A", SgAgency.BCA, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Internal load-bearing wall",         "IFC+SG COP3"),
            R("Pset_WallCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Mark as load-bearing",               "BC 2:2021"),
        }));

        Add(E("A-WAL-PRW", "Party Wall / Shared Wall", "IFCWALL", "SOLIDWALL", "A", SgAgency.SCDF, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Party walls are internal between units", "IFC+SG COP3"),
            R("Pset_WallCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "",      "TRUE if structural",                 "BC 2:2021"),
            R("SGPset_WallFireRating", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "Min 60 min FRR for party walls - SCDF Fire Code 2018 Table 4.2", "SCDF Fire Code 2018 Table 4.2"),
            R("SGPset_WallFireRating", "FireTestStandard",     true,  SgAgency.SCDF, GCon, "SS 332","Fire test standard",                 "SCDF Fire Code 2018 §4.3"),
        }));

        Add(E("A-WAL-FRW", "Fire-Rated Wall", "IFCWALL", "SOLIDWALL", "A", SgAgency.SCDF, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE depending on location","IFC+SG COP3"),
            R("SGPset_WallFireRating", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "REI in minutes per SCDF Fire Code 2018 Table 4.2", "SCDF Fire Code 2018"),
            R("SGPset_WallFireRating", "FireTestStandard",     true,  SgAgency.SCDF, GCon, "",      "SS 332 / BS 476 / EN 13501",        "SCDF Fire Code 2018 §4.3"),
            R("SGPset_WallFireRating", "FireRatingCertificate",false, SgAgency.SCDF, GCon, "",      "Certificate reference number",       "SCDF"),
        }));

        Add(E("A-WAL-CWT", "Curtain Wall", "IFCCURTAINWALL", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Pset_CurtainWallCommon","IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "Curtain walls are external",         "IFC+SG COP3"),
            R("SGPset_WallThermal",    "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "<=0.50","Whole-system U-value",               "BCA Green Mark 2021"),
            R("SGPset_WallThermal",    "SolarAbsorptance",     false, SgAgency.BCA,  GCon, "",      "For ETTV calculation",               "BCA Green Mark 2021"),
        }));

        Add(E("A-WAL-RTW", "Retaining Wall (Architectural)", "IFCWALL", "RETAININGWALL", "A", SgAgency.BCA, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "Retaining wall is external element", "IFC+SG COP3"),
            R("Pset_WallCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Retaining walls are structural",     "BC 2:2021"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - SLABS (A-SLB-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-SLB-ROF", "Roof Slab", "IFCSLAB", "ROOF", "A", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "Roof slabs are external",            "IFC+SG COP3"),
            R("Pset_SlabCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Roof slabs are structural",          "BC 2:2021"),
            R("SGPset_RoofThermal",    "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "<=0.35","Roof U-value max 0.35 W/m²K",        "BCA Green Mark 2021 §3.4.1"),
            R("SGPset_SlabFireRating", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "Roof fire resistance per SCDF",      "SCDF Fire Code 2018"),
        }));

        Add(E("A-SLB-FLR", "Floor Slab", "IFCSLAB", "FLOOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Internal floor slab",                "IFC+SG COP3"),
            R("Pset_SlabCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Floor slabs are structural",         "BC 2:2021"),
        }));

        Add(E("A-SLB-FRS", "Fire-Rated Floor Slab (Compartment Boundary)", "IFCSLAB", "FLOOR", "A", SgAgency.SCDF, new[]
        {
            R("Pset_SlabCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Internal slab",                      "IFC+SG COP3"),
            R("Pset_SlabCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural floor slab",              "BC 2:2021"),
            R("SGPset_SlabFireRating", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "Min 60 min REI for fire compartment floor", "SCDF Fire Code 2018 Table 7.2"),
            R("SGPset_SlabFireRating", "FireTestStandard",     true,  SgAgency.SCDF, GCon, "",      "Fire test standard reference",       "SCDF Fire Code 2018 §4.3"),
        }));

        Add(E("A-SLB-RMP", "Ramp Slab", "IFCSLAB", "FLOOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural ramp slab",               "BC 2:2021"),
        }));

        Add(E("A-SLB-PTH", "Transfer/Podium Slab", "IFCSLAB", "FLOOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Transfer slab is structural",        "BC 2:2021"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - DOORS (A-DOR-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-DOR-EXT", "External Door", "IFCDOOR", "DOOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_DoorCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "External door",                      "IFC+SG COP3"),
            R("Pset_DoorCommon",       "HandicapAccessible",   true,  SgAgency.BCA,  GCon, "",      "TRUE if on accessible route (BFA)",  "Code on Accessibility 2025 §4.2"),
        }));

        Add(E("A-DOR-INT", "Internal Door", "IFCDOOR", "DOOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_DoorCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Internal door",                      "IFC+SG COP3"),
            R("Pset_DoorCommon",       "HandicapAccessible",   true,  SgAgency.BCA,  GCon, "",      "TRUE if on accessible route",        "Code on Accessibility 2025"),
        }));

        Add(E("A-DOR-FRD", "Fire Door / Fire-Rated Door", "IFCDOOR", "DOOR", "A", SgAgency.SCDF, new[]
        {
            R("Pset_DoorCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
            R("Pset_DoorCommon",       "FireExit",             true,  SgAgency.SCDF, GCon, "TRUE",  "Fire door on escape route",          "SCDF Fire Code 2018 §5.2"),
            R("Pset_DoorCommon",       "FireRating",           true,  SgAgency.SCDF, GCon, "FD30",  "Fire door rating e.g. FD30, FD60, FD120", "SCDF Fire Code 2018"),
            R("Pset_DoorCommon",       "SmokeStop",            true,  SgAgency.SCDF, GCon, "TRUE",  "Smoke stop rated door",              "SCDF Fire Code 2018 §4.3"),
            R("SGPset_DoorFireDoor",   "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "30",    "Min 30 min FRR for fire doors",      "SCDF Fire Code 2018 Table 4.2"),
            R("SGPset_DoorFireDoor",   "FireTestStandard",     true,  SgAgency.SCDF, GCon, "",      "Fire test certificate standard",     "SCDF"),
        }));

        Add(E("A-DOR-ACS", "Accessible Door (BFA Compliant)", "IFCDOOR", "DOOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_DoorCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
            R("Pset_DoorCommon",       "HandicapAccessible",   true,  SgAgency.BCA,  GCon, "TRUE",  "BFA accessible door",                "Code on Accessibility 2025 §4.2"),
            R("SGPset_DoorAccessibility","ClearWidth",         true,  SgAgency.BCA,  GCon, ">=0.850","Min 850mm clear width",             "Code on Accessibility 2025 §4.2.1"),
            R("SGPset_DoorAccessibility","OpeningForce",       false, SgAgency.BCA,  GCon, "<=22.2","Max 22.2N opening force",            "Code on Accessibility 2025 §4.2.4"),
        }));

        Add(E("A-DOR-EXD", "Exit Door / Emergency Exit", "IFCDOOR", "DOOR", "A", SgAgency.SCDF, new[]
        {
            R("Pset_DoorCommon",       "FireExit",             true,  SgAgency.SCDF, GCon, "TRUE",  "Emergency exit door",                "SCDF Fire Code 2018 §9.3"),
            R("Pset_DoorCommon",       "SmokeStop",            true,  SgAgency.SCDF, GCon, "",      "Smoke stop if required by code",     "SCDF Fire Code 2018"),
        }));

        Add(E("A-DOR-SLD", "Sliding Door", "IFCDOOR", "SLIDING", "A", SgAgency.BCA, new[]
        {
            R("Pset_DoorCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
            R("Pset_DoorCommon",       "HandicapAccessible",   true,  SgAgency.BCA,  GCon, "",      "TRUE if on accessible route",        "Code on Accessibility 2025"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - WINDOWS (A-WIN-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-WIN-EXW", "External Window", "IFCWINDOW", "WINDOW", "A", SgAgency.BCA, new[]
        {
            R("Pset_WindowCommon",     "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "External window",                    "IFC+SG COP3"),
            R("Pset_WindowCommon",     "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "",      "Whole-window U-value W/m²K",         "BCA Green Mark 2021"),
            R("SGPset_WindowPerformance","SolarHeatGainCoefficient", true, SgAgency.BCA, GCon, "<=0.30","SHGC max 0.30",                 "BCA Green Mark 2021 §3.4.3"),
            R("SGPset_WindowPerformance","VisibleLightTransmittance",false,SgAgency.BCA, GCon, ">=0.20","VLT min 0.20 recommended",       "NEA - natural lighting"),
            R("SGPset_WindowFireRating","FireRating",          false, SgAgency.SCDF, GCon, "",      "Fire rating if in fire-rated wall",  "SCDF Fire Code 2018"),
        }));

        Add(E("A-WIN-SKL", "Skylight / Roof Window", "IFCWINDOW", "SKYLIGHT", "A", SgAgency.BCA, new[]
        {
            R("Pset_WindowCommon",     "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "Skylight is external",               "IFC+SG COP3"),
            R("Pset_WindowCommon",     "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "",      "Skylight U-value",                   "BCA Green Mark 2021"),
            R("SGPset_WindowPerformance","SolarHeatGainCoefficient",true,SgAgency.BCA,GCon,"<=0.30","SHGC for roof daylighting elements","BCA Green Mark 2021"),
        }));

        Add(E("A-WIN-CWT", "Curtain Wall Window Panel", "IFCWINDOW", "WINDOW", "A", SgAgency.BCA, new[]
        {
            R("Pset_WindowCommon",     "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "Curtain wall panel is external",     "IFC+SG COP3"),
            R("Pset_WindowCommon",     "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "",      "Panel U-value",                      "BCA Green Mark 2021"),
            R("SGPset_WindowPerformance","SolarHeatGainCoefficient",true,SgAgency.BCA,GCon,"<=0.30","SHGC",                              "BCA Green Mark 2021"),
        }));

        Add(E("A-WIN-FRW", "Fire-Rated Window", "IFCWINDOW", "WINDOW", "A", SgAgency.SCDF, new[]
        {
            R("Pset_WindowCommon",     "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
            R("SGPset_WindowFireRating","FireRating",          true,  SgAgency.SCDF, GCon, "",      "Fire rating REI in minutes",         "SCDF Fire Code 2018"),
            R("SGPset_WindowFireRating","FireTestStandard",    true,  SgAgency.SCDF, GCon, "",      "Fire test standard",                 "SCDF"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - SPACES (A-SPC-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-SPC-LVN", "Living / Dining Space", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "LIVING_ROOM","Space use category",            "URA Planning Parameters 2023"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, ">=13.0","Min 13.0m² (private) or 16.0m² (HDB)", "URA Handbook 2023 §2.3"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "RESIDENTIAL","GFA category for URA computation","IFC+SG COP3 - SGPset_SpaceGFA"),
            R("Pset_SpaceCommon",      "Height",               true,  SgAgency.BCA,  GCon, ">=2.4", "Min 2.4m ceiling height for habitable rooms", "Building Control Regs 2003 §4"),
        }));

        Add(E("A-SPC-BED", "Bedroom", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "BEDROOM","Space category",                   "URA Handbook 2023"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, ">=9.0", "Min 9.0m² per bedroom",              "URA Handbook 2023 §3.1"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "RESIDENTIAL","GFA category",                 "IFC+SG COP3"),
            R("Pset_SpaceCommon",      "Height",               true,  SgAgency.BCA,  GCon, ">=2.4", "Min 2.4m ceiling height",            "Building Control Regs 2003 §4"),
        }));

        Add(E("A-SPC-MBD", "Master Bedroom", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "MASTER_BEDROOM","Space category",            "URA Handbook 2023"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, ">=12.5","Min 12.5m² for master bedroom",      "URA Handbook 2023 §3.2"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "RESIDENTIAL","GFA category",                 "IFC+SG COP3"),
        }));

        Add(E("A-SPC-KIT", "Kitchen", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "KITCHEN","Space category",                   "URA Planning Parameters 2023"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, ">=4.5", "Min 4.5m² with min 1500mm width",    "URA Handbook 2023 §3.3"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "RESIDENTIAL","GFA category",                 "IFC+SG COP3"),
        }));

        Add(E("A-SPC-BTH", "Bathroom / Toilet", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "BATHROOM","Space category",                  "URA Planning Parameters 2023"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, ">=2.5", "Min 2.5m²",                          "URA Handbook 2023 §3.5"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "RESIDENTIAL","GFA category",                 "IFC+SG COP3"),
        }));

        Add(E("A-SPC-ACC", "Accessible Toilet (BFA)", "IFCSPACE", "INTERNAL", "A", SgAgency.BCA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "ACCESSIBLE_TOILET","Space category",         "Code on Accessibility 2025"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.BCA,  GCon, ">=2.7", "Min 1800mm x 1500mm (2.7m²)",        "Code on Accessibility 2025 §4.2.2"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "RESIDENTIAL","GFA category",                 "IFC+SG COP3"),
        }));

        Add(E("A-SPC-BAL", "Balcony", "IFCSPACE", "EXTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "BALCONY","Space category",                   "URA Guidelines on Balconies 2019"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, "",      "Balcony area - max 10% of total GFA","URA Circular Nov 2019"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "BALCONY","GFA category - balcony counted separately", "URA GFA Rules"),
        }));

        Add(E("A-SPC-CPK", "Carpark Space", "IFCSPACE", "INTERNAL", "A", SgAgency.LTA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "CARPARK","Space category",                   "URA / LTA"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.LTA,  GCon, ">=12.0","Min 2400mm x 4800mm = 11.52m²",      "LTA Code of Practice for Parking §3.2.1"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "CARPARK","GFA exempt if mechanical carpark",  "URA GFA Rules"),
        }));

        Add(E("A-SPC-PLT", "Plant Room / Mechanical Room", "IFCSPACE", "INTERNAL", "A", SgAgency.BCA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "PLANT_ROOM","Space category",                "IFC+SG COP3"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, "",      "Plant room area for GFA assessment", "URA GFA Rules"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "PLANT_ROOM","GFA may be exempt",            "URA GFA Rules"),
        }));

        Add(E("A-SPC-OFF", "Office Space", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "OFFICE","Space category",                    "URA Planning Parameters 2023"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, "",      "GFA for URA computation",            "URA DC Handbook"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "OFFICE","GFA category",                     "IFC+SG COP3"),
            R("Pset_SpaceCommon",      "Height",               true,  SgAgency.BCA,  GCon, ">=2.6", "Min 2.6m for office spaces",         "Building Control Regs 2003 §5"),
        }));

        Add(E("A-SPC-RET", "Retail / Shop Space", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "RETAIL","Space category",                    "URA Planning Parameters 2023"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.URA,  GDes, "",      "GFA for URA computation",            "URA DC Handbook"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "RETAIL","GFA category",                     "IFC+SG COP3"),
        }));

        Add(E("A-SPC-COR", "Corridor / Circulation", "IFCSPACE", "INTERNAL", "A", SgAgency.BCA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "CORRIDOR","Space category",                  "IFC+SG COP3"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "CIRCULATION","GFA category",                "URA GFA Rules"),
        }));

        Add(E("A-SPC-LBY", "Lift Lobby", "IFCSPACE", "INTERNAL", "A", SgAgency.BCA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "LIFT_LOBBY","Space category",                "IFC+SG COP3"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "CIRCULATION","GFA category",                "URA GFA Rules"),
        }));

        Add(E("A-SPC-STW", "Stairwell / Stair Enclosure", "IFCSPACE", "INTERNAL", "A", SgAgency.SCDF, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "STAIRCASE","Space category",                 "IFC+SG COP3"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "CIRCULATION","GFA category - stairwells typically exempt", "URA GFA Rules"),
        }));

        Add(E("A-SPC-VDD", "Void / Atrium", "IFCSPACE", "INTERNAL", "A", SgAgency.URA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "VOID","Space category",                      "IFC+SG COP3"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "VOID","GFA exempt if qualifying void",       "URA GFA Handbook"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - STAIRS & RAMPS (A-STR-*, A-RMP-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-STR-FES", "Fire Escape Stair", "IFCSTAIR", "STRAIGHT", "A", SgAgency.SCDF, new[]
        {
            R("Pset_StairCommon",      "FireExit",             true,  SgAgency.SCDF, GCon, "TRUE",  "Mark as fire escape stair",          "SCDF Fire Code 2018 §9.4"),
            R("SGPset_StairFireEscape","Width",                true,  SgAgency.SCDF, GCon, ">=1050","Min 1050mm clear width",             "SCDF Fire Code 2018 §9.4.1"),
            R("SGPset_StairFireEscape","FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "FRR of stair enclosure",             "SCDF Fire Code 2018"),
        }));

        Add(E("A-STR-INT", "Internal Stair", "IFCSTAIR", "STRAIGHT", "A", SgAgency.BCA, new[]
        {
            R("Pset_StairFlightCommon","RiserHeight",          true,  SgAgency.BCA,  GCon, "<=0.175","Max 175mm riser height",            "Building Control Regs 2003 §8.1"),
            R("Pset_StairFlightCommon","TreadLength",          true,  SgAgency.BCA,  GCon, ">=0.250","Min 250mm tread depth",             "Building Control Regs 2003 §8.2"),
            R("SGPset_StairAccessibility","Width",             true,  SgAgency.BCA,  GCon, ">=1000","Min 1000mm for accessible stairs",   "Code on Accessibility 2025"),
        }));

        Add(E("A-RMP-ACS", "Accessible Ramp (BFA)", "IFCRAMP", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Pset_RampCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
            R("SGPset_RampAccessibility","Gradient",           true,  SgAgency.BCA,  GCon, "<=0.0833","Max 1:12 gradient (0.0833)",       "Code on Accessibility 2025 §4.3"),
            R("SGPset_RampAccessibility","Width",              true,  SgAgency.BCA,  GCon, ">=1.200","Min 1200mm width",                  "Code on Accessibility 2025 §4.3.1"),
        }));

        Add(E("A-RMP-VHC", "Vehicular Ramp", "IFCRAMP", "NOTDEFINED", "A", SgAgency.LTA, new[]
        {
            R("Pset_RampCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - ROOFS (A-ROF-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-ROF-FLT", "Flat Roof", "IFCROOF", "FLAT_ROOF", "A", SgAgency.BCA, new[]
        {
            R("Pset_RoofCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "Roof is external",                   "IFC+SG COP3"),
            R("SGPset_RoofThermal",    "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "<=0.35","Max U-value 0.35 W/m²K",             "BCA Green Mark 2021 §3.4.1"),
            R("SGPset_RoofFireRating", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "Fire resistance per SCDF",           "SCDF Fire Code 2018"),
        }));

        Add(E("A-ROF-GRN", "Green Roof", "IFCROOF", "FLAT_ROOF", "A", SgAgency.NParks, new[]
        {
            R("Pset_RoofCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "External green roof",                "IFC+SG COP3"),
            R("SGPset_RoofThermal",    "ThermalTransmittance", true,  SgAgency.BCA,  GCon, "<=0.35","U-value even for green roof",        "BCA Green Mark 2021"),
            R("SGPset_GreenRoof",      "SubstrateDepth",       false, SgAgency.NParks, GCon,"",     "Growing substrate depth in mm",      "NParks - Landscaping Requirements"),
            R("SGPset_GreenRoof",      "PlantingArea",         false, SgAgency.NParks, GCon,"",     "Green area in m² for LUSH credits",  "NParks LUSH Programme"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // STRUCTURAL - COLUMNS (S-COL-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("S-COL-RCC", "Reinforced Concrete Column", "IFCCOLUMN", "COLUMN", "S", SgAgency.BCA, new[]
        {
            R("Pset_ColumnCommon",     "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "RC columns are structural",          "BC 2:2021"),
            R("SGPset_ColumnStructural","ConcreteGrade",       true,  SgAgency.BCA,  GCon, "C32/40","Concrete grade per SS EN 1992",      "BC 2:2021 / SS EN 1992-1-1 §3.1"),
            R("SGPset_ColumnStructural","DesignCode",          true,  SgAgency.BCA,  GCon, "BC2:2021","Design code used",                 "Building Control Act / BC 2:2021"),
            R("SGPset_ColumnStructural","FireResistancePeriod",true,  SgAgency.SCDF, GCon, "60",    "Fire resistance per SCDF Table 4.2","SCDF Fire Code 2018 Table 4.2"),
        }));

        Add(E("S-COL-STL", "Steel Column", "IFCCOLUMN", "COLUMN", "S", SgAgency.BCA, new[]
        {
            R("Pset_ColumnCommon",     "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural steel column",            "BC 1:2012"),
            R("SGPset_ColumnStructural","SteelGrade",          true,  SgAgency.BCA,  GCon, "S275",  "Steel grade per SS EN 1993",         "BC 1:2012 / SS EN 1993-1-1"),
            R("SGPset_ColumnStructural","DesignCode",          true,  SgAgency.BCA,  GCon, "BC1:2012","Design code",                      "Building Control Act"),
            R("SGPset_ColumnStructural","FireResistancePeriod",true,  SgAgency.SCDF, GCon, "60",    "Fire protection of steel column",    "SCDF Fire Code 2018"),
            R("SGPset_ColumnStructural","FireProtectionType",  false, SgAgency.SCDF, GCon, "",      "Intumescent / board / spray",        "SCDF"),
        }));

        Add(E("S-COL-COM", "Composite Column", "IFCCOLUMN", "COLUMN", "S", SgAgency.BCA, new[]
        {
            R("Pset_ColumnCommon",     "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural composite column",        "BC 1:2012"),
            R("SGPset_ColumnStructural","DesignCode",          true,  SgAgency.BCA,  GCon, "",      "BC1 or BC2 depending on type",       "Building Control Act"),
            R("SGPset_ColumnStructural","FireResistancePeriod",true,  SgAgency.SCDF, GCon, "60",    "Fire resistance",                    "SCDF Fire Code 2018"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // STRUCTURAL - BEAMS (S-BEM-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("S-BEM-RCC", "Reinforced Concrete Beam", "IFCBEAM", "BEAM", "S", SgAgency.BCA, new[]
        {
            R("Pset_BeamCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "RC beam is structural",              "BC 2:2021"),
            R("SGPset_BeamStructural", "ConcreteGrade",        true,  SgAgency.BCA,  GCon, "C32/40","Concrete grade per SS EN 1992",      "BC 2:2021 / SS EN 1992-1-1"),
            R("SGPset_BeamStructural", "DesignCode",           true,  SgAgency.BCA,  GCon, "BC2:2021","Design code",                      "Building Control Act / BC 2:2021"),
            R("SGPset_BeamStructural", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "Fire resistance per SCDF",           "SCDF Fire Code 2018 Table 4.2"),
        }));

        Add(E("S-BEM-STL", "Steel Beam", "IFCBEAM", "BEAM", "S", SgAgency.BCA, new[]
        {
            R("Pset_BeamCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural steel beam",              "BC 1:2012"),
            R("SGPset_BeamStructural", "SteelGrade",           true,  SgAgency.BCA,  GCon, "S275",  "Steel grade",                        "BC 1:2012 / SS EN 1993-1-1"),
            R("SGPset_BeamStructural", "DesignCode",           true,  SgAgency.BCA,  GCon, "BC1:2012","Design code",                      "Building Control Act"),
            R("SGPset_BeamStructural", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "60",    "Fire protection of steel beam",      "SCDF Fire Code 2018"),
        }));

        Add(E("S-BEM-TRF", "Transfer Beam", "IFCBEAM", "BEAM", "S", SgAgency.BCA, new[]
        {
            R("Pset_BeamCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Transfer beam is critical structural", "BC 2:2021"),
            R("SGPset_BeamStructural", "ConcreteGrade",        true,  SgAgency.BCA,  GCon, "C40/50","High-grade concrete for transfer beams","BC 2:2021"),
            R("SGPset_BeamStructural", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "120",   "Transfer beams typically need 2hr FRR","SCDF Fire Code 2018"),
        }));

        Add(E("S-BEM-PTB", "Post-Tensioned Beam", "IFCBEAM", "BEAM", "S", SgAgency.BCA, new[]
        {
            R("Pset_BeamCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "PT beam is structural",              "BC 2:2021"),
            R("SGPset_BeamStructural", "ConcreteGrade",        true,  SgAgency.BCA,  GCon, "C35/45","Concrete grade for PT",              "BC 2:2021"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // STRUCTURAL - SLABS (S-SLB-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("S-SLB-RCC", "Reinforced Concrete Floor Slab", "IFCSLAB", "FLOOR", "S", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "RC slab is structural",              "BC 2:2021"),
            R("SGPset_SlabStructural", "ConcreteGrade",        true,  SgAgency.BCA,  GCon, "C32/40","Concrete grade",                     "BC 2:2021 / SS EN 1992-1-1"),
            R("SGPset_SlabStructural", "Thickness",            true,  SgAgency.BCA,  GCon, ">=0.125","Min 125mm for residential",         "BC 2:2008 §5.3.1"),
        }));

        Add(E("S-SLB-PTN", "Post-Tensioned Slab", "IFCSLAB", "FLOOR", "S", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "PT slab is structural",              "BC 2:2021"),
            R("SGPset_SlabStructural", "ConcreteGrade",        true,  SgAgency.BCA,  GCon, "C35/45","Concrete grade for PT slab",         "BC 2:2021"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // STRUCTURAL - WALLS (S-WAL-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("S-WAL-CRW", "Shear Wall / Core Wall", "IFCWALL", "SHEAR", "S", SgAgency.BCA, new[]
        {
            R("Pset_WallCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Shear wall is structural",           "BC 2:2021"),
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Core walls are internal",            "IFC+SG COP3"),
            R("SGPset_WallFireRating", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "120",   "Core walls typically need 2hr FRR",  "SCDF Fire Code 2018"),
        }));

        Add(E("S-WAL-RCW", "Reinforced Concrete Wall", "IFCWALL", "SOLIDWALL", "S", SgAgency.BCA, new[]
        {
            R("Pset_WallCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "RC wall is structural",              "BC 2:2021"),
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // STRUCTURAL - FOUNDATIONS (S-FTG-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("S-FTG-PAD", "Pad Footing", "IFCFOOTING", "PAD_FOOTING", "S", SgAgency.BCA, new[]
        {
            R("Pset_FootingCommon",    "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural foundation",              "BC 2:2021"),
            R("SGPset_FootingFoundation","ConcreteGrade",      true,  SgAgency.BCA,  GCon, "C30/37","Concrete grade for foundation",      "BC 2:2021"),
        }));

        Add(E("S-FTG-MAT", "Mat / Raft Foundation", "IFCFOOTING", "STRIP_FOOTING", "S", SgAgency.BCA, new[]
        {
            R("Pset_FootingCommon",    "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Raft foundation is structural",      "BC 2:2021"),
            R("SGPset_FootingFoundation","ConcreteGrade",      true,  SgAgency.BCA,  GCon, "C30/37","Concrete grade",                     "BC 2:2021"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // STRUCTURAL - PILES (S-PIL-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("S-PIL-BOR", "Bored Pile", "IFCPILE", "BORED", "S", SgAgency.BCA, new[]
        {
            R("Pset_PileCommon",       "LoadBearing",          true,  SgAgency.BCA,  GPil, "TRUE",  "Structural pile",                    "BC 3:2013"),
            R("SGPset_PileFoundation", "PileType",             true,  SgAgency.BCA,  GPil, "BORED", "Pile type declaration",              "BC 3:2013 / Building Control Regs 2003 Reg 12"),
            R("SGPset_PileFoundation", "DesignLoad",           true,  SgAgency.BCA,  GPil, "",      "Design load per pile in kN",         "BC 3:2013"),
            R("SGPset_PileFoundation", "PileLength",           true,  SgAgency.BCA,  GPil, "",      "Pile length in metres",              "BC 3:2013"),
            R("SGPset_PileFoundation", "PileDiameter",         true,  SgAgency.BCA,  GPil, "",      "Pile diameter in metres",            "BC 3:2013"),
        }));

        Add(E("S-PIL-DRV", "Driven Pile", "IFCPILE", "DRIVEN", "S", SgAgency.BCA, new[]
        {
            R("Pset_PileCommon",       "LoadBearing",          true,  SgAgency.BCA,  GPil, "TRUE",  "Structural pile",                    "BC 3:2013"),
            R("SGPset_PileFoundation", "PileType",             true,  SgAgency.BCA,  GPil, "DRIVEN","Pile type declaration",              "BC 3:2013"),
            R("SGPset_PileFoundation", "DesignLoad",           true,  SgAgency.BCA,  GPil, "",      "Design load per pile in kN",         "BC 3:2013"),
            R("SGPset_PileFoundation", "PileLength",           true,  SgAgency.BCA,  GPil, "",      "Pile length in metres",              "BC 3:2013"),
        }));

        Add(E("S-PIL-MCR", "Micropile", "IFCPILE", "DRIVEN", "S", SgAgency.BCA, new[]
        {
            R("Pset_PileCommon",       "LoadBearing",          true,  SgAgency.BCA,  GPil, "TRUE",  "Structural micropile",               "BC 3:2013"),
            R("SGPset_PileFoundation", "PileType",             true,  SgAgency.BCA,  GPil, "MICROPILE","Pile type",                      "BC 3:2013"),
            R("SGPset_PileFoundation", "DesignLoad",           true,  SgAgency.BCA,  GPil, "",      "Design load in kN",                  "BC 3:2013"),
        }));

        Add(E("S-PIL-JGP", "Jet Grouting Pile", "IFCPILE", "JETGROUTING", "S", SgAgency.BCA, new[]
        {
            R("Pset_PileCommon",       "LoadBearing",          true,  SgAgency.BCA,  GPil, "TRUE",  "Structural pile",                    "BC 3:2013"),
            R("SGPset_PileFoundation", "PileType",             true,  SgAgency.BCA,  GPil, "JETGROUTING","Pile type",                    "BC 3:2013"),
            R("SGPset_PileFoundation", "DesignLoad",           true,  SgAgency.BCA,  GPil, "",      "Design load in kN",                  "BC 3:2013"),
        }));

        Add(E("S-PIL-SHP", "Sheet Pile (Temporary/Permanent)", "IFCPILE", "DRIVEN", "S", SgAgency.BCA, new[]
        {
            R("Pset_PileCommon",       "LoadBearing",          true,  SgAgency.BCA,  GPil, "TRUE",  "Sheet pile for earth retention",     "BC 3:2013"),
            R("SGPset_PileFoundation", "PileType",             true,  SgAgency.BCA,  GPil, "SHEETPILE","Sheet pile type",                "BC 3:2013"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // SITE (A-SIT-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-SIT-GND", "Site / Ground Plane", "IFCSITE", "NOTDEFINED", "A", SgAgency.SLA, new[]
        {
            R("Pset_SiteCommon",       "SiteID",               false, SgAgency.SLA,  GDes, "",      "Singapore Land Authority site lot number e.g. MK22-01234X", "SLA Land Registry"),
            R("Pset_SiteCommon",       "RefElevation",         false, SgAgency.SLA,  GDes, "",      "Site datum in Singapore Height Datum (SHD) metres", "CORENET-X COP §3 - CRS"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // LANDSCAPE / GREENERY (L-GRN-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("L-GRN-LRA", "Landscaping Replacement Area", "IFCSPACE", "EXTERNAL", "L", SgAgency.NParks, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "LANDSCAPING","Space category",               "NParks / URA LUSH"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.NParks, GDes,"",     "Landscape area for LRA computation", "NParks Landscape Plan Requirements"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "LANDSCAPING","GFA exempt",                  "URA GFA Rules"),
        }));

        Add(E("L-GRN-SKY", "Sky Garden / Communal Garden", "IFCSPACE", "EXTERNAL", "L", SgAgency.NParks, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "SKY_GARDEN","Space category",                "URA / NParks LUSH"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.NParks, GDes,"",     "Sky garden area",                   "NParks"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "COMMUNAL_GARDEN","GFA exemption status",    "URA GFA Rules"),
        }));


        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - COLUMNS / CEILINGS / FLOORING (A-COL-*, A-CLG-*, A-FLR-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-COL-EXP", "Exposed Architectural Column", "IFCCOLUMN", "COLUMN", "A", SgAgency.BCA, new[]
        {
            R("Pset_ColumnCommon",     "LoadBearing",          true,  SgAgency.BCA,  GCon, "",      "TRUE if structural",                 "IFC+SG COP3"),
            R("Pset_ColumnCommon",     "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE if external element",           "IFC+SG COP3"),
        }));

        Add(E("A-CLG-SUS", "Suspended Ceiling", "IFCCOVERING", "CEILING", "A", SgAgency.BCA, new[]
        {
            R("Pset_CoveringCommon",   "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Ceiling is internal",                "IFC+SG COP3"),
            R("SGPset_CeilingAcoustic","AcousticRating",       false, SgAgency.BCA,  GCon, "",      "NRC/CAC rating if specified",        "BCA Green Mark 2021"),
        }));

        Add(E("A-CLG-EXP", "Exposed Structure Ceiling (No Ceiling Finish)", "IFCSLAB", "FLOOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "FALSE", "Exposed soffit - internal",          "IFC+SG COP3"),
        }));

        Add(E("A-FLR-FNS", "Floor Finish / Screed", "IFCCOVERING", "FLOORING", "A", SgAgency.BCA, new[]
        {
            R("Pset_CoveringCommon",   "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE if external/wet area",          "IFC+SG COP3"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - RAILINGS & BALUSTRADES (A-RLG-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-RLG-BLS", "Balustrade / Guardrail", "IFCRAILING", "GUARDRAIL", "A", SgAgency.BCA, new[]
        {
            R("Pset_RailingCommon",    "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE if external balcony/roof",      "IFC+SG COP3"),
            R("SGPset_RailingBarrier", "Height",               true,  SgAgency.BCA,  GCon, ">=1.000","Min 1000mm height for balconies",   "Building Control Regs 2003 §9"),
        }));

        Add(E("A-RLG-HND", "Handrail", "IFCRAILING", "HANDRAIL", "A", SgAgency.BCA, new[]
        {
            R("Pset_RailingCommon",    "IsExternal",           true,  SgAgency.BCA,  GCon, "",      "TRUE or FALSE",                      "IFC+SG COP3"),
            R("SGPset_RailingAccessibility","Height",          true,  SgAgency.BCA,  GCon, "0.850-0.950","Handrail height 850-950mm AFF",  "Code on Accessibility 2025 §4.4"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - SHADING / SOLAR DEVICES (A-SHD-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-SHD-FIN", "Solar Shading Fin / Louvre", "IFCBUILDINGELEMENTPROXY", "USERDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_ShadingDevice",  "ShadingCoefficient",   false, SgAgency.BCA,  GCon, "",      "Shading coefficient for ETTV",       "BCA Green Mark 2021"),
            R("SGPset_ShadingDevice",  "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "External shading device",            "IFC+SG COP3"),
        }));

        Add(E("A-SHD-OVH", "Overhang / Canopy (Solar Control)", "IFCSLAB", "USERDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Pset_SlabCommon",       "IsExternal",           true,  SgAgency.BCA,  GCon, "TRUE",  "External overhang",                  "IFC+SG COP3"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ARCHITECTURAL - LIFTS & ESCALATORS (A-LFT-*, A-ESC-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("A-LFT-PAS", "Passenger Lift / Elevator", "IFCTRANSPORTELEMENT", "ELEVATOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_TransportElementCommon","CapacityPeopleMax",true, SgAgency.BCA,  GCon, "",      "Lift car capacity persons",          "BCA Green Mark 2021"),
            R("SGPset_LiftAccessibility","CarInternalWidth",   true,  SgAgency.BCA,  GCon, ">=1.100","Min 1100mm for accessible lift",    "Code on Accessibility 2025 §4.6"),
            R("SGPset_LiftAccessibility","CarInternalDepth",   true,  SgAgency.BCA,  GCon, ">=1.400","Min 1400mm depth",                  "Code on Accessibility 2025 §4.6"),
            R("SGPset_LiftAccessibility","DoorOpenWidth",      true,  SgAgency.BCA,  GCon, ">=0.900","Min 900mm door clear opening",      "Code on Accessibility 2025 §4.6.2"),
        }));

        Add(E("A-LFT-FRE", "Fireman's Lift", "IFCTRANSPORTELEMENT", "ELEVATOR", "A", SgAgency.SCDF, new[]
        {
            R("Pset_TransportElementCommon","CapacityPeopleMax",true, SgAgency.SCDF, GCon, "",      "Fireman lift capacity",              "SCDF Fire Code 2018 §10"),
            R("SGPset_LiftFireman",    "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "120",   "2hr FRR for fireman lift lobby/shaft","SCDF Fire Code 2018 §10.3"),
        }));

        Add(E("A-ESC-PAS", "Escalator", "IFCTRANSPORTELEMENT", "ESCALATOR", "A", SgAgency.BCA, new[]
        {
            R("Pset_TransportElementCommon","CapacityPeopleMax",true, SgAgency.BCA,  GCon, "",      "Design capacity per hour",           "BCA"),
        }));

        Add(E("A-ESC-TRW", "Travelator / Moving Walkway", "IFCTRANSPORTELEMENT", "MOVINGWALKWAY", "A", SgAgency.BCA, new[]
        {
            R("Pset_TransportElementCommon","CapacityPeopleMax",true, SgAgency.BCA,  GCon, "",      "Design throughput",                  "BCA"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // MECHANICAL - HVAC (M-AHU-*, M-CHW-*, M-FCU-*, M-EXH-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("M-AHU-PRI", "Primary Air Handling Unit", "IFCAIRHANDLINGUNIT", "NOTDEFINED", "M", SgAgency.BCA, new[]
        {
            R("Pset_AirHandlingUnitTypeCommon","NominalAirFlowRate",true,SgAgency.BCA,GCon,"",     "Design airflow m³/s",                "ASHRAE 62.1 / SS 553:2016"),
            R("SGPset_AHUEnergy",      "CoefficientOfPerformance",true,SgAgency.BCA, GCon, ">=3.2","Min COP for energy efficiency",      "BCA Green Mark 2021 §4.1"),
            R("SGPset_AHUEnergy",      "EnergyEfficiencyRatio",false, SgAgency.BCA,  GCon, "",      "EER rating W/W",                    "BCA Green Mark 2021"),
        }));

        Add(E("M-AHU-FAH", "Fresh Air Handling Unit (FAHU)", "IFCAIRHANDLINGUNIT", "NOTDEFINED", "M", SgAgency.NEA, new[]
        {
            R("Pset_AirHandlingUnitTypeCommon","NominalAirFlowRate",true,SgAgency.NEA,GCon,"",     "Fresh air intake rate per occupant", "SS 553:2016 §5.2"),
            R("SGPset_VentilationRate","OutdoorAirFlow",        true, SgAgency.NEA,  GCon, ">=0.008","Min 8 L/s per person outdoor air", "SS 553:2016 Table 6.2.2"),
        }));

        Add(E("M-CHW-PLT", "Chilled Water Plant / Chiller", "IFCCHILLER", "NOTDEFINED", "M", SgAgency.BCA, new[]
        {
            R("Pset_ChillerTypeCommon","NominalCapacity",       true, SgAgency.BCA,  GCon, "",      "Chiller cooling capacity in kW",     "BCA Green Mark 2021"),
            R("SGPset_ChillerEnergy",  "CoefficientOfPerformance",true,SgAgency.BCA, GCon, ">=0.56","Min system COP kW/RT",               "BCA Green Mark 2021 §4.2"),
        }));

        Add(E("M-FCU-STD", "Fan Coil Unit (Standard)", "IFCFANCOILUNIT", "NOTDEFINED", "M", SgAgency.BCA, new[]
        {
            R("Pset_FanCoilUnitTypeCommon","NominalAirFlowRate",true,SgAgency.BCA,  GCon, "",      "FCU nominal airflow",                "BCA Green Mark 2021"),
        }));

        Add(E("M-EXH-KIT", "Kitchen Exhaust System", "IFCFAN", "EXHAUSTFAN", "M", SgAgency.NEA, new[]
        {
            R("Pset_FanTypeCommon",    "NominalFlowRate",       true, SgAgency.NEA,  GCon, "",      "Exhaust flow rate m³/s",             "NEA Environmental Health Guidelines"),
            R("SGPset_ExhaustVentilation","ExhaustRate",        true, SgAgency.NEA,  GCon, ">=25",  "Min 25 air changes/hour for kitchens","NEA Guidelines §4.3"),
        }));

        Add(E("M-EXH-CAR", "Carpark Ventilation / CO Exhaust", "IFCFAN", "SUPPLYRETURNFAN", "M", SgAgency.NEA, new[]
        {
            R("SGPset_CarParkVentilation","ExhaustRate",        true, SgAgency.NEA,  GCon, ">=6",   "Min 6 ACH for enclosed carparks",    "NEA Environmental Health §6 / SS 553"),
            R("SGPset_CarParkVentilation","CODetectionProvided",true, SgAgency.NEA,  GCon, "TRUE",  "CO detection system required",       "NEA Guidelines"),
        }));

        Add(E("M-EXH-TOI", "Toilet Exhaust Fan", "IFCFAN", "EXHAUSTFAN", "M", SgAgency.NEA, new[]
        {
            R("SGPset_ExhaustVentilation","ExhaustRate",        true, SgAgency.NEA,  GCon, ">=15",  "Min 15 ACH for toilets without windows","NEA Environmental Health Guidelines"),
        }));

        Add(E("M-SPR-WET", "Wet Pipe Sprinkler System", "IFCFIRESUPPRESSIONTERMINAL", "SPRINKLERHEAD", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_SprinklerSystem", "SystemType",           true, SgAgency.SCDF, GCon, "WET",   "Wet pipe sprinkler system",          "SCDF Fire Code 2018 §14"),
            R("SGPset_SprinklerSystem", "HazardCategory",       true, SgAgency.SCDF, GCon, "",      "Light/Ordinary/High hazard",         "SCDF / SS 537"),
            R("SGPset_SprinklerSystem", "DesignDensity",        true, SgAgency.SCDF, GCon, "",      "Design density mm/min",              "SCDF / SS 537 §9.2"),
        }));

        Add(E("M-SPR-DRY", "Dry Pipe Sprinkler System", "IFCFIRESUPPRESSIONTERMINAL", "SPRINKLERHEAD", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_SprinklerSystem", "SystemType",           true, SgAgency.SCDF, GCon, "DRY",   "Dry pipe sprinkler system",          "SCDF Fire Code 2018 §14"),
            R("SGPset_SprinklerSystem", "HazardCategory",       true, SgAgency.SCDF, GCon, "",      "Hazard occupancy category",          "SS 537"),
        }));

        Add(E("M-SPR-PRE", "Pre-Action Sprinkler System", "IFCFIRESUPPRESSIONTERMINAL", "SPRINKLERHEAD", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_SprinklerSystem", "SystemType",           true, SgAgency.SCDF, GCon, "PREACTION","Pre-action sprinkler",            "SCDF Fire Code 2018 §14"),
        }));

        Add(E("M-HYD-PIL", "Fire Hydrant (Pillar)", "IFCFLOWTERMINAL", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_FireHydrant",    "HydrantType",           true, SgAgency.SCDF, GCon, "PILLAR","Type of fire hydrant",              "SCDF Fire Code 2018 §15"),
            R("SGPset_FireHydrant",    "FlowRate",              true, SgAgency.SCDF, GCon, ">=1000","Min flow 1000 L/min",               "SCDF Fire Code 2018 §15.2"),
        }));

        Add(E("M-HYD-INL", "Fire Hydrant (Inlet / Siamese)", "IFCFLOWTERMINAL", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_FireHydrant",    "HydrantType",           true, SgAgency.SCDF, GCon, "INLET", "Siamese connection / inlet",         "SCDF Fire Code 2018 §15"),
        }));

        Add(E("M-DET-SMK", "Smoke Detector", "IFCALARM", "SMOKE", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_SmokeDetector",  "DetectorType",          true, SgAgency.SCDF, GCon, "SMOKE", "Optical or ionisation smoke detector","SCDF Fire Code 2018 §13"),
            R("SGPset_SmokeDetector",  "CoverageArea",          true, SgAgency.SCDF, GCon, "<=60",  "Max 60m² coverage per detector",     "SS 645 / SCDF §13.3"),
        }));

        Add(E("M-DET-HET", "Heat Detector", "IFCALARM", "HEAT", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_HeatDetector",   "DetectorType",          true, SgAgency.SCDF, GCon, "HEAT",  "Fixed temperature or rate-of-rise", "SCDF Fire Code 2018 §13"),
            R("SGPset_HeatDetector",   "CoverageArea",          true, SgAgency.SCDF, GCon, "<=25",  "Max 25m² for high-risk areas",       "SS 645"),
        }));

        Add(E("M-DET-GAS", "Gas Detector", "IFCALARM", "GAS", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_GasDetector",    "DetectorType",          true, SgAgency.SCDF, GCon, "GAS",   "Combustible gas detector",           "SCDF / NEA Gas Safety Act"),
        }));

        Add(E("M-EXS-MAN", "Emergency Exit Sign / Exit Light", "IFCLAMP", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_EmergencyLight", "EmergencyLightingType", true, SgAgency.SCDF, GCon, "EXIT",  "Maintained exit sign",               "SCDF Fire Code 2018 §12.2"),
            R("SGPset_EmergencyLight", "BackupDuration",        true, SgAgency.SCDF, GCon, ">=3",   "Min 3 hours battery backup",         "SCDF §12.2 / SS 563"),
        }));

        Add(E("M-EXS-EMG", "Emergency Lighting (Escape Path)", "IFCLAMP", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_EmergencyLight", "EmergencyLightingType", true, SgAgency.SCDF, GCon, "ESCAPE","Escape route emergency lighting",   "SCDF Fire Code 2018 §12"),
            R("SGPset_EmergencyLight", "BackupDuration",        true, SgAgency.SCDF, GCon, ">=3",   "Min 3 hours battery",                "SS 563"),
            R("SGPset_EmergencyLight", "IlluminanceLevel",      true, SgAgency.SCDF, GCon, ">=1.0", "Min 1 lux on escape route",          "SS 563 §4.1"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // ELECTRICAL (E-LTG-*, E-SOC-*, E-PNL-*, E-GEN-*, E-SOL-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("E-LTG-INT", "Interior Lighting Fitting", "IFCLAMP", "LIGHTFIXTURE", "E", SgAgency.BCA, new[]
        {
            R("SGPset_LightingEnergy", "LampEfficacy",          true, SgAgency.BCA,  GCon, ">=80",  "Min 80 lm/W for interior lighting", "BCA Green Mark 2021 §5"),
            R("SGPset_LightingEnergy", "PowerDensity",          true, SgAgency.BCA,  GCon, "<=15",  "Max 15 W/m² LPD",                   "BCA Green Mark 2021 §5.2"),
        }));

        Add(E("E-LTG-EXT", "Exterior / Facade Lighting", "IFCLAMP", "LIGHTFIXTURE", "E", SgAgency.BCA, new[]
        {
            R("SGPset_LightingEnergy", "LampEfficacy",          true, SgAgency.BCA,  GCon, ">=80",  "Efficient exterior lighting",        "BCA Green Mark 2021"),
        }));

        Add(E("E-LTG-EMR", "Emergency / Exit Lighting", "IFCLAMP", "LIGHTFIXTURE", "E", SgAgency.SCDF, new[]
        {
            R("SGPset_EmergencyLight", "BackupDuration",        true, SgAgency.SCDF, GCon, ">=3",   "3 hours emergency backup",           "SCDF Fire Code 2018 §12"),
        }));

        Add(E("E-PNL-MDB", "Main Distribution Board (MDB)", "IFCELECTRICDISTRIBUTIONBOARD", "USERDEFINED", "E", SgAgency.BCA, new[]
        {
            R("Pset_ElectricDistributionBoardOccurrence","RatedCurrent",true,SgAgency.BCA,GCon,"",  "MDB rated current in Amperes",       "SP PowerGrid / EMA"),
            R("Pset_ElectricDistributionBoardOccurrence","NominalVoltage",true,SgAgency.BCA,GCon,"230","Nominal voltage V",               "SS IEC 60364"),
        }));

        Add(E("E-PNL-SDB", "Sub-Distribution Board", "IFCELECTRICDISTRIBUTIONBOARD", "USERDEFINED", "E", SgAgency.BCA, new[]
        {
            R("Pset_ElectricDistributionBoardOccurrence","RatedCurrent",true,SgAgency.BCA,GCon,"",  "SDB rated current",                  "EMA / SP PowerGrid"),
        }));

        Add(E("E-GEN-DIG", "Diesel Generator / Standby Generator", "IFCELECTRICGENERATOR", "NOTDEFINED", "E", SgAgency.BCA, new[]
        {
            R("Pset_ElectricGeneratorTypeCommon","MaximumPowerOutput",true,SgAgency.BCA,GCon,"",    "Generator rated output in kVA",      "Building Control Act"),
            R("SGPset_GeneratorEmissions","EmissionStandard",   false, SgAgency.NEA,  GCon, "EPA_TIER4","Emission standard for generators","NEA Air Pollution Control Act"),
        }));

        Add(E("E-SOL-PNL", "Photovoltaic Solar Panel", "IFCSOLARPANEL", "PHOTOVOLTAIC", "E", SgAgency.BCA, new[]
        {
            R("Pset_SolarPanelTypeCommon","NominalEfficiency",  true, SgAgency.BCA,  GCon, ">=0.15","Min 15% panel efficiency",           "BCA Green Mark 2021 §6.2"),
            R("Pset_SolarPanelTypeCommon","NominalPeakPower",   true, SgAgency.BCA,  GCon, "",      "Rated peak power kWp",               "BCA Green Mark 2021"),
            R("SGPset_SolarEnergy",       "SystemCapacity",     true, SgAgency.BCA,  GCon, "",      "Total installed capacity kWp",       "BCA Green Mark 2021 §6.2"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // PLUMBING & DRAINAGE (P-SAN-*, P-DRN-*, P-WTR-*, P-GAS-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("P-SAN-WC", "WC / Water Closet (Toilet Bowl)", "IFCSANITARYTERMINAL", "TOILETPAN", "P", SgAgency.PUB, new[]
        {
            R("Pset_SanitaryTerminalTypeWC","WaterConsumptionPerFlushing",true,SgAgency.PUB,GCon,"<=4.5","Max 4.5L dual-flush (full)",    "PUB Water Efficiency Label - mandatory for sanitary fittings"),
            R("SGPset_WaterFittingPUB", "WELSRating",           true, SgAgency.PUB,  GCon, ">=3",   "Min 3 ticks WELS label",             "PUB Water Efficiency (Labelling) Regulations 2009"),
        }));

        Add(E("P-SAN-LAV", "Lavatory Basin / Wash Basin", "IFCSANITARYTERMINAL", "WASHHANDBASIN", "P", SgAgency.PUB, new[]
        {
            R("SGPset_WaterFittingPUB", "FlowRate",             true, SgAgency.PUB,  GCon, "<=4.0", "Max 4.0 L/min tap flow rate",        "PUB WELS Regulations 2009"),
            R("SGPset_WaterFittingPUB", "WELSRating",           true, SgAgency.PUB,  GCon, ">=3",   "Min 3 ticks WELS",                   "PUB WELS Regulations 2009"),
        }));

        Add(E("P-SAN-SHW", "Shower Head / Shower Fitting", "IFCSANITARYTERMINAL", "SHOWER", "P", SgAgency.PUB, new[]
        {
            R("SGPset_WaterFittingPUB", "FlowRate",             true, SgAgency.PUB,  GCon, "<=9.0", "Max 9.0 L/min shower flow rate",     "PUB WELS Regulations 2009"),
            R("SGPset_WaterFittingPUB", "WELSRating",           true, SgAgency.PUB,  GCon, ">=3",   "Min 3 ticks WELS",                   "PUB WELS Regulations 2009"),
        }));

        Add(E("P-SAN-URN", "Urinal", "IFCSANITARYTERMINAL", "URINAL", "P", SgAgency.PUB, new[]
        {
            R("SGPset_WaterFittingPUB", "FlushVolume",          true, SgAgency.PUB,  GCon, "<=1.0", "Waterless or max 1.0L flush",        "PUB WELS Regulations 2009"),
            R("SGPset_WaterFittingPUB", "WELSRating",           true, SgAgency.PUB,  GCon, ">=3",   "Min 3 ticks WELS",                   "PUB WELS Regulations 2009"),
        }));

        Add(E("P-SAN-DRN", "Floor Drain / Floor Trap", "IFCSANITARYTERMINAL", "FLOORDRAIN", "P", SgAgency.PUB, new[]
        {
            R("Pset_SanitaryTerminalTypeCommon","IsExternal",   true, SgAgency.BCA,  GCon, "",      "TRUE for external drains",           "IFC+SG COP3"),
        }));

        Add(E("P-WTR-TAN", "Water Tank (Cold Water Storage)", "IFCTANK", "STORAGE", "P", SgAgency.PUB, new[]
        {
            R("Pset_TankTypeCommon",   "WorkingVolume",         true, SgAgency.PUB,  GCon, "",      "Tank working volume in m³",          "PUB WSR 2014 §12"),
            R("SGPset_WaterTank",      "TankMaterial",          false, SgAgency.PUB, GCon, "",      "Material type - GRP/SS/concrete",    "PUB WSR 2014"),
            R("SGPset_WaterTank",      "IsInsulated",           false, SgAgency.BCA, GCon, "",      "Insulation for hot/cold service",    "BCA"),
        }));

        Add(E("P-WTR-PMP", "Water Booster Pump", "IFCPUMP", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("Pset_PumpTypeCommon",   "FlowRateRange",         true, SgAgency.PUB,  GCon, "",      "Design flow rate L/min",             "PUB WSR 2014"),
            R("Pset_PumpTypeCommon",   "PressureRange",         true, SgAgency.PUB,  GCon, "",      "Design head in m or kPa",            "PUB WSR 2014"),
        }));

        Add(E("P-DRN-SUI", "Sewer / Soil Drain (Gravity)", "IFCPIPESEGMENT", "GUTTER", "P", SgAgency.PUB, new[]
        {
            R("Pset_PipeSegmentTypeCommon","NominalDiameter",   true, SgAgency.PUB,  GCon, "",      "Drain pipe nominal diameter mm",     "PUB Sewerage and Drainage Act"),
            R("SGPset_DrainagePUB",    "PipeGradient",          true, SgAgency.PUB,  GCon, ">=1:100","Min gradient 1:100",                "PUB Code of Practice on Sewerage 2019 §5.3"),
        }));

        Add(E("P-DRN-STM", "Stormwater Drain / Surface Drain", "IFCPIPESEGMENT", "CULVERT", "P", SgAgency.PUB, new[]
        {
            R("Pset_PipeSegmentTypeCommon","NominalDiameter",   true, SgAgency.PUB,  GCon, "",      "Drain nominal diameter",             "PUB Drainage Code of Practice"),
            R("SGPset_DrainagePUB",    "DrainageCategory",      true, SgAgency.PUB,  GCon, "STORMWATER","Stormwater drainage system",     "PUB Code of Practice on Surface Water Drainage 2018"),
        }));

        Add(E("P-GAS-SVC", "Town Gas / Natural Gas Service Pipe", "IFCPIPESEGMENT", "RIGIDSEGMENT", "P", SgAgency.NEA, new[]
        {
            R("Pset_PipeSegmentTypeCommon","NominalDiameter",   true, SgAgency.NEA,  GCon, "",      "Gas pipe nominal diameter",          "City Gas / PUB Gas Act"),
            R("SGPset_GasPiping",      "DesignPressure",        true, SgAgency.NEA,  GCon, "",      "Design pressure kPa",                "Gas Safety Act / SS 531"),
            R("SGPset_GasPiping",      "PipeMaterial",          true, SgAgency.NEA,  GCon, "",      "Pipe material - steel/HDPE/copper",  "SS 531"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // CIVIL / SITE WORKS (C-DRN-*, C-PKG-*, C-RDW-*, C-RTW-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("C-DRN-SBK", "Soakaway / Infiltration Trench", "IFCBUILDINGELEMENTPROXY", "USERDEFINED", "C", SgAgency.PUB, new[]
        {
            R("SGPset_DrainagePUB",    "DrainageCategory",      true, SgAgency.PUB,  GCon, "STORMWATER","Stormwater management",          "PUB ABC Waters Design Guidelines"),
            R("SGPset_DrainagePUB",    "RetentionVolume",        false,SgAgency.PUB, GCon, "",      "Water retention volume m³",          "PUB ABC Waters"),
        }));

        Add(E("C-PKG-CAR", "Surface Carpark (At Grade)", "IFCSPACE", "PARKING", "C", SgAgency.LTA, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.URA,  GDes, "CARPARK","Space category",                   "URA / LTA"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.LTA,  GCon, ">=11.5","Min stall 2400 x 4800mm = 11.52m²",  "LTA Code of Practice for Parking §3.2"),
            R("SGPset_SpaceGFA",       "GFACategory",          true,  SgAgency.URA,  GDes, "CARPARK","Surface carpark exempt from GFA",   "URA GFA Rules"),
        }));

        Add(E("C-RTW-GRV", "Gravity Retaining Wall (Civil)", "IFCWALL", "RETAININGWALL", "C", SgAgency.BCA, new[]
        {
            R("Pset_WallCommon",       "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Retaining wall is structural",       "BC 3:2013"),
        }));

        Add(E("C-RDW-PVT", "Internal Road Pavement / Driveway", "IFCBUILDINGELEMENTPROXY", "USERDEFINED", "C", SgAgency.LTA, new[]
        {
            R("SGPset_Pavement",       "PavementType",         true,  SgAgency.LTA,  GCon, "",      "Asphalt/concrete/block paving",      "LTA Road Design Manual"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // MALAYSIA NBeS CODES (MY-* prefix - UBBL 1984 / MS 1184 / JBPM)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("MY-WAL-EXW", "External Wall (Malaysia NBeS)", "IFCWALL", "SOLIDWALL", "A", SgAgency.CIDB, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.CIDB, GCon, "TRUE",  "External wall per UBBL 1984",        "UBBL 1984 §47"),
            R("Pset_WallCommon",       "LoadBearing",          false, SgAgency.CIDB, GCon, "",      "TRUE if structural",                 "UBBL 1984 §47"),
        }));

        Add(E("MY-WAL-FRW", "Fire-Rated Wall (Malaysia JBPM)", "IFCWALL", "SOLIDWALL", "A", SgAgency.JBPM, new[]
        {
            R("Pset_WallCommon",       "IsExternal",           true,  SgAgency.CIDB, GCon, "",      "TRUE or FALSE",                      "UBBL 1984"),
            R("SGPset_WallFireRating", "FireResistancePeriod", true,  SgAgency.JBPM, GCon, "60",    "Min 60 min REI - UBBL 1984 §§113-120","UBBL 1984 §113 / JBPM FSS 2020"),
            R("SGPset_WallFireRating", "FireTestStandard",     true,  SgAgency.JBPM, GCon, "",      "BS 476 Part 22 / MS 1500",           "JBPM Fire Safety Standards 2020"),
        }));

        Add(E("MY-DOR-FRD", "Fire Door (Malaysia JBPM)", "IFCDOOR", "DOOR", "A", SgAgency.JBPM, new[]
        {
            R("Pset_DoorCommon",       "FireRating",           true,  SgAgency.JBPM, GCon, "FD30",  "Min FD30 - UBBL 1984 §116",          "UBBL 1984 §116 / JBPM FSS 2020"),
            R("Pset_DoorCommon",       "SmokeStop",            true,  SgAgency.JBPM, GCon, "",      "Smoke stop where required",          "UBBL 1984 / JBPM"),
            R("SGPset_DoorFireDoor",   "FireResistancePeriod", true,  SgAgency.JBPM, GCon, "30",    "Min 30 min fire resistance",         "JBPM FSS 2020"),
        }));

        Add(E("MY-SPC-LVN", "Living Space (Malaysia UBBL)", "IFCSPACE", "INTERNAL", "A", SgAgency.CIDB, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.CIDB, GDes, "LIVING_ROOM","Space category",               "UBBL 1984 §44"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.CIDB, GDes, ">=9.0", "Min 9.0m² for habitable rooms",      "UBBL 1984 §47"),
            R("Pset_SpaceCommon",      "Height",               true,  SgAgency.CIDB, GCon, ">=2.4", "Min 2.4m ceiling height UBBL §53",   "UBBL 1984 §53"),
        }));

        Add(E("MY-SPC-BTH", "Bathroom / Toilet (Malaysia UBBL)", "IFCSPACE", "INTERNAL", "A", SgAgency.CIDB, new[]
        {
            R("Pset_SpaceCommon",      "Category",             true,  SgAgency.CIDB, GDes, "BATHROOM","Space category",                  "UBBL 1984"),
            R("Pset_SpaceCommon",      "GrossPlannedArea",     true,  SgAgency.CIDB, GDes, ">=2.25","Min 1500x1500mm floor area",         "UBBL 1984 §56"),
        }));

        Add(E("MY-RMP-OKU", "OKU Ramp (Malaysia MS 1184)", "IFCRAMP", "NOTDEFINED", "A", SgAgency.CIDB, new[]
        {
            R("SGPset_RampAccessibility","Gradient",           true,  SgAgency.CIDB, GCon, "<=0.0833","Max 1:12 gradient per MS 1184",   "MS 1184:2014 §6.3"),
            R("SGPset_RampAccessibility","Width",              true,  SgAgency.CIDB, GCon, ">=1.200","Min 1200mm clear width",            "MS 1184:2014 §6.3.1"),
        }));

        Add(E("MY-DOR-OKU", "OKU Accessible Door (MS 1184)", "IFCDOOR", "DOOR", "A", SgAgency.CIDB, new[]
        {
            R("Pset_DoorCommon",       "HandicapAccessible",   true,  SgAgency.CIDB, GCon, "TRUE",  "OKU accessible door",                "MS 1184:2014 §5.4"),
            R("SGPset_DoorAccessibility","ClearWidth",         true,  SgAgency.CIDB, GCon, ">=0.850","Min 850mm clear opening width",    "MS 1184:2014 §5.4.2"),
        }));

        Add(E("MY-COL-RCC", "RC Column (Malaysia UBBL)", "IFCCOLUMN", "COLUMN", "S", SgAgency.CIDB, new[]
        {
            R("Pset_ColumnCommon",     "LoadBearing",          true,  SgAgency.CIDB, GCon, "TRUE",  "Structural RC column",               "UBBL 1984 §49"),
            R("SGPset_ColumnStructural","ConcreteGrade",       true,  SgAgency.CIDB, GCon, "C25/30","Concrete grade per MS EN 1992",      "MS EN 1992-1-1:2010"),
        }));

        Add(E("MY-PIL-BOR", "Bored Pile (Malaysia)", "IFCPILE", "BORED", "S", SgAgency.CIDB, new[]
        {
            R("Pset_PileCommon",       "LoadBearing",          true,  SgAgency.CIDB, GPil, "TRUE",  "Structural pile",                    "UBBL 1984 §70"),
            R("SGPset_PileFoundation", "PileType",             true,  SgAgency.CIDB, GPil, "BORED", "Pile type declaration",              "MS EN 1536:2011"),
            R("SGPset_PileFoundation", "DesignLoad",           true,  SgAgency.CIDB, GPil, "",      "Design load kN",                     "MS EN 1536"),
        }));

        Add(E("MY-SPR-WET", "Wet Pipe Sprinkler (Malaysia JBPM)", "IFCFIRESUPPRESSIONTERMINAL", "SPRINKLERHEAD", "M", SgAgency.JBPM, new[]
        {
            R("SGPset_SprinklerSystem", "SystemType",          true,  SgAgency.JBPM, GCon, "WET",   "Wet pipe sprinkler",                 "JBPM FSS 2020 §14 / MS 1489:2010"),
            R("SGPset_SprinklerSystem", "HazardCategory",      true,  SgAgency.JBPM, GCon, "",      "Light/Ordinary/High hazard group",   "MS 1489:2010 §9"),
        }));

        Add(E("MY-WTR-WC", "WC (Malaysia - SIRIM)", "IFCSANITARYTERMINAL", "TOILETPAN", "P", SgAgency.CIDB, new[]
        {
            R("Pset_SanitaryTerminalTypeWC","WaterConsumptionPerFlushing",true,SgAgency.CIDB,GCon,"<=6.0","Max 6L/flush - MS 1522",       "MS 1522:2010 - Water Efficiency Requirements"),
        }));

        // ═══════════════════════════════════════════════════════════════════════
        // STRUCTURAL EXTRAS - PRECAST / TRUSSES / BRACING (S-PNL-*, S-TRS-*)
        // ═══════════════════════════════════════════════════════════════════════

        Add(E("S-PNL-PRE", "Precast Concrete Panel", "IFCPLATE", "WALL_PANEL", "S", SgAgency.BCA, new[]
        {
            R("Pset_PlateCommon",      "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Precast structural panel",           "BC 2:2021 / SS EN 1992-1-1"),
            R("SGPset_PrecastElement", "ConcreteGrade",        true,  SgAgency.BCA,  GCon, "C40/50","High-strength precast concrete",     "BC 2:2021"),
            R("SGPset_PrecastElement", "ManufacturerCode",     false, SgAgency.BCA,  GCon, "",      "Precast element ID/mark",            "IFC+SG COP3"),
        }));

        Add(E("S-TRS-STL", "Steel Roof Truss", "IFCMEMBER", "RAFTER", "S", SgAgency.BCA, new[]
        {
            R("Pset_MemberCommon",     "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural steel truss",             "BC 1:2012"),
            R("SGPset_BeamStructural", "SteelGrade",           true,  SgAgency.BCA,  GCon, "S275",  "Steel grade",                        "BC 1:2012"),
            R("SGPset_BeamStructural", "FireResistancePeriod", true,  SgAgency.SCDF, GCon, "30",    "Min 30 min for roof structure",      "SCDF Fire Code 2018"),
        }));

        Add(E("S-BRC-STL", "Steel Bracing Member", "IFCMEMBER", "BRACE", "S", SgAgency.BCA, new[]
        {
            R("Pset_MemberCommon",     "LoadBearing",          true,  SgAgency.BCA,  GCon, "TRUE",  "Structural bracing",                 "BC 1:2012"),
            R("SGPset_BeamStructural", "SteelGrade",           true,  SgAgency.BCA,  GCon, "S275",  "Steel grade",                        "BC 1:2012"),
        }));


        // ================================================================
        // ALL 81 IDENTIFIED COMPONENTS - IFC+SG INDUSTRY MAPPING DEC 2025
        // Source: BCA IFC+SG Industry Mapping, COP3.1 Edition
        // 833 property mappings, 8 agencies covered
        // ================================================================

        // ═══════════════════════════════════════════════════════════════════════
        // CORENET-X COP 3.1 (December 2025) - Section 4 Identified Components
        // Full property rules for all 62 Identified Components
        // Source: CORENET-X COP 3.1 Edition 2025-12, Section 4 pp.250-440
        // All 812 IFC+SG property requirements embedded.
        // ═══════════════════════════════════════════════════════════════════════

        // Accessible Route
        Add(E("CX-001", "Accessible Route", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Accessible Route", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.BCA, GCon, "1200", "IFC+SG: Accessible Route", "COP 3.1 Dec 2025 p.251"),
        }));

        // Beam
        Add(E("CX-002", "Beam", "IFCBEAM", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Beam", "BeamSpanType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "BottomLeft", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "BottomMiddle", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "BottomRight", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamDimension", "ConstructionMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamDimension", "Depth", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_SteelConnection", "LeftConnectionDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_SteelConnection", "LeftConnectionType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamDimension", "Mark", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_GeographicElement", "MaterialGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_SteelConnection", "MechanicalConnectionType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "MemberSection", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "PrefabricatedReinforcementCage", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "ReferTo2DDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_GeographicElement", "ReinforcementSteelGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_SteelConnection", "RightConnectionDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_SteelConnection", "RightConnectionType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "SectionFabricationMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "SideBar", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "StirrupsLeft", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "StirrupsMiddle", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "StirrupsRight", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "StirrupsTypeLeft", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "StirrupsTypeMiddle", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "StirrupsTypeRight", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_SteelConnection", "SpliceConnection", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "TopLeft", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "TopMiddle", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamReinforcement", "TopRight", false, SgAgency.BCA, GCon, "No", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_BeamDimension", "Width", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "10", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "11", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "12", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "13", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "14", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "15", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "16", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "17", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "18", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "19", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
            R("SGPset_Beam", "20", false, SgAgency.BCA, GCon, "", "IFC+SG: Beam", "COP 3.1 Dec 2025 p.251"),
        }));

        // Borehole
        Add(E("CX-004", "Borehole", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Depth", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Borehole", "COP 3.1 Dec 2025 p.262"),
            R("SGPset_BuildingElementProxyDimension", "Mark", false, SgAgency.BCA, GCon, "No", "IFC+SG: Borehole", "COP 3.1 Dec 2025 p.262"),
            R("SGPset_CivilElement", "SHDLevel_SPT_MoreThan_100N", false, SgAgency.BCA, GCon, "No", "IFC+SG: Borehole", "COP 3.1 Dec 2025 p.262"),
            R("SGPset_CivilElement", "SHDLevel_SPT_MoreThan_60N", false, SgAgency.BCA, GCon, "No", "IFC+SG: Borehole", "COP 3.1 Dec 2025 p.262"),
            R("SGPset_BuildingElementProxy", "TerminationLevel", false, SgAgency.BCA, GCon, "No", "IFC+SG: Borehole", "COP 3.1 Dec 2025 p.262"),
            R("SGPset_BuildingElementProxy", "TopLevel", false, SgAgency.BCA, GCon, "No", "IFC+SG: Borehole", "COP 3.1 Dec 2025 p.262"),
        }));

        // Breeching Inlet
        Add(E("CX-005", "Breeching Inlet", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Hose_NominalDiameter", false, SgAgency.SCDF, GCon, "-", "IFC+SG: Breeching Inlet", "COP 3.1 Dec 2025 p.263"),
            R("SGPset_FireSuppressionTerminal", "ID", false, SgAgency.SCDF, GCon, "-", "IFC+SG: Breeching Inlet", "COP 3.1 Dec 2025 p.263"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.SCDF, GCon, "Dry Riser, Wet Riser, Foam Sprinkler, Sprinkler", "IFC+SG: Breeching Inlet", "COP 3.1 Dec 2025 p.263"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.SCDF, GCon, "-", "IFC+SG: Breeching Inlet", "COP 3.1 Dec 2025 p.263"),
        }));

        // Ceiling
        Add(E("CX-007", "Ceiling", "IFCCOVERING", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("SGPset_RoofFireRating", "FireRating", true, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Ceiling", "COP 3.1 Dec 2025 p.265"),
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.SCDF, GCon, "Sand, Corey Dust, Granite Dust, Gravel, Crusher Run, Recycled Aggregates, Intume", "IFC+SG: Ceiling", "COP 3.1 Dec 2025 p.265"),
        }));

        // Column
        Add(E("CX-008", "Column", "IFCCOLUMN", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Column", "ArrangementType", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_ColumnDimension", "Breadth", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_SteelConnection", "ConnectionDetailsBottom", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_SteelConnection", "ConnectionDetailsTop", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_SteelConnection", "ConnectionTypeBottom", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_SteelConnection", "ConnectionTypeTop", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_ColumnDimension", "ConstructionMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_ColumnDimension", "Diameter", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "EndStorey", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "MainRebar", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_ColumnDimension", "Mark", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_GeographicElement", "MaterialGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_SteelConnection", "MechanicalConnectionType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "MemberSection", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "PrefabricatedReinforcementCage", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "ReferTo2DDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_GeographicElement", "ReinforcementSteelGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "SectionFabricationMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "SpliceDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "StartingStorey", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_ColumnReinforcement", "Stirrups", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_ColumnReinforcement", "StirrupsType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_ColumnDimension", "Width", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_PileFoundation", "WorkingLoad_DA1-1", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_PileFoundation", "WorkingLoad_DA1-2", false, SgAgency.BCA, GCon, "No", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "10", false, SgAgency.BCA, GCon, "", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "11", false, SgAgency.BCA, GCon, "", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "12", false, SgAgency.BCA, GCon, "", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
            R("SGPset_Column", "13", false, SgAgency.BCA, GCon, "", "IFC+SG: Column", "COP 3.1 Dec 2025 p.266"),
        }));

        // Control Element
        Add(E("CX-009", "Control Element", "IFCUNITARYCONTROLELEMENT", "NOTDEFINED", "E", SgAgency.BCA, new[]
        {
            R("SGPset_UnitaryControlElement", "Purpose", false, SgAgency.BCA, GCon, "Main Panel, Sub Panel", "IFC+SG: Control Element", "COP 3.1 Dec 2025 p.270"),
            R("SGPset_GeographicElement", "PWCS_Flushing", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Control Element", "COP 3.1 Dec 2025 p.270"),
        }));

        // Culvert/ Drains
        Add(E("CX-010", "Culvert/ Drains", "IFCCIVILELEMENT", "NOTDEFINED", "C", SgAgency.PUB, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Diameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_CivilElement", "Gradient", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_BuildingElementProxyDimension", "Height", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_BuildingElementProxyDimension", "Length", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_BuildingElementProxy", "LoadBearing", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_CivilElement", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_CivilElement", "SystemType", false, SgAgency.PUB, GCon, "Rainwater, Drainage", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_BuildingElementProxyDimension", "Thickness", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.PUB, GCon, "-", "IFC+SG: Culvert/ Drains", "COP 3.1 Dec 2025 p.271"),
        }));

        // Damper
        Add(E("CX-012", "Damper", "IFCDAMPER", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_WallFireRating", "FireRating", true, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Damper", "COP 3.1 Dec 2025 p.273"),
        }));

        // Distribution Chamber
        Add(E("CX-013", "Distribution Chamber", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "C", SgAgency.PUB, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Diameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_BuildingElementProxyDimension", "Depth", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_BuildingElementProxyDimension", "Height", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_Covering", "ID", false, SgAgency.PUB, GCon, "", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_CivilElement", "InvertLevel", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_BuildingElementProxyDimension", "Length", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_Covering", "Status", true, SgAgency.PUB, GCon, "Existing, Proposed, To Be Removed, Abandoned, New, Temporary, Demolished", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary, Sewerage", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_Covering", "TopLevel", false, SgAgency.PUB, GCon, "-50, 3.423", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_Covering", "TradeEffluent", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.PUB, GCon, "-", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_Covering", "Watertight", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
            R("SGPset_Covering", "ExternalReference", false, SgAgency.PUB, GCon, "SS 30 Manhole Tops and Surface-box Tops", "IFC+SG: Distribution Chamber", "COP 3.1 Dec 2025 p.274"),
        }));

        // Door
        Add(E("CX-014", "Door", "IFCDOOR", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_DoorAccessibility", "ClearWidth", false, SgAgency.BCA, GCon, "1200", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorAccessibility", "ClearHeight", false, SgAgency.BCA, GCon, "N.A.", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorFireDoor", "FireAccessOpening", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorFireDoor", "FireExit", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorFireDoor", "FireRating", true, SgAgency.BCA, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorAccessibility", "MainEntrance", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorDimension", "Material", false, SgAgency.BCA, GCon, "-", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_Door", "OneWayLockingDevice", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_Door", "OperationType", true, SgAgency.BCA, GCon, "Pls refer to the next page", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorDimension", "OverallWidth", false, SgAgency.BCA, GCon, "-", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_Door", "PowerOperated", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_Door", "SelfClosing", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorDimension", "StructuralHeight", false, SgAgency.BCA, GCon, "710", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorDimension", "StructuralWidth", false, SgAgency.BCA, GCon, "490", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_DoorDimension", "Thickness", false, SgAgency.BCA, GCon, "N.A.", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
            R("SGPset_Door", "VisionPanel", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Door", "COP 3.1 Dec 2025 p.278"),
        }));

        // Earthworks
        Add(E("CX-015", "Earthworks", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "C", SgAgency.BCA, new[]
        {
            R("SGPset_GeographicElement", "Area", false, SgAgency.BCA, GCon, "-", "IFC+SG: Earthworks", "COP 3.1 Dec 2025 p.282"),
            R("SGPset_GeographicElement", "Status", true, SgAgency.BCA, GCon, "Existing, Proposed", "IFC+SG: Earthworks", "COP 3.1 Dec 2025 p.282"),
        }));

        // Finishes
        Add(E("CX-019", "Finishes", "IFCCOVERING", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_WallFireRating", "FireRating", true, SgAgency.BCA, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Finishes", "COP 3.1 Dec 2025 p.286"),
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.BCA, GCon, "-", "IFC+SG: Finishes", "COP 3.1 Dec 2025 p.286"),
        }));

        // Fire Access Opening
        Add(E("CX-020", "Fire Access Opening", "IFCDOOR", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_WallFireRating", "FireAccessOpening", true, SgAgency.SCDF, GCon, "TRUE / FALSE", "IFC+SG: Fire Access Opening", "COP 3.1 Dec 2025 p.287"),
        }));

        // Fire Extinguisher
        Add(E("CX-022", "Fire Extinguisher", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_BuildingElementProxy", "FireExtinguisherRating", false, SgAgency.SCDF, GCon, "-", "IFC+SG: Fire Extinguisher", "COP 3.1 Dec 2025 p.289"),
        }));

        // Fire Hydrant
        Add(E("CX-023", "Fire Hydrant", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_FireSuppressionTerminal", "ID", false, SgAgency.SCDF, GCon, "N.A.", "IFC+SG: Fire Hydrant", "COP 3.1 Dec 2025 p.290"),
            R("SGPset_FireSuppressionTerminal", "Private", true, SgAgency.SCDF, GCon, "TRUE / FALSE", "IFC+SG: Fire Hydrant", "COP 3.1 Dec 2025 p.290"),
            R("SGPset_FireSuppressionTerminal", "Public", true, SgAgency.SCDF, GCon, "TRUE / FALSE", "IFC+SG: Fire Hydrant", "COP 3.1 Dec 2025 p.290"),
        }));

        // Foam Inlet / Outlet
        Add(E("CX-024", "Foam Inlet / Outlet", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.SCDF, GCon, "Foam Fire Extinguishing, Foam Sprinkler", "IFC+SG: Foam Inlet / Outlet", "COP 3.1 Dec 2025 p.291"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.SCDF, GCon, "-", "IFC+SG: Foam Inlet / Outlet", "COP 3.1 Dec 2025 p.291"),
        }));

        // Footing / Pilecap
        Add(E("CX-025", "Footing / Pilecap", "IFCFOOTING", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Footing", "BottomDistribution", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "BottomMain", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_FootingDimension", "Breadth", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_PileFoundation", "DA1-1_BearingCapacity", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_PileFoundation", "DA1-2_BearingCapacity", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_FootingDimension", "Depth", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_FootingDimension", "Mark", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_GeographicElement", "MaterialGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "ReferTo2DDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_GeographicElement", "ReinforcementSteelGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_FootingReinforcement", "SideBar", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "SoilVerificationTest", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_FootingReinforcement", "Stirrups", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_FootingReinforcement", "StirrupsType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "TopDistribution", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "TopMain", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_FootingDimension", "Width", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_PileFoundation", "WorkingLoad_DA1-1", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_PileFoundation", "WorkingLoad_DA1-2", false, SgAgency.BCA, GCon, "No", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "10", false, SgAgency.BCA, GCon, "", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "11", false, SgAgency.BCA, GCon, "", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "12", false, SgAgency.BCA, GCon, "", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "13", false, SgAgency.BCA, GCon, "", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "14", false, SgAgency.BCA, GCon, "", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "15", false, SgAgency.BCA, GCon, "", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
            R("SGPset_Footing", "16", false, SgAgency.BCA, GCon, "", "IFC+SG: Footing / Pilecap", "COP 3.1 Dec 2025 p.295"),
        }));

        // Footpath
        Add(E("CX-026", "Footpath", "IFCCIVILELEMENT", "NOTDEFINED", "C", SgAgency.BCA, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.BCA, GCon, "-", "IFC+SG: Footpath", "COP 3.1 Dec 2025 p.298"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.BCA, GCon, "-", "IFC+SG: Footpath", "COP 3.1 Dec 2025 p.298"),
        }));

        // Grating
        Add(E("CX-027", "Grating", "IFCDISCRETEACCESSORY", "NOTDEFINED", "C", SgAgency.PUB, new[]
        {
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Drainage", "IFC+SG: Grating", "COP 3.1 Dec 2025 p.299"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Grating", "COP 3.1 Dec 2025 p.299"),
        }));

        // Green Verges
        Add(E("CX-028", "Green Verges", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.NParks, new[]
        {
            R("SGPset_Space", "Area", false, SgAgency.NParks, GCon, "-", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
            R("SGPset_Space", "ApprovedSoilMixture", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
            R("SGPset_Space", "ApprovedTurfSpecies", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
            R("SGPset_Space", "Status", true, SgAgency.NParks, GCon, "Proposed, Existing, To be removed", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
            R("SGPset_Space", "ShrubSpecies", false, SgAgency.NParks, GCon, "-", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
            R("SGPset_GeographicElement", "ALS_LandscapeType", true, SgAgency.NParks, GCon, "Turfing, Groundcover, Shrubs", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
            R("SGPset_Space", "ALS_GreeneryFeatures", true, SgAgency.NParks, GCon, "Green Verge", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
            R("SGPset_Space", "ALS_Status", true, SgAgency.NParks, GCon, "Proposed, Existing, To be removed", "IFC+SG: Green Verges", "COP 3.1 Dec 2025 p.300"),
        }));

        // Gutter
        Add(E("CX-029", "Gutter", "IFCPIPESEGMENT", "NOTDEFINED", "C", SgAgency.PUB, new[]
        {
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Drainage", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
            R("SGPset_CivilElementDimension", "ConstructionMethod", false, SgAgency.PUB, GCon, "-", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
            R("SGPset_CivilElementDimension", "Height", false, SgAgency.PUB, GCon, "-", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
            R("SGPset_CivilElementDimension", "Length", false, SgAgency.PUB, GCon, "-", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
            R("SGPset_CivilElementDimension", "Thickness", false, SgAgency.PUB, GCon, "-", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
            R("SGPset_CivilElementDimension", "Width", false, SgAgency.PUB, GCon, "-", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
            R("SGPset_CivilElement", "Public", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Gutter", "COP 3.1 Dec 2025 p.301"),
        }));

        // Hose Reel
        Add(E("CX-030", "Hose Reel", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Hose_NominalDiameter", false, SgAgency.SCDF, GCon, "-", "IFC+SG: Hose Reel", "COP 3.1 Dec 2025 p.302"),
        }));

        // Household Shelter
        Add(E("CX-031", "Household Shelter", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Space", "SpaceName", true, SgAgency.BCA, GCon, "Household Shelter, Setback", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
            R("SGPset_BuildingElementProxyDimension", "ConstructionMethod", false, SgAgency.BCA, GCon, "Precast, Prefab, CIS", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
            R("SGPset_Space", "Area", false, SgAgency.BCA, GCon, "-", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
            R("SGPset_BuildingElementProxyDimension", "InternalLength", false, SgAgency.BCA, GCon, "-", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
            R("SGPset_BuildingElementProxyDimension", "InternalWidth", false, SgAgency.BCA, GCon, "-", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
            R("SGPset_Space", "AGF_Name", true, SgAgency.BCA, GCon, "Household Shelter", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
            R("SGPset_BuildingElementProxyDimension", "Thickness", false, SgAgency.BCA, GCon, "300", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
            R("SGPset_BuildingElementProxy", "ShelterUsage", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Household Shelter", "COP 3.1 Dec 2025 p.304"),
        }));

        // Interceptor
        Add(E("CX-032", "Interceptor", "IFCINTERCEPTOR", "NOTDEFINED", "P", SgAgency.NEA, new[]
        {
            R("SGPset_Interceptor", "ComplyToPUBStandardDrawing", true, SgAgency.NEA, GCon, "TRUE / FALSE", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_Interceptor", "ReferToDrawingNumber", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_CivilElement", "InvertLevel", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_Interceptor", "TopLevel", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_InterceptorDimension", "Diameter", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_InterceptorDimension", "Height", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_InterceptorDimension", "Length", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_InterceptorDimension", "Width", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_Interceptor", "TradeEffluent", true, SgAgency.NEA, GCon, "TRUE / FALSE", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.NEA, GCon, "Sanitary, Sewerage", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.NEA, GCon, "-", "IFC+SG: Interceptor", "COP 3.1 Dec 2025 p.306"),
        }));

        // Landscape Plants
        Add(E("CX-034", "Landscape Plants", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.NParks, new[]
        {
            R("SGPset_GeographicElement", "Girth", false, SgAgency.NParks, GCon, "100, 300, 1000", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "HedgeNumber", false, SgAgency.NParks, GCon, "H001, H002, H003", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElementDimension", "Height", false, SgAgency.NParks, GCon, "2500, 10000", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "ReasonForRemoval", false, SgAgency.NParks, GCon, "-", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "Roadside", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "SingleStem", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "Species", false, SgAgency.NParks, GCon, "Samaneasaman, Cyrtostachysrenda, Gardenia tubifera", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "Status", true, SgAgency.NParks, GCon, "Proposed,Existing, To be removed, To be transplanted", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "TreeNumber", false, SgAgency.NParks, GCon, "T001, T002, T003", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "TreeSize", false, SgAgency.NParks, GCon, "Small to medium, Large", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
            R("SGPset_GeographicElement", "Turf", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Landscape Plants", "COP 3.1 Dec 2025 p.309"),
        }));

        // Lift
        Add(E("CX-035", "Lift", "IFCTRANSPORTELEMENT", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_LiftAccessibility", "BarrierFreeAccessbility^", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
            R("SGPset_BuildingElementProxyDimension", "Length", false, SgAgency.BCA, GCon, "-", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.BCA, GCon, "-", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
            R("SGPset_BuildingElementProxyDimension", "ClearDepth^", false, SgAgency.BCA, GCon, "-", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
            R("SGPset_LiftAccessibility", "ClearHeight^", false, SgAgency.BCA, GCon, "-", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
            R("SGPset_LiftAccessibility", "ClearWidth^", false, SgAgency.BCA, GCon, "-", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
            R("SGPset_TransportElement", "FireFightingLift^", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
            R("SGPset_TransportElementDimension", "LiftType^", false, SgAgency.BCA, GCon, "Goods Lift, Platform Lift, Bin Lifter, Bed Lift", "IFC+SG: Lift", "COP 3.1 Dec 2025 p.310"),
        }));

        // Parking Lot
        Add(E("CX-036", "Parking Lot", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "A", SgAgency.LTA, new[]
        {
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "FamilyLot", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxyDimension", "Length", false, SgAgency.LTA, GCon, "N.A.", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.LTA, GCon, "N.A.", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "LotNumber", false, SgAgency.LTA, GCon, "123", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "CarParking_ServedByCarLift", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "MechanisedParkingSystem", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "Perforated", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_GeographicElement", "OpenAtGrade", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "VehicleType", false, SgAgency.LTA, GCon, "Rigid-framed vehicle", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "ParkingUse", false, SgAgency.LTA, GCon, "Electric Vehicle, Oil Tanker, Buggy, Vacuum Truck, Mobile Tanker", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "BicycleRack_Type", true, SgAgency.LTA, GCon, "Single-Tier Wheel Rack, Single-Tier U-Bar, Double-Tier", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "VentilationMode", true, SgAgency.LTA, GCon, "Natural Ventilation, Air Conditioning Mechanical Ventilation", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_Space", "SpaceName", true, SgAgency.LTA, GCon, "Parking place", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "Area", false, SgAgency.LTA, GCon, "-", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "AGF_Name", true, SgAgency.LTA, GCon, "Car Parking Lot (Mechanised)", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
        }));

        // Parking Lot
        Add(E("CX-037", "Parking Lot", "IFCSPACE", "NOTDEFINED", "A", SgAgency.LTA, new[]
        {
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "FamilyLot", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxyDimension", "Length", false, SgAgency.LTA, GCon, "N.A.", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.LTA, GCon, "N.A.", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "LotNumber", false, SgAgency.LTA, GCon, "123", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "CarParking_ServedByCarLift", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "MechanisedParkingSystem", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "Perforated", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_GeographicElement", "OpenAtGrade", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "VehicleType", false, SgAgency.LTA, GCon, "Rigid-framed vehicle", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "ParkingUse", false, SgAgency.LTA, GCon, "Electric Vehicle, Oil Tanker, Buggy, Vacuum Truck, Mobile Tanker", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "BicycleRack_Type", true, SgAgency.LTA, GCon, "Single-Tier Wheel Rack, Single-Tier U-Bar, Double-Tier", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "VentilationMode", true, SgAgency.LTA, GCon, "Natural Ventilation, Air Conditioning Mechanical Ventilation", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_Space", "SpaceName", true, SgAgency.LTA, GCon, "Parking place", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "Area", false, SgAgency.LTA, GCon, "-", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
            R("SGPset_BuildingElementProxy", "AGF_Name", true, SgAgency.LTA, GCon, "Car Parking Lot (Mechanised)", "IFC+SG: Parking Lot", "COP 3.1 Dec 2025 p.312"),
        }));

        // Pile
        Add(E("CX-038", "Pile", "IFCPILE", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_PileFoundation", "BoreholeRef", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileDimension", "Breadth", false, SgAgency.BCA, GPil, "No*", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileDimension", "ConstructionMethod", false, SgAgency.BCA, GPil, "Yes", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "CutOffLevel_SHD", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-1_CompressionCapacity", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-1_CompressionDesignLoad", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-1_TensionCapacity", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-1_TensionDesignLoad", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-2_CompressionCapacity", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-2_CompressionDesignLoad", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-2_TensionCapacity", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "DA1-2_TensionDesignLoad", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileDimension", "Diameter", false, SgAgency.BCA, GPil, "No*", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileDimension", "Length", false, SgAgency.BCA, GPil, "No*", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "MainRebar", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileDimension", "Mark", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_GeographicElement", "MaterialGrade", false, SgAgency.BCA, GPil, "Yes", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "MemberSection", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "MinEmbedmentIntoBearingLayer_SPT_ MoreThan_100N", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "MinEmbedmentIntoBearingLayer_SPT_ MoreThan_60N", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileDimension", "MinRockSocketingLength", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "NegativeSkinFriction", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "PileType", false, SgAgency.BCA, GPil, "Yes", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileDimension", "ReinforcementLength", false, SgAgency.BCA, GPil, "Yes", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_GeographicElement", "ReinforcementSteelGrade", false, SgAgency.BCA, GPil, "Yes", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_FootingReinforcement", "Stirrups", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "StructuralCompressionCapacity", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "StructuralTensionCapacity", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_PileFoundation", "ToeLevel_SHD", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Building", "Width", false, SgAgency.BCA, GPil, "No*", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_BuildingElementProxy", "PileModelFactor", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_BuildingElementProxy", "ShaftR4DesignFactor", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_BuildingElementProxy", "EndBearingR4DesignFactor", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_BuildingElementProxy", "NoOfULTTest", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_BuildingElementProxy", "NoOfWorkingLoadTest_MaintainedLoadTest", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_BuildingElementProxy", "NoOfWorkingLoadTest_RapidLoadTest", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_BuildingElementProxy", "NoOfNonDestructiveTestPile", false, SgAgency.BCA, GPil, "No", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "10", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "11", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "12", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "13", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "14", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "15", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "16", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "17", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "18", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "19", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
            R("SGPset_Pile", "20", false, SgAgency.BCA, GPil, "", "IFC+SG: Pile", "COP 3.1 Dec 2025 p.316"),
        }));

        // Pipes/ Ducts
        Add(E("CX-039", "Pipes/ Ducts", "IFCPIPESEGMENT", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_PipeSegment", "PreInsulated", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "Perforated", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegmentDimension", "ConstructionMethod", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegmentDimension", "Material", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "Gradient", false, SgAgency.PUB, GCon, "1:100", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "InnerDiameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegmentDimension", "Length", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegmentDimension", "Thickness", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "TradeEffluent", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "DemountableStructureAbovePipe", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary, Sewerage", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegmentDimension", "NominalDiameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
            R("SGPset_PipeSegment", "OuterDiameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pipes/ Ducts", "COP 3.1 Dec 2025 p.320"),
        }));

        // Planting Areas
        Add(E("CX-041", "Planting Areas", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.NParks, new[]
        {
            R("SGPset_GeographicElement", "Area", false, SgAgency.NParks, GCon, "-", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
            R("SGPset_GeographicElement", "ApprovedSoilMixture", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
            R("SGPset_GeographicElement", "Status", true, SgAgency.NParks, GCon, "Existing, Proposed, New, To be Removed", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
            R("SGPset_GeographicElement", "Turf", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
            R("SGPset_GeographicElement", "TurfSpecies", false, SgAgency.NParks, GCon, "-", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
            R("SGPset_GeographicElement", "Compensated", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
            R("SGPset_GeographicElement", "Encroachment", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
            R("SGPset_GeographicElement", "CarparkProvision", true, SgAgency.NParks, GCon, "TRUE / FALSE", "IFC+SG: Planting Areas", "COP 3.1 Dec 2025 p.324"),
        }));

        // Pollution Control
        Add(E("CX-042", "Pollution Control", "IFCUNITARYEQUIPMENT", "NOTDEFINED", "M", SgAgency.NEA, new[]
        {
            R("SGPset_Unitaryequipment", "Mark", false, SgAgency.NEA, GCon, "", "IFC+SG: Pollution Control", "COP 3.1 Dec 2025"),
        }));

        // Prefabricated Building Systems and MEP Components
        Add(E("CX-043", "Prefabricated Building Systems and MEP Components", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Space", "SpaceName", true, SgAgency.BCA, GCon, "Master Bath, Maid Bath, Yard Bath", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_WallDimension", "InternalLength", false, SgAgency.BCA, GCon, "-", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_WallDimension", "InternalWidth", false, SgAgency.BCA, GCon, "-", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_WallDimension", "ConstructionMethod", false, SgAgency.BCA, GCon, "Prefab, CIS, PC, PBU", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_Wall", "Accreditation_MAS", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_SteelConnection", "MechanicalConnectionType", false, SgAgency.BCA, GCon, "-", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_WallDimension", "Thickness", false, SgAgency.BCA, GCon, "300", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_DiscreteAccessory", "PreInsulated", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.BCA, GCon, "-", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_PipeSegment", "SystemType", true, SgAgency.BCA, GCon, "Chilled Water", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
            R("SGPset_DiscreteAccessory", "IsCommon", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Prefabricated Building Systems and MEP Components", "COP 3.1 Dec 2025 p.326"),
        }));

        // Project Development Type
        Add(E("CX-044", "Project Development Type", "IFCBUILDING", "NOTDEFINED", "A", SgAgency.URA, new[]
        {
            R("SGPset_BuildingElementProxy", "OwnerBuiltOwnerStay", true, SgAgency.URA, GCon, "TRUE / FALSE", "IFC+SG: Project Development Type", "COP 3.1 Dec 2025 p.328"),
            R("SGPset_BuildingElementProxy", "ProjectDevelopmentType", true, SgAgency.URA, GCon, "Residential (landed), Residential (non- landed), Mixed Development, Commercial,", "IFC+SG: Project Development Type", "COP 3.1 Dec 2025 p.328"),
        }));

        // Pump
        Add(E("CX-045", "Pump", "IFCPUMP", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_Pump", "Capacity", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pump", "COP 3.1 Dec 2025 p.329"),
            R("SGPset_Pump", "Duty", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Pump", "COP 3.1 Dec 2025 p.329"),
            R("SGPset_Pump", "Standby", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Pump", "COP 3.1 Dec 2025 p.329"),
            R("SGPset_Pump", "PumpHead", false, SgAgency.PUB, GCon, "1, 2", "IFC+SG: Pump", "COP 3.1 Dec 2025 p.329"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pump", "COP 3.1 Dec 2025 p.329"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "-", "IFC+SG: Pump", "COP 3.1 Dec 2025 p.329"),
        }));

        // Railing
        Add(E("CX-047", "Railing", "IFCRAILING", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_RailingDimension", "Height", false, SgAgency.BCA, GCon, "1000", "IFC+SG: Railing", "COP 3.1 Dec 2025 p.331"),
            R("SGPset_RailingDimension", "Material", false, SgAgency.BCA, GCon, "-", "IFC+SG: Railing", "COP 3.1 Dec 2025 p.331"),
            R("SGPset_Railing", "SafetyBarrier", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Railing", "COP 3.1 Dec 2025 p.331"),
            R("SGPset_Railing", "TypeOfBarrier", false, SgAgency.BCA, GCon, "-", "IFC+SG: Railing", "COP 3.1 Dec 2025 p.331"),
            R("SGPset_Railing", "IsLaminated", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Railing", "COP 3.1 Dec 2025 p.331"),
        }));

        // Ramp
        Add(E("CX-048", "Ramp", "IFCRAMP", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_CivilElement", "Gradient", false, SgAgency.BCA, GCon, "1:16", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_RampDimension", "Width", false, SgAgency.BCA, GCon, "1200", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_RampAccessibility", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_Ramp", "TransitionRamp", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_Ramp", "Accessway", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_Ramp", "Egress", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_Ramp", "Ingress", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_Ramp", "Vehicular", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
            R("SGPset_RampDimension", "Material", false, SgAgency.BCA, GCon, "-", "IFC+SG: Ramp", "COP 3.1 Dec 2025 p.332"),
        }));

        // Refuse Chute / Recyclables Chute
        Add(E("CX-049", "Refuse Chute / Recyclables Chute", "IFCWALL", "NOTDEFINED", "A", SgAgency.NEA, new[]
        {
            R("SGPset_Space", "SpaceName", true, SgAgency.NEA, GCon, "Refuse Chute, Recyclables Chute", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_WallDimension", "ConstructionMethod", false, SgAgency.NEA, GCon, "Precast, Prefab, CIS", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_WallDimension", "InnerLength", false, SgAgency.NEA, GCon, "-", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_WallDimension", "InnerWidth", false, SgAgency.NEA, GCon, "-", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_WallDimension", "OuterLength", false, SgAgency.NEA, GCon, "-", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_WallDimension", "OuterWidth", false, SgAgency.NEA, GCon, "-", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Wall", "ChamferRadius", false, SgAgency.NEA, GCon, "-", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Door", "AirTight", true, SgAgency.NEA, GCon, "TRUE / FALSE", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_DoorFireDoor", "FireRating", false, SgAgency.NEA, GCon, "½-hr , 1-hr etc.", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Door", "SelfClosing", true, SgAgency.NEA, GCon, "TRUE / FALSE", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Door", "VolumeControlled", true, SgAgency.NEA, GCon, "TRUE / FALSE", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_DoorAccessibility", "ClearWidth", false, SgAgency.NEA, GCon, "335", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_DoorAccessibility", "ClearHeight", false, SgAgency.NEA, GCon, "335", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_DoorDimension", "StructuralWidth", false, SgAgency.NEA, GCon, "No", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_DoorDimension", "StructuralHeight", false, SgAgency.NEA, GCon, "No", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_DoorDimension", "Material", false, SgAgency.NEA, GCon, "No", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_DoorDimension", "Thickness", false, SgAgency.NEA, GCon, "No", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Tank", "CompactionRatio", false, SgAgency.NEA, GCon, "1:3", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Tank", "NominalCapacity", false, SgAgency.NEA, GCon, "-", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Tank", "ColourCode", false, SgAgency.NEA, GCon, "Pantone 350c (for green colour general waste container/compactor), RAL 5005 (for", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_BuildingElementProxyDimension", "BasePlateMaterial", false, SgAgency.NEA, GCon, "Mild Steel", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_BuildingElementProxyDimension", "BasePlateThickness", false, SgAgency.NEA, GCon, "6", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Tank", "TailGateOrientation", false, SgAgency.NEA, GCon, "Inward Facing", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Space", "HookUpPoint", false, SgAgency.NEA, GCon, "Outward Facing", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
            R("SGPset_Tank", "EquipmentType", true, SgAgency.NEA, GCon, "RORO Compactor, RORO Container, Dust Screw Compactor, Rotary Drum Compactor", "IFC+SG: Refuse Chute / Recyclables Chute", "COP 3.1 Dec 2025 p.333"),
        }));

        // Refuse Handling Equipment
        Add(E("CX-050", "Refuse Handling Equipment", "IFCTANK", "NOTDEFINED", "A", SgAgency.NEA, new[]
        {
            R("SGPset_Tank", "Litre", false, SgAgency.NEA, GCon, "660", "IFC+SG: Refuse Handling Equipment", "COP 3.1 Dec 2025 p.336"),
        }));

        // Road
        Add(E("CX-051", "Road", "IFCCIVILELEMENT", "NOTDEFINED", "C", SgAgency.LTA, new[]
        {
            R("SGPset_BuildingElementProxy", "DesignedVehicleMass", false, SgAgency.LTA, GCon, "-", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_BuildingElementProxy", "Egress", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_BuildingElementProxy", "Ingress", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.LTA, GCon, "-", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_BuildingElementProxy", "RoadCategory", false, SgAgency.LTA, GCon, "-", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_CivilElement", "LoadingCapacity^", true, SgAgency.LTA, GCon, "24, 30, 50", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_CivilElementDimension", "Width", false, SgAgency.LTA, GCon, "-", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_CivilElement", "Vehicular", true, SgAgency.LTA, GCon, "TRUE / FALSE", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_CivilElement", "KerbType", false, SgAgency.LTA, GCon, "K2A", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_CivilElementDimension", "Thickness", false, SgAgency.LTA, GCon, "-", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
            R("SGPset_CivilElementDimension", "Height", false, SgAgency.LTA, GCon, "-", "IFC+SG: Road", "COP 3.1 Dec 2025 p.337"),
        }));

        // Roof
        Add(E("CX-052", "Roof", "IFCROOF", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "ConstructionMethod", false, SgAgency.BCA, GCon, "-", "IFC+SG: Roof", "COP 3.1 Dec 2025 p.339"),
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.BCA, GCon, "-", "IFC+SG: Roof", "COP 3.1 Dec 2025 p.339"),
        }));

        // Sanitary Appliances
        Add(E("CX-053", "Sanitary Appliances", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Mounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "PanMounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
        }));

        // Sanitary Appliances
        Add(E("CX-054", "Sanitary Appliances", "IFCSANITARYTERMINAL", "BATH", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Mounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "PanMounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
        }));

        // Sanitary Appliances
        Add(E("CX-055", "Sanitary Appliances", "IFCSANITARYTERMINAL", "BIDET", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Mounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "PanMounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
        }));

        // Sanitary Appliances
        Add(E("CX-056", "Sanitary Appliances", "IFCSANITARYTERMINAL", "SHOWER", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Mounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "PanMounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
        }));

        // Sanitary Appliances
        Add(E("CX-057", "Sanitary Appliances", "IFCSANITARYTERMINAL", "URINAL", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Mounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "PanMounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
        }));

        // Sanitary Appliances
        Add(E("CX-058", "Sanitary Appliances", "IFCSANITARYTERMINAL", "WASHHANDBASIN", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Mounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "PanMounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
        }));

        // Sanitary Appliances
        Add(E("CX-059", "Sanitary Appliances", "IFCSANITARYTERMINAL", "WATERCLOSET", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "Sanitary", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Mounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "PanMounting", false, SgAgency.PUB, GCon, "-", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Sanitary Appliances", "COP 3.1 Dec 2025 p.340"),
        }));

        // Seating
        Add(E("CX-060", "Seating", "IFCFURNITURE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Furniture", "SeatingCapacity", false, SgAgency.BCA, GCon, "-", "IFC+SG: Seating", "COP 3.1 Dec 2025 p.343"),
        }));

        // Shading Device
        Add(E("CX-063", "Shading Device", "IFCSHADINGDEVICE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_ShadingDevice", "ShadingDevice", false, SgAgency.BCA, GCon, "-", "IFC+SG: Shading Device", "COP 3.1 Dec 2025 p.346"),
        }));

        // Signage
        Add(E("CX-064", "Signage", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "MountingHeight", false, SgAgency.SCDF, GCon, "-", "IFC+SG: Signage", "COP 3.1 Dec 2025 p.347"),
        }));

        // Site
        Add(E("CX-065", "Site", "IFCSITE", "NOTDEFINED", "A", SgAgency.SLA, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "NumberOfWorkers", false, SgAgency.SLA, GCon, "-", "IFC+SG: Site", "COP 3.1 Dec 2025 p.348"),
        }));

        // Site Boundary
        Add(E("CX-066", "Site Boundary", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "A", SgAgency.SLA, new[]
        {
            R("SGPset_GeographicElement", "Area", false, SgAgency.SLA, GCon, "N.A.", "IFC+SG: Site Boundary", "COP 3.1 Dec 2025 p.349"),
            R("SGPset_GeographicElement", "BroadLandUse*", true, SgAgency.SLA, GCon, "Agriculture, Beach Area, Business 1, Business 1- White, Business 2, Business 2-", "IFC+SG: Site Boundary", "COP 3.1 Dec 2025 p.349"),
            R("SGPset_GeographicElement", "VacantLand*", true, SgAgency.SLA, GCon, "TRUE / FALSE", "IFC+SG: Site Boundary", "COP 3.1 Dec 2025 p.349"),
        }));

        // Slab
        Add(E("CX-068", "Slab", "IFCSLAB", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGpset_Slab", "Accreditation_MAS", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "BottomDistribution_nominal", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "BottomMain_nominal", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_SlabDimension", "ConstructionMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "LatticeGirderReinforcement", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "LoadBearing", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_SlabDimension", "Mark", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_GeographicElement", "MaterialGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_SteelConnection", "MechanicalConnectionType", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "ReferTo2DDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_GeographicElement", "ReinforcementSteelGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "ShelterUsage", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "SlabType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_SlabReinforcement", "Stirrups", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_SlabReinforcement", "StirrupsType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGPset_SlabDimension", "Thickness", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "TopDistribution_nominal", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "TopMain_nominal", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "TypeDesignator", false, SgAgency.BCA, GCon, "No", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "WeldedMesh", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "10", false, SgAgency.BCA, GCon, "", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "11", false, SgAgency.BCA, GCon, "", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "12", false, SgAgency.BCA, GCon, "", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "13", false, SgAgency.BCA, GCon, "", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
            R("SGpset_Slab", "14", false, SgAgency.BCA, GCon, "", "IFC+SG: Slab", "COP 3.1 Dec 2025 p.353"),
        }));

        // Soffit
        Add(E("CX-069", "Soffit", "IFCCOVERING", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("SGPset_RoofFireRating", "FireRating", false, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Soffit", "COP 3.1 Dec 2025 p.355"),
        }));

        // IFC Entity: IfcSpace
        Add(E("CX-070", "IFC Entity: IfcSpace", "IFCSPACE", "AREA_GFA", "A", SgAgency.URA, new[]
        {
            R("SGPset_SpaceGFA", "AGF_DevelopmentUse", true, SgAgency.URA, GCon, "Agriculture, Beach Area, Business Park, Business 1, Business 2, Cemetery, Civic & Community Institution, Commercial, Educational Institution, Health & Medical Care, Hotel, Open Space, Park, Place of Worship, Port/Airport, Rapid Transit, Reserve Site, Residential (Landed), Residential (Non-landed), Road, Special Use, Sports & Recreation, Transport Facilities, Utility, Waterbody", "IFC+SG Space GFA - URA AGF_DevelopmentUse: one of the 25 approved development use categories. Mandatory per COP 3.1 p.356-381.", "COP 3.1 Dec 2025 p.356 - URA GFA Handbook 2024"),
            R("SGPset_Space", "AGF_Name", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "AGF_UnitNumber", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_SpaceGFA", "AGF_BonusGFAType", false, SgAgency.URA, GCon, "Balcony Incentive Scheme, Conserved Bungalows Scheme, Community and Sports Facilities Scheme, Indoor Recreation Spaces Scheme, Built Environment Transformation Scheme, Rooftop ORA on Landscaped Roofs, ORA within POPS, CBD Incentive Scheme, Strategic Development Incentive (SDI) Scheme, Facade Articulation Scheme", "IFC+SG Space GFA - BonusGFA type. Required only when bonus GFA is claimed.", "COP 3.1 Dec 2025 p.362"),
            R("SGPset_Space", "AGF_Note", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_SpaceGFA", "AGF_UseQuantum", true, SgAgency.URA, GCon, "Predominant, Ancillary", "IFC+SG Space GFA - UseQuantum: whether this use is Predominant or Ancillary in the space.", "COP 3.1 Dec 2025 p.362"),
            R("SGPset_Space", "AGF_BuildingTypology", false, SgAgency.URA, GCon, "• Light Industry • Clean Industry • General Industry • Special Industry", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "AGF_SupportingFacility", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "AST_AreaType [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "AST_LegalArea [Number]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "AST_Prop_StrataLotNumber [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "AST_Extg_StrataLotNumber [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ACN_ConncectivityType [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ACN_ActivityGeneratingUseType [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ACN_IsPavingSpecified [Boolean]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ACN_PavingSpecification [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ACN_IsOpen24HoursToPublic [Boolean]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ACN_OpenTime [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ACN_CloseTime [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_GeographicElement", "ALS_LandscapeType [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ALS_GreeneryFeatures [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "ALS_Species [Text]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
            R("SGPset_Space", "AVF_IncludeAsGFA [Boolean]", false, SgAgency.URA, GCon, "", "IFC+SG: IFC Entity: IfcSpace", "COP 3.1 Dec 2025 p.356"),
        }));

        // Space (Usage)
        Add(E("CX-071", "Space (Usage)", "IFCSPACE", "SPACE", "A", SgAgency.URA, new[]
        {
            R("SGPset_Space", "Mark", false, SgAgency.URA, GCon, "", "IFC+SG: Space (Usage)", "COP 3.1 Dec 2025"),
        }));

        // Sprinkler (Non-Fire; For NEA)
        Add(E("CX-072", "Sprinkler (Non-Fire; For NEA)", "IFCSANITARYTERMINAL", "SPRINKLER", "M", SgAgency.NEA, new[]
        {
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.NEA, GCon, "-", "IFC+SG: Sprinkler (Non-Fire; For NEA)", "COP 3.1 Dec 2025 p.418"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.NEA, GCon, "-", "IFC+SG: Sprinkler (Non-Fire; For NEA)", "COP 3.1 Dec 2025 p.418"),
        }));

        // Staircase
        Add(E("CX-073", "Staircase", "IFCSTAIR", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_StairFireEscape", "FireExit", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "NumberOfRisers", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "RiserHeight", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "NumberOfTreads", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "TreadLength", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_GeographicElement", "MaterialGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "ConstructionMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Space", "SpaceName", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "BottomDistribution", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "BottomMain", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_SteelConnection", "ConnectionDetailsBottom", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_SteelConnection", "ConnectionDetailsTop", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_SteelConnection", "ConnectionTypeBottom", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_SteelConnection", "ConnectionTypeTop", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "Mark", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "MemberSection", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "ReferTo2DDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_GeographicElement", "ReinforcementSteelGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "SectionFabricationMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "Thickness", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "TopDistribution", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "TopMain", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_StairFlight", "Width", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_SteelConnection", "MechanicalConnectionType", false, SgAgency.BCA, GCon, "No", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "10", false, SgAgency.BCA, GCon, "", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "11", false, SgAgency.BCA, GCon, "", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "12", false, SgAgency.BCA, GCon, "", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "13", false, SgAgency.BCA, GCon, "", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
            R("SGPset_Stair", "14", false, SgAgency.BCA, GCon, "", "IFC+SG: Staircase", "COP 3.1 Dec 2025 p.420"),
        }));

        // Tank
        Add(E("CX-074", "Tank", "IFCTANK", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_Space", "IsPotable", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "NominalCapacity", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "EffectiveCapacity", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Diameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Height", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Length", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Thickness", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "TradeEffluent", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "EquipmentType", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "SpaceName", true, SgAgency.PUB, GCon, "oil tank room, balancing tank, detention tank, domestic water tank, rainwater ha", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "Area", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
        }));

        // Tank
        Add(E("CX-075", "Tank", "IFCSPACE", "STORAGE", "P", SgAgency.PUB, new[]
        {
            R("SGPset_Space", "IsPotable", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "NominalCapacity", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "EffectiveCapacity", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Diameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Height", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Length", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Thickness", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_BuildingElementProxyDimension", "Width", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "TradeEffluent", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "EquipmentType", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "SpaceName", true, SgAgency.PUB, GCon, "oil tank room, balancing tank, detention tank, domestic water tank, rainwater ha", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
            R("SGPset_Space", "Area", false, SgAgency.PUB, GCon, "-", "IFC+SG: Tank", "COP 3.1 Dec 2025 p.427"),
        }));

        // Type Bedding for Pipe
        Add(E("CX-076", "Type Bedding for Pipe", "IFCPIPESEGMENT", "NOTDEFINED", "C", SgAgency.PUB, new[]
        {
            R("SGPset_PipeSegment", "BeddingType", false, SgAgency.PUB, GCon, "Type 1, Type 2, Type 3", "IFC+SG: Type Bedding for Pipe", "COP 3.1 Dec 2025 p.428"),
        }));

        // Valve
        Add(E("CX-077", "Valve", "IFCVALVE", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "-", "IFC+SG: Valve", "COP 3.1 Dec 2025 p.429"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Valve", "COP 3.1 Dec 2025 p.429"),
        }));

        // Wall
        Add(E("CX-078", "Wall", "IFCWALL", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_WallDimension", "ConstructionMethod", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "IsPartyWall", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "ArrangementType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "BeamFacade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "DoubleBayFacade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "HorizontalRebar", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "IsExternal", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "LoadBearing", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_WallDimension", "Mark", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_GeographicElement", "MaterialGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_SteelConnection", "MechanicalConnectionType", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "PrefabricatedReinforcementCage", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "PrefinishedFacade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "ReferTo2DDetail", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_GeographicElement", "ReinforcementSteelGrade", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "ShelterUsage", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_WallReinforcement", "Stirrups", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_WallReinforcement", "StirrupsType", false, SgAgency.BCA, GCon, "Yes", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_WallDimension", "Thickness", false, SgAgency.BCA, GCon, "No*", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "VerticalRebar", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_PileFoundation", "WorkingLoad_DA1-1", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_PileFoundation", "WorkingLoad_DA1-2", false, SgAgency.BCA, GCon, "No", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "10", false, SgAgency.BCA, GCon, "", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
            R("SGPset_Wall", "11", false, SgAgency.BCA, GCon, "", "IFC+SG: Wall", "COP 3.1 Dec 2025 p.430"),
        }));

        // Waste Terminal
        Add(E("CX-079", "Waste Terminal", "IFCWASTETERMINAL", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Material", false, SgAgency.PUB, GCon, "-", "IFC+SG: Waste Terminal", "COP 3.1 Dec 2025 p.436"),
            R("SGPset_WasteTerminal", "TradeEffluent", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Waste Terminal", "COP 3.1 Dec 2025 p.436"),
            R("SGPset_WasteTerminal", "SystemType", false, SgAgency.PUB, GCon, "Sanitary, Sewerage", "IFC+SG: Waste Terminal", "COP 3.1 Dec 2025 p.436"),
            R("SGPset_WasteTerminal", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Waste Terminal", "COP 3.1 Dec 2025 p.436"),
        }));

        // Water Meter
        Add(E("CX-080", "Water Meter", "IFCFLOWMETER", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_FlowMeter", "Capacity", false, SgAgency.PUB, GCon, "-", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_FlowMeterDimension", "Diameter", false, SgAgency.PUB, GCon, "-", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_FlowMeterDimension", "Length", false, SgAgency.PUB, GCon, "-", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_FlowMeter", "Purpose", false, SgAgency.PUB, GCon, "Private", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_FlowMeter", "UnitNumber", false, SgAgency.PUB, GCon, "-", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_FlowMeter", "UnitNumberTag", true, SgAgency.PUB, GCon, "TRUE / FALSE", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_FlowMeter", "WaterSupplySource", false, SgAgency.PUB, GCon, "-", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_PipeSegment", "SystemType", false, SgAgency.PUB, GCon, "-", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
            R("SGPset_PipeSegment", "SystemName", false, SgAgency.PUB, GCon, "-", "IFC+SG: Water Meter", "COP 3.1 Dec 2025 p.437"),
        }));

        // Window
        Add(E("CX-081", "Window", "IFCWINDOW", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_PipeSegment", "InnerDiameter", false, SgAgency.BCA, GCon, "-", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
            R("SGPset_PipeSegment", "OuterDiameter", false, SgAgency.BCA, GCon, "-", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
            R("SGPset_WindowFireRating", "FireAccessOpening", true, SgAgency.BCA, GCon, "TRUE / FALSE", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
            R("SGPset_WindowDimension", "StructuralWidth", false, SgAgency.BCA, GCon, "-", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
            R("SGPset_WindowDimension", "StructuralHeight", false, SgAgency.BCA, GCon, "-", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
            R("SGPset_WindowDimension", "Material", false, SgAgency.BCA, GCon, "-", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
            R("SGPset_WindowDimension", "SafetyBarrierHeight", false, SgAgency.BCA, GCon, "-", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
            R("SGPset_Window", "PercentageOfOpening", false, SgAgency.BCA, GCon, "-", "IFC+SG: Window", "COP 3.1 Dec 2025 p.438"),
        }));


        
        // ════════════════════════════════════════════════════════════════════
        // MALAYSIA NBeS IFC MAPPING 2024 (CIDB 2nd Edition) - Additional Codes
        // ════════════════════════════════════════════════════════════════════

        Add(E("MY-BEM-RCC", "RC Beam (Malaysia NBeS)", "IFCBEAM", "BEAM", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Beam",              "Mark",               true,  SgAgency.BCA, GCon, "",          "NBeS: Beam mark matching structural drawings",   "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Beam",              "MaterialGrade",      true,  SgAgency.BCA, GCon, "C30, C35, C40", "NBeS: Concrete grade per MS EN 206",        "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Beam",              "ConstructionMethod", true,  SgAgency.BCA, GCon, "CIS, PC, IBS",  "NBeS: Construction method inc IBS",          "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_BeamReinforcement", "BottomMain_nominal", false, SgAgency.BCA, GCon, "3H25",          "NBeS: Bottom main reinforcement notation",    "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_BeamReinforcement", "TopMain_nominal",    false, SgAgency.BCA, GCon, "2H25",          "NBeS: Top main reinforcement notation",       "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_BeamDimension",     "Width",              false, SgAgency.BCA, GCon, "250",           "NBeS: Beam width in mm",                     "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_BeamDimension",     "Depth",              false, SgAgency.BCA, GCon, "600",           "NBeS: Beam depth in mm",                     "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-COL-STL", "Steel Column (Malaysia NBeS)", "IFCCOLUMN", "COLUMN", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Column",            "Mark",               true,  SgAgency.BCA, GCon, "",              "NBeS: Column mark",                          "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Column",            "MaterialGrade",      true,  SgAgency.BCA, GCon, "Grade 43, Grade 50", "NBeS: Steel grade per MS EN 10025",     "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Column",            "MemberSection",      true,  SgAgency.BCA, GCon, "203x203x46UC",  "NBeS: UC/RHS/CHS section size",              "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_SteelConnection",   "ConnectionTypeBottom", false, SgAgency.BCA, GCon, "Pinned, Fixed", "NBeS: Base connection type",               "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-SLB-RCC", "RC Slab (Malaysia NBeS)", "IFCSLAB", "FLOOR", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Slab",              "Mark",               true,  SgAgency.BCA, GCon, "",              "NBeS: Slab mark",                            "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Slab",              "MaterialGrade",      true,  SgAgency.BCA, GCon, "C30, C35",      "NBeS: Concrete grade",                       "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Slab",              "ConstructionMethod", true,  SgAgency.BCA, GCon, "CIS, PC, IBS",  "NBeS: Construction method",                  "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_SlabDimension",     "Thickness",          true,  SgAgency.BCA, GCon, "150",           "NBeS: Slab thickness in mm, min 125mm",      "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_SlabFireRating",    "FireRating",         true,  SgAgency.BCA, GCon, "60, 120",       "NBeS: Fire resistance period in minutes",    "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-WAL-LBW", "Load-Bearing RC Wall (Malaysia NBeS)", "IFCWALL", "SOLIDWALL", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Wall",              "Mark",               true,  SgAgency.BCA, GCon, "",              "NBeS: Wall mark",                            "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Wall",              "MaterialGrade",      true,  SgAgency.BCA, GCon, "C30, C35",      "NBeS: Concrete grade",                       "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_WallDimension",     "Thickness",          true,  SgAgency.BCA, GCon, "150, 200",      "NBeS: Wall thickness in mm, min 150mm",      "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_WallFireRating",    "FireRating",         true,  SgAgency.BCA, GCon, "60, 120, 180",  "NBeS: Fire resistance in minutes",           "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Wall",              "LoadBearing",        true,  SgAgency.BCA, GCon, "TRUE",          "NBeS: Mark as load bearing",                 "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-FTG-RAT", "Raft Foundation (Malaysia NBeS)", "IFCFOOTING", "PAD_FOOTING", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Footing",           "Mark",               true,  SgAgency.BCA, GCon, "",              "NBeS: Footing mark",                         "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Footing",           "MaterialGrade",      true,  SgAgency.BCA, GCon, "C30, C35",      "NBeS: Concrete grade",                       "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_FootingFoundation", "FootingType",        true,  SgAgency.BCA, GCon, "RAFT, PAD, STRIP", "NBeS: Foundation type",                  "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-PIL-DRV", "Driven Pile (Malaysia NBeS)", "IFCPILE", "DRIVEN", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Pile",              "Mark",               true,  SgAgency.BCA, GCon, "",              "NBeS: Pile mark",                            "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Pile",              "ConstructionMethod", true,  SgAgency.BCA, GPil, "DRIVEN",        "NBeS: Pile construction method",             "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_PileDimension",     "Diameter",           true,  SgAgency.BCA, GPil, "300, 350, 400", "NBeS: Pile diameter in mm",                  "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_PileFoundation",    "CutOffLevel_SHD",    true,  SgAgency.BCA, GPil, "",              "NBeS: Cut-off level",                        "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_PileStructuralLoad","DA1-1_CompressionCapacity", true, SgAgency.BCA, GPil, "", "NBeS: Design compression capacity kN",         "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-SPC-OFF", "Office Space (Malaysia NBeS)", "IFCSPACE", "SPACE", "A", SgAgency.URA, new[]
        {
            R("SGPset_Space",             "SpaceUsage",         true,  SgAgency.URA, GCon, "OFFICE",        "NBeS: Space usage type",                     "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Space",             "GrossArea",          true,  SgAgency.URA, GCon, "",              "NBeS: Gross area in m²",                     "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Space",             "Height",             true,  SgAgency.URA, GCon, "2.8",           "NBeS: Ceiling height in m, min 2.8m",        "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Space",             "AirChangeRate",      false, SgAgency.NEA, GCon, "6",             "NBeS: Air changes per hour, min 6 ACH",      "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-WIN-EXW", "External Window (Malaysia NBeS)", "IFCWINDOW", "WINDOW", "A", SgAgency.BCA, new[]
        {
            R("SGPset_WindowDimension",   "StructuralWidth",    true,  SgAgency.BCA, GCon, "",              "NBeS: Structural opening width in mm",       "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_WindowDimension",   "StructuralHeight",   true,  SgAgency.BCA, GCon, "",              "NBeS: Structural opening height in mm",      "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_WindowPerformance", "ThermalTransmittance", false, SgAgency.BCA, GCon, "<=4.0",       "NBeS: U-value W/m²K, max 4.0 for GBI",      "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-SAN-WC", "Water Closet (Malaysia NBeS)", "IFCSANITARYTERMINAL", "WATERCLOSET", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal",  "SystemType",         true,  SgAgency.PUB, GCon, "SOIL_WASTE",    "NBeS: Plumbing system type",                 "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_SanitaryTerminal",  "WELS",               true,  SgAgency.PUB, GCon, "1, 2, 3",       "NBeS: WELS/SIRIM water efficiency rating",   "NBeS IFC Mapping 2024 CIDB"),
        }));

        Add(E("MY-SIT-GND", "Site (Malaysia NBeS)", "IFCSITE", "NOTDEFINED", "A", SgAgency.SLA, new[]
        {
            R("SGPset_Site",              "LandLotNumber",      true,  SgAgency.SLA, GCon, "",              "NBeS: Land lot number from title deed",       "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Site",              "RefLongitude",       true,  SgAgency.SLA, GCon, "",              "NBeS: GDM2000 longitude / RSO Easting",       "NBeS IFC Mapping 2024 CIDB"),
            R("SGPset_Site",              "RefLatitude",        true,  SgAgency.SLA, GCon, "",              "NBeS: GDM2000 latitude / RSO Northing",       "NBeS IFC Mapping 2024 CIDB"),
        }));


        return d;
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private const CorenetGateway GDes = CorenetGateway.Design;
    private const CorenetGateway GPil = CorenetGateway.Piling;
    private const CorenetGateway GCon = CorenetGateway.Construction;

    private static ClassificationCodeEntry E(
        string code, string name, string ifcClass, string predType,
        string discipline, SgAgency primaryAgency,
        CodePropertyRule[] rules)
        => new()
        {
            Code           = code,
            Name           = name,
            IfcClass       = ifcClass,
            PredefinedType = predType,
            Discipline     = discipline,
            PrimaryAgency  = primaryAgency,
            Rules          = rules.ToList()
        };

    private static CodePropertyRule R(
        string pset, string prop, bool required, SgAgency agency,
        CorenetGateway gateway, string expected, string description, string regulation)
        => new()
        {
            PropertySetName = pset,
            PropertyName    = prop,
            IsRequired      = required,
            Agency          = agency,
            Gateway         = gateway,
            ExpectedValue   = expected,
            Description     = description,
            Regulation      = regulation
        };
}
