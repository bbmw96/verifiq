// VERIFIQ  -  Singapore Design Code Rules
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
//
// Comprehensive design code checking for Singapore:
//   • URA Planning Parameters (room sizes, GFA rules, setbacks, plot ratio)
//   • BCA Code on Accessibility 2025 (doors, corridors, ramps, lifts, toilets)
//   • SCDF Fire Code 2018 + 2023 Amendment (travel distances, exit widths, compartmentation)
//   • BCA Building Control Regulations (structural minimums)
//   • NEA Environmental Public Health Act (ventilation, natural lighting)
//   • PUB Sewerage and Drainage Act (sanitary fitting ratios)
//   • BCA Green Mark 2021 (WWR, U-values, thermal transmittance)
//   • LTA Transport planning (parking dimensions, loading bay)
//
// All rules reference the actual published regulation with section number.

using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;

namespace VERIFIQ.Rules.SG;

public static class SingaporeDesignRules
{
    /// <summary>
    /// Returns the complete set of Singapore design code rules.
    /// Each rule defines the check parameter, minimum/maximum value, formula, and code reference.
    /// </summary>
    public static List<DesignCodeRule> GetAllRules() =>
    [
        // ══════════════════════════════════════════════════════════════════════
        // 1. URA ROOM SIZE REQUIREMENTS
        // Reference: URA Handbook on Singapore's Planning Parameters (2023 Edition)
        // ══════════════════════════════════════════════════════════════════════

        // ── Living / Dining Rooms ────────────────────────────────────────────

        new() {
            RuleId       = "SG-URA-ROOM-001",
            RuleName     = "Living Room  -  Minimum Gross Floor Area (HDB / Public Housing)",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "LIVING_ROOM",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 16.0,
            FormulaDescription = "GrossArea(Living Room) ≥ 16.0 m²",
            CodeReference = "URA Handbook on Planning Parameters 2023  -  Residential Development (HDB)",
            RegulationText = "For public housing (HDB), the minimum living / dining room area shall not be less than 16.0 m².",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-URA-ROOM-002",
            RuleName     = "Living Room  -  Minimum Gross Floor Area (Private Residential)",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "LIVING_ROOM",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 13.0,
            FormulaDescription = "GrossArea(Living Room) ≥ 13.0 m²",
            CodeReference = "URA Development Control Parameters for Private Residential 2023 §2.3",
            RegulationText = "The minimum floor area of a living / dining room in a private residential development shall be 13.0 m².",
            FailSeverity = Severity.Error
        },

        // ── Bedrooms ─────────────────────────────────────────────────────────

        new() {
            RuleId       = "SG-URA-ROOM-003",
            RuleName     = "Bedroom  -  Minimum Floor Area",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "BEDROOM",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 9.0,
            FormulaDescription = "GrossArea(Bedroom) ≥ 9.0 m²",
            CodeReference = "URA Handbook on Planning Parameters 2023 §3.1  -  Bedroom Sizes",
            RegulationText = "Every bedroom in a residential development shall have a minimum floor area of 9.0 m² (excluding wardrobe and en-suite bathroom area).",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-URA-ROOM-004",
            RuleName     = "Master Bedroom  -  Minimum Floor Area",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "MASTER_BEDROOM",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 12.5,
            FormulaDescription = "GrossArea(Master Bedroom) ≥ 12.5 m²",
            CodeReference = "URA Handbook 2023 §3.2",
            RegulationText = "The master bedroom shall have a minimum floor area of 12.5 m².",
            FailSeverity = Severity.Warning
        },

        // ── Kitchen ──────────────────────────────────────────────────────────

        new() {
            RuleId       = "SG-URA-ROOM-005",
            RuleName     = "Kitchen  -  Minimum Floor Area",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "KITCHEN",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 4.5,
            FormulaDescription = "GrossArea(Kitchen) ≥ 4.5 m²",
            CodeReference = "URA Planning Parameters 2023 §3.3  -  Kitchen",
            RegulationText = "The kitchen shall have a minimum floor area of 4.5 m² with a minimum width of 1500 mm.",
            FailSeverity = Severity.Error
        },

        // ── Study Room ────────────────────────────────────────────────────────

        new() {
            RuleId       = "SG-URA-ROOM-006",
            RuleName     = "Study Room  -  Minimum Floor Area",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "STUDY",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 4.5,
            FormulaDescription = "GrossArea(Study) ≥ 4.5 m²",
            CodeReference = "URA Planning Parameters 2023 §3.4",
            RegulationText = "A study room shall have a minimum floor area of 4.5 m². A room below this size shall not be described as a study room on drawings.",
            FailSeverity = Severity.Warning
        },

        // ── Bathrooms / Toilets ───────────────────────────────────────────────

        new() {
            RuleId       = "SG-URA-ROOM-007",
            RuleName     = "Bathroom / Toilet  -  Minimum Floor Area",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "BATHROOM",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 2.5,
            FormulaDescription = "GrossArea(Bathroom) ≥ 2.5 m²",
            CodeReference = "URA Planning Parameters 2023 §3.5",
            RegulationText = "A bathroom / toilet shall have a minimum floor area of 2.5 m².",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-URA-ROOM-008",
            RuleName     = "Accessible Toilet  -  Minimum Floor Area",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "ACCESSIBLE_TOILET",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 2.7,
            FormulaDescription = "GrossArea(Accessible Toilet) ≥ 2.7 m²",
            CodeReference = "BCA Code on Accessibility 2025 §4.2.2",
            RegulationText = "An accessible toilet shall have a minimum clear floor area of 1,800 mm × 1,500 mm (2.7 m²), with adequate turning space for a wheelchair.",
            FailSeverity = Severity.Critical
        },

        // ── Minimum Ceiling Height ────────────────────────────────────────────

        new() {
            RuleId       = "SG-BCA-HEIGHT-001",
            RuleName     = "Habitable Room  -  Minimum Ceiling Height",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "Height",
            CheckUnit    = "m",
            MinimumValue = 2.4,
            FormulaDescription = "CeilingHeight(Room) ≥ 2.4 m",
            CodeReference = "Building Control Regulations 2003 (Rev 2021)  -  First Schedule §4",
            RegulationText = "Every habitable room in a residential building shall have a minimum clear height of 2.4 m above finished floor level.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-BCA-HEIGHT-002",
            RuleName     = "Commercial / Office Space  -  Minimum Ceiling Height",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "OFFICE",
            CheckParameter = "Height",
            CheckUnit    = "m",
            MinimumValue = 2.6,
            FormulaDescription = "CeilingHeight(Office) ≥ 2.6 m",
            CodeReference = "Building Control Regulations 2003 §5",
            RegulationText = "Office and commercial occupancies shall have a minimum floor-to-ceiling height of 2.6 m.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-HEIGHT-003",
            RuleName     = "Car Park  -  Minimum Headroom Clearance",
            Category     = DesignCodeCategory.ParkingAndVehicularAccess,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.LTA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CARPARK",
            CheckParameter = "Height",
            CheckUnit    = "m",
            MinimumValue = 2.1,
            FormulaDescription = "Headroom(Car Park) ≥ 2.1 m",
            CodeReference = "LTA Building Maintenance and Strata Management Act  -  Car Park Requirements §8",
            RegulationText = "Minimum headroom in car park areas shall be 2.1 m to the underside of any beam, duct or obstruction.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // 2. BCA CODE ON ACCESSIBILITY 2025
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-ACC-001",
            RuleName     = "Accessible Route Door  -  Minimum Clear Width",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCDOOR",
            PredefinedTypeFilter = "DOOR",
            CheckParameter = "ClearOpeningWidth",
            CheckUnit    = "mm",
            MinimumValue = 850,
            FormulaDescription = "ClearOpeningWidth(Door) ≥ 850 mm",
            CodeReference = "BCA Code on Accessibility 2025 §3.2.2",
            RegulationText = "Doors on accessible routes shall have a minimum clear opening width of 850 mm when fully open at 90°, measured between the door face and the opposite stop.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-BCA-ACC-002",
            RuleName     = "Accessible Route Door  -  Maximum Opening Force",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCDOOR",
            CheckParameter = "OpeningForce",
            CheckUnit    = "N",
            MaximumValue = 22.2,
            FormulaDescription = "OpeningForce(Door) ≤ 22.2 N",
            CodeReference = "BCA Code on Accessibility 2025 §3.2.4",
            RegulationText = "The opening force for doors on accessible routes shall not exceed 22.2 N (5 lbf).",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-ACC-003",
            RuleName     = "Accessible Corridor  -  Minimum Clear Width",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CORRIDOR",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 1200,
            FormulaDescription = "Width(Corridor) ≥ 1200 mm",
            CodeReference = "BCA Code on Accessibility 2025 §3.3.1",
            RegulationText = "An accessible corridor shall have a minimum clear width of 1200 mm throughout its length, free of obstructions and projections.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-BCA-ACC-004",
            RuleName     = "Ramp  -  Maximum Gradient",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCRAMP",
            CheckParameter = "SlopeRatio",
            CheckUnit    = "1:N",
            MinimumValue = 12.0,
            FormulaDescription = "SlopeRatio(Ramp) ≥ 1:12 (gradient ≤ 1/12 = 8.33%)",
            CodeReference = "BCA Code on Accessibility 2025 §3.5.1",
            RegulationText = "Ramps on accessible routes shall not have a running gradient steeper than 1:12 (8.33%). For every 750 mm rise a level landing of minimum 1500 mm depth shall be provided.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-BCA-ACC-005",
            RuleName     = "Lift Car  -  Minimum Internal Dimensions",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "LIFT",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 1.75,
            FormulaDescription = "Area(Lift Car) ≥ 1.75 m² (min 1300 mm × 1400 mm internal)",
            CodeReference = "BCA Code on Accessibility 2025 §3.7.1",
            RegulationText = "An accessible lift car shall have internal dimensions of not less than 1300 mm (width) × 1400 mm (depth), giving a minimum area of 1.82 m².",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-BCA-ACC-006",
            RuleName     = "Accessible Parking Lot  -  Minimum Dimensions",
            Category     = DesignCodeCategory.ParkingAndVehicularAccess,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "ACCESSIBLE_PARKING",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 3600,
            FormulaDescription = "Width(Accessible Parking Lot) ≥ 3600 mm",
            CodeReference = "BCA Code on Accessibility 2025 §6.1.1",
            RegulationText = "Accessible parking lots shall have a minimum width of 3600 mm and a minimum length of 7500 mm, including the adjacent 1500 mm transfer zone.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // 3. SCDF FIRE CODE 2018 (with 2023 Amendment)
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-SCDF-FIRE-001",
            RuleName     = "Travel Distance to Exit  -  Non-Sprinklered",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistanceToExit",
            CheckUnit    = "m",
            MaximumValue = 30.0,
            FormulaDescription = "TravelDistance(NonSprinklered) ≤ 30 m",
            CodeReference = "SCDF Fire Code 2018 (2023 Ed.) §9.2.2.1",
            RegulationText = "In a building not protected by an automatic sprinkler system, the maximum travel distance to the nearest exit shall not exceed 30 m.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-002",
            RuleName     = "Travel Distance to Exit  -  Sprinklered",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistanceToExit_Sprinklered",
            CheckUnit    = "m",
            MaximumValue = 60.0,
            FormulaDescription = "TravelDistance(Sprinklered) ≤ 60 m",
            CodeReference = "SCDF Fire Code 2018 §9.2.2.2",
            RegulationText = "Where the building is fully protected by an automatic sprinkler system, the maximum travel distance may be increased to 60 m.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-003",
            RuleName     = "Dead-End Corridor  -  Maximum Length",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "DEAD_END_CORRIDOR",
            CheckParameter = "Length",
            CheckUnit    = "m",
            MaximumValue = 15.0,
            FormulaDescription = "Length(Dead-End Corridor) ≤ 15 m",
            CodeReference = "SCDF Fire Code 2018 §9.2.3",
            RegulationText = "The length of a dead-end corridor measured from the nearest exit to the farthest point shall not exceed 15 m.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-004",
            RuleName     = "Exit Door  -  Minimum Clear Width",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCDOOR",
            SpaceCategoryFilter = "FIRE_EXIT",
            CheckParameter = "ClearOpeningWidth",
            CheckUnit    = "mm",
            MinimumValue = 1050,
            FormulaDescription = "ClearWidth(Exit Door) ≥ 1050 mm",
            CodeReference = "SCDF Fire Code 2018 §9.3.1",
            RegulationText = "Every exit door shall have a minimum clear opening width of 1050 mm.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-005",
            RuleName     = "Fire Escape Staircase  -  Minimum Width",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSTAIRFLIGHT",
            PredefinedTypeFilter = "STRAIGHT",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 1050,
            FormulaDescription = "Width(Fire Stair) ≥ 1050 mm",
            CodeReference = "SCDF Fire Code 2018 §9.4.1",
            RegulationText = "Every escape staircase shall have a minimum clear width of 1050 mm, measured clear of handrails and any other obstruction.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-006",
            RuleName     = "Fire Compartment  -  Maximum Area (Non-Sprinklered)",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "FIRE_COMPARTMENT",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MaximumValue = 2500,
            FormulaDescription = "Area(Fire Compartment, Non-Sprinklered) ≤ 2500 m²",
            CodeReference = "SCDF Fire Code 2018 §7.2.1",
            RegulationText = "In a non-sprinklered building, the floor area of each fire compartment shall not exceed 2500 m².",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-007",
            RuleName     = "Fire Compartment  -  Maximum Area (Sprinklered)",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "FIRE_COMPARTMENT_SPRINKLERED",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MaximumValue = 6000,
            FormulaDescription = "Area(Fire Compartment, Sprinklered) ≤ 6000 m²",
            CodeReference = "SCDF Fire Code 2018 §7.2.2",
            RegulationText = "In a fully sprinklered building, the maximum fire compartment area may be increased to 6000 m².",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-008",
            RuleName     = "Fire-Rated Wall  -  Minimum Fire Resistance Rating",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "FireRatingMinutes",
            CheckUnit    = "minutes",
            MinimumValue = 60,
            FormulaDescription = "FireRating(Party Wall / Compartment Wall) ≥ 60 minutes R/E/I",
            CodeReference = "SCDF Fire Code 2018 Table 7.1",
            RegulationText = "Party walls and fire compartment walls shall achieve a minimum fire resistance rating of 60/60/60 (R/E/I) for residential occupancies and 120/120/120 for commercial occupancies.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-009",
            RuleName     = "Fire-Rated Floor / Slab  -  Minimum Fire Resistance Rating",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSLAB",
            CheckParameter = "FireRatingMinutes",
            CheckUnit    = "minutes",
            MinimumValue = 60,
            FormulaDescription = "FireRating(Floor Slab separating fire compartments) ≥ 60 minutes R/E/I",
            CodeReference = "SCDF Fire Code 2018 Table 7.2",
            RegulationText = "Floor slabs forming part of a fire compartment boundary shall achieve a minimum fire resistance rating of 60/60/60 for the floors of residential occupancies.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // 4. BCA STRUCTURAL  -  Building Control Regulations
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-STR-001",
            RuleName     = "RC Slab  -  Minimum Thickness (Residential)",
            Category     = DesignCodeCategory.StructuralAndConstrucitonal,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSLAB",
            PredefinedTypeFilter = "FLOOR",
            CheckParameter = "Thickness",
            CheckUnit    = "mm",
            MinimumValue = 125,
            FormulaDescription = "Thickness(RC Slab, Residential) ≥ 125 mm",
            CodeReference = "BCA Building Control Act  -  BC 2: 2008 Structural Use of Concrete §5.3.1",
            RegulationText = "The minimum thickness of a reinforced concrete floor slab in a residential building shall be 125 mm. Slabs supporting concentrated loads or in commercial occupancies shall be a minimum of 150 mm.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-STR-002",
            RuleName     = "External Wall  -  Minimum Thickness (RC)",
            Category     = DesignCodeCategory.StructuralAndConstrucitonal,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "Thickness",
            CheckUnit    = "mm",
            MinimumValue = 150,
            FormulaDescription = "Thickness(RC External Wall) ≥ 150 mm",
            CodeReference = "BCA Building Control Act  -  BC 2: 2008 §6.2",
            RegulationText = "Load-bearing reinforced concrete walls shall have a minimum thickness of 150 mm. Non-load-bearing RC walls shall be minimum 100 mm.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // 5. NEA VENTILATION AND NATURAL LIGHTING
        // Environmental Public Health Act  -  Licensing of Premises
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-NEA-VENT-001",
            RuleName     = "Habitable Room  -  Minimum Natural Ventilation Opening",
            Category     = DesignCodeCategory.VentilationAndLighting,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "VentilationOpeningRatio",
            CheckUnit    = "% of floor area",
            MinimumValue = 5.0,
            FormulaDescription = "VentilationOpening ≥ 5% × FloorArea",
            CodeReference = "NEA Environmental Public Health (Licensing of Premises) Regulations  -  Third Schedule §2",
            RegulationText = "Every habitable room shall be provided with natural ventilation openings of not less than 5% of the floor area of that room.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-BCA-LIGHT-001",
            RuleName     = "Habitable Room  -  Minimum Natural Lighting Window Area",
            Category     = DesignCodeCategory.VentilationAndLighting,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "WindowAreaRatio",
            CheckUnit    = "% of floor area",
            MinimumValue = 10.0,
            FormulaDescription = "WindowArea ≥ 10% × FloorArea",
            CodeReference = "Building Control Regulations 2003  -  First Schedule §7",
            RegulationText = "Every habitable room shall have window openings with an aggregate area of not less than 10% of the floor area of that room to admit natural light.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // 6. URA GFA RULES
        // URA Circular on GFA  -  Planning Parameters Handbook
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-URA-GFA-001",
            RuleName     = "Balcony  -  Maximum Aggregate Area (10% of GFA)",
            Category     = DesignCodeCategory.GrossFloorAreaRules,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "BALCONY",
            CheckParameter = "BalconyRatioToGFA",
            CheckUnit    = "% of GFA",
            MaximumValue = 10.0,
            FormulaDescription = "TotalBalconyArea / TotalGFA ≤ 10%",
            CodeReference = "URA Circular  -  Guidelines on Balconies (Nov 2019)",
            RegulationText = "The total aggregate area of balconies in a residential development shall not exceed 10% of the total Gross Floor Area (GFA) of the development.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-URA-GFA-002",
            RuleName     = "Car Porch  -  Maximum Area",
            Category     = DesignCodeCategory.GrossFloorAreaRules,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CAR_PORCH",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MaximumValue = 50.0,
            FormulaDescription = "Area(Car Porch) ≤ 50 m²",
            CodeReference = "URA DC Handbook  -  Landed Housing §3.4.1",
            RegulationText = "The car porch area shall not exceed 50 m² and its roof shall not be used as a balcony or terrace.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // 7. BCA GREEN MARK 2021
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-GM-001",
            RuleName     = "Window Wall Ratio (WWR)  -  Maximum (North/South Facade)",
            Category     = DesignCodeCategory.SustainabilityAndGreenMark,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "WindowWallRatio_NS",
            CheckUnit    = "%",
            MaximumValue = 50.0,
            FormulaDescription = "WWR(N/S Facade) = WindowArea / WallArea × 100 ≤ 50%",
            CodeReference = "BCA Green Mark for Residences 2021  -  Energy Section §E2.1",
            RegulationText = "The Window Wall Ratio (WWR) for north and south facing facades shall not exceed 50% to limit solar heat gain. Formula: WWR = Σ(Window Area) ÷ Σ(Gross Wall Area) × 100%",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-GM-002",
            RuleName     = "Window Wall Ratio (WWR)  -  Maximum (East/West Facade)",
            Category     = DesignCodeCategory.SustainabilityAndGreenMark,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "WindowWallRatio_EW",
            CheckUnit    = "%",
            MaximumValue = 25.0,
            FormulaDescription = "WWR(E/W Facade) = WindowArea / WallArea × 100 ≤ 25%",
            CodeReference = "BCA Green Mark for Residences 2021 §E2.2",
            RegulationText = "The Window Wall Ratio for east and west facing facades (which receive direct morning and afternoon sun) shall not exceed 25% to control solar heat gain.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-GM-003",
            RuleName     = "External Wall  -  Maximum Thermal Transmittance (U-value)",
            Category     = DesignCodeCategory.SustainabilityAndGreenMark,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/(m²·K)",
            MaximumValue = 2.1,
            FormulaDescription = "U-value(External Wall) ≤ 2.1 W/(m²·K)",
            CodeReference = "BCA Code on Envelope Thermal Performance 2008  -  RETV/ETTV §3.2",
            RegulationText = "The thermal transmittance (U-value) of external walls shall not exceed 2.1 W/(m²·K) for non-residential buildings.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-GM-004",
            RuleName     = "Roof  -  Maximum Thermal Transmittance (RETV)",
            Category     = DesignCodeCategory.SustainabilityAndGreenMark,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSLAB",
            PredefinedTypeFilter = "ROOF",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/(m²·K)",
            MaximumValue = 0.5,
            FormulaDescription = "U-value(Roof Slab) ≤ 0.5 W/(m²·K)",
            CodeReference = "BCA Code on Envelope Thermal Performance 2008 §3.3",
            RegulationText = "The thermal transmittance (U-value) of the roof shall not exceed 0.5 W/(m²·K) for all building types in Singapore.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // 8. LTA PARKING STANDARDS
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-LTA-PARK-001",
            RuleName     = "Standard Parking Lot  -  Minimum Dimensions",
            Category     = DesignCodeCategory.ParkingAndVehicularAccess,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.LTA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "PARKING",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 2400,
            FormulaDescription = "Width(Standard Parking Lot) ≥ 2400 mm",
            CodeReference = "LTA Code of Practice for Vehicle Parking Provision §3.2.1",
            RegulationText = "A standard vehicle parking lot shall have minimum dimensions of 2400 mm (width) × 4800 mm (length) and a minimum headroom of 2100 mm.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // 9. PUB  -  DRAINAGE AND SANITARY
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-PUB-SAN-001",
            RuleName     = "Office  -  Minimum Sanitary Fittings (WC per Occupant)",
            Category     = DesignCodeCategory.PlumbingAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "OFFICE",
            CheckParameter = "SanitaryFittingRatio",
            CheckUnit    = "persons per WC",
            MaximumValue = 25.0,
            FormulaDescription = "OccupantLoad / NumberOfWCs ≤ 1 WC per 25 persons",
            CodeReference = "SS 636: 2018 Code of Practice for Water Services  -  Table 3.1",
            RegulationText = "For office occupancies, the minimum sanitary provision is 1 WC per 25 persons for female occupants and 1 WC per 30 persons (plus 1 urinal per 60 persons) for male occupants.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // 10. STAIRCASE GEOMETRY  -  BCA / SCDF
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-STAIR-001",
            RuleName     = "Stair Riser  -  Maximum Height",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSTAIRFLIGHT",
            CheckParameter = "RiserHeight",
            CheckUnit    = "mm",
            MaximumValue = 175,
            FormulaDescription = "RiserHeight ≤ 175 mm",
            CodeReference = "Building Control Regulations 2003  -  First Schedule §8.1",
            RegulationText = "The riser of a stair shall not exceed 175 mm in height. Formula: 2R + T = 550 mm to 700 mm (where R = riser, T = tread).",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-STAIR-002",
            RuleName     = "Stair Tread  -  Minimum Going",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSTAIRFLIGHT",
            CheckParameter = "TreadDepth",
            CheckUnit    = "mm",
            MinimumValue = 250,
            FormulaDescription = "TreadDepth(Going) ≥ 250 mm",
            CodeReference = "Building Control Regulations 2003  -  First Schedule §8.2",
            RegulationText = "The going (tread depth, exclusive of nosing) shall not be less than 250 mm.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // HIGH PRIORITY 1: GFA CATEGORIES - Full enumeration from COP 3.1
        // Source: COP 3.1 Dec 2025 Section 4 pp.362-381 + URA GFA Handbook 2024
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-URA-GFA-001",
            RuleName       = "GFACategory - Mandatory for all IfcSpace elements",
            Category       = DesignCodeCategory.GrossFloorArea,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "GFACategory",
            FormulaDescription = "GFACategory must be set to an approved URA value",
            CodeReference  = "COP 3.1 Dec 2025 §4 p.362 / URA GFA Handbook 2024",
            RegulationText = "Every IfcSpace element must carry a GFACategory value from the approved URA list. " +
                "URA uses this field to verify Gross Plot Ratio compliance. Missing or invalid GFACategory " +
                "causes automatic submission rejection at G1 and G2.",
            PermittedValues = new List<string> {
                // AGF_DevelopmentUse values (development land use classification)
                "Agriculture", "Beach Area", "Business Park", "Business 1", "Business 2",
                "Cemetery", "Civic & Community Institution", "Commercial", "Educational Institution",
                "Health & Medical Care", "Hotel", "Open Space", "Park", "Place of Worship",
                "Port/Airport", "Rapid Transit", "Reserve Site", "Residential (Landed)",
                "Residential (Non-landed)", "Road", "Special Use", "Sports & Recreation",
                "Transport Facilities", "Utility", "Waterbody",
                // Standard classification codes (from IFC+SG mapping)
                "RESIDENTIAL_PRIVATE", "RESIDENTIAL_HDB", "RESIDENTIAL_LANDED",
                "COMMERCIAL_RETAIL", "COMMERCIAL_OFFICE", "COMMERCIAL_HOTEL",
                "COMMERCIAL_FOOD_BEVERAGE", "COMMERCIAL_HEALTHCARE",
                "INDUSTRIAL_B1", "INDUSTRIAL_B2", "INDUSTRIAL_BP",
                "EDUCATIONAL", "HEALTHCARE_HOSPITAL", "HEALTHCARE_CLINIC",
                "CIVIC_COMMUNITY", "SPORTS_RECREATION", "PLACE_OF_WORSHIP",
                "TRANSPORT_RAIL", "TRANSPORT_BUS", "TRANSPORT_PARKING",
                "UTILITY_CARPARK", "UTILITY_MECH_ELEC", "UTILITY_SUBSTATION",
                "VOID", "ATRIUM", "ROOF_TERRACE", "BALCONY",
                "OPEN_AREA", "EXCLUDED_GFA", "GFA_EXEMPTED", "BONUS_GFA",
                "HOUSEHOLD_SHELTER", "STOREY_SHELTER", "PRECINCT_SHELTER",
                "AC_LEDGE", "RC_LEDGE", "BIN_CENTRE", "LOADING_BAY",
                "CAR_PARK_SURFACE", "CAR_PARK_BASEMENT", "CAR_PARK_MECHANISED",
                "CORRIDOR", "STAIRCASE", "LIFT_LOBBY", "PLANT_ROOM",
                "OTHERS"
            },
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-URA-GFA-002",
            RuleName       = "AGF_DevelopmentUse - Approved values for Space Area elements",
            Category       = DesignCodeCategory.GrossFloorArea,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "AGF_DevelopmentUse",
            FormulaDescription = "AGF_DevelopmentUse must match an approved URA development use category",
            CodeReference  = "COP 3.1 Dec 2025 §4 p.362",
            RegulationText = "The AGF_DevelopmentUse property identifies the master land use classification of the space.",
            PermittedValues = new List<string> {
                "Agriculture", "Beach Area", "Business Park", "Business 1", "Business 2",
                "Cemetery", "Civic & Community Institution", "Commercial", "Educational Institution",
                "Health & Medical Care", "Hotel", "Open Space", "Park", "Place of Worship",
                "Port/Airport", "Rapid Transit", "Reserve Site", "Residential (Landed)",
                "Residential (Non-landed)", "Road", "Special Use", "Sports & Recreation",
                "Transport Facilities", "Utility", "Waterbody"
            },
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-URA-GFA-003",
            RuleName       = "AGF_BuildingTypology - Approved values",
            Category       = DesignCodeCategory.GrossFloorArea,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "AGF_BuildingTypology",
            FormulaDescription = "AGF_BuildingTypology must be from the approved list",
            CodeReference  = "COP 3.1 Dec 2025 §4 p.362",
            RegulationText = "Building typology classification for strata area calculations.",
            PermittedValues = new List<string> {
                "Flats", "Condominium", "Shophouse", "Terrace House", "Detached House",
                "Semi-Detached House", "Good Class Bungalow", "Strata-Landed Housing",
                "Serviced Apartments", "Polyclinic", "Data Centre", "Community Club / Centre",
                "Adult Disability Home", "Medical Centre", "Public Acute Hospital",
                "Public Community Hospital", "Private Hospital", "Assisted Living Facility",
                "Confinement Centre", "Service Apartment II", "Adventure Centre / Campsite",
                "Farm", "Airport", "Port", "Light Industry", "Clean Industry",
                "General Industry", "Special Industry", "Electrical Substation",
                "Vehicular Parking Area"
            },
            FailSeverity   = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // HIGH PRIORITY 1: SPACE USAGE TYPES - All 95 types from COP 3.1 p.368-414
        // Source: COP 3.1 Dec 2025 Section 4 pp.368-414
        // These drive SCDF Purpose Group, PUB sanitary counts, NEA ventilation
        // BCA accessibility requirements, and NEA noise/pollution requirements
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-URA-USAGE-001",
            RuleName       = "SpaceUsage - Full approved list of 95 occupancy types",
            Category       = DesignCodeCategory.GrossFloorArea,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "SpaceUsage",
            FormulaDescription = "SpaceUsage must be a recognised COP 3.1 occupancy type",
            CodeReference  = "COP 3.1 Dec 2025 §4 pp.368-414",
            RegulationText = "SpaceUsage drives Purpose Group assignment (SCDF), sanitary fitting counts (PUB), " +
                "ventilation rates (NEA), and accessibility requirements (BCA). Every IfcSpace must carry a valid SpaceUsage.",
            PermittedValues = new List<string> {
                // Residential (1-5)
                "Single dwelling residential", "Multi-unit residential",
                "Supervisory care facility", "Supervisory care facility (detention)",
                "Nursing care facilities",
                // Healthcare (6-10)
                "Hospital with A&E services", "Hospital without A&E services",
                "Ambulatory care facility", "Ambulatory care facility (standalone)",
                "Custodian care facility",
                // Education (11-17)
                "Primary school", "Secondary school", "Custodian care facility (nursery)",
                "Tertiary Education Institution", "Public education institution",
                "Private education institution", "Tuition centre",
                // Workers / Office (18-21)
                "Worker dormitory", "Office", "Telephone exchange/central office", "Factory office",
                // Retail / Commercial (22-28)
                "Shop", "ODA", "Outpatient clinic", "Polyclinic", "Market",
                "Temporary showflat", "Factory showroom",
                // Industrial (29-40)
                "Petrol station", "Factory", "Food production factory", "M&E area",
                "Wafer fabrication plant", "Trade effluent treatment plant",
                "Waste management and recycling", "Embalming facility",
                "Agriculture", "Animal related facility", "High containment facility",
                "Electrical and gas facility",
                // Entertainment / Recreation (41-52)
                "Body treatment place", "Entertainment place", "Assembly place", "Cinema",
                "Recreational place", "Sky terrace", "F&B outlet", "Fast food outlet",
                "Outdoor Refreshment Area (ORA)", "Food centre", "Educational place",
                "Serviced apartment",
                // Hospitality (53-56)
                "Hostel", "Hotel", "Backpacker hotel", "Capsule hotel",
                // Community / Civic (57-59)
                "Community club", "Social club", "Religious place",
                // Open / Outdoor (60-63)
                "Park", "Sports facility", "Sports facility (ancillary)", "Residential amenities",
                // Transport (64-72)
                "Train interchange station", "Airport", "Ferry terminal", "Bus interchange",
                "Train station", "Bus terminal", "Rail depot", "Bus depot", "Parking",
                // Parking / Storage (73-76)
                "Fully Automated Mechanized Car Park Buildings (FAMCP)", "Warehouse",
                "Chemical/ hazmat storage", "Storage",
                // Military / Security (77-79)
                "Airbase", "Live firing area", "Training area",
                // Infrastructure (80-95)
                "Road Tunnel", "Campsite", "Wet play field", "Reservoir", "River",
                "Canal", "Major drain", "Pond", "Lake", "Other waterbody",
                "Nature reserve", "Nature area", "School field", "Promenade", "Marina", "Quarry",
                // PUB space categories
                "Living spaces", "Temporary residences",
                "Non-residential toilet spaces (for spaces with WC)",
                "Resting, care, hygiene spaces (for spaces without WC)",
                "Commercial, work, institutional spaces", "F&B spaces",
                "Medical, healthcare spaces", "Assembly Spaces",
                "Supporting spaces for performing", "Entertainment, recreation spaces",
                "Open spaces and open-sided spaces", "M&E spaces", "Storage spaces",
                "Commuter facilities", "Circulation spaces"
            },
            FailSeverity   = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY 1: SCDF FIRE CODE 2023 - Full FRR requirements
        // Source: COP 3.1 Dec 2025 Section 3 SCDF pp.73-81 + SCDF Fire Code 2023
        // ══════════════════════════════════════════════════════════════════════

        // Purpose Group definitions (drives FRR requirements)
        new() {
            RuleId         = "SG-SCDF-PG-001",
            RuleName       = "Purpose Group I - Residential (single or 2-storey)",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "Single dwelling residential",
            CheckParameter = "PurposeGroup",
            ExpectedValue  = "I",
            FormulaDescription = "Single dwelling residential = Purpose Group I",
            CodeReference  = "SCDF Fire Code 2023 Table 1.4A",
            RegulationText = "Single dwelling residential (landed houses, bungalows, semi-detached) is classified " +
                "as Purpose Group I. FRR: Walls 30 min, Floors 30 min, Columns/Beams 30 min.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-SCDF-FRR-001",
            RuleName       = "Compartment Wall FRR - Non-residential buildings ≤ 24m",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "FireRating",
            CheckUnit      = "hr",
            MinimumValue   = 1.0,
            FormulaDescription = "Compartment walls: FireRating ≥ 1.0 hr (60 min)",
            CodeReference  = "SCDF Fire Code 2023 §4.3 / Table 3.2",
            RegulationText = "Compartment walls in non-residential buildings up to 24m habitable height " +
                "require a minimum fire resistance of 60 minutes (1 hr). " +
                "Buildings exceeding 24m require FRR ≥ 120 min (2 hr) for compartment walls.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-FRR-002",
            RuleName       = "Compartment Floor FRR - Buildings > 24m habitable height",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCSLAB",
            CheckParameter = "FireRating",
            CheckUnit      = "hr",
            MinimumValue   = 2.0,
            FormulaDescription = "Compartment floors (buildings > 24m): FireRating ≥ 2.0 hr (120 min)",
            CodeReference  = "SCDF Fire Code 2023 §4.3 Table 3.2 / Compartmentation §4.4",
            RegulationText = "For buildings with habitable height exceeding 24m, compartment floors " +
                "must achieve FRR ≥ 120 minutes (2 hr). Maximum 1 storey per compartment.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-FRR-003",
            RuleName       = "Fire Door FRR - Doors in compartment walls",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCDOOR",
            CheckParameter = "FireRating",
            CheckUnit      = "hr",
            MinimumValue   = 0.5,
            FormulaDescription = "Fire doors in compartment walls: FireRating ≥ 0.5 hr (30 min)",
            CodeReference  = "SCDF Fire Code 2023 §4.3 / SS 332",
            RegulationText = "Doors in fire-rated compartment walls must be fire-rated. " +
                "Minimum 30 min (0.5 hr) for compartment doors. " +
                "Exit doors and staircase doors require minimum 60 min (1 hr) FRR.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-FRR-004",
            RuleName       = "Stair Enclosure FRR - Protected exit staircase",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "FireRating",
            CheckUnit      = "hr",
            MinimumValue   = 1.0,
            FormulaDescription = "Staircase enclosure walls: FireRating ≥ 1.0 hr (60 min)",
            CodeReference  = "SCDF Fire Code 2023 §5.4 / Table 5.2",
            RegulationText = "Protected exit staircases must be enclosed by fire-rated construction. " +
                "Minimum FRR 60 min (1 hr) for buildings ≤ 24m. " +
                "Minimum FRR 120 min (2 hr) for buildings > 24m.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-EXIT-001",
            RuleName       = "Exit Staircase - Minimum clear width",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "EffectiveWidth",
            CheckUnit      = "mm",
            MinimumValue   = 1100.0,
            FormulaDescription = "Exit staircase EffectiveWidth ≥ 1100mm (buildings ≤ 24m)",
            CodeReference  = "SCDF Fire Code 2023 §5.4.3",
            RegulationText = "Exit staircases must have a minimum clear width of 1100mm for buildings " +
                "with habitable height not exceeding 24m. " +
                "Buildings exceeding 24m require minimum 1200mm clear width.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-EXIT-002",
            RuleName       = "Exit Staircase - Minimum clear width for high-rise (> 24m)",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "EffectiveWidth",
            CheckUnit      = "mm",
            MinimumValue   = 1200.0,
            FormulaDescription = "Exit staircase EffectiveWidth ≥ 1200mm (buildings > 24m)",
            CodeReference  = "SCDF Fire Code 2023 §5.4.3",
            RegulationText = "For buildings with habitable height exceeding 24m, exit staircases " +
                "require a minimum clear width of 1200mm.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-COMP-001",
            RuleName       = "Compartmentation - Maximum compartment storeys (buildings ≤ 24m)",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "FireCompartmentStoreys",
            MaximumValue   = 3.0,
            FormulaDescription = "Max 3 storeys per compartment when habitable height ≤ 24m",
            CodeReference  = "SCDF Fire Code 2023 §4.4",
            RegulationText = "When the habitable height does not exceed 24m, a maximum of 3 storeys " +
                "may be included in a single fire compartment. " +
                "Exception: car parks are exempt from compartment size limitations.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-COMP-002",
            RuleName       = "Compartmentation - Maximum 1 storey per compartment (buildings > 24m)",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "FireCompartmentStoreys",
            MaximumValue   = 1.0,
            FormulaDescription = "Max 1 storey per compartment when habitable height > 24m",
            CodeReference  = "SCDF Fire Code 2023 §4.4",
            RegulationText = "When the habitable height exceeds 24m, each floor must form its own " +
                "fire compartment. Maximum 1 storey per compartment.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-LIFT-001",
            RuleName       = "Fireman's Lift - Required above 24m (60m coverage)",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCTRANSPORTELEMENT",
            CheckParameter = "FireFightersLift",
            ExpectedValue  = "TRUE",
            FormulaDescription = "FireFightersLift = TRUE required for lifts serving floors > 24m",
            CodeReference  = "SCDF Fire Code 2023 §6.2 / Cl 2.2.2",
            RegulationText = "All buildings with habitable height exceeding 24m must have at least one " +
                "fireman's lift serving all floors. The fireman's lift must be located within 60m " +
                "of all parts of any floor (except PG I and II residential buildings).",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-DAMP-001",
            RuleName       = "Fire Damper FRR - Penetrations through fire-rated construction",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCDAMPER",
            CheckParameter = "FireRating",
            CheckUnit      = "hr",
            MinimumValue   = 1.0,
            FormulaDescription = "Fire dampers: FireRating ≥ 1.0 hr (60 min)",
            CodeReference  = "SCDF Fire Code 2023 §4.3.4",
            RegulationText = "Fire dampers installed in ducts passing through fire-rated compartment walls " +
                "or floors must achieve the same FRR as the element being penetrated. " +
                "Minimum 60 min (1 hr) FRR.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SCDF-SPRK-001",
            RuleName       = "Fire Sprinkler - Required in buildings > 1000m² GFA",
            Category       = DesignCodeCategory.FireSafety,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "SprinklerProvided",
            ExpectedValue  = "TRUE",
            FormulaDescription = "SprinklerProvided = TRUE for buildings with total GFA > 1000m²",
            CodeReference  = "SCDF Fire Code 2023 §8.1",
            RegulationText = "Sprinkler systems are mandatory in: buildings with total GFA > 1000m², " +
                "all high-rise buildings, shopping complexes, hotels, and certain industrial buildings. " +
                "Sprinkler provision allows relaxation of travel distances and compartment sizes.",
            FailSeverity   = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY 2: CODE ON ACCESSIBILITY 2025 - Full dimensions
        // Source: COP 3.1 Dec 2025 references + Code on Accessibility 2025
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-BCA-ACC-001",
            RuleName       = "Accessible Route Width - Minimum 1500mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "ACCESSIBLEROUTE",
            CheckParameter = "Width",
            CheckUnit      = "mm",
            MinimumValue   = 1200.0,
            FormulaDescription = "Accessible route Width ≥ 1200mm",
            CodeReference  = "Code on Accessibility 2025 §3.2 / COP 3.1 p.251",
            RegulationText = "Accessible routes must have a minimum clear width of 1200mm. " +
                "Where a passing space is required (routes > 5m), width of 1800mm at passing points.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-BCA-ACC-002",
            RuleName       = "Accessible Door - Minimum clear width 850mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCDOOR",
            CheckParameter = "ClearWidth",
            CheckUnit      = "mm",
            MinimumValue   = 850.0,
            FormulaDescription = "Accessible door ClearWidth ≥ 850mm (preferred ≥ 900mm)",
            CodeReference  = "Code on Accessibility 2025 §3.3 / SS 553",
            RegulationText = "Doors on accessible routes must have a minimum clear opening width of 850mm. " +
                "The preferred minimum clear width is 900mm for better wheelchair access.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-BCA-ACC-003",
            RuleName       = "Accessible Ramp - Maximum gradient 1:12",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCRAMP",
            CheckParameter = "Gradient",
            MaximumValue   = 0.0833,
            FormulaDescription = "Ramp Gradient ≤ 0.0833 (1:12)",
            CodeReference  = "Code on Accessibility 2025 §4.3",
            RegulationText = "Accessible ramps must not exceed a gradient of 1:12 (8.33%). " +
                "The preferred maximum gradient is 1:20 (5%). " +
                "Ramps steeper than 1:10 are not permitted on accessible routes.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-BCA-ACC-004",
            RuleName       = "Accessible Ramp - Minimum width 1200mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCRAMP",
            CheckParameter = "Width",
            CheckUnit      = "mm",
            MinimumValue   = 1200.0,
            FormulaDescription = "Accessible ramp Width ≥ 1200mm",
            CodeReference  = "Code on Accessibility 2025 §4.3.2",
            RegulationText = "Accessible ramps must have a minimum clear width of 1200mm between handrails. " +
                "Handrails must be provided on both sides of the ramp.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-BCA-ACC-005",
            RuleName       = "Accessible Ramp - Level landing minimum 1500mm at top and bottom",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCRAMP",
            CheckParameter = "LandingLength",
            CheckUnit      = "mm",
            MinimumValue   = 1500.0,
            FormulaDescription = "Ramp landing LandingLength ≥ 1500mm at top and bottom",
            CodeReference  = "Code on Accessibility 2025 §4.3.3",
            RegulationText = "Level landings of minimum 1500mm in the direction of travel are required " +
                "at the top and bottom of every accessible ramp, and at intermediate landings where ramps " +
                "change direction.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-BCA-ACC-006",
            RuleName       = "Handrail Height - 850mm to 950mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCRAILING",
            CheckParameter = "Height",
            CheckUnit      = "mm",
            MinimumValue   = 850.0,
            MaximumValue   = 950.0,
            FormulaDescription = "Handrail Height: 850mm ≤ Height ≤ 950mm",
            CodeReference  = "Code on Accessibility 2025 §3.5",
            RegulationText = "Handrails on accessible routes must be at a height of 850mm to 950mm " +
                "above the ramp or stair surface. " +
                "A second (lower) handrail at 600mm to 750mm is required on accessible routes.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-BCA-ACC-007",
            RuleName       = "Accessible Lift - Minimum car size 1100mm × 1400mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCTRANSPORTELEMENT",
            CheckParameter = "CarSize",
            CheckUnit      = "mm",
            MinimumValue   = 1100.0,
            FormulaDescription = "Accessible lift car minimum 1100mm (width) × 1400mm (depth)",
            CodeReference  = "Code on Accessibility 2025 §6.1",
            RegulationText = "Lifts on accessible routes must have a minimum car size of 1100mm wide × 1400mm deep " +
                "(measured from door to back wall). Door clear opening width: minimum 900mm. " +
                "Audible and visual floor indicators required.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-BCA-ACC-008",
            RuleName       = "Accessible Toilet - Manoeuvring space 1800mm × 1800mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "ACCESSIBLE_TOILET",
            CheckParameter = "GrossPlannedArea",
            CheckUnit      = "m²",
            MinimumValue   = 3.24,
            FormulaDescription = "Accessible toilet: minimum 1800mm × 1800mm clear floor area (3.24 m²)",
            CodeReference  = "Code on Accessibility 2025 §4.2.2",
            RegulationText = "An accessible toilet must provide a minimum clear floor area of 1800mm × 1800mm " +
                "to allow a wheelchair turning circle of 1500mm diameter. " +
                "Grab bars must be provided at WC: one side wall and one rear wall.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-BCA-ACC-009",
            RuleName       = "Stair Riser Height - Maximum 175mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "RiserHeight",
            CheckUnit      = "mm",
            MaximumValue   = 175.0,
            FormulaDescription = "Stair RiserHeight ≤ 175mm",
            CodeReference  = "Code on Accessibility 2025 §5.1 / Building Control Regulations",
            RegulationText = "The maximum riser height for any staircase is 175mm. " +
                "All risers within a flight must be uniform.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-BCA-ACC-010",
            RuleName       = "Stair Tread Length - Minimum 280mm",
            Category       = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "TreadLength",
            CheckUnit      = "mm",
            MinimumValue   = 280.0,
            FormulaDescription = "Stair TreadLength ≥ 280mm",
            CodeReference  = "Code on Accessibility 2025 §5.1",
            RegulationText = "The minimum tread going is 280mm. For accessible stairs, preferred minimum is 300mm. " +
                "Nosings must be clearly distinguishable from the tread by contrast or tactile marking.",
            FailSeverity   = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY 3: BCA GREEN MARK 2021 - Full thermal and energy
        // Source: BCA Green Mark 2021 Criteria + COP 3.1 references
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-BCA-GM-001",
            RuleName       = "External Wall U-Value - Maximum 0.50 W/m²K (Green Mark)",
            Category       = DesignCodeCategory.EnergyPerformance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "ThermalTransmittance",
            CheckUnit      = "W/m²K",
            MaximumValue   = 0.50,
            FormulaDescription = "External wall ThermalTransmittance ≤ 0.50 W/m²K",
            CodeReference  = "BCA Green Mark 2021 §3.4 / SS 552",
            RegulationText = "External walls must achieve a maximum U-value of 0.50 W/m²K for Green Mark compliance. " +
                "This applies to all non-transparent external wall assemblies. " +
                "The U-value contributes to the RETV calculation.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-BCA-GM-002",
            RuleName       = "Roof U-Value - Maximum 0.40 W/m²K (Green Mark)",
            Category       = DesignCodeCategory.EnergyPerformance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCSLAB",
            SpaceCategoryFilter = "ROOF",
            CheckParameter = "ThermalTransmittance",
            CheckUnit      = "W/m²K",
            MaximumValue   = 0.40,
            FormulaDescription = "Roof ThermalTransmittance ≤ 0.40 W/m²K",
            CodeReference  = "BCA Green Mark 2021 §3.4.2 / SS 552",
            RegulationText = "Roof assemblies (including roof slabs) must achieve a maximum U-value of " +
                "0.40 W/m²K. For Green Roof systems the U-value may be adjusted to account for " +
                "the additional thermal mass and evapotranspiration.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-BCA-GM-003",
            RuleName       = "Window SHGC - Maximum 0.25 (Green Mark)",
            Category       = DesignCodeCategory.EnergyPerformance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCWINDOW",
            CheckParameter = "SolarHeatGainCoefficient",
            MaximumValue   = 0.25,
            FormulaDescription = "Window SolarHeatGainCoefficient ≤ 0.25",
            CodeReference  = "BCA Green Mark 2021 §3.4.3",
            RegulationText = "External windows must achieve a maximum Solar Heat Gain Coefficient (SHGC) of 0.25 " +
                "for Green Mark compliance. SHGC is used in the ETTV calculation.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-BCA-GM-004",
            RuleName       = "Window-to-Wall Ratio - Maximum 40% per facade",
            Category       = DesignCodeCategory.EnergyPerformance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "PercentageGlazedArea",
            CheckUnit      = "%",
            MaximumValue   = 40.0,
            FormulaDescription = "Window to Wall Ratio ≤ 40%",
            CodeReference  = "BCA Green Mark 2021 §3.4.4 / ETTV formula",
            RegulationText = "The Window-to-Wall Ratio (WWR) per facade should not exceed 40% for optimal " +
                "energy performance. Higher WWR requires compensatory measures such as external shading or " +
                "high-performance glazing to meet the ETTV target.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-BCA-GM-005",
            RuleName       = "ETTV Target - Maximum 50 W/m² for non-residential",
            Category       = DesignCodeCategory.EnergyPerformance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCBUILDING",
            CheckParameter = "EnvelopeThermalTransferValue",
            CheckUnit      = "W/m²",
            MaximumValue   = 50.0,
            FormulaDescription = "ETTV ≤ 50 W/m² (non-residential buildings)",
            CodeReference  = "BCA Green Mark 2021 §3.4 / SS 530",
            RegulationText = "Non-residential buildings must achieve an Envelope Thermal Transfer Value (ETTV) " +
                "of not more than 50 W/m². The ETTV formula incorporates U-values, SHGC, " +
                "and window areas per facade orientation.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-BCA-GM-006",
            RuleName       = "RETV Target - Maximum 25 W/m² for residential",
            Category       = DesignCodeCategory.EnergyPerformance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCBUILDING",
            CheckParameter = "ResidentialEnvelopeThermalTransferValue",
            CheckUnit      = "W/m²",
            MaximumValue   = 25.0,
            FormulaDescription = "RETV ≤ 25 W/m² (residential buildings)",
            CodeReference  = "BCA Green Mark 2021 §3.4 / SS 553 residential RETV",
            RegulationText = "Residential buildings must achieve a Residential Envelope Thermal Transfer Value " +
                "(RETV) of not more than 25 W/m².",
            FailSeverity   = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY 1: STRUCTURAL GRADES - SS EN 1992/1993
        // Source: BC 2:2021 / SS EN 1992-1-1 / SS EN 1993-1-1
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-BCA-STR-001",
            RuleName       = "Concrete Grade - Minimum C25/30 for structural use",
            Category       = DesignCodeCategory.StructuralAdequacy,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCCOLUMN",
            CheckParameter = "MaterialGrade",
            FormulaDescription = "Column MaterialGrade must be ≥ C25/30",
            CodeReference  = "BC 2:2021 §3.1 / SS EN 1992-1-1 Table 3.1",
            RegulationText = "Structural concrete must achieve a minimum characteristic cylinder strength of " +
                "C25/30 (25 MPa cylinder / 30 MPa cube). Approved concrete grades: " +
                "C25/30, C30/37, C32/40, C35/45, C40/50, C45/55, C50/60. " +
                "Piling concrete minimum: C35/45. High-strength concrete (> C50/60): requires BCA approval.",
            PermittedValues = new List<string> {
                "C12/15", "C16/20", "C20/25", "C25/30", "C28/35",
                "C30/37", "C32/40", "C35/45", "C40/50", "C45/55",
                "C50/60", "C55/67", "C60/75", "C70/85", "C80/95",
                "C90/105", "C100/115"
            },
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-BCA-STR-002",
            RuleName       = "Reinforcement Grade - Approved values",
            Category       = DesignCodeCategory.StructuralAdequacy,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCBEAM",
            CheckParameter = "ReinforcementSteelGrade",
            FormulaDescription = "ReinforcementSteelGrade must be from approved list",
            CodeReference  = "BC 2:2021 §3.2 / SS 560",
            RegulationText = "Reinforcement steel must comply with SS 560. Approved grades: " +
                "500A (ductility class A), 500B (ductility class B), 500C (ductility class C), " +
                "600A, 600B, 600C. Grade 500B is the standard for most structural applications.",
            PermittedValues = new List<string> { "500A", "500B", "500C", "600A", "600B", "600C" },
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-BCA-STR-003",
            RuleName       = "Steel Structural Grade - Approved values",
            Category       = DesignCodeCategory.StructuralAdequacy,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCBEAM",
            CheckParameter = "SteelGrade",
            FormulaDescription = "Steel SteelGrade must be S275, S355 or S460",
            CodeReference  = "BC 2:2021 §3.3 / SS EN 1993-1-1 Table 3.1",
            RegulationText = "Structural steel must comply with SS EN 1993. Approved grades: " +
                "S275 (275 MPa yield), S355 (355 MPa yield), S420, S460. " +
                "S355 is the most commonly used grade for Singapore structural steelwork.",
            PermittedValues = new List<string> { "S235", "S275", "S355", "S420", "S460", "S690" },
            FailSeverity   = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY 2: PUB WELS / SANITARY FITTING RATIOS
        // Source: PUB Sewerage and Sanitary Works Code + SS 608-2:2020
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-PUB-WELS-001",
            RuleName       = "WC WELS Rating - Minimum 3 ticks (≤ 4.5L flush)",
            Category       = DesignCodeCategory.PlumbingAndDrainage,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            SpaceCategoryFilter = "WATERCLOSET",
            CheckParameter = "WELSRating",
            MinimumValue   = 3.0,
            FormulaDescription = "WC WELSRating ≥ 3 (max flush 4.5L/3L dual flush)",
            CodeReference  = "PUB Water Efficiency Requirements 2021 / SS 608-2:2020",
            RegulationText = "All water closets must achieve a minimum WELS rating of 3 ticks. " +
                "3-tick WC: maximum flush volume 4.5L (full flush) and 3.0L (half flush) for dual-flush. " +
                "Single-flush WC: maximum 4.5L per flush. " +
                "This is mandatory for all new developments and major additions/alterations.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-PUB-WELS-002",
            RuleName       = "Wash Basin WELS Rating - Minimum 2 ticks (≤ 6L/min)",
            Category       = DesignCodeCategory.PlumbingAndDrainage,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            SpaceCategoryFilter = "WASHHANDBASIN",
            CheckParameter = "WELSRating",
            MinimumValue   = 2.0,
            FormulaDescription = "Wash basin WELSRating ≥ 2 (max 6L/min flow)",
            CodeReference  = "PUB Water Efficiency Requirements 2021 / SS 608-2:2020",
            RegulationText = "Wash basins (including lavatory basins) must achieve a minimum WELS rating of " +
                "2 ticks. 2-tick basin tap: maximum flow rate of 6 litres per minute at 3 bar pressure.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-PUB-WELS-003",
            RuleName       = "Shower WELS Rating - Minimum 2 ticks (≤ 9L/min)",
            Category       = DesignCodeCategory.PlumbingAndDrainage,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            SpaceCategoryFilter = "SHOWER",
            CheckParameter = "WELSRating",
            MinimumValue   = 2.0,
            FormulaDescription = "Shower WELSRating ≥ 2 (max 9L/min flow)",
            CodeReference  = "PUB Water Efficiency Requirements 2021 / SS 608-2:2020",
            RegulationText = "Shower fittings must achieve a minimum WELS rating of 2 ticks. " +
                "2-tick shower: maximum flow rate of 9 litres per minute at 3 bar pressure.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-PUB-WELS-004",
            RuleName       = "Urinal WELS Rating - Minimum 2 ticks (≤ 1.5L/flush)",
            Category       = DesignCodeCategory.PlumbingAndDrainage,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            SpaceCategoryFilter = "URINAL",
            CheckParameter = "WELSRating",
            MinimumValue   = 2.0,
            FormulaDescription = "Urinal WELSRating ≥ 2 (max 1.5L/flush)",
            CodeReference  = "PUB Water Efficiency Requirements 2021 / SS 608-2:2020",
            RegulationText = "Urinals must achieve a minimum WELS rating of 2 ticks. " +
                "2-tick urinal: maximum 1.5 litres per flush. " +
                "Waterless urinals are permitted and receive the highest WELS rating.",
            FailSeverity   = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY 3: LTA PARKING PROVISION RATES
        // Source: LTA Code of Practice for Vehicle Parking Provision 2019
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-LTA-PKG-001",
            RuleName       = "Standard Car Bay Dimensions - 2400mm × 4800mm minimum",
            Category       = DesignCodeCategory.ParkingAndTransport,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            CheckParameter = "BayWidth",
            CheckUnit      = "mm",
            MinimumValue   = 2400.0,
            FormulaDescription = "Car parking bay BayWidth ≥ 2400mm",
            CodeReference  = "LTA Code of Practice for Vehicle Parking Provision 2019 §2.3",
            RegulationText = "Standard car parking bays must have minimum dimensions of " +
                "2400mm width × 4800mm length. End bays (adjacent to wall/column) minimum 2600mm width.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-LTA-PKG-002",
            RuleName       = "Standard Car Bay Length - 4800mm minimum",
            Category       = DesignCodeCategory.ParkingAndTransport,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            CheckParameter = "BayLength",
            CheckUnit      = "mm",
            MinimumValue   = 4800.0,
            FormulaDescription = "Car parking bay BayLength ≥ 4800mm",
            CodeReference  = "LTA Code of Practice for Vehicle Parking Provision 2019 §2.3",
            RegulationText = "Standard car parking bays require a minimum length of 4800mm. " +
                "Parallel bays: minimum 6500mm length.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-LTA-PKG-003",
            RuleName       = "PWD Car Bay - Minimum 3600mm width",
            Category       = DesignCodeCategory.ParkingAndTransport,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            SpaceCategoryFilter = "CARPWDPARKINGLOT",
            CheckParameter = "BayWidth",
            CheckUnit      = "mm",
            MinimumValue   = 3600.0,
            FormulaDescription = "PWD bay BayWidth ≥ 3600mm",
            CodeReference  = "LTA Code of Practice for Vehicle Parking Provision 2019 §2.4 / Code on Accessibility 2025",
            RegulationText = "Parking bays designated for persons with disabilities (PWD) must have a minimum " +
                "width of 3600mm to allow wheelchair transfer. " +
                "PWD bays must be located closest to the building entrance or lift lobby.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-LTA-PKG-004",
            RuleName       = "Car Park Headroom - Minimum 2100mm clear height",
            Category       = DesignCodeCategory.ParkingAndTransport,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            CheckParameter = "HeadRoom",
            CheckUnit      = "mm",
            MinimumValue   = 2100.0,
            FormulaDescription = "Car park HeadRoom ≥ 2100mm",
            CodeReference  = "LTA Code of Practice for Vehicle Parking Provision 2019 §2.5",
            RegulationText = "The minimum clear headroom throughout a car park (including at ramps and in bays) " +
                "is 2100mm. The entry/exit point must provide at least 2200mm clear height. " +
                "Bays designated for PWD and vans require minimum 2600mm headroom.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-LTA-PKG-005",
            RuleName       = "Lorry Bay Dimensions - 3000mm × 12000mm minimum",
            Category       = DesignCodeCategory.ParkingAndTransport,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            SpaceCategoryFilter = "LORRYLOT",
            CheckParameter = "BayWidth",
            CheckUnit      = "mm",
            MinimumValue   = 3000.0,
            FormulaDescription = "Lorry bay BayWidth ≥ 3000mm",
            CodeReference  = "LTA Code of Practice for Vehicle Parking Provision 2019 §3.2",
            RegulationText = "Lorry loading/unloading bays must have minimum dimensions of " +
                "3000mm width × 12000mm length for standard 10-tonne lorries. " +
                "Articulated vehicle (trailer) bays: minimum 3500mm × 18000mm.",
            FailSeverity   = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // G4 COMPLETION GATEWAY REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 pp.169-175
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-G4-BCA-001",
            RuleName       = "G4 TOP - BCA Completion of Structural Works declaration",
            Category       = DesignCodeCategory.GatewayCompliance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCBUILDING",
            CheckParameter = "StructuralWorksComplete",
            ExpectedValue  = "TRUE",
            FormulaDescription = "At G4 (TOP): StructuralWorksComplete = TRUE",
            CodeReference  = "COP 3.1 Dec 2025 §3 p.169 / Building Control Act (Cap 29)",
            RegulationText = "At the Temporary Occupation Permit (TOP) stage, the QP (Structural) must declare " +
                "that all structural works have been completed in accordance with approved plans. " +
                "Household/Storey Shelter commissioning documentation required.",
            IsRequired          = true,
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-G4-SCDF-001",
            RuleName       = "G4 TOP - SCDF Temporary Fire Permit application",
            Category       = DesignCodeCategory.GatewayCompliance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SCDF,
            IfcClassFilter = "IFCBUILDING",
            CheckParameter = "TemporaryFirePermit",
            ExpectedValue  = "APPLIED",
            FormulaDescription = "At G4 (TOP): SCDF Temporary Fire Permit must be applied",
            CodeReference  = "COP 3.1 Dec 2025 §3 p.170 / Fire Safety Act",
            RegulationText = "Before Temporary Occupation Permit (TOP) can be issued, a Temporary Fire Permit " +
                "(TFP) must be obtained from SCDF. At CSC stage, a Fire Safety Certificate (FSC) is required.",
            IsRequired          = true,
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-G4-PUB-001",
            RuleName       = "G4 TOP - PUB Compliance Certificate for sanitary/drainage works",
            Category       = DesignCodeCategory.GatewayCompliance,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.PUB,
            IfcClassFilter = "IFCBUILDING",
            CheckParameter = "SanitaryDrainageCompliance",
            ExpectedValue  = "DECLARED",
            FormulaDescription = "At G4 (TOP): QP declaration of supervised drainage/sanitary works",
            CodeReference  = "COP 3.1 Dec 2025 §3 p.170 / Sewerage and Drainage Act",
            RegulationText = "The QP (Civil/Structural) must declare that all sanitary and drainage works have " +
                "been supervised and built according to approved plans. " +
                "Application for Compliance Certificate for Sanitary/Sewerage and TOP clearance required.",
            IsRequired          = true,
            FailSeverity   = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // JTC INDUSTRIAL REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 BCA p.44 / JTC development guidelines
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-JTC-IND-001",
            RuleName       = "JTC Industrial - Floor Loading Capacity declaration",
            Category       = DesignCodeCategory.StructuralAdequacy,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCSLAB",
            CheckParameter = "DesignFloorLoad",
            CheckUnit      = "kN/m²",
            MinimumValue   = 10.0,
            FormulaDescription = "Industrial floor slab DesignFloorLoad ≥ 10 kN/m² for JTC premises",
            CodeReference  = "JTC Industrial Development Guidelines 2024 / BC 2:2021",
            RegulationText = "JTC industrial premises (B1/B2 industrial zoning) require floor loading capacity " +
                "of minimum 10 kN/m² for general industrial use. " +
                "Heavy industrial (B2) may require 20-30 kN/m² or higher. " +
                "Loading capacity must be stated on structural drawings and IFC model.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-JTC-IND-002",
            RuleName       = "JTC Industrial - Floor-to-floor height minimum 6000mm",
            Category       = DesignCodeCategory.RoomSizesAndDimensions,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "Factory",
            CheckParameter = "Height",
            CheckUnit      = "mm",
            MinimumValue   = 6000.0,
            FormulaDescription = "Industrial factory space Height ≥ 6000mm",
            CodeReference  = "JTC Industrial Development Guidelines 2024",
            RegulationText = "JTC factory and warehouse spaces require a minimum floor-to-floor height of 6000mm " +
                "to accommodate industrial processes, mezzanine floors, and material handling equipment. " +
                "High-bay warehouses may require 10m or more.",
            FailSeverity   = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // NEA - Additional ventilation and environmental requirements
        // Source: COP 3.1 Dec 2025 Section 3 NEA pp.52-63
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-NEA-VENT-001",
            RuleName       = "Car Park - Mechanical ventilation minimum 6 ACH",
            Category       = DesignCodeCategory.VentilationAndAirQuality,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "Parking",
            CheckParameter = "AirChangeRate",
            CheckUnit      = "ACH",
            MinimumValue   = 6.0,
            FormulaDescription = "Car park AirChangeRate ≥ 6 ACH",
            CodeReference  = "NEA EPH Regulations / COP 3.1 Dec 2025 §3 NEA p.52 / SS 553",
            RegulationText = "Enclosed car parks must have mechanical ventilation providing a minimum of " +
                "6 air changes per hour (ACH). Carbon monoxide detection with automatic ventilation " +
                "activation is required at CO levels of 50ppm.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-NEA-VENT-002",
            RuleName       = "Kitchen Exhaust - Commercial kitchen minimum 20 ACH",
            Category       = DesignCodeCategory.VentilationAndAirQuality,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "F&B outlet",
            CheckParameter = "AirChangeRate",
            CheckUnit      = "ACH",
            MinimumValue   = 20.0,
            FormulaDescription = "Commercial kitchen AirChangeRate ≥ 20 ACH",
            CodeReference  = "NEA Code of Practice for Industrial Exhaust / COP 3.1 §3 NEA p.52",
            RegulationText = "Commercial kitchens require a minimum of 20 air changes per hour. " +
                "Grease interception is required. Exhaust air must be discharged vertically at a " +
                "minimum height of 1m above the highest point of the roof.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-NEA-VENT-003",
            RuleName       = "Office Space - Natural/mechanical ventilation minimum 6 ACH",
            Category       = DesignCodeCategory.VentilationAndAirQuality,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "Office",
            CheckParameter = "AirChangeRate",
            CheckUnit      = "ACH",
            MinimumValue   = 6.0,
            FormulaDescription = "Office AirChangeRate ≥ 6 ACH",
            CodeReference  = "NEA EPH Regulations §3 / SS 553:2016 Table 1",
            RegulationText = "Office spaces must provide a minimum of 6 air changes per hour, or minimum " +
                "10 litres per second per person fresh air supply. " +
                "Air-conditioned offices must have CO2 monitoring where occupancy > 50 persons.",
            FailSeverity   = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // NParks LUSH 3.0 REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 NParks pp.64-67 / NParks LUSH 3.0
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-NPARKS-LUSH-001",
            RuleName       = "NParks - Plant species must use approved botanical name",
            Category       = DesignCodeCategory.LandscapeAndGreenery,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            CheckParameter = "PlantSpecies",
            FormulaDescription = "PlantSpecies must be a full Latin botanical name",
            CodeReference  = "COP 3.1 Dec 2025 §4 p.309 / NParks LUSH 3.0 Programme / NParks Flora & Fauna Web",
            RegulationText = "All trees, palms, and shrubs must be identified by their full Latin botanical name " +
                "(genus and species, e.g. Terminalia mantaly, Heliconia psittacorum). " +
                "Common names alone are not accepted. The species must appear on the NParks approved species list.",
            FailSeverity   = Severity.Error
        },

        new() {
            RuleId         = "SG-NPARKS-LUSH-002",
            RuleName       = "NParks - Tree girth size for mature trees",
            Category       = DesignCodeCategory.LandscapeAndGreenery,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            SpaceCategoryFilter = "LANDSCAPE_TREE",
            CheckParameter = "GirthSize",
            CheckUnit      = "mm",
            MinimumValue   = 300.0,
            FormulaDescription = "Planted trees: GirthSize ≥ 300mm (measured at 1m height)",
            CodeReference  = "NParks LUSH 3.0 Programme / NParks Planting Requirements 2024",
            RegulationText = "Trees specified for LUSH 3.0 compliance must have a minimum girth of 300mm " +
                "measured at 1 metre above ground level at time of planting. " +
                "Trees in the Heritage Road Tree Programme must not be removed without NParks approval.",
            FailSeverity   = Severity.Warning
        },

        new() {
            RuleId         = "SG-NPARKS-LUSH-003",
            RuleName       = "NParks - Soil depth for planted areas minimum 600mm",
            Category       = DesignCodeCategory.LandscapeAndGreenery,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            SpaceCategoryFilter = "PLANTINGAREAS",
            CheckParameter = "SoilDepth",
            CheckUnit      = "mm",
            MinimumValue   = 600.0,
            FormulaDescription = "Planting area SoilDepth ≥ 600mm for shrubs and trees",
            CodeReference  = "NParks LUSH 3.0 Soil Specification 2024 / NParks ASM",
            RegulationText = "Planting areas for shrubs and trees require a minimum soil depth of 600mm " +
                "using the NParks Approved Soil Mixture (ASM). " +
                "Turf areas require minimum 300mm soil depth. " +
                "A drainage layer must be provided beneath all planted areas on structures.",
            FailSeverity   = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // SLA GEOREFERENCING
        // Source: COP 3.1 Dec 2025 / SLA SVY21 requirements
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId         = "SG-SLA-GEO-001",
            RuleName       = "SVY21 Georeferencing - Easting must be within Singapore bounds",
            Category       = DesignCodeCategory.GeoreferencingAndSpatial,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SLA,
            IfcClassFilter = "IFCSITE",
            CheckParameter = "SVY21_Easting",
            CheckUnit      = "m",
            MinimumValue   = 2000.0,
            MaximumValue   = 48000.0,
            FormulaDescription = "SVY21 Easting: 2,000m ≤ E ≤ 48,000m (Singapore bounds)",
            CodeReference  = "SLA SVY21 Datum / IFC+SG COP 3.1 §L13",
            RegulationText = "The IfcSite map conversion must use SVY21 coordinates. " +
                "Valid Singapore SVY21 Easting range: 2,000m to 48,000m. " +
                "Zero easting (E=0, N=0) indicates a missing or invalid georeference. " +
                "SLA will reject submissions with zero or out-of-bounds coordinates.",
            FailSeverity   = Severity.Critical
        },

        new() {
            RuleId         = "SG-SLA-GEO-002",
            RuleName       = "SVY21 Georeferencing - Northing must be within Singapore bounds",
            Category       = DesignCodeCategory.GeoreferencingAndSpatial,
            Country        = CountryMode.Singapore,
            Agency         = SgAgency.SLA,
            IfcClassFilter = "IFCSITE",
            CheckParameter = "SVY21_Northing",
            CheckUnit      = "m",
            MinimumValue   = 14000.0,
            MaximumValue   = 50000.0,
            FormulaDescription = "SVY21 Northing: 14,000m ≤ N ≤ 50,000m (Singapore bounds)",
            CodeReference  = "SLA SVY21 Datum / IFC+SG COP 3.1 §L13",
            RegulationText = "The IfcSite map conversion SVY21 Northing must be within the Singapore bounds. " +
                "Valid Singapore SVY21 Northing range: 14,000m to 50,000m. " +
                "The coordinate origin for SVY21 is at latitude 1°22'02.9154\"N, longitude 103°49'31.9752\"E.",
            FailSeverity   = Severity.Critical
        },



        // ══════════════════════════════════════════════════════════════════════
        // HIGH PRIORITY: GFA CATEGORY FULL ENUMERATION (URA)
        // Source: COP 3.1 Dec 2025 Section 4 pp.362-395
        // AGF_DevelopmentUse values and AGF_Name values per development use
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-URA-GFA-001",
            RuleName     = "GFA Category - AGF_DevelopmentUse must be from approved list",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "AREA_GFA",
            CheckParameter = "AGF_DevelopmentUse",
            PermittedValues = new List<string>
            {
                "Agriculture","Beach Area","Business Park","Business 1","Business 2",
                "Cemetery","Civic & Community Institution","Commercial","Educational Institution",
                "Health & Medical Care","Hotel","Open Space","Park","Place of Worship",
                "Port/Airport","Rapid Transit","Reserve Site","Residential (Landed)",
                "Residential (Non-landed)","Road","Special Use","Sports & Recreation",
                "Transport Facilities","Utility","Waterbody"
            },
            FormulaDescription = "AGF_DevelopmentUse must match URA Master Plan approved land use categories",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.362 - AGF_DevelopmentUse",
            RegulationText = "The development use must match one of the URA Master Plan approved categories exactly as listed in COP 3.1 Table p.362.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-URA-GFA-002",
            RuleName     = "GFA Category - AGF_UseQuantum must be Predominant or Ancillary",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "AREA_GFA",
            CheckParameter = "AGF_UseQuantum",
            PermittedValues = new List<string> { "Predominant", "Ancillary" },
            FormulaDescription = "AGF_UseQuantum must be 'Predominant' or 'Ancillary'",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.362",
            RegulationText = "Each space must be classified as either Predominant use or Ancillary use for GFA computation.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-URA-GFA-003",
            RuleName     = "GFA Category - AGF_BonusGFAType must be from approved incentive schemes",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "AREA_GFA",
            CheckParameter = "AGF_BonusGFAType",
            PermittedValues = new List<string>
            {
                "Balcony Incentive Scheme","Conserved Bungalows Scheme","Indoor Recreation Spaces Scheme",
                "Built Environment Transformation Scheme","Community and Sports Facilities Scheme",
                "Rooftop ORA on Landscaped Roofs","ORA within Privately-Owned Public Spaces (POPS)",
                "CBD Incentive Scheme","Strategic Development Incentive (SDI) Scheme",
                "Facade Articulation Scheme"
            },
            FormulaDescription = "AGF_BonusGFAType must match an approved URA bonus GFA incentive scheme name",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.362",
            RegulationText = "Bonus GFA must be applied under one of the approved URA incentive schemes.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-URA-GFA-004",
            RuleName     = "GFA Area Schemes - Strata type must be from approved list",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "AREA_GFA",
            CheckParameter = "AST_AreaType",
            PermittedValues = new List<string> { "Strata (Private)","Strata (Communal)","Common Area" },
            FormulaDescription = "AST_AreaType must be Strata (Private), Strata (Communal) or Common Area",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.363",
            RegulationText = "Strata lot areas must be classified as Strata (Private), Strata (Communal) or Common Area.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-URA-GFA-005",
            RuleName     = "GFA Connectivity - ACN_ConnectivityType must be from approved list",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "AREA_GFA",
            CheckParameter = "ACN_ConncectivityType",
            PermittedValues = new List<string>
            {
                "Open Walkway","Covered Walkway","Covered Linkway","Through Block Link","Elevated Pedestrian Link"
            },
            FormulaDescription = "Connectivity type must be from COP 3.1 approved connectivity categories",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.363",
            RegulationText = "Pedestrian connectivity spaces must use the approved connectivity type values.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-URA-GFA-006",
            RuleName     = "Landscape - ALS_LandscapeType must be from approved list",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "AREA_GFA",
            CheckParameter = "ALS_LandscapeType",
            PermittedValues = new List<string>
            {
                "Turfing","Groundcovers","Shrubs","Decked/ Patterned Floor",
                "Water Feature","Landscaped Footpath","Playground","BBQ Pit"
            },
            FormulaDescription = "Landscape type must be from approved list",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.364",
            RegulationText = "Landscape areas must be classified by approved landscape type for LUSH computation.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-URA-GFA-007",
            RuleName     = "Landscape - ALS_GreeneryFeatures must be from approved list",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.URA,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "AREA_GFA",
            CheckParameter = "ALS_GreeneryFeatures",
            PermittedValues = new List<string>
            {
                "Green Buffer","Peripheral Planting Verge","Landscape Deck - Vertical Greenery",
                "Landscape Deck - Surface Greenery","Communal Ground Garden","Sky Terrace",
                "Roof Top Landscaping","Ground Landscaping","Urban Farm / Greenhouse",
                "Vertical Greenery","Extensive Green Roof","Green Verge"
            },
            FormulaDescription = "Greenery feature must be from approved NParks/URA LUSH list",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.364",
            RegulationText = "Greenery features must be classified per the NParks LUSH 3.0 approved categories.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // HIGH PRIORITY: SPACE USAGE - SpaceName FULL ENUMERATION
        // Source: COP 3.1 Dec 2025 Section 4 pp.396-418
        // 16 categories, 200+ approved SpaceName values
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-SCDF-SPACE-001",
            RuleName     = "Space (Usage) - SpaceName must be from approved COP 3.1 list",
            Category     = DesignCodeCategory.SpaceUsageAndOccupancy,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "SPACE",
            CheckParameter = "SpaceName",
            PermittedValues = new List<string>
            {
                // 1) Living spaces (p.396)
                "Balcony","Bedroom","Master Bedroom","Maid Room","Guestroom","Bathroom","Master Bath",
                "Maid Bath","Yard Bath","Dining Room","Dining Area","Household Shelter","Kitchen",
                "Living Room","Living Area","Loft","Private Lift Lobby","Private Enclosed Space",
                "Service Yard","Study","Toilet","Walk-in Wardrobe",
                // 2) Temporary residences (p.397)
                "Student Bedroom Individual","Student Bedroom Multipax","Staff Quarters","Housekeeping",
                "Common Toilet","Isolation Ward Toilet","Individual Family Washroom",
                // 3) Non-residential toilet spaces (p.398)
                "Accessible Washroom","Male Toilet","Powder Room","Female Toilet",
                "Accessible Changing Room","Unisex Toilet","Male Shower Room","Female Shower Room",
                "Changing Room","Female Changing Room","Locker Room","Male Changing Room",
                // 4) Resting, care, hygiene spaces (p.399)
                "Sick Room","Restroom","Lactation Room","Nursing Room","Wash Area",
                // 5) Commercial, work, institutional (p.400)
                "Archive Room (Reading)","Archive Room (Stack)","Ball Room","Banking Hall","Bazaar",
                "Business Centre","Business Office","Cashier","Classroom","Clean Room",
                "Computer Classroom","Common Room","Computer Room","Conference Room","Concierge",
                "Consultant Room","Crematoria","Dance Studio","Department Store","Design Studio",
                "Detention Room","Exposition / Trade Fair Area","Filing Room","Store",
                "Fire Command Centre","Function Room","Guard House","Hobby Room","Exhibits Gallery",
                "Choir Gallery","Lab / Science Lab","Lecture Theatre","Library","Meeting Room",
                "Office","Retail","Shop","Showroom","Studio","Workshop",
                // 6) F&B spaces (p.401)
                "Canteen","Coffeeshop","Food Court","Hawker Centre","Kitchen","Pantry",
                "Restaurant","Bar","Pub","Cafeteria",
                // 7) Medical, healthcare (p.402)
                "Consultation Room","Day Surgery","Emergency Department","ICU","Operating Theatre",
                "Pharmacy","Physiotherapy","Recovery Room","Treatment Room","Ward",
                "Isolation Room","Radiology","Mortuary",
                // 8) Assembly spaces (p.403)
                "Auditorium","Chapel","Church Hall","Cinema","Concert Hall","Multipurpose Hall",
                "Performance Venue","Prayer Hall","Sports Hall","Theatre",
                // 9) Supporting spaces for performing (p.404)
                "Backstage","Dressing Room","Green Room","Rehearsal Room","Stage",
                // 10) Entertainment, recreation (p.405)
                "Amusement Arcade","Bowling Alley","Gym","Indoor Sports Hall","Karaoke",
                "Nightclub","Play Area","Recreation Room","Swimming Pool","Tennis Court",
                // 11) Open spaces (p.406)
                "Atrium","Covered Linkway","Covered Walkway","Open Corridor","Sky Garden",
                "Void Deck","Walkway",
                // 12) M&E spaces (p.407)
                "AHU Room","BMS Room","Control Room","Electrical Room","Generator Room",
                "Lift Machine Room","Mechanical Room","Plant Room","Pump Room","Switch Room",
                "Transformer Room","UPS Room",
                // 13) Storage (p.408)
                "Bin Centre","Bin Point","Car Park","Cargo Area","Cold Room","Loading Bay",
                "Refuse Room","Riser","Storeroom","Substation","Vault",
                // 14) Commuter facilities (p.409)
                "Bicycle Parking","Bus Bay","Car Park","Motorcycle Parking","Taxi Bay",
                // 15) Circulation (p.410)
                "Corridor","Escalator","Foyer","Landing","Lift Lobby","Lobby","Ramp",
                "Reception","Staircase","Void",
                // 16) Other non-simultaneous (p.411)
                "Accessible Route","Accessible Toilet","Fire Command Centre",
                "Household Shelter","Storey Shelter","Others"
            },
            FormulaDescription = "SpaceName must match an approved value from COP 3.1 Section 4 pp.396-418",
            CodeReference = "COP 3.1 Dec 2025 Section 4 pp.396-418",
            RegulationText = "All IfcSpace elements with SubType SPACE must have a SpaceName from the COP 3.1 approved list. " +
                             "SCDF uses SpaceName to determine OccupancyType and compute occupant loads per Fire Code Table 1.4B.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-SCDF-SPACE-002",
            RuleName     = "Space (Usage) - OccupancyType must match SpaceName",
            Category     = DesignCodeCategory.SpaceUsageAndOccupancy,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            PredefinedTypeFilter = "SPACE",
            CheckParameter = "OccupancyType",
            PermittedValues = new List<string>
            {
                "Multi-Unit Residential","Single-Unit Residential","Hotel","Dormitory","Hospital",
                "Education","Assembly","Office","Retail","Industrial","Warehouse","Car Park",
                "Medical Clinic","Place of Worship","Recreation","Restaurant and Bar",
                "Sports and Recreation","Any"
            },
            FormulaDescription = "OccupancyType must be from approved SCDF Fire Code Purpose Group list",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.396 - SCDF Fire Code Table 1.4A",
            RegulationText = "OccupancyType drives the SCDF Purpose Group assignment for fire safety calculations. " +
                             "Must match SCDF Fire Code Table 1.4A Purpose Groups.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: SCDF FIRE CODE - FRR BY BUILDING TYPE/HEIGHT
        // Source: COP 3.1 Dec 2025 Section 3 pp.73-81 + SCDF Fire Code 2023
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-SCDF-FIRE-001",
            RuleName     = "Fire Engine Accessway - Width compliance",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCCIVILELEMENT",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 4000,
            FormulaDescription = "Fire engine accessway Width >= 4000mm",
            CodeReference = "SCDF Fire Code 2023 §4.1 / COP 3.1 Dec 2025 Section 3 p.73",
            RegulationText = "Fire engine accessway must be at least 4.0 metres wide to accommodate SCDF fire engines. " +
                             "The accessway must be within 10m of fire access openings.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-002",
            RuleName     = "Exit Door - Fire exit width minimum",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCDOOR",
            CheckParameter = "FireExit",
            FormulaDescription = "All exit doors must have FireExit=TRUE and ClearWidth >= 850mm",
            CodeReference = "SCDF Fire Code 2023 §5.4 / COP 3.1 Dec 2025 Section 3 p.74",
            RegulationText = "Exit doors on means of escape must be clearly identified with FireExit=TRUE. " +
                             "Minimum clear width is 850mm for single leaf, 1200mm for double leaf.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-003",
            RuleName     = "Protected Staircase - FRR minimum",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "FireRating",
            CheckUnit    = "hr",
            MinimumValue = 1.0,
            FormulaDescription = "Protected stairs FireRating >= 1hr (60 min FRR)",
            CodeReference = "SCDF Fire Code 2023 §5.4.2 / COP 3.1 Dec 2025 p.74",
            RegulationText = "Walls enclosing a protected staircase (means of escape) must have FRR >= 60 minutes. " +
                             "Buildings above 24m require FRR >= 120 minutes for staircase enclosures.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-004",
            RuleName     = "Fire Compartment Wall - FRR minimum 60 min",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "FireRating",
            CheckUnit    = "hr",
            MinimumValue = 1.0,
            FormulaDescription = "Fire compartment walls FireRating >= 1hr (60 min)",
            CodeReference = "SCDF Fire Code 2023 Table 3.2 / COP 3.1 Dec 2025 p.74",
            RegulationText = "Walls forming fire compartment boundaries must achieve a minimum FRR of 60 minutes. " +
                             "Compartment walls between different occupancies require FRR >= 120 minutes.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-005",
            RuleName     = "Fire Door - FRR must match compartment wall",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCDOOR",
            CheckParameter = "FireRating",
            CheckUnit    = "hr",
            MinimumValue = 0.5,
            FormulaDescription = "Fire doors FireRating >= 0.5hr (30 min FRR minimum)",
            CodeReference = "SCDF Fire Code 2023 §4.3 / SS 332 / COP 3.1 Dec 2025 p.74",
            RegulationText = "Fire doors must achieve at minimum FRR 30 minutes. " +
                             "Doors in compartment walls >= 60 min must use FRR 60 min rated doors. " +
                             "Fire door sets must be certified to SS 332.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FIRE-006",
            RuleName     = "SCDF Fire Access Opening - mandatory on external walls",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCWINDOW",
            CheckParameter = "FireAccessOpening",
            FormulaDescription = "External windows on fire access facade must have FireAccessOpening=TRUE",
            CodeReference = "SCDF Fire Code 2023 §4.2 / COP 3.1 Dec 2025 p.74",
            RegulationText = "Windows on fire engine accessway facades must be designated as fire access openings " +
                             "(FireAccessOpening=TRUE) for SCDF rescue operations. " +
                             "Minimum clear dimensions: 600mm high x 500mm wide.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: JTC INDUSTRIAL REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 pp.44-51
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-JTC-IND-001",
            RuleName     = "JTC Industrial - Floor Loading declaration",
            Category     = DesignCodeCategory.StructuralAndFoundation,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.JTC,
            IfcClassFilter = "IFCSLAB",
            CheckParameter = "FloorLoading",
            CheckUnit    = "kN/m²",
            MinimumValue = 7.5,
            FormulaDescription = "JTC industrial floor slab FloorLoading >= 7.5 kN/m²",
            CodeReference = "JTC Development Guide 2024 §3.2 / COP 3.1 Dec 2025 Section 3 p.44",
            RegulationText = "For JTC industrial developments, all floor slabs must declare the design floor loading. " +
                             "Minimum: B1 industrial 7.5 kN/m², B2 and general industry 15 kN/m². " +
                             "Floor loading must match the approved use in the JTC tenancy agreement.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-JTC-IND-002",
            RuleName     = "JTC Industrial - Floor-to-ceiling height minimum",
            Category     = DesignCodeCategory.RoomSizesAndDimensions,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.JTC,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "Height",
            CheckUnit    = "m",
            MinimumValue = 6.0,
            FormulaDescription = "JTC industrial space Height >= 6.0m clear floor-to-soffit",
            CodeReference = "JTC Development Guide 2024 §3.3 / COP 3.1 Dec 2025 Section 3 p.44",
            RegulationText = "JTC B2 industrial buildings require minimum 6.0m clear floor-to-soffit height for manufacturing. " +
                             "Mezzanines are permitted but must not reduce the structural bay height below this minimum.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-JTC-IND-003",
            RuleName     = "JTC Industrial - Loading/unloading bay provision",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.JTC,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "LOADING_BAY",
            CheckParameter = "BayLength",
            CheckUnit    = "mm",
            MinimumValue = 12000,
            FormulaDescription = "JTC loading/unloading bay length >= 12000mm (12m)",
            CodeReference = "JTC Development Guide 2024 §5.1 / LTA guidelines / COP 3.1 p.44",
            RegulationText = "Loading bays for industrial developments must accommodate 40-foot containers. " +
                             "Minimum bay length: 12m (40ft), width: 3.6m, height clearance: 5m.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: GATEWAY G4 COMPLETION REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 pp.169-177
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-G4-BCA-001",
            RuleName     = "G4 Completion - All structural elements must have as-built Mark",
            Category     = DesignCodeCategory.StructuralAndFoundation,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCBEAM",
            Gateway      = CorenetGateway.Completion,
            CheckParameter = "Mark",
            FormulaDescription = "All beams must have Mark populated at G4 Completion Gateway",
            CodeReference = "COP 3.1 Dec 2025 Section 3 p.169 - G4 Completion BCA requirements",
            RegulationText = "At G4 Completion Gateway (TOP/CSC), all structural elements must have as-built mark numbers " +
                             "populated to allow verification against as-built structural drawings.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-G4-LTA-001",
            RuleName     = "G4 Completion - LTA road/as-built topographic survey",
            Category     = DesignCodeCategory.CivilAndInfrastructure,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.LTA,
            IfcClassFilter = "IFCCIVILELEMENT",
            Gateway      = CorenetGateway.Completion,
            CheckParameter = "Mark",
            FormulaDescription = "LTA road elements need as-built data at G4 Completion",
            CodeReference = "COP 3.1 Dec 2025 Section 3 p.169 - G4 LTA requirements",
            RegulationText = "At G4 Completion CSC, LTA requires: road data forms, asset master input forms, " +
                             "road test reports, declaration plans, as-built M&E plans. " +
                             "Final railway protection details with as-built plans must be provided.",
            IsRequired          = true,
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-G4-NEA-001",
            RuleName     = "G4 Completion - NEA photo evidence of compliance",
            Category     = DesignCodeCategory.EnvironmentalAndSustainability,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            Gateway      = CorenetGateway.Completion,
            CheckParameter = "AirChangeRate",
            FormulaDescription = "NEA ventilation compliance must be documented at G4",
            CodeReference = "COP 3.1 Dec 2025 Section 3 p.169",
            RegulationText = "At G4 Completion, NEA requires photo evidence demonstrating compliance with " +
                             "ventilation requirements, noise assessment reports (ACMV), and " +
                             "completed works reports for any pollution control equipment.",
            IsRequired          = true,
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-G4-PUB-001",
            RuleName     = "G4 Completion - PUB water fitting final declaration",
            Category     = DesignCodeCategory.WaterAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            Gateway      = CorenetGateway.Completion,
            CheckParameter = "WELS",
            FormulaDescription = "All sanitary fittings must have WELS=TRUE at G4 Completion",
            CodeReference = "COP 3.1 Dec 2025 Section 3 p.170",
            RegulationText = "At G4 Completion, PUB requires declaration that all water fittings are WELS-rated. " +
                             "As-built plumbing plans and commissioning test records must be provided.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: NEA FULL REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 pp.52-63
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-NEA-VENT-001",
            RuleName     = "NEA Ventilation - Car park minimum air change rate",
            Category     = DesignCodeCategory.EnvironmentalAndSustainability,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CAR_PARK",
            CheckParameter = "AirChangeRate",
            CheckUnit    = "ACH",
            MinimumValue = 6.0,
            FormulaDescription = "Car park AirChangeRate >= 6.0 ACH",
            CodeReference = "NEA EPH (Air Impurities) Regulations / COP 3.1 Dec 2025 p.52",
            RegulationText = "Basement/covered car parks must have minimum 6 air changes per hour (ACH) via mechanical ventilation. " +
                             "CO monitoring system must be installed with automatic activation at 50ppm.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-NEA-VENT-002",
            RuleName     = "NEA Ventilation - Kitchen exhaust provision",
            Category     = DesignCodeCategory.EnvironmentalAndSustainability,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "KITCHEN",
            CheckParameter = "AirChangeRate",
            CheckUnit    = "ACH",
            MinimumValue = 20.0,
            FormulaDescription = "Commercial kitchen AirChangeRate >= 20 ACH",
            CodeReference = "NEA EPH Regulations §5 / COP 3.1 Dec 2025 p.53",
            RegulationText = "Commercial kitchens must provide minimum 20 ACH exhaust ventilation. " +
                             "Grease filters and grease traps (IfcInterceptor/GREASE) must be provided.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-NEA-REFUSE-001",
            RuleName     = "NEA Refuse - Bin centre minimum area",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "BIN_CENTRE",
            CheckParameter = "GrossPlannedArea",
            CheckUnit    = "m²",
            MinimumValue = 4.5,
            FormulaDescription = "Bin centre GrossPlannedArea >= 4.5 m²",
            CodeReference = "NEA Refuse Management Requirements / COP 3.1 Dec 2025 p.54",
            RegulationText = "Bin centre must have minimum 4.5 m² floor area with direct vehicular access. " +
                             "Minimum clear height 2.4m. Roller shutter access minimum 2.4m wide, 2.4m high.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: LTA FULL REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 pp.45-51
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-LTA-PARK-001",
            RuleName     = "LTA Parking - Accessible PWD bay dimensions",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            PredefinedTypeFilter = "CARPWDPARKINGLOT",
            CheckParameter = "BayWidth",
            CheckUnit    = "mm",
            MinimumValue = 3600,
            FormulaDescription = "PWD parking bay BayWidth >= 3600mm",
            CodeReference = "LTA Code of Practice for Vehicle Parking Provision 2019 §3.4 / COP 3.1 Dec 2025 p.45",
            RegulationText = "Accessible (PWD) parking bays must be minimum 3600mm wide x 5000mm long. " +
                             "Must be located as close as possible to accessible building entrances. " +
                             "Transfer space of 1200mm minimum must be provided beside each PWD bay.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-LTA-PARK-002",
            RuleName     = "LTA Parking - Standard car bay minimum width",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            PredefinedTypeFilter = "CARGENERALPARKINGLOT",
            CheckParameter = "BayWidth",
            CheckUnit    = "mm",
            MinimumValue = 2400,
            FormulaDescription = "Standard car bay BayWidth >= 2400mm",
            CodeReference = "LTA Code of Practice for Vehicle Parking Provision 2019 §3.3 / COP 3.1 Dec 2025 p.45",
            RegulationText = "Standard car parking bays must be minimum 2400mm wide x 4800mm long. " +
                             "Head room minimum 2200mm. Aisle width minimum 6000mm for 90-degree bays.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-LTA-PARK-003",
            RuleName     = "LTA Parking - Lorry bay minimum dimensions",
            Category     = DesignCodeCategory.PlanningAndGFA,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.LTA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            PredefinedTypeFilter = "LORRYLOT",
            CheckParameter = "BayLength",
            CheckUnit    = "mm",
            MinimumValue = 12000,
            FormulaDescription = "Lorry bay BayLength >= 12000mm (12m for 40ft container)",
            CodeReference = "LTA Code of Practice for Vehicle Parking Provision 2019 §3.5 / COP 3.1 Dec 2025 p.45",
            RegulationText = "Lorry parking bays must accommodate 40-foot containers (12m). " +
                             "Width minimum 3600mm. Head clearance minimum 4800mm.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: PUB FULL REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 pp.68-72
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-PUB-DRAIN-001",
            RuleName     = "PUB Drainage - Minimum gradient for foul water drain",
            Category     = DesignCodeCategory.WaterAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCPIPESEGMENT",
            CheckParameter = "Gradient",
            MinimumValue = 0.01,
            FormulaDescription = "Foul water drain Gradient >= 0.01 (1:100)",
            CodeReference = "PUB Code of Practice on Sewerage and Sanitary Works 2019 §4.3 / COP 3.1 p.68",
            RegulationText = "Soil and foul water drainage pipes must have minimum fall of 1:100 (1%). " +
                             "Stormwater drains minimum 1:200 (0.5%). " +
                             "PUB may require flatter gradients for specific circumstances subject to approval.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-PUB-WELS-001",
            RuleName     = "PUB WELS - WC must be WELS rated (minimum 3 ticks)",
            Category     = DesignCodeCategory.WaterAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            PredefinedTypeFilter = "WATERCLOSET",
            CheckParameter = "WELS",
            PermittedValues = new List<string> { "TRUE","True","true","1","Yes","YES" },
            FormulaDescription = "WC must have WELS=TRUE (minimum 3-tick dual flush cistern)",
            CodeReference = "PUB WELS Requirements 2024 / SS 608-2:2020 / COP 3.1 p.68",
            RegulationText = "All WCs installed in new buildings must carry the WELS label with minimum 3 ticks. " +
                             "3-tick WC flush volume: max 4.5L full flush, max 3.0L half flush. " +
                             "Single flush cisterns are not permitted for new installations.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-PUB-WELS-002",
            RuleName     = "PUB WELS - Basin and shower minimum 2 ticks",
            Category     = DesignCodeCategory.WaterAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            PredefinedTypeFilter = "WASHHANDBASIN",
            CheckParameter = "WELS",
            PermittedValues = new List<string> { "TRUE","True","true","1","Yes","YES" },
            FormulaDescription = "Wash basin must have WELS=TRUE (min 2-tick, max 6L/min flow)",
            CodeReference = "PUB WELS Requirements 2024 / SS 608-1:2020 / COP 3.1 p.68",
            RegulationText = "Tap fittings and showers must carry minimum 2-tick WELS rating. " +
                             "2-tick flow rates: tap <= 6L/min at 3 bar, shower <= 9L/min at 3 bar.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: NPARKS LUSH REQUIREMENTS
        // Source: COP 3.1 Dec 2025 Section 3 pp.64-67
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-NParks-LUSH-001",
            RuleName     = "NParks LUSH - Botanical name required for trees",
            Category     = DesignCodeCategory.LandscapeAndGreenery,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            PredefinedTypeFilter = "LANDSCAPE_TREE",
            CheckParameter = "PlantSpecies",
            FormulaDescription = "Tree elements must have PlantSpecies set to full botanical name",
            CodeReference = "NParks LUSH 3.0 Programme / COP 3.1 Dec 2025 Section 3 p.64",
            RegulationText = "All trees must have PlantSpecies set to the full botanical (Latin) name as listed in " +
                             "the NParks Flora & Fauna Web database (https://www.nparks.gov.sg/florafaunaweb). " +
                             "Common names are not accepted for CORENET-X submission.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-NParks-LUSH-002",
            RuleName     = "NParks LUSH - Palm botanical name required",
            Category     = DesignCodeCategory.LandscapeAndGreenery,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            PredefinedTypeFilter = "LANDSCAPE_PALM",
            CheckParameter = "PlantSpecies",
            FormulaDescription = "Palm elements must have PlantSpecies set to full botanical name",
            CodeReference = "NParks LUSH 3.0 Programme / COP 3.1 Dec 2025 Section 3 p.64",
            RegulationText = "All palms must have PlantSpecies set to the full botanical name from NParks Flora & Fauna Web. " +
                             "Girth size and planting method must also be specified.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-NParks-LUSH-003",
            RuleName     = "NParks Landscape - GirthSize minimum for transplanted trees",
            Category     = DesignCodeCategory.LandscapeAndGreenery,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            PredefinedTypeFilter = "LANDSCAPE_TREE",
            CheckParameter = "GirthSize",
            CheckUnit    = "mm",
            MinimumValue = 100.0,
            FormulaDescription = "Trees GirthSize >= 100mm (measured 1m from ground)",
            CodeReference = "NParks Approved Soil Mixture (ASM) and Planting Specifications / COP 3.1 p.64",
            RegulationText = "Trees for CORENET-X submission must have a minimum girth of 100mm measured at 1m above ground level. " +
                             "Tree girth specification is required for NParks LUSH computation and tree retention tracking.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY: BCA GREEN MARK FULL REQUIREMENTS
        // Source: BCA Green Mark 2021 (referenced in COP 3.1)
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-GM-001",
            RuleName     = "Green Mark - Roof slab ThermalTransmittance maximum",
            Category     = DesignCodeCategory.EnvironmentalAndSustainability,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSLAB",
            PredefinedTypeFilter = "ROOF",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/m²K",
            MaximumValue = 0.4,
            FormulaDescription = "Roof slab ThermalTransmittance <= 0.40 W/m²K",
            CodeReference = "BCA Green Mark 2021 §3.4.1 / ETTV calculation",
            RegulationText = "Roof U-value must not exceed 0.40 W/m²K for buildings to meet BCA Green Mark " +
                             "baseline requirements. This contributes to the RETV (Residential) / ETTV (Non-residential) calculation.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-GM-002",
            RuleName     = "Green Mark - External wall ThermalTransmittance maximum",
            Category     = DesignCodeCategory.EnvironmentalAndSustainability,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/m²K",
            MaximumValue = 0.5,
            FormulaDescription = "External wall ThermalTransmittance <= 0.50 W/m²K",
            CodeReference = "BCA Green Mark 2021 §3.4.2",
            RegulationText = "External walls U-value must not exceed 0.50 W/m²K for BCA Green Mark compliance.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-BCA-GM-003",
            RuleName     = "Green Mark - Window OTTV maximum",
            Category     = DesignCodeCategory.EnvironmentalAndSustainability,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWINDOW",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/m²K",
            MaximumValue = 2.8,
            FormulaDescription = "Window ThermalTransmittance <= 2.8 W/m²K",
            CodeReference = "BCA Green Mark 2021 §3.4.3 / SS 553 OTTV",
            RegulationText = "Window glazing U-value and SHGC must be controlled to limit OTTV. " +
                             "Reference U-value limit: 2.8 W/m²K for non-residential, 3.2 W/m²K for residential.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY: SLA CADASTRAL / GEOREFERENCING
        // Source: COP 3.1 Dec 2025 (SLA requirements embedded across agencies)
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-SLA-GEO-001",
            RuleName     = "SLA Georeferencing - Site must have SVY21 coordinates",
            Category     = DesignCodeCategory.GeoreferencingAndSurvey,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SLA,
            IfcClassFilter = "IFCSITE",
            CheckParameter = "RefLatitude",
            FormulaDescription = "IfcSite must have SVY21 coordinates (RefLatitude/RefLongitude non-zero)",
            CodeReference = "SLA Code of Practice for Land Survey / COP 3.1 Dec 2025 p.95",
            RegulationText = "All IFC models must be georeferenced to the Singapore SVY21 coordinate system. " +
                             "IfcMapConversion must be present with Eastings/Northings in SVY21 and " +
                             "OrthogonalHeight in SHD (Singapore Height Datum). " +
                             "SLA requires all submitted models to pass Level 13 Georeferencing check.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SLA-GEO-002",
            RuleName     = "SLA Cadastral - LandLotNumber must match SLA land register",
            Category     = DesignCodeCategory.GeoreferencingAndSurvey,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SLA,
            IfcClassFilter = "IFCSITE",
            CheckParameter = "LandLotNumber",
            FormulaDescription = "IfcSite LandLotNumber must be populated",
            CodeReference = "SLA INLIS / COP 3.1 Dec 2025",
            RegulationText = "The LandLotNumber must match the cadastral lot number as registered in SLA's " +
                             "Integrated Land Information Service (INLIS). " +
                             "Format: 'Lot [number][suffix] MK[mukim number]' e.g. 'Lot 12345K MK10'. " +
                             "Multiple lots must all be listed.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY: FULL CODE ON ACCESSIBILITY 2025 DIMENSIONS
        // Source: BCA Code on Accessibility 2025
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-ACC-001",
            RuleName     = "Accessibility - Lift car minimum dimensions",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCTRANSPORTELEMENT",
            PredefinedTypeFilter = "LIFT",
            CheckParameter = "BarrierFreeAccessibility",
            PermittedValues = new List<string> { "TRUE","True","true","1","Yes","YES" },
            FormulaDescription = "Accessible lifts must have BarrierFreeAccessibility=TRUE with min 1100mm x 1400mm car",
            CodeReference = "BCA Code on Accessibility 2025 §6.1 / SS 553 §7",
            RegulationText = "Accessible lifts must have: car width >= 1100mm, car depth >= 1400mm, " +
                             "door clear opening >= 900mm, landing zone at least 1500mm x 1500mm. " +
                             "Must have tactile floor indicators, audible floor announcements, and Braille signage.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-BCA-ACC-002",
            RuleName     = "Accessibility - Stair effective width minimum",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "EffectiveWidth",
            CheckUnit    = "mm",
            MinimumValue = 1100,
            FormulaDescription = "Stair EffectiveWidth >= 1100mm (1200mm above 24m building height)",
            CodeReference = "BCA Code on Accessibility 2025 §4.3 / SCDF Fire Code §5.4",
            RegulationText = "All staircases must have minimum effective (clear) width of 1100mm. " +
                             "Buildings above 24m require 1200mm minimum. " +
                             "Effective width measured between handrails (not between walls).",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-BCA-ACC-003",
            RuleName     = "Accessibility - Ramp handrail height",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCRAMP",
            CheckParameter = "HandrailHeight",
            CheckUnit    = "mm",
            MinimumValue = 850,
            MaximumValue = 1000,
            FormulaDescription = "Accessible ramp HandrailHeight between 850mm and 1000mm",
            CodeReference = "BCA Code on Accessibility 2025 §4.3.4",
            RegulationText = "Handrails on accessible ramps must be between 850mm and 1000mm above ramp surface. " +
                             "Both sides required. Continuous handrail extending 300mm beyond top and bottom.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-BCA-ACC-004",
            RuleName     = "Accessibility - Tactile tile must be at hazard locations",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            PredefinedTypeFilter = "TACTILETILE",
            CheckParameter = "TactileTileType",
            PermittedValues = new List<string>
            { "ATTENTIONINDICATOR","DIRECTIONINDICATOR","Attention Indicator","Direction Indicator" },
            FormulaDescription = "Tactile tiles must be classified as ATTENTIONINDICATOR or DIRECTIONINDICATOR",
            CodeReference = "BCA Code on Accessibility 2025 §3.2 / SS 553",
            RegulationText = "Tactile tiles must be either: Attention Indicators (dotted, at hazard locations) " +
                             "or Direction Indicators (striped, on accessible routes). " +
                             "Material must contrast with surrounding floor.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY: MODEL QUALITY CHECKLIST
        // Source: COP 3.1 Dec 2025 Section 4 pp.245-246
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-MQC-001",
            RuleName     = "Model Quality - No gap between cadastral lot boundaries",
            Category     = DesignCodeCategory.ModelQuality,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SLA,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            PredefinedTypeFilter = "CADASTRALLOT",
            CheckParameter = "LotArea",
            FormulaDescription = "Cadastral lot boundaries must be continuous with no gaps",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.245 - Model Quality Quick Checklist",
            RegulationText = "Check that there is no gap between boundaries of cadastral lots. " +
                             "Each common boundary between strata lots or common property must align exactly " +
                             "at the centre of the floor, wall or ceiling.",
            FailSeverity = Severity.Warning
        },

        new() {
            RuleId       = "SG-MQC-002",
            RuleName     = "Model Quality - Spaces must be adjacent to walls or floors",
            Category     = DesignCodeCategory.ModelQuality,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "GrossPlannedArea",
            MinimumValue = 0.1,
            FormulaDescription = "All IfcSpace elements must have a positive area > 0",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.245 - Model Quality Quick Checklist",
            RegulationText = "Check that spaces are directly adjacent to other space components, surrounding walls or floors below. " +
                             "Spaces with zero or negative area indicate modelling errors.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-MQC-003",
            RuleName     = "Model Quality - IFC+SG parameters must be in correct units",
            Category     = DesignCodeCategory.ModelQuality,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCBUILDING",
            CheckParameter = "GrossPlannedArea",
            FormulaDescription = "All IFC+SG structural parameters must use correct units and input format per COP 3.1",
            CodeReference = "COP 3.1 Dec 2025 Section 4 p.246 - Model Quality Quick Checklist",
            RegulationText = "Ensure all IFC+SG parameters follow the IFC+SG property set, property type and standardized " +
                             "naming as described in Section 4. All structural parameter inputs must be in correct units: " +
                             "lengths in mm, levels in SHD metres, loads in kN, areas in m².",
            FailSeverity = Severity.Warning
        },



        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: SCDF FIRE CODE 2023 - FRR BY BUILDING TYPE
        // Source: SCDF Fire Code 2023 Table 3.2 / 4.2, COP 3.1 §3 pp.73-81
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-SCDF-FRR-001",
            RuleName     = "Fire Compartment Wall - Minimum FRR (Residential)",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "FireRating",
            CheckUnit    = "hr",
            MinimumValue = 1.0,
            FormulaDescription = "FireRating(Fire Compartment Wall, Residential) ≥ 1 hr",
            CodeReference = "SCDF Fire Code 2023 Table 3.2",
            RegulationText = "Fire compartment walls in residential buildings (flats, condominiums) must have minimum FRR of 1 hour (60 minutes).",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FRR-002",
            RuleName     = "Fire Compartment Wall - Minimum FRR (Commercial/Industrial)",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "FireRating",
            CheckUnit    = "hr",
            MinimumValue = 2.0,
            FormulaDescription = "FireRating(Fire Compartment Wall, Commercial/Industrial) ≥ 2 hr",
            CodeReference = "SCDF Fire Code 2023 Table 3.2",
            RegulationText = "Fire compartment walls in commercial and industrial buildings require minimum FRR of 2 hours. Sprinklered buildings may qualify for reduction to 1 hour.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FRR-003",
            RuleName     = "Fire Exit Staircase Enclosure - Minimum FRR",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "FireRating",
            CheckUnit    = "hr",
            MinimumValue = 1.0,
            FormulaDescription = "FireRating(Fire Exit Staircase enclosure) ≥ 1 hr",
            CodeReference = "SCDF Fire Code 2023 §5.4",
            RegulationText = "All fire escape staircase enclosures must have FRR ≥ 60 minutes. Above 24m building height, FRR ≥ 120 minutes required.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-FRR-004",
            RuleName     = "Fire Door - FRR must match compartment wall",
            Category     = DesignCodeCategory.FireSafety,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCDOOR",
            CheckParameter = "FireRating",
            CheckUnit    = "hr",
            MinimumValue = 0.5,
            FormulaDescription = "FireRating(Fire Door) ≥ 0.5 hr (minimum), matching the FRR of the enclosing compartment wall",
            CodeReference = "SCDF Fire Code 2023 §4.3, SS 332",
            RegulationText = "Fire doors must have FRR not less than the fire resistance of the enclosing element. Minimum 0.5hr (30 min). Typical values: 0.5hr, 1hr, 2hr.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-TRAVEL-001",
            RuleName     = "Travel Distance - Single Exit (Unsprinklered)",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistance",
            CheckUnit    = "m",
            MaximumValue = 18.0,
            FormulaDescription = "TravelDistance(Single exit, unsprinklered) ≤ 18 m",
            CodeReference = "SCDF Fire Code 2023 §5.5",
            RegulationText = "Maximum travel distance to a single exit in an unsprinklered building is 18 metres. For sprinklered buildings, maximum 30 metres.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-SCDF-TRAVEL-002",
            RuleName     = "Travel Distance - To Nearest Exit (Unsprinklered)",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistance",
            CheckUnit    = "m",
            MaximumValue = 30.0,
            FormulaDescription = "TravelDistance(to nearest exit, unsprinklered) ≤ 30 m",
            CodeReference = "SCDF Fire Code 2023 §5.5",
            RegulationText = "Maximum travel distance to the nearest exit in an unsprinklered building is 30 metres (45m if sprinklered).",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-SCDF-EXIT-001",
            RuleName     = "Exit Staircase Width - Below 24m Building Height",
            Category     = DesignCodeCategory.FireSafetyAndEscape,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.SCDF,
            IfcClassFilter = "IFCSTAIR",
            CheckParameter = "EffectiveWidth",
            CheckUnit    = "mm",
            MinimumValue = 1100,
            FormulaDescription = "EffectiveWidth(Fire Exit Staircase) ≥ 1100mm (buildings ≤ 24m)",
            CodeReference = "SCDF Fire Code 2023 §5.4.3",
            RegulationText = "Minimum clear width of exit staircase enclosure for buildings not exceeding 24m is 1100mm. For buildings exceeding 24m, minimum 1200mm.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: PUB - WELS FLOW RATES & DRAINAGE GRADIENTS
        // Source: PUB WELS requirements, COP 3.1 §3 pp.68-72
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-PUB-WELS-001",
            RuleName     = "WC - Maximum Flush Volume (3-tick WELS)",
            Category     = DesignCodeCategory.WaterAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            SpaceCategoryFilter = "WATERCLOSET",
            CheckParameter = "WELSVolume",
            CheckUnit    = "L",
            MaximumValue = 4.5,
            FormulaDescription = "WELSVolume(WC, full flush) ≤ 4.5L (3-tick minimum)",
            CodeReference = "PUB WELS requirements 2023, SS 608-2:2020",
            RegulationText = "All water closets must achieve minimum 3-tick WELS rating. Maximum full flush volume: 4.5L. Maximum reduced flush: 3.0L. Dual-flush cisterns preferred.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-PUB-WELS-002",
            RuleName     = "Wash Basin Tap - Maximum Flow Rate (2-tick WELS)",
            Category     = DesignCodeCategory.WaterAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            SpaceCategoryFilter = "WASHHANDBASIN",
            CheckParameter = "WELSFlowRate",
            CheckUnit    = "L/min",
            MaximumValue = 6.0,
            FormulaDescription = "WELSFlowRate(Wash basin tap) ≤ 6.0 L/min (2-tick minimum)",
            CodeReference = "PUB WELS requirements 2023",
            RegulationText = "Wash basin taps and mixers must achieve minimum 2-tick WELS. Maximum flow rate 6.0 L/min at 3 bar. 3-tick preferred (≤ 4.0 L/min).",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-PUB-WELS-003",
            RuleName     = "Shower - Maximum Flow Rate (2-tick WELS)",
            Category     = DesignCodeCategory.WaterAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCSANITARYTERMINAL",
            SpaceCategoryFilter = "SHOWER",
            CheckParameter = "WELSFlowRate",
            CheckUnit    = "L/min",
            MaximumValue = 9.0,
            FormulaDescription = "WELSFlowRate(Shower) ≤ 9.0 L/min (2-tick minimum)",
            CodeReference = "PUB WELS requirements 2023",
            RegulationText = "Shower fittings must achieve minimum 2-tick WELS. Maximum flow rate 9.0 L/min at 3 bar. 3-tick preferred (≤ 7.0 L/min).",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-PUB-DRAIN-001",
            RuleName     = "Foul Water Drain - Minimum Gradient",
            Category     = DesignCodeCategory.PlumbingAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCPIPESEGMENT",
            CheckParameter = "Gradient",
            CheckUnit    = "ratio",
            MinimumValue = 0.01,
            FormulaDescription = "Gradient(foul water drain) ≥ 1:100 (0.01)",
            CodeReference = "PUB Code of Practice on Sewerage and Sanitary Works 2019 §4.3",
            RegulationText = "All foul water drains must have minimum gradient of 1:100 (1%). Preferred gradient 1:50 for pipes ≤100mm diameter.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-PUB-DRAIN-002",
            RuleName     = "Stormwater Drain - Minimum Gradient",
            Category     = DesignCodeCategory.PlumbingAndDrainage,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.PUB,
            IfcClassFilter = "IFCPIPESEGMENT",
            CheckParameter = "Gradient",
            CheckUnit    = "ratio",
            MinimumValue = 0.005,
            FormulaDescription = "Gradient(stormwater drain) ≥ 1:200 (0.005)",
            CodeReference = "PUB Code of Practice on Surface Water Drainage §3.2",
            RegulationText = "All stormwater drains must have minimum gradient of 1:200 (0.5%). Flat areas require pumped systems.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: BCA GREEN MARK 2021 - THERMAL TRANSMITTANCE
        // Source: BCA Green Mark for Buildings NRB:2021, COP 3.1 §3 pp.33-44
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-GM-001",
            RuleName     = "External Wall - Maximum Thermal Transmittance (U-value)",
            Category     = DesignCodeCategory.EnergyPerformance,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWALL",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/m²K",
            MaximumValue = 0.50,
            FormulaDescription = "ThermalTransmittance(External Wall) ≤ 0.50 W/m²K",
            CodeReference = "BCA Green Mark NRB:2021 §3.4.1, ETTV requirements",
            RegulationText = "External walls must not exceed U-value of 0.50 W/m²K. For Green Mark Certified, maximum 0.45. For Green Mark GoldPLUS/Platinum, maximum 0.40.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-BCA-GM-002",
            RuleName     = "Roof - Maximum Thermal Transmittance (RTTV)",
            Category     = DesignCodeCategory.EnergyPerformance,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSLAB",
            SpaceCategoryFilter = "ROOF",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/m²K",
            MaximumValue = 0.50,
            FormulaDescription = "ThermalTransmittance(Roof Slab) ≤ 0.50 W/m²K (RTTV component)",
            CodeReference = "BCA Green Mark NRB:2021 §3.4.2",
            RegulationText = "Roof slabs must not exceed U-value of 0.50 W/m²K for RTTV calculation compliance. Green roof systems count towards RTTV improvement.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-BCA-GM-003",
            RuleName     = "External Window - Maximum Thermal Transmittance",
            Category     = DesignCodeCategory.EnergyPerformance,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCWINDOW",
            CheckParameter = "ThermalTransmittance",
            CheckUnit    = "W/m²K",
            MaximumValue = 2.80,
            FormulaDescription = "ThermalTransmittance(External Window/Glazing) ≤ 2.80 W/m²K",
            CodeReference = "BCA Green Mark NRB:2021 §3.4.1, ETTV",
            RegulationText = "External windows and glazing must not exceed U-value of 2.80 W/m²K. Low-emissivity glazing (U ≤ 2.0) contributes to ETTV compliance.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // MEDIUM PRIORITY: NPARKS - PLANT SPECIES REQUIREMENTS
        // Source: NParks LUSH 3.0 Programme, COP 3.1 §3 pp.64-67
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-NPARKS-001",
            RuleName     = "Landscape Plant - Botanical Species Name Required",
            Category     = DesignCodeCategory.LandscapeAndGreenery,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            CheckParameter = "PlantSpecies",
            FormulaDescription = "PlantSpecies(IfcGeographicElement) must be provided as botanical name",
            CodeReference = "NParks LUSH 3.0 Programme, NParks Guidelines Chapter 2",
            RegulationText = "All trees, palms and shrubs must have PlantSpecies declared using the full botanical name (e.g. Terminalia mantaly, Heliconia psittacorum). Common names are not accepted. Refer to NParks Flora and Fauna Web for approved species list.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-NPARKS-002",
            RuleName     = "Landscape Tree - Minimum Girth Size",
            Category     = DesignCodeCategory.LandscapeAndGreenery,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NParks,
            IfcClassFilter = "IFCGEOGRAPHICELEMENT",
            CheckParameter = "GirthSize",
            CheckUnit    = "mm",
            MinimumValue = 150,
            FormulaDescription = "GirthSize(Landscape Tree at 1m height) ≥ 150mm girth",
            CodeReference = "NParks Guidelines for Planting, NParks LUSH 3.0",
            RegulationText = "Trees planted in developments must have minimum girth of 150mm (measured at 1 metre above ground). Heritage trees and transplanted trees may have specific requirements.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOW PRIORITY: COMPLETE ACCESSIBILITY DIMENSIONS
        // Source: Code on Accessibility 2025, COP 3.1 §3 pp.33-44
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-BCA-ACC-010",
            RuleName     = "Accessible Corridor - Minimum Clear Width",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CORRIDOR",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 1200,
            FormulaDescription = "Width(Accessible Corridor) ≥ 1200mm",
            CodeReference = "Code on Accessibility 2025 §4.1",
            RegulationText = "Accessible corridors must have minimum clear width of 1200mm. Where passing places are provided for manual wheelchairs, minimum 900mm is acceptable with 1800mm passing points at max 25m intervals.",
            FailSeverity = Severity.Critical
        },

