// VERIFIQ v2.0 - IFC Entity + SubType Property Database
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// ─── COMPREHENSIVE IFC+SG ENTITY PROPERTY DATABASE ────────────────────────────
//
// Source: CORENET-X Code of Practice 3.1 Edition (December 2025), Section 4
// BCA IFC+SG Industry Mapping December 2025 (COP3.1, 833 rows, 81 components)
//
// This database encodes for every IFC Entity + SubType combination:
//   - All IFC+SG required properties (SGPset_ and Pset_)
//   - Property Type (Text, Boolean, Length, Integer, Real)
//   - Type of Elements (which element subtypes the property applies to)
//   - Unit (mm, m, kN, hr, m2, etc.)
//   - Input Limitation (whether accepted values are restricted)
//   - Accepted values list (for Input Limitation = Yes)
//   - Example values
//   - Regulatory agency
//   - BIM platform representations: Revit, ArchiCAD, Tekla, Bentley
//   - Discipline: ARC, STR, MEP, External Works
//
// ArchiCAD classification note:
//   In ArchiCAD, the IFC entity is determined by the classification code assigned
//   in the Classification Manager, NOT by the native element type.
//   Example: A slab element assigned classification "Wall" exports as IfcWall.
//   VERIFIQ validates based on the exported IFC entity + classification combination.
//   Different disciplines (ARC/STR/MEP) model separately and federate via IFC.

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Rules.Common;

/// <summary>
/// Represents a single IFC+SG property definition extracted from the COP3.1 tables.
/// </summary>
public sealed class IfcSgPropertyDef
{
    public int    SN              { get; init; }
    public string PropertyName   { get; init; } = "";
    public string PropertyType   { get; init; } = "Text";  // Text/Boolean/Length/Integer/Real
    public string TypeOfElements { get; init; } = "";      // "All walls", "RC beam", etc.
    public string Unit           { get; init; } = "";      // mm, m, kN, hr, m2, etc.
    public bool   InputLimitation{ get; init; }            // true = must match accepted values
    public string[] AcceptedValues{ get; init; } = [];     // populated when InputLimitation = true
    public string Example        { get; init; } = "";
    public string PropertySetName{ get; init; } = "";      // SGPset_ or Pset_
    public bool   IsRequired     { get; init; } = true;
    public SgAgency Agency       { get; init; } = SgAgency.BCA;
    public string Regulation     { get; init; } = "IFC+SG COP3.1";
}

/// <summary>
/// A key into the entity+subtype property table.
/// </summary>
public readonly struct EntitySubTypeKey : IEquatable<EntitySubTypeKey>
{
    public string Entity  { get; }
    public string SubType { get; }  // "N.A." means applies to any subtype
    public EntitySubTypeKey(string entity, string subType)
    { Entity = entity.ToUpperInvariant(); SubType = subType.ToUpperInvariant(); }
    public bool Equals(EntitySubTypeKey other) => Entity == other.Entity && SubType == other.SubType;
    public override bool Equals(object? obj) => obj is EntitySubTypeKey k && Equals(k);
    public override int GetHashCode() => HashCode.Combine(Entity, SubType);
    public override string ToString() => $"{Entity}.{SubType}";
}

/// <summary>
/// Represents one identified component block from the COP3.1 "By IFC Representation" tables.
/// </summary>
public sealed class EntityPropertyBlock
{
    public string ComponentName       { get; init; } = "";
    public string IfcEntity          { get; init; } = "";
    public string[] SubTypes         { get; init; } = [];    // multiple subtypes share same property list
    public string DisciplineContext  { get; init; } = "";    // "Architectural", "Structural", "MEP"
    public string[] Disciplines      { get; init; } = [];    // ARC, STR, MEP, External Works
    public PlatformRepresentation Platforms { get; init; } = new();
    public IfcSgPropertyDef[] Properties { get; init; } = [];
    public string Notes              { get; init; } = "";
    public string CopSection         { get; init; } = "";    // Section 4, page reference
}

public sealed class PlatformRepresentation
{
    public string[] Revit   { get; init; } = [];
    public string[] ArchiCAD{ get; init; } = [];
    public string[] Tekla   { get; init; } = [];
    public string[] Bentley { get; init; } = [];
}

/// <summary>
/// Complete IFC+SG property database. Provides entity+subtype → property definitions.
/// All data derived from COP3.1 Section 4 and IFC+SG Industry Mapping Dec 2025.
/// </summary>
public static class IFCEntityPropertyDatabase
{
    private static readonly Dictionary<string, EntityPropertyBlock[]> _byEntity
        = BuildDatabase().GroupBy(b => b.IfcEntity.ToUpperInvariant())
                         .ToDictionary(g => g.Key, g => g.ToArray());

    private static readonly List<EntityPropertyBlock> _all = BuildDatabase();

    // ── PUBLIC API ────────────────────────────────────────────────────────────

    public static IReadOnlyList<EntityPropertyBlock> All => _all;

    public static EntityPropertyBlock[] GetByEntity(string ifcEntity)
    {
        var key = ifcEntity.ToUpperInvariant();
        return _byEntity.TryGetValue(key, out var blocks) ? blocks : [];
    }

    /// <summary>
    /// Get all required properties for an entity+subtype combination.
    /// Falls back to N.A. subtype if no specific subtype match.
    /// </summary>
    public static IfcSgPropertyDef[] GetProperties(string ifcEntity, string subType = "N.A.")
    {
        var blocks = GetByEntity(ifcEntity);
        if (!blocks.Any()) return [];
        var subUpper = subType.ToUpperInvariant();

        // Exact subtype match first
        var exact = blocks.FirstOrDefault(b =>
            b.SubTypes.Any(s => s.ToUpperInvariant() == subUpper));
        if (exact != null) return exact.Properties;

        // N.A. fallback (applies to all subtypes)
        var fallback = blocks.FirstOrDefault(b =>
            b.SubTypes.Contains("N.A.", StringComparer.OrdinalIgnoreCase));
        return fallback?.Properties ?? [];
    }

    /// <summary>
    /// Get platform representation guidance for a component.
    /// </summary>
    public static PlatformRepresentation? GetPlatformContext(string ifcEntity, string? componentName = null)
    {
        var blocks = GetByEntity(ifcEntity);
        if (!blocks.Any()) return null;
        var block = componentName != null
            ? blocks.FirstOrDefault(b => b.ComponentName.Equals(componentName, StringComparison.OrdinalIgnoreCase))
              ?? blocks[0]
            : blocks[0];
        return block.Platforms;
    }

    public static string[] GetSubTypesForEntity(string ifcEntity)
        => GetByEntity(ifcEntity).SelectMany(b => b.SubTypes).Distinct().ToArray();

