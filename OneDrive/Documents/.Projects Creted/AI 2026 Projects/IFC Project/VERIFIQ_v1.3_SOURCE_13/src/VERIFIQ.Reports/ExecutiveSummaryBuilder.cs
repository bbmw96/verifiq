// VERIFIQ - IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// ─── EXECUTIVE SUMMARY BUILDER ───────────────────────────────────────────────
//
// Translates a full ValidationSession into a Director's Report - a one-page
// executive brief answering the questions a Principal Architect, Director, or
// Project Owner would ask before a CORENET-X or NBeS submission:
//
//   1. Can I submit this model today? (Readiness verdict)
//   2. Which agency will reject us and why?
//   3. What are the top blockers by count and by severity?
//   4. How many hours of rework does this represent?
//   5. What do I fix first?
//   6. How does this model compare to the last submission?
//
// This class is intentionally decoupled from the validation engine - it takes
// a completed ValidationSession and computes all metrics from the findings list.

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Reports;

// ─── OUTPUT MODEL ─────────────────────────────────────────────────────────────

public sealed class ExecutiveSummary
{
    // Overall submission readiness
    public ReadinessVerdict         Verdict             { get; set; }
    public int                      OverallScore        { get; set; }   // 0-100
    public string                   VerdictMessage      { get; set; } = string.Empty;
    public string                   VerdictDetail       { get; set; } = string.Empty;

    // Counts
    public int                      TotalElements       { get; set; }
    public int                      PassingElements     { get; set; }
    public int                      FailingElements     { get; set; }
    public int                      TotalFindings       { get; set; }
    public int                      BlockerCount        { get; set; }
    public int                      ErrorCount          { get; set; }
    public int                      WarningCount        { get; set; }
    public int                      AdvisoryCount       { get; set; }

    // Per-agency breakdown - ordered by risk (worst first)
    public List<AgencyRiskEntry>    AgencyRisk          { get; set; } = new();

    // Top 5 blockers - the five findings the Director needs to see
    public List<TopBlocker>         TopBlockers         { get; set; } = new();

    // Recommended fix sequence - prioritised action list
    public List<RecommendedAction>  ActionPlan          { get; set; } = new();

    // Effort estimate
    public EffortEstimate           Effort              { get; set; } = new();

    // Gateway readiness (Singapore only)
    public GatewayReadiness?        GatewayStatus       { get; set; }

    // Model quality metrics
    public ModelQuality             Quality             { get; set; } = new();

    // When generated
    public DateTime                 GeneratedAt         { get; set; } = DateTime.UtcNow;
    public CountryMode              CountryMode         { get; set; }
}

public enum ReadinessVerdict
{
    ReadyToSubmit       = 0,   // Green  - 0 blockers, 0 errors
    ConditionallyReady  = 1,   // Amber  - 0 blockers, some warnings only
    NotReady            = 2,   // Red    - blockers or errors present
    InsufficientData    = 3    // Grey   - no IFC file loaded / no validation run
}

public sealed class AgencyRiskEntry
{
    public string   Agency          { get; set; } = string.Empty;
    public string   AgencyFullName  { get; set; } = string.Empty;
    public int      BlockerCount    { get; set; }
    public int      ErrorCount      { get; set; }
    public int      WarningCount    { get; set; }
    public int      TotalIssues     { get; set; }
    public string   RiskLevel       { get; set; } = string.Empty;   // HIGH / MEDIUM / LOW / CLEAR
    public string   PrimaryIssue    { get; set; } = string.Empty;
    public string   RecommendedFix  { get; set; } = string.Empty;
}

public sealed class TopBlocker
{
    public int      Rank            { get; set; }
    public string   Severity        { get; set; } = string.Empty;
    public string   Agency          { get; set; } = string.Empty;
    public string   CheckLevel      { get; set; } = string.Empty;
    public string   Issue           { get; set; } = string.Empty;
    public int      AffectedCount   { get; set; }
    public string   Fix             { get; set; } = string.Empty;
    public string   CodeReference   { get; set; } = string.Empty;
}