        new() {
            RuleId       = "SG-BCA-ACC-011",
            RuleName     = "Handrail - Height Range on Accessible Routes",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCRAILING",
            CheckParameter = "Height",
            CheckUnit    = "mm",
            MinimumValue = 850,
            MaximumValue = 950,
            FormulaDescription = "HandrailHeight(Accessible Route) between 850-950mm",
            CodeReference = "Code on Accessibility 2025 §5.3",
            RegulationText = "Handrails on accessible routes (ramps, stairs) must be at height between 850mm and 950mm above the finished floor/nosing. Dual handrails: lower rail at 650-750mm.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-BCA-ACC-012",
            RuleName     = "Accessible Lift Car - Minimum Internal Dimensions",
            Category     = DesignCodeCategory.AccessibilityAndUniversalDesign,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.BCA,
            IfcClassFilter = "IFCTRANSPORTELEMENT",
            CheckParameter = "Width",
            CheckUnit    = "mm",
            MinimumValue = 1100,
            FormulaDescription = "Width(Accessible Lift Car) ≥ 1100mm (depth ≥ 1400mm)",
            CodeReference = "Code on Accessibility 2025 §6.1",
            RegulationText = "Accessible lift cars must have minimum internal dimensions of 1100mm wide x 1400mm deep. Door clear opening minimum 900mm. Audible and visual floor indicators required.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOW PRIORITY: JTC FLOOR LOADING REQUIREMENTS
        // Source: JTC Industrial Tenancy Conditions, BC 2:2021 loading
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-JTC-LOAD-001",
            RuleName     = "Industrial Floor Slab - Minimum Live Load (B1 General)",
            Category     = DesignCodeCategory.StructuralAdequacy,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.JTC,
            IfcClassFilter = "IFCSLAB",
            CheckParameter = "WorkingLoad",
            CheckUnit    = "kPa",
            MinimumValue = 7.5,
            FormulaDescription = "WorkingLoad(Industrial Floor, B1 General) ≥ 7.5 kPa",
            CodeReference = "BC 2:2021 Table 3.1, JTC Tenancy Conditions",
            RegulationText = "For JTC B1 industrial floor slabs, minimum live load capacity is 7.5 kPa (general use). For B2 heavy industrial, minimum 10 kPa. For racking system areas, design load per racking specification.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-JTC-RACK-001",
            RuleName     = "Racking System - WorkingLoad Declaration Required",
            Category     = DesignCodeCategory.StructuralAdequacy,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.JTC,
            IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            CheckParameter = "WorkingLoad",
            FormulaDescription = "WorkingLoad(Racking System) must be declared",
            CodeReference = "JTC Industrial Tenancy Conditions §4.2",
            RegulationText = "Racking systems in JTC premises must declare the design working load. The floor slab must be verified to support the racking point loads. Manufacturer's load specifications required.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // LOW PRIORITY: NEA VENTILATION RATES BY SPACE TYPE
        // Source: NEA EPH Regulations, SS 553:2016
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId       = "SG-NEA-VENT-001",
            RuleName     = "Office Space - Minimum Fresh Air Rate",
            Category     = DesignCodeCategory.VentilationAndAirQuality,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "OFFICE",
            CheckParameter = "AirChangeRate",
            CheckUnit    = "ACH",
            MinimumValue = 6.0,
            FormulaDescription = "AirChangeRate(Office) ≥ 6 ACH",
            CodeReference = "NEA EPH (Emissions) Regulations, SS 553:2016 Table 1",
            RegulationText = "Office spaces must provide minimum 6 air changes per hour. Alternatively: minimum 10 L/s per person plus 0.6 L/s per m² of conditioned area.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-NEA-VENT-002",
            RuleName     = "Car Park - Minimum Ventilation Rate",
            Category     = DesignCodeCategory.VentilationAndAirQuality,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "CARPARK",
            CheckParameter = "AirChangeRate",
            CheckUnit    = "ACH",
            MinimumValue = 6.0,
            FormulaDescription = "AirChangeRate(Carpark) ≥ 6 ACH",
            CodeReference = "NEA EPH (Emissions) Regulations §6",
            RegulationText = "Enclosed car parks must provide minimum 6 air changes per hour or CO sensor-controlled ventilation system maintaining CO < 50 ppm (8-hour average) and < 100 ppm (1-hour average).",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId       = "SG-NEA-VENT-003",
            RuleName     = "Kitchen - Minimum Exhaust Air Change Rate",
            Category     = DesignCodeCategory.VentilationAndAirQuality,
            Country      = CountryMode.Singapore,
            Agency       = SgAgency.NEA,
            IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "KITCHEN",
            CheckParameter = "AirChangeRate",
            CheckUnit    = "ACH",
            MinimumValue = 20.0,
            FormulaDescription = "AirChangeRate(Kitchen exhaust) ≥ 20 ACH",
            CodeReference = "NEA Code of Practice on Pollution Control, SS 553:2016",
            RegulationText = "Commercial kitchens must provide minimum 20 air changes per hour with mechanical exhaust. Grease filters, grease interceptors and exhaust treatment required for cooking with oil/grease.",
            FailSeverity = Severity.Error
        },

