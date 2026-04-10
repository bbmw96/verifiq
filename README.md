<div align="center">

<img src="https://img.shields.io/badge/VERIFIQ-IFC%20Compliance%20Checker-0B1F45?style=for-the-badge&labelColor=0E7C86" />

# VERIFIQ — IFC Compliance Checker

**Version 1.2.0 · Singapore CORENET-X + Malaysia NBeS · 100% Offline · BBMW0 Technologies**

[![License](https://img.shields.io/badge/Licence-Commercial-0B1F45)](https://bbmw0.com/verifiq/licence)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue)](https://github.com/bbmw96/verifiq)
[![.NET](https://img.shields.io/badge/.NET-8.0%20WPF-purple)](https://dotnet.microsoft.com/download/dotnet/8)
[![IFC+SG](https://img.shields.io/badge/IFC%2BSG-2025.1%20COP3-teal)](https://info.corenet.gov.sg)
[![NBeS](https://img.shields.io/badge/NBeS-2024.1%20CIDB-green)](https://nbes.cidb.gov.my)

[Download](#download) · [Features](#features) · [Screenshots](#screenshots) · [Licence Keys](#licence-keys) · [Documentation](#documentation)

</div>

---

## What is VERIFIQ?

VERIFIQ is a professional Windows desktop application for BIM practitioners who need to validate IFC models against Singapore CORENET-X (IFC+SG 2025) and Malaysia NBeS (UBBL 1984) regulatory requirements — **without an internet connection**.

Conceived by **Jia Wen Gan** of [Kyoob Architects Pte Ltd](https://kyoob.com.sg), Singapore, and developed by **BBMW0 Technologies**.

---

## Features

### ✅ 20-Level IFC Data Compliance Engine
Checks every IFC element against a systematic 20-level hierarchy:

| Level | Check |
|---|---|
| L1 | IFC Entity Class (flags IfcBuildingElementProxy with AI-suggested replacement) |
| L2–L3 | PredefinedType + ObjectType |
| L4–L5 | Classification Reference + Edition |
| L6–L8 | Mandatory Pset_, SGPset_ Property Sets, Property Values |
| L9–L10 | Data Types + Enumeration Values |
| L11–L14 | Spatial Containment, Storey Elevations, SVY21 Georeferencing, Site Hierarchy |
| L15–L18 | GUID Uniqueness, Materials, Space Boundaries, Geometry Validity |
| L19–L20 | IFC Schema Version, File Header |

### 🏛 8 Singapore Agencies — 89 Rules
BCA · URA · SCDF · LTA · NEA · PUB · NParks · SLA — all 5 CORENET-X gateways covered.

### 🇲🇾 Malaysia UBBL 1984 — All 9 Purpose Groups
Parts III–IX, MS 1184:2014, JBPM Fire Safety Requirements 2020, NBeS 2024 (CIDB).

### 📐 50+ Design Code Rules
- **URA**: Room minimum sizes (living room ≥13m², bedroom ≥9m², kitchen ≥4.5m²)
- **BCA Accessibility 2025**: Door widths, corridor widths, ramp gradients, lift car dimensions
- **SCDF Fire Code**: Compartment areas, travel distances, exit widths, stair widths
- **BCA Green Mark 2021**: ETTV/RETV, U-values, LPD, WWR
- **NEA**: Natural ventilation, mechanical ventilation rates (SS 553:2016)
- **LTA**: Parking quantum, bay dimensions, EV charging, cycling facilities
- **UBBL**: Ceiling heights, room areas, stair dimensions, FRR by Purpose Group

### 🧊 3D Viewer
Dual-engine WebGL viewer — web-ifc WASM geometry engine (primary) with C# parser fallback. Elements colour-coded by compliance status. Click any element to see its GUID, IFC class, storey, and all validation findings.

### 📤 8 Export Templates
Professional · Executive · BCA · SCDF · NBeS · Minimal · Technical · Audit  
Formats: Word (.docx) · PDF (.html) · Excel (.xlsx) · CSV · JSON · XML · Markdown · BCF

### 🔐 1,001 Licence Keys — Perpetual & Offline
All validation, 3D viewing, and export works without any internet connection.

---

## Screenshots

> *Coming soon — attach screenshots to this repository*

---

## Download

### Requirements
- Windows 10 (1903) or Windows 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8)
- [Microsoft WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (usually pre-installed on Windows 11)
- Visual Studio 2022+ (to build from source)

### Build from source

```bash
git clone https://github.com/bbmw96/verifiq.git
cd verifiq
```

Open `VERIFIQ.sln` in Visual Studio 2022, set `VERIFIQ.Desktop` as the Startup Project, and press **F5**.

Or from command line:

```batch
cd VERIFIQ\src\VERIFIQ.Desktop
dotnet build -c Release
dotnet run
```

---

## Licence Keys

VERIFIQ uses offline SHA-256 key verification — no server call is ever made.

| Tier | Users | Elements/run | Example Key |
|---|---|---|---|
| **Trial** | 1 | 10 | `VRFQ-TRIAL-DEMO0-0000-00000001` |
| **Individual** | 1 | Unlimited | `VRFQ-IND1-0001-0000-6C6A84BB` |
| **Practice** | 5 | Unlimited | `VRFQ-PRAC-0251-0000-F3E3C137` |
| **Enterprise** | 25 | Unlimited | `VRFQ-ENT1-0501-0000-9303B434` |
| **Site Licence** | Unlimited | Unlimited | `VRFQ-ENTX-0751-0000-AB60C977` |

To purchase: **bbmw0@hotmail.com** · [bbmw0.com](https://bbmw0.com)

---

## Architecture

```
VERIFIQ.sln
├── VERIFIQ.Core        — IFC data models, enums, shared types
├── VERIFIQ.Parser      — IFC STEP parser (Brep + ExtrudedAreaSolid + MappedItem geometry)
├── VERIFIQ.Rules       — Validation engine (20 levels) + Design code engine (50+ rules)
│   ├── Common/         — KnowledgeLibrary, EntityClassRules, SqliteRulesDatabase
│   ├── SG/             — SingaporeDesignRules (URA, BCA, SCDF, LTA, NEA, PUB)
│   └── MY/             — MalaysiaDesignRules (UBBL, MS 1184, JBPM)
├── VERIFIQ.Reports     — ReportGenerator (8 templates × 8 formats)
├── VERIFIQ.Security    — LicenceValidator (1,001 SHA-256 keys), IntegrityChecker
└── VERIFIQ.Desktop     — WPF + WebView2 shell
    └── wwwroot/        — Frontend (Three.js 3D viewer, compliance dashboard)
```

---

## Documentation

- [User Guide](https://bbmw0.com/verifiq/docs)
- [IFC+SG CORENET-X COP3](https://info.corenet.gov.sg)
- [NBeS CIDB](https://nbes.cidb.gov.my)
- [BCA Code on Accessibility 2025](https://www.bca.gov.sg)
- [SCDF Fire Code 2018](https://www.scdf.gov.sg)
- [BCA Green Mark 2021](https://www.bca.gov.sg/greenmark)

---

## Version History

| Version | Date | Highlights |
|---|---|---|
| **1.2.0** | Apr 2026 | 3D Viewer, 89 SG rules, health score A–F, agency chart, Top 5 Quick Fixes, live progress |
| **1.1.0** | Mar 2026 | KnowledgeLibrary, 120+ auto-classifier, element-specific remediation, 8 export templates |
| **1.0.0** | Jan 2026 | Initial release — Singapore + Malaysia, 20 check levels, 50+ design rules, 1,001 keys |

---

## About

**VERIFIQ** is conceived and founded by **Jia Wen Gan** (Kyoob Architects Pte Ltd, Singapore)  
**Developed by** BBMW0 Technologies · bbmw0@hotmail.com · [bbmw0.com](https://bbmw0.com)

© 2026 BBMW0 Technologies. All rights reserved.  
Unauthorised reproduction, redistribution or reverse engineering is prohibited.