public sealed class RecommendedAction
{
    public int      Priority        { get; set; }
    public string   Action          { get; set; } = string.Empty;
    public string   Why             { get; set; } = string.Empty;
    public string   Agency          { get; set; } = string.Empty;
    public string   EstimatedTime   { get; set; } = string.Empty;
    public int      IssuesResolved  { get; set; }
}

public sealed class EffortEstimate
{
    public string   Total           { get; set; } = string.Empty;
    public string   BlockerEffort   { get; set; } = string.Empty;
    public string   ErrorEffort     { get; set; } = string.Empty;
    public string   WarningEffort   { get; set; } = string.Empty;
    public string   Confidence      { get; set; } = string.Empty;
    public string   Note            { get; set; } = string.Empty;
}

public sealed class GatewayReadiness
{
    public bool     DesignGateway       { get; set; }
    public bool     PilingGateway       { get; set; }
    public bool     ConstructionGateway { get; set; }
    public bool     CompletionGateway   { get; set; }
    public string   RecommendedGateway  { get; set; } = string.Empty;
    public string   GatewayNote         { get; set; } = string.Empty;
}

public sealed class ModelQuality
{
    public int      ClassificationCoverage  { get; set; }   // % elements with classification
    public int      PropertySetCoverage     { get; set; }   // % required psets present
    public int      PropertyValueCoverage   { get; set; }   // % required values filled
    public int      GeometryHealth          { get; set; }   // % elements with valid geometry
    public int      NamingConventionScore   { get; set; }   // % elements properly named
    public string   OverallGrade            { get; set; } = string.Empty;   // A / B / C / D / F
}

// ─── BUILDER ──────────────────────────────────────────────────────────────────

public static class ExecutiveSummaryBuilder
{
    // Effort constants - minutes per issue type
    // Based on typical Kyoob/ArchiCAD workflow times:
    //   - Setting a classification in ArchiCAD IFC Manager: ~2-3 min per element
    //   - Adding a missing property set: ~5-8 min per element type (then applies to all)
    //   - Filling in a missing property value: ~1-2 min per element
    //   - Fixing a geometry issue: ~15-30 min per element
    private const double MinutesPerBlocker  = 8.0;
    private const double MinutesPerError    = 4.0;
    private const double MinutesPerWarning  = 2.0;

