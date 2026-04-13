// VERIFIQ v2.0 - BIM Platform Context Library
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// Source: IFC+SG Industry Mapping December 2025 (BCA/GovTech, COP3.1 Edition)
// All 81 identified components with their BIM platform representations.
//
// Platform note on ArchiCAD:
//   In ArchiCAD, the IFC entity is determined by the classification code assigned
//   in the Classification Manager, NOT the native element type.
//   Example: ArchiCAD Slab with classification "A-WAL-EXW-01" → exports as IfcWall.
//   Each ArchiCAD element has exactly ONE classification → ONE IFC entity.
//   VERIFIQ validates the EXPORTED IFC entity + classification combination.
//
// Platform note on Revit:
//   Revit elements are categorised by Family/Category which maps to an IFC entity
//   via the IFC Export Settings and IFC+SG shared parameters.
//   Use the CORENET-X Revit IFC+SG configuration files from go.gov.sg/ifcsg.
//
// Platform note on Tekla:
//   Tekla Structures exports structural elements natively. Use the IFC+SG
//   export settings available on go.gov.sg/ifcsg for correct property export.
//
// Platform note on Bentley OpenBuildings:
//   Use the IFC+SG configuration and shared parameter files from go.gov.sg/ifcsg.

using VERIFIQ.Core.Enums;

namespace VERIFIQ.Rules.Common;

public sealed class ComponentPlatformContext
{
    public string ComponentName   { get; init; } = "";
    public string IfcEntity       { get; init; } = "";
    public string[] Disciplines   { get; init; } = [];
    public string RevitCategory   { get; init; } = "";
    public string RevitFamilyType { get; init; } = "";
    public string ArchiCadTool    { get; init; } = "";
    public string ArchiCadNote    { get; init; } = "";
    public string TeklaComponent  { get; init; } = "";
    public string BentleyTool     { get; init; } = "";
    public string IFCNote         { get; init; } = "";
    public string ResourceKitLink { get; init; } = "https://go.gov.sg/ifcsg";
}

public static class BimPlatformContextLibrary
{
    public static readonly IReadOnlyList<ComponentPlatformContext> All = Build();