    // ── ACCEPTED VALUES CONSTANTS ─────────────────────────────────────────────
    private static readonly string[] CONSTRUCTION_METHODS =
        ["CIS", "PC", "PT (Pre)", "PT (Post)", "PF", "PPVC", "Spun"];
    private static readonly string[] MATERIAL_GRADES =
        ["C12/15","C20/25","C25/30","C30/37","C32/40","C35/45","C40/50",
         "C50/60","C55/67","C60/75","C70/85","C80/95",
         "S235","S275","S355","S460"];
    private static readonly string[] REBAR_GRADES = ["500A","500B","500C","600A","600B","600C"];
    private static readonly string[] CONNECTION_TYPES = ["Pinned","Fixed","Free"];
    private static readonly string[] SECTION_FABRICATION = ["Hot rolled","Cold formed"];
    private static readonly string[] SLAB_TYPES =
        ["One way","Two way","Cantilever","Flat slab","Flat slab with drop panel","Transfer Slab"];
    private static readonly string[] FIRE_RATINGS = ["0.5","1","1.5","2","2.5","3","3.5","4"];
    private static readonly string[] BEAM_SPAN_TYPES = ["Single","End","Interior","Cantilever"];
    private static readonly string[] PILE_TYPES = ["Driven","Bored","Jacked in","Spun"];
    private static readonly string[] STIRRUPS_TYPES = ["Normal","U","C","CL","Torsion"];
    private static readonly string[] STATUS_VALUES =
        ["Existing","Proposed","To Be Removed","Abandoned","New","Temporary","Demolished"];
    private static readonly string[] VENTILATION_MODES = ["Natural Ventilation","Air Conditioning Mechanical Ventilation"];
    private static readonly string[] BOOL_VALUES = ["TRUE","FALSE"];