        new() {
            RuleId = "SG-SCDF-FEA-001", RuleName = "Fire Engine Accessway - Minimum Width",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Singapore,
            Agency = SgAgency.SCDF, IfcClassFilter = "IFCCIVILELEMENT",
            CheckParameter = "Width", CheckUnit = "mm", MinimumValue = 4000,
            FormulaDescription = "FireEngineAccessway.Width >= 4000mm",
            CodeReference = "SCDF Fire Code 2023 §4.1 - Fire Engine Accessway",
            RegulationText = "All fire engine accessways shall have a minimum clear width of 4,000mm and minimum headroom clearance of 4,000mm. The accessway surface shall be capable of supporting a fire engine axle load of 12 tonnes.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "SG-SCDF-FEA-002", RuleName = "Fire Engine Accessway - Maximum Distance to Building Face",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Singapore,
            Agency = SgAgency.SCDF, IfcClassFilter = "IFCBUILDING",
            CheckParameter = "DistanceToFireAccessway", CheckUnit = "m", MaximumValue = 10.0,
            FormulaDescription = "DistanceFromBuildingFaceToFireAccessway <= 10m",
            CodeReference = "SCDF Fire Code 2023 §4.1.3",
            RegulationText = "The distance from the fire engine accessway to the face of the building shall not exceed 10m. For buildings requiring fire engine access on more than one face, all required faces shall comply.",
            FailSeverity = Severity.Critical
        },

