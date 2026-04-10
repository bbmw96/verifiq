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
