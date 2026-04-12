// VERIFIQ  -  Design Code Engine
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// Orchestrates all design code checks  -  reads actual dimensions, areas,
// distances and ratios from the IFC model and compares them against
// the applicable design code rules for Singapore and Malaysia.
// This goes beyond data presence (Level 1-20) to actual value checking.

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;
using VERIFIQ.Rules.SG;
using VERIFIQ.Rules.MY;

namespace VERIFIQ.Rules;

/// <summary>
/// Runs the full suite of design code checks on a parsed IFC model.
/// Produces DesignCodeResult objects that integrate with ValidationSession.
/// </summary>
public sealed class DesignCodeEngine
{
    public event Action<int, string>? ProgressChanged;

    // ─── PUBLIC ENTRY POINT ──────────────────────────────────────────────────

    public async Task<DesignCodeSession> RunAsync(
        List<IfcFile> files,
        CountryMode mode,
        CancellationToken ct = default)
    {
        var session = new DesignCodeSession { Country = mode };

        var allElements = files.SelectMany(f => f.Elements).ToList();
        var allSpaces   = files.SelectMany(f => f.Spaces).ToList();

        Report(5, "Running design code checks...");

        // Per-element checks
        int done = 0;
        var batches = allElements.Chunk(50).ToList();
        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            var tasks = batch.Select(element => Task.Run(() =>
                RunElementChecks(element, mode), ct)).ToList();

            var batchResults = await Task.WhenAll(tasks);
            session.Results.AddRange(batchResults.SelectMany(r => r));

            done += batch.Length;
            Report((int)(done * 0.6 / allElements.Count * 100 + 5), $"Design-checked {done} of {allElements.Count} elements...");
        }

        // Space / room checks (design intent validation)
        Report(65, "Running room-size and design-intent checks...");
        foreach (var space in allSpaces)
        {
            ct.ThrowIfCancellationRequested();
            var spaceResults = RunSpaceChecks(space, mode);
            session.Results.AddRange(spaceResults);
        }

        // Aggregate cross-model checks
        Report(85, "Running aggregate checks (balcony ratios, compartment totals)...");
        session.Results.AddRange(RunAggregateChecks(files, mode));

        // Compile statistics
        CompileStatistics(session);