        // ── Fire Compartmentation ─────────────────────────────────────────────
        new() {
            RuleId = "SG-SCDF-TD-001", RuleName = "Travel Distance - Residential (Single Direction)",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Singapore,
            Agency = SgAgency.SCDF, IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistance", CheckUnit = "m", MaximumValue = 9,
            SpaceCategoryFilter = "RESIDENTIAL",
            FormulaDescription = "TravelDistance(residential, single direction) <= 9m",
            CodeReference = "SCDF Fire Code 2023 Table 5.1",
            RegulationText = "Maximum travel distance in one direction for residential spaces (HDB/private): 9m (non-sprinklered), 18m (sprinklered).",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "SG-SCDF-TD-002", RuleName = "Travel Distance - Commercial/Office (Any Direction)",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Singapore,
            Agency = SgAgency.SCDF, IfcClassFilter = "IFCSPACE",
            CheckParameter = "TravelDistance", CheckUnit = "m", MaximumValue = 30,
            FormulaDescription = "TravelDistance(commercial/office) <= 30m (non-sprinklered), <= 60m (sprinklered)",
            CodeReference = "SCDF Fire Code 2023 Table 5.1",
            RegulationText = "Maximum travel distance for commercial and office occupancies: 30m (non-sprinklered), 60m (sprinklered). Travel distance measured along the natural path of travel.",
            FailSeverity = Severity.Error
        },

