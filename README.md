<div align="center">

<img src="https://img.shields.io/badge/VERIFIQ-v2.1.0-007B6E?style=for-the-badge&labelColor=061221" />
<img src="https://img.shields.io/badge/CORENET--X-COP%203.1%20Dec%202025-007B6E?style=flat-square&labelColor=061221" />
<img src="https://img.shields.io/badge/Malaysia-NBeS%202024-007B6E?style=flat-square&labelColor=061221" />
<img src="https://img.shields.io/badge/Platform-Windows%2010%2F11-0078D4?style=flat-square" />
<img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square" />
<img src="https://img.shields.io/badge/Licence-Commercial-C9A84C?style=flat-square" />

<br/><br/>

# VERIFIQ IFC Compliance Checker

**The professional IFC model validation tool for Singapore CORENET-X and Malaysia NBeS submissions.**

[**Download v2.1.0**](https://github.com/bbmw96/verifiq/releases/latest) · [**Website**](https://verifiq.bbmw0.com) · [**BBMW0 Technologies**](https://bbmw0.com)

</div>

---

## What VERIFIQ Does

VERIFIQ answers two questions before every BIM submission:

| Question | What it checks |
|----------|----------------|
| **Is the IFC model complete?** | Every element has the correct entity class, classification code, all mandatory Pset_ and SGPset_ properties, and correct values   checked against all 9 Singapore regulatory agencies simultaneously |
| **Is the classification chain complete?** | When a classification code is present, all linked SGPset_ property sets and required values are also present 196 COP 3.1 codes, per agency, per gateway |

VERIFIQ runs **20 sequential check levels** on every element and produces findings with specific regulation clause references and remediation guidance. It is 100% offline your IFC files never leave your machine.

---

## What's New in v2.1.0

- **Full CORENET-X COP 3.1 (December 2025)** implementation all 62 Identified Components, 970 property rules, 196 classification codes
- **192 Singapore design code rules** across all 9 agencies: SCDF Fire Code 2023, Code on Accessibility 2025, URA GFA Handbook 2024, BCA Green Mark 2021, PUB SSW Code 2019, LTA Parking Code 2019, NParks LUSH 3.0, JTC Guidelines, SLA SVY21
- **52 Malaysia design code rules** UBBL 1984, MS 1184:2014, JBPM Fire Safety 2020, GBI, MSMA 2nd Edition, CIDB NBeS 2024
- **Full GFA category enumeration** all 25 `AGF_DevelopmentUse` categories, full `AGF_Name` lists per development use, all 10 bonus GFA scheme types
- **JTC industrial rules** floor loading, ceiling height, loading bay provision
- **G4 Completion gateway rules** as-built Mark, structural completion, agency clearances
- **IFC+SG rules auto-update engine** checks `info.corenet.gov.sg` and `go.gov.sg/ifcsg` daily
- **Malaysia NBeS 2024 expanded** 21 MY classification codes, full structural property coverage, GDM2000 georeferencing checks
- **Modelling guidance** COP 3.1 Section 4 modelling notes for all 17 key component types, accessible from every finding

---

## Embedded Rules Database

| Item | Count | Source |
|------|-------|--------|
| Classification codes | 206 (SG: 117, CX: 68, MY: 21) | COP 3.1 + NBeS 2024 |
| IFC+SG property rules | 970 | COP 3.1 Section 4   all 62 Identified Components |
| IFC entity types | 39 with 122 subtypes | COP 3.1 |
| Singapore design rules | 192 | SCDF / URA / BCA / NEA / PUB / SLA / LTA / NParks / JTC |
| Malaysia design rules | 52 | UBBL / MS 1184 / JBPM / GBI / MSMA / CIDB NBeS |
| GFA development-use categories | 25 | URA GFA Handbook 2024 |
| Bonus GFA scheme types | 10 | COP 3.1 p.362 |
| Building typology values | 29 | COP 3.1 p.362 |
| Auto-update sources | 3 | info.corenet.gov.sg + go.gov.sg/ifcsg + bbmw0.com |

---

## The 20 Check Levels

| Level | Name | Severity | What it checks |
|-------|------|----------|----------------|
| L1 | IFC Entity Class | Critical | IFC class against COP 3.1 approved class for classification code |
| L2 | GUID Uniqueness | Critical | Every GlobalId unique within and across all loaded files |
| L3 | Spatial Containment | Critical | Every element contained within an IfcBuildingStorey |
| L4 | Classification Reference | Critical/Error | Classification code present, correct system |
| L5 | Classification Edition | Error | Edition matches current approved mapping |
| L6 | Mandatory Pset_ | Critical/Error | All standard IFC4 property sets present |
| L7 | SGPset_ / NBeS Pset_ | Critical | All Singapore or Malaysia property sets present |
| L8 | Classification-to-Property Chain | Critical/Error | All SGPset_ linked to the code are present   196 codes |
| L9 | Property Values | Error | Required values populated (not blank/null) |
| L10 | Enumeration Values | Error | Text properties contain only approved enumeration values |
| L11 | Data Types | Error | Values match declared data types |
| L12 | Georeferencing | Critical | IfcMapConversion with SVY21 (SG) or GDM2000 (MY) within national bounds |
| L13 | Coordinate Reference System | Warning | CRS matches SVY21 or GDM2000/RSO |
| L14 | Geometry Validity | Warning | Bounding box checked for zero-extent, NaN, or infinite values |
| L15 | Storey Elevations | Warning | IfcBuildingStorey elevations non-zero and ascending |
| L16 | IDS Compliance | Variable | Elements checked against loaded IDS specification |
| L17 | BCF Cross-Reference | Info | BCF issues linked to referenced elements |
| L18 | Design Code | Variable | 192 SG + 52 MY rules dimensions, fire ratings, WELS, accessibility, parking |
| L19 | IFC Schema Version | Error | IFC4 ADD2 TC1 or later required for CORENET-X |
| L20 | Model Quality | Warning | COP 3.1 Model Quality Checklist GFA consistency, space adjacency, cadastral lots |

---

## Singapore Coverage   9 Agencies

| Agency | What VERIFIQ checks |
|--------|---------------------|
| **BCA** | Mark, MaterialGrade, ConstructionMethod on structural elements; Code on Accessibility 2025 (doors, ramps, lifts, corridors, toilets); Green Mark U-values (wall ≤0.5, roof ≤0.4, window ≤3.5 W/m²K); full IFC+SG data completeness |
| **URA** | `AGF_DevelopmentUse` (25 approved categories, mandatory on all IfcSpace/AREA_GFA); `AVF_IncludeAsGFA` (mandatory); `GrossArea`; room size minimums; balcony depth max 1.5m |
| **SCDF** | `FireExit` on exit doors; `FireAccessOpening` on windows; `SpaceName` + `OccupancyType` on spaces; fire engine accessway ≥4000mm; compartment areas; travel distances; exit staircase widths; FRR on walls, floors, doors |
| **NEA** | `AirChangeRate` (offices/car parks min 6 ACH, kitchens min 20 ACH); bin centre area; grease interceptor capacity |
| **PUB** | `WELSRating` (WC min 3 ticks, basins/showers/urinals min 2 ticks); drain `Gradient` (foul 1:100, stormwater 1:200); pipe `InvertLevel`; `SystemType` |
| **SLA** | SVY21 Easting 2,667–49,001m; Northing 12,727–55,796m; `RefElevation` (SHD); `LandLotNumber` format matching SLA land register |
| **LTA** | Car bay 2400×4800mm; PWD bay min 3600mm; lorry lot min 9000mm; motorcycle lot min 1000×2200mm; coach lot min 12000mm |
| **NParks** | Botanical `PlantSpecies` names (NParks Flora & Fauna Web); transplanted tree `GirthSize` min 150mm; soil depth min 600mm; LUSH 3.0 `ALS_GreeneryFeatures` |
| **JTC** | Industrial slab `ImposedLoad` min 10 kN/m² (B2); factory clear height min 5m; loading bay provision |

---

## Malaysia Coverage   NBeS 2024

| Code | VERIFIQ checks |
|------|----------------|
| **UBBL 1984** | Room areas, ceiling heights, window/ventilation ratios, fire escape widths and travel distances, structural minimums |
| **MS 1184:2014** | OKU ramp ≤1:12, door ≥800mm clear, corridor ≥1500mm, lift 1100×1400mm, accessible toilet 1600×2000mm |
| **JBPM Fire Safety 2020** | FRR min 1hr, fire door FRR min 1hr, exit door ≥850mm, hydrant within 90m, Bomba access road ≥4500mm |
| **CIDB NBeS 2024** | Mark on all structural elements, MaterialGrade, ConstructionMethod (incl. IBS), GDM2000 georeferencing |
| **GBI** | Wall U-value ≤2.0, roof ≤0.4, window ≤4.0 W/m²K |
| **MSMA 2nd Ed.** | Stormwater drain gradient min 1:333, OSD tank for sites >1 ha |

---

## System Requirements

| Requirement | Specification |
|-------------|---------------|
| Operating System | Windows 10 (version 1903 or later) or Windows 11, 64-bit |
| .NET Runtime | .NET 8 Desktop Runtime (installed automatically) |
| WebView2 Runtime | Microsoft Edge WebView2 (installed automatically) |
| RAM | 4 GB minimum; 8 GB recommended for models >200 MB |
| Disk | 250 MB installation |
| Internet | Not required for validation   optional for rules auto-update |

---

## Licence Tiers

| Tier | Price | Elements |
|------|-------|----------|
| Trial | Free | First 10 elements per run |
| Individual | £299 one-time | Unlimited |
| Practice | On request | Unlimited (5 users) |
| Enterprise | On request | Unlimited (25 users) |
| Unlimited | On request | Site licence |

Trial key: `VRFQ-TRIAL-DEMO0-0000-00000001`

---

## References

- **CORENET-X COP 3.1** (December 2025): [go.gov.sg/cxcop](https://go.gov.sg/cxcop)
- **IFC+SG Industry Mapping & Resource Kit**: [go.gov.sg/ifcsg](https://go.gov.sg/ifcsg)
- **CORENET-X Portal**: [info.corenet.gov.sg](https://info.corenet.gov.sg)
- **VERIFIQ Website**: [verifiq.bbmw0.com](https://verifiq.bbmw0.com)
- **BBMW0 Technologies**: [bbmw0.com](https://bbmw0.com)

---

## Contact & Support

**BBMW0 Technologies**
- Email: bbmw0@hotmail.com
- Phone / WhatsApp: +44 7920 212 969
- Website: [bbmw0.com](https://bbmw0.com)
- VERIFIQ: [verifiq.bbmw0.com](https://verifiq.bbmw0.com)

---

<div align="center">
<sub>VERIFIQ is not affiliated with BCA, GovTech, CORENET-X, URA, SCDF, NEA, PUB, SLA, LTA, NParks, JTC, CIDB, JBPM, or any Singapore or Malaysia government agency. CORENET-X and NBeS are trademarks of their respective owners. All regulatory values are cited by clause reference only   full regulation text belongs to the issuing authorities.</sub>
</div>