    public static ComponentPlatformContext? Find(string ifcEntity, string? componentName = null)
    {
        var upper = ifcEntity.ToUpperInvariant();
        if (componentName != null)
            return All.FirstOrDefault(c =>
                c.IfcEntity.Equals(ifcEntity, StringComparison.OrdinalIgnoreCase) &&
                c.ComponentName.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                ?? All.FirstOrDefault(c => c.IfcEntity.Equals(ifcEntity, StringComparison.OrdinalIgnoreCase));
        return All.FirstOrDefault(c => c.IfcEntity.Equals(ifcEntity, StringComparison.OrdinalIgnoreCase));
    }

    private static List<ComponentPlatformContext> Build() => [
        new() { ComponentName="Accessible Route",           IfcEntity="IfcSpace",                    Disciplines=["ARC"],
            RevitCategory="Areas", ArchiCadTool="Zone",
            ArchiCadNote="In ArchiCAD: create a Zone element, assign IFC+SG classification for accessible route via Classification Manager.",
            TeklaComponent="N/A - use IfcSpace workaround", BentleyTool="Space",
            IFCNote="IfcSpace.ACCESSIBLEROUTE subtype. Width property ≥1200mm required by BCA Accessibility 2025." },

        new() { ComponentName="Beam",                       IfcEntity="IfcBeam",                     Disciplines=["STR"],
            RevitCategory="Structural Framing",
            ArchiCadTool="Beam",
            ArchiCadNote="In ArchiCAD: use Beam tool. Assign structural IFC properties via IFC+SG translator. BeamSpanType required.",
            TeklaComponent="Beam / Concrete Beam", BentleyTool="Beam",
            IFCNote="RC beams: include all reinforcement parameters (TopLeft/Middle/Right, BottomLeft/Middle/Right, Stirrups). Steel beams: MemberSection + SectionFabricationMethod." },

        new() { ComponentName="Block Name",                 IfcEntity="IfcSite",                     Disciplines=["ARC"],
            RevitCategory="Project Information / N/A",
            ArchiCadTool="IFC Project Manager",
            ArchiCadNote="In ArchiCAD: set block name in IFC Project Manager > IfcSite properties.",
            TeklaComponent="N/A", BentleyTool="Floor Manager",
            IFCNote="Block name stored as IfcSite.Name property." },

        new() { ComponentName="Borehole",                   IfcEntity="IfcBuildingElementProxy",     Disciplines=["STR"],
            RevitCategory="Generic Models",
            ArchiCadTool="Object",
            ArchiCadNote="In ArchiCAD: model as GDL object or morph. Classify as Borehole in Classification Manager → exports as IfcBuildingElementProxy.",
            TeklaComponent="N/A", BentleyTool="Object",
            IFCNote="IfcBuildingElementProxy with SGPset_ containing depth, termination level, SPT values." },

        new() { ComponentName="Breeching Inlet",            IfcEntity="IfcFireSuppressionTerminal",  Disciplines=["MEP"],
            RevitCategory="Plumbing Fixtures",
            ArchiCadTool="Pipe Flow Terminal",
            ArchiCadNote="In ArchiCAD: use MEP pipe flow terminal. Classify as fire system component.",
            TeklaComponent="N/A", BentleyTool="Equipment / Fire Protection",
            IFCNote="IfcFireSuppressionTerminal.BREECHINGINLET. Must also be part of dry/wet riser system." },

        new() { ComponentName="Building Storey",            IfcEntity="IfcBuildingStorey",           Disciplines=["ARC"],
            RevitCategory="Levels",
            ArchiCadTool="Storey",
            ArchiCadNote="ArchiCAD storeys export automatically as IfcBuildingStorey. Ensure SVY21 z-values are set correctly for geo-referencing.",
            TeklaComponent="N/A", BentleyTool="Floor",
            IFCNote="No IFC+SG properties required. All disciplines must align storey naming and z-values for federation." },

        new() { ComponentName="Ceiling",                    IfcEntity="IfcCovering",                 Disciplines=["ARC"],
            RevitCategory="Ceilings",
            ArchiCadTool="Ceiling",
            ArchiCadNote="In ArchiCAD: use Ceiling tool. Assign FireRating in IFC+SG properties. Material parameter required.",
            TeklaComponent="N/A", BentleyTool="Slab",
            IFCNote="IfcCovering.CEILING. FireRating in hours (0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4)." },

        new() { ComponentName="Column",                     IfcEntity="IfcColumn",                   Disciplines=["STR"],
            RevitCategory="Structural Columns",
            ArchiCadTool="Column",
            ArchiCadNote="In ArchiCAD: use Column tool. For structural columns, assign SGPset_ via IFC+SG translator. StartingStorey and EndStorey required.",
            TeklaComponent="Concrete Column", BentleyTool="Column",
            IFCNote="RC: Breadth, Width, MainRebar, Stirrups. Steel: MemberSection, SectionFabricationMethod, ConnectionType." },

        new() { ComponentName="Control Element",            IfcEntity="IfcUnitaryControlElement",    Disciplines=["MEP"],
            RevitCategory="Electrical Equipment",
            ArchiCadTool="Flow Equipment",
            ArchiCadNote="In ArchiCAD: use MEP flow equipment. Assign IFC subtype CONTROLPANEL.",
            TeklaComponent="N/A", BentleyTool="Equipment",
            IFCNote="IfcUnitaryControlElement.CONTROLPANEL. Purpose and PWCS_Flushing properties." },

        new() { ComponentName="Culvert / Drains",           IfcEntity="IfcCivilElement",             Disciplines=["MEP","STR"],
            RevitCategory="Generic Models",
            ArchiCadTool="Object",
            ArchiCadNote="In ArchiCAD: classify drainage element with drainage classification code → exports as IfcCivilElement.",
            TeklaComponent="Slab, Panel", BentleyTool="Drains & Basins",
            IFCNote="IfcCivilElement with subtype COMMONDRAIN, CULVERT etc. Sanitary drain-lines may be schematic/2D." },

        new() { ComponentName="Curtain Wall",               IfcEntity="IfcCurtainWall",              Disciplines=["ARC"],
            RevitCategory="Curtain Systems",
            ArchiCadTool="Curtain Wall",
            ArchiCadNote="In ArchiCAD: use Curtain Wall tool. IFC+SG COP3 requires no properties for IfcCurtainWall.",
            TeklaComponent="N/A", BentleyTool="Curtain Wall",
            IFCNote="IfcCurtainWall. No IFC+SG properties required per COP3.1." },

        new() { ComponentName="Damper",                     IfcEntity="IfcDamper",                   Disciplines=["MEP"],
            RevitCategory="Duct Accessories",
            ArchiCadTool="Object",
            ArchiCadNote="In ArchiCAD: model as generic object, classify as damper component.",
            TeklaComponent="N/A", BentleyTool="Object",
            IFCNote="IfcDamper subtypes: FIREDAMPER, FIRESMOKEDAMPER, SMOKEDAMPER. FireRating required." },

        new() { ComponentName="Door",                       IfcEntity="IfcDoor",                     Disciplines=["ARC"],
            RevitCategory="Doors",
            ArchiCadTool="Door",
            ArchiCadNote="In ArchiCAD: use Door tool. IFC+SG door properties set via IFC+SG translator/object properties.",
            TeklaComponent="N/A", BentleyTool="Door",
            IFCNote="ClearWidth, FireExit, FireRating, OperationType required. BCA Accessibility: main entrance ≥850mm clear width." },

        new() { ComponentName="Escalator",                  IfcEntity="IfcTransportElement",         Disciplines=["ARC"],
            RevitCategory="Mechanical Equipment",
            ArchiCadTool="Transport Element",
            ArchiCadNote="In ArchiCAD: use transport element object. Classify as escalator.",
            TeklaComponent="N/A", BentleyTool="Equipment",
            IFCNote="IfcTransportElement.ESCALATOR." },

        new() { ComponentName="Footing",                    IfcEntity="IfcFooting",                  Disciplines=["STR"],
            RevitCategory="Structural Foundations",
            ArchiCadTool="Footing",
            ArchiCadNote="In ArchiCAD: use Footing tool. Assign structural IFC+SG properties. ConstructionMethod and MaterialGrade required.",
            TeklaComponent="Concrete Footing", BentleyTool="Footing",
            IFCNote="IfcFooting subtypes: STRIP_FOOTING, CAISSON_FOUNDATION, PILE_CAP." },

        new() { ComponentName="Landscape Plants",           IfcEntity="IfcGeographicElement",        Disciplines=["External Works","ARC"],
            RevitCategory="Planting",
            ArchiCadTool="Object",
            ArchiCadNote="In ArchiCAD: use GDL object for trees. Classify as landscape plant. Properties: Species, Girth, Height, Status.",
            TeklaComponent="N/A", BentleyTool="Object",
            IFCNote="Subtypes: LANDSCAPE_TREE, LANDSCAPE_PALM, LANDSCAPE_HEDGE. All must have Status value." },

        new() { ComponentName="Lift",                       IfcEntity="IfcTransportElement",         Disciplines=["ARC"],
            RevitCategory="Specialty Equipment / Parking",
            ArchiCadTool="Object / Transport Element",
            ArchiCadNote="In ArchiCAD: use transport element. Set lift type, barrier-free accessibility, clear dimensions.",
            TeklaComponent="N/A", BentleyTool="Equipment / Object",
            IFCNote="IfcTransportElement.LIFT or CARLIFT. BarrierFreeAccessibility, FireFightingLift required for LIFT subtype." },

        new() { ComponentName="Parking Lot",                IfcEntity="IfcBuildingElementProxy",     Disciplines=["ARC"],
            RevitCategory="Generic Models / Parking",
            ArchiCadTool="Object",
            ArchiCadNote="In ArchiCAD: classify parking elements with LTA/BCA parking codes. Exports as IfcBuildingElementProxy with parking subtypes.",
            TeklaComponent="N/A", BentleyTool="Object",
            IFCNote="Subtypes: CARLOT, LORRYLOT, MOTORCYCLELOT, BICYCLELOT. Length and Width from BIM model dimensions." },

        new() { ComponentName="Pile",                       IfcEntity="IfcPile",                     Disciplines=["STR"],
            RevitCategory="Structural Foundations",
            ArchiCadTool="Object",
            ArchiCadNote="In ArchiCAD: model piles as custom GDL objects or columns with pile classification. All DA1-1 and DA1-2 capacity/load properties required.",
            TeklaComponent="Concrete Column", BentleyTool="Base Plate / Floor Manager",
            IFCNote="IfcPile. CutOffLevel_SHD, ToeLevel_SHD in Singapore Height Datum (SHD) format." },

        new() { ComponentName="Pipes / Ducts",              IfcEntity="IfcPipeSegment",              Disciplines=["MEP"],
            RevitCategory="Pipes",
            ArchiCadTool="Pipe / Pipe Fitting",
            ArchiCadNote="In ArchiCAD: use MEP pipe segments from MEP Modeler. Ensure SystemType is set for every pipe.",
            TeklaComponent="N/A", BentleyTool="Pipe Accessory",
            IFCNote="InnerDiameter, Gradient, and SystemType required. Sanitary drain-lines may be 2D schematic." },

        new() { ComponentName="Railing",                    IfcEntity="IfcRailing",                  Disciplines=["STR","ARC"],
            RevitCategory="Railings",
            ArchiCadTool="Railing",
            ArchiCadNote="In ArchiCAD: use Railing tool. Assign discipline and structural properties as needed.",
            TeklaComponent="N/A / Component", BentleyTool="Railing",
            IFCNote="IfcRailing. BarrierFreeAccessibility if accessible handrail." },

        new() { ComponentName="Ramp",                       IfcEntity="IfcRamp",                     Disciplines=["ARC"],
            RevitCategory="Ramps",
            ArchiCadTool="Ramp",
            ArchiCadNote="In ArchiCAD: use Ramp tool or Stair/Ramp option. Assign accessibility properties.",
            TeklaComponent="Item", BentleyTool="Object",
            IFCNote="IfcRamp. BarrierFreeAccessibility required if accessible ramp." },

        new() { ComponentName="Roof",                       IfcEntity="IfcRoof",                     Disciplines=["ARC"],
            RevitCategory="Roofs",
            ArchiCadTool="Roof / Slab",
            ArchiCadNote="In ArchiCAD: Roof tool or Slab with roof classification. URA roofscape rules apply.",
            TeklaComponent="N/A / Slab", BentleyTool="Roof / Slab",
            IFCNote="IfcRoof or IfcSlab.ROOF. Green roof parameters required for NParks." },

        new() { ComponentName="Sanitary Appliances",        IfcEntity="IfcSanitaryTerminal",         Disciplines=["MEP","ARC"],
            RevitCategory="Plumbing Fixtures",
            ArchiCadTool="Pipe Flow Terminal",
            ArchiCadNote="In ArchiCAD: use MEP Pipe Flow Terminal. Classify by sanitary type (WC, basin, etc.). SystemType required for PUB WELS.",
            TeklaComponent="N/A", BentleyTool="Fixture",
            IFCNote="Subtypes: BATH, BIDET, SHOWER, URINAL, WASHHANDBASIN, WATERCLOSET. WaterUsagePerMonth for PUB." },

        new() { ComponentName="Slab",                       IfcEntity="IfcSlab",                     Disciplines=["STR"],
            RevitCategory="Floors",
            ArchiCadTool="Slab",
            ArchiCadNote="In ArchiCAD: use Slab tool. SlabType (One way / Two way / Cantilever / Flat slab) is mandatory for all slabs. ShelterUsage for civil defence shelters.",
            TeklaComponent="Concrete Slab / Slab", BentleyTool="Slab",
            IFCNote="IfcSlab subtypes: FLOOR, LANDING. All reinforcement parameters required for structural slabs." },

        new() { ComponentName="Space (Area Scheme)",        IfcEntity="IfcSpace",                    Disciplines=["ARC"],
            RevitCategory="Areas",
            ArchiCadTool="Zone",
            ArchiCadNote="In ArchiCAD: use Zone tool for GFA areas. Assign SGPset_SpaceArea_ properties. AGF_Name must match accepted list of 800+ values.",
            TeklaComponent="N/A", BentleyTool="Space",
            IFCNote="IfcSpace.AREA_GFA, AREA_LANDSCAPE etc. URA GFA calculations use these spaces." },

        new() { ComponentName="Space (Usage)",              IfcEntity="IfcSpace",                    Disciplines=["ARC"],
            RevitCategory="Rooms",
            ArchiCadTool="Zone",
            ArchiCadNote="In ArchiCAD: use Zone for rooms/spaces. SpaceName must match the 420-value COP3.1 list. OccupancyType restricted to 95 values.",
            TeklaComponent="N/A", BentleyTool="Space",
            IFCNote="IfcSpace.SPACE. SpaceName and OccupancyType required by URA/NEA/SCDF." },

        new() { ComponentName="Staircase",                  IfcEntity="IfcStair",                    Disciplines=["STR","ARC"],
            RevitCategory="Stairs",
            ArchiCadTool="Stair",
            ArchiCadNote="In ArchiCAD: use Stair tool. Assign ConstructionMethod and MaterialGrade. FireExit required for fire exit staircases.",
            TeklaComponent="Component", BentleyTool="Stair",
            IFCNote="IfcStair (structural model) + IfcStairFlight (flight properties). Space IfcSpace.SpaceName='external exit staircase' for SCDF." },

        new() { ComponentName="Tank",                       IfcEntity="IfcTank",                     Disciplines=["MEP"],
            RevitCategory="Mechanical Equipment",
            ArchiCadTool="Object",
            ArchiCadNote="In ArchiCAD: model tank as GDL object. IsPotable required for drinking water tanks.",
            TeklaComponent="N/A", BentleyTool="Object",
            IFCNote="Object tanks: IfcTank. RC tanks: IfcSpace with SpaceName matching tank type." },

        new() { ComponentName="Valve",                      IfcEntity="IfcValve",                    Disciplines=["MEP"],
            RevitCategory="Pipe Accessories",
            ArchiCadTool="Pipe In-line Flow Device",
            ArchiCadNote="In ArchiCAD: use MEP in-line flow device for valves. SystemType and SystemName required.",
            TeklaComponent="N/A", BentleyTool="Valve",
            IFCNote="IfcValve. Landing valves must also be in Wet/Dry Riser system." },

        new() { ComponentName="Wall",                       IfcEntity="IfcWall",                     Disciplines=["STR","ARC"],
            RevitCategory="Walls",
            ArchiCadTool="Wall",
            ArchiCadNote="In ArchiCAD: Wall tool. Classification code determines model: architectural classification → SGPset_WallCommon, structural classification → full 21-property SGPset_WallStructural. A slab element with wall classification exports as IfcWall.",
            TeklaComponent="Concrete Panel / Panel", BentleyTool="Wall",
            IFCNote="Architectural (PARAPET/RETAINING/BOUNDARY): ConstructionMethod + IsPartyWall. Structural (N.A.): 21 properties including all reinforcement data." },

        new() { ComponentName="Waste Terminal",             IfcEntity="IfcWasteTerminal",            Disciplines=["MEP"],
            RevitCategory="Pipe Accessories",
            ArchiCadTool="Pipe Flow Terminal",
            ArchiCadNote="In ArchiCAD: use MEP pipe flow terminal. Assign TradeEffluent for NEA.",
            TeklaComponent="N/A", BentleyTool="Fixture",
            IFCNote="IfcWasteTerminal subtypes: FLOORTRAP, FLOORWASTE, GULLYSUMP, GULLYTRAP." },

        new() { ComponentName="Water Meter",                IfcEntity="IfcFlowMeter",                Disciplines=["MEP"],
            RevitCategory="Pipe Accessories",
            ArchiCadTool="Pipe In-line Flow Device",
            ArchiCadNote="In ArchiCAD: use MEP in-line device. UnitNumber and Purpose required for PUB.",
            TeklaComponent="N/A", BentleyTool="Pipe Accessory",
            IFCNote="IfcFlowMeter.WATERMETER. WaterUsagePerMonth unit changed to m3/month (COP3 Dec 2025 change log)." },

        new() { ComponentName="Window",                     IfcEntity="IfcWindow",                   Disciplines=["ARC"],
            RevitCategory="Windows",
            ArchiCadTool="Window",
            ArchiCadNote="In ArchiCAD: use Window tool. PercentageOfOpening required for NEA natural ventilation calculations.",
            TeklaComponent="N/A", BentleyTool="Window",
            IFCNote="Subtypes: BAYWINDOW, VENTILATIONSLEEVE, LOUVRE, WINDOW, SKYLIGHT." },
    ];
}
