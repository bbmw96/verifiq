// VERIFIQ  -  Design Code Check Models
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Extends the core validation model with actual design code checking  - 
// verifying that physical dimensions, areas, distances and ratios meet
// the specific requirements of Singapore planning codes and Malaysia UBBL.

using VERIFIQ.Core.Enums;

namespace VERIFIQ.Core.Models;

// ─── DESIGN CODE RESULT ───────────────────────────────────────────────────────

/// <summary>
/// A single design code check result.
/// Unlike a data-compliance ValidationResult (which checks that data IS present),
/// a DesignCodeResult checks that the ACTUAL VALUE of a dimension or area
/// meets the minimum or maximum in the applicable code or regulation.
/// </summary>
public sealed class DesignCodeResult
{
    public string   ElementGuid       { get; set; } = string.Empty;
    public string   ElementName       { get; set; } = string.Empty;
    public string   IfcClass          { get; set; } = string.Empty;
    public string   StoreyName        { get; set; } = string.Empty;
    public string   SpaceCategory     { get; set; } = string.Empty;

    // Rule identification
    public string   RuleId            { get; set; } = string.Empty;  // e.g. SG-URA-ROOM-001
    public string   RuleName          { get; set; } = string.Empty;
    public string   CodeReference     { get; set; } = string.Empty;  // e.g. URA Handbook 2023 §4.3.2
    public string   RegulationText    { get; set; } = string.Empty;  // Exact quoted regulation clause
    public DesignCodeCategory Category { get; set; }
    public Severity Severity          { get; set; }
    public CountryMode Country        { get; set; }
    public SgAgency AffectedAgency    { get; set; }

    // The actual check
    public string   CheckParameter    { get; set; } = string.Empty;  // e.g. "Room Area"
    public string   CheckUnit         { get; set; } = string.Empty;  // e.g. "m²" or "mm" or "°"
    public double   ActualValue       { get; set; }
    public double   RequiredMinimum   { get; set; }
    public double?  RequiredMaximum   { get; set; }
    public string   ActualDisplay     { get; set; } = string.Empty;  // e.g. "7.2 m²"
    public string   RequiredDisplay   { get; set; } = string.Empty;  // e.g. "Min 9.0 m²"

    // Verdict
    public bool     Complies          { get; set; }
    public string   Message           { get; set; } = string.Empty;
    public string   RemediationGuidance { get; set; } = string.Empty;

    // Formula used for the check
    public string   Formula           { get; set; } = string.Empty;  // e.g. "GrossArea ≥ 9.0 m²"
    public string   FormulaResult     { get; set; } = string.Empty;  // e.g. "7.2 m² < 9.0 m² (FAIL)"
}

/// <summary>
/// Category of design code check  -  groups related checks.
/// </summary>
public enum DesignCodeCategory
{
    RoomSizesAndDimensions = 1,    // URA minimum room sizes, UBBL floor areas
    AccessibilityAndUniversalDesign = 2,  // BCA Code on Accessibility
    FireSafetyAndEscape = 3,       // SCDF fire codes, travel distances, exit widths
    StructuralAndConstrucitonal = 4, // Slab thickness, wall thickness
    VentilationAndLighting = 5,    // Window area ratios, ventilation requirements
    GrossFloorAreaRules = 6,       // URA GFA rules, balcony area limits, void exemptions
    ParkingAndVehicularAccess = 7, // LTA parking dimensions
    SustainabilityAndGreenMark = 8, // BCA Green Mark, WWR, U-values
    PlumbingAndDrainage = 9,       // PUB drainage, sanitary requirements
    PlantAndEquipmentRooms = 10,   // Minimum plant room dimensions
    SiteAndSetbackRequirements = 11, // URA setbacks and building envelopes
    SignageAndWayfinding = 12,     // SCDF exit signage
    MalaysiaUBBL = 20,             // Malaysia-specific UBBL checks
    MalaysiaFireCode = 21,         // JBPM fire code checks
    MalaysiaAccessibility = 22,    // MS 1184 accessibility
    MalaysiaGreenBuilding = 23     // GBI / Green Building Index
}

// ─── DESIGN CODE SESSION ──────────────────────────────────────────────────────

