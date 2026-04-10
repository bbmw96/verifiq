// VERIFIQ  -  Malaysia Design Code Rules
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// Comprehensive design code checking for Malaysia:
//   • UBBL 1984 Part III  -  Space, Light and Ventilation
//   • UBBL 1984 Part V   -  Structural Requirements
//   • UBBL 1984 Part VI  -  Constructional Requirements
//   • UBBL 1984 Part VII  -  Fire Requirements
//   • UBBL 1984 Part VIII  -  Fire Alarms and Extinguishing
//   • UBBL 1984 Part IX   -  Special Requirements
//   • MS 1184:2014  -  Code of Practice for Access for Disabled People
//   • JBPM Fire Safety Requirements 2020
//   • GBI (Green Building Index) Malaysia  -  Building Design
//   • Uniform Building By-Laws  -  By-Law 47 (Headroom), By-Law 116 (Sanitary)

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Rules.MY;

public static class MalaysiaDesignRules
{
    public static List<DesignCodeRule> GetAllRules() =>
    [
        // ══════════════════════════════════════════════════════════════════════
        // UBBL 1984 PART III  -  SPACE, LIGHT AND VENTILATION
        // ══════════════════════════════════════════════════════════════════════

        // ── Minimum Room Sizes ────────────────────────────────────────────────

        new() {
            RuleId       = "MY-UBBL-III-001",
            RuleName     = "Bedroom  -  Minimum Floor Area (UBBL By-Law 48)",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "BEDROOM",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 6.5,
            FormulaDescription = "GrossArea(Bedroom) ≥ 6.5 m²",
            CodeReference = "UBBL 1984 By-Law 48(1)(a)  -  Habitable Rooms",
            RegulationText = "No habitable room shall have a floor area of less than 6.5 m² and shall have no dimension less than 2.3 m except in the case of a kitchen which shall not be less than 4.5 m².",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "MY-UBBL-III-002",
            RuleName     = "Habitable Room  -  Minimum Dimension",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "Width",
            CheckUnit    = "m",
            MinimumValue = 2.3,
            FormulaDescription = "Width(Habitable Room) ≥ 2.3 m",
            CodeReference = "UBBL 1984 By-Law 48(1)(b)",
            RegulationText = "No dimension of a habitable room shall be less than 2.3 m. A room with any dimension less than 2.3 m shall not be classified as a habitable room.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "MY-UBBL-III-003",
            RuleName     = "Kitchen  -  Minimum Floor Area",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "KITCHEN",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 4.5,
            FormulaDescription = "GrossArea(Kitchen) ≥ 4.5 m²",
            CodeReference = "UBBL 1984 By-Law 48(2)",
            RegulationText = "A kitchen in a residential development shall have a minimum floor area of 4.5 m².",
            FailSeverity = Severity.Error
        },

        // ── Ceiling Height ────────────────────────────────────────────────────

        new() {
            RuleId       = "MY-UBBL-III-004",
            RuleName     = "Habitable Room  -  Minimum Clear Ceiling Height",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "Height",
            CheckUnit    = "m",
            MinimumValue = 2.6,
            FormulaDescription = "CeilingHeight ≥ 2.6 m",
            CodeReference = "UBBL 1984 By-Law 47(1)",
            RegulationText = "The minimum clear headroom (height from finished floor to finished ceiling) in any habitable room shall be 2.6 m throughout. In any room in which cooking is carried out the minimum headroom shall be not less than 2.4 m.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "MY-UBBL-III-005",
            RuleName     = "Kitchen  -  Minimum Clear Ceiling Height",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "KITCHEN",
            CheckParameter = "Height",
            CheckUnit    = "m",
            MinimumValue = 2.4,
            FormulaDescription = "CeilingHeight(Kitchen) ≥ 2.4 m",
            CodeReference = "UBBL 1984 By-Law 47(2)",
            RegulationText = "In a kitchen the minimum clear headroom shall be not less than 2.4 m.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "MY-UBBL-III-006",
            RuleName     = "Corridor / Staircase  -  Minimum Clear Headroom",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CORRIDOR",
            CheckParameter = "Height",
            CheckUnit    = "m",
            MinimumValue = 2.3,
            FormulaDescription = "Headroom(Corridor) ≥ 2.3 m",
            CodeReference = "UBBL 1984 By-Law 47(3)",
            RegulationText = "Any corridor, passageway or staircase shall have a minimum clear headroom of not less than 2.3 m measured from the nosing of the stair at all points.",
            FailSeverity = Severity.Warning
        },

        // ── Natural Lighting and Ventilation ─────────────────────────────────

        new() {
            RuleId       = "MY-UBBL-III-007",
            RuleName     = "Habitable Room  -  Minimum Window Area (Natural Light)",
            Category     = DesignCodeCategory.VentilationAndLighting,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "WindowAreaRatio",
            CheckUnit    = "% of floor area",
            MinimumValue = 10.0,
            FormulaDescription = "WindowArea ≥ 10% × FloorArea",
            CodeReference = "UBBL 1984 By-Law 38  -  Natural Lighting",
            RegulationText = "Every habitable room shall be provided with windows having a total glazed area of not less than 10% of the floor area of that room for the admission of natural light.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "MY-UBBL-III-008",
            RuleName     = "Habitable Room  -  Minimum Ventilation Opening",
            Category     = DesignCodeCategory.VentilationAndLighting,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "VentilationOpeningRatio",
            CheckUnit    = "% of floor area",
            MinimumValue = 5.0,
            FormulaDescription = "VentilationOpening ≥ 5% × FloorArea",
            CodeReference = "UBBL 1984 By-Law 39  -  Natural Ventilation",
            RegulationText = "Every habitable room shall be provided with openable ventilation openings having a total area of not less than 5% of the floor area of that room.",
            FailSeverity = Severity.Error
        },

        // ── Corridor Width ────────────────────────────────────────────────────

        new() {
            RuleId       = "MY-UBBL-III-009",
            RuleName     = "Common Corridor  -  Minimum Width",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CORRIDOR",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 1050,
            FormulaDescription = "Width(Common Corridor) ≥ 1050 mm",
            CodeReference = "UBBL 1984 By-Law 139  -  Corridors and Passages",
            RegulationText = "Every corridor or passageway giving access to a room shall have a minimum clear width of 1050 mm.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // UBBL 1984 PART V  -  STRUCTURAL REQUIREMENTS
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "MY-UBBL-V-001",
            RuleName     = "RC Slab  -  Minimum Thickness",
            Category     = DesignCodeCategory.StructuralAndConstrucitonal,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSLAB",
            CheckParameter = "Thickness",
            CheckUnit    = "mm",
            MinimumValue = 100,
            FormulaDescription = "Thickness(RC Slab) ≥ 100 mm",
            CodeReference = "MS EN 1992-1-1:2010 (Eurocode 2)  -  Table NA.1 adopted via UBBL",
            RegulationText = "The minimum thickness of a reinforced concrete slab shall be 100 mm. In car parks the minimum slab thickness shall be 150 mm.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "MY-UBBL-V-002",
            RuleName     = "RC Wall  -  Minimum Thickness",
            Category     = DesignCodeCategory.StructuralAndConstrucitonal,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "Thickness",
            CheckUnit    = "mm",
            MinimumValue = 125,
            FormulaDescription = "Thickness(RC Wall) ≥ 125 mm",
            CodeReference = "UBBL 1984 By-Law 111",
            RegulationText = "All reinforced concrete walls shall have a minimum thickness of 125 mm. Walls forming part of a fire compartment shall have minimum 150 mm thickness.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // UBBL 1984 PART VII  -  FIRE REQUIREMENTS
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "MY-UBBL-VII-001",
            RuleName     = "Travel Distance to Exit  -  Purpose Groups III/IV/V",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistanceToExit",
            CheckUnit    = "m",
            MaximumValue = 30.0,
            FormulaDescription = "TravelDistance ≤ 30 m",
            CodeReference = "UBBL 1984 By-Law 166(1)",
            RegulationText = "The maximum travel distance from any point within a building to the nearest place of safety (exit) shall not exceed 30 m in any direction of travel. For sprinklered buildings this may be increased to 45 m.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-UBBL-VII-002",
            RuleName     = "Travel Distance to Exit  -  Sprinklered Building",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistanceToExit_Sprinklered",
            CheckUnit    = "m",
            MaximumValue = 45.0,
            FormulaDescription = "TravelDistance(Sprinklered) ≤ 45 m",
            CodeReference = "UBBL 1984 By-Law 166(2)",
            RegulationText = "Where the building is protected throughout with an automatic fire sprinkler system, the permitted travel distance may be increased to 45 m.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-UBBL-VII-003",
            RuleName     = "Exit Door  -  Minimum Clear Width",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCDOOR",
            SpaceCategoryFilter = "FIRE_EXIT",
            CheckParameter = "ClearOpeningWidth",
            CheckUnit    = "mm",
            MinimumValue = 850,
            FormulaDescription = "ClearWidth(Exit Door) ≥ 850 mm",
            CodeReference = "UBBL 1984 By-Law 168",
            RegulationText = "Every exit door shall have a minimum clear opening width of 850 mm when fully open.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-UBBL-VII-004",
            RuleName     = "Escape Staircase  -  Minimum Width",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSTAIRFLIGHT",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 900,
            FormulaDescription = "Width(Escape Stair) ≥ 900 mm",
            CodeReference = "UBBL 1984 By-Law 170",
            RegulationText = "Every staircase serving as a means of escape shall have a minimum clear width of 900 mm. For buildings with more than 200 occupants the minimum width shall be 1200 mm.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-UBBL-VII-005",
            RuleName     = "Fire-Rated Wall  -  Minimum Fire Resistance Period",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "FireRatingMinutes",
            CheckUnit    = "minutes",
            MinimumValue = 60,
            FormulaDescription = "FireRating(Compartment Wall) ≥ 60 minutes",
            CodeReference = "UBBL 1984 By-Law 154  -  Table A, Purpose Group III",
            RegulationText = "Walls forming boundaries of fire compartments in residential occupancies (Purpose Group III) shall achieve a minimum fire resistance period of 60 minutes.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-UBBL-VII-006",
            RuleName     = "Fire Compartment  -  Maximum Floor Area",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "FIRE_COMPARTMENT",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MaximumValue = 2000,
            FormulaDescription = "Area(Fire Compartment) ≤ 2000 m²",
            CodeReference = "UBBL 1984 By-Law 154 Table B",
            RegulationText = "The maximum floor area of a single fire compartment shall not exceed 2000 m² for residential (Purpose Group III) occupancies.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-UBBL-VII-007",
            RuleName     = "Stair Riser  -  Maximum Height",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSTAIRFLIGHT",
            CheckParameter = "RiserHeight",
            CheckUnit    = "mm",
            MaximumValue = 180,
            FormulaDescription = "RiserHeight ≤ 180 mm",
            CodeReference = "UBBL 1984 By-Law 122(1)",
            RegulationText = "In every staircase the maximum height of a riser shall be 180 mm and the minimum going shall be 225 mm. The ratio 2R + T shall be between 550 mm and 700 mm.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "MY-UBBL-VII-008",
            RuleName     = "Stair Tread  -  Minimum Going",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSTAIRFLIGHT",
            CheckParameter = "TreadDepth",
            CheckUnit    = "mm",
            MinimumValue = 225,
            FormulaDescription = "TreadDepth(Going) ≥ 225 mm",
            CodeReference = "UBBL 1984 By-Law 122(2)",
            RegulationText = "The going (tread, exclusive of nosing) of every step in a staircase shall not be less than 225 mm.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // MS 1184:2014  -  ACCESS FOR DISABLED PEOPLE
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "MY-MS1184-001",
            RuleName     = "Accessible Door  -  Minimum Clear Opening Width",
            Category     = DesignCodeCategory.MalaysiaAccessibility,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCDOOR",
            CheckParameter = "ClearOpeningWidth",
            CheckUnit    = "mm",
            MinimumValue = 900,
            FormulaDescription = "ClearOpeningWidth(Accessible Door) ≥ 900 mm",
            CodeReference = "MS 1184:2014 §6.3.1",
            RegulationText = "On accessible routes, all door openings shall provide a minimum clear opening width of 900 mm when the door is open at 90°.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-MS1184-002",
            RuleName     = "Accessible Corridor  -  Minimum Clear Width",
            Category     = DesignCodeCategory.MalaysiaAccessibility,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CORRIDOR",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 1500,
            FormulaDescription = "Width(Accessible Corridor) ≥ 1500 mm",
            CodeReference = "MS 1184:2014 §6.2.1",
            RegulationText = "The minimum clear width of an accessible corridor, walkway or passageway shall be 1500 mm to allow two wheelchairs to pass.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "MY-MS1184-003",
            RuleName     = "Accessible Ramp  -  Maximum Gradient",
            Category     = DesignCodeCategory.MalaysiaAccessibility,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCRAMP",
            CheckParameter = "SlopeRatio",
            CheckUnit    = "1:N",
            MinimumValue = 12.0,
            FormulaDescription = "SlopeRatio(Ramp) ≥ 1:12 (max gradient 8.33%)",
            CodeReference = "MS 1184:2014 §6.4.1",
            RegulationText = "The maximum gradient of a ramp on an accessible route shall be 1:12 (8.33%). A ramp with a gradient between 1:12 and 1:10 may be used only if the vertical rise does not exceed 150 mm.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-MS1184-004",
            RuleName     = "Accessible Toilet  -  Minimum Dimensions",
            Category     = DesignCodeCategory.MalaysiaAccessibility,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "ACCESSIBLE_TOILET",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 2.25,
            FormulaDescription = "Area(Accessible Toilet) ≥ 1500 mm × 1500 mm (2.25 m²)",
            CodeReference = "MS 1184:2014 §7.2.1",
            RegulationText = "An accessible toilet shall have minimum internal dimensions of 1500 mm (width) × 1500 mm (depth) with a clear turning circle of 1500 mm diameter.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // UBBL  -  SANITARY REQUIREMENTS
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "MY-UBBL-IX-001",
            RuleName     = "Sanitary Fittings  -  Minimum per Occupant (Office)",
            Category     = DesignCodeCategory.PlumbingAndDrainage,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "OFFICE",
            CheckParameter = "SanitaryFittingRatio",
            CheckUnit    = "persons per WC",
            MaximumValue = 20.0,
            FormulaDescription = "OccupantLoad / NumberOfWCs ≤ 1 WC per 20 persons",
            CodeReference = "UBBL 1984 By-Law 116  -  Table C",
            RegulationText = "For office occupancies a minimum of 1 WC shall be provided per 20 female occupants and 1 WC plus 1 urinal per 30 male occupants.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // JBPM  -  FIRE SAFETY 2020
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "MY-JBPM-001",
            RuleName     = "Dead-End Corridor  -  Maximum Length",
            Category     = DesignCodeCategory.MalaysiaFireCode,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "DEAD_END_CORRIDOR",
            CheckParameter = "Length",
            CheckUnit    = "m",
            MaximumValue = 15.0,
            FormulaDescription = "Length(Dead-End Corridor) ≤ 15 m",
            CodeReference = "JBPM Fire Safety Requirements 2020 §7.3.3",
            RegulationText = "A dead-end corridor shall not exceed 15 m in length, measured from the point of divergence from the direct escape route to the farthest point in the dead end.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "MY-JBPM-002",
            RuleName     = "Lobby Approach Stair  -  Minimum Width",
            Category     = DesignCodeCategory.MalaysiaFireCode,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSTAIRFLIGHT",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 1200,
            FormulaDescription = "Width(Lobby Approach Stair) ≥ 1200 mm",
            CodeReference = "JBPM Fire Safety Requirements 2020 §9.1.2",
            RegulationText = "Fire escape staircases with a lobby approach shall have a minimum clear width of 1200 mm where the building serves more than 500 occupants.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // GBI  -  GREEN BUILDING INDEX MALAYSIA
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "MY-GBI-001",
            RuleName     = "External Wall  -  Maximum Thermal Transmittance",
            Category     = DesignCodeCategory.MalaysiaGreenBuilding,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/(m²·K)",
            MaximumValue = 2.0,
            FormulaDescription = "U-value(External Wall) ≤ 2.0 W/(m²·K)",
            CodeReference = "GBI Non-Residential New Construction V1.0  -  Energy Efficiency §EE4",
            RegulationText = "For GBI certification, the thermal transmittance (U-value) of external walls shall not exceed 2.0 W/(m²·K) to reduce cooling load.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "MY-GBI-002",
            RuleName     = "Roof  -  Maximum Thermal Transmittance",
            Category     = DesignCodeCategory.MalaysiaGreenBuilding,
            Country      = CountryMode.Malaysia,
            IfcClassFilter = "IFCSLAB",
            PredefinedTypeFilter = "ROOF",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/(m²·K)",
            MaximumValue = 0.4,
            FormulaDescription = "U-value(Roof) ≤ 0.4 W/(m²·K)",
            CodeReference = "GBI §EE4.2",
            RegulationText = "The U-value of the roof shall not exceed 0.4 W/(m²·K) for GBI certification.",
            FailSeverity = Severity.Warning
        },
    ];

    public static List<DesignCodeRule> GetRulesForClass(string ifcClass)
    {
        return GetAllRules().Where(r =>
            string.IsNullOrEmpty(r.IfcClassFilter) ||
            r.IfcClassFilter.Equals(ifcClass, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }
}