        // ── Exit Width ────────────────────────────────────────────────────────
        new() {
            RuleId = "SG-SCDF-EXIT-003", RuleName = "Exit Staircase - Minimum Width (Buildings > 24m)",
            Category = DesignCodeCategory.FireSafetyAndEmergency, Country = CountryMode.Singapore,
            Agency = SgAgency.SCDF, IfcClassFilter = "IFCSTAIR",
            CheckParameter = "EffectiveWidth", CheckUnit = "mm", MinimumValue = 1200,
            FormulaDescription = "ExitStair.EffectiveWidth >= 1200mm (buildings > 24m height)",
            CodeReference = "SCDF Fire Code 2023 §5.4.2",
            RegulationText = "For buildings above 24m in height, all exit staircases shall have a minimum effective width of 1,200mm.",
            FailSeverity = Severity.Critical
        },

        // ── Fire Door / Compartment Wall FRR ─────────────────────────────────
        new() {
            RuleId = "SG-URA-PLOT-001", RuleName = "Plot Ratio - Gross Floor Area Declaration",
            Category = DesignCodeCategory.UrbanPlanningAndGFA, Country = CountryMode.Singapore,
            Agency = SgAgency.URA, IfcClassFilter = "IFCSPACE",
            CheckParameter = "GrossArea", CheckUnit = "m²", MinimumValue = 0.01,
            FormulaDescription = "IfcSpace[AREA_GFA].GrossArea > 0",
            CodeReference = "URA Development Control Parameters 2024 - Plot Ratio",
            RegulationText = "Every IfcSpace with AREA_GFA subtype must have a GrossArea value greater than zero. URA sums all GrossArea values to verify total GFA against the approved Gross Plot Ratio.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "SG-URA-BALC-001", RuleName = "Balcony - Maximum Depth",
            Category = DesignCodeCategory.UrbanPlanningAndGFA, Country = CountryMode.Singapore,
            Agency = SgAgency.URA, IfcClassFilter = "IFCSPACE",
            SpaceCategoryFilter = "BALCONY",
            CheckParameter = "Depth", CheckUnit = "m", MaximumValue = 1.5,
            FormulaDescription = "Balcony.Depth <= 1.5m",
            CodeReference = "URA Balcony Incentive Scheme - Development Control Parameters",
            RegulationText = "Balconies shall not exceed 1.5m in depth (measured from the external face of the main wall). Balconies exceeding this depth will be computed as GFA.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // NEA - FULL VENTILATION AND ENVIRONMENTAL RULES
        // Reference: NEA Environmental Public Health Act + COP 3.1 §3 pp.52-63
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId = "SG-NEA-WELS-001", RuleName = "Water Closet - Minimum WELS Rating",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.NEA, IfcClassFilter = "IFCSANITARYTERMINAL",
            CheckParameter = "WELSRating", CheckUnit = "", MinimumValue = 3,
            FormulaDescription = "WaterCloset.WELSRating >= 3 ticks (dual flush <= 4.5L/3L)",
            CodeReference = "PUB WELS Requirements - SS 608-2:2020",
            RegulationText = "All water closets must achieve a minimum WELS rating of 3 ticks. Dual-flush cisterns must not exceed 4.5L (full flush) / 3L (half flush). Single-flush cisterns must not exceed 4.5L.",
            FailSeverity = Severity.Critical
        },
        new() {
            RuleId = "SG-NEA-WELS-002", RuleName = "Wash Basin Tap - Minimum WELS Rating",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.NEA, IfcClassFilter = "IFCSANITARYTERMINAL",
            CheckParameter = "WELSRating", CheckUnit = "", MinimumValue = 2,
            FormulaDescription = "WashBasin.WELSRating >= 2 ticks (<= 6 L/min flow rate)",
            CodeReference = "PUB WELS Requirements - SS 608-2:2020",
            RegulationText = "All wash basin taps and mixers shall achieve a minimum WELS rating of 2 ticks with maximum flow rate of 6 L/min at 3 bar pressure.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "SG-NEA-WELS-003", RuleName = "Shower - Minimum WELS Rating",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.NEA, IfcClassFilter = "IFCSANITARYTERMINAL",
            CheckParameter = "WELSRating", CheckUnit = "", MinimumValue = 2,
            FormulaDescription = "Shower.WELSRating >= 2 ticks (<= 9 L/min flow rate)",
            CodeReference = "PUB WELS Requirements - SS 608-2:2020",
            RegulationText = "All shower heads and fittings shall achieve a minimum WELS rating of 2 ticks with maximum flow rate of 9 L/min at 3 bar pressure.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // PUB - DRAINAGE AND SANITARY REQUIREMENTS
        // Reference: PUB Code of Practice on Sewerage 2019 + COP 3.1 §3 pp.68-72
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId = "SG-PUB-DRN-001", RuleName = "Foul Water Drain - Minimum Gradient",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.PUB, IfcClassFilter = "IFCPIPESEGMENT",
            CheckParameter = "Gradient", CheckUnit = "ratio", MinimumValue = 0.01,
            FormulaDescription = "FoulWaterDrain.Gradient >= 1:100 (0.01)",
            CodeReference = "PUB Code of Practice on Sewerage and Sanitary Works 2019 §5.3",
            RegulationText = "All foul water drains and soil pipes shall be laid at a minimum gradient of 1:100. Stormwater drains shall be at minimum 1:200. Self-cleansing velocity of 0.75 m/s shall be maintained.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "SG-PUB-DRN-002", RuleName = "Stormwater Drain - Minimum Gradient",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.PUB, IfcClassFilter = "IFCPIPESEGMENT",
            CheckParameter = "Gradient", CheckUnit = "ratio", MinimumValue = 0.005,
            FormulaDescription = "StormwaterDrain.Gradient >= 1:200 (0.005)",
            CodeReference = "PUB Code of Practice on Sewerage and Sanitary Works 2019 §6.2",
            RegulationText = "Stormwater drainage pipes shall be laid at minimum 1:200 gradient. Surface drains may be at 1:500 where self-cleansing cannot be achieved.",
            FailSeverity = Severity.Warning
        },

