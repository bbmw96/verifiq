// VERIFIQ - Rules Implementations
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// Real (non-stub) implementations of ISgRules and IMyRules.
// These expose agency-level and UBBL-level rule metadata used by the
// validation engine and displayed in the Rules Database page.
// The primary compliance checking logic is in SqliteRulesDatabase and
// ValidationEngine; these classes provide structured agency requirements
// for richer findings context and the Rules Database information page.

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Rules.Common;

// ─────────────────────────────────────────────────────────────────────────────
//  SINGAPORE - IFC+SG / CORENET-X
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Singapore IFC+SG rules covering all 8 regulatory agencies.
/// Data sourced from CORENET-X COP 3.1 Edition (December 2025),
/// IFC+SG Industry Mapping 2025, and individual agency code requirements.
/// </summary>
public sealed class SgRules : ISgRules
{
    public List<SgAgencyRequirement> GetAgencyRequirements(
        string ifcClass, SgAgency agency, CorenetGateway gateway)
    {
        var cls = ifcClass.ToUpperInvariant();
        var all = _agencyRequirements
            .Where(r => (r.Agency == agency || agency == SgAgency.All) &&
                        (r.Gateway == gateway || r.Gateway == CorenetGateway.Design))
            .ToList();

        // Further filter by element class if a specific class is requested
        if (!string.IsNullOrWhiteSpace(cls) && cls != "ALL")
            all = all.Where(r => string.IsNullOrEmpty(r.IfcClassFilter) ||
                                 r.IfcClassFilter.Equals(cls, StringComparison.OrdinalIgnoreCase))
                     .ToList();

        return all.Cast<SgAgencyRequirement>().ToList();
    }

    public List<string> GetMandatoryAgenciesForElement(string ifcClass)
    {
        var cls = ifcClass.ToUpperInvariant();
        return _elementAgencyMap.TryGetValue(cls, out var list) ? list : new();
    }

