// VERIFIQ - Entity Class Rules + Auto-Classification Engine
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// Provides:
//   1. GetPermittedPredefinedTypes() - IFC4 schema enumeration validation
//   2. SuggestEntityClass()          - AI-style keyword classifier for proxies
//   3. IsKnownIfcClass()             - validates the element class exists in IFC4
//
// The suggestion engine uses a priority-weighted keyword table covering 120+
// terms across Architecture, Structure, MEP, Civil and Landscape disciplines.
// Terms are matched against both element Name and ObjectType attributes.

namespace VERIFIQ.Rules.Common;

public sealed class EntityClassRules : IEntityClassRules
{
    // ─── IFC4 PREDEFINED TYPES ────────────────────────────────────────────────
    private static readonly Dictionary<string, List<string>> _predefinedTypes =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["IFCBEAM"] = new() { "BEAM","JOIST","HOLLOWCORE","LINTEL","SPANDREL","T_BEAM","EDGEBEAM","USERDEFINED","NOTDEFINED" },
        ["IFCBUILDING"] = new() { "NOTDEFINED","USERDEFINED" },
        ["IFCBUILDINGELEMENTPROXY"] = new() { "USERDEFINED","NOTDEFINED","BOREHOLE","TACTILETILE","PORTABLEFIREEXTINGUISHER","SIGNAGE_EXIT","SIGNAGE_DIRECTION","SIGNAGE_IDENTIFICATION","SIGNAGE_SAFETY","DROPINLETCHAMBER","CULVERT","DRAIN","CARGENERALPARKINGLOT","CARBICYCLEPARKINGLOT","MOTORCYCLEPARKINGLOT","CARPWDPARKINGLOT","LORRYLOT","COACHLOT","ARTICULATEDVEHICLELOT","HOLDINGBAY","QUEUINGSPACE","HUMP","SPEEDTABLE","DIVIDER","SOAKAWAY","PREFABRICATEDBATHROOMUNIT","PREFABRICATEDPUMPSKID","PREFABRICATEDMEPVERTICALMODULE" },
        ["IFCBUILDINGSTOREY"] = new() { "NOTDEFINED","USERDEFINED" },
        ["IFCCIVILELEMENT"] = new() { "GUTTER","ROADKERB","ROADPAVEMENT","ROADSHOULDER","FOOTPATH","USERDEFINED","NOTDEFINED" },
        ["IFCCOLUMN"] = new() { "COLUMN","PILASTER","USERDEFINED","NOTDEFINED" },
        ["IFCCOVERING"] = new() { "CEILING","FLOORING","CLADDING","ROOFING","WRAPPING","MEMBRANE","INSULATION","FINISHING","SOFFIT","USERDEFINED","NOTDEFINED","PWCSINSPECTIONCHAMBERCOVER","PWCSMANHOLECOVER" },
        ["IFCDAMPER"] = new() { "FIREDAMPER","FIRESMOKEDAMPER","SMOKEDAMPER","CONTROLDAMPER","USERDEFINED","NOTDEFINED" },
        ["IFCDISCRETEACCESSORY"] = new() { "GRATING","PIPESUPPORT","USERDEFINED","NOTDEFINED" },
        ["IFCDOOR"] = new() { "DOOR","GATE","TRAPDOOR","BLASTDOOR","ROLLERSHUTTER","ACCESSHATCH","RECYCLABLESCHUTEACCESSPANEL","RECYCLABLESCHUTEHOPPER","USERDEFINED","NOTDEFINED" },
        ["IFCFIRESUPPRESSIONTERMINAL"] = new() { "BREECHINGINLET","FIREHYDRANT","HOSEREEL","STANDBYFIREHOSE","FOAMINLET","FOAMOUTLET","SPRINKLERHEAD","USERDEFINED","NOTDEFINED" },
        ["IFCFLOWMETER"] = new() { "WATERMETER","GASMETER","ENERGYMETER","USERDEFINED","NOTDEFINED" },
        ["IFCFOOTING"] = new() { "CAISSON_FOUNDATION","FOOTING_BEAM","PAD_FOOTING","PILE_CAP","STRIP_FOOTING","RAFT","USERDEFINED","NOTDEFINED" },
        ["IFCFURNITURE"] = new() { "CHAIR","BENCH","SEAT","TABLE","BED","USERDEFINED","NOTDEFINED" },
        ["IFCGEOGRAPHICELEMENT"] = new() { "TERRAIN","SITEBOUNDARY","CADASTRALLOT","LANDSCAPE_TREE","LANDSCAPE_PALM","LANDSCAPE_HEDGE","LANDSCAPE_SHRUB","LANDSCAPE_TURF","PLANTINGAREAS","GREENVERGE","USERDEFINED","NOTDEFINED" },
        ["IFCINTERCEPTOR"] = new() { "GREASE","OIL","USERDEFINED","NOTDEFINED" },
        ["IFCOPENINGELEMENT"] = new() { "OPENING","RECESS","USERDEFINED","NOTDEFINED" },
        ["IFCPILE"] = new() { "BORED","DRIVEN","JETGROUTING","COHESION","FRICTION","SUPPORT","MICROPILE","USERDEFINED","NOTDEFINED" },
        ["IFCPIPEFITTING"] = new() { "BEND","CONNECTOR","ENTRY","EXIT","JUNCTION","TRANSITION","USERDEFINED","NOTDEFINED" },
        ["IFCPIPESEGMENT"] = new() { "RIGIDSEGMENT","FLEXIBLESEGMENT","GUTTER","CULVERT","SCUPPERDRAIN","SPOOL","FLARESTACK","RAINWATEROUTLET","FOUNDATION","USERDEFINED","NOTDEFINED" },
        ["IFCPUMP"] = new() { "SUMPPUMP","BOOSTERPUMP","FIREPUMP","CIRCULATINGPUMP","USERDEFINED","NOTDEFINED" },
        ["IFCRAILING"] = new() { "HANDRAIL","GUARDRAIL","BALUSTRADE","FENCE","NOTDEFINED","USERDEFINED" },
        ["IFCRAMP"] = new() { "STRAIGHT_RUN_RAMP","TWO_STRAIGHT_RUN_RAMP","QUARTER_TURN_RAMP","HALF_TURN_RAMP","SPIRAL_RAMP","CURVEDRAMP","FLAREDKERBRAMP","USERDEFINED","NOTDEFINED" },
        ["IFCROOF"] = new() { "FLAT_ROOF","SHED_ROOF","GABLE_ROOF","HIP_ROOF","MANSARD_ROOF","BUTTERFLY_ROOF","DOME_ROOF","FREEFORM","USERDEFINED","NOTDEFINED" },
        ["IFCSANITARYTERMINAL"] = new() { "BATH","BIDET","SHOWER","SINK","WASHHANDBASIN","WATERCLOSET","URINAL","FLOORDRAIN","SPRINKLER","USERDEFINED","NOTDEFINED" },
        ["IFCSHADINGDEVICE"] = new() { "LOUVREDPANEL","AWNING","CANOPY","OVERHANG","USERDEFINED","NOTDEFINED" },
        ["IFCSITE"] = new() { "NOTDEFINED","USERDEFINED" },
        ["IFCSLAB"] = new() { "FLOOR","ROOF","LANDING","BASESLAB","APPROACH_SLAB","PAVING","WEARING","SIDEWALK","USERDEFINED","NOTDEFINED" },
        ["IFCSPACE"] = new() { "INTERNAL","EXTERNAL","SPACE","PARKING","AREA_GFA","ACCESSIBLEROUTE","STORAGE","DETENTIONTANK","RAINWATERHARVESTINGTANK","IRRIGATIONTANK","SPRINKLERTANK","SWIMMINGPOOL","USERDEFINED","NOTDEFINED" },
        ["IFCSTAIR"] = new() { "STRAIGHT_RUN_STAIR","TWO_STRAIGHT_RUN_STAIR","QUARTER_WINDER_STAIR","QUARTER_TURN_STAIR","HALF_WINDER_STAIR","HALF_TURN_STAIR","SPIRAL_STAIR","DOUBLE_RETURN_STAIR","CURVED_RUN_STAIR","USERDEFINED","NOTDEFINED" },
        ["IFCSTAIRFLIGHT"] = new() { "STRAIGHT","WINDER","SPIRAL","CURVED","FREEFORM","USERDEFINED","NOTDEFINED" },
        ["IFCSWITCHINGDEVICE"] = new() { "COMMUNICATIONSOUTLET","DATAOUTLET","POWEROUTLET","USERDEFINED","NOTDEFINED" },
        ["IFCTANK"] = new() { "STORAGE","EXPANSION","SECTIONAL","REFUSEBIN","RECYCLINGBIN","USERDEFINED","NOTDEFINED" },
        ["IFCTRANSPORTELEMENT"] = new() { "ELEVATOR","ESCALATOR","MOVINGWALKWAY","LIFT","CARLIFT","USERDEFINED","NOTDEFINED" },
        ["IFCUNITARYCONTROLELEMENT"] = new() { "CONTROLPANEL","USERDEFINED","NOTDEFINED" },
        ["IFCVALVE"] = new() { "LANDINGVALVE","SPRINKLERCONTROL","DOUBLECHECK","MIXING","AIRADMITTANCE","DRAINOFFCOCK","CHECK","BALANCING","BUTTERFLY","GATE","GLOBE","USERDEFINED","NOTDEFINED" },
        ["IFCWALL"] = new() { "STANDARD","SHEAR","PARAPET","PARTITIONING","PLUMBINGWALL","MOVABLE","ELEMENTEDWALL","SOLIDWALL","RETAININGWALL","USERDEFINED","NOTDEFINED" },
        ["IFCWASTETERMINAL"] = new() { "FLOORTRAP","FLOORWASTE","GULLYSUMP","GULLYTRAP","WASTETRAP","WASTESUMP","USERDEFINED","NOTDEFINED" },
        ["IFCWINDOW"] = new() { "WINDOW","SKYLIGHT","LIGHTDOME","BAYWINDOW","VENTILATIONSLEEVE","LOUVRE","USERDEFINED","NOTDEFINED" },
    };


    // ─── KEYWORD → IFC CLASS SUGGESTION TABLE (120+ terms, priority-ordered) ──
    // Longer / more specific terms are listed before shorter ones to avoid
    // false positives (e.g. "stair" before "air").
    // Source: IFC4 schema, CORENET-X Industry Mapping, NBeS Classification.
    private static readonly (string Keyword, string IfcClass, int Priority)[] _suggestions =
    {
        // Structural - high priority (very specific terms first)
        ("curtain wall",     "IfcCurtainWall",    100),
        ("rainscreen",       "IfcCurtainWall",    100),
        ("facade panel",     "IfcCurtainWall",    100),
        ("stair flight",     "IfcStairFlight",    100),
        ("stairflight",      "IfcStairFlight",    100),
        ("ramp flight",      "IfcRampFlight",     100),
        ("pile cap",         "IfcFooting",        100),
        ("strip footing",    "IfcFooting",        100),
        ("pad footing",      "IfcFooting",        100),
        ("base plate",       "IfcPlate",          100),
        ("gusset plate",     "IfcPlate",          100),
        ("splice plate",     "IfcPlate",          100),
        ("floor slab",       "IfcSlab",           100),
        ("roof slab",        "IfcSlab",           100),
        ("suspended slab",   "IfcSlab",           100),
        ("ground slab",      "IfcSlab",           100),
        ("retaining wall",   "IfcWall",           100),
        ("party wall",       "IfcWall",           100),
        ("shear wall",       "IfcWall",           100),
        ("fire wall",        "IfcWall",           100),
        ("handrail",         "IfcRailing",        100),
        ("balustrade",       "IfcRailing",        100),
        ("guardrail",        "IfcRailing",        100),
        ("parapet",          "IfcWall",            95),
        ("lintel",           "IfcBeam",            95),
        ("purlin",           "IfcMember",          95),
        ("rafter",           "IfcMember",          95),
        ("joist",            "IfcBeam",            95),
        ("brace",            "IfcMember",          95),
        ("strut",            "IfcMember",          95),
        ("truss",            "IfcMember",          95),
        ("hip",              "IfcRoof",            90),
        ("stair",            "IfcStair",           90),
        ("ramp",             "IfcRamp",            90),
        ("lift shaft",       "IfcSpace",           90),
        ("lift pit",         "IfcSpace",           90),
        // Architectural
        ("skylight",         "IfcWindow",          90),
        ("rooflight",        "IfcWindow",          90),
        ("glazing",          "IfcWindow",          85),
        ("louvre",           "IfcWindow",          85),
        ("slab",             "IfcSlab",            85),
        ("roof",             "IfcRoof",            85),
        ("floor",            "IfcSlab",            80),
        ("ceiling",          "IfcCovering",        80),
        ("cladding",         "IfcCovering",        80),
        ("insulation",       "IfcCovering",        80),
        ("membrane",         "IfcCovering",        80),
        ("topping",          "IfcCovering",        75),
        ("column",           "IfcColumn",          80),
        ("pillar",           "IfcColumn",          80),
        ("post",             "IfcColumn",          75),
        ("pier",             "IfcColumn",          75),
        ("wall",             "IfcWall",            70),
        ("partition",        "IfcWall",            70),
        ("door",             "IfcDoor",            80),
        ("gate",             "IfcDoor",            75),
        ("window",           "IfcWindow",          80),
        ("beam",             "IfcBeam",            80),
        ("railing",          "IfcRailing",         80),
        ("footing",          "IfcFooting",         80),
        ("foundation",       "IfcFooting",         80),
        ("pile",             "IfcPile",            80),
        ("plinth",           "IfcFooting",         75),
        ("plate",            "IfcPlate",           75),
        ("member",           "IfcMember",          70),
        // MEP
        ("sprinkler head",   "IfcFlowTerminal",   100),
        ("fire hydrant",     "IfcFlowTerminal",   100),
        ("hose reel",        "IfcFlowTerminal",   100),
        ("water closet",     "IfcSanitaryTerminal",100),
        ("wash basin",       "IfcSanitaryTerminal",100),
        ("wash hand basin",  "IfcSanitaryTerminal",100),
        ("hand dryer",       "IfcElectricAppliance",100),
        ("cooker hood",      "IfcFlowTerminal",   100),
        ("exhaust fan",      "IfcFlowTerminal",    95),
        ("toilet",           "IfcSanitaryTerminal", 90),
        ("wc",               "IfcSanitaryTerminal", 90),
        ("urinal",           "IfcSanitaryTerminal", 90),
        ("basin",            "IfcSanitaryTerminal", 85),
        ("sink",             "IfcSanitaryTerminal", 85),
        ("shower",           "IfcSanitaryTerminal", 85),
        ("bath",             "IfcSanitaryTerminal", 85),
        ("tap",              "IfcFlowTerminal",     80),
        ("sprinkler",        "IfcPipeSegment",      85),
        ("fire riser",       "IfcPipeSegment",      90),
        ("downpipe",         "IfcPipeSegment",      85),
        ("drain",            "IfcPipeSegment",      80),
        ("pipe",             "IfcPipeSegment",      75),
        ("duct",             "IfcDuctSegment",      80),
        ("grille",           "IfcAirTerminal",      80),
        ("diffuser",         "IfcAirTerminal",      80),
        ("air terminal",     "IfcAirTerminal",      90),
        ("vav",              "IfcDuctFitting",      85),
        ("damper",           "IfcDuctFitting",      85),
        ("pump",             "IfcFlowMovingDevice", 80),
        ("fan",              "IfcFlowMovingDevice", 75),
        ("light",            "IfcLightFixture",     75),
        ("luminaire",        "IfcLightFixture",     80),
        ("downlight",        "IfcLightFixture",     85),
        ("spotlight",        "IfcLightFixture",     85),
        ("pendant",          "IfcLightFixture",     80),
        ("switch",           "IfcElectricDistributionBoard",75),
        ("socket",           "IfcElectricDistributionBoard",70),
        ("panel board",      "IfcElectricDistributionBoard",85),
        ("db",               "IfcElectricDistributionBoard",70),
        ("sensor",           "IfcSensor",           80),
        ("detector",         "IfcFireSuppressionTerminal",80),
        ("fire alarm",       "IfcAlarm",            85),
        ("alarm",            "IfcAlarm",            75),
        // Transport
        ("lift",             "IfcTransportElement", 85),
        ("elevator",         "IfcTransportElement", 90),
        ("escalator",        "IfcTransportElement", 90),
        ("travellator",      "IfcTransportElement", 90),
        ("moving walk",      "IfcTransportElement", 90),
        ("travelator",       "IfcTransportElement", 90),
        // Civil / Infrastructure
        ("bridge deck",      "IfcSlab",            100),
        ("bridge girder",    "IfcBeam",            100),
        ("abutment",         "IfcWall",            100),
        ("pylon",            "IfcColumn",          100),
        ("kerb",             "IfcCovering",         80),
        ("pavement",         "IfcCovering",         80),
        ("road",             "IfcSlab",             75),
        // Spaces and Rooms
        ("car park",         "IfcSpace",            90),
        ("carpark",          "IfcSpace",            90),
        ("parking",          "IfcSpace",            85),
        ("plant room",       "IfcSpace",            85),
        ("plantroom",        "IfcSpace",            85),
        ("void",             "IfcSpace",            75),
        ("atrium",           "IfcSpace",            75),
        ("lobby",            "IfcSpace",            70),
        ("corridor",         "IfcSpace",            70),
        ("room",             "IfcSpace",            65),
        ("space",            "IfcSpace",            60),
        // Furniture
        ("chair",            "IfcFurniture",        80),
        ("desk",             "IfcFurniture",        80),
        ("table",            "IfcFurniture",        75),
        ("shelf",            "IfcFurniture",        75),
        ("cabinet",          "IfcFurniture",        70),
        ("sofa",             "IfcFurniture",        80),
        ("bed",              "IfcFurniture",        80),
        ("wardrobe",         "IfcFurniture",        80),
    };

    public List<string> GetPermittedPredefinedTypes(string ifcClass)
    {
        return _predefinedTypes.TryGetValue(ifcClass.ToUpperInvariant(), out var types)
            ? types
            : new List<string>();
    }

    public string SuggestEntityClass(string name, string objectType)
    {
        var combined = $"{name} {objectType}".ToLowerInvariant();

        // Sort by priority descending so more specific terms win
        string bestSuggestion = string.Empty;
        int bestPriority = -1;

        foreach (var (keyword, cls, priority) in _suggestions.OrderByDescending(s => s.Priority))
        {
            if (priority <= bestPriority) continue;  // can't improve
            if (combined.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                bestSuggestion = cls;
                bestPriority   = priority;
                if (priority >= 100) break;  // exact match, no need to continue
            }
        }
        return bestSuggestion;
    }

    public bool IsKnownIfcClass(string ifcClass) =>
        _predefinedTypes.ContainsKey(ifcClass.ToUpperInvariant());
}
