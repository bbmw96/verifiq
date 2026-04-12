// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// ─── COMPREHENSIVE RULES DATABASE ────────────────────────────────────────────
//
// Complete IFC+SG and NBeS parameter mappings for Singapore and Malaysia.
//
// SINGAPORE - IFC+SG CORENET-X (COP 3rd Edition, October 2025)
//   Agencies : BCA, URA, LTA, NEA, NParks, PUB, SCDF, SLA
//   Gateways : Design, Piling, Construction, Completion, DirectSubmission (DSP)
//   Standards: IFC+SG Industry Mapping 2025, IFC4 Reference View ADD2 TC1
//   CRS      : SVY21 (EPSG:3414) - mandatory
//   Legislation: Building Control Act, Planning Act, Fire Safety Act,
//                Environmental Public Health Act, Code on Accessibility 2025,
//                BCA Green Mark 2021
//
// MALAYSIA - NBeS (National BIM e-Submission, CIDB 2024)
//   Agencies  : JBPM (Fire), JKR (Public Works), CIDB, Local Authority (PBT)
//   Standards : UBBL 1984 (all 9 Parts), MS 1184:2014, MS 1183:2007,
//               JBPM Fire Safety Requirements 2020, Uniform Building By-Laws
//   Purpose Grps: All 9 (I–IX per UBBL Third Schedule / MS 1183)
//   CRS       : GDM2000 (per-state projection) - recommended
//   Legislation: Street, Drainage and Building Act 1974 (Act 133),
//                Uniform Building By-Laws 1984, Fire Services Act 1988
//
// ─────────────────────────────────────────────────────────────────────────────

using Microsoft.Data.Sqlite;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Rules.Common;

