// VERIFIQ  -  IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// All Rights Reserved.

namespace VERIFIQ.Core.Enums;

/// <summary>
/// Country mode  -  controls which regulatory framework is active.
/// When Singapore is selected, ALL Malaysia content is completely hidden.
/// When Malaysia is selected, ALL Singapore content is completely hidden.
/// Combined mode activates both simultaneously.
/// </summary>
public enum CountryMode
{
    Singapore = 1,
    Malaysia = 2,
    Combined = 3
}

/// <summary>
/// The 20 validation check levels applied to every IFC element.
/// </summary>
public enum CheckLevel
{
    // Classification checks
    IfcEntityClass           = 1,
    PredefinedType           = 2,
    ObjectTypeUserDefined    = 3,
    ClassificationReference  = 4,
    ClassificationEdition    = 5,

    // Property set checks
    MandatoryPropertySets    = 6,
    SgPropertySets           = 7,   // Singapore SGPset_ only
    PropertyValuesPopulated  = 8,
    PropertyValueDataType    = 9,
    PropertyValueEnumeration = 10,

    // Spatial / structural checks
    SpatialContainment       = 11,
    StoreyElevation          = 12,
    Georeferencing           = 13,  // Singapore SVY21 / Malaysia GDM2000
    SiteAndBuildingHierarchy = 14,
    GuidUniqueness           = 15,
    MaterialAssignment       = 16,
    SpaceBoundaryIntegrity   = 17,

    // Technical integrity checks
    GeometryValidity         = 18,
    IfcSchemaVersion         = 19,
    FileHeaderCompleteness   = 20
}

/// <summary>
/// Validation result severity.
/// </summary>
public enum Severity
{
    Pass    = 0,
    Info    = 1,
    Warning = 2,
    Error   = 3,   // Submission will likely be rejected
    Critical = 4   // Submission will definitely be rejected
}

/// <summary>
/// Singapore regulatory agencies covered by CORENET-X.
/// </summary>
public enum SgAgency
{
    None   = 0,
    BCA    = 1,
    URA    = 2,
    LTA    = 3,
    NEA    = 4,
    NParks = 5,
    PUB    = 6,
    SCDF   = 7,
    SLA    = 8,
    // Malaysia regulatory agencies
    CIDB   = 10,   // Construction Industry Development Board / Local Authority (PBT)
    JBPM   = 11,   // Jabatan Bomba dan Penyelamat Malaysia (Fire & Rescue)
    All    = 99
}

/// <summary>
/// CORENET-X submission gateways.
/// </summary>
public enum CorenetGateway
{
    Design         = 1,
    Piling         = 2,  // Optional
    Construction   = 3,
    Completion     = 4,
    DirectSubmission = 5  // DSP  -  smaller projects
}

/// <summary>
/// Malaysia NBeS purpose groups (UBBL 1984 Third Schedule / MS 1183).
/// All nine purpose groups are included for full UBBL compliance checking.
/// </summary>
public enum MalaysiaPurposeGroup
{
    None             = 0,
    PurposeGroupI    = 1,  // Small Residential (house, terrace, semi-D)
    PurposeGroupII   = 2,  // Small Flat / Apartment (< 280 m²)
    PurposeGroupIII  = 3,  // Other Residential (flat, apartment, hostel, hotel)
    PurposeGroupIV   = 4,  // Office
    PurposeGroupV    = 5,  // Shop (retail, food court, wet market)
    PurposeGroupVI   = 6,  // Factory / Industrial / Warehouse
    PurposeGroupVII  = 7,  // Place of Public Resort (cinema, theatre, stadium)
    PurposeGroupVIII = 8,  // Institution (hospital, clinic, school, place of worship)
    PurposeGroupIX   = 9,  // Hazardous / Special Risk
    All              = 99  // Applies to all purpose groups
}

/// <summary>
/// Supported IFC schema versions.
/// </summary>
public enum IfcSchemaVersion
{
    Unknown = 0,
    IFC2X3  = 1,
    IFC4    = 2,    // IFC4 ADD2 TC1  -  required for CORENET-X IFC+SG
    IFC4X3  = 3     // Infrastructure (bridges, roads, rail)
}

/// <summary>
/// Supported input file formats.
/// </summary>
public enum InputFileFormat
{
    // IFC / OpenBIM
    IFC         = 1,
    IFCXML      = 2,
    IFCZIP      = 3,

    // CAD formats
    DWG         = 10,
    DXF         = 11,
    DWF         = 12,
    DGN         = 13,

    // Native BIM (metadata/reference read)
    RVT         = 20,  // Revit
    PLN         = 21,  // ArchiCAD
    BIMX        = 22,  // ArchiCAD BIMx
    SKP         = 23,  // SketchUp
    RHN         = 24,  // Rhino .3dm
    VWX         = 25,  // Vectorworks

    // Coordination / review
    NWD         = 30,  // Navisworks
    NWF         = 31,
    NWC         = 32,
    BCF         = 33,  // BIM Collaboration Format
    SMC         = 34,  // Solibri

    // Exchange / neutral 3D
    STEP        = 40,
    IGES        = 41,
    OBJ         = 42,
    FBX         = 43,
    STL         = 44,
    DAE         = 45,  // COLLADA
    GLTF        = 46,
    GLB         = 47,

    // Point cloud
    E57         = 50,
    LAS         = 51,
    LAZ         = 52,
    PTS         = 53,
    XYZ         = 54,
    RCP         = 55,

    // COBie / asset data
    COBIE_XLSX  = 60,
    COBIE_XML   = 61,

    // Document / data
    PDF         = 70,
    XLSX        = 71,
    CSV         = 72,
    JSON        = 73,
    XML         = 74,

    // GIS / location
    KML         = 80,
    KMZ         = 81,
    GEOJSON     = 82,
    CITYGML     = 83,

    // Emerging / future
    USD         = 90,
    USDA        = 91,
    USDC        = 92,
    USDZ        = 93,
    FORMAT_3MF  = 94,
}

/// <summary>
/// Report export formats.
/// </summary>
public enum ExportFormat
{
    Word        = 1,
    PDF         = 2,
    Excel       = 3,
    CSV         = 4,
    JSON        = 5,
    HTML        = 6,
    XML         = 7,
    Markdown    = 8,
    Text        = 9,
    BCF         = 10  // BIM Collaboration Format issue export
}