    // ── DATABASE BUILD ────────────────────────────────────────────────────────
    private static List<EntityPropertyBlock> BuildDatabase()
    {
        var db = new List<EntityPropertyBlock>();

        // ─── IfcWall ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Wall (Architectural - Parapet/Retaining/Boundary)",
            IfcEntity = "IfcWall",
            SubTypes = ["PARAPET","RETAININGWALL","BOUNDARYWALL"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Walls"], ArchiCAD = ["Wall"], Tekla = ["Concrete Panel","Panel"], Bentley = ["Wall"] },
            Notes = "Parapet, retaining walls and boundary walls. ArchiCAD: assign wall classification code to export as IfcWall.",
            CopSection = "COP3.1 Section 4 p.435",
            Properties = [
                P(1,"SGPset_WallCommon","ConstructionMethod","Text","All walls","",true,CONSTRUCTION_METHODS,"CIS","BCA"),
                P(2,"SGPset_WallCommon","IsPartyWall","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
            ]
        });

        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Wall (Structural)",
            IfcEntity = "IfcWall",
            SubTypes = ["N.A."],
            DisciplineContext = "Structural",
            Disciplines = ["STR"],
            Platforms = new() {
                Revit = ["Walls"], ArchiCAD = ["Wall"], Tekla = ["Concrete Panel","Panel"], Bentley = ["Wall"] },
            Notes = "Structural wall including RC walls, precast, PC. 21 parameters. Household shelter walls need ShelterUsage=TRUE.",
            CopSection = "COP3.1 Section 4 p.435-436",
            Properties = [
                P(1, "SGPset_WallStructural","ArrangementType","Text","","",true,["Multi-Tier","Single-Tier"],"Multi-Tier","BCA"),
                P(2, "SGPset_WallStructural","BeamFacade","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(3, "SGPset_WallCommon","ConstructionMethod","Text","All walls","",true,CONSTRUCTION_METHODS,"CIS","BCA"),
                P(4, "SGPset_WallStructural","DoubleBayFacade","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(5, "SGPset_WallStructural","HorizontalRebar","Text","All walls","",false,[],"2H20-150","BCA"),
                P(6, "Pset_WallCommon","IsExternal","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(7, "Pset_WallCommon","LoadBearing","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(8, "SGPset_WallCommon","Mark","Text","All walls","",false,[],"W1, W2","BCA"),
                P(9, "SGPset_WallStructural","MaterialGrade","Text","All walls","",true,MATERIAL_GRADES,"C32/40","BCA"),
                P(10,"SGPset_WallStructural","MechanicalConnectionType","Text","","",false,[],"flexible loops, grouted sleeves, spiral connector","BCA"),
                P(11,"SGPset_WallStructural","PrefabricatedReinforcementCage","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(12,"SGPset_WallStructural","PrefinishedFacade","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(13,"SGPset_WallCommon","ReferTo2DDetail","Text","When required / relevant","",false,[],"Dwg Number","BCA"),
                P(14,"SGPset_WallStructural","ReinforcementSteelGrade","Text","All walls","",true,REBAR_GRADES,"500B","BCA"),
                P(15,"SGPset_WallCommon","ShelterUsage","Boolean","When required / relevant","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(16,"SGPset_WallStructural","Stirrups","Text","When required / relevant","",false,[],"H10-150-300","BCA"),
                P(17,"SGPset_WallStructural","StirrupsType","Text","When required / relevant","",true,STIRRUPS_TYPES,"CL","BCA"),
                P(18,"SGPset_WallCommon","Thickness","Length","All walls","mm",false,[],"300","BCA"),
                P(19,"SGPset_WallStructural","VerticalRebar","Text","All walls","",false,[],"H32-150+H25-150","BCA"),
                P(20,"SGPset_WallStructural","WorkingLoad_DA1-1","Integer","When required / relevant","kN",false,[],"1234","BCA"),
                P(21,"SGPset_WallStructural","WorkingLoad_DA1-2","Integer","When required / relevant","kN",false,[],"1234","BCA"),
            ]
        });

        // ─── IfcBeam ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Beam",
            IfcEntity = "IfcBeam",
            SubTypes = ["N.A."],
            DisciplineContext = "Structural",
            Disciplines = ["STR"],
            Platforms = new() {
                Revit = ["Structural Framing"], ArchiCAD = ["Beam"], Tekla = ["Beam","Concrete Beam"], Bentley = ["Beam"] },
            Notes = "RC beams use reinforcement parameters; steel beams use MemberSection/SectionFabricationMethod. ArchiCAD: Beam classification exports as IfcBeam.",
            CopSection = "COP3.1 Section 4 p.260-261",
            Properties = [
                P(1, "SGPset_Beam","BeamSpanType","Text","All beams","",true,BEAM_SPAN_TYPES,"Single","BCA"),
                P(2, "SGPset_BeamReinforcement","BottomLeft","Text","RC beam","",false,[],"3H25","BCA"),
                P(3, "SGPset_BeamReinforcement","BottomMiddle","Text","RC beam","",false,[],"3H32+3H25","BCA"),
                P(4, "SGPset_BeamReinforcement","BottomRight","Text","RC beam","",false,[],"3H25","BCA"),
                P(5, "SGPset_Beam","ConstructionMethod","Text","All beams","",true,CONSTRUCTION_METHODS,"CIS","BCA"),
                P(6, "SGPset_BeamDimension","Depth","Length","RC beam","mm",false,[],"600","BCA"),
                P(7, "SGPset_SteelConnection","LeftConnectionDetail","Text","Steel beam","",false,[],"Detail 1","BCA"),
                P(8, "SGPset_SteelConnection","LeftConnectionType","Text","Steel beam","",true,CONNECTION_TYPES,"Pinned","BCA"),
                P(9, "SGPset_Beam","Mark","Text","All beams","",false,[],"HB1, VB1, B1","BCA"),
                P(10,"SGPset_Beam","MaterialGrade","Text","All beams","",true,MATERIAL_GRADES,"C32/40","BCA"),
                P(11,"SGPset_BeamStructural","MechanicalConnectionType","Text","","",true,[],"telescopic beam connector, grouted sleeves","BCA"),
                P(12,"SGPset_BeamDimension","MemberSection","Text","Steel beam","",false,[],"RHS600x30x4, CHS500x3.0","BCA"),
                P(13,"SGPset_Beam","PrefabricatedReinforcementCage","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(14,"SGPset_Beam","ReferTo2DDetail","Text","When required / relevant","",false,[],"Dwg Number","BCA"),
                P(15,"SGPset_Beam","ReinforcementSteelGrade","Text","RC beam","",true,REBAR_GRADES,"500B","BCA"),
                P(16,"SGPset_SteelConnection","RightConnectionDetail","Text","Steel beam","",false,[],"Detail 1","BCA"),
                P(17,"SGPset_SteelConnection","RightConnectionType","Text","Steel beam","",true,CONNECTION_TYPES,"Pinned","BCA"),
                P(18,"SGPset_Beam","SectionFabricationMethod","Text","Steel beam","",true,SECTION_FABRICATION,"Hot rolled","BCA"),
                P(19,"SGPset_BeamReinforcement","SideBar","Text","When required / relevant","",false,[],"H13-250","BCA"),
                P(20,"SGPset_BeamReinforcement","StirrupsLeft","Text","RC beam","",false,[],"4H13-300","BCA"),
                P(21,"SGPset_BeamReinforcement","StirrupsMiddle","Text","RC beam","",false,[],"4H13-300","BCA"),
                P(22,"SGPset_BeamReinforcement","StirrupsRight","Text","RC beam","",false,[],"4H13-300","BCA"),
                P(23,"SGPset_Beam","StirrupsType","Text","When required/relevant","",true,STIRRUPS_TYPES,"Normal","BCA"),
                P(24,"SGPset_BeamReinforcement","TopLeft","Text","RC beam","",false,[],"3H25","BCA"),
                P(25,"SGPset_BeamReinforcement","TopMiddle","Text","RC beam","",false,[],"3H32+3H25","BCA"),
                P(26,"SGPset_BeamReinforcement","TopRight","Text","RC beam","",false,[],"3H25","BCA"),
                P(27,"SGPset_BeamDimension","Width","Length","RC beam","mm",false,[],"300","BCA"),
                P(28,"SGPset_Beam","WorkingLoad_DA1-1","Integer","When required/relevant","kN",false,[],"1234","BCA"),
                P(29,"SGPset_Beam","WorkingLoad_DA1-2","Integer","When required/relevant","kN",false,[],"1234","BCA"),
            ]
        });

        // ─── IfcColumn ───────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Column",
            IfcEntity = "IfcColumn",
            SubTypes = ["N.A."],
            DisciplineContext = "Structural",
            Disciplines = ["STR"],
            Platforms = new() {
                Revit = ["Structural Columns"], ArchiCAD = ["Column"], Tekla = ["Concrete Column"], Bentley = ["Column"] },
            Notes = "RC columns use reinforcement parameters. Steel columns use MemberSection. StirrupsType normal/U/C/CL/Torsion.",
            CopSection = "COP3.1 Section 4 p.269-270",
            Properties = [
                P(1, "SGPset_Column","ArrangementType","Text","","",false,[],"Multi-Tier","BCA"),
                P(2, "SGPset_ColumnDimension","Breadth","Length","RC column","mm",false,[],"300","BCA"),
                P(3, "SGPset_SteelConnection","ConnectionDetailsBottom","Text","Steel column","",false,[],"Detail 1","BCA"),
                P(4, "SGPset_SteelConnection","ConnectionDetailsTop","Text","Steel column","",false,[],"Detail 1","BCA"),
                P(5, "SGPset_SteelConnection","ConnectionTypeBottom","Text","Steel column","",true,CONNECTION_TYPES,"Pinned","BCA"),
                P(6, "SGPset_SteelConnection","ConnectionTypeTop","Text","Steel column","",true,CONNECTION_TYPES,"Pinned","BCA"),
                P(7, "SGPset_Column","ConstructionMethod","Text","All columns","",true,CONSTRUCTION_METHODS,"CIS","BCA"),
                P(8, "SGPset_ColumnDimension","Diameter","Length","When required/relevant","mm",false,[],"600","BCA"),
                P(9, "SGPset_Column","EndStorey","Text","All columns","",false,[],"2nd Storey, Roof Storey","BCA"),
                P(10,"SGPset_ColumnReinforcement","MainRebar","Text","RC column","",false,[],"6H32+6H25","BCA"),
                P(11,"SGPset_Column","Mark","Text","All columns","",false,[],"C1, TC1","BCA"),
                P(12,"SGPset_Column","MaterialGrade","Text","All columns","",true,MATERIAL_GRADES,"C32/40","BCA"),
                P(13,"SGPset_Column","MechanicalConnectionType","Text","","",true,[],"column shoes, grouted sleeves, spiral connector","BCA"),
                P(14,"SGPset_ColumnDimension","MemberSection","Text","Steel column","",false,[],"UC305x305x118kg/m","BCA"),
                P(15,"SGPset_Column","PrefabricatedReinforcementCage","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(16,"SGPset_Column","ReferTo2DDetail","Text","When required/relevant","",false,[],"Dwg Number","BCA"),
                P(17,"SGPset_Column","ReinforcementSteelGrade","Text","RC column","",true,REBAR_GRADES,"500B","BCA"),
                P(18,"SGPset_Column","SectionFabricationMethod","Text","Steel column","",true,SECTION_FABRICATION,"Hot rolled","BCA"),
                P(19,"SGPset_SteelConnection","SpliceDetail","Text","When required/relevant","",false,[],"Detail 3","BCA"),
                P(20,"SGPset_Column","StartingStorey","Text","All columns","",false,[],"1st Storey","BCA"),
                P(21,"SGPset_ColumnReinforcement","Stirrups","Text","RC column","",false,[],"4H13-300","BCA"),
                P(22,"SGPset_ColumnReinforcement","StirrupsType","Text","RC column","",true,STIRRUPS_TYPES,"Normal","BCA"),
                P(23,"SGPset_ColumnDimension","Width","Length","RC column","mm",false,[],"600","BCA"),
                P(24,"SGPset_Column","WorkingLoad_DA1-1","Integer","When required/relevant","kN",false,[],"4536","BCA"),
                P(25,"SGPset_Column","WorkingLoad_DA1-2","Integer","When required/relevant","kN",false,[],"3864","BCA"),
            ]
        });

        // ─── IfcSlab ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Slab",
            IfcEntity = "IfcSlab",
            SubTypes = ["N.A.","FLOOR","LANDING"],
            DisciplineContext = "Structural",
            Disciplines = ["STR"],
            Platforms = new() {
                Revit = ["Floors"], ArchiCAD = ["Slab"], Tekla = ["Concrete Slab","Slab"], Bentley = ["Slab"] },
            Notes = "ShelterUsage=TRUE for civil defence shelter slabs. SlabType is required for all slabs.",
            CopSection = "COP3.1 Section 4 p.353-354",
            Properties = [
                P(1, "SGPset_Slab","Accreditation_MAS","Boolean","When required/relevant","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(2, "SGPset_SlabReinforcement","BottomDistribution_nominal","Text","When required/relevant","",false,[],"H25-150+H16-300","BCA"),
                P(3, "SGPset_SlabReinforcement","BottomMain_nominal","Text","When required/relevant","",false,[],"H25-150+H16-300","BCA"),
                P(4, "SGPset_Slab","ConstructionMethod","Text","All slabs","",true,CONSTRUCTION_METHODS,"CIS","BCA"),
                P(5, "SGPset_Slab","LatticeGirderReinforcement","Boolean","When required/relevant","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(6, "Pset_SlabCommon","LoadBearing","Boolean","When required/relevant","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(7, "SGPset_Slab","Mark","Text","All slabs","",false,[],"S1, S01, PS01","BCA"),
                P(8, "SGPset_Slab","MaterialGrade","Text","All slabs","",true,MATERIAL_GRADES,"C32/40","BCA"),
                P(9, "SGPset_Slab","MechanicalConnectionType","Text","","",false,[],"","BCA"),
                P(10,"SGPset_Slab","ReferTo2DDetail","Text","When required/relevant","",false,[],"Dwg Number","BCA"),
                P(11,"SGPset_Slab","ReinforcementSteelGrade","Text","All slabs","",true,REBAR_GRADES,"500B","BCA"),
                P(12,"SGPset_Slab","ShelterUsage","Boolean","When required/relevant","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(13,"SGPset_Slab","SlabType","Text","All slabs","",true,SLAB_TYPES,"Two way","BCA"),
                P(14,"SGPset_SlabReinforcement","Stirrups","Text","When required/relevant","",false,[],"H10-150-300","BCA"),
                P(15,"SGPset_SlabReinforcement","StirrupsType","Text","Optional","",true,STIRRUPS_TYPES,"CL","BCA"),
                P(16,"SGPset_SlabDimension","Thickness","Length","All slabs","mm",false,[],"300","BCA"),
                P(17,"SGPset_SlabReinforcement","TopDistribution_nominal","Text","When required/relevant","",false,[],"H25-150+H16-300","BCA"),
                P(18,"SGPset_SlabReinforcement","TopMain_nominal","Text","When required/relevant","",false,[],"H32-150+H20-300","BCA"),
                P(19,"SGPset_Slab","TypeDesignator","Text","","",false,[],"Double T Slab, Hollowcore","BCA"),
                P(20,"SGPset_Slab","WeldedMesh","Boolean","All slabs","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
            ]
        });

        // ─── IfcPile ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Pile",
            IfcEntity = "IfcPile",
            SubTypes = ["N.A."],
            DisciplineContext = "Structural",
            Disciplines = ["STR"],
            Platforms = new() {
                Revit = ["Structural Foundations","Project Information"],
                ArchiCAD = ["Object","IFC Project Manager"],
                Tekla = ["Concrete Column"],
                Bentley = ["Base Plate","Floor Manager"] },
            Notes = "Pile type, capacity loads DA1-1 and DA1-2 mandatory. Borehole reference required.",
            CopSection = "COP3.1 Section 4 p.316-318",
            Properties = [
                P(1, "SGPset_Pile","BoreholeRef","Text","All piles","",false,[],"BH2, BH12-2","BCA"),
                P(2, "SGPset_Pile","Breadth","Length","RC non-circular piles","mm",false,[],"300","BCA"),
                P(3, "SGPset_Pile","ConstructionMethod","Text","All piles","",true,CONSTRUCTION_METHODS,"CIS","BCA"),
                P(4, "SGPset_Pile","CutOffLevel_SHD","Real","All piles","SHD m",false,[],"-1.35","BCA"),
                P(5, "SGPset_Pile","DA1-1_CompressionCapacity","Integer","All piles","kN",false,[],"5683","BCA"),
                P(6, "SGPset_Pile","DA1-1_CompressionDesignLoad","Integer","All piles","kN",false,[],"5515","BCA"),
                P(7, "SGPset_Pile","DA1-1_TensionCapacity","Integer","When required/relevant","kN",false,[],"3655","BCA"),
                P(8, "SGPset_Pile","DA1-1_TensionDesignLoad","Integer","When required/relevant","kN",false,[],"3255","BCA"),
                P(9, "SGPset_Pile","DA1-2_CompressionCapacity","Integer","All piles","kN",false,[],"4823","BCA"),
                P(10,"SGPset_Pile","DA1-2_CompressionDesignLoad","Integer","All piles","kN",false,[],"4650","BCA"),
                P(11,"SGPset_Pile","DA1-2_TensionCapacity","Integer","When required/relevant","kN",false,[],"3025","BCA"),
                P(12,"SGPset_Pile","DA1-2_TensionDesignLoad","Integer","When required/relevant","kN",false,[],"2850","BCA"),
                P(13,"SGPset_PileDimension","Diameter","Length","RC circular piles","mm",false,[],"600","BCA"),
                P(14,"SGPset_PileDimension","Length","Length","All piles","mm",false,[],"40500","BCA"),
                P(15,"SGPset_PileReinforcement","MainRebar","Text","RC piles","",false,[],"10H32+10H16","BCA"),
                P(16,"SGPset_Pile","Mark","Text","All piles","",false,[],"P-1600, 250x250","BCA"),
                P(17,"SGPset_Pile","MaterialGrade","Text","All piles","",true,MATERIAL_GRADES,"C35/45","BCA"),
                P(18,"SGPset_Pile","PileType","Text","All piles","",true,PILE_TYPES,"Bored","BCA"),
                P(19,"SGPset_Pile","ReinforcementLength","Text","RC piles","",true,["Fully reinforced","Unreinforced"],"24","BCA"),
                P(20,"SGPset_Pile","ReinforcementSteelGrade","Text","RC piles","",true,REBAR_GRADES,"500B","BCA"),
                P(21,"SGPset_PileReinforcement","Stirrups","Text","RC piles","",false,[],"H16-250","BCA"),
                P(22,"SGPset_Pile","StructuralCompressionCapacity","Integer","All piles","kN",false,[],"6525","BCA"),
                P(23,"SGPset_Pile","StructuralTensionCapacity","Integer","When required/relevant","kN",false,[],"3825","BCA"),
                P(24,"SGPset_Pile","ToeLevel_SHD","Real","All piles","SHD m",false,[],"-63.35","BCA"),
                P(25,"SGPset_PileDimension","Width","Length","RC non-circular piles","mm",false,[],"600","BCA"),
            ]
        });

        // ─── IfcFooting ──────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Footing",
            IfcEntity = "IfcFooting",
            SubTypes = ["N.A.","STRIP_FOOTING","CAISSON_FOUNDATION","PILE_CAP"],
            DisciplineContext = "Structural",
            Disciplines = ["STR"],
            Platforms = new() {
                Revit = ["Structural Foundations"], ArchiCAD = ["Footing"], Tekla = ["Concrete Footing"], Bentley = ["Footing"] },
            CopSection = "COP3.1 Section 4 p.291",
            Properties = [
                P(1,"SGPset_Footing","ConstructionMethod","Text","All footings","",true,CONSTRUCTION_METHODS,"CIS","BCA"),
                P(2,"SGPset_FootingDimension","Depth","Length","All footings","mm",false,[],"800","BCA"),
                P(3,"SGPset_FootingReinforcement","BottomMainX","Text","RC footing","",false,[],"H25-150","BCA"),
                P(4,"SGPset_FootingReinforcement","BottomMainY","Text","RC footing","",false,[],"H25-150","BCA"),
                P(5,"SGPset_FootingReinforcement","TopMainX","Text","RC footing","",false,[],"H25-150","BCA"),
                P(6,"SGPset_FootingReinforcement","TopMainY","Text","RC footing","",false,[],"H25-150","BCA"),
                P(7,"SGPset_Footing","Mark","Text","All footings","",false,[],"F1, PF1","BCA"),
                P(8,"SGPset_Footing","MaterialGrade","Text","All footings","",true,MATERIAL_GRADES,"C32/40","BCA"),
                P(9,"SGPset_Footing","ReinforcementSteelGrade","Text","RC footing","",true,REBAR_GRADES,"500B","BCA"),
            ]
        });