    public bool IsSubmissionRequired(string ifcClass, CorenetGateway gateway)
    {
        // All physical building elements require submission data at Construction gateway
        var exemptClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IFCBUILDINGELEMENTPROXY","IFCVIRTUALELEMENT","IFCANNOTATION"
        };
        return !exemptClasses.Contains(ifcClass);
    }

    // ── Mandatory agencies by IFC class ──────────────────────────────────────

    private static readonly Dictionary<string, List<string>> _elementAgencyMap =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFCWALL"]         = new() { "BCA","SCDF" },
        ["IFCSLAB"]         = new() { "BCA","SCDF" },
        ["IFCDOOR"]         = new() { "BCA","SCDF" },
        ["IFCWINDOW"]       = new() { "BCA" },
        ["IFCROOF"]         = new() { "BCA","SCDF" },
        ["IFCBEAM"]         = new() { "BCA" },
        ["IFCCOLUMN"]       = new() { "BCA" },
        ["IFCFOOTING"]      = new() { "BCA" },
        ["IFCPILE"]         = new() { "BCA" },
        ["IFCSTAIR"]        = new() { "BCA","SCDF" },
        ["IFCSTAIRFLIGHT"]  = new() { "BCA","SCDF" },
        ["IFCRAMP"]         = new() { "BCA" },
        ["IFCSPACE"]        = new() { "URA","SCDF","NEA" },
        ["IFCSITE"]         = new() { "URA","SLA","BCA" },
        ["IFCBUILDING"]     = new() { "BCA","URA" },
        ["IFCBUILDINGSTOREY"]= new() { "BCA" },
        ["IFCRAILING"]      = new() { "BCA" },
        ["IFCSANITARYTERMINAL"] = new() { "PUB" },
        ["IFCPIPESEGMENT"]  = new() { "PUB","SCDF" },
        ["IFCDUCTSEGMENT"]  = new() { "NEA","SCDF" },
        ["IFCAIRTERMINAL"]  = new() { "NEA" },
        ["IFCLIGHTFIXTURE"] = new() { "BCA" },
        ["IFCCURTAINWALL"]  = new() { "BCA","SCDF" },
        ["IFCOPENINGELEMENT"]      = new() { "BCA","SCDF" },
        // ── Additional entities from COP 3.1 Section 4 ────────────────────────
        ["IFCCOVERING"]            = new() { "BCA","SCDF" },
        ["IFCROOF"]                = new() { "BCA","SCDF" },
        ["IFCSTAIR"]               = new() { "BCA","SCDF" },
        ["IFCSTAIRFLIGHT"]         = new() { "BCA","SCDF" },
        ["IFCRAMP"]                = new() { "BCA" },
        ["IFCRAILING"]             = new() { "BCA" },
        ["IFCFOOTING"]             = new() { "BCA" },
        ["IFCPILE"]                = new() { "BCA" },
        ["IFCSANITARYTERMINAL"]    = new() { "PUB" },
        ["IFCPIPESEGMENT"]         = new() { "PUB","SCDF" },
        ["IFCPIPEFITTING"]         = new() { "PUB" },
        ["IFCFIRESUPPRESSIONTERMINAL"] = new() { "SCDF" },
        ["IFCDAMPER"]              = new() { "SCDF" },
        ["IFCVALVE"]               = new() { "PUB","SCDF" },
        ["IFCTANK"]                = new() { "PUB","NEA" },
        ["IFCPUMP"]                = new() { "PUB" },
        ["IFCINTERCEPTOR"]         = new() { "NEA" },
        ["IFCWASTETERMINAL"]       = new() { "PUB" },
        ["IFCFLOWMETER"]           = new() { "PUB" },
        ["IFCTRANSPORTELEMENT"]    = new() { "BCA" },
        ["IFCGEOGRAPHICELEMENT"]   = new() { "NParks","SLA" },
        ["IFCCIVILELEMENT"]        = new() { "LTA","PUB" },
        ["IFCDISCRETEACCESSORY"]   = new() { "PUB" },
        ["IFCFURNITURE"]           = new() { "BCA" },
        ["IFCSHADINGDEVICE"]       = new() { "BCA" },
        ["IFCUNITARYCONTROLELEMENT"] = new() { "BCA" },
        ["IFCSWITCHINGDEVICE"]     = new() { "BCA" },
        ["IFCINTERCEPTOR"]         = new() { "NEA" },
    };

    // ── Agency requirements dataset ───────────────────────────────────────────

    private static readonly List<ExtSgRequirement> _agencyRequirements = new()
    {
// ── SINGAPORE EXPANSION - added to RulesStubs.cs _agencyRequirements list ──
// This block replaces the existing list with 100+ comprehensive rules
// covering all 8 agencies across all 5 gateways and all element types.

        // ══════════════════════════════════════════════════════════════════════
        // BCA - BUILDING AND CONSTRUCTION AUTHORITY (25 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("BCA-STRUCT-001", SgAgency.BCA, CorenetGateway.Construction,
            "Structural Adequacy - load-bearing elements (BC 2:2021 / SS EN 1992/1993)",
            "All structural elements must be designed per BC 2:2021 (concrete), SS EN 1993 (steel), or SS EN 1995 (timber). Design loading per SS EN 1991-1-1. All structural models must be submitted by a QP (Engineer) at the Construction Gateway.",
            "Building Control Act (Cap 29) s.5 / BC 2:2021 / SS EN 1992-1-1 / SS EN 1993-1-1", true, null),

        Sg("BCA-STRUCT-002", SgAgency.BCA, CorenetGateway.Piling,
            "Foundation Design - piling works prior approval",
            "Piling Gateway submission required before any piling works. IfcPile must have SGPset_PileFoundation.PileType (BORED/DRIVEN/JETGROUTING), DesignLoad (kN), PileLength (m), PileDiameter (m), and reference to SE endorsement.",
            "Building Control Regulations 2003 Reg 12 / BC 3:2013 (Foundations)", true, "IFCPILE"),

        Sg("BCA-STRUCT-003", SgAgency.BCA, CorenetGateway.Construction,
            "Column - structural concrete grade and design code",
            "IfcColumn must have SGPset_ColumnStructural.ConcreteGrade (e.g. C32/40), ReinforcementGrade (e.g. T40), DesignCode (BC2:2021), and LoadBearing=TRUE in Pset_ColumnCommon.",
            "BC 2:2021 §3.1 / SS EN 1992-1-1 §3.1", true, "IFCCOLUMN"),

        Sg("BCA-STRUCT-004", SgAgency.BCA, CorenetGateway.Construction,
            "Beam - structural properties and fire resistance",
            "IfcBeam must specify LoadBearing, FireRating (REI notation), and material specification. Transfer beams and primary beams require separate structural calculation endorsement.",
            "BC 2:2021 §5 / SS EN 1992-1-1 §5 / SCDF Fire Code §4.3", true, "IFCBEAM"),

        Sg("BCA-STRUCT-005", SgAgency.BCA, CorenetGateway.Construction,
            "Slab - floor and roof slab fire resistance and loading",
            "IfcSlab must include FireRating (FRR per SCDF), ThermalTransmittance (U-value for roof/external), and IsExternal flag. Flat roofs require waterproofing membrane specification in Pset_SlabCommon.",
            "BC 2:2021 §4 / SCDF Fire Code Table 4.2 / BCA Green Mark 2021 §3.4.1", true, "IFCSLAB"),

        Sg("BCA-FOUND-001", SgAgency.BCA, CorenetGateway.Piling,
            "Footing - pad and strip footing bearing capacity",
            "IfcFooting must include SGPset_FootingFoundation.SoilBearingCapacity (kPa), FootingType (PAD/STRIP/RAFT), and FrostDepth. Endorsed by QP (Engineer) before earthwork.",
            "BC 3:2013 (Foundations) §4", true, "IFCFOOTING"),

        Sg("BCA-WALLS-001", SgAgency.BCA, CorenetGateway.Construction,
            "Wall - IsExternal, LoadBearing and fire rating",
            "IfcWall and IfcWallStandardCase must declare IsExternal, LoadBearing, and FireRating. Party walls and separation walls require FRR ≥ 60 min. External walls must declare ThermalTransmittance per BCA Green Mark.",
            "Building Control Act / BC 2:2021 / SCDF Fire Code §4.3", true, "IFCWALL"),

        Sg("BCA-STAIR-001", SgAgency.BCA, CorenetGateway.Construction,
            "Stair - riser, tread and handrail requirements",
            "IfcStair must declare HandicapAccessible, NumberOfRisers, RiserHeight (≤175mm), TreadLength (≥280mm). Fire escape stairs must be enclosed with FRR ≥ 60 min and width ≥ 1100mm (≥1200mm above 24m building height).",
            "Code on Accessibility 2025 §4.3 / SCDF Fire Code §5.4", true, "IFCSTAIR"),

        Sg("BCA-RAMP-001", SgAgency.BCA, CorenetGateway.Construction,
            "Ramp - gradient, width and accessible ramp requirements",
            "IfcRamp must declare HandicapAccessible and SGPset_RampAccessibility.Gradient (max 1:12 = 0.0833), Width (min 1200mm). All ramps on accessible routes must have kerb guards, handrails both sides, and level landings at top and bottom.",
            "Code on Accessibility 2025 §4.3 / BC 2:2021 §9", true, "IFCRAMP"),

        Sg("BCA-DOOR-001", SgAgency.BCA, CorenetGateway.Construction,
            "Door - clear width, HandicapAccessible and fire rating",
            "IfcDoor must have ClearWidth (accessible doors ≥850mm, preferred ≥900mm), HandicapAccessible (TRUE/FALSE), FireRating (for compartment doors), FireExit (TRUE for escape route doors), and SmokeStop.",
            "Code on Accessibility 2025 §4.2.1 / SCDF Fire Code §5.3", true, "IFCDOOR"),

        Sg("BCA-WIN-001", SgAgency.BCA, CorenetGateway.Construction,
            "Window - U-value, SHGC and natural ventilation",
            "IfcWindow must declare ThermalTransmittance (U-value, W/m²K), SolarHeatGainCoefficient (SHGC ≤0.3), IsExternal, and OpeningArea for natural ventilation calculations per NEA EPHA.",
            "BCA Green Mark 2021 §3.4.3 / NEA EPHA", true, "IFCWINDOW"),

        Sg("BCA-ACCESS-001", SgAgency.BCA, CorenetGateway.Construction,
            "Code on Accessibility 2025 - continuous accessible route",
            "An unbroken accessible route must be provided from the building entrance to all accessible facilities: lifts, accessible parking, accessible toilets, and at least one accessible entrance on every accessible floor.",
            "Code on Accessibility 2025 (BCA) Chapter 4", true, null),

        Sg("BCA-ACCESS-002", SgAgency.BCA, CorenetGateway.Construction,
            "Accessible toilet - minimum dimensions and fittings",
            "Accessible toilet enclosure must have minimum clear floor area 1800mm × 2200mm, clear door width ≥850mm, WC with transfer space on one side, grab bars per CoA 2025 §4.2.2.",
            "Code on Accessibility 2025 §4.2.2", true, "IFCSPACE"),

        Sg("BCA-GM-001", SgAgency.BCA, CorenetGateway.Construction,
            "Green Mark 2021 - ETTV (non-residential) or RETV (residential)",
            "Non-residential: ETTV ≤50 W/m². Residential: RETV ≤25 W/m². IfcWindow and IfcWall must have ThermalTransmittance and SolarHeatGainCoefficient values to enable ETTV computation. Applies to GFA ≥1,000m².",
            "BCA Green Mark 2021 §3.4 / BC(ES)R 2008", true, null),

        Sg("BCA-GM-002", SgAgency.BCA, CorenetGateway.Construction,
            "Green Mark 2021 - Lighting Power Density (LPD)",
            "Maximum LPD: Office 12 W/m², Retail 20 W/m², Hotel guestroom 10 W/m², Corridor 5 W/m², Carpark 3 W/m². IfcLightFixture must declare LampType, RatedPower (W), and DesignIlluminance (lux).",
            "BCA Green Mark 2021 §3.5 - Lighting and Electrical", true, "IFCLIGHTFIXTURE"),

        Sg("BCA-GM-003", SgAgency.BCA, CorenetGateway.Construction,
            "Green Mark 2021 - Roof U-value and Green Roof",
            "Roof U-value ≤0.35 W/m²K. IfcRoof and roof IfcSlab must include ThermalTransmittance in Pset_RoofCommon / Pset_SlabCommon. Green roof areas count towards site greenery replacement.",
            "BCA Green Mark 2021 §3.4.1 / NParks LUSH Programme", true, "IFCROOF"),

        Sg("BCA-LIFT-001", SgAgency.BCA, CorenetGateway.Construction,
            "Lift - accessible lift car dimensions",
            "IfcTransportElement (elevator) must have lift car dimensions ≥1100mm (W) × 1400mm (D), door clear width ≥900mm, call buttons at 900–1200mm AFF, floor level indicator, and emergency communication. Accessible lifts must serve all storeys.",
            "Code on Accessibility 2025 §4.5 / CP 2:2021 (Lifts and Escalators)", true, "IFCTRANSPORTELEMENT"),

        Sg("BCA-FIRE-WALL", SgAgency.BCA, CorenetGateway.Construction,
            "Party wall and separation wall - fire resistance",
            "Walls between different occupancies or tenancies must have fire resistance ≥ as required by SCDF. Structural separation walls must be continuous from floor to soffit.",
            "SCDF Fire Code §4.3 Table 4.2 / Building Control Act", true, "IFCWALL"),

        Sg("BCA-CURTWALL", SgAgency.BCA, CorenetGateway.Construction,
            "Curtain wall - thermal, structural and fire performance",
            "IfcCurtainWall must declare ThermalTransmittance (whole-system U-value), SolarHeatGainCoefficient (SHGC ≤0.30 per Green Mark), and SGPset_CurtainWallPerformance.WindLoadDesign and StructuralSiliconeDesign.",
            "BCA Green Mark 2021 §3.4.3 / SS 553:2016 (Curtain Walls)", true, "IFCCURTAINWALL"),

        Sg("BCA-COVER-001", SgAgency.BCA, CorenetGateway.Construction,
            "Ceiling - fire-rated ceiling system",
            "Suspended ceilings below fire-rated slabs must not reduce the effective FRR below the required period. SGPset_CeilingSystem.FireRated (TRUE/FALSE) and SystemProvider must be declared.",
            "SCDF Fire Code §4.3 / BCA Technical Note on Ceiling Systems", true, "IFCCOVERING"),

        Sg("BCA-SITE-001", SgAgency.BCA, CorenetGateway.Design,
            "Site - SVY21 georeferencing mandatory",
            "IfcSite must have IfcMapConversion to SVY21 (EPSG:3414). MapUnit = METRE, Eastings/Northings in SVY21 coordinates. All building coordinates must reference the national grid.",
            "Singapore Land Authority (SLA) - SVY21 / IFC+SG COP3 §2.1", true, "IFCSITE"),

        Sg("BCA-BLDG-001", SgAgency.BCA, CorenetGateway.Design,
            "Building - storey heights and gross floor area",
            "IfcBuilding must reference all IfcBuildingStorey elements. Each storey must have ElevationOfStorey and ElevationOfSStorey. IfcSpace.GrossPlannedArea must be declared for URA GFA computation.",
            "IFC+SG COP3 §3 / URA Development Control", true, "IFCBUILDING"),

        Sg("BCA-MAT-001", SgAgency.BCA, CorenetGateway.Construction,
            "Material association - structural and fire elements",
            "All structural (columns, beams, slabs, walls, foundations) and fire-rated elements must have IfcMaterialLayerSet or IfcMaterial associated. Material name must match approved specifications.",
            "BC 2:2021 §2 / SS EN 1992-1-1 §3", true, null),

        // ══════════════════════════════════════════════════════════════════════
        // URA - URBAN REDEVELOPMENT AUTHORITY (15 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("URA-GFA-001", SgAgency.URA, CorenetGateway.Design,
            "GFA - IfcSpace gross planned area declaration",
            "All IfcSpace elements must have Pset_SpaceCommon.GrossPlannedArea (m²) and SGPset_SpaceGFA.GFACategory. The sum of GFA must match the declared GFA on the planning permit. GFA categories: RESIDENTIAL, OFFICE, RETAIL, HOTEL, CARPARK, VOID, BALCONY.",
            "URA Development Control Handbook - Gross Floor Area", true, "IFCSPACE"),

        Sg("URA-GFA-002", SgAgency.URA, CorenetGateway.Design,
            "GFA exemption - balcony area ≤10% of unit GFA",
            "Balcony spaces (SGPset_SpaceGFA.GFACategory = BALCONY) must not exceed 10% of the unit's nett internal floor area. SGPset_SpaceGFA.IsGFAExempt must be set to TRUE only for URA-approved GFA concessions.",
            "URA Development Control Handbook - Balcony Incentive", true, "IFCSPACE"),

        Sg("URA-GFA-003", SgAgency.URA, CorenetGateway.Design,
            "Space category - mandatory enumeration per URA permitted values",
            "Pset_SpaceCommon.Category must use URA-permitted values: LIVING_ROOM, BEDROOM, MASTER_BEDROOM, KITCHEN, STUDY, BATHROOM, ACCESSIBLE_TOILET, CORRIDOR, LOBBY, LIFT_LOBBY, STAIRCASE, CARPARK, PLANT_ROOM, VOID, BALCONY, ROOF_TERRACE, WET_PANTRY, DRY_PANTRY, SERVICE_YARD.",
            "IFC+SG Industry Mapping 2025 - Space Categories", true, "IFCSPACE"),

        Sg("URA-PLAN-001", SgAgency.URA, CorenetGateway.Design,
            "Plot ratio - building height and plot ratio compliance",
            "IfcBuilding must declare SGPset_BuildingPlanningParams.PlotRatio, MaxHeightAGL (m), MaxStoreys, and ZoneType (per URA Master Plan 2019). These are checked against URA Grant of Written Permission.",
            "URA Master Plan 2019 / Planning Act (Cap 232)", true, "IFCBUILDING"),

        Sg("URA-PLAN-002", SgAgency.URA, CorenetGateway.Design,
            "Setbacks - road reserve, side and rear setbacks",
            "IfcSite must declare SGPset_SiteSetbacks.RoadReserveSetback (m, by road category), SideSetback, and RearSetback. Category 1 road: ≥7.5m. Category 2: ≥5m. Category 3-5: ≥3m.",
            "URA Development Control - Setback Requirements", true, "IFCSITE"),

        Sg("URA-ROOM-001", SgAgency.URA, CorenetGateway.Design,
            "Bedroom - minimum area ≥9m² (private residential)",
            "IfcSpace categorised as BEDROOM must have GrossPlannedArea ≥9.0m² per URA Planning Parameters 2023 §3.1. Master bedroom must be ≥12.5m². Non-compliance will result in planning objection.",
            "URA Planning Parameters 2023 §3.1", true, "IFCSPACE"),

        Sg("URA-ROOM-002", SgAgency.URA, CorenetGateway.Design,
            "Living room - minimum area ≥13m² (private residential)",
            "IfcSpace categorised as LIVING_ROOM in private residential developments must have GrossPlannedArea ≥13.0m². HDB public housing living rooms must be ≥16.0m².",
            "URA Planning Parameters 2023 §2.3", true, "IFCSPACE"),

        Sg("URA-ROOM-003", SgAgency.URA, CorenetGateway.Design,
            "Kitchen - minimum area ≥4.5m²",
            "IfcSpace categorised as KITCHEN must have GrossPlannedArea ≥4.5m². Kitchenette in serviced apartments must be ≥3.0m². Open-plan kitchen/living counted as combined area ≥17.5m².",
            "URA Planning Parameters 2023 §3.3", true, "IFCSPACE"),

        Sg("URA-UNIT-001", SgAgency.URA, CorenetGateway.Design,
            "Minimum unit size - residential units ≥35m²",
            "Individual residential units in private developments must have a minimum nett internal floor area of 35m² per URA DC parameters. Serviced apartments minimum 35m² (studio) or 45m² (with separate bedroom).",
            "URA Development Control - Minimum Unit Size", true, "IFCSPACE"),

        Sg("URA-CARPARK-001", SgAgency.URA, CorenetGateway.Construction,
            "Carpark - standard bay 2.5m × 5.0m, accessible bay 3.6m × 5.0m",
            "IfcSpace categorised as CARPARK must declare SGPset_ParkingBay.BayWidth (≥2.5m), BayLength (≥5.0m), ClearHeight (≥2.1m). Accessible bays: ≥3.6m × 5.0m. One accessible bay required per 50 bays up to 200, then 1 per 100.",
            "LTA Circular on Parking Provision / Code on Accessibility 2025 §4.6", true, "IFCSPACE"),

        Sg("URA-LOADING-001", SgAgency.URA, CorenetGateway.Construction,
            "Loading/unloading bay - dimension requirements",
            "Loading bay: ≥3.5m (W) × 12.0m (L) × 4.2m clear height. Heavy vehicle loading bay: ≥3.5m × 18m × 5.0m. Must be within building curtilage and not obstruct public footway.",
            "LTA Technical Specifications - Loading Bay", true, "IFCSPACE"),

        Sg("URA-SETBACK-001", SgAgency.URA, CorenetGateway.Design,
            "Sky terrace and communal spaces - GFA treatment",
            "Sky terraces (open to sky, accessible by residents) may receive GFA concession if ≥5% of total GFA. Must be declared in IfcSpace as ROOF_TERRACE with IsGFAExempt = TRUE and SGPset_SpaceGFA.GFAConcessType = SKY_TERRACE.",
            "URA Development Control - Sky Terrace Incentive", true, "IFCSPACE"),

        Sg("URA-FSRV-001", SgAgency.URA, CorenetGateway.Design,
            "Floor-to-floor height - minimum 2.4m",
            "All habitable floors must have floor-to-floor height ≥2.4m. IfcBuildingStorey.ElevationOfStorey subtracted from next storey must be ≥2.4m. Attic exemptions apply for landed properties.",
            "URA Development Control Handbook §2.8", true, "IFCBUILDINGSTOREY"),

        Sg("URA-BC-001", SgAgency.URA, CorenetGateway.Design,
            "IfcClassificationReference - URA use zone classification",
            "All IfcSpace elements must have IfcClassificationReference referencing the URA Master Plan 2019 use zone (Residential, Commercial, Business 1, Business 2, etc.).",
            "IFC+SG Industry Mapping 2025 - Classification Reference / URA Master Plan", true, "IFCSPACE"),

        Sg("URA-CLASS-001", SgAgency.URA, CorenetGateway.Design,
            "IFC+SG Classification - all elements must be classified",
            "All IfcElement elements must have IfcClassificationReference linking to the IFC+SG Industry Mapping 2025. Classification items must include Notation (code), Name (description), and Source (IFC+SG 2025 edition).",
            "IFC+SG Industry Mapping 2025 - IFC+SG Classification System COP3", true, null),

        // ══════════════════════════════════════════════════════════════════════
        // SCDF - SINGAPORE CIVIL DEFENCE FORCE (15 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("SCDF-COMP-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Fire compartmentation - maximum compartment floor area",
            "Fire compartments must not exceed 7,000m² (sprinklered) or 3,500m² (non-sprinklered) per floor. Compartment separating elements (walls, floors) must have FRR ≥ requirements in SCDF Table 4.2. Compartment walls must extend full storey height.",
            "SCDF Fire Code 2018 §4.3 Table 4.3", true, null),

        Sg("SCDF-COMP-002", SgAgency.SCDF, CorenetGateway.Construction,
            "Wall fire resistance rating - FRR per SCDF Table 4.2",
            "IfcWall used as compartment wall or separation wall must have Pset_WallCommon.FireRating in REI notation: '60/60/60', '90/90/90', '120/120/120'. Residential: FRR 60 min. Commercial (sprinklered): FRR 60 min. Commercial (non-sprinklered): FRR 120 min.",
            "SCDF Fire Code 2018 §4.3 Table 4.2", true, "IFCWALL"),

        Sg("SCDF-COMP-003", SgAgency.SCDF, CorenetGateway.Construction,
            "Slab fire resistance rating - floor and roof FRR",
            "IfcSlab used as compartment floor/ceiling must have Pset_SlabCommon.FireRating. Floor slabs separating fire compartments: FRR ≥ 60 min. Roof slabs: FRR ≥ 30 min (non-sprinklered residential). SGPset_SlabFireRating.FireResistancePeriod must match.",
            "SCDF Fire Code 2018 §4.3 Table 4.2", true, "IFCSLAB"),

        Sg("SCDF-EXIT-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Exit doors - minimum width and direction of swing",
            "Exit doors must have clear width ≥750mm (small occupancy, <60 persons) or ≥1,050mm (≥60 persons). All exit doors must open in direction of travel. FireExit=TRUE must be set in Pset_DoorCommon. Fire door leaves must have self-closing devices.",
            "SCDF Fire Code 2018 §5.3.1", true, "IFCDOOR"),

        Sg("SCDF-EXIT-002", SgAgency.SCDF, CorenetGateway.Construction,
            "Escape stair - width, enclosure and pressurisation",
            "Escape stairs must be ≥1,100mm clear width (≤24m building) or ≥1,200mm (>24m building height). Must be enclosed in ≥FRR 60 min construction. Buildings >60m height require pressurised staircase (SGPset_StairFireEscape.IsPressurised).",
            "SCDF Fire Code 2018 §5.4.2 / SCDF Performance Requirements - High-Rise", true, "IFCSTAIR"),

        Sg("SCDF-EXIT-003", SgAgency.SCDF, CorenetGateway.Construction,
            "Travel distance - to nearest exit",
            "Maximum travel distance to nearest exit: 30m (non-sprinklered) or 60m (fully sprinklered). Dead-end corridors must not exceed 7.5m. SGPset_StairFireEscape.TravelDistance must be declared on escape stair elements.",
            "SCDF Fire Code 2018 §5.2", true, null),

        Sg("SCDF-EXIT-004", SgAgency.SCDF, CorenetGateway.Construction,
            "Minimum 2 escape routes - occupancy ≥60 persons",
            "All buildings with floor occupancy ≥60 persons must have ≥2 separate escape routes at each level. Each route must be remote from the other (angular separation ≥45°). IfcZone grouping escape routes must be declared.",
            "SCDF Fire Code 2018 §5.2.1", true, null),

        Sg("SCDF-HOSE-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Fire hose reel - spacing and height requirements",
            "IfcFlowTerminal (FIREHOSEREEL) must be positioned within 30m hose reach of any point in the building. Mounting height 1.0m to 1.5m AFF. Minimum one hose reel per 20m of corridor per floor.",
            "SCDF Fire Code 2018 §6 / SS 578 (Hose Reels)", true, "IFCFLOWTERMINAL"),

        Sg("SCDF-SPRINK-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Sprinkler system - coverage and spacing",
            "IfcPipeSegment (sprinkler mains) must declare SGPset_FireSuppression.SystemType (WET/DRY/PREACTION), CoverageArea (m²), MaxHeadSpacing (m). Residential quick-response heads ≤ 12 m² coverage.",
            "SS 537 (Sprinkler Systems) / SCDF Fire Code §6.3", true, "IFCPIPESEGMENT"),

        Sg("SCDF-DET-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Fire detection - smoke detector coverage",
            "Smoke detectors (IfcSensor with SGPset_FireDetection.DetectorType = SMOKE) must be within 7.5m of any point in protected area. Heat detectors in kitchens and plant rooms. Alarm must be audible at 65dB at any point.",
            "SS 550 (Fire Detection and Alarm Systems) / SCDF Fire Code §6.4", true, "IFCSENSOR"),

        Sg("SCDF-HYDRANT-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Fire hydrant - perimeter hydrant provision",
            "Perimeter hydrants required within 90m of any part of the building for buildings >5 storeys or >500m² footprint. Dry riser inlet within 18m of hydrant. IfcFlowTerminal (FIREHYDRANT) must declare SGPset_Hydrant.FlowRate (L/min).",
            "SCDF Fire Code 2018 §6.2 / SS 575 (Fire Hydrant Systems)", true, "IFCFLOWTERMINAL"),

        Sg("SCDF-DRY-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Dry/wet riser - high-rise mandatory provision",
            "Buildings ≥4 storeys with any floor >7m above ground require dry riser or wet riser system. Buildings >60m: wet riser mandatory. IfcPipeSegment (RISER) must declare SystemType, InletLocation, and MaxFloorServed.",
            "SCDF Fire Code 2018 §6.5 / SS 575", true, "IFCPIPESEGMENT"),

        Sg("SCDF-SMOKE-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Smoke control - mechanical smoke extraction for large spaces",
            "Basements, atria, large open floors (>2,000m²) require mechanical smoke exhaust. Atria ≥3 storeys require smoke reservoirs and exhaust fans. IfcDuctSegment must declare SGPset_SmokeControl.ExhaustRate (m³/s).",
            "SCDF Fire Code 2018 §7 - Smoke Control / SS EN 12101", true, "IFCDUCTSEGMENT"),

        Sg("SCDF-EMGLT-001", SgAgency.SCDF, CorenetGateway.Construction,
            "Emergency lighting - maintained luminaires on escape routes",
            "Emergency lighting must provide ≥1 lux on escape route floor level, ≥5 lux at evacuation assembly points, and battery backup ≥3 hours. IfcLightFixture.SGPset_EmergencyLighting.IsEmergency = TRUE on all escape route luminaires.",
            "SCDF Fire Code 2018 §6.6 / SS 563 (Emergency Lighting)", true, "IFCLIGHTFIXTURE"),

        Sg("SCDF-EXIT-SIGN", SgAgency.SCDF, CorenetGateway.Construction,
            "Exit signage - photoluminescent or internally illuminated",
            "All exit doors and directional signs on escape routes must be internally illuminated (≥1 lux at 30m) or photoluminescent (Class B min). IfcAnnotation.SGPset_ExitSign.SignType must be declared for all exit signs.",
            "SCDF Fire Code 2018 §6.7 / SS ISO 3864", true, null),

        // ══════════════════════════════════════════════════════════════════════
        // LTA - LAND TRANSPORT AUTHORITY (10 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("LTA-PARK-001", SgAgency.LTA, CorenetGateway.Construction,
            "Parking quantum - minimum provision by use type",
            "IfcSpace (CARPARK) must collectively meet LTA parking quantum. Residential: 1 space/unit (private), 0.7/unit (HDB). Office: 1/200m² GFA. Retail: 1/200m² GFA (core districts 1/500m²). EV charging provision ≥10% of total bays from 2024.",
            "LTA Travel Smart Master Plan / Development Control Circular (Parking Quantum)", true, "IFCSPACE"),

        Sg("LTA-PARK-002", SgAgency.LTA, CorenetGateway.Construction,
            "Standard parking bay - 2.5m × 5.0m",
            "Standard parking bay: 2.5m wide × 5.0m long. Parallel parking: 2.3m × 6.0m. Aisle width: 6.0m (90°), 3.6m (45°). All dimensions must be declared in SGPset_ParkingBay or IfcSpace bounding geometry.",
            "LTA Circular - Parking Bay Dimensions", true, "IFCSPACE"),

        Sg("LTA-PARK-003", SgAgency.LTA, CorenetGateway.Construction,
            "Accessible parking - location and dimensions",
            "Accessible bays: ≥3.6m × 5.0m with 1.2m side transfer zone. Located nearest to accessible entrance. Minimum 1 bay per 50 bays (first 200 bays), then 1 per 100. SGPset_ParkingBay.IsAccessible = TRUE.",
            "Code on Accessibility 2025 §4.6 / LTA Circular", true, "IFCSPACE"),

        Sg("LTA-LOAD-001", SgAgency.LTA, CorenetGateway.Construction,
            "Loading bay - minimum dimensions and headroom",
            "Loading bay: ≥3.5m (W) × 12m (L) × 4.2m clear headroom. Heavy vehicle: ≥3.5m × 18m × 5.0m. Manoeuvring area must allow full vehicle turn without using public road. IfcSpace.SGPset_LoadingBay.VehicleType must be declared.",
            "LTA Technical Specifications - Loading and Unloading Bays", true, "IFCSPACE"),

        Sg("LTA-DRIVEWAY-001", SgAgency.LTA, CorenetGateway.Construction,
            "Driveway - width and visibility splay",
            "Driveway: ≥4.5m (two-way) or ≥3.0m (one-way). Visibility splay at exit: 4.5m × 4.5m min. Maximum gradient 1:6 for first 5m from boundary. Speed ramp at exit required for residential >50 units.",
            "LTA Technical Specifications - Driveway Design", true, "IFCSITE"),

        Sg("LTA-CYCLING-001", SgAgency.LTA, CorenetGateway.Construction,
            "Cycling facilities - bicycle parking and end-of-trip",
            "Office/retail buildings >1,000m² GFA require bicycle parking: 1 space per 200m² GFA (office), 1 per 500m² (retail). End-of-trip facilities (lockers, shower) for >100 spaces. IfcSpace (BICYCLE_STORE) must be declared.",
            "LTA Active Mobility Framework / JTC/BCA Circular on Cycling Infrastructure", true, "IFCSPACE"),

        Sg("LTA-EV-001", SgAgency.LTA, CorenetGateway.Construction,
            "EV charging - conduit provision for future EV chargers",
            "From 2024, all new non-residential buildings and residential developments ≥5 storeys must provide conduit infrastructure for EV charging for ≥10% of car parking spaces. IfcDistributionElement (CONDUIT) must be tagged.",
            "LTA EV Charging Infrastructure Circular 2023", true, null),

        Sg("LTA-TAXI-001", SgAgency.LTA, CorenetGateway.Construction,
            "Taxi/pick-up - sheltered taxi stand for hotel and commercial",
            "Hotels, convention centres and major shopping centres must provide sheltered taxi/private hire vehicle holding bays: ≥4 spaces (hotel <200 rooms), ≥8 spaces (hotel ≥200 rooms). Aisle ≥4.5m.",
            "LTA Development Control - Taxi and Private Hire", true, "IFCSPACE"),

        Sg("LTA-FREIGHT-001", SgAgency.LTA, CorenetGateway.Construction,
            "Refuse collection and waste - NEA compliant refuse chamber",
            "Residential: refuse chamber per ≤20 units with direct access from common corridor. IfcSpace (REFUSE_CHAMBER) must have dimensions per NEA Code of Practice on Environmental Health. Area ≥3.0m² minimum.",
            "NEA COPEH / LTA Development Control", true, "IFCSPACE"),

        Sg("LTA-SIGN-001", SgAgency.LTA, CorenetGateway.Design,
            "Vehicular signage - traffic management plan",
            "SGPset_TrafficManagement.VehicularSignageRequired = TRUE for developments with ≥50 car parking spaces. Traffic Impact Assessment (TIA) required for developments generating ≥100 trips/hr.",
            "LTA Traffic Impact Assessment Circular", true, "IFCSITE"),

        // ══════════════════════════════════════════════════════════════════════
        // NEA - NATIONAL ENVIRONMENT AGENCY (8 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("NEA-VENT-001", SgAgency.NEA, CorenetGateway.Construction,
            "Natural ventilation - openable area ≥5% of floor area",
            "Habitable rooms must have natural ventilation with openable window area ≥5% of floor area (Singapore Code of Practice on Environmental Health). IfcWindow.OpeningArea / IfcSpace.GrossPlannedArea ≥ 0.05.",
            "NEA EPHA §3 / Building Control Regulations", true, "IFCWINDOW"),

        Sg("NEA-VENT-002", SgAgency.NEA, CorenetGateway.Construction,
            "Mechanical ventilation - fresh air supply rates (SS 553:2016)",
            "Air-conditioned spaces must receive fresh air per SS 553:2016: Office 10 L/s/person, Conference 10 L/s/person, Shop 10 L/s/person, Carpark 7.5 ACH (exhausted), Toilet 10 ACH (exhausted). IfcDuctSegment must declare FreshAirRate.",
            "SS 553:2016 (Air Conditioning and Mechanical Ventilation) - Appendix A", true, "IFCDUCTSEGMENT"),

        Sg("NEA-REFUSE-001", SgAgency.NEA, CorenetGateway.Construction,
            "Refuse - centralised refuse chute and chamber",
            "Residential buildings ≥5 storeys must have refuse chutes (one per ≤20 units). Refuse chambers: ≥4.5m² (residential), hose point, mechanical ventilation 6 ACH, floor drain. IfcSpace (REFUSE_CHAMBER) with SGPset_RefuseChamber declared.",
            "NEA Code of Practice on Environmental Health §2", true, "IFCSPACE"),

        Sg("NEA-DRAINAGE-001", SgAgency.NEA, CorenetGateway.Construction,
            "Stormwater - runoff coefficient and retention pond",
            "Developments >0.2 ha require drainage impact assessment. On-site detention tank or retention pond may be required. SGPset_StormwaterManagement.RunoffCoefficient and RetentionVolume (m³) must be declared on IfcSite.",
            "PUB Code of Practice on Surface Water Drainage §2 / NEA EPHA", true, "IFCSITE"),

        Sg("NEA-LIGHT-001", SgAgency.NEA, CorenetGateway.Construction,
            "Natural daylighting - ≥10% of floor area as window",
            "Habitable rooms must have natural daylighting with window area ≥10% of floor area. Window transmittance must not be reduced below 0.5 visible light transmittance (VLT). IfcWindow.SGPset_DaylightPerformance.VLT must be declared.",
            "NEA EPHA §2 / Building Control Regulations", true, "IFCWINDOW"),

        Sg("NEA-NOISE-001", SgAgency.NEA, CorenetGateway.Construction,
            "Noise - facade insulation for road/industrial noise",
            "Buildings within 150m of expressways or industrial areas must assess road traffic noise. Facade Rw ≥ 25dB for bedrooms if noise exceeds 67 dB(A) daytime. IfcWall.SGPset_AcousticPerformance.WeightedSoundInsulation declared.",
            "NEA Environmental Protection and Management Act / BCA Technical Guidance - Acoustics", true, "IFCWALL"),

        Sg("NEA-IEQ-001", SgAgency.NEA, CorenetGateway.Construction,
            "Indoor environmental quality - BCA Green Mark IEQ Prerequisite",
            "Buildings pursuing Green Mark must achieve IEQ credits: CO₂ monitoring in air-conditioned areas, VOC-limited finishes, formaldehyde testing, thermal comfort per ASHRAE 55. IfcSpace.SGPset_IndoorAirQuality.CO2MonitoringRequired = TRUE.",
            "BCA Green Mark 2021 - IEQ Prerequisites", true, "IFCSPACE"),

        Sg("NEA-VECTOR-001", SgAgency.NEA, CorenetGateway.Construction,
            "Vector control - no stagnant water features without preventive measures",
            "Water features, roof gardens, planter boxes, and flat roofs must have drainage or anti-mosquito larviciding provisions. IfcSite.SGPset_VectorControl.HasWaterFeatures and DrainageProvided must be declared.",
            "NEA Environmental Health Institute - Vector Control Guidelines", true, "IFCSITE"),

        // ══════════════════════════════════════════════════════════════════════
        // PUB - PUBLIC UTILITIES BOARD (8 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("PUB-DRAIN-001", SgAgency.PUB, CorenetGateway.Construction,
            "Platform level - minimum platform level above road/drain",
            "IfcSite.SGPset_SitePlatformLevel.MinimumPlatformLevel must meet PUB minimum platform level (generally road crown + 150mm or 300yr flood level). Critical infrastructure: additional 600mm above 1% AEP flood level.",
            "PUB Code of Practice on Surface Water Drainage §1 / PUB PLM Advisory", true, "IFCSITE"),

        Sg("PUB-DRAIN-002", SgAgency.PUB, CorenetGateway.Construction,
            "Surface water drainage - design storm return period",
            "Drainage must be designed for: public drain connection ≥10yr ARI, underground drain ≥50yr ARI, basement drain ≥50yr ARI. IfcSite.SGPset_StormwaterManagement.DesignARI declared. On-site detention ≥10% runoff reduction may be required.",
            "PUB Code of Practice on Surface Water Drainage 8th Edition §3", true, "IFCSITE"),

        Sg("PUB-WATER-001", SgAgency.PUB, CorenetGateway.Construction,
            "Water supply - compliance with PUB Water Supply Regulations",
            "IfcPipeSegment (WATER_SUPPLY) must declare SGPset_WaterSupply.PipeSize (DN), Material (CPVC/copper/SS316), PressureRating (bar), and ServicePressure. Maximum 3.5 bar at any outlet. Backflow prevention on all non-potable connections.",
            "PUB Water Supply (Waterworks) Regulations / SS 636 (Water Services)", true, "IFCPIPESEGMENT"),

        Sg("PUB-WATER-002", SgAgency.PUB, CorenetGateway.Construction,
            "Sanitary fitting provision - minimum numbers per NEA/PUB",
            "Toilet provision per PUB Code: Male: 1 WC per 75 males (office), 1 urinal per 50 males. Female: 1 WC per 30 females. Accessible: 1 accessible WC per toilet block. IfcSanitaryTerminal elements must be counted per IfcSpace.",
            "PUB Water Supply Regulations / NEA EPHA Schedule 3 (Sanitary Facilities)", true, "IFCSANITARYTERMINAL"),

        Sg("PUB-SEWER-001", SgAgency.PUB, CorenetGateway.Construction,
            "Sewerage - mandatory connection to public sewer",
            "All buildings with sanitary discharges must connect to public sewer. Private on-site treatment (septic tanks) not permitted in sewered areas. IfcSite.SGPset_Sewerage.ConnectedToPublicSewer = TRUE required.",
            "Sewerage and Drainage Act (Cap 294) / PUB SD Handbook", true, "IFCSITE"),

        Sg("PUB-RAIN-001", SgAgency.PUB, CorenetGateway.Construction,
            "Rainwater harvesting - NEWater/ABC Water features",
            "For buildings ≥5,000m² GFA, rainwater harvesting tanks are encouraged (not mandatory). ABC Waters Design Features may be required per PUB ABC Waters Masterplan. IfcSystem (RAINWATER_HARVESTING) with TankCapacity (m³) declared.",
            "PUB ABC Waters Programme / BCA Green Mark 2021 - Water Efficiency Credits", true, null),

        Sg("PUB-GREASE-001", SgAgency.PUB, CorenetGateway.Construction,
            "Grease trap - mandatory for food and beverage outlets",
            "All F&B outlets and food preparation areas must install grease traps: capacity ≥30 min detention time, accessible for maintenance. IfcDistributionElement (GREASE_TRAP) must declare CapacityLitres and MaintenanceAccess = TRUE.",
            "PUB Technical Note on Grease Traps / NEA EPHA", true, null),

        Sg("PUB-METER-001", SgAgency.PUB, CorenetGateway.Construction,
            "Water metering - sub-meters for large users",
            "Buildings with GFA >10,000m² must install sub-meters for major water users (cooling towers, irrigation, kitchen). IfcMeter (WATER) must declare SGPset_WaterMeter.MeterType, Location, and ConnectedGFA.",
            "PUB Water Efficiency Management Plan (WEMP) - Large Commercial Buildings", false, null),

        // ══════════════════════════════════════════════════════════════════════
        // NPARKS - NATIONAL PARKS BOARD (5 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("NPARKS-LUSH-001", SgAgency.NParks, CorenetGateway.Construction,
            "LUSH 3.0 - Landscaping for Urban Spaces and High-rises",
            "Greenery Replacement Area (GRA) must equal or exceed the site's greenery area lost to development. GRA can be on ground, rooftop (Green Mark §3.7), sky terraces, or planter ledges. SGPset_LandscapeArea.GreeneryAreaM2 on IfcSite.",
            "NParks LUSH 3.0 Programme / URA Development Control - Greenery", true, "IFCSITE"),

        Sg("NPARKS-TREE-001", SgAgency.NParks, CorenetGateway.Design,
            "Heritage tree - no works within root protection zone",
            "No earthworks, construction, or service routing permitted within 2.0m of gazetted heritage trees without NParks approval. IfcSite must declare SGPset_HeritageTree.HasHeritageTree and RootProtectionZoneRadius (m).",
            "Parks and Trees Act (Cap 216) §14 / NParks Heritage Trees", true, "IFCSITE"),

        Sg("NPARKS-RPTB-001", SgAgency.NParks, CorenetGateway.Construction,
            "Roadside trees - preservation and replanting bond",
            "Roadside trees within or adjacent to the development site that are felled require replanting bond and replacement within 6 months. SGPset_StreetTree.TreesToBeRetained declared on IfcSite landscaping elements.",
            "NParks Trees by Roadside Advisory / URA Green Corridor Requirements", true, "IFCSITE"),

        Sg("NPARKS-BIOD-001", SgAgency.NParks, CorenetGateway.Construction,
            "Biodiversity-sensitive design - bird-safe glazing",
            "Buildings near nature reserves must use bird-safe glazing on large glass facades (>10m² uninterrupted glazed area within 1km of nature reserve). IfcWindow.SGPset_BirdSafeGlazing.ComplianceRequired = TRUE.",
            "NParks Advisory on Bird-Friendly Building Design 2022", false, "IFCWINDOW"),

        Sg("NPARKS-ROOF-001", SgAgency.NParks, CorenetGateway.Construction,
            "Rooftop greenery - substrate depth and plant palette",
            "Rooftop gardens per LUSH: extensive green roof ≥75mm substrate, intensive ≥200mm. Native/naturalised plant species preferred. IfcCovering (roof) with SGPset_GreenRoof.SubstrateDepthMm and PlantPalette declared.",
            "NParks LUSH 3.0 / BCA Green Mark 2021 - Greenery Credits", false, "IFCCOVERING"),

        // ══════════════════════════════════════════════════════════════════════
        // SLA - SINGAPORE LAND AUTHORITY (5 rules)
        // ══════════════════════════════════════════════════════════════════════
        Sg("SLA-SVY-001", SgAgency.SLA, CorenetGateway.Design,
            "SVY21 coordinates - mandatory for all CORENET-X submissions",
            "IfcSite must include IfcMapConversion with SLA SVY21 coordinates (EPSG:3414). IfcGeographicElement.SGPset_Coordinates.Northing and Easting in metres on SVY21 datum. MapUnit = METRE, Scale = 1.0.",
            "SLA SVY21 Technical Reference Manual / IFC+SG COP3 §2.1", true, "IFCSITE"),

        Sg("SLA-BOUND-001", SgAgency.SLA, CorenetGateway.Design,
            "Survey boundary - lot boundary coordinates from SLA title deed",
            "Site lot boundaries must be drawn from SLA certified plan (CP) or lot boundary coordinates as endorsed by Licensed Surveyor. IfcSite.LandTitleNumber and SurveyPlanReference must be declared in SGPset_SiteInfo.",
            "Land Titles Act (Cap 157) / SLA Cadastral Survey Standards", true, "IFCSITE"),

        Sg("SLA-HEIGHT-001", SgAgency.SLA, CorenetGateway.Design,
            "Building height - height above Singapore Height Datum (SHD)",
            "All IfcBuildingStorey elevations must reference Singapore Height Datum (SHD). IfcBuildingStorey.SGPset_StoreyElevation.ElevationAboveSHD (metres above MSL) must be declared. Top of parapet/roof must be declared for URA height compliance.",
            "SLA Height Datum / URA Planning - Building Height Control", true, "IFCBUILDINGSTOREY"),

        Sg("SLA-STRATA-001", SgAgency.SLA, CorenetGateway.Design,
            "Strata title - strata lot areas and boundaries",
            "For strata-titled developments, each strata lot (typically each apartment unit) must have IfcSpace.LotNumber and GrossPlannedArea per the approved strata subdivision plan. Must match SLA Land Titles (Strata) Act requirements.",
            "Land Titles (Strata) Act (Cap 158) / SLA Strata Survey", true, "IFCSPACE"),

        Sg("SLA-ROAD-001", SgAgency.SLA, CorenetGateway.Design,
            "Road reserve - setback from road reserve line",
            "No permanent structures within LTA/SLA road reserve. IfcSite boundary must clearly identify road reserve boundary. SGPset_SiteInfo.RoadReservePlan from LTA Plan Number must be declared for all boundary roads.",
            "Street Works Act / LTA Road Reserve Plans", true, "IFCSITE"),

    
    };

    private static ExtSgRequirement Sg(
        string id, SgAgency agency, CorenetGateway gateway,
        string desc, string detail, string regRef,
        bool mandatory, string? ifcFilter) =>
        new()
        {
            Agency          = agency,
            Gateway         = gateway,
            Description     = desc,
            DetailText      = detail,
            RegReference    = regRef,
            IsMandatory     = mandatory,
            IfcClassFilter  = ifcFilter ?? string.Empty
        };

    private sealed class ExtSgRequirement : SgAgencyRequirement
    {
        public string DetailText    { get; set; } = string.Empty;
        public string IfcClassFilter { get; set; } = string.Empty;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  MALAYSIA - NBeS / UBBL 1984
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Malaysia NBeS rules covering all 9 UBBL Purpose Groups and all UBBL Parts.
/// Data sourced from UBBL 1984 (all amendments), MS 1184:2014, MS 1183:2007,
/// and JBPM Fire Safety Requirements 2020.
/// </summary>
public sealed class MyRules : IMyRules
{
    public List<UbblRequirement> GetUbblRequirements(
        string ifcClass, MalaysiaPurposeGroup purposeGroup)
    {
        return _ubblRequirements
            .Where(r => (r.PurposeGroup == purposeGroup ||
                         r.PurposeGroup == MalaysiaPurposeGroup.All) &&
                        (string.IsNullOrEmpty(r.IfcClassFilter) ||
                         r.IfcClassFilter.Equals(ifcClass, StringComparison.OrdinalIgnoreCase)))
            .Cast<UbblRequirement>().ToList();
    }

    public List<string> GetJbpmFireRequirements(string ifcClass)
    {
        return _jbpmFireReqs.TryGetValue(ifcClass.ToUpperInvariant(), out var list) ? list : new();
    }

    public UbblPart GetRelevantUbblPart(string ifcClass) =>
        _classPartMap.TryGetValue(ifcClass.ToUpperInvariant(), out var part)
            ? part : UbblPart.III;

    // ── IFC class → primary UBBL part ────────────────────────────────────────

    private static readonly Dictionary<string, UbblPart> _classPartMap =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFCSPACE"]        = UbblPart.III,
        ["IFCWALL"]         = UbblPart.VI,
        ["IFCSLAB"]         = UbblPart.VI,
        ["IFCBEAM"]         = UbblPart.V,
        ["IFCCOLUMN"]       = UbblPart.V,
        ["IFCFOOTING"]      = UbblPart.V,
        ["IFCPILE"]         = UbblPart.V,
        ["IFCDOOR"]         = UbblPart.III,
        ["IFCWINDOW"]       = UbblPart.III,
        ["IFCSTAIR"]        = UbblPart.VI,
        ["IFCSTAIRFLIGHT"]  = UbblPart.VI,
        ["IFCRAMP"]         = UbblPart.VI,
        ["IFCROOF"]         = UbblPart.VI,
    };

    // ── JBPM fire requirements by element ─────────────────────────────────────

    private static readonly Dictionary<string, List<string>> _jbpmFireReqs =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFCWALL"] = new()
        {
            "Compartment walls: FRR 60–240 min per UBBL Third Schedule by Purpose Group",
            "External walls < 1.0m from boundary: FRR 60 min minimum",
            "Separating walls between residential units: FRR 60 min",
            "Fire-rated wall construction must use certified materials per JBPM approved list"
        },
        ["IFCSLAB"] = new()
        {
            "Floor slabs bounding fire compartments: FRR per UBBL Third Schedule",
            "Roof slab over basement: FRR 90 min minimum",
            "Suspended ceiling systems forming fire compartment: certified assembly required"
        },
        ["IFCDOOR"] = new()
        {
            "Fire exit doors: minimum FD30S with approved self-closer",
            "Corridor fire doors: FD30S minimum",
            "Stair enclosure doors: FD60S minimum for buildings > 4 storeys",
            "All fire doors must be tested and certified to MS 1012 or equivalent"
        },
        ["IFCSTAIR"] = new()
        {
            "Protected stairways required in buildings > 2 storeys",
            "Minimum width 900mm (residential), 1,100mm (commercial/assembly)",
            "Maximum riser 175mm, minimum tread 255mm",
            "Stair enclosure walls: FRR 60 min minimum"
        },
        ["IFCCOLUMN"] = new()
        {
            "Structural columns: FRR per UBBL Third Schedule by Purpose Group",
            "Encased columns or intumescent coating required for FRR compliance",
            "Steel columns: fire protection to achieve required FRR"
        },
        ["IFCBEAM"] = new()
        {
            "Structural beams: FRR per UBBL Third Schedule by Purpose Group",
            "Exposed steel beams require intumescent coating or encasement",
            "Beams forming part of compartment floor: FRR ≥ floor slab rating"
        },
    };

    // ── Comprehensive UBBL requirements dataset ───────────────────────────────

    private static readonly List<ExtUbblRequirement> _ubblRequirements = new()
    {
        // ── UBBL PART III - Space, Light and Ventilation ──────────────────────

        My("MY-UBBL-III-001", UbblPart.III, MalaysiaPurposeGroup.All,
            "By-Law 47 - Minimum Ceiling Height: 2.6m for habitable rooms",
            "UBBL 1984 By-Law 47(1)", true, "IFCSPACE"),

        My("MY-UBBL-III-002", UbblPart.III, MalaysiaPurposeGroup.All,
            "By-Law 47 - Minimum Ceiling Height: 2.3m for bathrooms/stores/corridors",
            "UBBL 1984 By-Law 47(2)", true, "IFCSPACE"),

        My("MY-UBBL-III-003", UbblPart.III, MalaysiaPurposeGroup.All,
            "By-Law 48 - Minimum Floor Area: bedroom ≥ 6.5m², habitable room ≥ 11.0m²",
            "UBBL 1984 By-Law 48(1)", true, "IFCSPACE"),

        My("MY-UBBL-III-004", UbblPart.III, MalaysiaPurposeGroup.All,
            "By-Law 38 - Natural Lighting: window area ≥ 10% of floor area for habitable rooms",
            "UBBL 1984 By-Law 38", true, "IFCWINDOW"),

        My("MY-UBBL-III-005", UbblPart.III, MalaysiaPurposeGroup.All,
            "By-Law 39 - Natural Ventilation: openable window area ≥ 5% of floor area",
            "UBBL 1984 By-Law 39", true, "IFCWINDOW"),

        My("MY-UBBL-III-006", UbblPart.III, MalaysiaPurposeGroup.All,
            "By-Law 40 - External Walls with No Windows: mechanical ventilation required",
            "UBBL 1984 By-Law 40", false, null),

        My("MY-UBBL-III-007", UbblPart.III, MalaysiaPurposeGroup.All,
            "By-Law 55 - Width of Corridors: minimum 1.5m clear width for escape corridors",
            "UBBL 1984 By-Law 55", true, null),

        // ── UBBL PART V - Structural Requirements ─────────────────────────────

        My("MY-UBBL-V-001", UbblPart.V, MalaysiaPurposeGroup.All,
            "By-Law 95 - Structural Design: all structural elements designed by registered PE",
            "UBBL 1984 By-Law 95 / Registration of Engineers Act 1967",
            true, null),

        My("MY-UBBL-V-002", UbblPart.V, MalaysiaPurposeGroup.All,
            "By-Law 96 - Loading: dead, live, wind and other loads per MS 1553 or EC1",
            "UBBL 1984 By-Law 96 / MS 1553 / SS EN 1991-1-1",
            true, null),

        My("MY-UBBL-V-003", UbblPart.V, MalaysiaPurposeGroup.All,
            "By-Law 101 - Foundation: suitable for soil type; piling requires PE approval",
            "UBBL 1984 By-Law 101",
            true, "IFCFOOTING"),

        // ── UBBL PART VI - Constructional Requirements ────────────────────────

        My("MY-UBBL-VI-001", UbblPart.VI, MalaysiaPurposeGroup.All,
            "By-Law 112 - Staircase Construction: max riser 175mm, min tread 255mm",
            "UBBL 1984 By-Law 112", true, "IFCSTAIRFLIGHT"),

        My("MY-UBBL-VI-002", UbblPart.VI, MalaysiaPurposeGroup.All,
            "By-Law 113 - Stair Width: minimum 900mm clear for private staircases; 1,100mm for shared",
            "UBBL 1984 By-Law 113", true, "IFCSTAIR"),

        My("MY-UBBL-VI-003", UbblPart.VI, MalaysiaPurposeGroup.All,
            "By-Law 117 - Roof: weatherproof construction; adequate drainage gradient",
            "UBBL 1984 By-Law 117", true, "IFCROOF"),

        My("MY-UBBL-VI-004", UbblPart.VI, MalaysiaPurposeGroup.All,
            "By-Law 119 - External Walls: min 100mm masonry or equivalent structural material",
            "UBBL 1984 By-Law 119", false, "IFCWALL"),

        My("MY-UBBL-VI-005", UbblPart.VI, MalaysiaPurposeGroup.All,
            "By-Law 120 - Party Walls: fire separation between buildings",
            "UBBL 1984 By-Law 120", true, "IFCWALL"),

        // ── UBBL PART VII - Fire Requirements ─────────────────────────────────

        My("MY-UBBL-VII-001", UbblPart.VII, MalaysiaPurposeGroup.All,
            "By-Law 121 - Fire Resistance: elements of structure to achieve minimum FRR per Third Schedule",
            "UBBL 1984 By-Law 121 / Third Schedule", true, null),

        My("MY-UBBL-VII-002", UbblPart.VII, MalaysiaPurposeGroup.All,
            "By-Law 122 - Compartmentation: maximum compartment floor area per Third Schedule",
            "UBBL 1984 By-Law 122 / Third Schedule", true, "IFCSPACE"),

        My("MY-UBBL-VII-003", UbblPart.VII, MalaysiaPurposeGroup.All,
            "By-Law 125 - Means of Escape: minimum two separate exits from each floor",
            "UBBL 1984 By-Law 125", true, null),

        My("MY-UBBL-VII-004", UbblPart.VII, MalaysiaPurposeGroup.All,
            "By-Law 126 - Exit Doors: minimum 900mm clear width; outward opening from rooms",
            "UBBL 1984 By-Law 126", true, "IFCDOOR"),

        My("MY-UBBL-VII-005", UbblPart.VII, MalaysiaPurposeGroup.All,
            "By-Law 127 - Escape Routes: travel distance to exit ≤ 30m (non-sprinklered)",
            "UBBL 1984 By-Law 127", true, null),

        My("MY-UBBL-VII-006", UbblPart.VII, MalaysiaPurposeGroup.All,
            "By-Law 133 - Fire Door: minimum FD30 at protected compartment boundaries",
            "UBBL 1984 By-Law 133 / MS 1012", true, "IFCDOOR"),

        My("MY-UBBL-VII-007", UbblPart.VII, MalaysiaPurposeGroup.All,
            "By-Law 137 - Smoke Control: smoke-stop lobbies and pressurisation for high-rise (>12 storeys)",
            "UBBL 1984 By-Law 137", false, null),

        My("MY-UBBL-VII-008", UbblPart.VII, MalaysiaPurposeGroup.PurposeGroupVII,
            "By-Law 140 - Places of Assembly: sprinkler system required throughout",
            "UBBL 1984 By-Law 140", true, null),

        // ── UBBL PART VIII - Fire Alarms and Extinguishing ────────────────────

        My("MY-UBBL-VIII-001", UbblPart.VIII, MalaysiaPurposeGroup.All,
            "By-Law 156 - Fire Detection: automatic detection required in buildings > 4 storeys",
            "UBBL 1984 By-Law 156 / MS 1745", true, null),

        My("MY-UBBL-VIII-002", UbblPart.VIII, MalaysiaPurposeGroup.All,
            "By-Law 158 - Fire Hose Reels: within 30m of every part of each floor",
            "UBBL 1984 By-Law 158 / MS 1210", true, null),

        My("MY-UBBL-VIII-003", UbblPart.VIII, MalaysiaPurposeGroup.All,
            "By-Law 163 - Sprinkler Systems: required for buildings > 6 storeys or > 2,000m² GFA",
            "UBBL 1984 By-Law 163 / MS 1489", false, null),

        // ── UBBL PART IX - Special Requirements ──────────────────────────────

        My("MY-UBBL-IX-001", UbblPart.IX, MalaysiaPurposeGroup.All,
            "By-Law 180 - Disabled Persons: access facilities per MS 1184:2014",
            "UBBL 1984 By-Law 180 / MS 1184:2014", true, null),

        My("MY-UBBL-IX-002", UbblPart.IX, MalaysiaPurposeGroup.All,
            "MS 1184:2014 - Accessible Route: continuous level path from car park to facilities",
            "MS 1184:2014 Code of Practice for Access for Disabled People", true, null),

        My("MY-UBBL-IX-003", UbblPart.IX, MalaysiaPurposeGroup.All,
            "MS 1184:2014 §5.3 - Accessible Door: min 800mm clear, level threshold (±6mm)",
            "MS 1184:2014 §5.3", true, "IFCDOOR"),

        My("MY-UBBL-IX-004", UbblPart.IX, MalaysiaPurposeGroup.All,
            "MS 1184:2014 §5.2 - Accessible Ramp: max gradient 1:12 with handrails both sides",
            "MS 1184:2014 §5.2 / UBBL By-Law 180", true, "IFCRAMP"),
    };

    private static ExtUbblRequirement My(
        string id, UbblPart part, MalaysiaPurposeGroup pg,
        string desc, string bylaw, bool mandatory, string? ifcFilter) =>
        new()
        {
            Part            = part,
            ByLaw           = bylaw,
            Description     = desc,
            PurposeGroup    = pg,
            IsMandatory     = mandatory,
            IfcClassFilter  = ifcFilter ?? string.Empty
        };

    private sealed class ExtUbblRequirement : UbblRequirement
    {
        public string IfcClassFilter { get; set; } = string.Empty;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  GEOMETRY CHECKER
// ─────────────────────────────────────────────────────────────────────────────

public sealed class BasicGeometryChecker : IGeometryChecker
{
    private const double Tol = 0.001; // 1mm

    public bool IsGeometryValid(IfcElement element)
    {
        if (element.BoundingBox == null) return true;
        return GetGeometryIssues(element).Count == 0;
    }

    public List<string> GetGeometryIssues(IfcElement element)
    {
        var issues = new List<string>();
        var b = element.BoundingBox;
        if (b == null) return issues;

        double dx = b.MaxX - b.MinX, dy = b.MaxY - b.MinY, dz = b.MaxZ - b.MinZ;
        if (dx <= Tol) issues.Add($"Zero X extent ({dx:F4} m) - element has no width");
        if (dy <= Tol) issues.Add($"Zero Y extent ({dy:F4} m) - element has no depth");
        if (dz <= Tol) issues.Add($"Zero Z extent ({dz:F4} m) - element has no height");
        if (double.IsNaN(dx) || double.IsNaN(dy) || double.IsNaN(dz))
            issues.Add("NaN bounding box coordinates - geometry is corrupt");
        if (double.IsInfinity(dx) || double.IsInfinity(dy) || double.IsInfinity(dz))
            issues.Add("Infinite bounding box - geometry extends to infinity");
        return issues;
    }
}
