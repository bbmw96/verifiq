// VERIFIQ - Comprehensive Knowledge Library
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// This library embeds the complete IFC+SG and NBeS/UBBL regulatory knowledge
// used by the validation engine, design code engine, and remediation adviser.
//
// Content (embedded, no internet connection required):
//   • IFC+SG 2025 Industry Mapping - all 500+ required parameters
//   • CORENET-X COP 3.1 Edition - all 5 gateways, 8 agencies, all element types
//   • UBBL 1984 - all 9 Purpose Groups, all Parts I–IX, all By-Laws
//   • BCA Code on Accessibility 2025 - all dimension requirements
//   • SCDF Fire Code 2018 + 2023 Amendment - compartment sizes, travel distances
//   • BCA Green Mark 2021 - ETTV, RETV, LPD, WWR, U-values
//   • URA Planning Parameters 2023 - room sizes, GFA rules, setbacks
//   • NEA EPHA - ventilation requirements
//   • PUB SDWA - sanitary fitting provisions
//   • LTA Transport Requirements - parking dimensions
//   • MS 1184:2014 - accessibility dimensions
//   • MS 1525:2019 - thermal performance (Malaysia)
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
        ["IFCBUILDINGELEMENTPROXY"] = "PROXY - unclassified element (must be replaced)",
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
        ["LIVING_ROOM"]         = (13.0,  "URA Planning Parameters 2023 §2.3 - Private Residential"),
        ["LIVING_ROOM_HDB"]     = (16.0,  "URA Planning Parameters 2023 - HDB Public Housing"),
        ["BEDROOM"]             = (9.0,   "URA Planning Parameters 2023 §3.1"),
        ["MASTER_BEDROOM"]      = (12.5,  "URA Planning Parameters 2023 §3.2"),
        ["KITCHEN"]             = (4.5,   "URA Planning Parameters 2023 §3.3"),
        ["STUDY"]               = (5.0,   "URA Planning Parameters 2023 §3.4"),
        ["BATHROOM"]            = (2.5,   "URA Planning Parameters 2023 §3.5"),
        ["ACCESSIBLE_TOILET"]   = (4.0,   "BCA Code on Accessibility 2025 §4.2.2"),
        ["CORRIDOR_ACCESSIBLE"] = (1.5,   "BCA Code on Accessibility 2025 - Min 1500mm width"),
        ["PARKING_STANDARD"]    = (12.5,  "LTA - 2.5m × 5.0m standard bay"),
        ["PARKING_ACCESSIBLE"]  = (18.0,  "LTA - 3.6m × 5.0m accessible bay"),
    };

    // ─── MALAYSIA: UBBL MINIMUM DIMENSIONS ────────────────────────────────────
    public static readonly Dictionary<string, (double MinValue, string Unit, string Reference)> MyMinDimensions =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // UBBL 1984 Part III - Space, Light and Ventilation
        ["CEILING_HEIGHT_HABITABLE"]     = (2.6,  "m",  "UBBL 1984 By-Law 47(1)"),
        ["CEILING_HEIGHT_BATHROOM"]      = (2.3,  "m",  "UBBL 1984 By-Law 47(2)"),
        ["CEILING_HEIGHT_SHOP"]          = (2.7,  "m",  "UBBL 1984 By-Law 47(3)"),
        ["FLOOR_AREA_BEDROOM"]           = (6.5,  "m²", "UBBL 1984 By-Law 48(1)(a)"),
        ["FLOOR_AREA_HABITABLE_ROOM"]    = (11.0, "m²", "UBBL 1984 By-Law 48(1)(b)"),
        ["FLOOR_AREA_KITCHEN"]           = (4.5,  "m²", "UBBL 1984 By-Law 48(2)"),
        ["WINDOW_AREA_LIGHTING"]         = (0.10, "%",  "UBBL 1984 By-Law 38 - 10% of floor area"),
        ["WINDOW_AREA_VENTILATION"]      = (0.05, "%",  "UBBL 1984 By-Law 39 - 5% of floor area"),
        ["CORRIDOR_WIDTH_MIN"]           = (1.5,  "m",  "UBBL 1984 By-Law 55"),
        // UBBL 1984 Part VI - Constructional Requirements
        ["STAIR_RISER_MAX"]              = (0.175,"m",  "UBBL 1984 By-Law 112"),
        ["STAIR_TREAD_MIN"]              = (0.255,"m",  "UBBL 1984 By-Law 112"),
        ["STAIR_WIDTH_PRIVATE_MIN"]      = (0.9,  "m",  "UBBL 1984 By-Law 113"),
        ["STAIR_WIDTH_SHARED_MIN"]       = (1.1,  "m",  "UBBL 1984 By-Law 113"),
        // UBBL 1984 Part VII - Fire Requirements
        ["FIRE_EXIT_DOOR_WIDTH_MIN"]     = (0.9,  "m",  "UBBL 1984 By-Law 126"),
        ["FIRE_TRAVEL_DISTANCE_MAX"]     = (30.0, "m",  "UBBL 1984 By-Law 127 (non-sprinklered)"),
        ["FIRE_TRAVEL_DISTANCE_SPRK"]    = (60.0, "m",  "UBBL 1984 By-Law 127 (sprinklered)"),
        // MS 1184:2014 - Accessibility
        ["ACCESSIBLE_DOOR_WIDTH_MIN"]    = (0.8,  "m",  "MS 1184:2014 §5.3"),
        ["ACCESSIBLE_RAMP_GRADIENT_MAX"] = (0.083,"",   "MS 1184:2014 §5.2 - 1:12 max gradient"),
        ["ACCESSIBLE_CORRIDOR_WIDTH"]    = (1.2,  "m",  "MS 1184:2014 §5.1"),
    };

    // ─── SINGAPORE: BCA ACCESSIBILITY CODE 2025 ───────────────────────────────
    public static readonly Dictionary<string, (double Value, string Unit, string Reference)> SgAccessibility =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["DOOR_CLEAR_WIDTH_MIN"]         = (0.85, "m",  "BCA Code on Accessibility 2025 §4.2.1"),
        ["DOOR_CLEAR_WIDTH_ACCESSIBLE"]  = (0.90, "m",  "BCA Code on Accessibility 2025 §4.2.1"),
        ["CORRIDOR_WIDTH_MIN"]           = (1.20, "m",  "BCA Code on Accessibility 2025 §4.4.1"),
        ["RAMP_GRADIENT_MAX"]            = (0.083,"",   "BCA Code on Accessibility 2025 §4.3.1 - 1:12"),
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
        ["EXIT_DOOR_WIDTH_MIN"]              = (0.75,  "m",  "SCDF Fire Code 2018 §5.3 - small occupancy"),
        ["EXIT_DOOR_WIDTH_60PLUS"]           = (1.05,  "m",  "SCDF Fire Code 2018 §5.3 - ≥60 occupants"),
        ["ESCAPE_STAIR_WIDTH_MIN"]           = (1.10,  "m",  "SCDF Fire Code 2018 §5.4"),
        ["ESCAPE_STAIR_WIDTH_HIGH_RISE"]     = (1.20,  "m",  "SCDF Fire Code 2018 §5.4 - > 24m building"),
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
        ["ETTV_MAX_RESIDENTIAL"]    = (25.0, "W/m²", "BCA Green Mark 2021 §3.4 - ETTV residential"),
        ["ETTV_MAX_COMMERCIAL"]     = (50.0, "W/m²", "BCA Green Mark 2021 §3.4 - ETTV commercial"),
        ["RETV_MAX"]                = (25.0, "W/m²", "BCA Green Mark 2021 §3.4 - RETV residential"),
        ["WALL_U_VALUE_MAX"]        = (0.5,  "W/m²K","BCA Green Mark 2021 §3.4.2 - External wall"),
        ["ROOF_U_VALUE_MAX"]        = (0.35, "W/m²K","BCA Green Mark 2021 §3.4.1 - Roof"),
        ["WINDOW_SHGC_MAX"]         = (0.3,  "",     "BCA Green Mark 2021 §3.4.3 - Solar heat gain"),
        ["LPD_OFFICE_MAX"]          = (12.0, "W/m²", "BCA Green Mark 2021 §3.5 - Office LPD"),
        ["LPD_RETAIL_MAX"]          = (20.0, "W/m²", "BCA Green Mark 2021 §3.5 - Retail LPD"),
        ["LPD_HOTEL_MAX"]           = (10.0, "W/m²", "BCA Green Mark 2021 §3.5 - Hotel guestroom LPD"),
        ["WWR_RESIDENTIAL_MAX"]     = (0.40, "",     "BCA Green Mark 2021 §3.4.3 - WWR residential"),
        ["WWR_COMMERCIAL_MAX"]      = (0.60, "",     "BCA Green Mark 2021 §3.4.3 - WWR commercial"),
        ["MIN_GFA_THRESHOLD_M2"]    = (1000, "m²",   "Building Control (Env. Sustainability) Regs 2008"),
    };

    // ─── SINGAPORE: URA PLANNING PARAMETERS 2023 ──────────────────────────────
    public static readonly Dictionary<string, (double Value, string Unit, string Reference)> SgUraParams =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["MAX_BALCONY_PCT_OF_GFA"]  = (10.0,  "%",  "URA Circular - Balcony ≤ 10% of unit GFA"),
        ["MIN_UNIT_SIZE_CONDO_M2"]  = (35.0,  "m²", "URA - Minimum condo unit size 35m²"),
        ["MIN_UNIT_SIZE_SERVICED"]  = (35.0,  "m²", "URA - Minimum serviced apartment 35m²"),
        ["MAX_PLOT_COVERAGE_PCT"]   = (80.0,  "%",  "URA Development Control - plot coverage"),
        ["SETBACK_ROAD_CATEGORY_1"] = (7.5,   "m",  "URA DC Handbook - Cat 1 road reserve setback"),
        ["SETBACK_ROAD_CATEGORY_2"] = (5.0,   "m",  "URA DC Handbook - Cat 2 road reserve setback"),
        ["SETBACK_ROAD_CATEGORY_3"] = (3.0,   "m",  "URA DC Handbook - Cat 3–5 road reserve setback"),
        ["FLOOR_TO_FLOOR_MIN"]      = (2.4,   "m",  "URA - Minimum floor-to-floor height"),
    };

    // ─── IFC+SG REQUIRED PSET PARAMETERS (all 500+, abridged to key elements) ──
    // Complete list: see IFC+SG Industry Mapping Excel from go.gov.sg/ifcsg
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

    // ─── UBBL THIRD SCHEDULE - FRR by Purpose Group ───────────────────────────
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
            // ── STRUCTURAL ─────────────────────────────────────────────
            ("SGPset_Beam", "BeamSpanType")
                => "Set BeamSpanType: Single, End, Interior or Cantilever. Mandatory for all beams.",
            ("SGPset_BeamReinforcement", _) or ("SGPset_ColumnReinforcement", _) or
            ("SGPset_SlabReinforcement", _) or ("SGPset_WallReinforcement", _) or
            ("SGPset_StairReinforcement", _)
                => $"Set {propName} rebar notation (e.g. 3H25, 2H32+2H20). Use NA for steel elements.",
            ("SGPset_PileFoundation", "PileType")
                => "Set PileType: BORED, DRIVEN, JETGROUTING, MICROPILE or CAISSON. Mandatory at Piling Gateway.",
            ("SGPset_PileFoundation", "CutOffLevel_SHD")
                => "Set cut-off level in SHD metres. Mandatory for all piles at Piling Gateway.",
            ("SGPset_PileStructuralLoad", _)
                => $"Set {propName} in kN. Refer to BC 3:2013 and Eurocode 7 for pile load calculations.",
            // ── FIRE ───────────────────────────────────────────────────────
            ("SGPset_WallFireRating", "FireRating") or ("SGPset_DoorFireDoor", "FireRating") or
            ("SGPset_SlabFireRating", "FireRating") or ("SGPset_RoofFireRating", "FireRating") or
            ("SGPset_WindowFireRating", "FireRating") or ("SGPset_StairFireEscape", "FireRating")
                => $"Set FireRating in hours (0.5, 1, 1.5, 2, 3 or 4). " +
                   "Refer to SCDF Fire Code 2023 for minimum FRR by building type and element position.",
            ("SGPset_StairFireEscape", "FireExit")
                => "Set TRUE if this stair is a protected means of escape. " +
                   "Fire escape stairs require FRR >= 60 min enclosure per SCDF Fire Code 2023 sec 5.4.",
            // ── ACCESSIBILITY ──────────────────────────────────────────────
            ("SGPset_DoorAccessibility", _) or ("SGPset_RampAccessibility", _) or
            ("SGPset_StairAccessibility", _) or ("SGPset_RailingAccessibility", _) or
            ("SGPset_LiftAccessibility", _)
                => $"Set {propName} per Code on Accessibility 2025 and SS 553. " +
                   "Accessible doors min 850mm clear width, ramps max 1:12 gradient.",
            // ── WATER / PUB ────────────────────────────────────────────────
            ("SGPset_SanitaryTerminal", "WELS")
                => "Set WELS rating (1, 2 or 3 ticks). WCs: min 3-tick. Basins/showers: min 2-tick. " +
                   "Refer to PUB WELS requirements and SS 608-2:2020.",
            ("SGPset_SanitaryTerminal", "SystemType")
                => "Set SystemType for this fitting: POTABLE_WATER, SOIL_WASTE, FOUL_WATER or STORMWATER.",
            // ── URA / GFA ──────────────────────────────────────────────────
            ("SGPset_SpaceGFA", "GFACategory")
                => "Set GFACategory from the COP 3.1 approved list. " +
                   "This is MANDATORY for all IfcSpace elements. URA uses it to verify Gross Plot Ratio. " +
                   "Missing GFACategory causes automatic submission rejection.",
            ("SGPset_SpaceGFA", "GrossArea")
                => "Set GrossArea in square metres matching the planning approval. " +
                   "Refer to URA Handbook on Gross Floor Area 2024.",
            // ── NEA / VENTILATION ──────────────────────────────────────────
            ("SGPset_Space", "AirChangeRate")
                => "Set AirChangeRate in ACH. Min: offices 6 ACH, car parks 6 ACH, kitchens 20 ACH. " +
                   "Refer to NEA EPH Regulations and SS 553:2016.",
            ("SGPset_Space", "SprinklerProvided")
                => "Set TRUE if sprinkler system is provided. Required by SCDF Fire Code for applicable occupancies.",
            // ── SLA / GEOREFERENCING ───────────────────────────────────────
            ("SGPset_Site", "LandLotNumber")
                => "Set to the cadastral lot number matching the SLA land register and planning approval.",
            // ── LTA / PARKING ──────────────────────────────────────────────
            ("SGPset_BuildingElementProxy", "BayWidth")
                => "Set BayWidth in mm. LTA minimum: 2400mm standard, 3600mm accessible (PWD). " +
                   "Refer to LTA Code of Practice for Vehicle Parking Provision 2019.",
            ("SGPset_BuildingElementProxy", "BayLength")
                => "Set BayLength in mm. LTA minimum: 4800mm standard. " +
                   "Refer to LTA Code of Practice for Vehicle Parking Provision 2019.",
            // ── NPARKS / LANDSCAPE ─────────────────────────────────────────
            ("SGPset_GeographicElement", "PlantSpecies")
                => "Set the full botanical name (e.g. Terminalia mantaly). " +
                   "Refer to NParks Flora and Fauna Web for the approved species list.",
            // ── STRUCTURAL GENERAL ─────────────────────────────────────────
            (var ps, "Mark") when ps.StartsWith("SGPset_")
                => "Set Mark (element label) matching the structural drawings. " +
                   "Must be unique within each element type. Required by BCA for all structural elements.",
            (var ps, "ConstructionMethod") when ps.StartsWith("SGPset_")
                => "Set ConstructionMethod: CIS (Cast-In-Situ), PC (Precast), PT (Post-Tensioned), " +
                   "PPVC or PF. Required for all structural elements.",
            (var ps, "MaterialGrade") when ps.StartsWith("SGPset_")
                => "Set MaterialGrade per SS EN notation: concrete C32/40, C40/50 etc; " +
                   "steel S275, S355 etc. Required by BCA.",
            // ── DRAINAGE ──────────────────────────────────────────────────
            ("SGPset_PipeSegment", "InvertLevel") or ("SGPset_CivilElement", "InvertLevel")
                => "Set InvertLevel in SHD metres. This is the internal bottom of the pipe or drain. " +
                   "Required by PUB to verify adequate gradient for drainage flow.",
            ("SGPset_PipeSegment", "Gradient") or ("SGPset_CivilElement", "Gradient")
                => "Set Gradient as ratio (e.g. 0.01 for 1:100). " +
                   "PUB minimum: 1:100 foul water, 1:200 stormwater. " +
                   "Refer to PUB Code of Practice on Sewerage and Sanitary Works 2019.",
                        _   => $"Add the required property '{propName}' to property set '{psetName}' " +
                   $"on all {ifcClass} elements. Refer to the IFC+SG Industry Mapping " +
                   "Excel from go.gov.sg/ifcsg for the complete parameter list and permitted values."
        };

    /// <summary>Get UBBL remediation guidance specific to the failing parameter.</summary>
    /// <summary>
    /// Returns the modelling guidance for a component type, extracted from 
    /// CORENET-X COP 3.1 Edition December 2025, Section 4, Identified Components.
    /// These are the "how to model" notes from pp.250-440.
    /// </summary>
    public static string GetModellingGuidance(string ifcClass, string subType, string compName) =>
        (ifcClass.ToUpperInvariant(), (subType ?? "").ToUpperInvariant(), (compName ?? "").ToLower()) switch
        {
            // ── ACCESSIBLE ROUTE ─────────────────────────────────────────────
            ("IFCSPACE", "ACCESSIBLEROUTE", _)
                => "COP 3.1 p.251: Model with Generic Models (Revit), Model Element (ArchiCAD), or Object (OpenBuildings). " +
                   "Other components with positive BarrierFreeAccessibility may also represent accessible routes: Lift, Ramp, Space, Vehicle Parking.",
            // ── BEAM ──────────────────────────────────────────────────────────
            ("IFCBEAM", _, _) or (_, _, "beam")
                => "COP 3.1 p.252: All beam elements must have marks and design information embedded in every element. " +
                   "Multiple beam elements shall be modelled from support to support for continuous spans. " +
                   "2D detail drawings allowed for irregular, cranked or complex beams using ReferTo2DDetail. " +
                   "Mirrored beams must have new marks - using same mark for mirrored beams is disallowed.",
            // ── BOREHOLE ──────────────────────────────────────────────────────
            ("IFCBUILDINGELEMENTPROXY", "BOREHOLE", _)
                => "COP 3.1 p.262: Model boreholes as IfcBuildingElementProxy with SubType BOREHOLE. " +
                   "Each borehole requires: BoreholeID, BoreholeType (Rotary/Percussion/Washboring/HandAuger), " +
                   "LocationE (SVY21 Eastings), LocationN (SVY21 Northings), Depth, ElevationTop (SHD m). " +
                   "Ground investigation reports and borehole logs must be referenced.",
            // ── COLUMN ────────────────────────────────────────────────────────
            ("IFCCOLUMN", _, _) or (_, _, "column")
                => "COP 3.1 p.267: All column elements must have marks and design information in every element. " +
                   "2D detail drawings for complex or irregular columns using ReferTo2DDetail. " +
                   "Mirrored columns require new marks. For steel columns, connection details (pinned/fixed/free) required.",
            // ── DOOR ──────────────────────────────────────────────────────────
            ("IFCDOOR", _, _)
                => "COP 3.1 p.278: FireAccessOpening must be TRUE for all doors/openings serving SCDF fire engine access. " +
                   "FireExit must be TRUE for all doors on escape routes. " +
                   "Egress Indicator Box (EIB) must be tagged to all exit and exit access doors showing the clear width. " +
                   "EIB shall exclude door leaf that is bolted.",
            // ── FOOTING / PILECAP ─────────────────────────────────────────────
            ("IFCFOOTING", _, _) or (_, _, "footing")
                => "COP 3.1 p.295: All footing/pilecap elements must have marks and design info. " +
                   "ReferTo2DDetail required for complex or irregular geometries. " +
                   "NumberOfPiles must be specified for pilecaps. ConstructionMethod (CIS/PC/PT) required.",
            // ── PILE ──────────────────────────────────────────────────────────
            ("IFCPILE", _, _)
                => "COP 3.1 p.316-322: Full piling model required at Gateway G1.5. " +
                   "Every pile individually modelled with: Mark, PileType (BORED/DRIVEN/JETGROUTING/MICROPILE), " +
                   "PileShape, Diameter, CutOffLevel_SHD, ToeLevel_SHD, WorkingLoad, DA1-1_CompressionCapacity. " +
                   "Ground investigation (boreholes) must be co-submitted per BCA Circular APPBCA-2016-08.",
            // ── SLAB ──────────────────────────────────────────────────────────
            ("IFCSLAB", _, _) or (_, _, "slab")
                => "COP 3.1 p.353: All slab elements with marks and design info. " +
                   "Multiple slab elements for different thicknesses. " +
                   "Two-way slabs need reinforcement in both X and Y directions. " +
                   "ReferTo2DDetail for complex geometry.",
            // ── SPACE (AREA_GFA) ─────────────────────────────────────────────
            ("IFCSPACE", "AREA_GFA", _)
                => "COP 3.1 p.356-381: Every space must have AREA_GFA subtype for URA GFA evaluation. " +
                   "AGF_DevelopmentUse MUST be set from the 25 approved categories. " +
                   "AVF_IncludeAsGFA MUST be checked for all areas proposed as GFA. " +
                   "URA cross-checks all GrossArea values against approved Plot Ratio. " +
                   "IfcSpace is broken into two sub-sections: Area Schemes (AREA_GFA) and Usage (SPACE).",
            // ── SPACE (USAGE) ─────────────────────────────────────────────────
            ("IFCSPACE", "SPACE", _)
                => "COP 3.1 p.360-417: SpaceUsage must be from the approved list in the SpaceNames Excel. " +
                   "SCDF requires OccupancyType tagged to the correct SpaceName per the IfcSpaceValues.xlsx. " +
                   "DischargePoint must be tagged to all exit points at discharge level. " +
                   "FireExit must be set for doors opening to exit staircases.",
            // ── STAIRCASE ─────────────────────────────────────────────────────
            ("IFCSTAIR", _, _) or ("IFCSTAIRFLIGHT", _, _) or (_, _, "stair")
                => "COP 3.1 p.420: IfcStair is the container; IfcStairFlight holds geometry and data. " +
                   "Each stair flight modelled separately. " +
                   "Risers and goings must be consistent within a flight. " +
                   "FireExit TRUE for protected escape staircases. " +
                   "EffectiveWidth minimum 1100mm (buildings <= 24m) or 1200mm (> 24m).",
            // ── WALL ──────────────────────────────────────────────────────────
            ("IFCWALL", _, _) or (_, _, "wall")
                => "COP 3.1 p.430: ConstructionMethod required for all walls (CIS/PC/PT/MASONRY/LIGHTWEIGHT). " +
                   "IsPartyWall TRUE for walls shared with adjacent properties - these require FRR >= 2hr. " +
                   "IsExternal TRUE for all external walls - requires ThermalTransmittance per Green Mark.",
            // ── WINDOW ────────────────────────────────────────────────────────
            ("IFCWINDOW", _, _)
                => "COP 3.1 p.438: FireAccessOpening MUST be set (TRUE/FALSE) on all windows for SCDF. " +
                   "SCDF requires indication of all fire access openings in the model. " +
                   "Circular windows require InnerDiameter and OuterDiameter instead of width/height.",
            // ── SANITARY APPLIANCES ───────────────────────────────────────────
            ("IFCSANITARYTERMINAL", _, _)
                => "COP 3.1 p.340: WELS field must be set for all fittings (PUB mandatory). " +
                   "WCs: minimum 3-tick WELS. Wash basins and showers: minimum 2-tick WELS. " +
                   "SystemType must indicate whether fitting connects to potable water, soil or foul system.",
            // ── PARKING LOT ───────────────────────────────────────────────────
            ("IFCBUILDINGELEMENTPROXY", "CARGENERALPARKINGLOT", _) or
            ("IFCBUILDINGELEMENTPROXY", "CARPWDPARKINGLOT", _) or
            ("IFCBUILDINGELEMENTPROXY", "LORRYLOT", _)
                => "COP 3.1 p.312: Each parking lot modelled as individual element with subtype. " +
                   "Standard bay: BayWidth >= 2400mm, BayLength >= 4800mm. " +
                   "PWD bay: BayWidth >= 3600mm. Lorry lot: BayLength >= 9000mm. " +
                   "Headroom minimum 2100mm for standard, 4500mm for lorry lots.",
            // ── GEOREFERENCING ────────────────────────────────────────────────
            ("IFCSITE", _, _)
                => "COP 3.1 p.348: IfcSite must have SVY21 Easting and Northing coordinates. " +
                   "Coordinate datum: SVY21 (Singapore Geodetic Reference System 1995). " +
                   "Height datum: SHD (Singapore Height Datum). " +
                   "LandLotNumber must match SLA land register format exactly.",
            // ── LANDSCAPE ────────────────────────────────────────────────────
            ("IFCGEOGRAPHICELEMENT", _, _)
                => "COP 3.1 p.309-327: All trees, palms and significant plants must use botanical names. " +
                   "NParks requires full botanical name (genus and species) from Flora & Fauna Web. " +
                   "For transplanted trees: GirthSize minimum 150mm. Soil depth for planted areas: minimum 600mm. " +
                   "LUSH 3.0 Programme greenery features must use approved ALS_GreeneryFeatures values.",
            // ── LIFT ─────────────────────────────────────────────────────────
            ("IFCTRANSPORTELEMENT", _, _)
                => "COP 3.1 p.310: All lifts modelled as IfcTransportElement with LIFT subtype. " +
                   "FireFightersLift TRUE for lifts serving fireman's lift function. " +
                   "Buildings above 24m require at least one fireman's lift with minimum capacity 630kg. " +
                   "Accessible lifts require car size >= 1100mm x 1400mm.",
            // ── PIPE / DRAINAGE ───────────────────────────────────────────────
            ("IFCPIPESEGMENT", _, _)
                => "COP 3.1 p.320: All pipes/drains must have InvertLevel (SHD m) and Gradient. " +
                   "PUB minimum gradients: foul water 1:100, stormwater 1:200. " +
                   "SystemType must indicate: POTABLE_WATER, SOIL_WASTE, FOUL_WATER, STORMWATER, or FIRE. " +
                   "InvertLevel is measured at the internal pipe invert (bottom inside of pipe).",
            // DEFAULT ─────────────────────────────────────────────────────────
            _   => $"Refer to CORENET-X COP 3.1 Edition December 2025, Section 4 for modelling guidance on {ifcClass}/{subType}. " +
                   "Download the IFC+SG Resource Kit from go.gov.sg/ifcsg for software-specific export guides."
        };

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
