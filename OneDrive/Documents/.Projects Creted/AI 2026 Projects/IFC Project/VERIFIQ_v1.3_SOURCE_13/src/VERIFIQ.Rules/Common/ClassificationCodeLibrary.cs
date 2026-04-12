// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// ─── IFC+SG CLASSIFICATION CODE LIBRARY ──────────────────────────────────────
//
// Comprehensive embedded library of IFC+SG classification codes based on:
//   • CORENET-X COP 3rd Edition (October 2025)
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
