// VERIFIQ  -  IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// Main Validation Engine  -  orchestrates all 20 check levels

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;
using VERIFIQ.Rules.Common;

namespace VERIFIQ.Rules;

/// <summary>
/// Main validation engine. Takes a parsed IfcFile and country mode,
/// runs all 20 check levels on every element, and returns a complete
/// ValidationSession with findings for every element.
/// </summary>
public sealed class ValidationEngine
{
    private readonly IRulesDatabase _db;
    private readonly IEntityClassRules _classRules;
    private readonly ISgRules _sgRules;
    private readonly IMyRules _myRules;
    private readonly IGeometryChecker _geomChecker;

    public event Action<int, string>? ProgressChanged;

    public ValidationEngine(
        IRulesDatabase db,
        IEntityClassRules classRules,
        ISgRules sgRules,
        IMyRules myRules,
        IGeometryChecker geomChecker)
    {
        _db = db;
        _classRules = classRules;
        _sgRules = sgRules;
        _myRules = myRules;
        _geomChecker = geomChecker;
    }

    // ─── PUBLIC ENTRY POINT ──────────────────────────────────────────────────

    public async Task<ValidationSession> ValidateAsync(
        List<IfcFile> files,
        CountryMode countryMode,
        CorenetGateway? sgGateway = null,
        MalaysiaPurposeGroup? myPurposeGroup = null,
        CancellationToken ct = default)
    {
        var session = new ValidationSession
        {
            CountryMode    = countryMode,
            SgGateway      = sgGateway,
            MyPurposeGroup = myPurposeGroup
        };

        session.LoadedFiles.AddRange(files);

        // Collect all elements across all loaded files
        var allElements = files.SelectMany(f => f.Elements).ToList();
        session.TotalElements = allElements.Count;

        int processed = 0;
        int total = allElements.Count;

        // Run checks in parallel (batches of 50)
        var batches = allElements.Chunk(50).ToList();
        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            // Parallel within batch
            var tasks = batch.Select(element => Task.Run(() =>
            {
                var results = RunAllChecks(element, files, countryMode, sgGateway, myPurposeGroup);
                element.ValidationResults.AddRange(results);
                return results;
            }, ct)).ToList();

            var batchResults = await Task.WhenAll(tasks);
            session.Results.AddRange(batchResults.SelectMany(r => r));

            processed += batch.Length;
            int pct = (int)((double)processed / total * 100);
            ProgressChanged?.Invoke(pct, $"Validated {processed} of {total} elements...");
        }

