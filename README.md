# VERIFIQ v2.0.0
**IFC Compliance Checker for Singapore CORENET-X and Malaysia NBeS**

[![Version](https://img.shields.io/badge/version-2.0.0-00c4a0)](https://github.com/bbmw96/verifiq/releases/tag/v2.0.0)
[![Platform](https://img.shields.io/badge/platform-Windows-blue)](https://github.com/bbmw96/verifiq/releases)
[![Singapore](https://img.shields.io/badge/Singapore-COP%203.1%20Dec%202025-red)](https://go.gov.sg/ifcsg)
[![Malaysia](https://img.shields.io/badge/Malaysia-NBeS%202024-blue)](https://cidb.gov.my)
[![Licence](https://img.shields.io/badge/licence-Commercial-orange)](https://bbmw0.com)

VERIFIQ checks your IFC models against Singapore **CORENET-X** (IFC+SG COP 3.1, December 2025) and Malaysia **NBeS/UBBL 1984** regulations. It runs 20 compliance check levels on every element in your model and produces detailed, actionable findings with specific remediation guidance for Revit, ArchiCAD, Tekla, and Bentley.

**100% offline. Nothing leaves your machine.**

---

## Download

**[Download VERIFIQ-v2.0.0-Setup.exe](https://github.com/bbmw96/verifiq/releases/tag/v2.0.0)**

Windows 10 (64-bit) or later. WebView2 Runtime included in installer.

---

## Features

### Compliance Checking
- **20 Check Levels** IFC entity class, PredefinedType, ObjectType, classification reference, classification edition, mandatory Pset_, Singapore SGPset_, property values, data types, enumerations, spatial containment, storey elevations, georeferencing (SVY21 / GDM2000), site hierarchy, GUID uniqueness, material assignment, space boundary, geometry validity, IFC schema version, file header completeness
- **Singapore CORENET-X** IFC+SG COP 3.1 (December 2025), 196 classification codes, 8 regulatory agencies (BCA, SCDF, URA, NEA, PUB, SLA, LTA, JTC), 4 submission gateways (G1 Outline, G1.5 Piling, G2 Structural, G3 Construction)
- **Malaysia NBeS** NBeS IFC Mapping 2024 (CIDB 2nd Edition), UBBL 1984, 8 Purpose Groups, MS 1184:2014 accessibility, MS 1525:2019 thermal
- **Design Code Engine** URA room dimension checks, BCA accessibility (door widths, ramp gradients), SCDF travel distances and exit widths, NEA ventilation rates, PUB sanitary fitting ratios, BCA Green Mark thermal values

### Results and Findings
- Compliance score with colour-coded severity (Critical / Error / Warning / Pass)
- 7 filter dimensions: Severity, Discipline, IFC Entity, Agency, Storey, Gateway, Check Type
- Every finding references the exact property set, property name, actual value, required value, and the regulation clause
- Platform Guide fix instructions for Revit, ArchiCAD, Tekla, and Bentley per finding

### Fix and Export
- **Property Editor** fix missing or wrong property values without returning to BIM software. Writes a corrected IFC alongside the original
- **Export Reports** Word (.docx), Excel (.xlsx), PDF, CSV, JSON, HTML, Markdown, BCF across 8 templates (Professional, Executive Summary, BCA Submission, SCDF Submission, NBeS Submission, Technical, Audit, Minimal)
- **COBie Export** Facility, Floor, Space, Component, Type, Attribute, Document data

### 3D Viewer
- Compliance colour overlay (red = Critical, orange = Error, yellow = Warning, green = Pass)
- First-person walk mode (WASD + mouse look)
- Section planes (X / Y / Z)
- Measurement tools
- Colour modes: Compliance / IFC Type / Storey / Discipline
- Storey and discipline filters
- Element selection with properties panel

### Tools
- **IDS Checker** validate against Information Delivery Specification files (ISO 21597)
- **IFC Merge** federate multiple discipline models for combined validation
- **Search and Select** search elements by name, GUID, IFC class, classification code, or storey
- **Import Mapping** import latest BCA IFC+SG Industry Mapping Excel from go.gov.sg/ifcsg

### Auto-Update System
- Daily check against GitHub Releases and bbmw0.com/verifiq/version.json
- Silent background download with progress bar in the update banner
- Auto Install runs installer with admin elevation, closes VERIFIQ automatically
- Defer to Close installer runs automatically on next app exit
- Mandatory update flag for organisation deployments

---

## Supported File Formats

| Format | Description |
|--------|-------------|
| `.ifc` | IFC STEP Physical File (IFC2x3 and IFC4) |
| `.ifczip` | Compressed IFC |
| `.ifcxml` | IFC XML encoding |
| `.ifc+sg` | IFC+SG extended format |

---

## System Requirements

| Component | Requirement |
|-----------|-------------|
| Operating System | Windows 10 (version 1903) or later, 64-bit |
| WebView2 Runtime | Included in installer |
| RAM | 4 GB minimum, 8 GB recommended for large models |
| Storage | 200 MB for installation |
| Internet | Not required. Optional for update checks only. |

---

## Licence Tiers

| Tier | Devices | Elements/run | Price |
|------|---------|-------------|-------|
| Trial | 1 | 10 | Free |
| Individual | 1 | Unlimited | Contact |
| Practice | 5 | Unlimited | Contact |
| Enterprise | 25 | Unlimited | Contact |
| Unlimited (Site) | All | Unlimited | Contact |

All paid tiers are perpetual (never expire) and include both Singapore and Malaysia modes.

Licence key format: `VRFQ-XXXX-XXXX-XXXX-XXXX`

To purchase: **bbmw0@hotmail.com** | **bbmw0.com**

---

## Build from Source

### Prerequisites
- Visual Studio 2022 (or later)
- .NET 8 SDK
- Microsoft WebView2 Runtime
- Inno Setup 6 (for building the installer)

### Steps
```
1. Clone this repository
2. Open VERIFIQ.sln in Visual Studio 2022
3. Right-click Solution → Restore NuGet Packages
4. Set VERIFIQ.Desktop as the startup project
5. Build → Rebuild Solution (x64, Windows)
6. Run (F5)
```

### Project Structure
```
VERIFIQ.sln
├── src/
│   ├── VERIFIQ.Core/          Data contracts, enumerations, IFC models
│   ├── VERIFIQ.Parser/        IFC STEP parser (IFC2x3 + IFC4), file format dispatcher
│   ├── VERIFIQ.Rules/         20-level validation engine, Design Code engine, rules database
│   │   ├── Common/            Classification codes, property rules, SQLite database
│   │   ├── SG/                Singapore design code rules (URA, BCA, SCDF, NEA, PUB)
│   │   └── MY/                Malaysia design code rules (UBBL, JBPM, MS 1184, MS 1525)
│   ├── VERIFIQ.Reports/       Report builders (Word, Excel, PDF, CSV, JSON, HTML, BCF, COBie)
│   ├── VERIFIQ.Security/      Licence validation, hardware fingerprint
│   └── VERIFIQ.Desktop/       WPF + WebView2 shell, JS frontend (21 pages)
│       └── wwwroot/
│           ├── js/app.js      Main application router (2758 lines)
│           ├── js/ui/         Page modules (dashboard, results, viewer3d, files)
│           └── js/modules/    Bridge, state, utilities
└── VERIFIQ-Setup.iss          Inno Setup installer script
```

---

## References

- [CORENET-X Portal](https://portal.corenet.gov.sg)
- [IFC+SG Downloads (go.gov.sg/ifcsg)](https://go.gov.sg/ifcsg)
- [IFC+SG Excel Mapping File](https://info.corenet.gov.sg/ifc-sg/templates--apps-and-more/ifc-sg-excel-mapping-file)
- [COP 3.1 Documentation](https://go.gov.sg/cxcop)
- [CORENET-X Info](https://go.gov.sg/cx)
- [NBeS Malaysia (CIDB)](https://cidb.gov.my)
- [buildingSMART IDS](https://github.com/buildingSMART/IDS)

---

## Publisher

**BBMW0 Technologies**
Developer: Jia Wen Gan
Email: bbmw0@hotmail.com
Website: bbmw0.com
GitHub: github.com/bbmw96

Copyright 2026 BBMW0 Technologies. All rights reserved.
