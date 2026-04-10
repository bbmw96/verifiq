// VERIFIQ  -  IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.

using VERIFIQ.Core.Enums;

namespace VERIFIQ.Core.Models;

// ═══════════════════════════════════════════════════════════════════════════════
// IFC FILE AND PROJECT
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents a fully parsed IFC file, including all entities and relationships.
/// </summary>
public sealed class IfcFile
{
    public string FilePath        { get; set; } = string.Empty;
    public string FileName        => Path.GetFileName(FilePath);
    public long   FileSizeBytes   { get; set; }
    public DateTime ParsedAt      { get; set; } = DateTime.UtcNow;
    public InputFileFormat Format { get; set; }
    public IfcSchemaVersion Schema { get; set; }

    // Header
    public IfcFileHeader Header   { get; set; } = new();

    // Spatial hierarchy
    public IfcProject? Project    { get; set; }
    public List<IfcSite> Sites    { get; set; } = new();
    public List<IfcBuilding> Buildings { get; set; } = new();
    public List<IfcBuildingStorey> Storeys { get; set; } = new();
    public List<IfcSpace> Spaces  { get; set; } = new();

    // Physical elements  -  all products
    public List<IfcElement> Elements { get; set; } = new();

    // Properties
    public List<IfcPropertySet> PropertySets { get; set; } = new();

    // Classifications
    public List<IfcClassificationSystem> ClassificationSystems { get; set; } = new();

    // Georeferencing
    public IfcGeoreference? Georeference { get; set; }

    // Computed stats
    public int TotalElementCount  => Elements.Count;
    public int ProxyElementCount  => Elements.Count(e => e.IfcClass == "IFCBUILDINGELEMENTPROXY");
}

/// <summary>
/// IFC file header metadata.
/// </summary>
public sealed class IfcFileHeader
{
    public string Description     { get; set; } = string.Empty;
    public string ImplementationLevel { get; set; } = string.Empty;
    public string FileName        { get; set; } = string.Empty;
    public DateTime TimeStamp     { get; set; }
    public string Author          { get; set; } = string.Empty;
    public string Organisation    { get; set; } = string.Empty;
    public string PreprocessorVersion { get; set; } = string.Empty;
    public string OriginatingSystem { get; set; } = string.Empty;
    public string Authorization   { get; set; } = string.Empty;
    public string SchemaIdentifier { get; set; } = string.Empty;  // IFC4, IFC2X3, etc.

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(OriginatingSystem) &&
        !string.IsNullOrWhiteSpace(SchemaIdentifier) &&
        TimeStamp != default;
}

// ═══════════════════════════════════════════════════════════════════════════════
// SPATIAL HIERARCHY
// ═══════════════════════════════════════════════════════════════════════════════

public abstract class IfcObject
{
    public int    StepId          { get; set; }
    public string GlobalId        { get; set; } = string.Empty;   // GUID  -  22 char base64
    public string Name            { get; set; } = string.Empty;
    public string Description     { get; set; } = string.Empty;
    public string ObjectType      { get; set; } = string.Empty;
    public string IfcClass        { get; set; } = string.Empty;   // e.g. IFCWALL
    public string PredefinedType  { get; set; } = string.Empty;
}

public sealed class IfcProject : IfcObject
{
    public string LongName        { get; set; } = string.Empty;
    public string Phase           { get; set; } = string.Empty;
    public string UnitsInContext  { get; set; } = string.Empty;
}

public sealed class IfcSite : IfcObject
{
    public string LongName        { get; set; } = string.Empty;
    public double? Latitude       { get; set; }
    public double? Longitude      { get; set; }
    public double? Elevation      { get; set; }
    public string LandTitleNumber { get; set; } = string.Empty;
    public string SiteAddress     { get; set; } = string.Empty;
}

public sealed class IfcBuilding : IfcObject
{
    public string LongName             { get; set; } = string.Empty;
    public double? ElevationOfRefHeight { get; set; }
    public double? ElevationOfTerrain  { get; set; }
    public string BuildingAddress      { get; set; } = string.Empty;
}