        // ─── IfcDoor ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Door",
            IfcEntity = "IfcDoor",
            SubTypes = ["DOOR","GATE","BLASTDOOR","ROLLERSHUTTER"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Doors"], ArchiCAD = ["Door"], Tekla = ["N.A."], Bentley = ["Door"] },
            Notes = "FireExit and FireRating required for all fire exit doors. BCA Accessibility 2025 requires ClearWidth>=850mm for main entrances.",
            CopSection = "COP3.1 Section 4 p.278",
            Properties = [
                P(1, "SGPset_DoorDimension","ClearWidth","Length","","mm",false,[],"1200","BCA"),
                P(2, "SGPset_DoorDimension","ClearHeight","Length","","mm",false,[],"2100","BCA"),
                P(3, "SGPset_DoorFireSafety","FireAccessOpening","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","SCDF"),
                P(4, "SGPset_DoorFireSafety","FireExit","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","SCDF"),
                P(5, "SGPset_DoorFireSafety","FireRating","Text","","hr",true,FIRE_RATINGS,"1","SCDF"),
                P(6, "SGPset_DoorCommon","MainEntrance","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(7, "Pset_DoorCommon","Material","Text","","",false,[],"","BCA"),
                P(8, "SGPset_DoorCommon","OneWayLockingDevice","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","SCDF"),
                P(9, "Pset_DoorCommon","OperationType","Text","","",true,[],"Single swing, Double swing, Sliding","BCA"),
                P(10,"SGPset_DoorDimension","OverallWidth","Length","","mm",false,[],"","BCA"),
                P(11,"SGPset_DoorCommon","PowerOperated","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(12,"SGPset_DoorCommon","SelfClosing","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","SCDF"),
                P(13,"SGPset_DoorDimension","StructuralHeight","Length","","mm",false,[],"710","BCA"),
                P(14,"SGPset_DoorDimension","StructuralWidth","Length","","mm",false,[],"490","BCA"),
                P(15,"SGPset_DoorDimension","Thickness","Length","","mm",false,[],"","BCA"),
                P(16,"SGPset_DoorCommon","VisionPanel","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","SCDF"),
            ]
        });

        // ─── IfcWindow ───────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Window",
            IfcEntity = "IfcWindow",
            SubTypes = ["BAYWINDOW","VENTILATIONSLEEVE","LOUVRE","WINDOW","SKYLIGHT"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Windows"], ArchiCAD = ["Window"], Tekla = ["N.A."], Bentley = ["Window"] },
            CopSection = "COP3.1 Section 4 p.439",
            Properties = [
                P(1,"SGPset_WindowDimension","InnerDiameter","Length","","mm",false,[],"","BCA"),
                P(2,"SGPset_WindowDimension","OuterDiameter","Length","","mm",false,[],"","BCA"),
                P(3,"SGPset_WindowFireSafety","FireAccessOpening","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","SCDF"),
                P(4,"SGPset_WindowDimension","StructuralWidth","Length","","mm",false,[],"","BCA"),
                P(5,"SGPset_WindowDimension","StructuralHeight","Length","","mm",false,[],"","BCA"),
                P(6,"Pset_WindowCommon","Material","Text","","",false,[],"","BCA"),
                P(7,"SGPset_WindowCommon","SafetyBarrierHeight","Length","","mm",false,[],"","BCA"),
                P(8,"SGPset_WindowCommon","PercentageOfOpening","Real","","",false,[],"","NEA"),
            ]
        });