public sealed class SqliteRulesDatabase : IRulesDatabase, IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public SqliteRulesDatabase(string dbPath) { _dbPath = dbPath; }

    public void Initialise()
    {
        if (!File.Exists(_dbPath)) CreateAndSeedDatabase();
        _connection = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
        _connection.Open();
    }

    // ─── IRulesDatabase ───────────────────────────────────────────────────────

    public bool RequiresClassification(string ifcClass, CountryMode mode)
    {
        // Proxy and generic base classes do not carry classification themselves
        var exempt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IFCBUILDINGELEMENTPROXY","IFCBUILDINGELEMENT","IFCDISTRIBUTIONELEMENT",
            "IFCFURNISHINGELEMENT","IFCVIRTUALELEMENT"
        };
        return !exempt.Contains(ifcClass);
    }

    public bool RequiresMaterial(string ifcClass, CountryMode mode)
    {
        // Structural and fire-rated elements require material specification
        var req = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "IFCWALL","IFCWALLSTANDARDCASE","IFCSLAB","IFCBEAM","IFCCOLUMN",
            "IFCFOOTING","IFCPILE","IFCPLATE","IFCMEMBER","IFCROOF",
            "IFCSTAIRFLIGHT","IFCRAMPFLIGHT","IFCBUILDINGELEMENTPART",
            "IFCCOVERING","IFCCURTAINWALL"
        };
        return req.Contains(ifcClass);
    }

    public string GetRequiredClassificationSystem(string ifcClass, CountryMode mode) =>
        mode switch
        {
            CountryMode.Singapore => "IFC+SG Classification System (BCA/GovTech) - CORENET-X COP 3rd Ed.",
            CountryMode.Malaysia  => "NBeS Classification System (CIDB/JBPM) - NBeS IFC Mapping 2024",
            _                     => "IFC+SG / NBeS Classification System"
        };

    public string GetCurrentClassificationEdition(CountryMode mode) =>
        mode switch
        {
            CountryMode.Singapore => "IFC+SG Industry Mapping 2025 (COP 3rd Edition, October 2025)",
            CountryMode.Malaysia  => "NBeS IFC Mapping 2024 (CIDB - 2nd Edition)",
            _                     => string.Empty
        };

    public List<PropertySetRequirement> GetRequiredPropertySets(string ifcClass, CountryMode mode)
    {
        var cls = ifcClass.ToUpperInvariant();
        return _psetRequirements.TryGetValue(cls, out var list)
            ? list.Where(p => p.Country == mode || mode == CountryMode.Combined
                          || p.Country == CountryMode.Combined).ToList()
            : new();
    }

    public List<PropertyRequirement> GetRequiredProperties(string ifcClass, CountryMode mode,
        CorenetGateway? gateway, MalaysiaPurposeGroup? purposeGroup)
    {
        return _propertyRequirements
            .Where(p =>
                p.AppliesTo(ifcClass) &&
                (p.Country == mode || mode == CountryMode.Combined || p.Country == CountryMode.Combined) &&
                (gateway == null || p.Gateway == gateway ||
                 p.Gateway == CorenetGateway.Design ||
                 (int)p.Gateway <= (int)(gateway ?? CorenetGateway.Construction)) &&
                (purposeGroup == null || p.PurposeGroup == purposeGroup ||
                 p.PurposeGroup == MalaysiaPurposeGroup.All || p.PurposeGroup == MalaysiaPurposeGroup.None))
            .Cast<PropertyRequirement>().ToList();
    }

    public List<PropertyTypeRule> GetPropertyTypeRules(string ifcClass, CountryMode mode) =>
        _typeRules.Where(r => (r.Country == mode || mode == CountryMode.Combined)).ToList();

    public List<EnumerationRule> GetEnumerationRules(string ifcClass, CountryMode mode) =>
        _enumRules.Where(r => (r.Country == mode || mode == CountryMode.Combined)).ToList();

    public string  GetRulesDbVersion(CountryMode mode) => mode switch
    {
        CountryMode.Singapore => "IFC+SG 2025.1 (COP3, Oct 2025)",
        CountryMode.Malaysia  => "NBeS 2024.1 (CIDB, 2nd Ed.)",
        _ => "2025.1"
    };

    public DateTime GetRulesDbLastUpdated(CountryMode mode) => new(2025, 10, 1);

    // ─── PROPERTY SET REQUIREMENTS - all IFC classes ──────────────────────────
    // Format: [IfcClass] → list of required property sets per country

    private static readonly Dictionary<string, List<PropertySetRequirement>> _psetRequirements =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // ── ARCHITECTURAL STRUCTURE ───────────────────────────────────────────

        ["IFCWALL"] = Psets(
            P("Pset_WallCommon",        true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_WallFireRating",  true,  true,  SgAgency.SCDF, SG,   "IFC+SG 2025 - SCDF Fire Code 2018"),
            P("SGPset_WallAcoustic",    false, true,  SgAgency.BCA,  SG,   "IFC+SG 2025 - BCA Green Mark"),
            P("SGPset_WallThermal",     false, true,  SgAgency.BCA,  SG,   "IFC+SG 2025 - BCA Green Mark 2021 §3.4")
        ),
        ["IFCWALLSTANDARDCASE"] = Psets(
            P("Pset_WallCommon",        true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_WallFireRating",  true,  true,  SgAgency.SCDF, SG,   "IFC+SG 2025"),
            P("SGPset_WallAcoustic",    false, true,  SgAgency.BCA,  SG,   "IFC+SG 2025")
        ),
        ["IFCSLAB"] = Psets(
            P("Pset_SlabCommon",        true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_SlabFireRating",  true,  true,  SgAgency.SCDF, SG,   "IFC+SG 2025 - SCDF Fire Code"),
            P("SGPset_SlabStructural",  true,  true,  SgAgency.BCA,  SG,   "IFC+SG 2025 - BCA BC Regs")
        ),
        ["IFCDOOR"] = Psets(
            P("Pset_DoorCommon",            true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_DoorFireDoor",        false, true,  SgAgency.SCDF, SG,   "IFC+SG 2025 - SCDF Fire Code"),
            P("SGPset_DoorAccessibility",   true,  true,  SgAgency.BCA,  SG,   "Code on Accessibility 2025"),
            P("SGPset_DoorSecurity",        false, true,  SgAgency.BCA,  SG,   "IFC+SG 2025")
        ),
        ["IFCWINDOW"] = Psets(
            P("Pset_WindowCommon",          true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_WindowThermal",       false, true,  SgAgency.BCA,  SG,   "BCA Green Mark 2021"),
            P("SGPset_WindowFireRating",    false, true,  SgAgency.SCDF, SG,   "SCDF Fire Code 2018")
        ),
        ["IFCROOF"] = Psets(
            P("Pset_RoofCommon",            true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_RoofThermal",         false, true,  SgAgency.BCA,  SG,   "BCA Green Mark 2021 §3.4 Roof U-value"),
            P("SGPset_RoofFireRating",      true,  true,  SgAgency.SCDF, SG,   "SCDF - Roof fire compartmentation")
        ),
        ["IFCBEAM"] = Psets(
            P("Pset_BeamCommon",            true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_BeamStructural",      true,  true,  SgAgency.BCA,  SG,   "IFC+SG 2025 - BCA Structural")
        ),
        ["IFCCOLUMN"] = Psets(
            P("Pset_ColumnCommon",          true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_ColumnStructural",    true,  true,  SgAgency.BCA,  SG,   "IFC+SG 2025 - BCA Structural")
        ),
        ["IFCFOOTING"] = Psets(
            P("Pset_FootingCommon",         true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_FootingFoundation",   true,  true,  SgAgency.BCA,  SG,   "IFC+SG 2025 - BCA Foundation")
        ),
        ["IFCPILE"] = Psets(
            P("Pset_PileCommon",            true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_PileFoundation",      true,  true,  SgAgency.BCA,  SG,   "IFC+SG 2025 - BCA Piling Gateway")
        ),
        ["IFCSTAIR"] = Psets(
            P("Pset_StairCommon",           true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_StairFireEscape",     false, true,  SgAgency.SCDF, SG,   "IFC+SG 2025 - SCDF Fire Escape"),
            P("SGPset_StairAccessibility",  true,  true,  SgAgency.BCA,  SG,   "Code on Accessibility 2025")
        ),
        ["IFCSTAIRFLIGHT"] = Psets(
            P("Pset_StairFlightCommon",     true,  false, SgAgency.BCA,  Both, "IFC4 - Riser/Tread dimensions"),
            P("SGPset_StairFlightSafety",   false, true,  SgAgency.SCDF, SG,   "SCDF Fire Code - stair dimension")
        ),
        ["IFCRAMP"] = Psets(
            P("Pset_RampCommon",            true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_RampAccessibility",   true,  true,  SgAgency.BCA,  SG,   "Code on Accessibility 2025 §4.3")
        ),
        ["IFCRAMPFLIGHT"] = Psets(
            P("Pset_RampFlightCommon",      true,  false, SgAgency.BCA,  Both, "IFC4"),
            P("SGPset_RampFlightAccess",    true,  true,  SgAgency.BCA,  SG,   "Code on Accessibility 2025")
        ),
        ["IFCSPACE"] = Psets(
            P("Pset_SpaceCommon",           true,  false, SgAgency.URA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_SpaceGFA",            true,  true,  SgAgency.URA,  SG,   "IFC+SG 2025 - URA GFA Rules"),
            P("SGPset_SpaceFireCompartment",false, true,  SgAgency.SCDF, SG,   "SCDF Fire Code - compartment volumes"),
            P("SGPset_SpaceVentilation",    false, true,  SgAgency.NEA,  SG,   "NEA EPHA - ventilation requirements")
        ),
        ["IFCZONE"] = Psets(
            P("Pset_ZoneCommon",            true,  false, SgAgency.BCA,  Both, "IFC4"),
            P("SGPset_ZoneFireCompartment", false, true,  SgAgency.SCDF, SG,   "SCDF Fire Code - zones")
        ),
        ["IFCCOVERING"] = Psets(
            P("Pset_CoveringCommon",        true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG")
        ),
        ["IFCCURTAINWALL"] = Psets(
            P("Pset_CurtainWallCommon",     true,  false, SgAgency.BCA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_CurtainWallThermal",  false, true,  SgAgency.BCA,  SG,   "BCA Green Mark 2021 - façade"),
            P("SGPset_CurtainWallFire",     true,  true,  SgAgency.SCDF, SG,   "SCDF Fire Code - external wall")
        ),
        ["IFCPLATE"] = Psets(
            P("Pset_PlateCommon",           true,  false, SgAgency.BCA,  Both, "IFC4"),
            P("SGPset_PlateStructural",     false, true,  SgAgency.BCA,  SG,   "IFC+SG 2025")
        ),
        ["IFCMEMBER"] = Psets(
            P("Pset_MemberCommon",          true,  false, SgAgency.BCA,  Both, "IFC4"),
            P("SGPset_MemberStructural",    false, true,  SgAgency.BCA,  SG,   "IFC+SG 2025")
        ),
        ["IFCRAILING"] = Psets(
            P("Pset_RailingCommon",         true,  false, SgAgency.BCA,  Both, "IFC4"),
            P("SGPset_RailingAccessibility",true,  true,  SgAgency.BCA,  SG,   "Code on Accessibility 2025 - handrails")
        ),
        ["IFCFURNISHINGELEMENT"] = Psets(
            P("Pset_FurnitureTypeCommon",   false, false, SgAgency.None, Both, "IFC4")
        ),
        ["IFCFURNITURE"] = Psets(
            P("Pset_FurnitureTypeCommon",   false, false, SgAgency.None, Both, "IFC4")
        ),

        // ── M&E ELEMENTS ─────────────────────────────────────────────────────

        ["IFCSANITARYTERMINAL"] = Psets(
            P("Pset_SanitaryTerminalTypeCommon", true, false, SgAgency.PUB, Both, "PUB SDWA / IFC+SG"),
            P("SGPset_SanitaryTerminalPUB",      true, true,  SgAgency.PUB, SG,   "IFC+SG 2025 - PUB fixture requirements")
        ),
        ["IFCFLOWTERMINAL"] = Psets(
            P("Pset_FlowTerminalTypeAirTerminal", false, false, SgAgency.NEA, Both, "IFC4"),
            P("SGPset_FlowTerminalNEA",           false, true,  SgAgency.NEA, SG,   "NEA ventilation - IFC+SG 2025")
        ),
        ["IFCAIRTERMINAL"] = Psets(
            P("Pset_AirTerminalTypeCommon",  true,  false, SgAgency.NEA,  Both, "IFC4 / IFC+SG"),
            P("SGPset_AirTerminalNEA",       false, true,  SgAgency.NEA,  SG,   "NEA EPHA - mechanical ventilation")
        ),
        ["IFCDUCTSEGMENT"] = Psets(
            P("Pset_DuctSegmentTypeCommon",  true,  false, SgAgency.NEA,  Both, "IFC4"),
            P("SGPset_DuctFireDamper",       false, true,  SgAgency.SCDF, SG,   "SCDF - duct fire damper requirement")
        ),
        ["IFCDUCTFITTING"] = Psets(
            P("Pset_DuctFittingTypeCommon",  true,  false, SgAgency.NEA,  Both, "IFC4")
        ),
        ["IFCPIPESEGMENT"] = Psets(
            P("Pset_PipeSegmentTypeCommon",  true,  false, SgAgency.PUB,  Both, "PUB SDWA / IFC4"),
            P("SGPset_PipeSprinkler",        false, true,  SgAgency.SCDF, SG,   "SCDF - sprinkler system")
        ),
        ["IFCPIPEFITTING"] = Psets(
            P("Pset_PipeFittingTypeCommon",  true,  false, SgAgency.PUB,  Both, "IFC4")
        ),
        ["IFCFLOWCONTROLLER"] = Psets(
            P("Pset_FlowControllerTypeCommon",true, false, SgAgency.None, Both, "IFC4")
        ),
        ["IFCFLOWSTORAGEDEVICE"] = Psets(
            P("Pset_FlowStorageDeviceTypeCommon",true, false, SgAgency.PUB, Both, "IFC4 / PUB")
        ),
        ["IFCFLOWMOVINGDEVICE"] = Psets(
            P("Pset_FlowMovingDeviceTypeCommon", true, false, SgAgency.NEA, Both, "IFC4")
        ),
        ["IFCELECTRICAPPLIANCE"] = Psets(
            P("Pset_ElectricApplianceTypeCommon", false, false, SgAgency.None, Both, "IFC4")
        ),
        ["IFCLIGHTFIXTURE"] = Psets(
            P("Pset_LightFixtureTypeCommon", true,  false, SgAgency.BCA, Both, "IFC4"),
            P("SGPset_LightFixtureGreenMark",false, true,  SgAgency.BCA, SG,   "BCA Green Mark 2021 - lighting LPD")
        ),
        ["IFCDISTRIBUTIONCONTROLELEMENT"] = Psets(
            P("Pset_DistributionControlElementCommon", false, false, SgAgency.None, Both, "IFC4")
        ),

        // ── CIVIL / INFRASTRUCTURE ────────────────────────────────────────────

        ["IFCOPENINGELEMENT"] = Psets(
            P("Pset_OpeningElementCommon",   true,  false, SgAgency.BCA,  Both, "IFC4"),
            P("SGPset_OpeningFireDamper",    false, true,  SgAgency.SCDF, SG,   "SCDF - opening fire protection")
        ),
        ["IFCBUILDINGELEMENTPROXY"] = Psets(
            // Proxy elements get no property set requirements - they are already flagged at Level 1.
            // Any property sets present are noted but not mandated.
        ),
    };

    // ─── COMPREHENSIVE PROPERTY REQUIREMENTS ──────────────────────────────────
    // All 5 gateways × 8 agencies × all IFC classes × both countries

    private static readonly List<ExtendedPropertyRequirement> _propertyRequirements = BuildAllPropertyRequirements();

    private static List<ExtendedPropertyRequirement> BuildAllPropertyRequirements()
    {
        var R = new List<ExtendedPropertyRequirement>();

        // ╔══════════════════════════════════════════════════════════════════════╗
        // ║  SINGAPORE - IFC+SG CORENET-X (all 5 gateways, all 8 agencies)     ║
        // ╚══════════════════════════════════════════════════════════════════════╝

        // ── IFCWALL ───────────────────────────────────────────────────────────

        R.Add(Req("IFCWALL","Pset_WallCommon","IsExternal",          true,  SgAgency.BCA,  GDes, PGAll, SG,
            "TRUE for external walls, FALSE for internal",
            "Set IsExternal correctly. External walls trigger thermal, acoustic and fire-rating checks.",
            "IFC4 / IFC+SG Industry Mapping - Pset_WallCommon.IsExternal"));

        R.Add(Req("IFCWALL","Pset_WallCommon","LoadBearing",         true,  SgAgency.BCA,  GCon, PGAll, SG,
            "TRUE for structural (load-bearing) walls, FALSE for non-structural",
            "Required by BCA for Building Plan structural review. C&S engineer to confirm.",
            "IFC4 / BCA Building Control Regs - Structural Plan submission"));

        R.Add(Req("IFCWALL","Pset_WallCommon","FireRating",          true,  SgAgency.SCDF, GCon, PGAll, SG,
            "Fire resistance period e.g. '60/60/60' or 'FRR60' (R/E/I minutes). '0' or 'N/A' if not fire-rated.",
            "Required by SCDF. Use REI notation per SS EN 1363 or local SCDF notation. " +
            "All compartment walls and staircase enclosures require minimum FRR 60–120.",
            "SCDF Fire Code 2018 §4.3 / IFC+SG SGPset_WallFireRating"));

        R.Add(Req("IFCWALL","Pset_WallCommon","AcousticRating",      false, SgAgency.BCA,  GCon, PGAll, SG,
            "STC (Sound Transmission Class) value e.g. 'STC 45'. Use '0' if not applicable.",
            "Required for party walls, walls between different occupancies, and hotel rooms.",
            "SS 553 / BCA Acoustic Guidelines / IFC+SG"));

        R.Add(Req("IFCWALL","Pset_WallCommon","ThermalTransmittance", false, SgAgency.BCA, GCon, PGAll, SG,
            "U-value in W/(m²·K) e.g. 2.5",
            "Set U-value for external walls. Required for BCA Green Mark assessment.",
            "BCA Green Mark 2021 §3.4 - Envelope Thermal Transfer Value (ETTV)"));

        // ── IFCSLAB ───────────────────────────────────────────────────────────

        R.Add(Req("IFCSLAB","Pset_SlabCommon","IsExternal",          true,  SgAgency.BCA,  GDes, PGAll, SG,
            "TRUE for roof slabs exposed to sky, FALSE for all other slabs",
            "Roof slabs trigger thermal transmittance and waterproofing requirements.",
            "IFC4 / IFC+SG Industry Mapping"));

        R.Add(Req("IFCSLAB","Pset_SlabCommon","LoadBearing",         true,  SgAgency.BCA,  GCon, PGAll, SG,
            "TRUE or FALSE",
            "Required by BCA structural plan submission.",
            "BCA Building Control Act - Structural Check"));

        R.Add(Req("IFCSLAB","Pset_SlabCommon","FireRating",          true,  SgAgency.SCDF, GCon, PGAll, SG,
            "Fire resistance period e.g. '90/90/90'. Required for all floor/ceiling slabs in fire compartments.",
            "SCDF requires fire rating on all slabs bounding fire compartments.",
            "SCDF Fire Code 2018 §4.3 - Compartmentation"));

        // ── IFCDOOR ───────────────────────────────────────────────────────────

        R.Add(Req("IFCDOOR","Pset_DoorCommon","IsExternal",          true,  SgAgency.BCA,  GDes, PGAll, SG,
            "TRUE for external doors, FALSE for internal",
            "External doors trigger weather-tightness and security requirements.",
            "IFC4 / IFC+SG"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","FireExit",            false, SgAgency.SCDF, GCon, PGAll, SG,
            "TRUE if this door forms part of a designated fire escape route",
            "All fire exit doors must be identified. SCDF reviews means of escape routes.",
            "SCDF Fire Code 2018 §5.2 - Means of Escape"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","HandicapAccessible",  true,  SgAgency.BCA,  GCon, PGAll, SG,
            "TRUE for accessible doors (min 900mm clear opening, level threshold)",
            "Mandatory for all accessible route doors per Code on Accessibility 2025.",
            "BCA Code on Accessibility 2025 §4.2.1"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","FireRating",          false, SgAgency.SCDF, GCon, PGAll, SG,
            "Fire door rating e.g. 'FD30S', 'FD60S', 'FD120'. 'N/A' if not fire-rated.",
            "Fire doors must have a certified fire resistance rating.",
            "SCDF Fire Code 2018 §4.4 / SS 332"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","SmokeStop",           false, SgAgency.SCDF, GCon, PGAll, SG,
            "TRUE if this door is also a smoke-stop door",
            "Smoke-stop doors required at protected corridors and escape stairways.",
            "SCDF Fire Code 2018 §4.6 - Smoke Control"));

        // ── IFCWINDOW ─────────────────────────────────────────────────────────

        R.Add(Req("IFCWINDOW","Pset_WindowCommon","IsExternal",       true, SgAgency.BCA, GDes, PGAll, SG,
            "TRUE for external windows, FALSE for internal glazed screens",
            "Required to distinguish external envelope elements for ETTV calculation.",
            "IFC4 / BCA Green Mark 2021 §3.4"));

        R.Add(Req("IFCWINDOW","Pset_WindowCommon","FireRating",       false, SgAgency.SCDF, GCon, PGAll, SG,
            "Fire-rated glazing rating e.g. 'E30', 'EW30'. 'N/A' if not fire-rated.",
            "Fire-rated glazing in compartment walls requires certified rating.",
            "SCDF Fire Code 2018 §4.4 - Fire-Rated Glazing"));

        R.Add(Req("IFCWINDOW","Pset_WindowCommon","ThermalTransmittance", false, SgAgency.BCA, GCon, PGAll, SG,
            "U-value in W/(m²·K) for the window assembly including frame.",
            "Required for ETTV calculation under BCA Green Mark 2021.",
            "BCA Green Mark 2021 §3.4 - Window-to-Wall Ratio & U-values"));

        // ── IFCSPACE (URA, SCDF, NEA) ─────────────────────────────────────────

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","IsExternal",         true, SgAgency.URA, GDes, PGAll, SG,
            "TRUE for external spaces (void decks, covered walkways), FALSE for internal rooms",
            "URA uses this to determine GFA inclusion or exclusion.",
            "URA GFA Rules - IFC+SG Industry Mapping"));

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","Category",           true, SgAgency.URA, GDes, PGAll, SG,
            "Space use category e.g. RESIDENTIAL, OFFICE, CARPARK, PLANT_ROOM, BALCONY",
            "URA requires space category for GFA computation and planning compliance review. " +
            "Use only values from the permitted enumeration list.",
            "URA Planning Parameters 2023 / IFC+SG SGPset_SpaceGFA"));

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","GrossPlannedArea",   true, SgAgency.URA, GDes, PGAll, SG,
            "Gross floor area of the space in m² (decimal)",
            "URA computes GFA from these values. The sum must tally with the GFA declared on the Form BCA-A.",
            "URA GFA Rules - Handbook on Singapore Planning Parameters 2023"));

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","NetPlannedArea",     false, SgAgency.URA, GCon, PGAll, SG,
            "Net usable area excluding structural elements, in m²",
            "Required for accurate NLA reporting in certain development types.",
            "URA Planning Parameters 2023 §5 - Net Lettable Area"));

        // ── IFCSTAIRFLIGHT - tread/riser dimensions ───────────────────────────

        R.Add(Req("IFCSTAIRFLIGHT","Pset_StairFlightCommon","RiserHeight", true, SgAgency.BCA, GCon, PGAll, SG,
            "Riser height in metres e.g. 0.175",
            "Maximum riser height 175mm for residential; 190mm for commercial per SCDF.",
            "SCDF Fire Code / BCA Code on Accessibility 2025 §4.3.1"));

        R.Add(Req("IFCSTAIRFLIGHT","Pset_StairFlightCommon","TreadLength", true, SgAgency.BCA, GCon, PGAll, SG,
            "Tread depth in metres e.g. 0.28",
            "Minimum tread depth 250mm (SCDF escape stairs); 280mm preferred.",
            "SCDF Fire Code §5.2 - Escape Stair Dimensions"));

        // ── IFCCOLUMN / IFCBEAM / IFCFOOTING / IFCPILE ───────────────────────

        R.Add(Req("IFCCOLUMN","Pset_ColumnCommon","LoadBearing",      true, SgAgency.BCA, GCon, PGAll, SG,
            "TRUE (columns are always load-bearing by definition)",
            "Confirm all columns are marked as load-bearing for structural review.",
            "BCA Building Control Act / IFC+SG"));

        R.Add(Req("IFCBEAM","Pset_BeamCommon","LoadBearing",          true, SgAgency.BCA, GCon, PGAll, SG,
            "TRUE for structural beams, FALSE for decorative beams",
            "BCA structural plan review requires load-bearing status for all beams.",
            "BCA Building Control Act / IFC+SG"));

        R.Add(Req("IFCFOOTING","Pset_FootingCommon","LoadBearing",    true, SgAgency.BCA, GCon, PGAll, SG,
            "TRUE (all footings are structural)",
            "Required for BCA foundation submission.",
            "BCA Building Control Act - Foundation Plans"));

        R.Add(Req("IFCPILE","Pset_PileCommon","LoadBearing",          true, SgAgency.BCA, GPil, PGAll, SG,
            "TRUE - piles are structural elements",
            "Required at Piling Gateway submission to BCA.",
            "CORENET-X Piling Gateway / BCA Building Control Act"));

        // ── GEOREFERENCING (SVY21) - file-level, mapped to IfcSite ───────────

        R.Add(Req("IFCSITE","Pset_SiteCommon","SiteID",               false, SgAgency.SLA, GDes, PGAll, SG,
            "Singapore Land Authority site lot number e.g. 'MK22-01234X'",
            "SLA uses the site lot number to verify the submission is for the correct site.",
            "SLA Land Registry / CORENET-X COP 3rd Edition - Site Identification"));

        R.Add(Req("IFCSITE","Pset_SiteCommon","RefElevation",         false, SgAgency.SLA, GDes, PGAll, SG,
            "Site datum elevation in Singapore Height Datum (SHD) metres",
            "All vertical coordinates should reference Singapore Height Datum.",
            "CORENET-X COP §3 - Coordinate Reference System"));

        // ────────────────────────────────────────────────────────────────────
        //  COMPLETION GATEWAY - additional requirements
        // ────────────────────────────────────────────────────────────────────

        R.Add(Req("IFCWALL","Pset_WallCommon","Reference",            false, SgAgency.BCA, GCom, PGAll, SG,
            "Drawing reference number for this wall type e.g. 'W-TYPE-A1'",
            "At Completion gateway, all element types should be cross-referenced to as-built drawings.",
            "CORENET-X Completion Gateway - As-Built Requirements"));

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","OccupancyType",      false, SgAgency.URA, GCom, PGAll, SG,
            "Actual occupancy use as completed e.g. 'RESIDENTIAL_4-ROOM_HDB'",
            "At Completion, URA requires confirmed occupancy type matching the CSC submission.",
            "CORENET-X Completion Gateway / URA Certificate of Statutory Completion"));

        // ────────────────────────────────────────────────────────────────────
        //  DIRECT SUBMISSION (DSP) - streamlined requirements for small projects
        // ────────────────────────────────────────────────────────────────────

        R.Add(Req("IFCWALL","Pset_WallCommon","IsExternal",           true, SgAgency.BCA, GDSP, PGAll, SG,
            "TRUE or FALSE",
            "Even at DSP level, external wall identification is required.",
            "CORENET-X DSP - Minimum Data Set"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","HandicapAccessible",   true, SgAgency.BCA, GDSP, PGAll, SG,
            "TRUE or FALSE",
            "Accessibility compliance remains mandatory at DSP.",
            "Code on Accessibility 2025 / CORENET-X DSP"));

        // ╔══════════════════════════════════════════════════════════════════════╗
        // ║  MALAYSIA - NBeS / UBBL 1984 (all 9 Purpose Groups)               ║
        // ╚══════════════════════════════════════════════════════════════════════╝

        // ── IFCWALL (Malaysia) ────────────────────────────────────────────────

        R.Add(Req("IFCWALL","Pset_WallCommon","IsExternal",           true,  SgAgency.None, GCon, PGAll, MY,
            "TRUE or FALSE",
            "Required by NBeS. External walls trigger UBBL fire resistance and thermal checks.",
            "UBBL 1984 By-Law 120 / NBeS IFC Mapping 2024"));

        R.Add(Req("IFCWALL","Pset_WallCommon","LoadBearing",          true,  SgAgency.None, GCon, PGAll, MY,
            "TRUE for load-bearing walls, FALSE for non-load-bearing",
            "Required for UBBL Part V structural compliance.",
            "UBBL 1984 Part V - Structural Requirements"));

        R.Add(Req("IFCWALL","Pset_WallCommon","FireRating",           true,  SgAgency.None, GCon, PGAll, MY,
            "Fire resistance period in minutes per UBBL Part VII. Use '60', '90', '120' or '0' if not required.",
            "UBBL 1984 Part VII / By-Law 121–140. JBPM requires fire rating for all compartment walls. " +
            "Use the fire resistance table in UBBL Third Schedule for minimum periods by Purpose Group.",
            "UBBL 1984 Part VII - Fire Requirements / JBPM Fire Safety Requirements 2020"));

        R.Add(Req("IFCWALL","Pset_WallCommon","ThermalTransmittance", false, SgAgency.None, GCon, PGAll, MY,
            "U-value in W/(m²·K)",
            "MS 1525:2019 requires thermal performance for external walls in non-residential buildings.",
            "MS 1525:2019 - Energy Efficiency for Non-Residential Buildings"));

        // ── IFCDOOR (Malaysia) ────────────────────────────────────────────────

        R.Add(Req("IFCDOOR","Pset_DoorCommon","IsExternal",           true, SgAgency.None, GCon, PGAll, MY,
            "TRUE or FALSE",
            "External doors trigger weather resistance and security requirements per UBBL.",
            "UBBL 1984 / NBeS IFC Mapping 2024"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","HandicapAccessible",   true, SgAgency.None, GCon, PGAll, MY,
            "TRUE for accessible doors (min 800mm clear), FALSE otherwise",
            "Mandatory per MS 1184:2014 Code of Practice for Access for Disabled People. " +
            "All accessible routes must have min 800mm clear opening.",
            "MS 1184:2014 §5.3 - Accessible Doors"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","FireRating",           false, SgAgency.None, GCon, PGAll, MY,
            "Fire door period e.g. 'FD30', 'FD60', 'FD120'. Leave empty if not fire-rated.",
            "Required for fire-rated compartment doors per UBBL 1984 Part VII.",
            "UBBL 1984 Part VII By-Law 130 / JBPM Fire Safety Requirements 2020"));

        R.Add(Req("IFCDOOR","Pset_DoorCommon","SmokeStop",            false, SgAgency.None, GCon, PGAll, MY,
            "TRUE if smoke-stop door required under Purpose Groups III–VII",
            "Smoke-stop doors required at protected corridors under UBBL 1984 Part VII.",
            "UBBL 1984 Part VII By-Law 137 - Smoke Compartmentation"));

        // ── IFCSPACE (Malaysia) ───────────────────────────────────────────────

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","Category",           true, SgAgency.None, GDes, PGAll, MY,
            "Space use per UBBL purpose group e.g. 'RESIDENTIAL', 'OFFICE', 'FACTORY', 'PLACE_OF_ASSEMBLY'",
            "Required by NBeS for compliance with UBBL occupancy-based requirements.",
            "UBBL 1984 Third Schedule - Purpose Groups / NBeS IFC Mapping 2024"));

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","GrossPlannedArea",   true, SgAgency.None, GDes, PGAll, MY,
            "Area in m²",
            "Required by NBeS for compliance with UBBL minimum room size requirements.",
            "UBBL 1984 By-Law 48 - Habitable Rooms / NBeS"));

        // Purpose-group-specific room height requirements

        R.Add(Req("IFCSPACE","Pset_SpaceCommon","Height",             true, SgAgency.None, GCon, PGAll, MY,
            "Clear ceiling height in metres. Minimum 2.6m for habitable rooms (UBBL By-Law 47).",
            "UBBL 1984 By-Law 47 requires minimum clear height 2.6m for habitable rooms " +
            "and 2.3m for bathrooms, store rooms and corridors.",
            "UBBL 1984 By-Law 47 - Ceiling Heights"));

        // ── IFCSLAB (Malaysia) ────────────────────────────────────────────────

        R.Add(Req("IFCSLAB","Pset_SlabCommon","IsExternal",           true, SgAgency.None, GCon, PGAll, MY,
            "TRUE for roof slabs, FALSE for internal floors",
            "External slabs trigger UBBL waterproofing and thermal requirements.",
            "UBBL 1984 Part VI §117 - Constructional Requirements"));

        R.Add(Req("IFCSLAB","Pset_SlabCommon","FireRating",           true, SgAgency.None, GCon, PGAll, MY,
            "Fire resistance period per UBBL Part VII e.g. '60', '90', '120'",
            "Floor slabs bounding fire compartments require fire rating per UBBL Third Schedule.",
            "UBBL 1984 Part VII By-Law 122 - Floor/Ceiling Compartmentation"));

        R.Add(Req("IFCSLAB","Pset_SlabCommon","LoadBearing",          true, SgAgency.None, GCon, PGAll, MY,
            "TRUE or FALSE",
            "Required for UBBL Part V structural compliance.",
            "UBBL 1984 Part V - Structural Requirements"));

        // ── IFCSTAIR (Malaysia) ───────────────────────────────────────────────

        R.Add(Req("IFCSTAIRFLIGHT","Pset_StairFlightCommon","RiserHeight", true, SgAgency.None, GCon, PGAll, MY,
            "Riser height in metres. Maximum 0.175m per UBBL By-Law 114.",
            "UBBL By-Law 114 sets maximum riser height at 175mm.",
            "UBBL 1984 By-Law 114 - Staircase Construction"));

        R.Add(Req("IFCSTAIRFLIGHT","Pset_StairFlightCommon","TreadLength", true, SgAgency.None, GCon, PGAll, MY,
            "Tread depth in metres. Minimum 0.255m per UBBL By-Law 114.",
            "UBBL By-Law 114 sets minimum tread depth at 255mm.",
            "UBBL 1984 By-Law 114 - Staircase Construction"));

        // ── FIRE SAFETY - Malaysia JBPM (all Purpose Groups) ─────────────────

        R.Add(Req("IFCROOF","Pset_RoofCommon","FireRating",           false, SgAgency.None, GCon, PGAll, MY,
            "Fire resistance period for roof structure if applicable. '0' if not required.",
            "UBBL 1984 By-Law 122 - roof fire resistance requirements.",
            "UBBL 1984 Part VII - Fire Requirements"));

        R.Add(Req("IFCCOLUMN","Pset_ColumnCommon","FireRating",        true, SgAgency.None, GCon, PGAll, MY,
            "Fire resistance period per UBBL Third Schedule, e.g. '60', '120', '180'",
            "UBBL 1984 Third Schedule defines minimum fire resistance by purpose group. " +
            "All structural columns must have certified fire protection.",
            "UBBL 1984 Part VII / JBPM Fire Safety Requirements 2020"));

        R.Add(Req("IFCBEAM","Pset_BeamCommon","FireRating",            true, SgAgency.None, GCon, PGAll, MY,
            "Fire resistance period per UBBL Third Schedule",
            "Structural beams must have certified fire protection per UBBL.",
            "UBBL 1984 Part VII - Fire Requirements / JBPM"));

        return R;
    }

    // ─── TYPE RULES ───────────────────────────────────────────────────────────

    private static readonly List<PropertyTypeRule> _typeRules = new()
    {
        // Boolean
        new() { PropertySetName="Pset_WallCommon",  PropertyName="IsExternal",  ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_WallCommon",  PropertyName="LoadBearing", ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_SlabCommon",  PropertyName="IsExternal",  ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_SlabCommon",  PropertyName="LoadBearing", ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_DoorCommon",  PropertyName="IsExternal",  ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_DoorCommon",  PropertyName="FireExit",    ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.SCDF },
        new() { PropertySetName="Pset_DoorCommon",  PropertyName="HandicapAccessible", ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_DoorCommon",  PropertyName="SmokeStop",   ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.SCDF },
        new() { PropertySetName="Pset_WindowCommon",PropertyName="IsExternal",  ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_SpaceCommon", PropertyName="IsExternal",  ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.URA },
        new() { PropertySetName="Pset_BeamCommon",  PropertyName="LoadBearing", ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_ColumnCommon",PropertyName="LoadBearing", ExpectedType="BOOLEAN", Country=SG, AffectedAgency=SgAgency.BCA },
        // Real (numeric)
        new() { PropertySetName="Pset_SpaceCommon", PropertyName="GrossPlannedArea",    ExpectedType="REAL", Country=SG, AffectedAgency=SgAgency.URA },
        new() { PropertySetName="Pset_SpaceCommon", PropertyName="NetPlannedArea",       ExpectedType="REAL", Country=SG, AffectedAgency=SgAgency.URA },
        new() { PropertySetName="Pset_WallCommon",  PropertyName="ThermalTransmittance", ExpectedType="REAL", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_WindowCommon",PropertyName="ThermalTransmittance", ExpectedType="REAL", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_StairFlightCommon",PropertyName="RiserHeight",  ExpectedType="REAL", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_StairFlightCommon",PropertyName="TreadLength",  ExpectedType="REAL", Country=SG, AffectedAgency=SgAgency.BCA },
        new() { PropertySetName="Pset_SpaceCommon", PropertyName="Height",  ExpectedType="REAL", Country=MY },
        new() { PropertySetName="Pset_SpaceCommon", PropertyName="GrossPlannedArea",  ExpectedType="REAL", Country=MY },
        new() { PropertySetName="Pset_StairFlightCommon",PropertyName="RiserHeight",  ExpectedType="REAL", Country=MY },
        new() { PropertySetName="Pset_StairFlightCommon",PropertyName="TreadLength",  ExpectedType="REAL", Country=MY },
        // Boolean Malaysia
        new() { PropertySetName="Pset_WallCommon",  PropertyName="IsExternal",  ExpectedType="BOOLEAN", Country=MY },
        new() { PropertySetName="Pset_WallCommon",  PropertyName="LoadBearing", ExpectedType="BOOLEAN", Country=MY },
        new() { PropertySetName="Pset_DoorCommon",  PropertyName="HandicapAccessible", ExpectedType="BOOLEAN", Country=MY },
        new() { PropertySetName="Pset_DoorCommon",  PropertyName="SmokeStop",   ExpectedType="BOOLEAN", Country=MY },
        new() { PropertySetName="Pset_SlabCommon",  PropertyName="LoadBearing", ExpectedType="BOOLEAN", Country=MY },
    };

    // ─── ENUMERATION RULES ────────────────────────────────────────────────────

    private static readonly List<EnumerationRule> _enumRules = new()
    {
        new()
        {
            PropertySetName = "Pset_SpaceCommon", PropertyName = "Category",
            Country = SG, AffectedAgency = SgAgency.URA,
            RuleSource = "URA Planning Parameters 2023 / IFC+SG Industry Mapping",
            PermittedValues = new()
            {
                // Residential
                "RESIDENTIAL","BEDROOM","MASTER_BEDROOM","LIVING_ROOM","DINING_ROOM",
                "KITCHEN","BATHROOM","TOILET","STUDY_ROOM","UTILITY_ROOM","STORE_ROOM",
                "WARDROBE","BALCONY","TERRACE","ROOF_TERRACE",
                // Commercial
                "OFFICE","COMMERCIAL","RETAIL","SHOP","FOOD_AND_BEVERAGE","RESTAURANT",
                "FOOD_COURT","SUPERMARKET","WET_MARKET",
                // Hospitality
                "HOTEL","SERVICED_APARTMENT","HOTEL_ROOM","HOTEL_LOBBY",
                // Healthcare
                "HEALTHCARE","HOSPITAL_WARD","CLINIC","PHARMACY","MEDICAL_SUITE",
                // Education
                "EDUCATIONAL","CLASSROOM","LECTURE_HALL","LIBRARY","LABORATORY","CHILDCARE",
                // Recreation / Civic
                "CIVIC","COMMUNITY_CLUB","PLACE_OF_WORSHIP","SPORTS_HALL","GYM","SWIMMING_POOL",
                "CINEMA","THEATRE",
                // Industrial
                "INDUSTRIAL","WAREHOUSE","FACTORY","WORKSHOP","CLEAN_ROOM","DATA_CENTRE",
                // Infrastructure
                "CARPARK","LOADING_BAY","PLANT_ROOM","MECHANICAL_ROOM","ELECTRICAL_ROOM",
                "LIFT_LOBBY","CORRIDOR","STAIRCASE","FIRE_ESCAPE","VOID","CIRCULATION",
                "COMMON","SERVICE","EXTERNAL","ROOF","BASEMENT",
                // Special Singapore
                "LANDSCAPING","SKY_GARDEN","COMMUNAL_GARDEN","RECREATIONAL_AMENITY",
                "CAR_SHOWROOM","PETROL_STATION","TRANSPORT_NODE","FERRY_TERMINAL",
                // Generic
                "USERDEFINED","NOTDEFINED"
            }
        },
        new()
        {
            PropertySetName = "Pset_SpaceCommon", PropertyName = "Category",
            Country = MY, AffectedAgency = SgAgency.None,
            RuleSource = "UBBL 1984 Third Schedule / NBeS IFC Mapping 2024",
            PermittedValues = new()
            {
                // UBBL Purpose Group I & II - Residential
                "RESIDENTIAL","HOUSE","TERRACE_HOUSE","SEMI_DETACHED","BUNGALOW",
                "APARTMENT","FLAT","CONDOMINIUM","BEDROOM","LIVING_ROOM","KITCHEN",
                "BATHROOM","STUDY","STORE_ROOM","BALCONY",
                // Purpose Group III - Other Residential
                "HOSTEL","DORMITORY","BOARDING_HOUSE","HOTEL","SERVICED_APARTMENT",
                // Purpose Group IV - Office
                "OFFICE","MEETING_ROOM","RECEPTION","OPEN_PLAN",
                // Purpose Group V - Shop
                "SHOP","RETAIL","SUPERMARKET","FOOD_COURT","RESTAURANT","WET_MARKET",
                // Purpose Group VI - Factory
                "FACTORY","WAREHOUSE","WORKSHOP","INDUSTRIAL","CLEAN_ROOM","DATA_CENTRE",
                // Purpose Group VII - Assembly
                "CINEMA","THEATRE","STADIUM","SPORTS_HALL","ASSEMBLY_HALL","CONFERENCE_ROOM",
                "PLACE_OF_WORSHIP","COMMUNITY_HALL",
                // Purpose Group VIII - Institution
                "SCHOOL","CLASSROOM","LIBRARY","HOSPITAL","CLINIC","NURSERY","UNIVERSITY",
                // Common
                "CARPARK","CORRIDOR","STAIRCASE","LIFT_LOBBY","PLANT_ROOM","MECHANICAL",
                "ELECTRICAL","LOADING_BAY","CIRCULATION","VOID","EXTERNAL","SERVICE",
                "USERDEFINED","NOTDEFINED"
            }
        },
        // Fire rating string format guidance
        new()
        {
            PropertySetName = "Pset_WallCommon", PropertyName = "FireRating",
            Country = SG, AffectedAgency = SgAgency.SCDF,
            RuleSource = "SCDF Fire Code 2018 / SS EN 1363",
            PermittedValues = new()
            {
                "30/30/30","60/60/60","90/90/90","120/120/120","180/180/180","240/240/240",
                "FRR 30","FRR 60","FRR 90","FRR 120","FRR 180","FRR 240",
                "30","60","90","120","180","240",
                "0","N/A","NOTDEFINED"
            }
        },
    };

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    // Country constants for readability
    private const CountryMode SG   = CountryMode.Singapore;
    private const CountryMode MY   = CountryMode.Malaysia;
    private const CountryMode Both = CountryMode.Combined;

    // Gateway constants
    private const CorenetGateway GDes = CorenetGateway.Design;
    private const CorenetGateway GPil = CorenetGateway.Piling;
    private const CorenetGateway GCon = CorenetGateway.Construction;
    private const CorenetGateway GCom = CorenetGateway.Completion;
    private const CorenetGateway GDSP = CorenetGateway.DirectSubmission;

    // Purpose group constants
    private const MalaysiaPurposeGroup PGAll = MalaysiaPurposeGroup.All;

    private static List<PropertySetRequirement> Psets(params PropertySetRequirement[] items)
        => new(items);

    private static PropertySetRequirement P(
        string name, bool required, bool sgSpecific,
        SgAgency agency, CountryMode country, string source) =>
        new()
        {
            Name                = name,
            IsRequired          = required,
            IsSingaporeSpecific = sgSpecific,
            AffectedAgency      = agency,
            Country             = country,
            RuleSource          = source
        };

    private static ExtendedPropertyRequirement Req(
        string ifcClass, string pset, string prop,
        bool required, SgAgency agency, CorenetGateway gateway,
        MalaysiaPurposeGroup pg, CountryMode country,
        string expectedDesc, string guidance, string source) =>
        new()
        {
            IfcClass                 = ifcClass,
            PropertySetName          = pset,
            PropertyName             = prop,
            IsRequired               = required,
            AffectedAgency           = agency,
            Gateway                  = gateway,
            PurposeGroup             = pg,
            Country                  = country,
            ExpectedValueDescription = expectedDesc,
            RemediationGuidance      = guidance,
            RuleSource               = source
        };

    // ─── DATABASE CREATION ────────────────────────────────────────────────────

    private void CreateAndSeedDatabase()
    {
        using var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS rules_metadata (
                key TEXT PRIMARY KEY, value TEXT NOT NULL);
            INSERT OR REPLACE INTO rules_metadata VALUES ('sg_version','IFC+SG 2025.1 (COP3)');
            INSERT OR REPLACE INTO rules_metadata VALUES ('my_version','NBeS 2024.1 (CIDB 2nd Ed.)');
            INSERT OR REPLACE INTO rules_metadata VALUES ('created_at',CURRENT_TIMESTAMP);
        ";
        cmd.ExecuteNonQuery();
    }

    // ─── CLASSIFICATION-SPECIFIC PROPERTY REQUIREMENTS ─────────────────────────
    //
    // CORENET-X IFC+SG Industry Mapping 2025 - classification code → Pset rules
    //
    // The IFC+SG classification ItemReference codes encode functional attributes:
    //   EXT = external,  INT = internal,  STR = structural/load-bearing
    //   FIR = fire-rated,  ACC = accessible,  THM = thermal/envelope
    //
    // When an element has a classification code with these segments, additional
    // property sets beyond the base IFC class requirements become mandatory.
    // This implements the key Q2 requirement: classification present →
    // check that ALL related CORENET-X property sets and properties are present.

    public List<PropertyRequirement> GetPropertiesForClassification(
        string ifcClass, string classificationCode, CountryMode mode)
    {
        var results = new List<PropertyRequirement>();
        if (string.IsNullOrWhiteSpace(classificationCode)) return results;

        var cls  = ifcClass.ToUpperInvariant();
        var code = classificationCode.ToUpperInvariant();

        // ── STEP 1: Exact lookup in embedded ClassificationCodeLibrary ────────
        // Covers all IFC+SG Industry Mapping codes. Each code maps directly
        // to the correct set of SGPset_ property requirements per agency.
        var libEntry = ClassificationCodeLibrary.Find(classificationCode);
        if (libEntry != null)
        {
            foreach (var rule in libEntry.Rules)
            {
                var country = (rule.Agency == SgAgency.CIDB || rule.Agency == SgAgency.JBPM)
                    ? CountryMode.Malaysia : CountryMode.Singapore;
                results.Add(new PropertyRequirement
                {
                    IfcClass            = ifcClass,
                    PropertySetName     = rule.PropertySetName,
                    PropertyName        = rule.PropertyName,
                    IsRequired          = rule.IsRequired,
                    AffectedAgency      = rule.Agency,
                    CheckLevel          = CheckLevel.SgPropertySets,
                    CountryApplicability = country,
                    RemediationGuidance = $"{rule.Description} - {rule.Regulation}",
                    RuleSource          = rule.Regulation,
                    ExpectedValue       = rule.ExpectedValue,
                    GatewayApplicability = rule.Gateway
                });
            }
        }

        // ── STEP 2: Pattern-based fallback (catches unlisted codes + class-based rules) ─
        if (mode == CountryMode.Singapore || mode == CountryMode.Combined)
        {
            // External elements → thermal requirements mandatory
            if (code.Contains("EXT") || code.Contains("EXTERNAL") || code.Contains("-EW") || code.Contains("-RF") || code.Contains("EXW") || code.Contains("EXF") || code.Contains("ROF") || code.Contains("CWT") || code.Contains("CUR") || code.Contains("FACADE") || code.Contains("-EX") || code.Contains("CLADDING"))
            {
                if (cls is "IFCWALL" or "IFCWALLSTANDARDCASE")
                {
                    results.Add(Req("IFCWALL","SGPset_WallThermal","ThermalTransmittance",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "U-value in W/m²K - max 0.50 per BCA Green Mark 2021 §3.4.2",
                        "Declare ThermalTransmittance (U-value W/m²K) for external walls per BCA Green Mark 2021. Max 0.50 W/m²K.",
                        "IFC+SG Industry Mapping - external wall → SGPset_WallThermal required"));
                    results.Add(Req("IFCWALL","SGPset_WallThermal","SolarAbsorptance",false,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "Solar absorptance coefficient (0.0–1.0)",
                        "Solar absorptance affects ETTV calculation per BCA Green Mark 2021.",
                        "BCA Green Mark 2021 §3.4.2 - ETTV calculation"));
                    results.Add(Req("IFCWALL","Pset_WallCommon","IsExternal",true,SgAgency.BCA,CorenetGateway.Design,PGAll,CountryMode.Singapore,
                        "TRUE - this is an external wall",
                        "Mark IsExternal=TRUE on all external walls. This triggers thermal and acoustic agency checks.",
                        "IFC+SG COP3 - Pset_WallCommon.IsExternal mandatory for external walls"));
                }
                if (cls is "IFCWINDOW")
                {
                    results.Add(Req("IFCWINDOW","Pset_WindowCommon","ThermalTransmittance",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "U-value W/m²K (whole-window system)",
                        "Whole-window U-value required for ETTV calculation. Declare for all external windows.",
                        "BCA Green Mark 2021 §3.4.3 / IFC+SG Industry Mapping - external window"));
                    results.Add(Req("IFCWINDOW","SGPset_WindowPerformance","SolarHeatGainCoefficient",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "SHGC (0.0–1.0) - max 0.30 per BCA Green Mark",
                        "Solar Heat Gain Coefficient for ETTV. Max 0.30 per BCA Green Mark 2021 §3.4.3.",
                        "BCA Green Mark 2021 §3.4.3 - SHGC for CORENET-X submission"));
                    results.Add(Req("IFCWINDOW","SGPset_WindowPerformance","VisibleLightTransmittance",false,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "VLT (0.0–1.0) - minimum 0.20 recommended",
                        "Visible Light Transmittance affects daylighting compliance per NEA EPHA.",
                        "NEA Environmental Public Health Act - daylighting"));
                }
                if (cls is "IFCROOF" or "IFCSLAB")
                {
                    results.Add(Req(cls,"Pset_RoofCommon","ThermalTransmittance",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "Roof U-value W/m²K - max 0.35 per BCA Green Mark 2021",
                        "Declare roof U-value. BCA Green Mark 2021 §3.4.1 requires roof ≤ 0.35 W/m²K.",
                        "BCA Green Mark 2021 §3.4.1 - roof thermal transmittance"));
                }
            }

            // Structural / load-bearing elements → structural property sets mandatory
            if (code.Contains("STR") || code.Contains("STRUCTURAL") || code.Contains("LOAD") || code.Contains("-SB") || code.Contains("-LB") || code.Contains("BRG") || code.Contains("BEAR") || code.Contains("-LW") || code.Contains("CONC") || code.Contains("STEEL") || code.Contains("RC-") || code.Contains("-RC") || code.Contains("FOUND"))
            {
                if (cls is "IFCWALL" or "IFCWALLSTANDARDCASE")
                    results.Add(Req("IFCWALL","Pset_WallCommon","LoadBearing",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "TRUE - this is a load-bearing wall",
                        "Mark LoadBearing=TRUE on all structural/load-bearing walls. Required for BCA structural adequacy checks.",
                        "IFC+SG COP3 - Pset_WallCommon.LoadBearing mandatory for structural walls / BC 2:2021"));
                if (cls is "IFCCOLUMN")
                {
                    results.Add(Req("IFCCOLUMN","SGPset_ColumnStructural","ConcreteGrade",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "e.g. C32/40, C40/50 - per SS EN 1992-1-1",
                        "Concrete grade required by BCA for structural adequacy. Declare per SS EN 1992 notation.",
                        "BC 2:2021 §3.1 / SS EN 1992-1-1 §3.1 - concrete grade for columns"));
                    results.Add(Req("IFCCOLUMN","SGPset_ColumnStructural","DesignCode",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "BC2:2021 or SS EN 1992-1-1",
                        "State the design code used for structural design of this column.",
                        "Building Control Act / BC 2:2021 - design code declaration required"));
                }
                if (cls is "IFCBEAM")
                    results.Add(Req("IFCBEAM","SGPset_BeamStructural","ConcreteGrade",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "e.g. C32/40 - per SS EN 1992-1-1",
                        "Concrete grade required for structural beams. Declare per BC 2:2021.",
                        "BC 2:2021 / SS EN 1992-1-1 - concrete grade for beams"));
                if (cls is "IFCSLAB")
                    results.Add(Req("IFCSLAB","Pset_SlabCommon","LoadBearing",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "TRUE - structural slab",
                        "Mark LoadBearing=TRUE on structural slabs per BCA requirements.",
                        "IFC+SG COP3 - Pset_SlabCommon.LoadBearing for structural slabs"));
            }

            // Fire-rated elements → fire resistance properties mandatory
            if (code.Contains("FIR") || code.Contains("FIRE") || code.Contains("FRR") || code.Contains("-FR") || code.Contains("FRD") || code.Contains("FRW") || code.Contains("COMP") || code.Contains("RESIS") || code.Contains("REI") || code.Contains("-FD") || code.Contains("-FW") || code.Contains("SPRINK") || code.Contains("ESCAPE") || code.Contains("EXIT") || code.Contains("STAIR"))
            {
                foreach (var fireClass in new[]{"IFCWALL","IFCWALLSTANDARDCASE","IFCSLAB","IFCDOOR","IFCSTAIR","IFCROOF"})
                {
                    if (cls == fireClass || (fireClass == "IFCWALL" && cls == "IFCWALLSTANDARDCASE"))
                    {
                        var psetName = fireClass switch {
                            "IFCWALL"     => "SGPset_WallFireRating",
                            "IFCWALLSTANDARDCASE" => "SGPset_WallFireRating",
                            "IFCSLAB"     => "SGPset_SlabFireRating",
                            "IFCDOOR"     => "Pset_DoorCommon",
                            "IFCSTAIR"    => "SGPset_StairFireEscape",
                            _             => "Pset_RoofCommon"
                        };
                        results.Add(Req(cls,psetName,"FireResistancePeriod",true,SgAgency.SCDF,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                            "REI notation: 30, 60, 90, 120, 180, 240 minutes",
                            $"Fire resistance period required on all fire-rated elements. Use REI notation (e.g. '60' = 60 min). Refer to SCDF Fire Code 2018 Table 4.2.",
                            "SCDF Fire Code 2018 §4.3 Table 4.2 / IFC+SG COP3 - fire-rated classification → SGPset_FireRating required"));
                        results.Add(Req(cls,psetName,"FireTestStandard",true,SgAgency.SCDF,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                            "SS 332, BS 476, EN 13501, UL 263",
                            "State the fire test standard used to certify the fire resistance period.",
                            "SCDF Fire Code 2018 §4.3 / BCA Technical Note - fire test standard declaration"));
                    }
                }
                if (cls == "IFCDOOR")
                {
                    results.Add(Req("IFCDOOR","Pset_DoorCommon","FireExit",true,SgAgency.SCDF,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "TRUE - this is a fire exit door on an escape route",
                        "Mark FireExit=TRUE on all escape route doors. Required for SCDF automated checking.",
                        "SCDF Fire Code 2018 §5.2 / IFC+SG COP3 - escape doors must be flagged"));
                    results.Add(Req("IFCDOOR","Pset_DoorCommon","SmokeStop",true,SgAgency.SCDF,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "TRUE - this door is smoke-stop rated",
                        "Declare smoke-stop capability on fire doors.",
                        "SCDF Fire Code 2018 §4.3 - smoke-stop doors in escape routes"));
                }
            }

            // Accessible elements → accessibility property sets mandatory
            if (code.Contains("ACC") || code.Contains("ACCESSIBLE") || code.Contains("DISABLE") || code.Contains("-AC") || code.Contains("WCHR") || code.Contains("RAMP") || code.Contains("LIFT") || code.Contains("BARRIER") || code.Contains("UNIV") || code.Contains("OKU") || code.Contains("-BF") || code.Contains("ADA"))
            {
                if (cls == "IFCDOOR")
                {
                    results.Add(Req("IFCDOOR","Pset_DoorCommon","HandicapAccessible",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "TRUE - this door is on an accessible route",
                        "Mark HandicapAccessible=TRUE. This triggers BCA to check clear width ≥850mm (Code on Accessibility 2025 §4.2.1).",
                        "Code on Accessibility 2025 §4.2.1 / IFC+SG COP3 - accessible doors must be flagged"));
                    results.Add(Req("IFCDOOR","SGPset_DoorAccessibility","ClearWidth",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "≥ 0.850 m (min) or ≥ 0.900 m (preferred)",
                        "Declare the clear door width in metres. Accessible doors must have ≥850mm clear width per Code on Accessibility 2025.",
                        "Code on Accessibility 2025 §4.2.1 - door clear width for accessible route"));
                }
                if (cls == "IFCRAMP")
                {
                    results.Add(Req("IFCRAMP","SGPset_RampAccessibility","Gradient",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "≤ 0.0833 (1:12 max gradient)",
                        "Declare ramp gradient as a ratio. Max 1:12 (0.0833) per BCA Code on Accessibility 2025 §4.3.",
                        "Code on Accessibility 2025 §4.3 - accessible ramp gradient limit"));
                    results.Add(Req("IFCRAMP","SGPset_RampAccessibility","Width",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "≥ 1.200 m",
                        "Accessible ramp must be ≥1.2m wide.",
                        "Code on Accessibility 2025 §4.3.1 - accessible ramp width"));
                }
                if (cls == "IFCSPACE")
                    results.Add(Req("IFCSPACE","Pset_SpaceCommon","IsAccessible",true,SgAgency.BCA,CorenetGateway.Construction,PGAll,CountryMode.Singapore,
                        "TRUE - this space is on an accessible route",
                        "Declare IsAccessible=TRUE on spaces that are part of the accessible route.",
                        "Code on Accessibility 2025 - accessible route spaces"));
            }

            // GFA / Planning spaces → URA property sets mandatory
            if (code.Contains("GFA") || code.Contains("SPACE") || code.Contains("ROOM") || code.Contains("UNIT") || cls == "IFCSPACE")
            {
                results.Add(Req("IFCSPACE","SGPset_SpaceGFA","GFACategory",true,SgAgency.URA,CorenetGateway.Design,PGAll,CountryMode.Singapore,
                    "RESIDENTIAL / OFFICE / RETAIL / CARPARK / VOID / BALCONY / PLANT_ROOM / ...",
                    "Declare the GFA category for URA automated GFA computation in CORENET-X. Category determines whether the space is counted or exempt from GFA.",
                    "URA Development Control / IFC+SG COP3 - SGPset_SpaceGFA.GFACategory mandatory for all spaces"));
                results.Add(Req("IFCSPACE","Pset_SpaceCommon","Category",true,SgAgency.URA,CorenetGateway.Design,PGAll,CountryMode.Singapore,
                    "LIVING_ROOM / BEDROOM / KITCHEN / OFFICE / CARPARK / ...",
                    "Declare space use category. URA uses this for GFA and planning parameter compliance.",
                    "IFC+SG COP3 - Pset_SpaceCommon.Category required for all spaces"));
                results.Add(Req("IFCSPACE","Pset_SpaceCommon","GrossPlannedArea",true,SgAgency.URA,CorenetGateway.Design,PGAll,CountryMode.Singapore,
                    "Area in m² (decimal)",
                    "Declare gross planned area in m². URA sums all IfcSpace.GrossPlannedArea values to compute building GFA.",
                    "URA Planning Parameters - GFA computed from sum of IfcSpace.GrossPlannedArea values"));
            }

            // Piling elements → foundation properties mandatory
            if (code.Contains("PIL") || code.Contains("PILE") || cls == "IFCPILE")
            {
                results.Add(Req("IFCPILE","SGPset_PileFoundation","PileType",true,SgAgency.BCA,CorenetGateway.Piling,PGAll,CountryMode.Singapore,
                    "BORED / DRIVEN / JETGROUTING",
                    "Declare pile type. Required at Piling Gateway before works commence.",
                    "BC 3:2013 (Foundations) / Building Control Regulations 2003 Reg 12"));
                results.Add(Req("IFCPILE","SGPset_PileFoundation","DesignLoad",true,SgAgency.BCA,CorenetGateway.Piling,PGAll,CountryMode.Singapore,
                    "Design load in kN",
                    "Declare design load per pile. Required for BCA pile submission.",
                    "BC 3:2013 - pile design load declaration"));
                results.Add(Req("IFCPILE","SGPset_PileFoundation","PileLength",true,SgAgency.BCA,CorenetGateway.Piling,PGAll,CountryMode.Singapore,
                    "Pile length in metres",
                    "Declare pile length in metres.",
                    "BC 3:2013 - pile length declaration"));
            }
        }

        // ── MALAYSIA NBeS - classification-driven property requirements ────────
        if (mode == CountryMode.Malaysia || mode == CountryMode.Combined)
        {
            if (code.Contains("EXT") || code.Contains("EXTERNAL"))
            {
                if (cls is "IFCWALL" or "IFCWALLSTANDARDCASE")
                    results.Add(Req("IFCWALL","Pset_WallCommon","IsExternal",true,SgAgency.None,CorenetGateway.Construction,PGAll,CountryMode.Malaysia,
                        "TRUE - external wall",
                        "Mark IsExternal=TRUE on external walls. Required for MS 1525:2019 thermal compliance checking.",
                        "MS 1525:2019 - thermal performance of buildings"));
            }
            if (code.Contains("FIR") || code.Contains("FIRE") || code.Contains("FRR"))
            {
                results.Add(Req(cls,"Pset_WallCommon","FireRating",true,SgAgency.None,CorenetGateway.Construction,PGAll,CountryMode.Malaysia,
                    "REI notation: 30, 60, 90, 120, 180, 240 minutes",
                    "Declare fire resistance period per UBBL 1984 Third Schedule. Required for JBPM fire submission.",
                    "UBBL 1984 Part VII / JBPM Fire Safety Requirements 2020"));
            }
            if (code.Contains("ACC") || code.Contains("ACCESSIBLE") && cls == "IFCDOOR")
                results.Add(Req("IFCDOOR","Pset_DoorCommon","HandicapAccessible",true,SgAgency.None,CorenetGateway.Construction,PGAll,CountryMode.Malaysia,
                    "TRUE - accessible door (≥800mm clear)",
                    "Mark accessible doors. MS 1184:2014 §5.3 requires ≥800mm clear width.",
                    "MS 1184:2014 §5.3 - accessible door clear width (Malaysia)"));
        }

        return results;
    }

    public void Dispose() => _connection?.Dispose();

    // ─── INNER TYPES ─────────────────────────────────────────────────────────

    private sealed class ExtendedPropertyRequirement : PropertyRequirement
    {
        public string IfcClass { get; set; } = string.Empty;
        public bool AppliesTo(string ifcClass) =>
            string.IsNullOrWhiteSpace(IfcClass) ||
            IfcClass.Equals(ifcClass, StringComparison.OrdinalIgnoreCase);
    }
}