/// <summary>
/// Aggregates all design code check results for a validation run.
/// Appended to the ValidationSession.
/// </summary>
public sealed class DesignCodeSession
{
    public List<DesignCodeResult> Results     { get; set; } = new();
    public CountryMode            Country     { get; set; }

    // Statistics
    public int TotalChecks       { get; set; }
    public int PassedChecks      { get; set; }
    public int FailedChecks      { get; set; }
    public int CriticalChecks    { get; set; }

    public double DesignComplianceScore =>
        TotalChecks == 0 ? 100.0
        : Math.Round((double)PassedChecks / TotalChecks * 100.0, 1);

    // Failures grouped by category
    public Dictionary<DesignCodeCategory, int> FailuresByCategory { get; set; } = new();

    // Failures grouped by regulation
    public Dictionary<string, int> FailuresByRegulation { get; set; } = new();
}

// ─── EXTEND ValidationSession with Design Code ────────────────────────────────

public sealed partial class ValidationSession
{
    public DesignCodeSession? DesignCode { get; set; }

    // Combined overall score (data compliance + design compliance)
    public double OverallScore =>
        DesignCode == null
            ? ComplianceScore
            : Math.Round((ComplianceScore + DesignCode.DesignComplianceScore) / 2.0, 1);
}

// ─── DESIGN CODE RULE DEFINITION ─────────────────────────────────────────────

public sealed class DesignCodeRule
{
    public string               RuleId          { get; set; } = string.Empty;
    public string               RuleName        { get; set; } = string.Empty;
    public string               CodeReference   { get; set; } = string.Empty;
    public string               RegulationText  { get; set; } = string.Empty;
    public DesignCodeCategory   Category        { get; set; }
    public Severity             FailSeverity    { get; set; } = Severity.Error;
    public CountryMode          Country         { get; set; }
    public SgAgency             Agency          { get; set; }
    public string               IfcClassFilter  { get; set; } = string.Empty; // empty = all

    // What value to check (from property set or geometry)
    public string               CheckParameter  { get; set; } = string.Empty;
    public string               CheckUnit       { get; set; } = string.Empty;
    public double               MinimumValue    { get; set; }
    public double?              MaximumValue    { get; set; }

    // Conditions  -  only run this rule when these conditions are true
    public string               SpaceCategoryFilter { get; set; } = string.Empty;
    public string               PredefinedTypeFilter { get; set; } = string.Empty;

    // Formula description
    public string               FormulaDescription { get; set; } = string.Empty;
}

// ─── SPACE DIMENSIONS ────────────────────────────────────────────────────────

/// <summary>
/// Computed space dimensions extracted from IFC geometry and properties.
/// Used by the design code engine for room-size checks.
/// </summary>
public sealed class SpaceDimensions
{
    public double? GrossArea        { get; set; }  // m²
    public double? NetArea          { get; set; }  // m²
    public double? Height           { get; set; }  // m
    public double? Length           { get; set; }  // m
    public double? Width            { get; set; }  // m
    public double? Volume           { get; set; }  // m³
    public string  Category         { get; set; } = string.Empty;
    public string  SubCategory      { get; set; } = string.Empty;
    public bool    IsExternal       { get; set; }
    public bool    IsAccessible     { get; set; }

    // Derived ratios
    public double? WindowAreaRatio  { get; set; }  // window area / floor area
    public double? VentilationRatio { get; set; }

    // How values were obtained
    public string  AreaSource       { get; set; } = string.Empty;  // "Pset_SpaceCommon", "BoundingBox", etc.
}

// ─── ELEMENT DIMENSIONS ───────────────────────────────────────────────────────

/// <summary>
/// Computed element dimensions for design code checks.
/// </summary>
public sealed class ElementDimensions
{
    // All in mm unless noted
    public double? Width             { get; set; }  // clear opening width (doors), corridor width
    public double? Height            { get; set; }
    public double? Depth             { get; set; }
    public double? Thickness         { get; set; }  // wall/slab thickness in mm
    public double? Length            { get; set; }
    public double? Slope             { get; set; }  // degrees (ramps)
    public double? SlopeRatio        { get; set; }  // 1:N notation
    public double? Area              { get; set; }  // m²
    public double? FireRatingMinutes { get; set; }  // parsed from FireRating string
    public double? ThermalTransmittance { get; set; } // U-value W/(m²·K)  -  for Green Mark / GBI checks
    public string  Source            { get; set; } = string.Empty;
}
