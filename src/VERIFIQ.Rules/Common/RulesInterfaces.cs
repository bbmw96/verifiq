// VERIFIQ — IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Rules database interfaces — contracts used by the validation engine

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Rules;

// ─── RULES DATABASE ──────────────────────────────────────────────────────────

/// <summary>
/// Main rules database interface. Implemented by the embedded SQLite rules store.
/// Contains all IFC+SG (Singapore) and NBeS (Malaysia) validation rules.
/// </summary>
public interface IRulesDatabase
{
    bool   RequiresClassification(string ifcClass, CountryMode mode);
    bool   RequiresMaterial(string ifcClass, CountryMode mode);
    string GetRequiredClassificationSystem(string ifcClass, CountryMode mode);
    string GetCurrentClassificationEdition(CountryMode mode);

    List<PropertySetRequirement> GetRequiredPropertySets(string ifcClass, CountryMode mode);
    List<PropertyRequirement>    GetRequiredProperties(string ifcClass, CountryMode mode,
        CorenetGateway? gateway, MalaysiaPurposeGroup? purposeGroup);
    List<PropertyTypeRule>       GetPropertyTypeRules(string ifcClass, CountryMode mode);
    List<EnumerationRule>        GetEnumerationRules(string ifcClass, CountryMode mode);

    string GetRulesDbVersion(CountryMode mode);
    DateTime GetRulesDbLastUpdated(CountryMode mode);

    /// <summary>Classification code → additional required Psets per CORENET-X / NBeS.</summary>
    List<PropertyRequirement> GetPropertiesForClassification(
        string ifcClass, string classificationCode, CountryMode mode);
}

/// <summary>
/// Entity class rules — permitted predefined types and class suggestions.
/// </summary>
public interface IEntityClassRules
{
    List<string> GetPermittedPredefinedTypes(string ifcClass);
    string       SuggestEntityClass(string name, string objectType);
    bool         IsKnownIfcClass(string ifcClass);
}

/// <summary>
/// Singapore-specific IFC+SG rules (CORENET-X).
/// </summary>
public interface ISgRules
{
    List<SgAgencyRequirement> GetAgencyRequirements(string ifcClass, SgAgency agency, CorenetGateway gateway);
    List<string>              GetMandatoryAgenciesForElement(string ifcClass);
    bool                      IsSubmissionRequired(string ifcClass, CorenetGateway gateway);

}

/// <summary>
/// Malaysia-specific NBeS / UBBL rules.
/// </summary>
public interface IMyRules
{
    List<UbblRequirement> GetUbblRequirements(string ifcClass, MalaysiaPurposeGroup purposeGroup);
    List<string>          GetJbpmFireRequirements(string ifcClass);
    UbblPart              GetRelevantUbblPart(string ifcClass);
}

/// <summary>
/// Geometry validity checker.
/// </summary>
public interface IGeometryChecker
{
    bool IsGeometryValid(IfcElement element);
    List<string> GetGeometryIssues(IfcElement element);
}

// ─── RULE DEFINITION TYPES ───────────────────────────────────────────────────

public sealed class PropertySetRequirement
{
    public string    Name            { get; set; } = string.Empty;
    public bool      IsRequired      { get; set; } = true;
    public bool      IsSingaporeSpecific { get; set; }
    public SgAgency  AffectedAgency  { get; set; }
    public string    RuleSource      { get; set; } = string.Empty;
    public string    AppliesTo       { get; set; } = string.Empty; // IFC class filter
    public CountryMode Country       { get; set; }
}

public class PropertyRequirement
{
    public string    PropertySetName { get; set; } = string.Empty;
    public string    PropertyName    { get; set; } = string.Empty;
    public bool      IsRequired      { get; set; } = true;
    public SgAgency  AffectedAgency  { get; set; }
    public CorenetGateway Gateway    { get; set; }
    public MalaysiaPurposeGroup PurposeGroup { get; set; }
    public string    ExpectedValueDescription { get; set; } = string.Empty;
    public string    RemediationGuidance      { get; set; } = string.Empty;
    public string    RuleSource      { get; set; } = string.Empty;
    public CountryMode Country       { get; set; }
}

public sealed class PropertyTypeRule
{
    public string    PropertySetName { get; set; } = string.Empty;
    public string    PropertyName    { get; set; } = string.Empty;
    public string    ExpectedType    { get; set; } = string.Empty; // BOOLEAN, INTEGER, REAL, STRING
    public SgAgency  AffectedAgency  { get; set; }
    public CountryMode Country       { get; set; }
}

public sealed class EnumerationRule
{
    public string       PropertySetName  { get; set; } = string.Empty;
    public string       PropertyName     { get; set; } = string.Empty;
    public List<string> PermittedValues  { get; set; } = new();
    public SgAgency     AffectedAgency   { get; set; }
    public string       RuleSource       { get; set; } = string.Empty;
    public CountryMode  Country          { get; set; }
}

public class SgAgencyRequirement
{
    public SgAgency    Agency           { get; set; }
    public CorenetGateway Gateway       { get; set; }
    public string      Description      { get; set; } = string.Empty;
    public string      RegReference     { get; set; } = string.Empty;
    public bool        IsMandatory      { get; set; }
}

public class UbblRequirement
{
    public UbblPart    Part             { get; set; }
    public string      ByLaw            { get; set; } = string.Empty;
    public string      Description      { get; set; } = string.Empty;
    public MalaysiaPurposeGroup PurposeGroup { get; set; }
    public bool        IsMandatory      { get; set; }
}

public enum UbblPart
{
    I   = 1, // Preliminary
    II  = 2, // Submission of Plans
    III = 3, // Space, Light and Ventilation
    IV  = 4, // Temporary Works
    V   = 5, // Structural Requirements
    VI  = 6, // Constructional Requirements
    VII = 7, // Fire Requirements
    VIII= 8, // Fire Alarms and Extinguishing
    IX  = 9  // Special Requirements
}
