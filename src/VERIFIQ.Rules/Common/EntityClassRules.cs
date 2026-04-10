// VERIFIQ — Entity Class Rules + Auto-Classification Engine
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// Provides:
//   1. GetPermittedPredefinedTypes() — IFC4 schema enumeration validation
//   2. SuggestEntityClass()          — AI-style keyword classifier for proxies
//   3. IsKnownIfcClass()             — validates the element class exists in IFC4
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
        // ── ARCHITECTURAL ─────────────────────────────────────────────────────
        ["IFCWALL"] = new()
        {
            "STANDARD","SHEAR","PARAPET","PARTITIONING","PLUMBINGWALL",
            "MOVABLE","ELEMENTEDWALL","USERDEFINED","NOTDEFINED"
        },
        ["IFCWALLSTANDARDCASE"] = new()
        {
            "STANDARD","SHEAR","PARAPET","PARTITIONING","PLUMBINGWALL",
            "MOVABLE","ELEMENTEDWALL","USERDEFINED","NOTDEFINED"
        },
        ["IFCSLAB"] = new()
        {
            "FLOOR","ROOF","LANDING","BASESLAB","APPROACH_SLAB",
            "PAVING","WEARING","SIDEWALK","USERDEFINED","NOTDEFINED"
        },
        ["IFCDOOR"] = new() { "DOOR","GATE","TRAPDOOR","USERDEFINED","NOTDEFINED" },
        ["IFCWINDOW"] = new() { "WINDOW","SKYLIGHT","LIGHTDOME","USERDEFINED","NOTDEFINED" },
        ["IFCROOF"] = new()
        {
            "FLAT_ROOF","SHED_ROOF","GABLE_ROOF","HIP_ROOF","HIPPED_GABLE_ROOF",
            "GAMBREL_ROOF","MANSARD_ROOF","BARREL_ROOF","RAINBOW_ROOF","BUTTERFLY_ROOF",
            "PAVILION_ROOF","DOME_ROOF","FREEFORM","USERDEFINED","NOTDEFINED"
        },
        ["IFCSTAIR"] = new()
        {
            "STRAIGHT_RUN_STAIR","TWO_STRAIGHT_RUN_STAIR","QUARTER_WINDER_STAIR",
            "QUARTER_TURN_STAIR","HALF_WINDER_STAIR","HALF_TURN_STAIR",
            "SPIRAL_STAIR","DOUBLE_RETURN_STAIR","CURVED_RUN_STAIR","USERDEFINED","NOTDEFINED"
        },
        ["IFCSTAIRFLIGHT"] = new()
        {
            "STRAIGHT","WINDER","SPIRAL","CURVED","FREEFORM","USERDEFINED","NOTDEFINED"
        },
        ["IFCRAMP"] = new()
        {
            "STRAIGHT_RUN_RAMP","TWO_STRAIGHT_RUN_RAMP","QUARTER_TURN_RAMP",
            "HALF_TURN_RAMP","SPIRAL_RAMP","USERDEFINED","NOTDEFINED"
        },
        ["IFCRAMPFLIGHT"] = new() { "STRAIGHT","SPIRAL","USERDEFINED","NOTDEFINED" },
        ["IFCRAILING"] = new()
        {
            "HANDRAIL","GUARDRAIL","BALUSTRADE","FENCE","NOTDEFINED","USERDEFINED"
        },
        ["IFCCOLUMN"] = new() { "COLUMN","PILASTER","USERDEFINED","NOTDEFINED" },
        ["IFCBEAM"] = new()
        {
            "BEAM","JOIST","HOLLOWCORE","LINTEL","SPANDREL","T_BEAM",
            "EDGEBEAM","USERDEFINED","NOTDEFINED"
        },
        ["IFCFOOTING"] = new()
        {
            "CAISSON_FOUNDATION","FOOTING_BEAM","PAD_FOOTING","PILE_CAP",
            "STRIP_FOOTING","USERDEFINED","NOTDEFINED"
        },
        ["IFCPILE"] = new()
        {
            "BORED","DRIVEN","JETGROUTING","COHESION","FRICTION",
            "SUPPORT","USERDEFINED","NOTDEFINED"
        },
        ["IFCPLATE"] = new()
        {
            "CURTAIN_PANEL","SHEET","BASE_PLATE","FLANGE_PLATE","WEB_PLATE",
            "STIFFENER_PLATE","SPLICE_PLATE","GUSSET_PLATE","USERDEFINED","NOTDEFINED"
        },
        ["IFCMEMBER"] = new()
        {
            "BRACE","CHORD","COLLAR","MEMBER","MULLION","PLATE","PURLIN",
            "RAFTER","STRINGER","STRUT","STUD","USERDEFINED","NOTDEFINED"
        },
        ["IFCCOVERING"] = new()
        {
            "CEILING","FLOORING","CLADDING","ROOFING","INSULATION","MEMBRANE",
            "SLEEVING","WRAPPING","USERDEFINED","NOTDEFINED"
        },
        ["IFCCURTAINWALL"] = new() { "NOTDEFINED","USERDEFINED" },
        ["IFCSPACE"] = new()
        {
            "SPACE","PARKING","GFA","INTERNAL","EXTERNAL","USERDEFINED","NOTDEFINED"
        },

        // ── MEP / BUILDING SERVICES ───────────────────────────────────────────
        ["IFCPIPESEGMENT"] = new()
        {
            "CULVERT","FLEXIBLESEGMENT","GUTTER","SPOOL","USERDEFINED","NOTDEFINED"
        },
        ["IFCPIPEFITTING"] = new()
        {
            "BEND","CONNECTOR","ENTRY","EXIT","JUNCTION","OBSTRUCTION","TRANSITION","USERDEFINED","NOTDEFINED"
        },
        ["IFCDUCTSEGMENT"] = new()
        {
            "RIGIDSEGMENT","FLEXIBLESEGMENT","USERDEFINED","NOTDEFINED"
        },
        ["IFCDUCTFITTING"] = new()
        {
            "BEND","CONNECTOR","ENTRY","EXIT","JUNCTION","OBSTRUCTION","TRANSITION","USERDEFINED","NOTDEFINED"
        },
        ["IFCAIRTERMINAL"] = new()
        {
            "DIFFUSER","GRILLE","LOUVRE","REGISTER","USERDEFINED","NOTDEFINED"
        },
        ["IFCPUMPASSEMBLY"] = new() { "CIRCULATOR","ENDSUCTION","SPLITCASE","SUBMERSIBLEPUMP","SUMPPUMP","USERDEFINED","NOTDEFINED" },
        ["IFCFLOWTERMINAL"] = new() { "USERDEFINED","NOTDEFINED" },
        ["IFCFLOWSTORAGDEVICE"] = new() { "USERDEFINED","NOTDEFINED" },
        ["IFCSANITARYTERMINAL"] = new()
        {
            "BATH","BIDET","CISTERN","SHOWER","SINK","SANITARYFOUNTAIN",
            "TOILETPAN","URINAL","WASHHANDBASIN","WCSEAT","USERDEFINED","NOTDEFINED"
        },
        ["IFCLIGHTFIXTURE"] = new()
        {
            "POINTSOURCE","DIRECTIONSOURCE","SECURITYLIGHTING","USERDEFINED","NOTDEFINED"
        },
        ["IFCELECTRICAPPLIANCE"] = new()
        {
            "DISHWASHER","ELECTRICCOOKER","FREESTANDINGELECTRICHEATER",
            "FREESTANDINGFAN","FREESTANDINGWATERHEATER","FREEZER","FRIDGE",
            "HANDDRYER","KITCHENMACHINE","MICROWAVE","PHOTOCOPIER","WASHINGMACHINE","USERDEFINED","NOTDEFINED"
        },
        ["IFCFURNITURE"] = new()
        {
            "CHAIR","DESK","FILECABINET","SHELF","SOFA","BED","TABLE",
            "TECHNICALCABINET","USERDEFINED","NOTDEFINED"
        },
        ["IFCTRANSPORTELEMENT"] = new()
        {
            "ELEVATOR","ESCALATOR","MOVINGWALKWAY","CRANEWAY","LIFTINGDEVICE","USERDEFINED","NOTDEFINED"
        },
    };

    // ─── KEYWORD → IFC CLASS SUGGESTION TABLE (120+ terms, priority-ordered) ──
    // Longer / more specific terms are listed before shorter ones to avoid
    // false positives (e.g. "stair" before "air").
    // Source: IFC4 schema, CORENET-X Industry Mapping, NBeS Classification.
    private static readonly (string Keyword, string IfcClass, int Priority)[] _suggestions =
    {
        // Structural — high priority (very specific terms first)
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