    private static readonly Dictionary<string, string> AgencyFullNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BCA"]     = "Building & Construction Authority",
        ["URA"]     = "Urban Redevelopment Authority",
        ["SCDF"]    = "Singapore Civil Defence Force",
        ["LTA"]     = "Land Transport Authority",
        ["NEA"]     = "National Environment Agency",
        ["PUB"]     = "Public Utilities Board",
        ["NParks"]  = "National Parks Board",
        ["SLA"]     = "Singapore Land Authority",
        ["CIDB"]    = "CIDB / Local Authority (NBeS)",
        ["JBPM"]    = "Jabatan Bomba dan Penyelamat Malaysia",
        ["None"]    = "General / Cross-Agency",
    };

    // ─── MAIN ENTRY POINT ────────────────────────────────────────────────────

    public static ExecutiveSummary Build(ValidationSession session)
    {
        if (session == null || !session.Results.Any())
            return BuildEmpty(session?.CountryMode ?? CountryMode.Singapore);

        var summary = new ExecutiveSummary
        {
            CountryMode    = session.CountryMode,
            TotalElements  = session.TotalElements,
            TotalFindings  = session.Results.Count
        };

        // Classify findings by severity
        var blockers  = session.Results.Where(r => r.Severity == Severity.Critical).ToList();
        var errors    = session.Results.Where(r => r.Severity == Severity.Error).ToList();
        var warnings  = session.Results.Where(r => r.Severity == Severity.Warning).ToList();
        var advisories= session.Results.Where(r => r.Severity == Severity.Info).ToList();
        var passes    = session.Results.Where(r => r.Severity == Severity.Pass).ToList();

        summary.BlockerCount  = blockers.Count;
        summary.ErrorCount    = errors.Count;
        summary.WarningCount  = warnings.Count;
        summary.AdvisoryCount = advisories.Count;

        // Passing elements - elements with no critical or error findings
        var failingGuids = new HashSet<string>(
            blockers.Concat(errors).Select(r => r.ElementGuid).Where(g => !string.IsNullOrEmpty(g)),
            StringComparer.OrdinalIgnoreCase);
        summary.FailingElements = failingGuids.Count;
        summary.PassingElements = Math.Max(0, session.TotalElements - summary.FailingElements);

        // Overall score (0-100)
        summary.OverallScore = ComputeScore(summary);

        // Verdict
        (summary.Verdict, summary.VerdictMessage, summary.VerdictDetail) =
            ComputeVerdict(summary);

        // Agency risk breakdown
        summary.AgencyRisk = BuildAgencyRisk(session.Results);

        // Top 5 blockers
        summary.TopBlockers = BuildTopBlockers(blockers, errors);

        // Action plan
        summary.ActionPlan = BuildActionPlan(summary.AgencyRisk, blockers, errors, warnings);

        // Effort estimate
        summary.Effort = EstimateEffort(summary);

        // Gateway readiness (Singapore only)
        if (session.CountryMode is CountryMode.Singapore or CountryMode.Combined)
            summary.GatewayStatus = BuildGatewayReadiness(session, blockers, errors);

        // Model quality metrics
        summary.Quality = BuildModelQuality(session, blockers, errors, warnings, passes);

        return summary;
    }

    // ─── SCORE ───────────────────────────────────────────────────────────────

    private static int ComputeScore(ExecutiveSummary s)
    {
        if (s.TotalElements == 0) return 0;

        // Start at 100, deduct per finding relative to element count
        double score = 100.0;
        score -= (double)s.BlockerCount  / s.TotalElements * 60;
        score -= (double)s.ErrorCount    / s.TotalElements * 30;
        score -= (double)s.WarningCount  / s.TotalElements * 10;
        return Math.Max(0, Math.Min(100, (int)Math.Round(score)));
    }

    // ─── VERDICT ─────────────────────────────────────────────────────────────

    private static (ReadinessVerdict, string, string) ComputeVerdict(ExecutiveSummary s)
    {
        if (s.TotalElements == 0)
            return (ReadinessVerdict.InsufficientData,
                "No data",
                "Load an IFC file and run validation to generate a Director's Report.");

        if (s.BlockerCount == 0 && s.ErrorCount == 0)
        {
            if (s.WarningCount == 0)
                return (ReadinessVerdict.ReadyToSubmit,
                    "Ready to Submit",
                    "No blockers or errors found. This model meets CORENET-X submission requirements.");
            else
                return (ReadinessVerdict.ConditionallyReady,
                    "Conditionally Ready",
                    $"{s.WarningCount} warning(s) found. Submission is possible but review of warnings is recommended before uploading to CORENET-X.");
        }

        var issues = new List<string>();
        if (s.BlockerCount > 0) issues.Add($"{s.BlockerCount} critical issue(s)");
        if (s.ErrorCount   > 0) issues.Add($"{s.ErrorCount} error(s)");

        return (ReadinessVerdict.NotReady,
            "Not Ready to Submit",
            $"{string.Join(" and ", issues)} must be resolved before this model can be submitted to CORENET-X. See the action plan below.");
    }

    // ─── AGENCY RISK TABLE ────────────────────────────────────────────────────

    private static List<AgencyRiskEntry> BuildAgencyRisk(List<ValidationResult> allResults)
    {
        var grouped = allResults
            .Where(r => r.Severity != Severity.Pass)
            .GroupBy(r => r.AffectedAgency.ToString())
            .ToDictionary(g => g.Key, g => g.ToList());

        // Always show all 8 SG agencies even if clear
        var agencies = new[] { "BCA", "URA", "SCDF", "LTA", "NEA", "PUB", "NParks", "SLA" };
        var entries = new List<AgencyRiskEntry>();

        foreach (var agency in agencies)
        {
            grouped.TryGetValue(agency, out var findings);
            findings ??= new List<ValidationResult>();

            var blockers = findings.Count(f => f.Severity == Severity.Critical);
            var errors   = findings.Count(f => f.Severity == Severity.Error);
            var warnings = findings.Count(f => f.Severity == Severity.Warning);
            var total    = blockers + errors + warnings;

            var riskLevel = blockers > 0 ? "HIGH"
                          : errors   > 0 ? "MEDIUM"
                          : warnings > 0 ? "LOW"
                          : "CLEAR";

            // Primary issue - most common message
            var primaryIssue = findings
                .Where(f => f.Severity is Severity.Critical or Severity.Error)
                .GroupBy(f => f.CheckLevel.ToString())
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? string.Empty;

            var recommendedFix = BuildAgencyFix(agency, primaryIssue);

            entries.Add(new AgencyRiskEntry
            {
                Agency         = agency,
                AgencyFullName = AgencyFullNames.TryGetValue(agency, out var fn) ? fn : agency,
                BlockerCount   = blockers,
                ErrorCount     = errors,
                WarningCount   = warnings,
                TotalIssues    = total,
                RiskLevel      = riskLevel,
                PrimaryIssue   = primaryIssue,
                RecommendedFix = recommendedFix
            });
        }

        // Sort: HIGH first, then MEDIUM, LOW, CLEAR
        return entries.OrderBy(e => e.RiskLevel switch
        {
            "HIGH"   => 0,
            "MEDIUM" => 1,
            "LOW"    => 2,
            _        => 3
        }).ToList();
    }

    private static string BuildAgencyFix(string agency, string checkLevel) => agency switch
    {
        "BCA"    => "Assign correct IFC entity class and structural property sets (SGPset_WallFireRating, SGPset_BeamStructural, SGPset_ColumnStructural) to all BCA-regulated elements.",
        "URA"    => "Ensure all IfcSpace elements have Pset_SpaceCommon.Category and GrossPlannedArea populated. Add SGPset_SpaceGFA.GFACategory for URA GFA computation.",
        "SCDF"   => "Set FireRating on all compartment walls/slabs. Mark fire exit doors with Pset_DoorCommon.FireExit=TRUE. Verify travel distances.",
        "LTA"    => "Confirm parking lot dimensions (2400mm width, 4800mm length). Add loading bay classification and property sets.",
        "NEA"    => "Add natural ventilation opening ratios (min 5% of floor area) to all habitable IfcSpace elements. Check exhaust provisions for toilets.",
        "PUB"    => "Confirm sanitary fitting ratios and drainage classification on plumbing elements.",
        "NParks" => "Ensure green provision is classified and landscaping area property sets are populated.",
        "SLA"    => "Confirm IfcSite has SiteID and RefElevation in Singapore Height Datum (SHD).",
        _        => "Review findings and add missing property sets per CORENET-X IFC+SG Industry Mapping 2025."
    };

    // ─── TOP BLOCKERS ─────────────────────────────────────────────────────────

    private static List<TopBlocker> BuildTopBlockers(
        List<ValidationResult> blockers,
        List<ValidationResult> errors)
    {
        var all = blockers.Concat(errors)
            .GroupBy(r => new { r.CheckLevel, r.AffectedAgency })
            .OrderByDescending(g => g.Count(r => r.Severity == Severity.Critical))
            .ThenByDescending(g => g.Count())
            .Take(5)
            .Select((g, i) => new TopBlocker
            {
                Rank          = i + 1,
                Severity      = g.Any(r => r.Severity == Severity.Critical) ? "Critical" : "Error",
                Agency        = g.Key.AffectedAgency.ToString(),
                CheckLevel    = g.Key.CheckLevel.ToString(),
                Issue         = g.First().Message    ?? "See findings table",
                AffectedCount = g.Count(),
                Fix           = g.First().RemediationGuidance ?? "Refer to CORENET-X IFC+SG Industry Mapping 2025.",
                CodeReference = g.First().RuleSource          ?? string.Empty
            }).ToList();

        return all;
    }

    // ─── ACTION PLAN ─────────────────────────────────────────────────────────

    private static List<RecommendedAction> BuildActionPlan(
        List<AgencyRiskEntry> agencyRisk,
        List<ValidationResult> blockers,
        List<ValidationResult> errors,
        List<ValidationResult> warnings)
    {
        var actions = new List<RecommendedAction>();
        int priority = 1;

        // Step 1: Classification (if classification blockers)
        var classificationIssues = blockers
            .Concat(errors)
            .Count(r => r.CheckLevel == CheckLevel.ClassificationReference || r.CheckLevel == CheckLevel.ClassificationEdition);

        if (classificationIssues > 0)
            actions.Add(new RecommendedAction
            {
                Priority       = priority++,
                Action         = "Add missing IFC classifications to all flagged elements",
                Why            = "Classification is the foundation of CORENET-X. Every element type must have a valid IFC+SG classification code. Without it, all downstream agency checks fail automatically.",
                Agency         = "All Agencies",
                EstimatedTime  = EstimateTime(classificationIssues, MinutesPerBlocker),
                IssuesResolved = classificationIssues
            });

        // Step 2: SCDF fire rating (if fire issues)
        var fireIssues = blockers
            .Concat(errors)
            .Count(r => r.AffectedAgency == SgAgency.SCDF);

        if (fireIssues > 0)
            actions.Add(new RecommendedAction
            {
                Priority       = priority++,
                Action         = "Add fire resistance ratings to all compartment walls, slabs, and doors",
                Why            = "SCDF will reject the submission if any fire-rated element is missing its FireRating or FireTestStandard property. This is a hard blocker.",
                Agency         = "SCDF",
                EstimatedTime  = EstimateTime(fireIssues, MinutesPerBlocker),
                IssuesResolved = fireIssues
            });

        // Step 3: URA spaces (if space/GFA issues)
        var uraIssues = blockers
            .Concat(errors)
            .Count(r => r.AffectedAgency == SgAgency.URA);

        if (uraIssues > 0)
            actions.Add(new RecommendedAction
            {
                Priority       = priority++,
                Action         = "Populate space categories and gross planned areas on all IfcSpace elements",
                Why            = "URA computes GFA automatically from IfcSpace property sets. Missing or incorrect categories cause GFA discrepancies that delay planning approval.",
                Agency         = "URA",
                EstimatedTime  = EstimateTime(uraIssues, MinutesPerError),
                IssuesResolved = uraIssues
            });

        // Step 4: BCA structural and accessibility
        var bcaIssues = blockers
            .Concat(errors)
            .Count(r => r.AffectedAgency == SgAgency.BCA);

        if (bcaIssues > 0)
            actions.Add(new RecommendedAction
            {
                Priority       = priority++,
                Action         = "Add BCA structural and accessibility property sets (SGPset_BeamStructural, SGPset_ColumnStructural, SGPset_DoorAccessibility)",
                Why            = "BCA checks structural adequacy data and accessibility compliance. Missing property sets prevent automated code-checking at Gateways 2 and 3.",
                Agency         = "BCA",
                EstimatedTime  = EstimateTime(bcaIssues, MinutesPerError),
                IssuesResolved = bcaIssues
            });

        // Step 5: Address remaining warnings
        var warningAgencies = warnings
            .GroupBy(w => w.AffectedAgency.ToString())
            .OrderByDescending(g => g.Count())
            .Take(2)
            .Select(g => g.Key);

        if (warnings.Count > 0)
            actions.Add(new RecommendedAction
            {
                Priority       = priority++,
                Action         = $"Review and resolve {warnings.Count} warning(s) - particularly for {string.Join(", ", warningAgencies)}",
                Why            = "Warnings do not block submission but typically generate review comments from agencies, causing delays. Clearing them before submission reduces review cycles.",
                Agency         = string.Join(", ", warningAgencies),
                EstimatedTime  = EstimateTime(warnings.Count, MinutesPerWarning),
                IssuesResolved = warnings.Count
            });

        // Final step: always recommend re-running validation
        actions.Add(new RecommendedAction
        {
            Priority       = priority,
            Action         = "Re-run full VERIFIQ validation after each fix batch to confirm resolution",
            Why            = "Some fixes have dependencies - resolving a classification issue can reveal underlying property set issues. Run VERIFIQ again after each action step.",
            Agency         = "All",
            EstimatedTime  = "5 min per run",
            IssuesResolved = 0
        });

        return actions;
    }

    // ─── EFFORT ESTIMATE ─────────────────────────────────────────────────────

    private static EffortEstimate EstimateEffort(ExecutiveSummary s)
    {
        double blockerMins  = s.BlockerCount  * MinutesPerBlocker;
        double errorMins    = s.ErrorCount    * MinutesPerError;
        double warningMins  = s.WarningCount  * MinutesPerWarning;
        double totalMins    = blockerMins + errorMins + warningMins;

        // Confidence based on how close it is to a known estimate
        var confidence = totalMins < 60  ? "High (small model, fast fix)"
                       : totalMins < 300 ? "Medium (typical project, half-day fix)"
                       : totalMins < 720 ? "Medium (large model, full-day fix)"
                       : "Low (complex model - estimate may vary significantly)";

        return new EffortEstimate
        {
            Total          = FormatMinutes(totalMins),
            BlockerEffort  = FormatMinutes(blockerMins),
            ErrorEffort    = FormatMinutes(errorMins),
            WarningEffort  = FormatMinutes(warningMins),
            Confidence     = confidence,
            Note           = "Estimates assume typical ArchiCAD IFC Manager workflow. " +
                             "Structural and fire rating data may require input from C&S or M&E engineers."
        };
    }

    // ─── GATEWAY READINESS ────────────────────────────────────────────────────

    private static GatewayReadiness BuildGatewayReadiness(
        ValidationSession session,
        List<ValidationResult> blockers,
        List<ValidationResult> errors)
    {
        // Gateway 1 (Design): classification, space categories, GFA data
        var designBlockers = blockers
            .Concat(errors)
            .Count(r => (r.CheckLevel.ToString().Contains("Classification") ||
                         r.CheckLevel.ToString().Contains("Level 4") ||
                         r.CheckLevel.ToString().Contains("Level 5")));

        // Gateway 2 (Construction): fire ratings, structural psets, accessibility
        var constructionBlockers = blockers
            .Concat(errors)
            .Count(r => r.AffectedAgency == SgAgency.SCDF ||
                        r.AffectedAgency == SgAgency.BCA);

        // Piling: pile classification and property sets
        var pilingBlockers = blockers
            .Concat(errors)
            .Count(r => r.Message != null && r.Message.Contains("pile", StringComparison.OrdinalIgnoreCase));

        bool designReady       = designBlockers       == 0;
        bool constructionReady = constructionBlockers == 0;
        bool pilingReady       = pilingBlockers       == 0;

        var recommended = !designReady       ? "Design Gateway - resolve classification and space data first"
                        : !constructionReady ? "Construction Gateway - resolve fire ratings and structural psets"
                        : "Ready for any gateway submission";

        var note = session.SgGateway switch
        {
            CorenetGateway.Design       => "Checking against Design Gateway requirements (Stage 1).",
            CorenetGateway.Construction => "Checking against Construction Gateway requirements (Stage 2 - full code compliance).",
            CorenetGateway.Completion   => "Checking against Completion Gateway requirements (CSC/TOP stage).",
            CorenetGateway.Piling       => "Checking against Piling Gateway requirements.",
            _ => "Set the target gateway in Settings to see gateway-specific readiness."
        };

        return new GatewayReadiness
        {
            DesignGateway       = designReady,
            PilingGateway       = pilingReady,
            ConstructionGateway = constructionReady,
            CompletionGateway   = constructionReady && designReady,
            RecommendedGateway  = recommended,
            GatewayNote         = note
        };
    }

    // ─── MODEL QUALITY ────────────────────────────────────────────────────────

    private static ModelQuality BuildModelQuality(
        ValidationSession session,
        List<ValidationResult> blockers,
        List<ValidationResult> errors,
        List<ValidationResult> warnings,
        List<ValidationResult> passes)
    {
        int total = Math.Max(1, session.TotalElements);

        // Classification coverage
        var noClassification = blockers
            .Concat(errors)
            .Count(r => r.CheckLevel.ToString().Contains("Classification") == true ||
                        r.CheckLevel.ToString().Contains("Level 4")        == true);
        int classificationCoverage = Math.Max(0, 100 - (noClassification * 100 / total));

        // Property set coverage
        var noPsets = blockers
            .Concat(errors)
            .Count(r => r.CheckLevel == CheckLevel.MandatoryPropertySets ||
                        r.CheckLevel == CheckLevel.SgPropertySets);
        int psetCoverage = Math.Max(0, 100 - (noPsets * 100 / total));

        // Property value coverage
        var noValues = blockers
            .Concat(errors)
            .Count(r => r.CheckLevel == CheckLevel.PropertyValuesPopulated);
        int valueCoverage = Math.Max(0, 100 - (noValues * 100 / total));

        // Geometry health (warnings and errors from geometry checks)
        var geomIssues = blockers
            .Concat(errors)
            .Concat(warnings)
            .Count(r => r.CheckLevel == CheckLevel.GeometryValidity);
        int geomHealth = Math.Max(0, 100 - (geomIssues * 100 / total));

        // Naming convention
        var namingIssues = warnings
            .Count(r => r.CheckLevel == CheckLevel.PredefinedType ||
                        r.CheckLevel == CheckLevel.ObjectTypeUserDefined);
        int namingScore = Math.Max(0, 100 - (namingIssues * 100 / total));

        // Overall grade from average
        double avg = (classificationCoverage + psetCoverage + valueCoverage + geomHealth + namingScore) / 5.0;
        var grade = avg >= 90 ? "A"
                  : avg >= 75 ? "B"
                  : avg >= 60 ? "C"
                  : avg >= 45 ? "D"
                  : "F";

        return new ModelQuality
        {
            ClassificationCoverage = classificationCoverage,
            PropertySetCoverage    = psetCoverage,
            PropertyValueCoverage  = valueCoverage,
            GeometryHealth         = geomHealth,
            NamingConventionScore  = namingScore,
            OverallGrade           = grade
        };
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private static ExecutiveSummary BuildEmpty(CountryMode mode) =>
        new()
        {
            CountryMode     = mode,
            Verdict         = ReadinessVerdict.InsufficientData,
            VerdictMessage  = "No Data",
            VerdictDetail   = "Load an IFC file and run validation to generate the Director's Report.",
            OverallScore    = 0
        };

    private static string FormatMinutes(double minutes)
    {
        if (minutes < 1)   return "< 1 min";
        if (minutes < 60)  return $"{(int)Math.Round(minutes)} min";
        if (minutes < 480) return $"{minutes / 60:0.#} hrs";
        return $"{minutes / 60 / 8:0.#} working days";
    }

    private static string EstimateTime(int count, double minsPerIssue) =>
        FormatMinutes(count * minsPerIssue);
}