        // ─── IfcSpace ────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Space (Usage)",
            IfcEntity = "IfcSpace",
            SubTypes = ["SPACE"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Rooms"], ArchiCAD = ["Zone"], Tekla = ["N.A."], Bentley = ["Space"] },
            Notes = "SpaceName must match the accepted list of 420 values. OccupancyType restricted to 95 accepted values.",
            CopSection = "COP3.1 Section 4 p.355-360",
            Properties = [
                P(1,"SGPset_SpaceUsage","SpaceName","Text","","",true,[],"Living Room, Office, Staircase","URA"),
                P(2,"SGPset_SpaceUsage","OccupancyType","Text","","",true,[],"Multi-unit residential, Office","URA"),
                P(3,"SGPset_SpaceCommon","FireEmergencyVentilationMode","Text","","",true,VENTILATION_MODES,"Natural Ventilation","SCDF"),
                P(4,"SGPset_SpaceCommon","VentilationMode","Text","","",true,VENTILATION_MODES,"Natural Ventilation","NEA"),
                P(5,"SGPset_SpaceDimension","Area","Length","","m2",false,[],"","URA"),
            ]
        });

        // ─── IfcCovering ─────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Ceiling",
            IfcEntity = "IfcCovering",
            SubTypes = ["CEILING"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Ceilings"], ArchiCAD = ["Ceiling"], Tekla = ["N.A."], Bentley = ["Slab"] },
            CopSection = "COP3.1 Section 4 p.266",
            Properties = [
                P(1,"SGPset_CoveringFireSafety","FireRating","Text","","hr",true,FIRE_RATINGS,"1","SCDF"),
                P(2,"SGPset_CoveringCommon","Material","Text","","",false,[],"Concrete, Steel, Timber","SCDF"),
            ]
        });

        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Soffit",
            IfcEntity = "IfcCovering",
            SubTypes = ["SOFFIT"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Roofs"], ArchiCAD = ["Roof"], Tekla = ["N.A."], Bentley = ["Roof"] },
            CopSection = "COP3.1 Section 4 p.355",
            Properties = [
                P(1,"SGPset_CoveringFireSafety","FireRating","Text","","hr",false,FIRE_RATINGS,"1","SCDF"),
            ]
        });

        // ─── IfcDamper ───────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Damper",
            IfcEntity = "IfcDamper",
            SubTypes = ["FIREDAMPER","FIRESMOKEDAMPER","SMOKEDAMPER"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Duct Accessories"], ArchiCAD = ["Object"], Tekla = ["N.A."], Bentley = ["Object"] },
            Notes = "Modelling damper is voluntary per COP3.1.",
            CopSection = "COP3.1 Section 4 p.274",
            Properties = [
                P(1,"SGPset_DamperFireSafety","FireRating","Text","","hr",true,FIRE_RATINGS,"1","SCDF"),
            ]
        });

        // ─── IfcStairFlight ──────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Staircase Flight (Architectural)",
            IfcEntity = "IfcStairFlight",
            SubTypes = ["CURVED","SPIRAL","WINDER","STRAIGHT"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Stairs"], ArchiCAD = ["Stair"], Tekla = ["Component"], Bentley = ["Stair"] },
            CopSection = "COP3.1 Section 4 p.420",
            Properties = [
                P(1,"Qto_StairFlightBaseQuantities","NumberOfRisers","Integer","All staircase","",false,[],"","BCA"),
                P(2,"Qto_StairFlightBaseQuantities","RiserHeight","Length","All staircase","mm",false,[],"","BCA"),
                P(3,"Qto_StairFlightBaseQuantities","NumberOfTreads","Integer","All staircase","",false,[],"","BCA"),
                P(4,"Qto_StairFlightBaseQuantities","TreadLength","Length","All staircase","mm",false,[],"","BCA"),
                P(5,"SGPset_StairFlight","MaterialGrade","Text","All staircase","",true,MATERIAL_GRADES,"C32/40","BCA"),
                P(6,"SGPset_StairFlight","ConstructionMethod","Text","All staircase","",true,CONSTRUCTION_METHODS,"PC","BCA"),
            ]
        });

        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Staircase (Structural)",
            IfcEntity = "IfcStair",
            SubTypes = ["N.A."],
            DisciplineContext = "Structural",
            Disciplines = ["STR"],
            Platforms = new() {
                Revit = ["Stairs"], ArchiCAD = ["Stair"], Tekla = ["Component"], Bentley = ["Stair"] },
            CopSection = "COP3.1 Section 4 p.421-422",
            Properties = [
                P(1, "SGPset_StairReinforcement","BottomDistribution","Text","RC staircase","",false,[],"H25-150+H16-300","BCA"),
                P(2, "SGPset_StairReinforcement","BottomMain","Text","RC staircase","",false,[],"H25-150+H16-300","BCA"),
                P(3, "SGPset_SteelConnection","ConnectionDetailsBottom","Text","When required/relevant","",false,[],"Detail 1","BCA"),
                P(4, "SGPset_SteelConnection","ConnectionDetailsTop","Text","When required/relevant","",false,[],"Detail 1","BCA"),
                P(5, "SGPset_SteelConnection","ConnectionTypeBottom","Text","When required/relevant","",true,CONNECTION_TYPES,"Pinned","BCA"),
                P(6, "SGPset_SteelConnection","ConnectionTypeTop","Text","When required/relevant","",true,CONNECTION_TYPES,"Pinned","BCA"),
                P(7, "SGPset_Stair","Mark","Text","All staircase","",false,[],"ST1, ST-A1","BCA"),
                P(8, "SGPset_StairDimension","MemberSection","Text","Steel staircase","",false,[],"RHS600x30x4","BCA"),
                P(9, "SGPset_Stair","ReferTo2DDetail","Text","When required/relevant","",false,[],"Dwg number","BCA"),
                P(10,"SGPset_Stair","ReinforcementSteelGrade","Text","RC staircase","",true,REBAR_GRADES,"500B","BCA"),
                P(11,"SGPset_Stair","MaterialGrade","Text","All staircase","",true,MATERIAL_GRADES,"C32/40","BCA"),
                P(12,"SGPset_Stair","ConstructionMethod","Text","All staircase","",true,CONSTRUCTION_METHODS,"PC","BCA"),
                P(13,"SGPset_StairDimension","Thickness","Length","RC staircase","mm",false,[],"150","BCA"),
                P(14,"SGPset_StairReinforcement","TopDistribution","Text","RC staircase","",false,[],"H10-200","BCA"),
                P(15,"SGPset_StairReinforcement","TopMain","Text","RC staircase","",false,[],"H10-200","BCA"),
                P(16,"SGPset_StairDimension","Width","Length","All staircase","mm",false,[],"1600","BCA"),
            ]
        });

        // ─── IfcTransportElement (Lift) ───────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Lift",
            IfcEntity = "IfcTransportElement",
            SubTypes = ["LIFT","CARLIFT"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Specialty Equipment","Parking"],
                ArchiCAD = ["Object","Transport Element"],
                Tekla = ["N.A."], Bentley = ["Equipment","Object"] },
            CopSection = "COP3.1 Section 4 p.310",
            Properties = [
                P(1,"SGPset_LiftCommon","BarrierFreeAccessbility","Boolean","LIFT only","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(2,"SGPset_LiftDimension","Length","Length","","mm",false,[],"","BCA"),
                P(3,"SGPset_LiftDimension","Width","Length","","mm",false,[],"","BCA"),
                P(4,"SGPset_LiftDimension","ClearDepth","Length","LIFT only","mm",false,[],"","BCA"),
                P(5,"SGPset_LiftDimension","ClearHeight","Length","LIFT only","mm",false,[],"","BCA"),
                P(6,"SGPset_LiftDimension","ClearWidth","Length","LIFT only","mm",false,[],"","BCA"),
                P(7,"SGPset_LiftCommon","FireFightingLift","Boolean","LIFT only","",true,BOOL_VALUES,"TRUE / FALSE","SCDF"),
                P(8,"SGPset_LiftCommon","LiftType","Text","LIFT only","",false,[],"Goods Lift, Platform Lift, Bin Lifter, Bed Lift","BCA"),
            ]
        });

        // ─── IfcPipeSegment ───────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Pipes",
            IfcEntity = "IfcPipeSegment",
            SubTypes = ["RIGIDSEGMENT","FLEXIBLESEGMENT"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Pipes"], ArchiCAD = ["Pipe","Pipe Fitting"], Tekla = ["N.A."], Bentley = ["Pipe Accessory"] },
            CopSection = "COP3.1 Section 4 p.320",
            Properties = [
                P(1,"SGPset_PipeSegment","PreInsulated","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","PUB"),
                P(2,"SGPset_PipeSegment","Perforated","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","PUB"),
                P(3,"SGPset_PipeSegment","ConstructionMethod","Text","","",false,[],"","PUB"),
                P(4,"SGPset_PipeSegment","Material","Text","","",false,[],"","PUB"),
                P(5,"SGPset_PipeSegment","Gradient","Text","","",false,[],"1:100","PUB"),
                P(6,"SGPset_PipeSegmentDimension","InnerDiameter","Length","","mm",false,[],"","PUB"),
                P(7,"SGPset_PipeSegmentDimension","Length","Length","","mm",false,[],"","PUB"),
                P(8,"SGPset_PipeSegmentDimension","Thickness","Length","","mm",false,[],"","PUB"),
                P(9,"SGPset_PipeSegment","TradeEffluent","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","NEA"),
                P(10,"SGPset_PipeSegment","DemountableStructureAbovePipe","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","PUB"),
                P(11,"SGPset_System","SystemType","Text","","",false,[],"Sanitary, Sewerage","PUB"),
                P(12,"SGPset_System","SystemName","Text","","",false,[],"","PUB"),
            ]
        });

        // ─── IfcTank ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Tank",
            IfcEntity = "IfcTank",
            SubTypes = ["STORAGE","DETENTIONTANK","RAINWATERHARVESTINGTANK","IRRIGATIONTANK","SPRINKLERTANK","BALANCINGTANK","SECTIONAL","VESSEL","RECHARGEWELL"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Mechanical Equipment"], ArchiCAD = ["Object"], Tekla = ["N.A."], Bentley = ["Object"] },
            CopSection = "COP3.1 Section 4 p.428",
            Properties = [
                P(1,"SGPset_TankCommon","IsPotable","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","PUB"),
                P(2,"SGPset_TankDimension","NominalCapacity","Real","","m3",false,[],"","PUB"),
                P(3,"SGPset_TankDimension","EffectiveCapacity","Real","","m3",false,[],"","PUB"),
                P(4,"SGPset_TankDimension","Diameter","Length","","mm",false,[],"","PUB"),
                P(5,"SGPset_TankDimension","Height","Length","","mm",false,[],"","PUB"),
                P(6,"SGPset_TankDimension","Length","Length","","mm",false,[],"","PUB"),
                P(7,"SGPset_TankDimension","Width","Length","","mm",false,[],"","PUB"),
                P(8,"SGPset_TankCommon","TradeEffluent","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","NEA"),
                P(9,"SGPset_TankCommon","EquipmentType","Text","","",false,[],"","PUB"),
            ]
        });

        // ─── IfcSanitaryTerminal ───────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Sanitary Appliances",
            IfcEntity = "IfcSanitaryTerminal",
            SubTypes = ["BATH","BIDET","SHOWER","URINAL","WASHHANDBASIN","WATERCLOSET","SPRINKLER"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP","ARC"],
            Platforms = new() {
                Revit = ["Plumbing Fixtures"], ArchiCAD = ["Pipe Flow Terminal"], Tekla = ["N.A."], Bentley = ["Fixture"] },
            Notes = "Water usage per month required for PUB WELS compliance.",
            CopSection = "COP3.1 Section 4 p.339-341",
            Properties = [
                P(1,"SGPset_SanitaryTerminal","SystemType","Text","","",true,[],"Sanitary, Sewerage, Potable Water","PUB"),
                P(2,"SGPset_SanitaryTerminal","SystemName","Text","","",false,[],"","PUB"),
                P(3,"SGPset_WaterUsage","WaterUsagePerMonth","Real","","m3/month",false,[],"","PUB"),
            ]
        });

        // ─── IfcFlowMeter ─────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Water Meter",
            IfcEntity = "IfcFlowMeter",
            SubTypes = ["WATERMETER"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Pipe Accessories"], ArchiCAD = ["Pipe In-line Flow Device"], Tekla = ["N.A."], Bentley = ["Pipe Accessory"] },
            CopSection = "COP3.1 Section 4 p.438",
            Properties = [
                P(1,"SGPset_WaterMeter","Capacity","Real","","L/s",false,[],"","PUB"),
                P(2,"SGPset_WaterMeter","Diameter","Length","","mm",false,[],"","PUB"),
                P(3,"SGPset_WaterMeter","Length","Length","","mm",false,[],"","PUB"),
                P(4,"SGPset_WaterMeter","Purpose","Text","","",false,[],"Private","PUB"),
                P(5,"SGPset_WaterMeter","UnitNumber","Text","","",false,[],"","PUB"),
                P(6,"SGPset_WaterMeter","UnitNumberTag","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","PUB"),
                P(7,"SGPset_WaterMeter","WaterSupplySource","Text","","",false,[],"","PUB"),
                P(8,"SGPset_System","SystemType","Text","","",false,[],"","PUB"),
                P(9,"SGPset_System","SystemName","Text","","",false,[],"","PUB"),
            ]
        });

        // ─── IfcFireSuppressionTerminal ────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Breeching Inlet / Hose Reel / Fire Hydrant",
            IfcEntity = "IfcFireSuppressionTerminal",
            SubTypes = ["BREECHINGINLET","FOAMINLET","FOAMOUTLET","FIREINLETBOX","FIREHYDRANT"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Plumbing Fixtures"], ArchiCAD = ["Pipe Flow Terminal"], Tekla = ["N.A."], Bentley = ["Fire Protection"] },
            Notes = "Breeching inlet must also be exported as part of Dry/Wet Riser system.",
            CopSection = "COP3.1 Section 4 p.264, p.289-290, p.301",
            Properties = [
                P(1,"SGPset_FireSuppressionTerminal","Hose_NominalDiameter","Text","","mm",false,[],"","SCDF"),
                P(2,"SGPset_FireSuppressionTerminal","ID","Text","","",false,[],"","SCDF"),
                P(3,"SGPset_System","SystemType","Text","","",false,[],"Dry Riser, Wet Riser, Foam Sprinkler, Sprinkler","SCDF"),
                P(4,"SGPset_System","SystemName","Text","","",false,[],"","SCDF"),
            ]
        });

        // ─── IfcValve ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Valve",
            IfcEntity = "IfcValve",
            SubTypes = ["LANDINGVALVE","SPRINKLERCONTROL","DOUBLECHECK","MIXING","AIRADMITTANCE","DRAINOFFCOCK","CHECK","ISOLATING","FAUCET"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Pipe Accessories"], ArchiCAD = ["Pipe In-line Flow Device"], Tekla = ["N.A."], Bentley = ["Valve"] },
            CopSection = "COP3.1 Section 4 p.430",
            Properties = [
                P(1,"SGPset_System","SystemType","Text","","",false,[],"","PUB"),
                P(2,"SGPset_System","SystemName","Text","","",false,[],"","PUB"),
            ]
        });

        // ─── IfcAlarm ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Fire Alarm",
            IfcEntity = "IfcAlarm",
            SubTypes = ["SMOKEALARM","FIREALARM","MULTISENSOR","HEATDETECTOR"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP","ARC"],
            Platforms = new() {
                Revit = ["Fire Alarm Devices"], ArchiCAD = ["Object"], Tekla = ["N.A."], Bentley = ["Solid"] },
            CopSection = "COP3.1 Section 4 p.287",
            Properties = [
                P(1,"SGPset_Alarm","SystemType","Text","","",false,[],"Fire Alarm","SCDF"),
                P(2,"SGPset_Alarm","SystemName","Text","","",false,[],"","SCDF"),
            ]
        });

        // ─── IfcDistributionChamberElement ────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Distribution Chamber (Manhole/Inspection Chamber)",
            IfcEntity = "IfcDistributionChamberElement",
            SubTypes = ["INSPECTIONCHAMBER","MANHOLE","METERCHAMBER","PWCSINSPECTIONCHAMBER","PWCSMANHOLE","SCREENCHAMBER","SAMPLINGSUMP","SUMP","TRENCH"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP","ARC"],
            Platforms = new() {
                Revit = ["Plumbing Fixtures","Generic Models"], ArchiCAD = ["Flow Equipment","Covering"], Tekla = ["N.A."], Bentley = ["Equipment","Slab"] },
            CopSection = "COP3.1 Section 4 p.276",
            Properties = [
                P(1,"SGPset_DistribChamber","Diameter","Length","","mm",false,[],"","PUB"),
                P(2,"SGPset_DistribChamber","Depth","Length","","mm",false,[],"","PUB"),
                P(3,"SGPset_DistribChamber","Height","Length","","mm",false,[],"","PUB"),
                P(4,"SGPset_DistribChamber","ID","Text","","",false,[],"","PUB"),
                P(5,"SGPset_DistribChamber","InvertLevel","Real","","SHD m",false,[],"","PUB"),
                P(6,"SGPset_DistribChamber","Length","Length","","mm",false,[],"","PUB"),
                P(7,"SGPset_DistribChamber","Material","Text","","",false,[],"","PUB"),
                P(8,"SGPset_DistribChamber","Status","Text","","",true,STATUS_VALUES,"Proposed","PUB"),
                P(9,"SGPset_System","SystemName","Text","","",false,[],"","PUB"),
                P(10,"SGPset_System","SystemType","Text","","",false,[],"Sanitary, Sewerage","PUB"),
                P(11,"SGPset_DistribChamber","TopLevel","Real","","SHD m",false,[],"3.423","PUB"),
                P(12,"SGPset_DistribChamber","TradeEffluent","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","NEA"),
                P(13,"SGPset_DistribChamber","Width","Length","","mm",false,[],"","PUB"),
            ]
        });

        // ─── IfcGeographicElement ─────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Landscape Plants",
            IfcEntity = "IfcGeographicElement",
            SubTypes = ["LANDSCAPE_TREE","LANDSCAPE_PALM","LANDSCAPE_HEDGE"],
            DisciplineContext = "External Works",
            Disciplines = ["External Works","ARC"],
            Platforms = new() {
                Revit = ["Planting"], ArchiCAD = ["Object"], Tekla = ["N.A."], Bentley = ["Object"] },
            CopSection = "COP3.1 Section 4 p.309",
            Properties = [
                P(1,"SGPset_PlantCommon","Girth","Length","","mm",false,[],"300","NParks"),
                P(2,"SGPset_PlantCommon","HedgeNumber","Text","","",false,[],"H001","NParks"),
                P(3,"SGPset_PlantCommon","Height","Length","","mm",false,[],"2500","NParks"),
                P(4,"SGPset_PlantCommon","ReasonForRemoval","Text","","",false,[],"","NParks"),
                P(5,"SGPset_PlantCommon","Roadside","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","NParks"),
                P(6,"SGPset_PlantCommon","SingleStem","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","NParks"),
                P(7,"SGPset_PlantCommon","Species","Text","","",false,[],"Samanea saman","NParks"),
                P(8,"SGPset_PlantCommon","Status","Text","","",true,STATUS_VALUES,"Proposed","NParks"),
                P(9,"SGPset_PlantCommon","TreeNumber","Text","","",false,[],"T001","NParks"),
                P(10,"SGPset_PlantCommon","TreeSize","Text","","",false,[],"Small to medium, Large","NParks"),
                P(11,"SGPset_PlantCommon","Turf","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","NParks"),
            ]
        });

        // ─── IfcWasteTerminal ─────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Waste Terminal",
            IfcEntity = "IfcWasteTerminal",
            SubTypes = ["FLOORTRAP","FLOORWASTE","GULLYSUMP","GULLYTRAP","WASTETRAP","WASTESUMP"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Pipe Accessories"], ArchiCAD = ["Pipe Flow Terminal"], Tekla = ["N.A."], Bentley = ["Fixture"] },
            CopSection = "COP3.1 Section 4 p.437",
            Properties = [
                P(1,"SGPset_WasteTerminal","Material","Text","","",false,[],"","PUB"),
                P(2,"SGPset_WasteTerminal","TradeEffluent","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","NEA"),
                P(3,"SGPset_System","SystemType","Text","","",false,[],"Sanitary, Sewerage","PUB"),
                P(4,"SGPset_System","SystemName","Text","","",false,[],"","PUB"),
            ]
        });

        // ─── IfcCivilElement ─────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Culvert / Drains",
            IfcEntity = "IfcCivilElement",
            SubTypes = ["COMMONDRAIN","CROSSCULVERT","CULVERT","ENTRANCECULVERT","EXTERNALDRAIN","INTERNALDRAIN","OUTLETDRAIN","ROADSIDEDRAIN","TRENCH"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP","STR"],
            Platforms = new() {
                Revit = ["Generic Models"], ArchiCAD = ["Object"], Tekla = ["Slab, Panel"], Bentley = ["Drains&Basins"] },
            CopSection = "COP3.1 Section 4 p.272",
            Properties = [
                P(1,"SGPset_CivilElement","Diameter","Length","","mm",false,[],"","LTA"),
                P(2,"SGPset_CivilElement","Gradient","Text","","",false,[],"","LTA"),
                P(3,"SGPset_CivilElement","Height","Length","","mm",false,[],"","LTA"),
                P(4,"SGPset_CivilElement","Length","Length","","mm",false,[],"","LTA"),
                P(5,"SGPset_CivilElement","LoadBearing","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","LTA"),
                P(6,"SGPset_CivilElement","Material","Text","","",false,[],"","LTA"),
                P(7,"SGPset_System","SystemName","Text","","",false,[],"","LTA"),
                P(8,"SGPset_System","SystemType","Text","","",false,[],"Rainwater, Drainage","PUB"),
                P(9,"SGPset_CivilElement","Thickness","Length","","mm",false,[],"","LTA"),
                P(10,"SGPset_CivilElement","Width","Length","","mm",false,[],"","LTA"),
            ]
        });

        // ─── IfcPump ─────────────────────────────────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Pump",
            IfcEntity = "IfcPump",
            SubTypes = ["SUMPPUMP","CIRCULATOR","ENDSUCTION","SPLITCASE","SUBMERSIBLEPUMP","VERTICALINLINE","VERTITURBINE"],
            DisciplineContext = "MEP",
            Disciplines = ["MEP"],
            Platforms = new() {
                Revit = ["Mechanical Equipment"], ArchiCAD = ["Flow Equipment"], Tekla = ["N.A."], Bentley = ["Pump"] },
            CopSection = "COP3.1 Section 4 p.328",
            Properties = [
                P(1,"SGPset_System","SystemType","Text","","",false,[],"","PUB"),
                P(2,"SGPset_System","SystemName","Text","","",false,[],"","PUB"),
                P(3,"SGPset_Pump","Capacity","Real","","L/s",false,[],"","PUB"),
            ]
        });

        // ─── IfcBuildingElementProxy - Parking Lot ────────────────────────────
        db.Add(new EntityPropertyBlock
        {
            ComponentName = "Parking Lot",
            IfcEntity = "IfcBuildingElementProxy",
            SubTypes = ["CARLOT","LORRYLOT","COACHLOT","ARTICULATEDVEHICLELOT","MOTORCYCLELOT","BICYCLELOT","HOLDINGBAY","QUEUINGSPACE"],
            DisciplineContext = "Architectural",
            Disciplines = ["ARC"],
            Platforms = new() {
                Revit = ["Generic Models","Parking"], ArchiCAD = ["Object"], Tekla = ["N.A."], Bentley = ["Object"] },
            CopSection = "COP3.1 Section 4 p.312-314",
            Properties = [
                P(1,"SGPset_ParkingLot","BarrierFreeAccessibility","Boolean","CARLOT","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(2,"SGPset_ParkingLot","FamilyLot","Boolean","CARLOT","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(3,"SGPset_ParkingLotDimension","Length","Length","","mm",false,[],"","LTA"),
                P(4,"SGPset_ParkingLotDimension","Width","Length","","mm",false,[],"","LTA"),
                P(5,"SGPset_ParkingLot","LotNumber","Text","","",false,[],"123","LTA"),
                P(6,"SGPset_ParkingLot","CarParking_ServedByCarLift","Boolean","CARLOT","",true,BOOL_VALUES,"TRUE / FALSE","BCA"),
                P(7,"SGPset_ParkingLot","MechanisedParkingSystem","Boolean","CARLOT","",true,BOOL_VALUES,"TRUE / FALSE","LTA"),
                P(8,"SGPset_ParkingLot","Perforated","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","LTA"),
                P(9,"SGPset_ParkingLot","OpenAtGrade","Boolean","","",true,BOOL_VALUES,"TRUE / FALSE","LTA"),
                P(10,"SGPset_ParkingLot","BicycleRack_Type","Text","BICYCLELOT","",true,["Single-Tier Wheel Rack","Single-Tier U-Bar","Double-Tier"],"","LTA"),
            ]
        });

        return db;
    }

    // ── BUILDER HELPERS ────────────────────────────────────────────────────────
    private static IfcSgPropertyDef P(
        int sn, string pset, string name, string type,
        string typeOfElem, string unit, bool inputLimitation,
        string[] accepted, string example, string agency)
    {
        var agencyEnum = agency switch {
            "BCA"    => SgAgency.BCA,    "URA" => SgAgency.URA,
            "SCDF"   => SgAgency.SCDF,   "LTA" => SgAgency.LTA,
            "NEA"    => SgAgency.NEA,    "PUB" => SgAgency.PUB,
            "NParks" => SgAgency.NParks, "SLA" => SgAgency.SLA,
            _        => SgAgency.BCA
        };
        return new IfcSgPropertyDef {
            SN = sn, PropertySetName = pset, PropertyName = name,
            PropertyType = type, TypeOfElements = typeOfElem, Unit = unit,
            InputLimitation = inputLimitation, AcceptedValues = accepted,
            Example = example, Agency = agencyEnum,
            IsRequired = inputLimitation || !string.IsNullOrEmpty(typeOfElem),
            Regulation = "IFC+SG COP3.1 Dec 2025"
        };
    }
}
