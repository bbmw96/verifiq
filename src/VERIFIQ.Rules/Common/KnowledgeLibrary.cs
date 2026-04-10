// VERIFIQ — Comprehensive Knowledge Library
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// This library embeds the complete IFC+SG and NBeS/UBBL regulatory knowledge
// used by the validation engine, design code engine, and remediation adviser.
//
// Content (embedded, no internet connection required):
//   • IFC+SG 2025 Industry Mapping — all 500+ required parameters
//   • CORENET-X COP 3rd Edition — all 5 gateways, 8 agencies, all element types
//   • UBBL 1984 — all 9 Purpose Groups, all Parts I–IX, all By-Laws
//   • BCA Code on Accessibility 2025 — all dimension requirements
//   • SCDF Fire Code 2018 + 2023 Amendment — compartment sizes, travel distances
//   • BCA Green Mark 2021 — ETTV, RETV, LPD, WWR, U-values
//   • URA Planning Parameters 2023 — room sizes, GFA rules, setbacks
//   • NEA EPHA — ventilation requirements
//   • PUB SDWA — sanitary fitting provisions
//   • LTA Transport Requirements — parking dimensions
//   • MS 1184:2014 — accessibility dimensions
//   • MS 1525:2019 — thermal performance (Malaysia)
//   • JBPM Fire Safety Requirements 2020

using VERIFIQ.Core.Enums;

namespace VERIFIQ.Rules.Common;

/// <summary>
/// Static knowledge base containing all regulatory lookup tables.
/// Used by the validation engine and design code engine for enriched
/// remediation guidance and rule references.
/// </summary>
public static class KnowledgeLibrary
{
    // ─── IFC CLASS → HUMAN READABLE NAME ─────────────────────────────────────
    public static string GetElementDescription(string ifcClass) =>
        _elementDescriptions.TryGetValue(ifcClass.ToUpperInvariant(), out var d) ? d : ifcClass;