public sealed class IfcBuildingStorey : IfcObject
{
    public double? Elevation    { get; set; }
    public string  LongName     { get; set; } = string.Empty;
}

public sealed class IfcSpace : IfcObject
{
    public string  LongName              { get; set; } = string.Empty;
    public string  InteriorOrExterior    { get; set; } = string.Empty;
    public double? ElevationWithFlooring { get; set; }
    public string  StoreyGuid            { get; set; } = string.Empty;
    public string  StoreyName            { get; set; } = string.Empty;

    // IFC+SG / NBeS required
    public string  SpaceCategory         { get; set; } = string.Empty;
    public double? GrossPlannedArea      { get; set; }
    public double? NetPlannedArea        { get; set; }

    public List<IfcPropertySet> PropertySets { get; set; } = new();
    public List<IfcClassificationReference> Classifications { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════════
// PHYSICAL ELEMENTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Represents any physical building element (IfcWall, IfcSlab, IfcBeam, etc.)
/// This is the primary object that VERIFIQ validates.
/// </summary>
public sealed class IfcElement : IfcObject
{
    // Containment
    public string StoreyGuid      { get; set; } = string.Empty;
    public string StoreyName      { get; set; } = string.Empty;
    public string SpaceGuid       { get; set; } = string.Empty;

    // Properties
    public List<IfcPropertySet> PropertySets { get; set; } = new();

    // Classifications
    public List<IfcClassificationReference> Classifications { get; set; } = new();

    // Materials
    public List<string> Materials { get; set; } = new();

    // Geometry — bounding box for design-code checks, mesh for 3D viewer
    public BoundingBox? BoundingBox { get; set; }
    /// <summary>Triangle mesh extracted from IFC geometry for the 3D viewer.</summary>
    public IfcMesh?     Mesh        { get; set; }

    // Validation results  -  populated after running the compliance engine
    public List<ValidationResult> ValidationResults { get; set; } = new();

    // Computed
    public bool IsProxy => IfcClass.Equals("IFCBUILDINGELEMENTPROXY", StringComparison.OrdinalIgnoreCase);
    public Severity OverallSeverity => ValidationResults.Count == 0
        ? Severity.Pass
        : ValidationResults.Max(r => r.Severity);

    public bool IsFullyCompliant => OverallSeverity == Severity.Pass;
    public int ErrorCount   => ValidationResults.Count(r => r.Severity >= Severity.Error);
    public int WarningCount => ValidationResults.Count(r => r.Severity == Severity.Warning);
}

public sealed class BoundingBox
{
    public double MinX { get; set; }
    public double MinY { get; set; }
    public double MinZ { get; set; }
    public double MaxX { get; set; }
    public double MaxY { get; set; }
    public double MaxZ { get; set; }
    public bool IsDegenerate =>
        (MaxX - MinX) <= 0 || (MaxY - MinY) <= 0 || (MaxZ - MinZ) <= 0;
}

/// <summary>
/// Extracted triangle mesh for the 3D viewer.
/// Vertices are stored as flat [x0,y0,z0, x1,y1,z1, ...] in world coordinates.
/// Indices are stored as flat [i0,i1,i2, ...] triangles.
/// This is populated by the parser for elements with Brep or swept-solid geometry.
/// </summary>
public sealed class IfcMesh
{
    /// <summary>Flat vertex buffer: x0,y0,z0, x1,y1,z1, ...</summary>
    public float[] Vertices { get; set; } = Array.Empty<float>();
    /// <summary>Triangle index buffer: i0,i1,i2, ...</summary>
    public int[]   Indices  { get; set; } = Array.Empty<int>();
    /// <summary>True when the mesh contains at least one triangle.</summary>
    public bool IsValid => Indices.Length >= 3;
    /// <summary>Number of triangles in this mesh.</summary>
    public int TriangleCount => Indices.Length / 3;
}

// ═══════════════════════════════════════════════════════════════════════════════
// PROPERTY SETS AND PROPERTIES
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class IfcPropertySet
{
    public int    StepId          { get; set; }
    public string GlobalId        { get; set; } = string.Empty;
    public string Name            { get; set; } = string.Empty;  // e.g. Pset_WallCommon
    public string Description     { get; set; } = string.Empty;
    public List<IfcProperty> Properties { get; set; } = new();

    public bool IsSingaporeSpecific => Name.StartsWith("SGPset_", StringComparison.OrdinalIgnoreCase);
    public bool IsStandardBuildingSmart => Name.StartsWith("Pset_", StringComparison.OrdinalIgnoreCase);

    public IfcProperty? GetProperty(string name) =>
        Properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public bool HasProperty(string name) => GetProperty(name) != null;

    public string? GetStringValue(string propertyName) =>
        GetProperty(propertyName)?.Value?.ToString();

    public double? GetDoubleValue(string propertyName)
    {
        var raw = GetProperty(propertyName)?.Value?.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return double.TryParse(raw, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out double v)
            ? v : null;
    }

    public bool? GetBoolValue(string propertyName)
    {
        var val = GetProperty(propertyName)?.Value?.ToString()?.ToUpperInvariant();
        return val switch { "TRUE" => true, "FALSE" => false, _ => null };
    }
}

public sealed class IfcProperty
{
    public string Name            { get; set; } = string.Empty;
    public string PropertyType    { get; set; } = string.Empty;  // SingleValue, EnumeratedValue, etc.
    public object? Value          { get; set; }
    public string? Unit           { get; set; }

    public bool IsPopulated => Value is not null &&
        !string.IsNullOrWhiteSpace(Value.ToString());
    public bool IsNotDefined => Value?.ToString()?.Equals("NOTDEFINED", StringComparison.OrdinalIgnoreCase) == true;
}

// ═══════════════════════════════════════════════════════════════════════════════
// CLASSIFICATIONS
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class IfcClassificationSystem
{
    public string Source          { get; set; } = string.Empty;
    public string Edition         { get; set; } = string.Empty;
    public DateTime? EditionDate  { get; set; }
    public string Name            { get; set; } = string.Empty;
    public string Description     { get; set; } = string.Empty;
    public string Location        { get; set; } = string.Empty;
}

public sealed class IfcClassificationReference
{
    public string Location        { get; set; } = string.Empty;
    public string ItemReference   { get; set; } = string.Empty;
    public string Name            { get; set; } = string.Empty;
    public string SystemName      { get; set; } = string.Empty;

    public bool IsPopulated =>
        !string.IsNullOrWhiteSpace(ItemReference) &&
        !string.IsNullOrWhiteSpace(Name);
}

// ═══════════════════════════════════════════════════════════════════════════════
// GEOREFERENCING
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class IfcGeoreference
{
    // IfcMapConversion
    public double? Eastings        { get; set; }
    public double? Northings       { get; set; }
    public double? OrthogonalHeight { get; set; }
    public double? XAxisAbscissa   { get; set; }
    public double? XAxisOrdinate   { get; set; }
    public double? Scale           { get; set; }

    // IfcProjectedCRS
    public string? CrsName         { get; set; }
    public string? CrsDescription  { get; set; }
    public string? CrsGeodeticDatum { get; set; }
    public string? CrsVerticalDatum { get; set; }
    public string? MapProjection   { get; set; }
    public string? MapZone         { get; set; }

    // For Singapore: CRS should reference SVY21 / EPSG:3414
    public bool IsValidForSingapore =>
        CrsName?.Contains("SVY21", StringComparison.OrdinalIgnoreCase) == true ||
        CrsName?.Contains("3414", StringComparison.OrdinalIgnoreCase) == true ||
        MapZone?.Contains("SVY21", StringComparison.OrdinalIgnoreCase) == true;

    // For Malaysia: CRS should reference GDM2000 / RSO or Cassini
    public bool IsValidForMalaysia =>
        CrsName?.Contains("GDM2000", StringComparison.OrdinalIgnoreCase) == true ||
        CrsName?.Contains("RSO", StringComparison.OrdinalIgnoreCase) == true;

    public bool HasMapConversion =>
        Eastings.HasValue && Northings.HasValue;
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALIDATION RESULTS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// A single validation finding for one element at one check level.
/// </summary>
public sealed class ValidationResult
{
    public string       ElementGuid     { get; set; } = string.Empty;
    public string       ElementName     { get; set; } = string.Empty;
    public string       IfcClass        { get; set; } = string.Empty;
    public string       StoreyName      { get; set; } = string.Empty;

    public CheckLevel   CheckLevel      { get; set; }
    public Severity     Severity        { get; set; }
    public CountryMode  Country         { get; set; }

    // Singapore-specific
    public SgAgency     AffectedAgency  { get; set; }
    public CorenetGateway AffectedGateway { get; set; }

    // Malaysia-specific
    public MalaysiaPurposeGroup PurposeGroup { get; set; }

    // Detail
    public string       PropertySetName { get; set; } = string.Empty;
    public string       PropertyName    { get; set; } = string.Empty;
    public string       ExpectedValue   { get; set; } = string.Empty;
    public string       ActualValue     { get; set; } = string.Empty;
    public string       Message         { get; set; } = string.Empty;
    public string       RemediationGuidance { get; set; } = string.Empty;

    // IFC+SG / NBeS rule reference
    public string       RuleReference   { get; set; } = string.Empty;
    public string       RuleSource      { get; set; } = string.Empty;  // e.g. "IFC+SG Industry Mapping v2025"
}

// ═══════════════════════════════════════════════════════════════════════════════
// VALIDATION SESSION  -  the complete result of one validation run
// ═══════════════════════════════════════════════════════════════════════════════

public sealed partial class ValidationSession
{
    public Guid        SessionId        { get; set; } = Guid.NewGuid();
    public DateTime    StartedAt        { get; set; } = DateTime.UtcNow;
    public DateTime?   CompletedAt      { get; set; }
    public CountryMode CountryMode      { get; set; }
    public CorenetGateway? SgGateway   { get; set; }
    public MalaysiaPurposeGroup? MyPurposeGroup { get; set; }

    public List<IfcFile>          LoadedFiles  { get; set; } = new();
    public List<ValidationResult> Results      { get; set; } = new();

    // Summary statistics
    public int TotalElements     { get; set; }
    public int PassedElements    { get; set; }
    public int WarningElements   { get; set; }
    public int ErrorElements     { get; set; }
    public int CriticalElements  { get; set; }
    public int ProxyElements     { get; set; }

    public double ComplianceScore =>
        TotalElements == 0 ? 0.0
        : Math.Round((double)PassedElements / TotalElements * 100.0, 1);

    // By agency (Singapore)
    public Dictionary<SgAgency, int> ErrorsByAgency { get; set; } = new();

    // By check level
    public Dictionary<CheckLevel, int> ErrorsByCheckLevel { get; set; } = new();

    public TimeSpan Duration =>
        CompletedAt.HasValue ? CompletedAt.Value - StartedAt : TimeSpan.Zero;
}

// ═══════════════════════════════════════════════════════════════════════════════
// APPLICATION SETTINGS
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class AppSettings
{
    public CountryMode DefaultCountryMode { get; set; } = CountryMode.Singapore;
    public string LicenceKey              { get; set; } = string.Empty;
    public string LicenceTier             { get; set; } = string.Empty;
    public string LicencedTo              { get; set; } = string.Empty;
    public string RecentFilesJson         { get; set; } = "[]";
    public string OutputFolderPath        { get; set; } = string.Empty;
    public bool ShowProxyWarnings         { get; set; } = true;
    public bool AutoCheckGeometry         { get; set; } = true;
    public string RulesDbVersion          { get; set; } = string.Empty;
    public DateTime LastRulesCheck        { get; set; }
}