        // ══════════════════════════════════════════════════════════════════════
        // LTA - TRANSPORT AND PARKING REQUIREMENTS
        // Reference: LTA Code of Practice for Vehicle Parking 2019 + COP 3.1 §3
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId = "SG-LTA-LORRY-001", RuleName = "Lorry Lot - Minimum Dimensions",
            Category = DesignCodeCategory.TransportAndParking, Country = CountryMode.Singapore,
            Agency = SgAgency.LTA, IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            CheckParameter = "BayLength", CheckUnit = "mm", MinimumValue = 9000,
            FormulaDescription = "LorryLot.BayLength >= 9000mm",
            CodeReference = "LTA Code of Practice for Vehicle Parking Provision 2019 §3.6",
            RegulationText = "Lorry lots shall have minimum dimensions of 3,500mm width x 9,000mm length with minimum headroom of 4,500mm. Loading bay ramps shall not exceed 1:6 gradient.",
            FailSeverity = Severity.Error
        },

        // ══════════════════════════════════════════════════════════════════════
        // NPARKS - LUSH PROGRAMME AND LANDSCAPE REQUIREMENTS
        // Reference: NParks LUSH 3.0 Programme + COP 3.1 §3 pp.64-67
        // ══════════════════════════════════════════════════════════════════════

        new() {
            RuleId = "SG-SLA-LOT-001", RuleName = "Land Lot Number - Format Validation",
            Category = DesignCodeCategory.GeoReferencingAndSiteData, Country = CountryMode.Singapore,
            Agency = SgAgency.SLA, IfcClassFilter = "IFCSITE",
            CheckParameter = "LandLotNumber", CheckUnit = "",
            FormulaDescription = "Site.LandLotNumber matches SLA lot format (e.g. Lot 12345K MK10)",
            CodeReference = "SLA INLIS Land Register",
            RegulationText = "The land lot number must match the SLA land register format exactly: 'Lot [number][letter] MK[mukim number]' or 'Lot [number][letter] TS[town subdivision]'. The lot number is verified against SLA INLIS.",
            FailSeverity = Severity.Critical
        },

        // ══════════════════════════════════════════════════════════════════════
        // BCA GREEN MARK 2021 - ADDITIONAL THERMAL/ENERGY RULES
        // Reference: BCA Green Mark 2021 + COP 3.1 §3
        // ══════════════════════════════════════════════════════════════════════


        // ══════════════════════════════════════════════════════════════════════
        // LOWER PRIORITY - SS EN MATERIAL GRADES + PUB FITTING RATIOS
        // Source: SS EN 1992-1-1, SS EN 1993-1-1, PUB SSW Code 2019 COP 3.1
        // ══════════════════════════════════════════════════════════════════════

        // ── SS EN - Concrete Grade Approved Values ────────────────────────────
        new() {
            RuleId = "SG-BCA-STR-004",
            RuleName = "Structural Concrete - Minimum Grade C25/30 for Beams/Columns",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Singapore,
            Agency = SgAgency.BCA, IfcClassFilter = "IFCBEAM",
            CheckParameter = "MaterialGrade", CheckUnit = "",
            FormulaDescription = "BeamOrColumn.ConcreteGrade >= C25/30",
            CodeReference = "SS EN 1992-1-1:2008 §4.4.1 / BC 2:2021 §4.1",
            RegulationText = "Minimum concrete grade for exposed reinforced concrete beams and columns is C32/40. Concrete grade below C25/30 is not permitted for structural use. Approved grades: C25/30, C28/35, C30/37, C32/40, C35/45, C40/50, C45/55, C50/60.",
            FailSeverity = Severity.Error
        },
        new() {
            RuleId = "SG-BCA-STR-005",
            RuleName = "Structural Steel - Minimum Grade S275 for Primary Members",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Singapore,
            Agency = SgAgency.BCA, IfcClassFilter = "IFCBEAM",
            CheckParameter = "MaterialGrade", CheckUnit = "",
            FormulaDescription = "SteelBeam.Grade must be S275 or higher",
            CodeReference = "SS EN 1993-1-1:2010 §3.2 / BC 4:2017",
            RegulationText = "Structural steel primary members shall use minimum Grade S275 (formerly Grade 43) per SS EN 10025. Approved grades: S235, S275, S355, S420, S460. Non-standard grades require QP (Engineer) endorsement.",
            FailSeverity = Severity.Warning
        },

        // ── PUB - Sanitary Fitting Ratios ────────────────────────────────────
        new() {
            RuleId = "SG-PUB-SAN-002",
            RuleName = "Factory / Industrial - Minimum WC per 25 Workers",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.PUB, IfcClassFilter = "IFCSANITARYTERMINAL",
            CheckParameter = "SystemType", CheckUnit = "",
            FormulaDescription = "Industrial: 1 WC per 25 workers (male), 1 WC per 15 workers (female)",
            CodeReference = "PUB Sewerage and Drainage Act / Building Control Regs - First Schedule §9",
            RegulationText = "Factories and industrial buildings shall provide: minimum 1 WC and 1 urinal per 25 male workers; 1 WC per 15 female workers. Sanitary accommodation shall be within 100m horizontal travel of all workplaces.",
            FailSeverity = Severity.Warning
        },
        new() {
            RuleId = "SG-PUB-SAN-003",
            RuleName = "Assembly / Place of Worship - Minimum WC Ratio",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.PUB, IfcClassFilter = "IFCSANITARYTERMINAL",
            CheckParameter = "SystemType", CheckUnit = "",
            FormulaDescription = "Assembly: 1 WC per 100 males + 1 per 50 females; 1 urinal per 50 males",
            CodeReference = "Building Control Regulations - First Schedule §9.3",
            RegulationText = "Places of assembly, theatres, cinemas and places of worship shall provide: 1 WC and 1 urinal per 100 males; 1 WC per 50 females. Accessible toilets at minimum 1 per floor per gender.",
            FailSeverity = Severity.Warning
        },
        new() {
            RuleId = "SG-PUB-SAN-004",
            RuleName = "Urinal - WELS Rating Minimum 2 Ticks",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.PUB, IfcClassFilter = "IFCSANITARYTERMINAL",
            CheckParameter = "WELSRating", CheckUnit = "", MinimumValue = 2,
            FormulaDescription = "Urinal.WELSRating >= 2 ticks (<= 1L/flush)",
            CodeReference = "PUB WELS Requirements / SS 608-3",
            RegulationText = "Urinals shall achieve minimum 2-tick WELS rating with maximum flush volume of 1.0 litre per flush (or waterless/sensor-operated). Non-water urinals are also acceptable.",
            FailSeverity = Severity.Error
        },

        // ── LTA - Additional Parking Provision Rates ─────────────────────────
        new() {
            RuleId = "SG-LTA-PKG-006",
            RuleName = "Motorcycle Lot - Minimum Dimensions",
            Category = DesignCodeCategory.TransportAndParking, Country = CountryMode.Singapore,
            Agency = SgAgency.LTA, IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            CheckParameter = "BayWidth", CheckUnit = "mm", MinimumValue = 1000,
            FormulaDescription = "MotorcycleLot.Width >= 1000mm AND Length >= 2200mm",
            CodeReference = "LTA Code of Practice for Vehicle Parking Provision 2019 §3.3",
            RegulationText = "Motorcycle parking lots shall have minimum dimensions of 1,000mm width x 2,200mm length with minimum headroom of 2,100mm. Motorcycle lots must be separated from car parking areas.",
            FailSeverity = Severity.Warning
        },
        new() {
            RuleId = "SG-LTA-PKG-007",
            RuleName = "Coach / Bus Lot - Minimum Dimensions",
            Category = DesignCodeCategory.TransportAndParking, Country = CountryMode.Singapore,
            Agency = SgAgency.LTA, IfcClassFilter = "IFCBUILDINGELEMENTPROXY",
            CheckParameter = "BayLength", CheckUnit = "mm", MinimumValue = 12000,
            FormulaDescription = "CoachLot.Length >= 12000mm AND Width >= 3500mm",
            CodeReference = "LTA Code of Practice for Vehicle Parking Provision 2019 §3.7",
            RegulationText = "Coach and bus parking lots shall have minimum dimensions of 3,500mm width x 12,000mm length with minimum headroom of 4,500mm. Articulated vehicle lots require minimum length of 17,000mm.",
            FailSeverity = Severity.Warning
        },

        // ── BCA - Structural Submission Additional ────────────────────────────
        new() {
            RuleId = "SG-BCA-STR-006",
            RuleName = "Precast Concrete - Accreditation Required (MAS)",
            Category = DesignCodeCategory.StructuralAndFoundation, Country = CountryMode.Singapore,
            Agency = SgAgency.BCA, IfcClassFilter = "IFCSLAB",
            CheckParameter = "Accreditation_MAS", CheckUnit = "",
            FormulaDescription = "PrecastElement.Accreditation_MAS = TRUE for accredited precast plants",
            CodeReference = "BC 2:2021 §2.2 / BCA Precast Accreditation Scheme",
            RegulationText = "Precast concrete elements manufactured at BCA-accredited plants (MAS scheme) must be indicated with Accreditation_MAS = TRUE. Non-accredited plants require additional QP endorsement and plant inspection.",
            Gateway          = CorenetGateway.Piling,
            FailSeverity = Severity.Warning
        },

        // ── NEA - Pollution Control ───────────────────────────────────────────
        new() {
            RuleId = "SG-NEA-POLL-001",
            RuleName = "Industrial Interceptor - Capacity per NEA",
            Category = DesignCodeCategory.PlumbingAndDrainage, Country = CountryMode.Singapore,
            Agency = SgAgency.NEA, IfcClassFilter = "IFCINTERCEPTOR",
            CheckParameter = "Capacity", CheckUnit = "L", MinimumValue = 300,
            FormulaDescription = "GreaseTrap.Capacity >= 300L for commercial food premises",
            CodeReference = "NEA EPH (Food Establishments) Regulations §10 / SS 508",
            RegulationText = "Grease interceptors for commercial food establishments shall have minimum capacity of 300L (small) to 5,000L (large commercial kitchens). Capacity based on number of meal covers per day per NEA calculation method.",
            FailSeverity = Severity.Error
        },

        // ── SLA - Additional Georeferencing ──────────────────────────────────
        new() {
            RuleId = "SG-SLA-GEO-003",
            RuleName = "IFC Map Conversion - RefElevation (SHD) Required",
            Category = DesignCodeCategory.GeoReferencingAndSiteData, Country = CountryMode.Singapore,
            Agency = SgAgency.SLA, IfcClassFilter = "IFCSITE",
            CheckParameter = "RefElevation", CheckUnit = "m SHD", MinimumValue = -10,
            MaximumValue = 200,
            FormulaDescription = "IfcSite.RefElevation between -10m and 200m SHD (Singapore Height Datum)",
            CodeReference = "SLA INLIS / IFC+SG COP 3.1 Georeferencing Requirements",
            RegulationText = "The IfcSite RefElevation shall be expressed in Singapore Height Datum (SHD). Typical Singapore ground levels range from 0m to 30m SHD (Bukit Timah: ~164m). The project datum (0.000 in model) must be declared relative to SHD.",
            FailSeverity = Severity.Warning
        },


    ];

    /// <summary>
    /// Returns Singapore design rules applicable to a specific IFC class.
    /// </summary>
    public static List<DesignCodeRule> GetRulesForClass(string ifcClass)
    {
        var all = GetAllRules();
        return all.Where(r =>
            string.IsNullOrEmpty(r.IfcClassFilter) ||
            r.IfcClassFilter.Equals(ifcClass, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    /// <summary>
    /// Returns rules applicable to a specific space category.
    /// </summary>
    public static List<DesignCodeRule> GetRulesForSpaceCategory(string category)
    {
        var all = GetAllRules();
        return all.Where(r =>
            string.IsNullOrEmpty(r.SpaceCategoryFilter) ||
            r.SpaceCategoryFilter.Equals(category, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }
}