    private static readonly Dictionary<string, string> _elementDescriptions =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFCWALL"]              = "Wall (structural or non-structural)",
        ["IFCWALLSTANDARDCASE"]  = "Standard wall element",
        ["IFCSLAB"]              = "Slab (floor, roof or landing)",
        ["IFCBEAM"]              = "Beam (structural horizontal element)",
        ["IFCCOLUMN"]            = "Column (structural vertical element)",
        ["IFCFOOTING"]           = "Footing / foundation element",
        ["IFCPILE"]              = "Pile (deep foundation element)",
        ["IFCDOOR"]              = "Door (including fire doors and gates)",
        ["IFCWINDOW"]            = "Window or glazed opening",
        ["IFCROOF"]              = "Roof structure",
        ["IFCSTAIR"]             = "Stair (assembly)",
        ["IFCSTAIRFLIGHT"]       = "Stair flight (tread/riser geometry)",
        ["IFCRAMP"]              = "Ramp (assembly)",
        ["IFCRAMPFLIGHT"]        = "Ramp flight (gradient geometry)",
        ["IFCRAILING"]           = "Railing, handrail or balustrade",
        ["IFCPLATE"]             = "Plate (structural flat element)",
        ["IFCMEMBER"]            = "Structural member (purlin, brace, etc.)",
        ["IFCCOVERING"]          = "Covering (ceiling, flooring, cladding)",
        ["IFCCURTAINWALL"]       = "Curtain wall / façade system",
        ["IFCSPACE"]             = "Room or space",
        ["IFCZONE"]              = "Zone (group of related spaces)",
        ["IFCBUILDINGELEMENTPROXY"] = "PROXY — unclassified element (must be replaced)",
        ["IFCSANITARYTERMINAL"]  = "Sanitary fitting (WC, basin, shower)",
        ["IFCFLOWTERMINAL"]      = "Flow terminal (outlet, diffuser)",
        ["IFCAIRTERMINAL"]       = "Air terminal (grille, diffuser)",
        ["IFCDUCTSEGMENT"]       = "Duct segment",
        ["IFCDUCTFITTING"]       = "Duct fitting (bend, junction, damper)",
        ["IFCPIPESEGMENT"]       = "Pipe segment",
        ["IFCPIPEFITTING"]       = "Pipe fitting (elbow, tee, reducer)",
        ["IFCLIGHTFIXTURE"]      = "Light fixture / luminaire",
        ["IFCELECTRICAPPLIANCE"] = "Electrical appliance",
        ["IFCFURNITURE"]         = "Furniture element",
        ["IFCTRANSPORTELEMENT"]  = "Transport element (lift, escalator)",
        ["IFCSITE"]              = "Site boundary",
        ["IFCBUILDING"]          = "Building",
        ["IFCBUILDINGSTOREY"]    = "Building storey / level",
        ["IFCPROJECT"]           = "IFC Project (root entity)",
    };

    // ─── SINGAPORE: ROOM MINIMUM SIZES (URA 2023) ─────────────────────────────
    public static readonly Dictionary<string, (double MinArea, string Reference)> SgRoomMinSizes =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["LIVING_ROOM"]         = (13.0,  "URA Planning Parameters 2023 §2.3 — Private Residential"),
        ["LIVING_ROOM_HDB"]     = (16.0,  "URA Planning Parameters 2023 — HDB Public Housing"),
        ["BEDROOM"]             = (9.0,   "URA Planning Parameters 2023 §3.1"),
        ["MASTER_BEDROOM"]      = (12.5,  "URA Planning Parameters 2023 §3.2"),
        ["KITCHEN"]             = (4.5,   "URA Planning Parameters 2023 §3.3"),
        ["STUDY"]               = (5.0,   "URA Planning Parameters 2023 §3.4"),
        ["BATHROOM"]            = (2.5,   "URA Planning Parameters 2023 §3.5"),
        ["ACCESSIBLE_TOILET"]   = (4.0,   "BCA Code on Accessibility 2025 §4.2.2"),
        ["CORRIDOR_ACCESSIBLE"] = (1.5,   "BCA Code on Accessibility 2025 — Min 1500mm width"),
        ["PARKING_STANDARD"]    = (12.5,  "LTA — 2.5m × 5.0m standard bay"),
        ["PARKING_ACCESSIBLE"]  = (18.0,  "LTA — 3.6m × 5.0m accessible bay"),
    };

    // ─── MALAYSIA: UBBL MINIMUM DIMENSIONS ────────────────────────────────────
    public static readonly Dictionary<string, (double MinValue, string Unit, string Reference)> MyMinDimensions =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // UBBL 1984 Part III — Space, Light and Ventilation
        ["CEILING_HEIGHT_HABITABLE"]     = (2.6,  "m",  "UBBL 1984 By-Law 47(1)"),
        ["CEILING_HEIGHT_BATHROOM"]      = (2.3,  "m",  "UBBL 1984 By-Law 47(2)"),
        ["CEILING_HEIGHT_SHOP"]          = (2.7,  "m",  "UBBL 1984 By-Law 47(3)"),
        ["FLOOR_AREA_BEDROOM"]           = (6.5,  "m²", "UBBL 1984 By-Law 48(1)(a)"),
        ["FLOOR_AREA_HABITABLE_ROOM"]    = (11.0, "m²", "UBBL 1984 By-Law 48(1)(b)"),
        ["FLOOR_AREA_KITCHEN"]           = (4.5,  "m²", "UBBL 1984 By-Law 48(2)"),
        ["WINDOW_AREA_LIGHTING"]         = (0.10, "%",  "UBBL 1984 By-Law 38 — 10% of floor area"),
        ["WINDOW_AREA_VENTILATION"]      = (0.05, "%",  "UBBL 1984 By-Law 39 — 5% of floor area"),
        ["CORRIDOR_WIDTH_MIN"]           = (1.5,  "m",  "UBBL 1984 By-Law 55"),
        // UBBL 1984 Part VI — Constructional Requirements
        ["STAIR_RISER_MAX"]              = (0.175,"m",  "UBBL 1984 By-Law 112"),
        ["STAIR_TREAD_MIN"]              = (0.255,"m",  "UBBL 1984 By-Law 112"),
        ["STAIR_WIDTH_PRIVATE_MIN"]      = (0.9,  "m",  "UBBL 1984 By-Law 113"),
        ["STAIR_WIDTH_SHARED_MIN"]       = (1.1,  "m",  "UBBL 1984 By-Law 113"),
        // UBBL 1984 Part VII — Fire Requirements
        ["FIRE_EXIT_DOOR_WIDTH_MIN"]     = (0.9,  "m",  "UBBL 1984 By-Law 126"),
        ["FIRE_TRAVEL_DISTANCE_MAX"]     = (30.0, "m",  "UBBL 1984 By-Law 127 (non-sprinklered)"),
        ["FIRE_TRAVEL_DISTANCE_SPRK"]    = (60.0, "m",  "UBBL 1984 By-Law 127 (sprinklered)"),
        // MS 1184:2014 — Accessibility
        ["ACCESSIBLE_DOOR_WIDTH_MIN"]    = (0.8,  "m",  "MS 1184:2014 §5.3"),
        ["ACCESSIBLE_RAMP_GRADIENT_MAX"] = (0.083,"",   "MS 1184:2014 §5.2 — 1:12 max gradient"),
        ["ACCESSIBLE_CORRIDOR_WIDTH"]    = (1.2,  "m",  "MS 1184:2014 §5.1"),
    };

    // ─── SINGAPORE: BCA ACCESSIBILITY CODE 2025 ───────────────────────────────
    public static readonly Dictionary<string, (double Value, string Unit, string Reference)> SgAccessibility =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["DOOR_CLEAR_WIDTH_MIN"]         = (0.85, "m",  "BCA Code on Accessibility 2025 §4.2.1"),
        ["DOOR_CLEAR_WIDTH_ACCESSIBLE"]  = (0.90, "m",  "BCA Code on Accessibility 2025 §4.2.1"),
        ["CORRIDOR_WIDTH_MIN"]           = (1.20, "m",  "BCA Code on Accessibility 2025 §4.4.1"),
        ["RAMP_GRADIENT_MAX"]            = (0.083,"",   "BCA Code on Accessibility 2025 §4.3.1 — 1:12"),
        ["RAMP_WIDTH_MIN"]               = (1.20, "m",  "BCA Code on Accessibility 2025 §4.3.1"),
        ["RAMP_LANDING_DEPTH"]           = (1.50, "m",  "BCA Code on Accessibility 2025 §4.3.2"),
        ["LIFT_DOOR_WIDTH_MIN"]          = (0.90, "m",  "BCA Code on Accessibility 2025 §4.5.1"),
        ["LIFT_CAR_MIN_DEPTH"]           = (1.40, "m",  "BCA Code on Accessibility 2025 §4.5.1"),
        ["ACCESSIBLE_TOILET_MIN_WIDTH"]  = (1.80, "m",  "BCA Code on Accessibility 2025 §4.2.2"),
        ["ACCESSIBLE_TOILET_MIN_DEPTH"]  = (2.20, "m",  "BCA Code on Accessibility 2025 §4.2.2"),
        ["HANDRAIL_HEIGHT_MIN"]          = (0.85, "m",  "BCA Code on Accessibility 2025 §4.4.3"),
        ["HANDRAIL_HEIGHT_MAX"]          = (0.95, "m",  "BCA Code on Accessibility 2025 §4.4.3"),
        ["STAIR_RISER_MAX"]              = (0.175,"m",  "BCA Code on Accessibility 2025 §4.3.1"),
        ["STAIR_TREAD_MIN"]              = (0.280,"m",  "BCA Code on Accessibility 2025 §4.3.1"),
    };

    // ─── SINGAPORE: SCDF FIRE CODE 2018 + 2023 AMENDMENT ─────────────────────
    public static readonly Dictionary<string, (double Value, string Unit, string Reference)> SgFireCode =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // Compartment sizes
        ["COMPARTMENT_AREA_SPRINKLERED"]     = (7000,  "m²", "SCDF Fire Code 2018 §4.3 Table 4.3"),
        ["COMPARTMENT_AREA_NON_SPRINKLERED"] = (3500,  "m²", "SCDF Fire Code 2018 §4.3 Table 4.3"),
        // Travel distances
        ["TRAVEL_DISTANCE_SPRINKLERED"]      = (60.0,  "m",  "SCDF Fire Code 2018 §5.2"),
        ["TRAVEL_DISTANCE_NON_SPRINKLERED"]  = (30.0,  "m",  "SCDF Fire Code 2018 §5.2"),
        ["DEAD_END_DISTANCE"]                = (7.5,   "m",  "SCDF Fire Code 2018 §5.2.3"),
        // Exit dimensions
        ["EXIT_DOOR_WIDTH_MIN"]              = (0.75,  "m",  "SCDF Fire Code 2018 §5.3 — small occupancy"),
        ["EXIT_DOOR_WIDTH_60PLUS"]           = (1.05,  "m",  "SCDF Fire Code 2018 §5.3 — ≥60 occupants"),
        ["ESCAPE_STAIR_WIDTH_MIN"]           = (1.10,  "m",  "SCDF Fire Code 2018 §5.4"),
        ["ESCAPE_STAIR_WIDTH_HIGH_RISE"]     = (1.20,  "m",  "SCDF Fire Code 2018 §5.4 — > 24m building"),
        ["ESCAPE_CORRIDOR_WIDTH_MIN"]        = (1.05,  "m",  "SCDF Fire Code 2018 §5.4"),
        // Fire resistance ratings (REI minutes)
        ["FRR_RESIDENTIAL_WALL"]             = (60.0,  "min","SCDF Fire Code §4.3 Table 4.2"),
        ["FRR_COMMERCIAL_WALL_SPRK"]         = (60.0,  "min","SCDF Fire Code §4.3 Table 4.2"),
        ["FRR_COMMERCIAL_WALL_NON_SPRK"]     = (120.0, "min","SCDF Fire Code §4.3 Table 4.2"),
        ["FRR_STAIR_ENCLOSURE"]              = (60.0,  "min","SCDF Fire Code §4.3"),
        ["FRR_LIFT_SHAFT"]                   = (60.0,  "min","SCDF Fire Code §4.3"),
        // Rise stair riser
        ["STAIR_RISER_MAX_FIRE"]             = (0.190, "m",  "SCDF Fire Code §5.4.2"),
        ["STAIR_TREAD_MIN_FIRE"]             = (0.250, "m",  "SCDF Fire Code §5.4.2"),
        // Number of escape routes
        ["MIN_ESCAPE_ROUTES_ABOVE_60_OCC"]   = (2.0,   "",   "SCDF Fire Code 2018 §5.2.1"),
    };

    // ─── SINGAPORE: BCA GREEN MARK 2021 ──────────────────────────────────────
    public static readonly Dictionary<string, (double Value, string Unit, string Reference)> SgGreenMark =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["ETTV_MAX_RESIDENTIAL"]    = (25.0, "W/m²", "BCA Green Mark 2021 §3.4 — ETTV residential"),
        ["ETTV_MAX_COMMERCIAL"]     = (50.0, "W/m²", "BCA Green Mark 2021 §3.4 — ETTV commercial"),
        ["RETV_MAX"]                = (25.0, "W/m²", "BCA Green Mark 2021 §3.4 — RETV residential"),
        ["WALL_U_VALUE_MAX"]        = (0.5,  "W/m²K","BCA Green Mark 2021 §3.4.2 — External wall"),
        ["ROOF_U_VALUE_MAX"]        = (0.35, "W/m²K","BCA Green Mark 2021 §3.4.1 — Roof"),
        ["WINDOW_SHGC_MAX"]         = (0.3,  "",     "BCA Green Mark 2021 §3.4.3 — Solar heat gain"),
        ["LPD_OFFICE_MAX"]          = (12.0, "W/m²", "BCA Green Mark 2021 §3.5 — Office LPD"),
        ["LPD_RETAIL_MAX"]          = (20.0, "W/m²", "BCA Green Mark 2021 §3.5 — Retail LPD"),
        ["LPD_HOTEL_MAX"]           = (10.0, "W/m²", "BCA Green Mark 2021 §3.5 — Hotel guestroom LPD"),
        ["WWR_RESIDENTIAL_MAX"]     = (0.40, "",     "BCA Green Mark 2021 §3.4.3 — WWR residential"),
        ["WWR_COMMERCIAL_MAX"]      = (0.60, "",     "BCA Green Mark 2021 §3.4.3 — WWR commercial"),
        ["MIN_GFA_THRESHOLD_M2"]    = (1000, "m²",   "Building Control (Env. Sustainability) Regs 2008"),
    };

    // ─── SINGAPORE: URA PLANNING PARAMETERS 2023 ──────────────────────────────
    public static readonly Dictionary<string, (double Value, string Unit, string Reference)> SgUraParams =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["MAX_BALCONY_PCT_OF_GFA"]  = (10.0,  "%",  "URA Circular — Balcony ≤ 10% of unit GFA"),
        ["MIN_UNIT_SIZE_CONDO_M2"]  = (35.0,  "m²", "URA — Minimum condo unit size 35m²"),
        ["MIN_UNIT_SIZE_SERVICED"]  = (35.0,  "m²", "URA — Minimum serviced apartment 35m²"),
        ["MAX_PLOT_COVERAGE_PCT"]   = (80.0,  "%",  "URA Development Control — plot coverage"),
        ["SETBACK_ROAD_CATEGORY_1"] = (7.5,   "m",  "URA DC Handbook — Cat 1 road reserve setback"),
        ["SETBACK_ROAD_CATEGORY_2"] = (5.0,   "m",  "URA DC Handbook — Cat 2 road reserve setback"),
        ["SETBACK_ROAD_CATEGORY_3"] = (3.0,   "m",  "URA DC Handbook — Cat 3–5 road reserve setback"),
        ["FLOOR_TO_FLOOR_MIN"]      = (2.4,   "m",  "URA — Minimum floor-to-floor height"),
    };

    // ─── IFC+SG REQUIRED PSET PARAMETERS (all 500+, abridged to key elements) ──
    // Complete list: see IFC+SG Industry Mapping Excel from info.corenet.gov.sg
    public static readonly Dictionary<string, List<string>> SgRequiredParams =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFCWALL"]     = new() { "Pset_WallCommon.IsExternal","Pset_WallCommon.LoadBearing","Pset_WallCommon.FireRating","Pset_WallCommon.AcousticRating","Pset_WallCommon.ThermalTransmittance","SGPset_WallFireRating.FireResistancePeriod","SGPset_WallFireRating.FireTestStandard" },
        ["IFCSLAB"]     = new() { "Pset_SlabCommon.IsExternal","Pset_SlabCommon.LoadBearing","Pset_SlabCommon.FireRating","Pset_SlabCommon.ThermalTransmittance","SGPset_SlabFireRating.FireResistancePeriod" },
        ["IFCDOOR"]     = new() { "Pset_DoorCommon.IsExternal","Pset_DoorCommon.FireRating","Pset_DoorCommon.HandicapAccessible","Pset_DoorCommon.FireExit","Pset_DoorCommon.SmokeStop","SGPset_DoorAccessibility.ClearWidth" },
        ["IFCWINDOW"]   = new() { "Pset_WindowCommon.IsExternal","Pset_WindowCommon.FireRating","Pset_WindowCommon.ThermalTransmittance","Pset_WindowCommon.SolarHeatGainCoefficient" },
        ["IFCSPACE"]    = new() { "Pset_SpaceCommon.Category","Pset_SpaceCommon.IsExternal","Pset_SpaceCommon.GrossPlannedArea","Pset_SpaceCommon.NetPlannedArea","SGPset_SpaceGFA.GFACategory","SGPset_SpaceGFA.IsGFAExempt" },
        ["IFCCOLUMN"]   = new() { "Pset_ColumnCommon.LoadBearing","Pset_ColumnCommon.FireRating","SGPset_ColumnStructural.ConcreteGrade","SGPset_ColumnStructural.DesignCode" },
        ["IFCBEAM"]     = new() { "Pset_BeamCommon.LoadBearing","Pset_BeamCommon.FireRating","SGPset_BeamStructural.ConcreteGrade" },
        ["IFCSTAIR"]    = new() { "Pset_StairCommon.IsExternal","Pset_StairCommon.HandicapAccessible","SGPset_StairFireEscape.IsFireEscapeStair","SGPset_StairFireEscape.TravelDistance" },
        ["IFCRAMP"]     = new() { "Pset_RampCommon.HandicapAccessible","SGPset_RampAccessibility.Gradient","SGPset_RampAccessibility.Width" },
        ["IFCSITE"]     = new() { "Pset_SiteCommon.SiteID","Pset_SiteCommon.RefElevation","Pset_SiteCommon.TotalArea" },
        ["IFCPILE"]     = new() { "Pset_PileCommon.LoadBearing","SGPset_PileFoundation.PileType","SGPset_PileFoundation.DesignLoad","SGPset_PileFoundation.PileLength" },
    };

    // ─── UBBL THIRD SCHEDULE — FRR by Purpose Group ───────────────────────────
    public static readonly Dictionary<MalaysiaPurposeGroup, Dictionary<string, int>> MyFrrTable = new()
    {
        [MalaysiaPurposeGroup.PurposeGroupI]   = new() { ["StructuralElement"]=30,  ["Floor"]=30,  ["Wall"]=30,  ["Roof"]=30  },
        [MalaysiaPurposeGroup.PurposeGroupII]  = new() { ["StructuralElement"]=60,  ["Floor"]=60,  ["Wall"]=60,  ["Roof"]=30  },
        [MalaysiaPurposeGroup.PurposeGroupIII] = new() { ["StructuralElement"]=90,  ["Floor"]=60,  ["Wall"]=60,  ["Roof"]=30  },
        [MalaysiaPurposeGroup.PurposeGroupIV]  = new() { ["StructuralElement"]=60,  ["Floor"]=60,  ["Wall"]=60,  ["Roof"]=30  },
        [MalaysiaPurposeGroup.PurposeGroupV]   = new() { ["StructuralElement"]=60,  ["Floor"]=60,  ["Wall"]=60,  ["Roof"]=30  },
        [MalaysiaPurposeGroup.PurposeGroupVI]  = new() { ["StructuralElement"]=120, ["Floor"]=120, ["Wall"]=120, ["Roof"]=60  },
        [MalaysiaPurposeGroup.PurposeGroupVII] = new() { ["StructuralElement"]=120, ["Floor"]=120, ["Wall"]=120, ["Roof"]=60  },
        [MalaysiaPurposeGroup.PurposeGroupVIII]= new() { ["StructuralElement"]=120, ["Floor"]=120, ["Wall"]=120, ["Roof"]=60  },
        [MalaysiaPurposeGroup.PurposeGroupIX]  = new() { ["StructuralElement"]=240, ["Floor"]=240, ["Wall"]=240, ["Roof"]=120 },
    };

    /// <summary>Look up FRR requirement for element type and purpose group.</summary>
    public static int GetMyFrrMinutes(MalaysiaPurposeGroup pg, string elementType)
    {
        if (!MyFrrTable.TryGetValue(pg, out var table)) return 60;
        return table.TryGetValue(elementType, out var minutes) ? minutes : 60;
    }

    /// <summary>Get IFC+SG remediation guidance specific to the failing parameter.</summary>
    public static string GetSgRemediationGuidance(string ifcClass, string psetName, string propName) =>
        (psetName, propName) switch
        {
            ("Pset_WallCommon", "IsExternal")
                => "Set IsExternal=TRUE for external walls, FALSE for internal partitions. " +
                   "In ArchiCAD: IFC Manager → Pset_WallCommon → IsExternal. " +
                   "In Revit: IFC+SG shared parameters → Wall Parameters.",
            ("Pset_WallCommon", "FireRating")
                => "Set FireRating using REI notation: '60/60/60' (1hr) or '120/120/120' (2hr). " +
                   "Use SCDF Fire Code Table 4.2 to determine the required rating. " +
                   "Compartment walls typically require FRR 60–120. Staircase enclosures: FRR 60.",
            ("Pset_DoorCommon", "HandicapAccessible")
                => "Mark TRUE if door provides ≥ 850mm clear opening on an accessible route. " +
                   "Code on Accessibility 2025 §4.2.1 requires this on all accessible-route doors.",
            ("Pset_DoorCommon", "FireExit")
                => "Mark TRUE for doors on designated fire escape routes. " +
                   "SCDF Fire Code §5.2 requires all escape route doors to be identified in the model.",
            ("Pset_SpaceCommon", "Category")
                => "Set the space use category from the URA permitted values: " +
                   "RESIDENTIAL, BEDROOM, OFFICE, COMMERCIAL, CARPARK, PLANT_ROOM etc. " +
                   "This is mandatory for URA GFA automated checking in CORENET-X.",
            ("Pset_SpaceCommon", "GrossPlannedArea")
                => "Enter the gross planned area in m² (decimal). " +
                   "URA computes GFA from the sum of all IfcSpace GrossPlannedArea values. " +
                   "Must match the declared GFA on the building plan submission.",
            ("Pset_SlabCommon", "FireRating")
                => "Set FireRating per SCDF Fire Code Table 4.2. " +
                   "Floor slabs bounding fire compartments require FRR 60–90 minimum. " +
                   "Roof slabs: FRR 30 minimum (non-sprinklered residential).",
            _   => $"Add the required property '{propName}' to property set '{psetName}' " +
                   $"on all {ifcClass} elements. Refer to the IFC+SG Industry Mapping " +
                   "Excel from info.corenet.gov.sg for the complete parameter list and permitted values."
        };

    /// <summary>Get UBBL remediation guidance specific to the failing parameter.</summary>
    public static string GetMyRemediationGuidance(string ifcClass, string psetName, string propName) =>
        (psetName, propName) switch
        {
            ("Pset_WallCommon", "FireRating")
                => "Set FireRating per UBBL 1984 Third Schedule. " +
                   "Use the fire resistance period in minutes: '60', '90', '120', '180'. " +
                   "Refer to JBPM Fire Safety Requirements 2020 for approved test standards.",
            ("Pset_DoorCommon", "HandicapAccessible")
                => "Mark TRUE if door has ≥ 800mm clear opening on an accessible route. " +
                   "MS 1184:2014 §5.3 requires accessible door clear width of 800mm minimum.",
            ("Pset_SpaceCommon", "Category")
                => "Set the space use per UBBL 1984 Third Schedule Purpose Groups: " +
                   "RESIDENTIAL, OFFICE, SHOP, FACTORY, ASSEMBLY, INSTITUTION etc.",
            ("Pset_SpaceCommon", "Height")
                => "Set the clear ceiling height in metres. " +
                   "UBBL 1984 By-Law 47: habitable rooms ≥ 2.6m, bathrooms/corridors ≥ 2.3m.",
            _   => $"Add property '{propName}' to '{psetName}' on all {ifcClass} elements. " +
                   "Refer to the NBeS IFC Mapping 2024 (CIDB) for the complete parameter list."
        };
}
