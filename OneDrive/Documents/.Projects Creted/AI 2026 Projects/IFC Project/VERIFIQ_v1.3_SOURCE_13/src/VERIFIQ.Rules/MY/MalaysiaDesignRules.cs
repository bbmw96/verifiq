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

        // ══════════════════════════════════════════════════════════════════════
        // MALAYSIA NBeS IFC MAPPING 2024 (CIDB 2nd Edition)
        // Additional rules from UBBL 1984, MS 1184:2014, JBPM Fire Safety 2020
        // Reference: NBeS IFC Mapping 2024 + UBBL 1984 + JBPM 2020
        // ══════════════════════════════════════════════════════════════════════

        // ── UBBL 1984 - STRUCTURAL ────────────────────────────────────────────
        new() {
            RuleId = "MY-UBBL-V-003", RuleName = "RC Beam - Minimum Width",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCBEAM", CheckParameter = "Width", CheckUnit = "mm", MinimumValue = 200,
            FormulaDescription = "RCBeam.Width >= 200mm",
            CodeReference = "UBBL 1984 Third Schedule / MS EN 1992-1-1:2010",
            RegulationText = "Reinforced concrete beams shall have a minimum width of 200mm to accommodate adequate reinforcement cover and bar spacing. Cover to reinforcement shall comply with MS EN 1992-1-1.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "MY-UBBL-V-004", RuleName = "RC Column - Minimum Dimension",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCCOLUMN", CheckParameter = "Width", CheckUnit = "mm", MinimumValue = 200,
            FormulaDescription = "RCColumn.MinDimension >= 200mm",
            CodeReference = "UBBL 1984 Third Schedule / MS EN 1992-1-1:2010 §9.5",
            RegulationText = "RC columns shall have a minimum cross-sectional dimension of 200mm. The minimum column dimension relates to bar spacing, cover and effective slenderness requirements.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "MY-UBBL-V-005", RuleName = "Pile - ConstructionMethod Required",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCPILE", CheckParameter = "ConstructionMethod", CheckUnit = "",
            FormulaDescription = "Pile.ConstructionMethod must be set (BORED/DRIVEN/MICRO/CAISSON)",
            CodeReference = "UBBL 1984 Third Schedule - Foundation Works / JKR Standard",
            RegulationText = "All piles must declare construction method: BORED (bored cast-in-situ), DRIVEN (driven precast), MICRO (micropile/minipile), or CAISSON. Required by Malaysia CIDB NBeS IFC Mapping 2024.",
            FailSeverity = Severity.Critical
        },

        // ── UBBL 1984 - FIRE SAFETY (BY-LAW 130-180) ─────────────────────────
        new() {
            RuleId = "MY-UBBL-VII-009", RuleName = "Fire Door - Self-Closing Device Required",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCDOOR", CheckParameter = "FireRating", CheckUnit = "min", MinimumValue = 60,
            FormulaDescription = "FireDoor.FireRating >= 60 min (1 hour)",
            CodeReference = "UBBL 1984 By-Law 134 / JBPM Fire Safety Requirements 2020",
            RegulationText = "Fire doors in fire-resisting enclosures shall have a minimum fire resistance of 1 hour (60 minutes) and shall be fitted with self-closing devices. Fire doors to protected staircases shall be smoke-sealed.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "MY-UBBL-VII-010", RuleName = "Protected Staircase - FRP Minimum",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCWALL", CheckParameter = "FireRating", CheckUnit = "min", MinimumValue = 60,
            FormulaDescription = "ProtectedStaircaseWall.FireRating >= 60 min",
            CodeReference = "UBBL 1984 By-Law 133 / JBPM Fire Safety Requirements 2020 §4.3",
            RegulationText = "Walls enclosing protected staircases shall have a minimum fire resistance of 1 hour (60 minutes). Buildings above 18m require 2-hour fire resistance for staircase enclosures.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "MY-UBBL-VII-011", RuleName = "Exit Signage - Required on All Exit Doors",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCDOOR", CheckParameter = "FireExit", CheckUnit = "",
            FormulaDescription = "ExitDoor.FireExit = TRUE where door serves means of escape",
            CodeReference = "UBBL 1984 By-Law 135 / JBPM Fire Safety Requirements 2020",
            RegulationText = "All exit doors shall be marked with FireExit = TRUE. Exit signs shall be provided above all exit doors and at all points of decision in escape routes, illuminated by emergency lighting.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "MY-UBBL-VII-012", RuleName = "Emergency Lighting - Required in All Escape Routes",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE", CheckParameter = "EmergencyLighting", CheckUnit = "",
            SpaceCategoryFilter = "CORRIDOR",
            FormulaDescription = "EscapeRoute.EmergencyLighting = TRUE",
            CodeReference = "UBBL 1984 By-Law 136 / JBPM Fire Safety Requirements 2020 §5.2",
            RegulationText = "All escape routes, staircases, corridors and lobbies shall be provided with emergency lighting capable of maintaining illuminance of minimum 1 lux at floor level for minimum 3 hours.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "MY-UBBL-VII-013", RuleName = "Sprinkler System - Required above Threshold Area",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE", CheckParameter = "SprinklerProvided", CheckUnit = "",
            FormulaDescription = "Space.SprinklerProvided = TRUE for buildings requiring sprinklers per UBBL",
            CodeReference = "UBBL 1984 By-Law 147 / JBPM Fire Safety Requirements 2020 §6",
            RegulationText = "Automatic sprinkler systems are required in: all buildings exceeding 30m in height, all buildings with total floor area exceeding 5,000m², all basement car parks, and all shopping complexes.",
            FailSeverity = Severity.Critical
        },

        // ── UBBL 1984 - ACCESSIBILITY (MS 1184:2014) ─────────────────────────
        new() {
            RuleId = "MY-MS1184-005", RuleName = "Accessible Lift - Minimum Car Dimensions",
            Category = DesignCodeCategory.AccessibilityAndUniversalDesign, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCTRANSPORTELEMENT", CheckParameter = "BarrierFreeAccessibility", CheckUnit = "",
            FormulaDescription = "AccessibleLift.CarWidth >= 1100mm AND CarDepth >= 1400mm",
            CodeReference = "MS 1184:2014 §6.1 - Lifts for Persons with Disabilities",
            RegulationText = "Accessible lifts for OKU (Orang Kurang Upaya) shall have minimum car dimensions of 1,100mm width x 1,400mm depth, minimum door clear opening of 900mm, and audible plus visual floor indicators.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "MY-MS1184-006", RuleName = "Accessible Parking Bay - Minimum Width",
            Category = DesignCodeCategory.AccessibilityAndUniversalDesign, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY", CheckParameter = "BayWidth", CheckUnit = "mm", MinimumValue = 3700,
            FormulaDescription = "OKUParkingBay.BayWidth >= 3700mm",
            CodeReference = "MS 1184:2014 §7.2 - Parking for OKU",
            RegulationText = "OKU parking bays shall have minimum width of 3,700mm (including side aisle transfer space of minimum 1,500mm). OKU bays shall be located nearest to accessible entrances and provided at minimum ratio of 1:50 bays.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "MY-MS1184-007", RuleName = "Accessible Toilet - Turning Circle",
            Category = DesignCodeCategory.AccessibilityAndUniversalDesign, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE", CheckParameter = "GrossArea", CheckUnit = "m²", MinimumValue = 3.2,
            SpaceCategoryFilter = "ACCESSIBLE_TOILET",
            FormulaDescription = "OKUToilet.GrossArea >= 3.2 m² (1600mm x 2000mm minimum)",
            CodeReference = "MS 1184:2014 §5.3 - Accessible Toilets",
            RegulationText = "OKU toilets shall have minimum clear floor area of 1,600mm x 2,000mm to accommodate wheelchair turning and transfer. Grab bars shall be provided on both sides of WC at 700-800mm height.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "MY-MS1184-008", RuleName = "Accessible Route - Minimum Width",
            Category = DesignCodeCategory.AccessibilityAndUniversalDesign, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE", CheckParameter = "Width", CheckUnit = "mm", MinimumValue = 1500,
            SpaceCategoryFilter = "ACCESSIBLE_ROUTE",
            FormulaDescription = "AccessibleRoute.Width >= 1500mm (passing places at 1800mm every 25m)",
            CodeReference = "MS 1184:2014 §4.2 - Accessible Routes",
            RegulationText = "All accessible routes shall have a minimum clear width of 1,500mm with passing places of 1,800mm minimum at regular intervals. Floor surface shall be firm, stable and slip-resistant.",
            FailSeverity = Severity.Critical
        },

        // ── UBBL 1984 - SANITARY FITTINGS (BY-LAW 90-106) ────────────────────
        new() {
            RuleId = "MY-UBBL-IX-002", RuleName = "WC - Minimum Provision (Residential)",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCSANITARYTERMINAL", CheckParameter = "SystemType", CheckUnit = "",
            FormulaDescription = "Residential units must have minimum 1 WC and 1 wash basin per unit",
            CodeReference = "UBBL 1984 By-Law 90 - Sanitary Fitments",
            RegulationText = "Every dwelling unit shall be provided with at least one water closet, one wash basin, and one bath or shower. All sanitary appliances shall comply with SIRIM standards and have WELS certification where applicable.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "MY-UBBL-IX-003", RuleName = "Grease Trap - Required for Food Premises",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCINTERCEPTOR", CheckParameter = "Capacity", CheckUnit = "L", MinimumValue = 300,
            FormulaDescription = "FoodPremisesGreaseTrap.Capacity >= 300L",
            CodeReference = "UBBL 1984 By-Law 104 / IWK Sewerage Design Guidelines",
            RegulationText = "All food premises, restaurants and commercial kitchens shall be provided with a grease interceptor/trap of minimum 300L capacity before discharge to the public sewer. Grease traps shall be accessible for maintenance.",
            FailSeverity = Severity.Error
        },

        // ── MALAYSIA NBeS IFC MAPPING 2024 - PROPERTY COMPLETENESS ───────────
        new() {
            RuleId = "MY-NBES-001", RuleName = "NBeS Mark - Required on All Structural Elements",
            Category = DesignCodeCategory.SubmissionAndDocumentation, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCBEAM", CheckParameter = "Mark", CheckUnit = "",
            FormulaDescription = "StructuralElement.Mark must be set",
            CodeReference = "NBeS IFC Mapping 2024 (CIDB 2nd Edition) - Structural Section",
            RegulationText = "All structural elements (beams, columns, slabs, walls, piles, footings) must have a Mark value. The mark must be unique within each element type and correspond to the structural drawings submitted to JKR/PWD.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "MY-NBES-002", RuleName = "NBeS MaterialGrade - Required on Structural Elements",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCBEAM", CheckParameter = "MaterialGrade", CheckUnit = "",
            FormulaDescription = "StructuralElement.MaterialGrade must be set (e.g. C30, C35, Grade 43)",
            CodeReference = "NBeS IFC Mapping 2024 (CIDB 2nd Edition)",
            RegulationText = "Material grades must be declared for all structural elements: concrete (e.g. C30, C35, C40 per MS EN 206), steel (Grade 43, Grade 50 per MS EN 10025), or timber (GL24h per MS 544). Required by CIDB NBeS mapping.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "MY-NBES-003", RuleName = "NBeS GDM2000 Georeferencing - Required",
            Category = DesignCodeCategory.GeoReferencingAndSiteData, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCSITE", CheckParameter = "RefLongitude", CheckUnit = "",
            FormulaDescription = "IfcSite must have GDM2000 coordinates (Easting/Northing in RSO Malaysia or GDM2000 LL)",
            CodeReference = "NBeS IFC Mapping 2024 - Georeferencing / JUPEM Geodetic Datum GDM2000",
            RegulationText = "Malaysian IFC models shall use GDM2000 (Geocentric Datum of Malaysia 2000) coordinate reference system. All IfcSite elements must have RefLatitude, RefLongitude, and RefElevation populated. Buildings in Peninsular Malaysia use RSO projection.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "MY-NBES-004", RuleName = "NBeS ConstructionMethod - Required on RC Elements",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCCOLUMN", CheckParameter = "ConstructionMethod", CheckUnit = "",
            FormulaDescription = "RCElement.ConstructionMethod = CIS / PC / PT / IBS",
            CodeReference = "NBeS IFC Mapping 2024 (CIDB 2nd Edition) / CIDB IBS Assessment Criteria",
            RegulationText = "Construction method must be declared for all RC elements: CIS (Cast-In-Situ), PC (Precast Concrete), PT (Post-Tensioned), or IBS (Industrialised Building System). IBS elements affect CIDB IBS Score calculation for contractor licensing.",
            FailSeverity = Severity.Error
        },

        // ── MALAYSIA - STORMWATER (MSMA 2ND EDITION) ─────────────────────────
        new() {
            RuleId = "MY-MSMA-001", RuleName = "Stormwater Drain - Minimum Gradient (MSMA)",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCPIPESEGMENT", CheckParameter = "Gradient", CheckUnit = "ratio", MinimumValue = 0.003,
            FormulaDescription = "StormwaterDrain.Gradient >= 1:333 (0.003) per MSMA",
            CodeReference = "MSMA 2nd Edition (DID Malaysia 2012) §4.4 - Open Channel Drainage",
            RegulationText = "Stormwater drains in Malaysia shall comply with the Urban Stormwater Management Manual (MSMA) 2nd Edition. Minimum longitudinal gradient for covered drains: 1:333 (0.3%). Minimum freeboard: 150mm for residential, 300mm for commercial.",
            FailSeverity = Severity.Warning
        },
        new() {
            RuleId = "MY-MSMA-002", RuleName = "OSD - On-Site Detention Required (>1ha)",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCSPACE", CheckParameter = "DetentionTankProvided", CheckUnit = "",
            FormulaDescription = "Sites > 1ha: On-Site Detention (OSD) tank required",
            CodeReference = "MSMA 2nd Edition §1.6 / DID Malaysia On-Site Detention Requirements",
            RegulationText = "Development sites exceeding 1 hectare must provide on-site stormwater detention to restrict post-development runoff to pre-development levels. OSD tank or bio-retention system required per local authority approval.",
            FailSeverity = Severity.Warning
        },

        // ── MALAYSIA - BOMBA (FIRE DEPARTMENT) REQUIREMENTS ──────────────────
        new() {
            RuleId = "MY-BOMBA-001", RuleName = "Fire Hydrant - Maximum Coverage Distance",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCFIRESUPPRESSIONTERMINAL", CheckParameter = "CoverageArea", CheckUnit = "m", MaximumValue = 90,
            FormulaDescription = "FireHydrant covers all parts of building within 90m",
            CodeReference = "JBPM Fire Safety Requirements 2020 §8.2 / UBBL 1984 By-Law 147",
            RegulationText = "External fire hydrants shall be positioned so that every part of the building is within 90m of a hydrant. Minimum water supply: 30 L/s for 2 hours. Hydrants shall be accessible to Bomba fire appliances.",
            IsRequired          = true,
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "MY-BOMBA-002", RuleName = "Fire Engine Access - Minimum Road Width",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCCIVILELEMENT", CheckParameter = "Width", CheckUnit = "mm", MinimumValue = 4500,
            FormulaDescription = "BombaAccessRoad.Width >= 4500mm",
            CodeReference = "JBPM Fire Safety Requirements 2020 §3.1 - Access for Fire Fighting",
            RegulationText = "Access roads for Bomba fire appliances shall have minimum clear width of 4,500mm and minimum headroom of 4,500mm. Turning circles and hard-standing areas shall comply with JBPM requirements.",
            IsRequired          = true,
            FailSeverity = Severity.Critical
        },

        // ── MALAYSIA - GREEN BUILDING INDEX (GBI) ADDITIONAL ─────────────────
        new() {
            RuleId = "MY-GBI-003", RuleName = "GBI Window - Maximum SHGC",
            Category = DesignCodeCategory.MalaysiaGreenBuilding, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCWINDOW", CheckParameter = "ThermalTransmittance", CheckUnit = "W/m²K", MaximumValue = 4.0,
            FormulaDescription = "Window.ThermalTransmittance <= 4.0 W/m²K (GBI baseline)",
            CodeReference = "GBI Residential New Construction V1.0 §EE1 - Energy Efficiency",
            RegulationText = "Windows and glazing shall have maximum U-value of 4.0 W/m²K for GBI certification. Solar Heat Gain Coefficient (SHGC) shall not exceed 0.4 for building facades with external radiation exposure.",
            FailSeverity = Severity.Warning
        },
        new() {
            RuleId = "MY-GBI-004", RuleName = "GBI Rainwater Harvesting - OSD Tank Declaration",
            Category = DesignCodeCategory.MalaysiaGreenBuilding, Country = CountryMode.Malaysia,
            IfcClassFilter = "IFCTANK", CheckParameter = "TankType", CheckUnit = "",
            FormulaDescription = "RainwaterHarvestingTank or RetentionTank.TankType must be declared",
            CodeReference = "GBI §WE3 - Rainwater Harvesting",
            RegulationText = "GBI projects with rainwater harvesting or stormwater retention tanks must declare TankType, Capacity, and SystemType. Minimum tank size for GBI WE3 credits: 1% of total roof area in litres.",
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