        Report(100, "Design code checks complete.");
        return session;
    }

    // ─── ELEMENT-LEVEL CHECKS ────────────────────────────────────────────────

    private List<DesignCodeResult> RunElementChecks(IfcElement element, CountryMode mode)
    {
        var results = new List<DesignCodeResult>();
        var dims    = ExtractElementDimensions(element);

        var rules = mode == CountryMode.Singapore
            ? SingaporeDesignRules.GetRulesForClass(element.IfcClass)
            : mode == CountryMode.Malaysia
                ? MalaysiaDesignRules.GetRulesForClass(element.IfcClass)
                : SingaporeDesignRules.GetRulesForClass(element.IfcClass)
                    .Concat(MalaysiaDesignRules.GetRulesForClass(element.IfcClass)).ToList();

        foreach (var rule in rules)
        {
            // Skip rules that filter by space category  -  those run in RunSpaceChecks
            if (!string.IsNullOrEmpty(rule.SpaceCategoryFilter)) continue;

            // Apply PredefinedType filter
            if (!string.IsNullOrEmpty(rule.PredefinedTypeFilter) &&
                !element.PredefinedType.Equals(rule.PredefinedTypeFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var result = CheckRule(rule, element.GlobalId, element.Name,
                element.IfcClass, element.StoreyName, dims, null);

            if (result != null) results.Add(result);
        }

        return results;
    }

    // ─── SPACE / ROOM CHECKS (DESIGN INTENT VALIDATION) ─────────────────────

    private List<DesignCodeResult> RunSpaceChecks(IfcSpace space, CountryMode mode)
    {
        var results = new List<DesignCodeResult>();
        var dims    = ExtractSpaceDimensions(space);

        if (string.IsNullOrWhiteSpace(dims.Category)) return results;

        var rules = mode == CountryMode.Singapore
            ? SingaporeDesignRules.GetRulesForSpaceCategory(dims.Category)
                .Concat(SingaporeDesignRules.GetAllRules()
                    .Where(r => r.IfcClassFilter == "IFCSPACE" &&
                                string.IsNullOrEmpty(r.SpaceCategoryFilter))).ToList()
            : mode == CountryMode.Malaysia
                ? MalaysiaDesignRules.GetRulesForClass("IFCSPACE")
                : SingaporeDesignRules.GetRulesForSpaceCategory(dims.Category)
                    .Concat(MalaysiaDesignRules.GetRulesForClass("IFCSPACE")).ToList();

        foreach (var rule in rules)
        {
            // Ensure category matches when specified
            if (!string.IsNullOrEmpty(rule.SpaceCategoryFilter) &&
                !dims.Category.Equals(rule.SpaceCategoryFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            var spaceName = space.Name.Length > 0 ? space.Name : $"Space {space.GlobalId[..Math.Min(8, space.GlobalId.Length)]}";
            var result = CheckRule(rule, space.GlobalId, spaceName,
                "IFCSPACE", string.Empty, null, dims);

            if (result != null) results.Add(result);
        }

        return results;
    }

    // ─── THE CORE CHECK ───────────────────────────────────────────────────────

    private DesignCodeResult? CheckRule(
        DesignCodeRule rule,
        string guid, string name, string cls, string storey,
        ElementDimensions? elemDims,
        SpaceDimensions? spaceDims = null)
    {
        double? actualValue = GetActualValue(rule, elemDims, spaceDims);

        if (!actualValue.HasValue) return null; // Cannot determine value  -  skip

        bool complies = true;
        if (rule.MinimumValue > 0 && actualValue.Value < rule.MinimumValue)
            complies = false;
        if (rule.MaximumValue.HasValue && actualValue.Value > rule.MaximumValue.Value)
            complies = false;

        // Always return Pass results for design checks (unlike data checks)
        // because design intent compliance is an additive layer

        string requiredDisplay = BuildRequiredDisplay(rule);
        string actualDisplay   = FormatValue(actualValue.Value, rule.CheckUnit);
        string formulaResult   = BuildFormulaResult(rule, actualValue.Value, complies);

        return new DesignCodeResult
        {
            ElementGuid     = guid,
            ElementName     = name,
            IfcClass        = cls,
            StoreyName      = storey,
            SpaceCategory   = spaceDims?.Category ?? string.Empty,

            RuleId          = rule.RuleId,
            RuleName        = rule.RuleName,
            CodeReference   = rule.CodeReference,
            RegulationText  = rule.RegulationText,
            Category        = rule.Category,
            Severity        = complies ? Severity.Pass : rule.FailSeverity,
            Country         = rule.Country,
            AffectedAgency  = rule.Agency,

            CheckParameter  = rule.CheckParameter,
            CheckUnit       = rule.CheckUnit,
            ActualValue     = actualValue.Value,
            RequiredMinimum = rule.MinimumValue,
            RequiredMaximum = rule.MaximumValue,
            ActualDisplay   = actualDisplay,
            RequiredDisplay = requiredDisplay,

            Complies        = complies,
            Message         = BuildMessage(rule, name, actualDisplay, requiredDisplay, complies),
            RemediationGuidance = complies ? string.Empty : BuildRemediation(rule, actualValue.Value),

            Formula         = rule.FormulaDescription,
            FormulaResult   = formulaResult
        };
    }

    // ─── VALUE EXTRACTION ─────────────────────────────────────────────────────

    private double? GetActualValue(DesignCodeRule rule,
        ElementDimensions? elem, SpaceDimensions? space)
    {
        return rule.CheckParameter switch
        {
            "GrossPlannedArea"        => space?.GrossArea   ?? elem?.Area,
            "NetPlannedArea"          => space?.NetArea,
            "Height"                  => space?.Height      ?? ToMetres(elem?.Height),
            "Width"                   => elem?.Width ?? (space?.Width.HasValue == true ? space.Width * 1000 : null),
            "Thickness"               => elem?.Thickness,
            "Length"                  => ToMetres(elem?.Length),
            "ClearOpeningWidth"       => elem?.Width,
            "SlopeRatio"              => elem?.SlopeRatio,
            "RiserHeight"             => elem?.Height,
            "TreadDepth"              => elem?.Depth,
            "FireRatingMinutes"       => elem?.FireRatingMinutes,
            "ThermalTransmittance"    => elem?.ThermalTransmittance,
            "WindowAreaRatio"         => space?.WindowAreaRatio,
            "VentilationOpeningRatio" => space?.VentilationRatio,
            _                         => null
        };
    }

    private static double? ToMetres(double? mm) =>
        mm.HasValue ? mm.Value / 1000.0 : null;

    // ─── DIMENSION EXTRACTION FROM IFC ───────────────────────────────────────

    private static ElementDimensions ExtractElementDimensions(IfcElement element)
    {
        var dims    = new ElementDimensions();
        var allPsets = element.PropertySets;

        // ── 1. BoundingBox geometry (coarse, from IfcBoundingBox entity) ──────
        if (element.BoundingBox != null && !element.BoundingBox.IsDegenerate)
        {
            double dx = (element.BoundingBox.MaxX - element.BoundingBox.MinX) * 1000; // mm
            double dy = (element.BoundingBox.MaxY - element.BoundingBox.MinY) * 1000;
            double dz = (element.BoundingBox.MaxZ - element.BoundingBox.MinZ) * 1000;

            // For walls: thickness is the smallest horizontal dim, width is largest
            dims.Width  = Math.Max(dx, dy);
            dims.Thickness = Math.Min(dx, dy);
            dims.Height = dz;
            dims.Area   = dx * dy / 1_000_000; // m²
        }

        // ── 2. Quantity sets (BaseQuantities / Qto_*)  -  more accurate ─────────
        var qto = allPsets.FirstOrDefault(p =>
            p.Name.StartsWith("Qto_", StringComparison.OrdinalIgnoreCase) ||
            p.Name.Contains("BaseQuantities", StringComparison.OrdinalIgnoreCase));

        if (qto != null)
        {
            dims.Length    = qto.GetDoubleValue("Length");
            dims.Width   ??= qto.GetDoubleValue("Width");
            dims.Height  ??= qto.GetDoubleValue("Height");
            dims.Thickness??= qto.GetDoubleValue("Depth") ?? qto.GetDoubleValue("Thickness");
            dims.Area    ??= qto.GetDoubleValue("GrossArea") ?? qto.GetDoubleValue("NetArea");
        }

        // ── 3. Pset_WallCommon ────────────────────────────────────────────────
        var wallCommon = allPsets.FirstOrDefault(p =>
            p.Name.Equals("Pset_WallCommon", StringComparison.OrdinalIgnoreCase));
        if (wallCommon != null)
        {
            dims.FireRatingMinutes    ??= ParseFireRating(wallCommon.GetStringValue("FireRating"));
            dims.ThermalTransmittance ??= wallCommon.GetDoubleValue("ThermalTransmittance");
        }

        // ── 4. Pset_DoorCommon ────────────────────────────────────────────────
        var doorCommon = allPsets.FirstOrDefault(p =>
            p.Name.Equals("Pset_DoorCommon", StringComparison.OrdinalIgnoreCase));
        if (doorCommon != null)
        {
            // OverallWidth may be in mm or m depending on file  -  normalise to mm
            var w = doorCommon.GetDoubleValue("OverallWidth");
            if (w.HasValue)
                dims.Width = w.Value > 10 ? w.Value : w.Value * 1000;
            // Clear opening is slightly less than overall width (subtract frame ~25mm)
        }

        // ── 5. Pset_WindowCommon ──────────────────────────────────────────────
        var windowCommon = allPsets.FirstOrDefault(p =>
            p.Name.Equals("Pset_WindowCommon", StringComparison.OrdinalIgnoreCase));
        if (windowCommon != null)
        {
            dims.ThermalTransmittance ??= windowCommon.GetDoubleValue("ThermalTransmittance");
        }

        // ── 6. Pset_SlabCommon ────────────────────────────────────────────────
        var slabCommon = allPsets.FirstOrDefault(p =>
            p.Name.Equals("Pset_SlabCommon", StringComparison.OrdinalIgnoreCase));
        if (slabCommon != null)
        {
            dims.FireRatingMinutes    ??= ParseFireRating(slabCommon.GetStringValue("FireRating"));
            dims.ThermalTransmittance ??= slabCommon.GetDoubleValue("ThermalTransmittance");
        }

        // ── 7. Pset_StairFlightCommon ─────────────────────────────────────────
        var stairCommon = allPsets.FirstOrDefault(p =>
            p.Name.Equals("Pset_StairFlightCommon", StringComparison.OrdinalIgnoreCase));
        if (stairCommon != null)
        {
            var rh = stairCommon.GetDoubleValue("RiserHeight");
            if (rh.HasValue) dims.Height = rh.Value > 1 ? rh.Value : rh.Value * 1000;
            var tl = stairCommon.GetDoubleValue("TreadLength");
            if (tl.HasValue) dims.Depth  = tl.Value > 1 ? tl.Value : tl.Value * 1000;
            dims.Width ??= stairCommon.GetDoubleValue("Width");
        }

        // ── 8. Pset_RampCommon ────────────────────────────────────────────────
        var rampCommon = allPsets.FirstOrDefault(p =>
            p.Name.Equals("Pset_RampCommon", StringComparison.OrdinalIgnoreCase));
        if (rampCommon != null)
        {
            var s = rampCommon.GetDoubleValue("Slope");
            if (s.HasValue && s.Value > 0)
            {
                dims.Slope      = s.Value;
                dims.SlopeRatio = 1.0 / Math.Tan(s.Value * Math.PI / 180.0);
            }
            dims.Width ??= rampCommon.GetDoubleValue("Width");
        }

        // ── 9. Pset_BeamCommon / Pset_ColumnCommon ────────────────────────────
        foreach (var pset in allPsets)
        {
            if (pset.Name.Equals("Pset_BeamCommon",   StringComparison.OrdinalIgnoreCase) ||
                pset.Name.Equals("Pset_ColumnCommon", StringComparison.OrdinalIgnoreCase))
            {
                dims.FireRatingMinutes ??= ParseFireRating(pset.GetStringValue("FireRating"));
            }
        }

        dims.Source = dims.Area.HasValue || dims.Width.HasValue
            ? "PropertySets+BoundingBox" : "BoundingBox";

        return dims;
    }

    private static SpaceDimensions ExtractSpaceDimensions(IfcSpace space)
    {
        var dims = new SpaceDimensions();

        // ── Pset_SpaceCommon (IFC standard) ───────────────────────────────────
        var psc = space.PropertySets
            .FirstOrDefault(p => p.Name.Equals("Pset_SpaceCommon",
                StringComparison.OrdinalIgnoreCase));

        if (psc != null)
        {
            dims.Category    = psc.GetStringValue("Category")     ?? string.Empty;
            dims.IsExternal  = psc.GetBoolValue("IsExternal")     ?? false;
            dims.IsAccessible = psc.GetBoolValue("HandicapAccessible") ?? false;
            dims.GrossArea   = psc.GetDoubleValue("GrossPlannedArea");
            dims.NetArea     = psc.GetDoubleValue("NetPlannedArea");

            // Ceiling height (stored as scalar or derived from storey heights)
            dims.Height = psc.GetDoubleValue("FinishCeilingHeight");
            if (!dims.Height.HasValue)
                dims.Height = psc.GetDoubleValue("CeilingHeight");
        }

        // ── Pset_SpaceThermalRequirements ─────────────────────────────────────
        var pstr = space.PropertySets
            .FirstOrDefault(p => p.Name.Equals("Pset_SpaceThermalRequirements",
                StringComparison.OrdinalIgnoreCase));
        // (future: populate thermal dims here)

        // ── SGPset_SpaceGFA (Singapore-specific) ──────────────────────────────
        var sgGfa = space.PropertySets
            .FirstOrDefault(p => p.Name.Equals("SGPset_SpaceGFA",
                StringComparison.OrdinalIgnoreCase));
        if (sgGfa != null)
        {
            dims.GrossArea ??= sgGfa.GetDoubleValue("GrossFloorArea");
            dims.NetArea   ??= sgGfa.GetDoubleValue("NetFloorArea");
            // SGPset may store sub-category
            if (string.IsNullOrEmpty(dims.Category))
                dims.Category = sgGfa.GetStringValue("SpaceCategory") ?? string.Empty;
        }

        // ── Pset_SpaceCoverage / Qto_SpaceBaseQuantities ──────────────────────
        var qto = space.PropertySets
            .FirstOrDefault(p => p.Name.StartsWith("Qto_Space",
                StringComparison.OrdinalIgnoreCase));
        if (qto != null)
        {
            dims.GrossArea  ??= qto.GetDoubleValue("GrossFloorArea");
            dims.NetArea    ??= qto.GetDoubleValue("NetFloorArea");
            dims.Height     ??= qto.GetDoubleValue("Height");
            dims.Volume     = qto.GetDoubleValue("GrossVolume");
        }

        // ── Window area and ventilation ratios ────────────────────────────────
        // Derived: WindowAreaRatio = sum-of-adjacent-window-areas / GrossArea
        // These are approximated from Pset_SpaceOccupancyRequirements or quantity sets
        var pocc = space.PropertySets
            .FirstOrDefault(p => p.Name.Equals("Pset_SpaceOccupancyRequirements",
                StringComparison.OrdinalIgnoreCase));
        if (pocc != null)
        {
            var ventRatio = pocc.GetDoubleValue("VentilationFlowRatePerArea");
            if (ventRatio.HasValue) dims.VentilationRatio = ventRatio.Value * 100; // convert to %
        }

        // If WindowAreaRatio not set, attempt to infer from a property
        // In a real IFC file these come from Pset_WindowCommon on adjacent windows
        // For now we mark as null so the check is skipped (data not available)
        // The design code engine will return null → check skipped (PASS assumed)

        dims.AreaSource = DetermineAreaSource(psc, sgGfa, qto);
        return dims;
    }

    private static string DetermineAreaSource(
        IfcPropertySet? psc, IfcPropertySet? sgGfa, IfcPropertySet? qto)
    {
        if (sgGfa != null && sgGfa.GetDoubleValue("GrossFloorArea").HasValue) return "SGPset_SpaceGFA";
        if (psc   != null && psc.GetDoubleValue("GrossPlannedArea").HasValue)  return "Pset_SpaceCommon";
        if (qto   != null && qto.GetDoubleValue("GrossFloorArea").HasValue)    return "Qto_SpaceBaseQuantities";
        return "Not found  -  area checks skipped";
    }

    // ─── AGGREGATE CHECKS ─────────────────────────────────────────────────────

    private List<DesignCodeResult> RunAggregateChecks(List<IfcFile> files, CountryMode mode)
    {
        var results = new List<DesignCodeResult>();

        if (mode is CountryMode.Singapore or CountryMode.Combined)
            results.AddRange(CheckBalconyRatioSG(files));

        return results;
    }

    /// <summary>
    /// URA Rule: Total balcony area ≤ 10% of total GFA.
    /// Formula: Σ(BalconyArea) / Σ(TotalGFA) × 100 ≤ 10%
    /// </summary>
    private List<DesignCodeResult> CheckBalconyRatioSG(List<IfcFile> files)
    {
        var results = new List<DesignCodeResult>();

        double totalGfa     = 0;
        double totalBalcony = 0;

        foreach (var file in files)
        {
            foreach (var space in file.Spaces)
            {
                var pset = space.PropertySets
                    .FirstOrDefault(p => p.Name.Equals("Pset_SpaceCommon",
                        StringComparison.OrdinalIgnoreCase));
                if (pset == null) continue;

                double area = 0;
                if (pset.GetStringValue("GrossPlannedArea") is string gpa &&
                    double.TryParse(gpa, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double v))
                    area = v;

                var category = pset.GetStringValue("Category") ?? string.Empty;
                if (category.Equals("BALCONY", StringComparison.OrdinalIgnoreCase))
                    totalBalcony += area;
                else
                    totalGfa += area;
            }
        }

        if (totalGfa <= 0) return results;

        double ratio = totalBalcony / totalGfa * 100.0;
        bool complies = ratio <= 10.0;

        results.Add(new DesignCodeResult
        {
            ElementGuid    = "AGGREGATE",
            ElementName    = "Development  -  Total Balcony Area",
            IfcClass       = "AGGREGATE",
            RuleId         = "SG-URA-GFA-001",
            RuleName       = "Balcony Aggregate Area ≤ 10% of Total GFA",
            CodeReference  = "URA Circular  -  Guidelines on Balconies (Nov 2019)",
            RegulationText = "The total aggregate area of balconies shall not exceed 10% of the total GFA.",
            Category       = DesignCodeCategory.GrossFloorAreaRules,
            Severity       = complies ? Severity.Pass : Severity.Critical,
            Country        = CountryMode.Singapore,
            AffectedAgency = SgAgency.URA,
            CheckParameter = "BalconyRatioToGFA",
            CheckUnit      = "%",
            ActualValue    = Math.Round(ratio, 2),
            RequiredMinimum = 0,
            RequiredMaximum = 10.0,
            ActualDisplay  = $"{ratio:F1}% ({totalBalcony:F1} m² of {totalGfa:F1} m² GFA)",
            RequiredDisplay = "Max 10% of total GFA",
            Complies       = complies,
            Formula        = "Balcony Ratio = Σ(BalconyArea) ÷ Σ(GFA) × 100 ≤ 10%",
            FormulaResult  = $"= {totalBalcony:F1} ÷ {totalGfa:F1} × 100 = {ratio:F1}% {(complies ? "✓ PASS" : "✗ FAIL")}",
            Message        = complies
                ? $"Balcony aggregate ratio is {ratio:F1}% (within 10% limit)."
                : $"Balcony aggregate area is {ratio:F1}% of GFA, exceeding the 10% URA limit. " +
                  $"Total balcony: {totalBalcony:F1} m², Total GFA: {totalGfa:F1} m².",
            RemediationGuidance = complies ? string.Empty
                : $"Reduce total balcony area by {(totalBalcony - totalGfa * 0.1):F1} m² to comply with URA guidelines."
        });

        return results;
    }

    // ─── STATISTICS ───────────────────────────────────────────────────────────

    private static void CompileStatistics(DesignCodeSession session)
    {
        session.TotalChecks    = session.Results.Count;
        session.PassedChecks   = session.Results.Count(r => r.Complies);
        session.FailedChecks   = session.Results.Count(r => !r.Complies);
        session.CriticalChecks = session.Results.Count(r =>
            !r.Complies && r.Severity == Severity.Critical);

        session.FailuresByCategory = session.Results
            .Where(r => !r.Complies)
            .GroupBy(r => r.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        session.FailuresByRegulation = session.Results
            .Where(r => !r.Complies)
            .GroupBy(r => r.CodeReference)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Parses a fire rating string like "60/60/60", "FRR 90", "120", "FD60" into minutes.
    /// </summary>
    public static double? ParseFireRating(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim().ToUpperInvariant();

        // "60/60/60" or "120/120/120"  -  R/E/I notation  -  take R value
        var slashIdx = raw.IndexOf('/');
        if (slashIdx > 0)
        {
            var part = raw[..slashIdx];
            if (double.TryParse(part, out double rValue)) return rValue;
        }

        // "FD60", "FD90", "FD120"  -  fire door notation
        if (raw.StartsWith("FD"))
        {
            if (double.TryParse(raw[2..], out double fd)) return fd;
        }

        // "FRR 60", "FRR 90"
        if (raw.StartsWith("FRR"))
        {
            var num = raw.Replace("FRR", "").Trim();
            if (double.TryParse(num, out double frr)) return frr;
        }

        // Plain number "60", "120"
        if (double.TryParse(raw, out double plain)) return plain;

        return null;
    }

    /// <summary>
    /// Parses a numeric property value string to double.
    /// Handles both metres and millimetres, locale-invariant.
    /// </summary>
    private static bool TryParseDouble(string? raw, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return double.TryParse(raw.Trim(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out value);
    }

    private static string FormatValue(double value, string unit) => unit switch
    {
        "m²"   => $"{value:F2} m²",
        "m"    => $"{value:F3} m",
        "mm"   => $"{value:F0} mm",
        "1:N"  => $"1:{value:F1}",
        "%"    => $"{value:F1}%",
        "% of floor area" => $"{value:F1}%",
        "W/(m²·K)" => $"{value:F2} W/(m²·K)",
        "minutes" => $"{value:F0} min",
        "N"    => $"{value:F1} N",
        "persons per WC" => $"1:{value:F0}",
        _ => $"{value:F2} {unit}"
    };

    private static string BuildRequiredDisplay(DesignCodeRule rule)
    {
        if (rule.MinimumValue > 0 && rule.MaximumValue.HasValue)
            return $"{FormatValueStatic(rule.MinimumValue, rule.CheckUnit)} to {FormatValueStatic(rule.MaximumValue.Value, rule.CheckUnit)}";
        if (rule.MinimumValue > 0)
            return $"Min {FormatValueStatic(rule.MinimumValue, rule.CheckUnit)}";
        if (rule.MaximumValue.HasValue)
            return $"Max {FormatValueStatic(rule.MaximumValue.Value, rule.CheckUnit)}";
        return " - ";
    }

    private static string FormatValueStatic(double value, string unit) =>
        unit switch
        {
            "m²"   => $"{value:F1} m²",
            "m"    => $"{value:F1} m",
            "mm"   => $"{value:F0} mm",
            "1:N"  => $"1:{value:F0}",
            "%"    => $"{value:F0}%",
            "% of floor area" => $"{value:F0}%",
            "W/(m²·K)" => $"{value:F2} W/(m²·K)",
            "minutes" => $"{value:F0} min",
            _ => $"{value:F2} {unit}"
        };

    private static string BuildFormulaResult(DesignCodeRule rule, double actual, bool complies)
    {
        string verdict = complies ? "✓ PASS" : "✗ FAIL";
        if (rule.MinimumValue > 0 && !rule.MaximumValue.HasValue)
            return $"= {FormatValueStatic(actual, rule.CheckUnit)} vs min {FormatValueStatic(rule.MinimumValue, rule.CheckUnit)} → {verdict}";
        if (rule.MaximumValue.HasValue && rule.MinimumValue <= 0)
            return $"= {FormatValueStatic(actual, rule.CheckUnit)} vs max {FormatValueStatic(rule.MaximumValue.Value, rule.CheckUnit)} → {verdict}";
        return $"= {FormatValueStatic(actual, rule.CheckUnit)} → {verdict}";
    }

    private static string BuildMessage(DesignCodeRule rule, string name,
        string actualDisplay, string requiredDisplay, bool complies)
    {
        if (complies)
            return $"{name}: {rule.CheckParameter} is {actualDisplay}  -  complies with {rule.RuleName}.";

        return rule.MinimumValue > 0 && rule.MaximumValue == null
            ? $"{name}: {rule.CheckParameter} is {actualDisplay}, below the required minimum of {requiredDisplay}. " +
              $"Code: {rule.CodeReference}"
            : $"{name}: {rule.CheckParameter} is {actualDisplay}, exceeding the maximum of {requiredDisplay}. " +
              $"Code: {rule.CodeReference}";
    }

    private static string BuildRemediation(DesignCodeRule rule, double actual)
    {
        if (rule.MinimumValue > 0 && actual < rule.MinimumValue)
        {
            double deficit = rule.MinimumValue - actual;
            return $"Increase {rule.CheckParameter} by {FormatValueStatic(deficit, rule.CheckUnit)} to meet the minimum of " +
                   $"{FormatValueStatic(rule.MinimumValue, rule.CheckUnit)} required by {rule.CodeReference}.";
        }
        if (rule.MaximumValue.HasValue && actual > rule.MaximumValue.Value)
        {
            double excess = actual - rule.MaximumValue.Value;
            return $"Reduce {rule.CheckParameter} by {FormatValueStatic(excess, rule.CheckUnit)} to meet the maximum of " +
                   $"{FormatValueStatic(rule.MaximumValue.Value, rule.CheckUnit)} required by {rule.CodeReference}.";
        }
        return $"Review and adjust {rule.CheckParameter} to comply with {rule.CodeReference}.";
    }

    private void Report(int pct, string step) => ProgressChanged?.Invoke(pct, step);
}