        // File-level checks (not per-element)
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var fileResults = RunFileLevelChecks(file, countryMode);
            session.Results.AddRange(fileResults);
        }

        // Federated model checks (across files)
        if (files.Count > 1)
        {
            var federatedResults = RunFederatedChecks(files, countryMode);
            session.Results.AddRange(federatedResults);
        }

        // Compile statistics
        CompileStatistics(session, allElements);

        session.CompletedAt = DateTime.UtcNow;
        return session;
    }

    // ─── ELEMENT-LEVEL CHECKS ────────────────────────────────────────────────

    private List<ValidationResult> RunAllChecks(
        IfcElement element,
        List<IfcFile> files,
        CountryMode mode,
        CorenetGateway? gateway,
        MalaysiaPurposeGroup? purposeGroup)
    {
        var results = new List<ValidationResult>();

        // LEVEL 1  -  IFC Entity Class
        results.AddRange(Check_Level1_EntityClass(element));

        // LEVEL 2  -  Predefined Type
        results.AddRange(Check_Level2_PredefinedType(element));

        // LEVEL 3  -  ObjectType when UserDefined
        results.AddRange(Check_Level3_ObjectTypeUserDefined(element));

        // LEVEL 4  -  Classification Reference present
        results.AddRange(Check_Level4_ClassificationReference(element, mode));

        // LEVEL 5  -  Classification edition
        results.AddRange(Check_Level5_ClassificationEdition(element, mode));

        // LEVEL 6  -  Mandatory Pset_ present
        results.AddRange(Check_Level6_MandatoryPropertySets(element, mode));

        // LEVEL 7  -  SGPset_ present (Singapore only)
        if (mode is CountryMode.Singapore or CountryMode.Combined)
            results.AddRange(Check_Level7_SgPropertySets(element));

        // LEVEL 8  -  Property values populated
        results.AddRange(Check_Level8_PropertyValuesPopulated(element, mode, gateway, purposeGroup));

        // LEVEL 9  -  Data type validation
        results.AddRange(Check_Level9_PropertyValueDataType(element, mode));

        // LEVEL 10  -  Enumeration values
        results.AddRange(Check_Level10_PropertyValueEnumeration(element, mode));

        // LEVEL 11  -  Spatial containment
        results.AddRange(Check_Level11_SpatialContainment(element));

        // LEVEL 16  -  Material assignment
        results.AddRange(Check_Level16_MaterialAssignment(element, mode));

        // LEVEL 17  -  Space boundary integrity (for IfcSpace)
        if (element.IfcClass.Equals("IFCSPACE", StringComparison.OrdinalIgnoreCase))
            results.AddRange(Check_Level17_SpaceBoundary(element, mode));

        // LEVEL 18  -  Geometry validity
        results.AddRange(Check_Level18_GeometryValidity(element));

        return results;
    }

    // ─── CHECK IMPLEMENTATIONS ───────────────────────────────────────────────

    private List<ValidationResult> Check_Level1_EntityClass(IfcElement element)
    {
        var results = new List<ValidationResult>();

        if (element.IsProxy)
        {
            // Try to suggest the correct class
            string suggestion = _classRules.SuggestEntityClass(element.Name, element.ObjectType);
            results.Add(new ValidationResult
            {
                ElementGuid = element.GlobalId,
                ElementName = element.Name,
                IfcClass    = element.IfcClass,
                StoreyName  = element.StoreyName,
                CheckLevel  = CheckLevel.IfcEntityClass,
                Severity    = Severity.Critical,
                Message     = $"'{element.Name}' uses IfcBuildingElementProxy — the generic fallback class. " +
                              "CORENET-X and NBeS automated checkers will reject proxy elements.",
                ExpectedValue = suggestion.Length > 0 ? suggestion : "A specific IFC entity class (e.g. IfcWall, IfcSlab, IfcBeam)",
                ActualValue   = "IFCBUILDINGELEMENTPROXY",
                RemediationGuidance = suggestion.Length > 0
                    ? $"VERIFIQ suggests this is a {suggestion}. " +
                      $"In your BIM software, open '{element.Name}' and change its IFC class to '{suggestion}'. " +
                      "In ArchiCAD: select the element → Settings → IFC → set IFC Type. " +
                      "In Revit: select element → IFC+SG Parameters → set IFC Entity Class."
                    : $"Review '{element.Name}' in your BIM software. Based on its name and position, " +
                      "assign the correct IFC entity class (IfcWall, IfcSlab, IfcBeam, IfcColumn etc.). " +
                      "Refer to the IFC+SG Industry Mapping Excel or NBeS Classification Guide.",
                RuleSource = "IFC4 Reference View / IFC+SG Industry Mapping"
            });
        }
        else if (string.IsNullOrWhiteSpace(element.IfcClass))
        {
            results.Add(new ValidationResult
            {
                ElementGuid = element.GlobalId,
                ElementName = element.Name,
                CheckLevel  = CheckLevel.IfcEntityClass,
                Severity    = Severity.Critical,
                Message     = "Element has no IFC entity class assigned. This is invalid and will cause submission failure.",
                RemediationGuidance = "Assign a specific IFC entity class in your BIM software."
            });
        }

        return results;
    }

    private List<ValidationResult> Check_Level2_PredefinedType(IfcElement element)
    {
        var results = new List<ValidationResult>();
        if (element.IsProxy) return results; // Already flagged at Level 1

        var expectedTypes = _classRules.GetPermittedPredefinedTypes(element.IfcClass);
        if (expectedTypes.Count == 0) return results; // Class doesn't use PredefinedType

        var actual = element.PredefinedType?.ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(actual) || actual == "NOTDEFINED")
        {
            results.Add(new ValidationResult
            {
                ElementGuid   = element.GlobalId,
                ElementName   = element.Name,
                IfcClass      = element.IfcClass,
                StoreyName    = element.StoreyName,
                CheckLevel    = CheckLevel.PredefinedType,
                Severity      = Severity.Error,
                Message       = $"PredefinedType is '{(string.IsNullOrWhiteSpace(actual) ? "(empty)" : actual)}'. " +
                                "A specific PredefinedType is required  -  NOTDEFINED is not acceptable for CORENET-X or NBeS submissions.",
                ExpectedValue = $"One of: {string.Join(", ", expectedTypes.Where(t => t != "NOTDEFINED" && t != "USERDEFINED"))}",
                ActualValue   = string.IsNullOrWhiteSpace(actual) ? "(empty)" : actual,
                RemediationGuidance =
                    $"In your BIM software, set the PredefinedType for this {element.IfcClass} element. " +
                    $"Permitted values: {string.Join(", ", expectedTypes.Where(t => t != "NOTDEFINED"))}. " +
                    "In ArchiCAD, this is set via the IFC Classification. In Revit, via the IFC+SG parameter."
            });
        }
        else if (actual != "USERDEFINED" && !expectedTypes.Contains(actual, StringComparer.OrdinalIgnoreCase))
        {
            results.Add(new ValidationResult
            {
                ElementGuid   = element.GlobalId,
                ElementName   = element.Name,
                IfcClass      = element.IfcClass,
                StoreyName    = element.StoreyName,
                CheckLevel    = CheckLevel.PredefinedType,
                Severity      = Severity.Error,
                Message       = $"PredefinedType '{actual}' is not in the permitted list for {element.IfcClass}.",
                ExpectedValue = $"One of: {string.Join(", ", expectedTypes.Where(t => t != "NOTDEFINED" && t != "USERDEFINED"))}",
                ActualValue   = actual,
                RemediationGuidance =
                    $"Change the PredefinedType to one of the permitted values for {element.IfcClass}."
            });
        }

        return results;
    }

    private List<ValidationResult> Check_Level3_ObjectTypeUserDefined(IfcElement element)
    {
        var results = new List<ValidationResult>();
        if (element.PredefinedType?.ToUpperInvariant() != "USERDEFINED") return results;

        if (string.IsNullOrWhiteSpace(element.ObjectType))
        {
            results.Add(new ValidationResult
            {
                ElementGuid   = element.GlobalId,
                ElementName   = element.Name,
                IfcClass      = element.IfcClass,
                StoreyName    = element.StoreyName,
                CheckLevel    = CheckLevel.ObjectTypeUserDefined,
                Severity      = Severity.Warning,
                Message       = "PredefinedType is USERDEFINED but ObjectType is empty. When using USERDEFINED, " +
                                "a descriptive ObjectType string is required to identify the element type.",
                ExpectedValue = "A descriptive string in ObjectType (e.g. 'CHILLED_WATER_PIPE', 'FEATURE_WALL')",
                ActualValue   = "(empty)",
                RemediationGuidance =
                    "Set the ObjectType attribute to a clear, descriptive string identifying this element's specific type."
            });
        }

        return results;
    }

    private List<ValidationResult> Check_Level4_ClassificationReference(
        IfcElement element, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        bool requiresClassification = _db.RequiresClassification(element.IfcClass, mode);
        if (!requiresClassification) return results;

        if (element.Classifications.Count == 0)
        {
            results.Add(new ValidationResult
            {
                ElementGuid    = element.GlobalId,
                ElementName    = element.Name,
                IfcClass       = element.IfcClass,
                StoreyName     = element.StoreyName,
                CheckLevel     = CheckLevel.ClassificationReference,
                Severity       = Severity.Critical,
                Country        = mode,
                Message        = "No classification reference (IfcClassificationReference) is attached to this element. " +
                                 "Classification references are mandatory for CORENET-X and NBeS submissions.",
                ExpectedValue  = _db.GetRequiredClassificationSystem(element.IfcClass, mode),
                ActualValue    = "(none)",
                RemediationGuidance =
                    "Attach the correct classification reference to this element using the IFC+SG Industry Mapping Excel. " +
                    "In ArchiCAD: use the Classification Manager and assign the IFC+SG classification. " +
                    "In Revit: use the IFC+SG shared parameters to assign the classification code.",
                RuleSource = mode == CountryMode.Singapore
                    ? "IFC+SG Industry Mapping / CORENET-X COP 3rd Edition"
                    : "NBeS IFC Classification Mapping / UBBL 1984"
            });
        }
        else
        {
            // Check that the classification reference has an item reference code
            foreach (var classif in element.Classifications)
            {
                if (!classif.IsPopulated)
                {
                    results.Add(new ValidationResult
                    {
                        ElementGuid    = element.GlobalId,
                        ElementName    = element.Name,
                        IfcClass       = element.IfcClass,
                        StoreyName     = element.StoreyName,
                        CheckLevel     = CheckLevel.ClassificationReference,
                        Severity       = Severity.Error,
                        Country        = mode,
                        Message        = "A classification reference exists but has no ItemReference code or Name. " +
                                         "An empty classification reference is treated as missing by CORENET-X and NBeS.",
                        ExpectedValue  = "A valid classification code and name",
                        ActualValue    = "(empty reference)",
                        RemediationGuidance =
                            "Populate the classification ItemReference and Name fields in your BIM software."
                    });
                }
            }
        }

        return results;
    }

    private List<ValidationResult> Check_Level5_ClassificationEdition(
        IfcElement element, CountryMode mode)
    {
        var results = new List<ValidationResult>();
        // Check that classifications reference the current edition, not a deprecated one
        string requiredEdition = _db.GetCurrentClassificationEdition(mode);
        if (string.IsNullOrWhiteSpace(requiredEdition)) return results;

        foreach (var classif in element.Classifications)
        {
            if (!string.IsNullOrWhiteSpace(classif.SystemName) &&
                classif.SystemName.Contains("deprecated", StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new ValidationResult
                {
                    ElementGuid   = element.GlobalId,
                    ElementName   = element.Name,
                    IfcClass      = element.IfcClass,
                    CheckLevel    = CheckLevel.ClassificationEdition,
                    Severity      = Severity.Warning,
                    Message       = $"Classification reference points to a potentially outdated classification system '{classif.SystemName}'.",
                    ExpectedValue = requiredEdition,
                    ActualValue   = classif.SystemName,
                    RemediationGuidance =
                        "Update the classification system reference to the current edition."
                });
            }
        }

        return results;
    }

    private List<ValidationResult> Check_Level6_MandatoryPropertySets(
        IfcElement element, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        var requiredPsets = _db.GetRequiredPropertySets(element.IfcClass, mode);
        var existingPsetNames = element.PropertySets
            .Select(p => p.Name.ToUpperInvariant()).ToHashSet();

        foreach (var req in requiredPsets.Where(p => !p.IsSingaporeSpecific))
        {
            if (!existingPsetNames.Contains(req.Name.ToUpperInvariant()))
            {
                results.Add(new ValidationResult
                {
                    ElementGuid      = element.GlobalId,
                    ElementName      = element.Name,
                    IfcClass         = element.IfcClass,
                    StoreyName       = element.StoreyName,
                    CheckLevel       = CheckLevel.MandatoryPropertySets,
                    Severity         = req.IsRequired ? Severity.Error : Severity.Warning,
                    Country          = mode,
                    AffectedAgency   = req.AffectedAgency,
                    PropertySetName  = req.Name,
                    Message          = $"Mandatory property set '{req.Name}' is missing from this {element.IfcClass} element.",
                    ExpectedValue    = req.Name,
                    ActualValue      = "(not present)",
                    RemediationGuidance =
                        $"Add property set '{req.Name}' to this element in your BIM software. " +
                        "Refer to the IFC+SG Industry Mapping for required properties within this set.",
                    RuleSource       = req.RuleSource
                });
            }
        }

        return results;
    }

    private List<ValidationResult> Check_Level7_SgPropertySets(IfcElement element)
    {
        var results = new List<ValidationResult>();

        var requiredSgPsets = _db.GetRequiredPropertySets(element.IfcClass, CountryMode.Singapore)
            .Where(p => p.IsSingaporeSpecific).ToList();
        var existingNames = element.PropertySets
            .Select(p => p.Name.ToUpperInvariant()).ToHashSet();

        foreach (var req in requiredSgPsets)
        {
            if (!existingNames.Contains(req.Name.ToUpperInvariant()))
            {
                results.Add(new ValidationResult
                {
                    ElementGuid      = element.GlobalId,
                    ElementName      = element.Name,
                    IfcClass         = element.IfcClass,
                    StoreyName       = element.StoreyName,
                    CheckLevel       = CheckLevel.SgPropertySets,
                    Severity         = req.IsRequired ? Severity.Error : Severity.Warning,
                    Country          = CountryMode.Singapore,
                    AffectedAgency   = req.AffectedAgency,
                    PropertySetName  = req.Name,
                    Message          = $"Singapore-specific property set '{req.Name}' (SGPset_) is missing. " +
                                       $"This is required by {req.AffectedAgency} for CORENET-X submissions.",
                    ExpectedValue    = req.Name,
                    ActualValue      = "(not present)",
                    RemediationGuidance =
                        $"Add SGPset_ property set '{req.Name}' using your BIM software's IFC+SG configuration files " +
                        "from info.corenet.gov.sg.",
                    RuleSource       = "IFC+SG Industry Mapping / CORENET-X COP 3rd Edition"
                });
            }
        }

        return results;
    }

    private List<ValidationResult> Check_Level8_PropertyValuesPopulated(
        IfcElement element, CountryMode mode,
        CorenetGateway? gateway, MalaysiaPurposeGroup? purposeGroup)
    {
        var results = new List<ValidationResult>();

        // Base property requirements (by IFC class)
        var requiredProps = _db.GetRequiredProperties(element.IfcClass, mode, gateway, purposeGroup)
            .ToList();

        // Classification-specific additional requirements (by classification code)
        // This implements the CORENET-X cross-reference: classification present →
        // check all property sets and properties that the classification implies.
        foreach (var classif in element.Classifications)
        {
            if (classif.IsPopulated)
            {
                var classifProps = _db.GetPropertiesForClassification(
                    element.IfcClass, classif.ItemReference, mode);
                // Merge — avoid duplicates by Pset+Property pair
                foreach (var cp in classifProps)
                {
                    if (!requiredProps.Any(r =>
                        r.PropertySetName.Equals(cp.PropertySetName, StringComparison.OrdinalIgnoreCase) &&
                        r.PropertyName.Equals(cp.PropertyName, StringComparison.OrdinalIgnoreCase)))
                    {
                        requiredProps.Add(cp);
                    }
                }
            }
        }

        foreach (var req in requiredProps)
        {
            var pset = element.PropertySets
                .FirstOrDefault(p => p.Name.Equals(req.PropertySetName, StringComparison.OrdinalIgnoreCase));
            if (pset == null) continue; // Pset absence already caught in Level 6/7

            var prop = pset.GetProperty(req.PropertyName);
            if (prop == null || !prop.IsPopulated || prop.IsNotDefined)
            {
                results.Add(new ValidationResult
                {
                    ElementGuid      = element.GlobalId,
                    ElementName      = element.Name,
                    IfcClass         = element.IfcClass,
                    StoreyName       = element.StoreyName,
                    CheckLevel       = CheckLevel.PropertyValuesPopulated,
                    Severity         = req.IsRequired ? Severity.Error : Severity.Warning,
                    Country          = mode,
                    AffectedAgency   = req.AffectedAgency,
                    AffectedGateway  = gateway ?? CorenetGateway.Construction,
                    PropertySetName  = req.PropertySetName,
                    PropertyName     = req.PropertyName,
                    Message          = $"Required property '{req.PropertySetName}.{req.PropertyName}' is empty or NOTDEFINED. " +
                                       $"Required by {req.AffectedAgency}.",
                    ExpectedValue    = req.ExpectedValueDescription,
                    ActualValue      = prop?.Value?.ToString() ?? "(empty)",
                    RemediationGuidance = 
                        req.Country == CountryMode.Singapore
                        ? KnowledgeLibrary.GetSgRemediationGuidance(element.IfcClass, req.PropertySetName, req.PropertyName)
                        : KnowledgeLibrary.GetMyRemediationGuidance(element.IfcClass, req.PropertySetName, req.PropertyName),
                    RuleSource          = req.RuleSource
                });
            }
        }

        return results;
    }

    private List<ValidationResult> Check_Level9_PropertyValueDataType(
        IfcElement element, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        var typeRules = _db.GetPropertyTypeRules(element.IfcClass, mode);
        foreach (var rule in typeRules)
        {
            var pset = element.PropertySets
                .FirstOrDefault(p => p.Name.Equals(rule.PropertySetName, StringComparison.OrdinalIgnoreCase));
            if (pset == null) continue;

            var prop = pset.GetProperty(rule.PropertyName);
            if (prop == null || !prop.IsPopulated) continue;

            bool valid = rule.ExpectedType switch
            {
                "BOOLEAN"  => prop.Value?.ToString()?.ToUpperInvariant() is "TRUE" or "FALSE",
                "INTEGER"  => int.TryParse(prop.Value?.ToString(), out _),
                "REAL"     => double.TryParse(prop.Value?.ToString(),
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out _),
                "STRING"   => prop.Value is string s && s.Length > 0,
                _ => true
            };

            if (!valid)
            {
                results.Add(new ValidationResult
                {
                    ElementGuid     = element.GlobalId,
                    ElementName     = element.Name,
                    IfcClass        = element.IfcClass,
                    StoreyName      = element.StoreyName,
                    CheckLevel      = CheckLevel.PropertyValueDataType,
                    Severity        = Severity.Error,
                    Country         = mode,
                    AffectedAgency  = rule.AffectedAgency,
                    PropertySetName = rule.PropertySetName,
                    PropertyName    = rule.PropertyName,
                    Message         = $"Property '{rule.PropertySetName}.{rule.PropertyName}' has incorrect data type. " +
                                      $"Expected {rule.ExpectedType} but got '{prop.Value}'.",
                    ExpectedValue   = $"A valid {rule.ExpectedType} value",
                    ActualValue     = prop.Value?.ToString() ?? "(empty)"
                });
            }
        }

        return results;
    }

    private List<ValidationResult> Check_Level10_PropertyValueEnumeration(
        IfcElement element, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        var enumRules = _db.GetEnumerationRules(element.IfcClass, mode);
        foreach (var rule in enumRules)
        {
            var pset = element.PropertySets
                .FirstOrDefault(p => p.Name.Equals(rule.PropertySetName, StringComparison.OrdinalIgnoreCase));
            if (pset == null) continue;

            var prop = pset.GetProperty(rule.PropertyName);
            if (prop == null || !prop.IsPopulated) continue;

            var actual = prop.Value?.ToString() ?? string.Empty;
            if (!rule.PermittedValues.Contains(actual, StringComparer.OrdinalIgnoreCase))
            {
                results.Add(new ValidationResult
                {
                    ElementGuid     = element.GlobalId,
                    ElementName     = element.Name,
                    IfcClass        = element.IfcClass,
                    StoreyName      = element.StoreyName,
                    CheckLevel      = CheckLevel.PropertyValueEnumeration,
                    Severity        = Severity.Error,
                    Country         = mode,
                    AffectedAgency  = rule.AffectedAgency,
                    PropertySetName = rule.PropertySetName,
                    PropertyName    = rule.PropertyName,
                    Message         = $"Property '{rule.PropertySetName}.{rule.PropertyName}' value '{actual}' " +
                                      "is not in the list of permitted values.",
                    ExpectedValue   = $"One of: {string.Join(", ", rule.PermittedValues)}",
                    ActualValue     = actual,
                    RemediationGuidance =
                        $"Change the value to one of the permitted values: {string.Join(", ", rule.PermittedValues)}",
                    RuleSource      = rule.RuleSource
                });
            }
        }

        return results;
    }

    private List<ValidationResult> Check_Level11_SpatialContainment(IfcElement element)
    {
        var results = new List<ValidationResult>();

        if (string.IsNullOrWhiteSpace(element.StoreyGuid))
        {
            results.Add(new ValidationResult
            {
                ElementGuid = element.GlobalId,
                ElementName = element.Name,
                IfcClass    = element.IfcClass,
                CheckLevel  = CheckLevel.SpatialContainment,
                Severity    = Severity.Warning,
                Message     = "Element is not contained within any IfcBuildingStorey. " +
                              "All physical elements should be spatially assigned to a storey " +
                              "for proper review by regulatory agencies.",
                ExpectedValue = "Contained in an IfcBuildingStorey",
                ActualValue   = "(not assigned to any storey)",
                RemediationGuidance =
                    "In your BIM software, ensure this element is placed within the correct building storey. " +
                    "In ArchiCAD, check the Home Storey assignment. In Revit, check the Level parameter."
            });
        }

        return results;
    }

    private List<ValidationResult> Check_Level16_MaterialAssignment(
        IfcElement element, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        // Material is required for structural and fire-rated elements
        bool requiresMaterial = _db.RequiresMaterial(element.IfcClass, mode);
        if (!requiresMaterial) return results;

        if (element.Materials.Count == 0)
        {
            results.Add(new ValidationResult
            {
                ElementGuid     = element.GlobalId,
                ElementName     = element.Name,
                IfcClass        = element.IfcClass,
                StoreyName      = element.StoreyName,
                CheckLevel      = CheckLevel.MaterialAssignment,
                Severity        = Severity.Warning,
                Country         = mode,
                AffectedAgency  = mode == CountryMode.Singapore ? SgAgency.BCA : SgAgency.None,
                Message         = $"No material is assigned to this {element.IfcClass} element. " +
                                  "BCA (Singapore) and UBBL (Malaysia) require material specification " +
                                  "for structural and fire-rated elements.",
                ExpectedValue   = "At least one material assigned",
                ActualValue     = "(no material)",
                RemediationGuidance =
                    "Assign the correct material to this element in your BIM software. " +
                    "Ensure the material name follows the IFC+SG naming conventions."
            });
        }

        return results;
    }

    private List<ValidationResult> Check_Level17_SpaceBoundary(
        IfcElement element, CountryMode mode)
    {
        // Space boundary check is handled separately for IfcSpace elements
        var results = new List<ValidationResult>();

        // Check that the space has a category
        var pset = element.PropertySets
            .FirstOrDefault(p => p.Name.Equals("Pset_SpaceCommon", StringComparison.OrdinalIgnoreCase));

        if (pset == null || !pset.HasProperty("Category"))
        {
            results.Add(new ValidationResult
            {
                ElementGuid     = element.GlobalId,
                ElementName     = element.Name,
                IfcClass        = "IFCSPACE",
                StoreyName      = element.StoreyName,
                CheckLevel      = CheckLevel.SpaceBoundaryIntegrity,
                Severity        = mode == CountryMode.Singapore ? Severity.Error : Severity.Warning,
                Country         = mode,
                AffectedAgency  = SgAgency.URA,
                Message         = "IfcSpace is missing the Category property in Pset_SpaceCommon. " +
                                  "URA (Singapore) and UBBL (Malaysia) require space category for planning review.",
                PropertySetName = "Pset_SpaceCommon",
                PropertyName    = "Category",
                ExpectedValue   = "A valid space use category (e.g. 'RESIDENTIAL', 'OFFICE', 'CARPARK')",
                ActualValue     = "(not set)",
                RemediationGuidance =
                    "Set the space category in Pset_SpaceCommon.Category. " +
                    "Refer to the IFC+SG Industry Mapping for valid category values."
            });
        }

        return results;
    }

    private List<ValidationResult> Check_Level18_GeometryValidity(IfcElement element)
    {
        var results = new List<ValidationResult>();
        if (element.BoundingBox == null) return results;

        if (element.BoundingBox.IsDegenerate)
        {
            results.Add(new ValidationResult
            {
                ElementGuid = element.GlobalId,
                ElementName = element.Name,
                IfcClass    = element.IfcClass,
                StoreyName  = element.StoreyName,
                CheckLevel  = CheckLevel.GeometryValidity,
                Severity    = Severity.Warning,
                Message     = "Element has degenerate or zero-dimension geometry (zero width, height, or depth). " +
                              "Degenerate geometry can cause visual and analytical failures in CORENET-X review.",
                ExpectedValue = "Non-zero geometry in all dimensions",
                ActualValue   = $"BBox: [{element.BoundingBox.MinX:F3},{element.BoundingBox.MinY:F3},{element.BoundingBox.MinZ:F3}] " +
                                $"to [{element.BoundingBox.MaxX:F3},{element.BoundingBox.MaxY:F3},{element.BoundingBox.MaxZ:F3}]",
                RemediationGuidance =
                    "Check this element in your BIM software for incorrect dimensions or accidental zero-size modelling."
            });
        }

        return results;
    }

    // ─── FILE-LEVEL CHECKS ────────────────────────────────────────────────────

    private List<ValidationResult> RunFileLevelChecks(IfcFile file, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        // Level 12: Storey elevation consistency  -  checked within file
        results.AddRange(Check_Level12_StoreyElevations(file));

        // Level 13: Georeferencing
        if (mode is CountryMode.Singapore or CountryMode.Combined)
            results.AddRange(Check_Level13_Georeferencing_SG(file));
        if (mode is CountryMode.Malaysia or CountryMode.Combined)
            results.AddRange(Check_Level13_Georeferencing_MY(file));

        // Level 14: Site and building hierarchy
        results.AddRange(Check_Level14_SiteAndBuildingHierarchy(file));

        // Level 15: GUID uniqueness
        results.AddRange(Check_Level15_GuidUniqueness(file));

        // Level 19: IFC schema version
        results.AddRange(Check_Level19_SchemaVersion(file, mode));

        // Level 20: File header completeness
        results.AddRange(Check_Level20_FileHeader(file));

        return results;
    }

    private static List<ValidationResult> Check_Level12_StoreyElevations(IfcFile file)
    {
        var results = new List<ValidationResult>();
        var storeys = file.Storeys.Where(s => s.Elevation.HasValue)
            .OrderBy(s => s.Elevation).ToList();

        // Check for duplicate elevations (different named storeys at same height)
        var byElevation = storeys.GroupBy(s => Math.Round(s.Elevation!.Value, 3));
        foreach (var group in byElevation.Where(g => g.Count() > 1))
        {
            results.Add(new ValidationResult
            {
                CheckLevel   = CheckLevel.StoreyElevation,
                Severity     = Severity.Warning,
                Message      = $"Multiple storeys share the same elevation {group.Key:F3}m: " +
                               string.Join(", ", group.Select(s => s.Name)),
                RemediationGuidance =
                    "Ensure each storey has a unique elevation. Duplicate elevations can cause " +
                    "spatial containment issues and confusion during regulatory review."
            });
        }

        return results;
    }

    private static List<ValidationResult> Check_Level13_Georeferencing_SG(IfcFile file)
    {
        var results = new List<ValidationResult>();

        if (file.Georeference == null || !file.Georeference.HasMapConversion)
        {
            results.Add(new ValidationResult
            {
                CheckLevel   = CheckLevel.Georeferencing,
                Severity     = Severity.Critical,
                Country      = CountryMode.Singapore,
                AffectedAgency = SgAgency.BCA,
                Message      = "IfcMapConversion is missing. Georeferencing is mandatory for all CORENET-X IFC+SG submissions. " +
                               "The model must be positioned in Singapore's SVY21 coordinate system (EPSG:3414).",
                ExpectedValue = "IfcMapConversion with Eastings, Northings in SVY21 coordinates",
                ActualValue  = "(not present)",
                RemediationGuidance =
                    "Set up georeferencing in your BIM software using the SVY21 coordinate system. " +
                    "In ArchiCAD: Project Location > Coordinate System > use IfcMapConversion. " +
                    "In Revit: Manage > Location and Shared Coordinates with IFC+SG export settings. " +
                    "Reference the CORENET-X COP Section on Georeferencing for SVY21 datum values.",
                RuleSource = "CORENET-X COP 3rd Edition  -  Georeferencing Requirements"
            });
        }
        else if (!file.Georeference.IsValidForSingapore)
        {
            results.Add(new ValidationResult
            {
                CheckLevel   = CheckLevel.Georeferencing,
                Severity     = Severity.Error,
                Country      = CountryMode.Singapore,
                AffectedAgency = SgAgency.SLA,
                Message      = "IfcProjectedCRS does not reference SVY21 (EPSG:3414). Singapore submissions require the SVY21 coordinate system.",
                ExpectedValue = "IfcProjectedCRS with CrsName referencing SVY21 or EPSG:3414",
                ActualValue  = file.Georeference.CrsName ?? "(unknown CRS)",
                RemediationGuidance =
                    "Update the IfcProjectedCRS to reference SVY21. " +
                    "CrsName should be 'SVY21 / Singapore TM' or reference EPSG:3414.",
                RuleSource = "CORENET-X COP  -  SVY21 Requirement"
            });
        }

        return results;
    }

    private static List<ValidationResult> Check_Level13_Georeferencing_MY(IfcFile file)
    {
        var results = new List<ValidationResult>();

        if (file.Georeference == null || !file.Georeference.HasMapConversion)
        {
            results.Add(new ValidationResult
            {
                CheckLevel   = CheckLevel.Georeferencing,
                Severity     = Severity.Warning,
                Country      = CountryMode.Malaysia,
                Message      = "IfcMapConversion is missing. Georeferencing is recommended for NBeS submissions. " +
                               "Malaysia uses the GDM2000 geodetic datum (West Malaysia RSO or Cassini projection per state).",
                ExpectedValue = "IfcMapConversion with GDM2000 coordinates",
                ActualValue  = "(not present)",
                RemediationGuidance =
                    "Set up georeferencing in your BIM software using the GDM2000 datum. " +
                    "The specific projection varies by state in Malaysia. " +
                    "Contact your local authority (PBT) for the required coordinate system."
            });
        }

        return results;
    }

    private static List<ValidationResult> Check_Level14_SiteAndBuildingHierarchy(IfcFile file)
    {
        var results = new List<ValidationResult>();

        if (file.Sites.Count == 0)
        {
            results.Add(new ValidationResult
            {
                CheckLevel = CheckLevel.SiteAndBuildingHierarchy,
                Severity   = Severity.Critical,
                Message    = "No IfcSite entity found. A valid IFC model must contain exactly one IfcSite.",
                RemediationGuidance = "Ensure your BIM project has a Site level defined and is exported with the full project hierarchy."
            });
        }
        else if (file.Sites.Count > 1)
        {
            results.Add(new ValidationResult
            {
                CheckLevel = CheckLevel.SiteAndBuildingHierarchy,
                Severity   = Severity.Warning,
                Message    = $"Multiple IfcSite entities found ({file.Sites.Count}). CORENET-X expects a single IfcSite per submission file.",
                RemediationGuidance = "Check your IFC export settings to ensure only one IfcSite is exported per file."
            });
        }

        if (file.Buildings.Count == 0)
        {
            results.Add(new ValidationResult
            {
                CheckLevel = CheckLevel.SiteAndBuildingHierarchy,
                Severity   = Severity.Critical,
                Message    = "No IfcBuilding entity found. A valid IFC model must contain at least one IfcBuilding.",
                RemediationGuidance = "Ensure your BIM project has a Building level defined in the spatial hierarchy."
            });
        }

        if (file.Storeys.Count == 0)
        {
            results.Add(new ValidationResult
            {
                CheckLevel = CheckLevel.SiteAndBuildingHierarchy,
                Severity   = Severity.Critical,
                Message    = "No IfcBuildingStorey entities found. All buildings must have at least one storey defined.",
                RemediationGuidance = "Define building storeys in your BIM software and ensure they are exported in the IFC hierarchy."
            });
        }

        return results;
    }

    private static List<ValidationResult> Check_Level15_GuidUniqueness(IfcFile file)
    {
        var results = new List<ValidationResult>();

        var allGuids = file.Elements.Select(e => e.GlobalId).ToList();
        var duplicates = allGuids.GroupBy(g => g)
            .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
            .ToList();

        foreach (var dup in duplicates)
        {
            results.Add(new ValidationResult
            {
                CheckLevel  = CheckLevel.GuidUniqueness,
                Severity    = Severity.Critical,
                Message     = $"Duplicate GUID found: '{dup.Key}' appears {dup.Count()} times. " +
                              "Every IFC element must have a unique GlobalId across the entire model.",
                ActualValue = dup.Key,
                RemediationGuidance =
                    "Regenerate GUIDs for duplicate elements. " +
                    "In ArchiCAD, this can be caused by copying elements without resetting IDs. " +
                    "In Revit, check that linked models are not creating duplicate GUID assignments."
            });
        }

        return results;
    }

    private static List<ValidationResult> Check_Level19_SchemaVersion(IfcFile file, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        if (mode is CountryMode.Singapore or CountryMode.Combined)
        {
            if (file.Schema != IfcSchemaVersion.IFC4 && file.Schema != IfcSchemaVersion.IFC4X3)
            {
                results.Add(new ValidationResult
                {
                    CheckLevel  = CheckLevel.IfcSchemaVersion,
                    Severity    = Severity.Error,
                    Country     = CountryMode.Singapore,
                    Message     = $"IFC schema version is '{file.Header.SchemaIdentifier}'. " +
                                  "CORENET-X IFC+SG submissions must use IFC4 Reference View (IFC4 ADD2 TC1).",
                    ExpectedValue = "IFC4 (IFC4 Reference View)",
                    ActualValue   = file.Header.SchemaIdentifier,
                    RemediationGuidance =
                        "Re-export your IFC model using IFC4 schema. " +
                        "In ArchiCAD: File > Interoperability > IFC > Save IFC, select IFC4 Reference View. " +
                        "In Revit: IFC Export Settings > IFC Version = IFC4 Reference View."
                });
            }
        }

        return results;
    }

    private static List<ValidationResult> Check_Level20_FileHeader(IfcFile file)
    {
        var results = new List<ValidationResult>();

        if (!file.Header.IsComplete)
        {
            results.Add(new ValidationResult
            {
                CheckLevel  = CheckLevel.FileHeaderCompleteness,
                Severity    = Severity.Warning,
                Message     = "IFC file header is incomplete. Missing: " +
                              string.Join(", ", new[]
                              {
                                  string.IsNullOrWhiteSpace(file.Header.OriginatingSystem) ? "OriginatingSystem" : null,
                                  string.IsNullOrWhiteSpace(file.Header.SchemaIdentifier) ? "SchemaIdentifier" : null,
                                  file.Header.TimeStamp == default ? "TimeStamp" : null
                              }.Where(s => s != null)),
                RemediationGuidance =
                    "Ensure your BIM software exports a complete IFC file header with the authoring application name, " +
                    "schema identifier, and creation date."
            });
        }

        return results;
    }

    // ─── FEDERATED MODEL CHECKS ───────────────────────────────────────────────

    private static List<ValidationResult> RunFederatedChecks(
        List<IfcFile> files, CountryMode mode)
    {
        var results = new List<ValidationResult>();

        // Check GUID uniqueness across files
        var allGuids = files.SelectMany(f => f.Elements.Select(e => e.GlobalId)).ToList();
        var crossFileDuplicates = allGuids.GroupBy(g => g)
            .Where(g => g.Count() > 1 && !string.IsNullOrWhiteSpace(g.Key))
            .ToList();

        if (crossFileDuplicates.Any())
        {
            results.Add(new ValidationResult
            {
                CheckLevel  = CheckLevel.GuidUniqueness,
                Severity    = Severity.Critical,
                Message     = $"{crossFileDuplicates.Count} GUIDs appear in multiple IFC files in the federated model. " +
                              "GUIDs must be unique across all discipline models in a CORENET-X federated submission.",
                RemediationGuidance =
                    "Ensure each discipline team regenerates GUIDs and does not copy elements " +
                    "from other discipline models without resetting GUIDs."
            });
        }

        // Check storey elevation consistency across files
        var storeyElevsByName = files
            .SelectMany(f => f.Storeys)
            .Where(s => s.Elevation.HasValue)
            .GroupBy(s => s.Name.Trim().ToUpperInvariant())
            .Where(g => g.Select(s => Math.Round(s.Elevation!.Value, 2)).Distinct().Count() > 1)
            .ToList();

        foreach (var inconsistency in storeyElevsByName)
        {
            var elevations = inconsistency
                .Select(s => $"{Math.Round(s.Elevation!.Value, 3):F3}m").Distinct();
            results.Add(new ValidationResult
            {
                CheckLevel  = CheckLevel.StoreyElevation,
                Severity    = Severity.Error,
                Message     = $"Storey '{inconsistency.Key}' has inconsistent elevations across discipline models: " +
                              string.Join(", ", elevations) + ". " +
                              "Storey elevations must match exactly across all discipline IFC files.",
                RemediationGuidance =
                    "Align storey elevations between all discipline BIM files (Architecture, C&S, M&E). " +
                    "Use a shared project reference file to synchronise datum levels."
            });
        }

        return results;
    }

    // ─── STATISTICS COMPILATION ───────────────────────────────────────────────

    private static void CompileStatistics(ValidationSession session, List<IfcElement> elements)
    {
        session.ProxyElements = elements.Count(e => e.IsProxy);

        foreach (var element in elements)
        {
            var severity = element.OverallSeverity;
            if (severity == Severity.Pass)         session.PassedElements++;
            else if (severity == Severity.Warning) session.WarningElements++;
            else if (severity == Severity.Error)   session.ErrorElements++;
            else if (severity == Severity.Critical) session.CriticalElements++;
        }

        // By agency (Singapore)
        session.ErrorsByAgency = session.Results
            .Where(r => r.AffectedAgency != SgAgency.None && r.Severity >= Severity.Error)
            .GroupBy(r => r.AffectedAgency)
            .ToDictionary(g => g.Key, g => g.Count());

        // By check level
        session.ErrorsByCheckLevel = session.Results
            .Where(r => r.Severity >= Severity.Error)
            .GroupBy(r => r.CheckLevel)
            .ToDictionary(g => g.Key, g => g.Count());
    }
}
