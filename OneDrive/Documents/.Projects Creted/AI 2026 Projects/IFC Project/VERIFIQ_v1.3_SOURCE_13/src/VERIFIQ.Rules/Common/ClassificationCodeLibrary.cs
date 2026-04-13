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

        // Accessible Route
        Add(E("CX-001", "Accessible Route", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_SpaceDimension", "Width", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Accessible Route", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Accessible Route", "BCA Mapping Dec 2025"),
        }));

        // Beam
        Add(E("CX-002", "Beam", "IFCBEAM", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Beam", "BeamSpanType", true, SgAgency.BCA, GCon, "Single, End, Interior, Cantilever", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_BeamReinforcement", "BottomLeft", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_BeamReinforcement", "BottomMiddle", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_BeamReinforcement", "BottomRight", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_Beam", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_BeamDimension", "Depth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_SteelConnection", "LeftConnectionDetail", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_SteelConnection", "LeftConnectionType", true, SgAgency.BCA, GCon, "Pinned, Fixed, Free", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_BeamDimension", "Mark", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "MaterialGrade", true, SgAgency.BCA, GCon, "C12/15, C20/25, C30/37, C32/40, C35/45, C40/50, C5", "IFC+SG: Beam", "BCA Mapping Dec 2025"),
        }));

        // Borehole
        Add(E("CX-004", "Borehole", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_BuildingElementProxyDimension", "Depth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Borehole", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "Mark", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Borehole", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "SHDLevel_SPT_MoreThan_100N", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Borehole", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "SHDLevel_SPT_MoreThan_60N", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Borehole", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "TerminationLevel", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Borehole", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "TopLevel", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Borehole", "BCA Mapping Dec 2025"),
        }));

        // Breeching Inlet
        Add(E("CX-005", "Breeching Inlet", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "F", SgAgency.SCDF, new[]
        {
            R("SGPset_FireSuppressionTerminal", "Hose_NominalDiameter", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Breeching Inlet", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "ID", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Breeching Inlet", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "SystemName", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Breeching Inlet", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "SystemType", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Breeching Inlet", "BCA Mapping Dec 2025"),
        }));

        // Ceiling
        Add(E("CX-007", "Ceiling", "IFCCOVERING", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("Pset_CoveringCommon", "FireRating", true, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Ceiling", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Ceiling", "BCA Mapping Dec 2025"),
        }));

        // Column
        Add(E("CX-008", "Column", "IFCCOLUMN", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_PrecastConcreteElementGeneral", "ArrangementType", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_ColumnDimension", "Breadth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_SteelConnection", "ConnectionDetailsBottom", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_SteelConnection", "ConnectionDetailsTop", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_SteelConnection", "ConnectionTypeBottom", true, SgAgency.BCA, GCon, "Pinned, Fixed, Free", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_SteelConnection", "ConnectionTypeTop", true, SgAgency.BCA, GCon, "Pinned, Fixed, Free", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_Column", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_ColumnDimension", "Diameter", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_ColumnDimension", "EndStorey", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Column", "BCA Mapping Dec 2025"),
            R("SGPset_ColumnReinforcement", "MainRebar", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Column", "BCA Mapping Dec 2025"),
        }));

        // Control Element
        Add(E("CX-009", "Control Element", "IFCUNITARYCONTROLELEMENT", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_UnitaryControlElement", "PWCS_Flushing", true, SgAgency.NEA, GCon, "TRUE/FALSE", "IFC+SG: Control Element", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryControlElement", "Purpose", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Control Element", "BCA Mapping Dec 2025"),
        }));

        // Culvert/ Drains
        Add(E("CX-010", "Culvert/ Drains", "IFCCIVILELEMENT", "NOTDEFINED", "C", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Diameter", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Gradient", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Height", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Length", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElement", "LoadBearing", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElement", "SystemName", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElement", "SystemType", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Thickness", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Culvert/ Drains", "BCA Mapping Dec 2025"),
        }));

        // Damper
        Add(E("CX-012", "Damper", "IFCDAMPER", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("SGPset_Damper", "FireRating", true, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Damper", "BCA Mapping Dec 2025"),
        }));

        // Distribution Chamber
        Add(E("CX-013", "Distribution Chamber", "IFCDISTRIBUTIONCHAMBERELEMENT", "NOTDEFINED", "P", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElementDimension", "Depth", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElementDimension", "Diameter", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElementDimension", "Height", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElement", "ID", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElementDimension", "InvertLevel", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElementDimension", "Length", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElement", "Status", true, SgAgency.PUB, GCon, "Existing, Proposed, To Be Removed, Abandoned, New,", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
            R("SGPset_DistributionChamberElement", "SystemName", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Distribution Chamber", "BCA Mapping Dec 2025"),
        }));

        // Door
        Add(E("CX-014", "Door", "IFCDOOR", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("SGPset_DoorDimension", "ClearHeight", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("SGPset_DoorDimension", "ClearWidth", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("SGPset_Door", "FireAccessOpening", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("Pset_DoorCommon", "FireExit", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("Pset_DoorCommon", "FireRating", true, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("SGPset_Door", "MainEntrance", true, SgAgency.NEA, GCon, "TRUE/FALSE", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("SGPset_Door", "OneWayLockingDevice", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Door", "BCA Mapping Dec 2025"),
            R("SGPset_Door", "OperationType", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Door", "BCA Mapping Dec 2025"),
        }));

        // Earthworks
        Add(E("CX-015", "Earthworks", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.URA, new[]
        {
            R("SGPset_GeographicElementDimension", "Area", true, SgAgency.URA, GCon, "N.A", "IFC+SG: Earthworks", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Status", true, SgAgency.URA, GCon, "Existing, Proposed", "IFC+SG: Earthworks", "BCA Mapping Dec 2025"),
        }));

        // Finishes
        Add(E("CX-019", "Finishes", "IFCCOVERING", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Finishes", "BCA Mapping Dec 2025"),
            R("Pset_CoveringCommon", "FireRating", true, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Finishes", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Finishes", "BCA Mapping Dec 2025"),
        }));

        // Fire Access Opening
        Add(E("CX-020", "Fire Access Opening", "IFCDOOR", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("SGPset_Door", "FireAccessOpening", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Fire Access Opening", "BCA Mapping Dec 2025"),
            R("SGPset_OpeningElement", "FireAccessOpening", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Fire Access Opening", "BCA Mapping Dec 2025"),
            R("SGPset_Window", "FireAccessOpening", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Fire Access Opening", "BCA Mapping Dec 2025"),
        }));

        // Fire Extinguisher
        Add(E("CX-022", "Fire Extinguisher", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("SGPset_BuildingElementProxy", "FireExtinguisherRating", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Fire Extinguisher", "BCA Mapping Dec 2025"),
        }));

        // Fire Hydrant
        Add(E("CX-023", "Fire Hydrant", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "F", SgAgency.SCDF, new[]
        {
            R("SGPset_FireSuppressionTerminal", "ID", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Fire Hydrant", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "Private", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Fire Hydrant", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "Public", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Fire Hydrant", "BCA Mapping Dec 2025"),
        }));

        // Foam Inlet / Outlet
        Add(E("CX-024", "Foam Inlet / Outlet", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "F", SgAgency.SCDF, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Foam Inlet / Outlet", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "SystemName", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Foam Inlet / Outlet", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "SystemType", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Foam Inlet / Outlet", "BCA Mapping Dec 2025"),
        }));

        // Footing
        Add(E("CX-025", "Footing", "IFCFOOTING", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_FootingReinforcement", "BottomDistribution", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_FootingReinforcement", "BottomMain", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_FootingDimension", "Breadth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_Footing", "DA1-1_BearingCapacity", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_Footing", "DA1-2_BearingCapacity", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_FootingDimension", "Depth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_FootingDimension", "Mark", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "MaterialGrade", true, SgAgency.BCA, GCon, "C12/15, C20/25, C30/37, C32/40, C35/45, C40/50, C5", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_Footing", "ReferTo2DDetail", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
            R("SGPset_Footing", "ReinforcementSteelGrade", true, SgAgency.BCA, GCon, "500A, 500B, 500C, 600A, 600B, 600C", "IFC+SG: Footing", "BCA Mapping Dec 2025"),
        }));

        // Footpath
        Add(E("CX-026", "Footpath", "IFCCIVILELEMENT", "NOTDEFINED", "C", SgAgency.BCA, new[]
        {
            R("SGPset_Material", "Material", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footpath", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Width", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Footpath", "BCA Mapping Dec 2025"),
        }));

        // Grating
        Add(E("CX-027", "Grating", "IFCDISCRETEACCESSORY", "NOTDEFINED", "A", SgAgency.PUB, new[]
        {
            R("SGPset_DiscreteAccessory", "SystemName", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Grating", "BCA Mapping Dec 2025"),
            R("SGPset_DiscreteAccessory", "SystemType", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Grating", "BCA Mapping Dec 2025"),
        }));

        // Green Verge
        Add(E("CX-028", "Green Verge", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.NParks, new[]
        {
            R("SGPset_GeographicElement", "ApprovedSoilMixture", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "ApprovedTurfSpecies", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElementDimension", "Area", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "ShrubSpecies", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Status", true, SgAgency.NParks, GCon, "Existing, Proposed, To be Removed", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_Landscape", "ALS_GreeneryFeatures", true, SgAgency.NParks, GCon, "Green Verge", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_Landscape", "ALS_LandscapeType", true, SgAgency.NParks, GCon, "Turfing, Groundcover, Shrubs", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_Landscape", "ALS_Status", true, SgAgency.NParks, GCon, "Existing, Proposed, To be Removed", "IFC+SG: Green Verge", "BCA Mapping Dec 2025"),
        }));

        // Gutter
        Add(E("CX-029", "Gutter", "IFCPIPESEGMENT", "NOTDEFINED", "M", SgAgency.PUB, new[]
        {
            R("SGPset_PipeSegment", "SystemName", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegment", "SystemType", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElement", "ConstructionMethod", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Height", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Length", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElement", "Public", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Thickness", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "Width", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "SystemName", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElementDimension", "SystemType", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Gutter", "BCA Mapping Dec 2025"),
        }));

        // Hose Reel
        Add(E("CX-030", "Hose Reel", "IFCFIRESUPPRESSIONTERMINAL", "NOTDEFINED", "F", SgAgency.SCDF, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Hose Reel", "BCA Mapping Dec 2025"),
            R("SGPset_FireSuppressionTerminal", "Hose_NominalDiameter", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Hose Reel", "BCA Mapping Dec 2025"),
        }));

        // Household Shelter
        Add(E("CX-031", "Household Shelter", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Space", "SpaceName", true, SgAgency.BCA, GCon, "Household Shelter, Setback", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "ConstructionMethod", true, SgAgency.BCA, GCon, "Precast, Prefab, CIS", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "Area", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "InternalLength", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "InternalWidth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_Name", true, SgAgency.BCA, GCon, "Household Shelter", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_WallDimension", "Thickness", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "ShelterUsage", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Household Shelter", "BCA Mapping Dec 2025"),
        }));

        // Interceptor
        Add(E("CX-032", "Interceptor", "IFCINTERCEPTOR", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_Interceptor", "ComplyToPUBStandardDrawing", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_Interceptor", "ReferToDrawingNumber", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_InterceptorDimension", "InvertLevel", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_InterceptorDimension", "TopLevel", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_InterceptorDimension", "Diameter", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_InterceptorDimension", "Height", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_InterceptorDimension", "Length", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_InterceptorDimension", "Width", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
            R("SGPset_InterceptorDimension", "TradeEffluent", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Interceptor", "BCA Mapping Dec 2025"),
        }));

        // Landscape Plants
        Add(E("CX-034", "Landscape Plants", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.NParks, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElementDimension", "Girth", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "HedgeNumber", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElementDimension", "Height", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "ReasonForRemoval", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Roadside", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "SingleStem", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Species", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Status", true, SgAgency.NParks, GCon, "Existing, Proposed, To be Removed, To be Transplan", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "TreeNumber", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Landscape Plants", "BCA Mapping Dec 2025"),
        }));

        // Lift
        Add(E("CX-035", "Lift", "IFCTRANSPORTELEMENT", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElement", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElementDimension", "Length", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElementDimension", "Width", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElementDimension", "ClearDepth", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElementDimension", "ClearHeight", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElementDimension", "ClearWidth", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElement", "FireFightingLift", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
            R("SGPset_TransportElement", "LiftType", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Lift", "BCA Mapping Dec 2025"),
        }));

        // Parking Lot
        Add(E("CX-036", "Parking Lot", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "FamilyLot", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "Length", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "Width", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "LotNumber", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "CarParking_ServedByCarLift", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "MechanisedParkingSystem", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "Perforated", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "OpenAtGrade", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Parking Lot", "BCA Mapping Dec 2025"),
        }));

        // Parking Lot (relevant elements)
        Add(E("CX-037", "Parking Lot (relevant elements)", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Space", "SpaceName", true, SgAgency.LTA, GCon, "Parking place", "IFC+SG: Parking Lot (relevant elements)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "VentilationMode", true, SgAgency.BCA, GCon, "Natural Ventilation, Air Conditioning, Mechanical ", "IFC+SG: Parking Lot (relevant elements)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "Area", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Parking Lot (relevant elements)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_Name", true, SgAgency.URA, GCon, "Car Parking Lot (Mechanised)", "IFC+SG: Parking Lot (relevant elements)", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "Length", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Parking Lot (relevant elements)", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "Width", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Parking Lot (relevant elements)", "BCA Mapping Dec 2025"),
        }));

        // Pile
        Add(E("CX-038", "Pile", "IFCPILE", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("SGPset_Pile", "BoreholeRef", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_PileDimension", "Breadth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_Pile", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_PileDimension", "CutOffLevel_SHD", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_Pile", "DA1-1_CompressionCapacity", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_PileStructuralLoad", "DA1-1_CompressionDesignLoad", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_Pile", "DA1-1_TensionCapacity", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_PileStructuralLoad", "DA1-1_TensionDesignLoad", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_Pile", "DA1-2_CompressionCapacity", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
            R("SGPset_PileStructuralLoad", "DA1-2_CompressionDesignLoad", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pile", "BCA Mapping Dec 2025"),
        }));

        // Pipes/ Ducts
        Add(E("CX-039", "Pipes/ Ducts", "IFCPIPESEGMENT", "NOTDEFINED", "M", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegment", "PreInsulated", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegment", "Perforated", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegment", "ConstructionMethod", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegment", "Gradient", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegmentDimension", "InnerDiameter", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegmentDimension", "Length", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegmentDimension", "Thickness", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
            R("SGPset_PipeSegment", "TradeEffluent", true, SgAgency.NEA, GCon, "TRUE/FALSE", "IFC+SG: Pipes/ Ducts", "BCA Mapping Dec 2025"),
        }));

        // Planting Areas
        Add(E("CX-041", "Planting Areas", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.NParks, new[]
        {
            R("SGPset_GeographicElementDimension", "Area", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "ApprovedSoilMixture", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Status", true, SgAgency.NParks, GCon, "Existing, Proposed, New, To be Removed", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Turf", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "TurfSpecies", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Compensated", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "Encroachment", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "CarparkProvision", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Planting Areas", "BCA Mapping Dec 2025"),
        }));

        // Pollution Control
        Add(E("CX-042", "Pollution Control", "IFCUNITARYEQUIPMENT", "NOTDEFINED", "M", SgAgency.NEA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_AmmoniaAndAmmonium", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_Antimony", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_Arsenic", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_Benzene", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_Cadmium", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_CarbonMonoxide", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_Chlorine", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_Copper", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
            R("SGPset_UnitaryEquipment", "AI_DioxinsAndFurans", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Pollution Control", "BCA Mapping Dec 2025"),
        }));

        // Prefabricated Building Systems and MEP Components
        Add(E("CX-043", "Prefabricated Building Systems and MEP Components", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "SpaceName", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "InternalLength", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "InternalWidth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "ConstructionMethod", true, SgAgency.BCA, GCon, "Prefab, CIS, PC, PBU", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "Accreditation_MAS", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "MechanicalConnectionType", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_Slab", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_SlabDimension", "Thickness", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Prefabricated Building Systems and MEP Components", "BCA Mapping Dec 2025"),
        }));

        // Project Development Type
        Add(E("CX-044", "Project Development Type", "IFCBUILDING", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Building", "OwnerBuiltOwnerStay", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Project Development Type", "BCA Mapping Dec 2025"),
            R("SGPset_Building", "ProjectDevelopmentType", true, SgAgency.BCA, GCon, "Residential (landed), Residential (non-landed), Mi", "IFC+SG: Project Development Type", "BCA Mapping Dec 2025"),
        }));

        // Pump
        Add(E("CX-045", "Pump", "IFCPUMP", "NOTDEFINED", "M", SgAgency.PUB, new[]
        {
            R("SGPset_Pump", "Capacity", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pump", "BCA Mapping Dec 2025"),
            R("SGPset_Pump", "Duty", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Pump", "BCA Mapping Dec 2025"),
            R("SGPset_Pump", "Standby", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Pump", "BCA Mapping Dec 2025"),
            R("SGPset_Pump", "PumpHead", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pump", "BCA Mapping Dec 2025"),
            R("SGPset_Pump", "SystemName", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pump", "BCA Mapping Dec 2025"),
            R("SGPset_Pump", "SystemType", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Pump", "BCA Mapping Dec 2025"),
        }));

        // Railing
        Add(E("CX-047", "Railing", "IFCRAILING", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Railing", "BCA Mapping Dec 2025"),
            R("SGPset_RailingDimension", "Height", true, SgAgency.BCA, GCon, "Any positive number", "IFC+SG: Railing", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Railing", "BCA Mapping Dec 2025"),
            R("SGPset_Railing", "SafetyBarrier", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Railing", "BCA Mapping Dec 2025"),
            R("SGPset_Railing", "TypeOfBarrier", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Railing", "BCA Mapping Dec 2025"),
            R("SGPset_Railing", "IsLaminated", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Railing", "BCA Mapping Dec 2025"),
        }));

        // Ramp
        Add(E("CX-048", "Ramp", "IFCRAMP", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_RampDimension", "Gradient", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_RampDimension", "Width", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_Ramp", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_Ramp", "TransitionRamp", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_Ramp", "Accessway", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_Ramp", "Egress", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_Ramp", "Ingress", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_Ramp", "Vehicular", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Ramp", "BCA Mapping Dec 2025"),
        }));

        // Refuse Chute / Recyclables Chute
        Add(E("CX-049", "Refuse Chute / Recyclables Chute", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Space", "SpaceName", true, SgAgency.BCA, GCon, "Refuse Chute, Recyclables Chute", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "ConstructionMethod", true, SgAgency.BCA, GCon, "Precast, Prefab, CIS", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "InnerLength", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "InnerWidth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "OuterLength", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "OuterWidth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "ChamferRadius", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "ConstructionMethod", true, SgAgency.NEA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
            R("SGPset_Door", "AirTight", true, SgAgency.NEA, GCon, "TRUE/FALSE", "IFC+SG: Refuse Chute / Recyclables Chute", "BCA Mapping Dec 2025"),
        }));

        // Refuse Handling Equipment
        Add(E("CX-050", "Refuse Handling Equipment", "IFCTANK", "NOTDEFINED", "M", SgAgency.NEA, new[]
        {
            R("SGPset_Tank", "Litre", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Refuse Handling Equipment", "BCA Mapping Dec 2025"),
        }));

        // Road
        Add(E("CX-051", "Road", "IFCCIVILELEMENT", "NOTDEFINED", "C", SgAgency.SCDF, new[]
        {
            R("SGPset_CivilElement", "DesignedVehicleMass", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElement", "Ingress", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_CivilElement", "Egress", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPSet_CivilElement", "RoadCategory", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "LoadingCapacity", true, SgAgency.SCDF, GCon, "24,30,50", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxyDimension", "Width", true, SgAgency.LTA, GCon, "N.A", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "Egress", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "Ingress", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Road", "BCA Mapping Dec 2025"),
            R("SGPset_BuildingElementProxy", "Vehicular", true, SgAgency.LTA, GCon, "TRUE/FALSE", "IFC+SG: Road", "BCA Mapping Dec 2025"),
        }));

        // Roof
        Add(E("CX-052", "Roof", "IFCROOF", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Roof", "ConstructionMethod", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Roof", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Roof", "BCA Mapping Dec 2025"),
            R("SGPset_Slab", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Roof", "BCA Mapping Dec 2025"),
            R("SGPset_Covering", "ConstructionMethod", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Roof", "BCA Mapping Dec 2025"),
        }));

        // Sanitary Appliances
        Add(E("CX-053", "Sanitary Appliances", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.BCA, new[]
        {
            R("SGPset_SanitaryTerminal", "SystemName", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Sanitary Appliances", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "SystemType", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Sanitary Appliances", "BCA Mapping Dec 2025"),
        }));

        // Sanitary Appliances (Bath)
        Add(E("CX-054", "Sanitary Appliances (Bath)", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Bath)", "BCA Mapping Dec 2025"),
        }));

        // Sanitary Appliances (Bidet)
        Add(E("CX-055", "Sanitary Appliances (Bidet)", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Bidet)", "BCA Mapping Dec 2025"),
        }));

        // Sanitary Appliances (Shower)
        Add(E("CX-056", "Sanitary Appliances (Shower)", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Shower)", "BCA Mapping Dec 2025"),
        }));

        // Sanitary Appliances (Urinal)
        Add(E("CX-057", "Sanitary Appliances (Urinal)", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.BCA, new[]
        {
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Urinal)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.NEA, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Urinal)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "Mounting", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Sanitary Appliances (Urinal)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "Waterless", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Urinal)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Urinal)", "BCA Mapping Dec 2025"),
        }));

        // Sanitary Appliances (Wash Basin)
        Add(E("CX-058", "Sanitary Appliances (Wash Basin)", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.NEA, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Wash Basin)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "Mounting", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Sanitary Appliances (Wash Basin)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Wash Basin)", "BCA Mapping Dec 2025"),
        }));

        // Sanitary Appliances (Water Closet)
        Add(E("CX-059", "Sanitary Appliances (Water Closet)", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.BCA, new[]
        {
            R("SGPset_SanitaryTerminal", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Water Closet)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "AmbulantDisabled", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Water Closet)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "ChildrenFriendly", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Water Closet)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "PanMounting", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Sanitary Appliances (Water Closet)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "ToiletPanType", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Sanitary Appliances (Water Closet)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "WELS", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Sanitary Appliances (Water Closet)", "BCA Mapping Dec 2025"),
        }));

        // Seating
        Add(E("CX-060", "Seating", "IFCFURNITURE", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("SGPset_Furniture", "SeatingCapacity", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Seating", "BCA Mapping Dec 2025"),
        }));

        // Shading Device
        Add(E("CX-063", "Shading Device", "IFCSHADINGDEVICE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_ShadingDevice", "ShadingDevice", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Shading Device", "BCA Mapping Dec 2025"),
        }));

        // Signage
        Add(E("CX-064", "Signage", "IFCBUILDINGELEMENTPROXY", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("SGPset_BuildingElementProxy", "MountingHeight", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Signage", "BCA Mapping Dec 2025"),
        }));

        // Site
        Add(E("CX-065", "Site", "IFCSITE", "NOTDEFINED", "A", SgAgency.URA, new[]
        {
            R("SGPset_Site", "NumberOfWorkers", true, SgAgency.URA, GCon, "N.A", "IFC+SG: Site", "BCA Mapping Dec 2025"),
        }));

        // Site Boundary
        Add(E("CX-066", "Site Boundary", "IFCGEOGRAPHICELEMENT", "NOTDEFINED", "L", SgAgency.URA, new[]
        {
            R("SGPset_GeographicElementDimension", "Area", true, SgAgency.URA, GCon, "N.A", "IFC+SG: Site Boundary", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "BroadLandUse", true, SgAgency.URA, GCon, "Agriculture, Beach Area, Business 1, Business 1- W", "IFC+SG: Site Boundary", "BCA Mapping Dec 2025"),
            R("SGPset_GeographicElement", "VacantLand", true, SgAgency.NParks, GCon, "TRUE/FALSE", "IFC+SG: Site Boundary", "BCA Mapping Dec 2025"),
        }));

        // Slab
        Add(E("CX-068", "Slab", "IFCSLAB", "NOTDEFINED", "S", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_Slab", "Accreditation_MAS", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_SlabReinforcement", "BottomDistribution_nominal", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_SlabReinforcement", "BottomMain_nominal", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_Slab", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_Slab", "LatticeGirderReinforcement", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_Slab", "LoadBearing", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_SlabDimension", "Mark", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "MaterialGrade", true, SgAgency.BCA, GCon, "C12/15, C20/25, C30/37, C32/40, C35/45, C40/50, C5", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
            R("SGPset_Slab", "MechanicalConnectionType", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Slab", "BCA Mapping Dec 2025"),
        }));

        // Soffit
        Add(E("CX-069", "Soffit", "IFCCOVERING", "NOTDEFINED", "A", SgAgency.SCDF, new[]
        {
            R("Pset_CoveringCommon", "FireRating", true, SgAgency.SCDF, GCon, "0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4", "IFC+SG: Soffit", "BCA Mapping Dec 2025"),
        }));

        // Space (Area)
        Add(E("CX-070", "Space (Area)", "IFCSPACE", "NOTDEFINED", "A", SgAgency.URA, new[]
        {
            R("SGPset_SpaceArea_GFA", "AGF_DevelopmentUse", true, SgAgency.URA, GCon, "Refer to Space Values sheet", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_Name", true, SgAgency.URA, GCon, "Refer to Space Values sheet", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_UnitNumber", true, SgAgency.URA, GCon, "N.A", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_BonusGFAType", true, SgAgency.URA, GCon, "Refer to Space Values sheet", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_Note", true, SgAgency.URA, GCon, "N.A", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_UseQuantum", true, SgAgency.URA, GCon, "Predominant, Ancillary", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_BuildingTypology", true, SgAgency.URA, GCon, "Refer to Space Values sheet", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_GFA", "AGF_SupportingFacility", true, SgAgency.URA, GCon, "Refer to Space Values sheet", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_Strata", "AST_AreaType", true, SgAgency.URA, GCon, "Refer to Space Values sheet", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceArea_Strata", "AST_LegalArea", true, SgAgency.URA, GCon, "N.A", "IFC+SG: Space (Area)", "BCA Mapping Dec 2025"),
        }));

        // Space (Usage)
        Add(E("CX-071", "Space (Usage)", "IFCSPACE", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("SGPset_Space", "Accreditation_MAS", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "AmbulantDisabled", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "Area", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "BarrierFreeAccessibility", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "ChildrenFriendly", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "CValue", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "ElderlyFriendly", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "EmergencyVoiceCommunicationSystem", true, SgAgency.BCA, GCon, "1-way EVC System, 2-way EVC System, Public Address", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "FireDetectionAndSuppressionSystem", true, SgAgency.BCA, GCon, "Automatic Fire Alarm System, Automatic Sprinkler S", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "FireEmergencyVentilationMode", true, SgAgency.BCA, GCon, "Natural Ventilation, Mechanical Ventilation, Press", "IFC+SG: Space (Usage)", "BCA Mapping Dec 2025"),
        }));

        // Sprinkler (Non-Fire; for NEA)
        Add(E("CX-072", "Sprinkler (Non-Fire; for NEA)", "IFCSANITARYTERMINAL", "NOTDEFINED", "P", SgAgency.NEA, new[]
        {
            R("SGPset_SanitaryTerminal", "SystemName", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Sprinkler (Non-Fire; for NEA)", "BCA Mapping Dec 2025"),
            R("SGPset_SanitaryTerminal", "SystemType", true, SgAgency.NEA, GCon, "N.A", "IFC+SG: Sprinkler (Non-Fire; for NEA)", "BCA Mapping Dec 2025"),
        }));

        // Staircase
        Add(E("CX-073", "Staircase", "IFCSTAIR", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Pset_StairCommon", "FireExit", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("Pset_StairFlightCommon", "NumberOfRiser", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("Pset_StairFlightCommon", "RiserHeight", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("Pset_StairFlightCommon", "NumberOfTreads", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("Pset_StairFlightCommon", "TreadLength", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "MaterialGrade", true, SgAgency.BCA, GCon, "C12/15, C20/25, C30/37, C32/40, C35/45, C40/50, C5", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("SGPset_StairFlight", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "SpaceName", true, SgAgency.BCA, GCon, "external exit staircase, internal exit staircase, ", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
            R("SGPset_StairReinforcement", "BottomDistribution", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Staircase", "BCA Mapping Dec 2025"),
        }));

        // Tank
        Add(E("CX-074", "Tank", "IFCTANK", "NOTDEFINED", "M", SgAgency.SCDF, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("SGPset_Tank", "IsPotable", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("Pset_TankTypeCommon", "NominalCapacity", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("Pset_TankTypeCommon", "EffectiveCapacity", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("SGPset_TankDimension", "Diameter", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("SGPset_TankDimension", "Height", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("SGPset_TankDimension", "Length", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("SGPset_TankDimension", "Thickness", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("SGPset_TankDimension", "Width", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
            R("SGPset_Tank", "TradeEffluent", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Tank", "BCA Mapping Dec 2025"),
        }));

        // Tank (RC Tank)
        Add(E("CX-075", "Tank (RC Tank)", "IFCSPACE", "NOTDEFINED", "A", SgAgency.PUB, new[]
        {
            R("SGPset_Space", "SpaceName", true, SgAgency.PUB, GCon, "balancing tank, detention tank, domestic booster t", "IFC+SG: Tank (RC Tank)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "Area", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank (RC Tank)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "Height", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank (RC Tank)", "BCA Mapping Dec 2025"),
            R("SGPset_SpaceDimension", "Thickness", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank (RC Tank)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "NominalCapacity", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank (RC Tank)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "EffectiveCapacity", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Tank (RC Tank)", "BCA Mapping Dec 2025"),
            R("SGPset_Space", "IsPotable", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Tank (RC Tank)", "BCA Mapping Dec 2025"),
        }));

        // Type Bedding for Pipe
        Add(E("CX-076", "Type Bedding for Pipe", "IFCPIPESEGMENT", "NOTDEFINED", "M", SgAgency.PUB, new[]
        {
            R("SGPset_PipeSegment", "BeddingType", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Type Bedding for Pipe", "BCA Mapping Dec 2025"),
        }));

        // Valve
        Add(E("CX-077", "Valve", "IFCVALVE", "NOTDEFINED", "M", SgAgency.BCA, new[]
        {
            R("SGPset_Valve", "SystemName", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Valve", "BCA Mapping Dec 2025"),
            R("SGPset_Valve", "SystemType", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Valve", "BCA Mapping Dec 2025"),
        }));

        // Wall
        Add(E("CX-078", "Wall", "IFCWALL", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.NParks, GCon, "N.A", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "ConstructionMethod", true, SgAgency.BCA, GCon, "CIS, PC, PT (Pre), PT (Post), PF, PPVC, Spun", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "IsPartyWall", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "ArrangementType", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "BeamFaçade", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "DoubleBayFaçade", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_WallReinforcement", "HorizontalRebar", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "IsExternal", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_Wall", "LoadBearing", true, SgAgency.BCA, GCon, "TRUE/FALSE", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
            R("SGPset_WallDimension", "Mark", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Wall", "BCA Mapping Dec 2025"),
        }));

        // Waste Terminal
        Add(E("CX-079", "Waste Terminal", "IFCWASTETERMINAL", "NOTDEFINED", "P", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Waste Terminal", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Waste Terminal", "BCA Mapping Dec 2025"),
            R("SGPset_WasteTerminal", "TradeEffluent", true, SgAgency.NEA, GCon, "TRUE/FALSE", "IFC+SG: Waste Terminal", "BCA Mapping Dec 2025"),
            R("SGPset_WasteTerminal", "SystemName", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Waste Terminal", "BCA Mapping Dec 2025"),
            R("SGPset_WasteTerminal", "SystemType", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Waste Terminal", "BCA Mapping Dec 2025"),
        }));

        // Water Meter
        Add(E("CX-080", "Water Meter", "IFCFLOWMETER", "NOTDEFINED", "P", SgAgency.PUB, new[]
        {
            R("SGPset_FlowMeter", "Capacity", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("SGPset_FlowMeterDimension", "Diameter", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("SGPset_FlowMeterDimension", "Length", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("Pset_FlowMeterOccurrence", "Purpose", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("SGPset_FlowMeter", "UnitNumber", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("SGPset_FlowMeter", "UnitNumberTag", true, SgAgency.PUB, GCon, "TRUE/FALSE", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("SGPset_FlowMeter", "WaterSupplySource", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("SGPset_FlowMeter", "SystemName", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
            R("SGPset_FlowMeter", "SystemType", true, SgAgency.PUB, GCon, "N.A", "IFC+SG: Water Meter", "BCA Mapping Dec 2025"),
        }));

        // Window
        Add(E("CX-081", "Window", "IFCWINDOW", "NOTDEFINED", "A", SgAgency.BCA, new[]
        {
            R("Please refer to property sets below", "Please refer to properties below", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_WindowDimension", "InnerDiameter", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_WindowDimension", "OuterDiameter", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_Window", "FireAccessOpening", true, SgAgency.SCDF, GCon, "TRUE/FALSE", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_WindowDimension", "StructuralWidth", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_WindowDimension", "StructuralHeight", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_Material", "Material", true, SgAgency.URA, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_Window", "SafetyBarrierHeight", true, SgAgency.SCDF, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
            R("SGPset_Window", "PercentageOfOpening", true, SgAgency.BCA, GCon, "N.A", "IFC+SG: Window", "BCA Mapping Dec 2025"),
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
