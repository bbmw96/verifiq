// VERIFIQ - Main Application
// Copyright 2026 BBMW0 Technologies. All rights reserved.

'use strict';

const App = (() => {
  const container = () => document.getElementById('page-container');

  // Page registry - every sidebar nav button must have an entry here
  const pages = {
    dashboard:   () => DashboardPage.render(),
    files:       () => FilesPage.render(),
    validation:  () => renderValidationPage(),
    results:     () => ResultsPage.render(),
    critical:    () => ResultsPage.renderCritical(),
    design:      () => ResultsPage.renderDesignCode(),
    '3d':        () => Viewer3DPage.render(),
    export:      () => ExportPage.render(),
    settings:    () => SettingsPage.render(),
    licence:     () => renderLicencePage(),
    rules:       () => renderRulesPage(),
    about:       () => AboutPage.render(),
    help:        () => renderHelpPage(),
    userguide:      () => renderUserGuidePage(),
    propertyeditor: () => renderPropertyEditorPage(),
  };

  // Rules page tab switcher
  const VRules = {
    showTab(idx) {
      for (let i=0; i<4; i++) {
        const tab = document.getElementById(`rtab-${i}`);
        const content = document.getElementById(`rtab-content-${i}`);
        if (tab) tab.style.cssText += `;color:${i===idx?'var(--teal)':'var(--mid-grey)'};border-bottom:2px solid ${i===idx?'var(--teal)':'transparent'}`;
        if (content) content.style.display = i===idx?'block':'none';
      }
      // Activate first tab styling on load
    }
  };
  window.VRules = VRules;

  function navigate(page) {
    VState.set({ currentPage: page });
    render(page);

    // Notify C# shell to update the sidebar highlight
    VBridge.send('navigateTo', { page });
  }

  function render(page) {
    const fn = pages[page] || pages.dashboard;
    const el = container();
    if (!el) return;
    try {
      const html = fn();
      if (html) {
        el.innerHTML = html;
      } else {
        el.innerHTML = `<div style="padding:40px;color:#64748B;text-align:center">
          <div style="font-size:40px;margin-bottom:12px">📄</div>
          <div style="font-size:16px">Page content unavailable</div></div>`;
      }
      // Post-render hooks for pages that need DOM initialisation
      if (page === '3d' && window.Viewer3DPage) {
        Viewer3DPage.onNavigate();
      }
    } catch (err) {
      console.error('[VERIFIQ] render error on page "' + page + '":', err);
      el.innerHTML = `<div style="padding:32px">
        <div style="background:#FEF2F2;border:1px solid #FECACA;border-radius:8px;padding:20px">
          <div style="font-weight:700;color:#B91C1C;margin-bottom:8px">⚠ Page render error - ${page}</div>
          <div style="font-family:monospace;font-size:12px;color:#7F1D1D">${err && err.message ? err.message : String(err)}</div>
          <button class="btn btn-outline" style="margin-top:12px" onclick="App.navigate('dashboard')">← Back to Dashboard</button>
        </div></div>`;
    }
  }

  function refresh() {
    render(VState.get('currentPage') || 'dashboard');
  }

  function init() {
    // Parse ?page= from URL
    const params = new URLSearchParams(window.location.search);
    const page   = params.get('page') || 'dashboard';

    // Initialise the bridge (sets up WebView2 message listener)
    VBridge.init();

    // Update banner: shown at the top of the page when C# finds a newer version.
    window._showUpdateBanner = (info) => {
      const el = document.getElementById('update-banner');
      if (!el) return;
      el.innerHTML = `
        <div style="background:#FEF3C7;border-bottom:2px solid #F59E0B;padding:8px 20px;
          display:flex;align-items:center;gap:12px;font-family:Arial;font-size:12px">
          <span style="font-size:16px">⬆</span>
          <span><strong>VERIFIQ ${VUtils.esc(info.latest)} is available</strong>
            (you have ${VUtils.esc(info.current)}).
            ${info.notes ? VUtils.esc(info.notes) + ' ' : ''}
            <a href="${VUtils.esc(info.url)}" target="_blank"
               style="color:#92400E;font-weight:700">Download →</a>
          </span>
          <button onclick="document.getElementById('update-banner').innerHTML=''"
            style="margin-left:auto;background:none;border:none;cursor:pointer;
                   font-size:18px;color:#92400E;padding:0 4px">✕</button>
        </div>`;
    };

    // Subscribe to state changes that affect the current page
    VState.subscribe('*', () => refresh());

    // Initial render
    render(page);
  }

  // ── LICENCE PAGE ─────────────────────────────────────────────────────────

  function renderLicencePage() {
    const state = VState.get();
    const tier  = state.licence || 'Trial';
    const isTrial = tier.toLowerCase().includes('trial');

    window._licenceErrorCallback = (msg) => {
      const el = document.getElementById('licence-error');
      if (el) { el.textContent = msg; el.style.display = 'block'; }
    };

    return `
      <div>
        <h1>Licence Management</h1>

        <div class="card">
          <div class="card-header"><span class="card-title">Current Licence</span></div>
          <div class="detail-panel">
            <div class="detail-row">
              <span class="detail-label">Tier</span>
              <span class="detail-value" style="font-weight:700;color:var(--teal)">${VUtils.esc(tier)}</span>
            </div>
            <div class="detail-row">
              <span class="detail-label">Country Coverage</span>
              <span class="detail-value">Singapore and Malaysia - all tiers include both countries</span>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-header"><span class="card-title">🔑 ${isTrial ? 'Activate Full Licence' : 'Change Licence Key'}</span></div>
          <p style="font-size:13px;margin-bottom:14px">
            ${isTrial
              ? 'You are on Trial mode (10 elements per run). Enter your licence key to unlock all features.'
              : 'Enter a new licence key to upgrade or change your licence tier.'}
          </p>
          <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap">
            <input id="licence-key-input" type="text"
              placeholder="VRFQ-XXXX-XXXX-XXXX-XXXX"
              style="font-family:Courier New,monospace;font-size:14px;padding:10px 14px;
                     border:2px solid var(--border);border-radius:6px;width:310px;
                     background:white;color:var(--black)"
              oninput="document.getElementById('licence-error').style.display='none'"
              maxlength="29"/>
            <button class="btn btn-teal" style="padding:10px 22px;font-size:14px"
              onclick="(function(){
                var k=document.getElementById('licence-key-input').value.trim();
                if(!k)return;
                VBridge.send('activateLicence',{key:k});
              })()">Activate &rarr;</button>
          </div>
          <div id="licence-error" style="display:none;margin-top:10px;color:#B91C1C;
            font-size:13px;background:#FEF2F2;padding:8px 12px;border-radius:5px;
            border:1px solid #FECACA"></div>
          <p style="margin-top:12px;font-size:12px;color:var(--mid-grey)">
            To purchase a licence, contact: <strong>bbmw0@hotmail.com</strong> | <strong>bbmw0.com</strong>
          </p>
        </div>

        <div class="card">
          <div class="card-header"><span class="card-title">Licence Tiers</span></div>
          <div class="table-wrap">
            <table>
              <thead><tr>
                <th>Tier</th><th>Devices</th><th>Elements per run</th><th>Countries</th><th>Notes</th>
              </tr></thead>
              <tbody>
                <tr>
                  <td><span class="badge badge-info">Trial</span></td>
                  <td>1</td><td>10</td><td>SG + MY</td>
                  <td>All 20 checks, all export formats</td></tr>
                <tr>
                  <td><span class="badge badge-pass">Individual</span></td>
                  <td>1</td><td>Unlimited</td><td>SG + MY</td>
                  <td>Full features, perpetual</td></tr>
                <tr>
                  <td><span class="badge badge-pass">Practice</span></td>
                  <td>5</td><td>Unlimited</td><td>SG + MY</td>
                  <td>Full features, perpetual</td></tr>
                <tr>
                  <td><span class="badge badge-pass">Enterprise</span></td>
                  <td>25</td><td>Unlimited</td><td>SG + MY</td>
                  <td>IT deployment to 25 workstations, perpetual</td></tr>
                <tr>
                  <td><span class="badge badge-pass">Unlimited</span></td>
                  <td>Site (all)</td><td>Unlimited</td><td>SG + MY</td>
                  <td>Site licence - deploy to entire organisation, perpetual</td></tr>
              </tbody>
            </table>
          </div>
          <p style="margin-top:10px;font-size:12px;color:var(--mid-grey)">
            All paid tiers are perpetual (they never expire).
            Format: <code>VRFQ-XXXX-XXXX-XXXX-XXXX</code>
          </p>
        </div>
      </div>`;
  }
  // ── RULES PAGE ────────────────────────────────────────────────────────────

  function renderRulesPage() {
    const state = VState.get();
    const mode  = state.countryMode || 'Singapore';
    const isSG  = mode !== 'Malaysia';
    const isMY  = mode !== 'Singapore';

    return `<div>
      <h1>Rules Database</h1>
      <p style="color:var(--mid-grey);font-size:13px;margin-bottom:12px">
        <strong>128 IFC+SG classification codes</strong> embedded - 64 Architectural, 28 Structural,
        18 M&amp;E, 10 Plumbing, 4 Civil, 4 Landscape, plus full Malaysia NBeS codes.
        Each code is mapped to its exact required SGPset_ property sets and properties.
        Import the official BCA Industry Mapping Excel below to add or update codes.
      </p>

      <div class="card" style="margin-bottom:16px;border-left:4px solid var(--teal)">
        <div class="card-header">
          <span class="card-title">📥 Import BCA Industry Mapping Excel</span>
        </div>
        <p style="font-size:12px;color:var(--mid-grey);margin-bottom:10px">
          Download the official <strong>IFC+SG Industry Mapping 2025 (COP3)</strong> Excel from
          <a href="#" onclick="VBridge.send('openUrl',{url:'https://info.corenet.gov.sg'})" style="color:var(--teal)">
            info.corenet.gov.sg
          </a>
          and import it here to ensure VERIFIQ uses the latest official code-to-property mappings.
        </p>
        <div style="display:flex;gap:10px;align-items:center;flex-wrap:wrap">
          <button class="btn btn-teal" onclick="RulesDbPage.browseAndImport()" style="font-size:13px">
            📂 Browse &amp; Import Excel
          </button>
          <div style="font-size:11px;color:var(--mid-grey)">
            Accepted: IFC+SG Industry Mapping 2025 (COP3), COP2, or NBeS Industry Mapping Excel
          </div>
        </div>
        <div id="industry-mapping-result" style="margin-top:8px"></div>
      </div>

      <!-- Tab bar -->
      <div style="display:flex;gap:6px;margin-bottom:16px;border-bottom:2px solid var(--border);padding-bottom:0">
        ${['Singapore','Malaysia','Design Code','Check Levels'].map((tab,i) =>
          `<button onclick="VRules.showTab(${i})" id="rtab-${i}"
            style="padding:8px 16px;border:none;background:none;cursor:pointer;font-family:inherit;
                   font-size:13px;font-weight:600;color:var(--mid-grey);border-bottom:2px solid transparent;
                   margin-bottom:-2px;transition:all .15s" class="rules-tab">
            ${tab}
          </button>`).join('')}
      </div>

      <!-- Singapore tab -->
      <div id="rtab-content-0">
        <div class="two-col">
          <div class="card">
            <div class="card-header"><span class="card-title">🇸🇬 CORENET-X IFC+SG 2025</span></div>
            <div style="font-size:12px;line-height:1.8">
              <b>Standard:</b> IFC+SG Industry Mapping 2025 (COP 3rd Edition, October 2025)<br>
              <b>Schema:</b> IFC4 Reference View ADD2 TC1 (IFCXML/IFC/IFCZIP)<br>
              <b>Coordinate:</b> SVY21 (EPSG:3414) - mandatory IfcMapConversion<br>
              <b>Agencies:</b> BCA · URA · LTA · NEA · NParks · PUB · SCDF · SLA<br>
              <b>Gateways:</b> Design · Piling · Construction · Completion · DSP
            </div>
          </div>
          <div class="card">
            <div class="card-header"><span class="card-title">📋 Legislation</span></div>
            <div style="font-size:12px;line-height:1.8">
              Building Control Act (Cap 29) · Planning Act<br>
              Fire Safety Act · Environmental Public Health Act<br>
              Code on Accessibility 2025 (BCA)<br>
              BCA Green Mark 2021<br>
              BC 2:2021 · SS EN 1992/1993 · SS 553:2016<br>
              Land Surveyors Act · PUB SDWA
            </div>
          </div>
        </div>
        ${renderAgencyRules()}
      </div>

      <!-- Malaysia tab -->
      <div id="rtab-content-1" style="display:none">
        <div class="two-col">
          <div class="card">
            <div class="card-header"><span class="card-title">🇲🇾 NBeS / UBBL 1984</span></div>
            <div style="font-size:12px;line-height:1.8">
              <b>Standard:</b> NBeS IFC Mapping 2024 (CIDB, 2nd Edition)<br>
              <b>Schema:</b> IFC4 Reference View ADD2 TC1<br>
              <b>Coordinate:</b> GDM2000 (per-state projection) - recommended<br>
              <b>Purpose Groups:</b> PG I–IX per UBBL 1984 Third Schedule<br>
              <b>Agencies:</b> JBPM · CIDB · JKR · Local Authority (PBT)
            </div>
          </div>
          <div class="card">
            <div class="card-header"><span class="card-title">📋 Legislation</span></div>
            <div style="font-size:12px;line-height:1.8">
              Street, Drainage and Building Act 1974 (Act 133)<br>
              Uniform Building By-Laws 1984 (Parts I–IX)<br>
              MS 1184:2014 - Access for Disabled People<br>
              MS 1183:2007 · MS 1525:2019<br>
              JBPM Fire Safety Requirements 2020<br>
              Fire Services Act 1988 · Registration of Engineers Act 1967
            </div>
          </div>
        </div>
        ${renderUbblTable()}
      </div>

      <!-- Design Code tab -->
      <div id="rtab-content-2" style="display:none">
        ${renderDesignCodeRules()}
      </div>

      <!-- Check Levels tab -->
      <div id="rtab-content-3" style="display:none">
        ${renderCheckLevels()}
      </div>
    </div>`;
  }

  function renderAgencyRules() {
    const agencies = [
      {id:'BCA',  name:'Building and Construction Authority',
       rules:['Structural adequacy (BC 2:2021 / SS EN 1992)', 'Code on Accessibility 2025 - accessible routes', 'BCA Green Mark 2021 - ETTV/RETV/LPD/WWR', 'Building Control Act - structural submission', 'Foundation - piling gateway requirements']},
      {id:'URA',  name:'Urban Redevelopment Authority',
       rules:['GFA computation from IfcSpace.GrossPlannedArea', 'Plot ratio compliance (Master Plan 2019)', 'Balcony ≤ 10% of unit GFA', 'Setback distances (road reserve categories)', 'Space category enumeration (50+ permitted values)']},
      {id:'SCDF', name:'Singapore Civil Defence Force',
       rules:['Fire compartment size (7,000m² sprinklered / 3,500m² non-sprinklered)', 'Travel distance (60m / 30m)', 'Exit widths - ≥750mm / 1,050mm (60+ occupants)', 'Escape stair widths - 1,100mm / 1,200mm (high-rise)', 'Fire resistance ratings (FRR) per SCDF Table 4.2']},
      {id:'LTA',  name:'Land Transport Authority',
       rules:['Parking quantum per use type', 'Standard bay 2.5m × 5.0m', 'Accessible bay 3.6m × 5.0m', 'Loading/unloading bay 3.5m × 12m × 4.2m clear height']},
      {id:'NEA',  name:'National Environment Agency',
       rules:['Natural ventilation ≥ 5% of floor area', 'Mechanical ventilation - SS 553:2016 fresh air rates', 'Office: 10 L/s/person · Carpark: 7.5 ACH']},
      {id:'PUB',  name:'Public Utilities Board',
       rules:['Minimum platform level (flood prevention)', 'Sanitary fitting provision per PUB Code 2019', 'Surface drainage adequacy']},
    ];
    return `<div class="card" style="margin-top:16px">
      <div class="card-header"><span class="card-title">🏛 Agency Requirements</span></div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
        ${agencies.map(a => `
          <div style="padding:12px;background:var(--light-bg);border-radius:6px">
            <div style="font-weight:700;font-size:12px;color:var(--navy-dark);margin-bottom:6px">
              <span class="badge agency-${a.id}" style="margin-right:6px">${a.id}</span>${VUtils.esc(a.name)}
            </div>
            ${a.rules.map(r => `<div style="font-size:11px;color:var(--mid-grey);padding:1px 0">• ${VUtils.esc(r)}</div>`).join('')}
          </div>`).join('')}
      </div>
    </div>`;
  }

  function renderUbblTable() {
    const parts = [
      {part:'Part III',name:'Space, Light and Ventilation',
       bylaws:['By-Law 47 - Ceiling heights (habitable 2.6m, bathroom 2.3m)', 'By-Law 48 - Room sizes (bedroom ≥6.5m², habitable ≥11m²)', 'By-Law 38 - Natural lighting (window ≥10% floor area)', 'By-Law 39 - Natural ventilation (openable ≥5% floor area)', 'By-Law 55 - Corridor width ≥1.5m']},
      {part:'Part V', name:'Structural Requirements',
       bylaws:['By-Law 95 - Design by registered Professional Engineer', 'By-Law 96 - Loading per MS 1553 / Eurocode 1', 'By-Law 101 - Foundation approval for piling']},
      {part:'Part VI',name:'Constructional Requirements',
       bylaws:['By-Law 112 - Stair: riser ≤175mm, tread ≥255mm', 'By-Law 113 - Stair width ≥900mm private / ≥1,100mm shared', 'By-Law 117 - Weatherproof roof with drainage', 'By-Law 120 - Party walls for fire separation']},
      {part:'Part VII',name:'Fire Requirements (JBPM)',
       bylaws:['By-Law 121 - FRR per Third Schedule (30–240 min by PG)', 'By-Law 122 - Compartmentation', 'By-Law 125 - ≥2 separate exits per floor', 'By-Law 126 - Exit doors ≥900mm clear, outward opening', 'By-Law 127 - Travel distance ≤30m (non-sprinklered)', 'By-Law 133 - Fire doors FD30 minimum', 'By-Law 137 - Smoke-stop lobbies for high-rise']},
      {part:'Part IX',name:'Special Requirements',
       bylaws:['By-Law 180 - Disabled access per MS 1184:2014', 'MS 1184:2014 §5.3 - Accessible door ≥800mm clear', 'MS 1184:2014 §5.2 - Ramp ≤1:12 gradient']},
    ];
    return `<div class="card" style="margin-top:16px">
      <div class="card-header"><span class="card-title">📖 UBBL 1984 - Key By-Laws Covered</span></div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
        ${parts.map(p => `
          <div style="padding:12px;background:var(--light-bg);border-radius:6px">
            <div style="font-weight:700;font-size:12px;color:var(--my-red);margin-bottom:6px">${p.part} - ${p.name}</div>
            ${p.bylaws.map(b => `<div style="font-size:11px;color:var(--mid-grey);padding:1px 0">• ${VUtils.esc(b)}</div>`).join('')}
          </div>`).join('')}
      </div>
    </div>`;
  }

  function renderDesignCodeRules() {
    const rules = [
      {cat:'URA Room Sizes',
       items:['Living room ≥13m² (private) / ≥16m² (HDB)','Bedroom ≥9m²','Master bedroom ≥12.5m²','Kitchen ≥4.5m²','Study ≥5m²','Bathroom ≥2.5m²','Accessible toilet ≥4.0m²']},
      {cat:'BCA Accessibility 2025',
       items:['Door clear width ≥850mm (all accessible routes)','Door clear width ≥900mm (preferred)','Corridor width ≥1,200mm','Ramp gradient ≤1:12','Ramp width ≥1,200mm','Stair riser ≤175mm, tread ≥280mm','Handrail height 850–950mm']},
      {cat:'SCDF Fire Code',
       items:['Exit door ≥750mm (small occupancy) / ≥1,050mm (≥60 occ)','Escape stair ≥1,100mm / ≥1,200mm (high-rise)','Travel distance ≤30m non-sprinklered / ≤60m sprinklered','Compartment ≤3,500m² non-sprinklered / ≤7,000m² sprinklered']},
      {cat:'BCA Green Mark 2021',
       items:['ETTV ≤25 W/m² (residential) / ≤50 W/m² (commercial)','Roof U-value ≤0.35 W/m²K','Wall U-value ≤0.5 W/m²K','Window SHGC ≤0.3','LPD office ≤12 W/m² / retail ≤20 W/m²']},
      {cat:'UBBL Room Dimensions (MY)',
       items:['Ceiling height ≥2.6m habitable / ≥2.3m bathroom','Bedroom ≥6.5m² / habitable room ≥11m²','Stair riser ≤175mm / tread ≥255mm','Travel distance ≤30m non-sprinklered']},
    ];
    return `<div class="card">
      <div class="card-header"><span class="card-title">📐 Design Code Dimensions Reference</span></div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
        ${rules.map(r => `
          <div style="padding:12px;background:var(--light-bg);border-radius:6px">
            <div style="font-weight:700;font-size:12px;color:var(--navy-dark);margin-bottom:6px">${VUtils.esc(r.cat)}</div>
            ${r.items.map(i => `<div style="font-size:11px;color:var(--mid-grey);padding:1px 0">• ${VUtils.esc(i)}</div>`).join('')}
          </div>`).join('')}
      </div>
    </div>`;
  }

  function renderCheckLevels() {
    const levels = [
      [1,'IFC Entity Class','Validates element class against IFC4 schema. Flags IfcBuildingElementProxy with auto-suggested replacement.'],
      [2,'Predefined Type','Validates PredefinedType against permitted IFC4 enumeration for the element class.'],
      [3,'ObjectType (UserDefined)','When PredefinedType=USERDEFINED, checks ObjectType is populated.'],
      [4,'Classification Reference','Mandatory IfcClassificationReference for all physical elements (IFC+SG / NBeS).'],
      [5,'Classification Edition','Checks classification references the current edition (2025 / 2024).'],
      [6,'Mandatory Pset_','Validates all standard IFC4 property sets are present per element type.'],
      [7,'SGPset_ / Classification Chain','Validates all SGPset_ property sets. When a classification code is present, checks ALL code-specific required property sets from the embedded 128-code library (e.g. A-WAL-EXW triggers SGPset_WallThermal checks; A-DOR-FRD triggers SGPset_DoorFireDoor checks).'],
      [8,'Property Values & Classification Chain','Checks each required property is populated (not empty or NOTDEFINED). For classification-coded elements, verifies every code-specific property value meets the requirement (e.g. ThermalTransmittance ≤0.50 for external walls, FireResistancePeriod ≥60 for fire-rated elements).'],
      [9,'Property Data Types','Validates BOOLEAN/REAL/INTEGER/STRING types per IFC schema.'],
      [10,'Enumeration Values','Validates values against permitted lists (space categories, fire ratings etc.).'],
      [11,'Spatial Containment','Every element must be assigned to an IfcBuildingStorey.'],
      [12,'Storey Elevations','Checks for duplicate elevations within and across discipline files.'],
      [13,'Georeferencing','Singapore: SVY21/EPSG:3414 mandatory. Malaysia: GDM2000 recommended.'],
      [14,'Site & Building Hierarchy','Validates IfcProject→IfcSite→IfcBuilding→IfcBuildingStorey chain.'],
      [15,'GUID Uniqueness','Every element must have a unique GlobalId across all discipline files.'],
      [16,'Material Assignment','Structural and fire-rated elements must have material specifications.'],
      [17,'Space Boundary Integrity','IfcSpace must have Category set in Pset_SpaceCommon.'],
      [18,'Geometry Validity','Checks for degenerate, NaN, or infinite bounding boxes.'],
      [19,'IFC Schema Version','CORENET-X requires IFC4 Reference View ADD2 TC1.'],
      [20,'File Header Completeness','Validates authoring system, schema identifier and timestamp.'],
    ];
    return `<div class="card">
      <div class="card-header"><span class="card-title">✅ All 20 Validation Check Levels</span></div>
      <div class="table-wrap">
        <table>
          <thead><tr><th style="width:40px">L</th><th>Check</th><th>Description</th></tr></thead>
          <tbody>
            ${levels.map(([n,name,desc]) => `
              <tr>
                <td style="font-weight:700;color:var(--teal);font-size:12px">${n}</td>
                <td style="font-weight:600;font-size:12px;white-space:nowrap">${VUtils.esc(name)}</td>
                <td style="font-size:11px;color:var(--mid-grey)">${VUtils.esc(desc)}</td>
              </tr>`).join('')}
          </tbody>
        </table>
      </div>
    </div>`;
  }


  // ── HELP PAGE ─────────────────────────────────────────────────────────────

  function renderHelpPage() {
    return `
      <div>
        <h1>Help & Documentation</h1>
        <div class="two-col">
          <div class="card">
            <div class="card-header"><span class="card-title">Getting Started</span></div>
            <ol style="font-size:12px;line-height:1.9;padding-left:20px;color:var(--mid-grey)">
              <li>Select your country mode - Singapore (CORENET-X), Malaysia (NBeS), or Combined</li>
              <li>Click <strong>Open IFC File</strong> and select your .ifc, .ifczip, or .ifcxml file</li>
              <li>Use <strong>Check All - Singapore</strong> on the Dashboard for a single-click full audit</li>
              <li>Or click <strong>Run Validation</strong> to check compliance</li>
              <li>Review findings in Critical Issues, All Results, and the Director's Report</li>
              <li>Click <strong>✏️ Fix</strong> on any finding to open the IFC Property Editor</li>
              <li>Apply fixes and save a corrected IFC file without returning to ArchiCAD or Revit</li>
              <li>Click <strong>Export Reports</strong> to generate PDF/Excel/BCF compliance documents</li>
            </ol>
          </div>
          <div class="card">
            <div class="card-header"><span class="card-title">IFC+SG Export: ArchiCAD</span></div>
            <ol style="font-size:12px;line-height:1.9;padding-left:20px;color:var(--mid-grey)">
              <li>Download the IFC+SG Export Translator from info.corenet.gov.sg</li>
              <li>Import it via Options > Import Scheme</li>
              <li>Use File > Save as IFC > select the IFC+SG scheme</li>
              <li>Ensure IFC4 Reference View is selected</li>
              <li>Load the exported .ifc file into VERIFIQ</li>
            </ol>
          </div>
          <div class="card">
            <div class="card-header"><span class="card-title">IFC+SG Export: Revit</span></div>
            <ol style="font-size:12px;line-height:1.9;padding-left:20px;color:var(--mid-grey)">
              <li>Download IFC+SG shared parameters from info.corenet.gov.sg</li>
              <li>Load the shared parameter file via Manage > Shared Parameters</li>
              <li>Apply the IFC+SG export settings file</li>
              <li>Export using File > Export > IFC, select IFC4 Reference View</li>
              <li>Load the .ifc file into VERIFIQ</li>
            </ol>
          </div>
          <div class="card">
            <div class="card-header"><span class="card-title">Understanding the Results</span></div>
            <div style="font-size:12px;line-height:1.7;color:var(--mid-grey)">
              <p><span class="badge badge-critical">Critical</span> : Submission will be rejected. Fix before submitting.</p>
              <p style="margin-top:6px"><span class="badge badge-error">Error</span> : Likely to cause rejection. Fixing is strongly recommended.</p>
              <p style="margin-top:6px"><span class="badge badge-warning">Warning</span> : May cause issues. Review and fix if applicable.</p>
              <p style="margin-top:6px"><span class="badge badge-pass">Pass</span> : Element meets all requirements for this check.</p>
            </div>
          </div>
        </div>

        <div class="card">
          <div class="card-header"><span class="card-title">Key References</span></div>
          <div class="table-wrap">
            <table>
              <thead><tr><th>Resource</th><th>Description</th><th>Country</th></tr></thead>
              <tbody>
                <tr><td>info.corenet.gov.sg</td><td>CORENET-X portal, IFC+SG toolkit, COP downloads</td><td>🇸🇬</td></tr>
                <tr><td>IFC+SG Industry Mapping Excel</td><td>500+ parameter mapping from BCA/GovTech</td><td>🇸🇬</td></tr>
                <tr><td>CORENET-X COP 3rd Edition (Sep 2025)</td><td>Code of Practice for IFC+SG submissions</td><td>🇸🇬</td></tr>
                <tr><td>Good Practices Guidebook (Dec 2025)</td><td>Practical guidance for IFC+SG preparation</td><td>🇸🇬</td></tr>
                <tr><td>NBeS portal (CIDB)</td><td>Malaysia BIM e-Submission system</td><td>🇲🇾</td></tr>
                <tr><td>UBBL 1984 (Act 133)</td><td>Uniform Building By-Laws (Malaysia)</td><td>🇲🇾</td></tr>
                <tr><td>bbmw0.com</td><td>VERIFIQ product page and support</td><td>Both</td></tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>`;
  }

  // ── VALIDATION PAGE ───────────────────────────────────────────────────────

  function renderValidationPage() {
    const state   = VState.get();
    const files   = state.filesLoaded  || [];
    const session = state.session;
    const mode    = state.countryMode;
    const modeInfo = VUtils.countryDisplay(mode);
    const hasFiles  = files.length > 0;
    const hasResults = !!session;

    return `<div>
      <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:20px">
        <div>
          <h1>Validation</h1>
          <p class="${modeInfo.cls}" style="font-size:13px;font-weight:600;margin-top:2px">${modeInfo.label}</p>
          <p style="font-size:12px;color:var(--mid-grey);margin-top:4px">
            20 IFC data levels + 89 Singapore rules (8 agencies) + 60 Malaysia rules + 128 embedded classification codes = 169+ checks per element
          </p>
        </div>
        ${hasFiles ? `<div style="display:flex;gap:10px">
          <button class="btn btn-primary" onclick="VBridge.openFile()">📂 Open More Files</button>
          <button class="btn btn-teal" onclick="VBridge.runValidation()">▶ Run Validation</button>
        </div>` : ''}
      </div>

      ${!hasFiles ? `
      <div class="card">
        ${VUtils.emptyState('📂', 'No IFC files loaded',
          'Open an IFC file from ArchiCAD, Revit, Tekla, or any IFC-capable BIM authoring tool to begin.',
          '<button class="btn btn-primary" style="margin-top:16px" onclick="VBridge.openFile()">📂 Open IFC File</button>')}
      </div>` : `

      <!-- Live validation progress (shown only during validation) -->
      ${state.loading ? `
      <div class="card" style="margin-bottom:16px;border-left:4px solid var(--teal)">
        <div style="font-size:14px;font-weight:600;color:var(--navy-dark);margin-bottom:10px">
          ⏳ Running validation...
        </div>
        <div style="background:var(--border);border-radius:4px;height:8px;overflow:hidden;margin-bottom:8px">
          <div id="val-progress-bar" style="background:var(--teal);height:100%;width:0%;border-radius:4px;transition:width .3s ease"></div>
        </div>
        <div id="val-progress-label" style="font-size:12px;color:var(--mid-grey)">Initialising...</div>
      </div>` : ''}

      <!-- Loaded files -->
      <div class="card" style="margin-bottom:16px">
        <div class="card-header">
          <span class="card-title">📁 Files Ready for Validation (${files.length})</span>
        </div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>File Name</th><th>Schema</th><th>Elements</th><th>Proxy Elements</th></tr></thead>
            <tbody>
              ${files.map(f => `<tr>
                <td><strong>${VUtils.esc(f.name)}</strong></td>
                <td>${VUtils.esc(f.schema)}</td>
                <td>${VUtils.fmt(f.elements)}</td>
                <td>${f.proxies > 0 ? `<span class="badge badge-warning">⚠ ${VUtils.fmt(f.proxies)}</span>` : '<span class="badge badge-pass">None</span>'}</td>
              </tr>`).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 20 Check Levels overview -->
      <div class="card" style="margin-bottom:16px">
        <div class="card-header"><span class="card-title">✅ What VERIFIQ Will Check</span></div>
        <div class="two-col">
          <div>
            <h3>Data Compliance: 20 Levels</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              L1: IFC Entity Class &nbsp; L2: Predefined Type &nbsp; L3: ObjectType<br>
              L4: Classification Reference &nbsp; L5: Classification Edition<br>
              L6: Mandatory Pset_ &nbsp; L7: SGPset_ (Singapore) &nbsp; L8: Property Values<br>
              L9: Data Types &nbsp; L10: Enumeration Values &nbsp; L11: Spatial Containment<br>
              L12: Storey Elevations &nbsp; L13: Georeferencing &nbsp; L14: Site Hierarchy<br>
              L15: GUID Uniqueness &nbsp; L16: Materials &nbsp; L17: Space Boundaries<br>
              L18: Geometry Validity &nbsp; L19: IFC Schema Version &nbsp; L20: File Header
            </div>
          </div>
          <div>
            <h3>Design Code: 50+ Rules</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              ${mode === 'Malaysia' ? `
              UBBL room dimensions and heights<br>
              MS 1184:2014 accessible door widths (850mm min)<br>
              Corridor widths for disabled access<br>
              JBPM 2020 fire door ratings<br>
              Ramp slopes (1:12 max for accessible)<br>
              Stair riser and tread dimensions<br>
              GBI Malaysia U-value and thermal checks` : `
              URA: Bedroom ≥ 9m², Living ≥ 13m², Kitchen ≥ 4.5m²<br>
              BCA Accessibility 2025: Door width ≥ 850mm<br>
              BCA: Corridor width ≥ 1,200mm, Ramp 1:12 max<br>
              SCDF Fire Code: Door and wall fire ratings<br>
              BCA Green Mark 2021: U-values, WWR, RETV<br>
              URA GFA: Balcony area ≤ 10% of unit GFA<br>
              LTA: Parking dimensions and turning radii`}
            </div>
          </div>
        </div>
        <div style="margin-top:16px;text-align:center">
          <button class="btn btn-teal" onclick="VBridge.runValidation()" style="padding:12px 32px;font-size:14px">
            ▶ Run Full Validation Now
          </button>
        </div>
      </div>`}

      ${hasResults ? `
      <!-- Previous results summary -->
      <div class="card" style="background:var(--green-bg);border-color:var(--green)">
        <div class="card-header">
          <span class="card-title" style="color:var(--green)">✓ Last Validation Results Available</span>
          <button class="btn btn-outline" onclick="App.navigate('results')">View Full Results →</button>
        </div>
        <div class="stat-grid" style="margin-top:8px">
          ${VUtils.statCard(VUtils.pct(session.score),    'Data Score',    VUtils.scoreColour(session.score))}
          ${session.designStats ? VUtils.statCard(VUtils.pct(session.designStats.score), 'Design Score', VUtils.scoreColour(session.designStats.score)) : ''}
          ${VUtils.statCard(VUtils.fmt(session.total),    'Elements')}
          ${VUtils.statCard(VUtils.fmt(session.critical), 'Critical',      session.critical > 0 ? 'red' : 'green')}
          ${VUtils.statCard(VUtils.fmt(session.errors),   'Errors',        session.errors   > 0 ? 'amber' : 'green')}
        </div>
        <div style="margin-top:12px;display:flex;gap:10px">
          <button class="btn btn-primary"  onclick="App.navigate('results')">📋 All Findings</button>
          <button class="btn btn-teal"     onclick="App.navigate('critical')">🚨 Critical Issues</button>
          ${session.designStats ? `<button class="btn btn-outline" onclick="App.navigate('design')">📐 Design Code</button>` : ''}
          <button class="btn btn-outline"  onclick="VBridge.send('export',{})">📤 Export Reports</button>
        </div>
      </div>` : ''}
    </div>`;
  }


  // -- USER GUIDE PAGE ----------------------------------------------------------

  function renderUserGuidePage() {
    return `<div>
      <!-- Header -->
      <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:20px;flex-wrap:wrap;gap:10px">
        <div>
          <h1 style="margin:0">User Guide</h1>
          <p style="font-size:13px;color:var(--mid-grey);margin-top:3px">
            VERIFIQ v1.3.0 - IFC Compliance Checker for Singapore CORENET-X and Malaysia NBeS
          </p>
        </div>
        <button class="btn btn-ghost" onclick="App.navigate('help')">← Back to Help</button>
      </div>

      <!-- Quick nav -->
      <div class="card" style="margin-bottom:16px;background:var(--navy-dark);color:white;border:none">
        <div style="font-weight:700;font-size:13px;margin-bottom:10px;color:#93C5FD">Jump to section</div>
        <div style="display:flex;gap:8px;flex-wrap:wrap;font-size:12px">
          ${['Overview','System Requirements','Getting Started','The Interface','Loading IFC Files',
             'Running Validation','Understanding Results','3D Viewer',
             'IFC Property Editor','Director\'s Report','Classification Library',
             'Exporting Reports','Singapore CORENET-X','Malaysia NBeS',
             'Troubleshooting','Glossary','FAQ'].map((s,i) =>
            `<button onclick="document.getElementById('ug-${i}').scrollIntoView({behavior:'smooth'})"
              style="background:rgba(255,255,255,.12);color:white;border:1px solid rgba(255,255,255,.2);
                     border-radius:4px;padding:4px 10px;cursor:pointer;font-size:11px">${s}</button>`
          ).join('')}
        </div>
      </div>

      <!-- 1. Overview -->
      <div class="card" id="ug-0">
        <div class="card-header"><span class="card-title">1. What is VERIFIQ?</span></div>
        <p style="font-size:13px;line-height:1.8">
          VERIFIQ is a desktop application that reads IFC (Industry Foundation Classes) building model files
          and checks every element against Singapore CORENET-X (IFC+SG 2025) and Malaysia NBeS (UBBL 1984)
          regulatory requirements. It runs entirely offline on your computer - no internet connection, no cloud
          upload, no subscription.
        </p>
        <div style="margin-top:14px;padding:12px;background:var(--navy-dark);color:white;border-radius:8px;margin-bottom:12px">
          <div style="font-weight:700;font-size:13px;margin-bottom:8px;color:#93C5FD">
            The two core questions VERIFIQ answers:
          </div>
          <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
            <div style="background:rgba(255,255,255,.08);border-radius:6px;padding:10px">
              <div style="font-weight:700;font-size:12px;color:#6EE7B7;margin-bottom:6px">Question 1 - Cross-check with CORENET-X</div>
              <div style="font-size:11px;color:#CBD5E1;line-height:1.7">
                Is every element in the IFC model populated with all the data that Singapore CORENET-X
                (IFC+SG 2025) and/or Malaysia NBeS (UBBL 1984) require? This covers entity classes,
                classifications, all required Pset_ and SGPset_ property sets, property values,
                data types, and enumeration constraints - checked against all 8 Singapore agencies
                and all Malaysian regulatory requirements simultaneously.
              </div>
            </div>
            <div style="background:rgba(255,255,255,.08);border-radius:6px;padding:10px">
              <div style="font-weight:700;font-size:12px;color:#FCD34D;margin-bottom:6px">Question 2 - Classification to Property Set chain</div>
              <div style="font-size:11px;color:#CBD5E1;line-height:1.7">
                For every element: (a) is the IFC+SG classification code present? (b) given that
                classification code, are ALL the related SGPset_ property sets and their required
                property values also present? For example: a wall classified as an external wall
                must also have SGPset_WallThermal with ThermalTransmittance, and a wall classified
                as fire-rated must have SGPset_WallFireRating with FireResistancePeriod and
                FireTestStandard. VERIFIQ checks both parts of this chain for every element type.
              </div>
            </div>
          </div>
        </div>
        <div class="two-col" style="margin-top:14px">
          <div style="padding:12px;background:var(--teal)10;border-radius:8px;border:1px solid var(--teal)30">
            <div style="font-weight:700;color:var(--teal);margin-bottom:6px">What VERIFIQ checks</div>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              20 IFC data levels + 89 Singapore rules (all 8 agencies) + 60 Malaysia rules
              (UBBL 1984 all parts) = 169+ checks per element per run. Covers entity classes,
              classifications, all Pset_ and SGPset_ property sets, property values, design code
              dimensions, fire ratings, accessibility, georeferencing, and geometry validity.
            </div>
          </div>
          <div style="padding:12px;background:var(--amber)10;border-radius:8px;border:1px solid var(--amber)30">
            <div style="font-weight:700;color:var(--amber);margin-bottom:6px">What VERIFIQ does NOT do</div>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              VERIFIQ does not modify your IFC model. It does not submit to CORENET-X or NBeS portals.
              A passing result is not regulatory approval - your Qualified Person (QP) remains
              fully responsible for all regulatory compliance determinations.
            </div>
          </div>
        </div>
      </div>

      <!-- 2. System Requirements -->
      <div class="card" id="ug-1" style="margin-top:12px">
        <div class="card-header"><span class="card-title">2. System Requirements</span></div>
        <div class="two-col">
          <div>
            <div class="detail-panel">
              <div class="detail-row"><span class="detail-label">Operating System</span><span class="detail-value">Windows 10 (build 18362 or later) or Windows 11</span></div>
              <div class="detail-row"><span class="detail-label">Architecture</span><span class="detail-value">64-bit (x64) only</span></div>
              <div class="detail-row"><span class="detail-label">.NET Runtime</span><span class="detail-value">.NET 8.0 (included in installer)</span></div>
              <div class="detail-row"><span class="detail-label">RAM</span><span class="detail-value">4 GB minimum; 8 GB recommended for large models</span></div>
              <div class="detail-row"><span class="detail-label">Disk Space</span><span class="detail-value">500 MB for installation; additional space for reports</span></div>
              <div class="detail-row"><span class="detail-label">Display</span><span class="detail-value">1280 x 720 minimum; 1920 x 1080 recommended</span></div>
              <div class="detail-row"><span class="detail-label">Internet</span><span class="detail-value">Not required. Validation and export are 100% offline.</span></div>
            </div>
          </div>
          <div>
            <div style="padding:12px;background:var(--light-bg);border-radius:8px">
              <div style="font-weight:700;font-size:12px;color:var(--navy-dark);margin-bottom:8px">Compatible BIM Authoring Tools</div>
              <div style="font-size:12px;line-height:1.9;color:var(--mid-grey)">
                <strong>ArchiCAD 25+</strong> - use IFC+SG Export Translator<br>
                <strong>Revit 2022+</strong> - use IFC+SG shared parameters<br>
                <strong>Tekla Structures 2020+</strong> - use IFC+SG UDA mapping<br>
                <strong>OpenBuildings Designer</strong> - native IFC4 export<br>
                <strong>Any IFC-capable tool</strong> - IFC4 Reference View ADD2 TC1
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- 3. Getting Started -->
      <div class="card" id="ug-2" style="margin-top:12px">
        <div class="card-header"><span class="card-title">3. Getting Started</span></div>
        <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(220px,1fr));gap:12px">
          ${[
            ['1','Install VERIFIQ','Download VERIFIQ-v1.3.0-Setup.exe from verifiq.bbmw0.com or GitHub. Run the installer as Administrator. VERIFIQ installs to C:\\Program Files\\VERIFIQ.'],
            ['2','Activate your licence','Launch VERIFIQ. Go to Licence in the sidebar. Enter your licence key (format: VRFQ-XXXX-XXXX-XXXX-XXXX). Trial mode is active by default and checks up to 10 elements per run.'],
            ['3','Select country mode','Choose Singapore (CORENET-X), Malaysia (NBeS), or SG + MY (Combined) using the mode buttons in the toolbar. Singapore mode uses IFC+SG 2025 rules. Malaysia mode uses UBBL 1984 / NBeS 2024 rules.'],
            ['4','Select gateway or purpose group','For Singapore: select the CORENET-X gateway (Design, Construction, Completion, Piling). For Malaysia: select the UBBL Purpose Group (I-IX). This controls which rules apply.'],
            ['5','Open an IFC file','Click Open IFC File. Select your .ifc, .ifczip, or .ifcxml file. VERIFIQ will parse it and show element counts and schema version. You can load multiple files for federated model checking.'],
            ['6','Run validation','Click Run Validation. VERIFIQ runs all 20 data levels and all applicable design code rules. Progress is shown in real time. Typical models validate in 5-30 seconds.'],
            ['7','Review results','Go to Dashboard for the summary. All Results for every finding. Critical Issues for submission blockers only. Design Code for dimensional checks.'],
            ['8','Export your report','Go to Export Reports. Choose your format: Word report for submission, Excel for detailed data, BCF for import back into ArchiCAD/Revit, PDF for sharing.'],
          ].map(([n,title,body]) => `
            <div style="padding:12px;background:var(--light-bg);border-radius:8px">
              <div style="display:flex;align-items:center;gap:8px;margin-bottom:6px">
                <div style="width:24px;height:24px;border-radius:50%;background:var(--navy-dark);color:white;
                  font-size:12px;font-weight:700;display:flex;align-items:center;justify-content:center;flex-shrink:0">${n}</div>
                <div style="font-weight:700;font-size:12px;color:var(--navy-dark)">${title}</div>
              </div>
              <div style="font-size:11px;color:var(--mid-grey);line-height:1.7">${body}</div>
            </div>`).join('')}
        </div>
      </div>

      <!-- 4. Interface -->
      <div class="card" id="ug-3" style="margin-top:12px">
        <div class="card-header"><span class="card-title">4. The Interface</span></div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Section</th><th>Location</th><th>What it does</th></tr></thead>
            <tbody>
              ${[
                ['Toolbar','Top bar','Mode buttons (Singapore/Malaysia/SG+MY), Open IFC File, Run Validation, Export Report. The active mode is always shown.'],
                ['Dashboard','Sidebar > Dashboard','Overall health score (A-F), KPI counts, agency risk chart, top 5 quick fixes. The main starting point after validation.'],
                ['Loaded Files','Sidebar > Loaded Files','Lists all open IFC files with element counts, schema version, and proxy element warnings. Add or remove files here.'],
                ['Validation','Sidebar > Validation','Shows files ready for checking, 20 check level overview, design code scope, and previous results summary. Run validation from here.'],
                ['All Results','Sidebar > All Results','Full filterable table of every validation finding across all 20 levels. Filter by severity, agency, or check level.'],
                ['Critical Issues','Sidebar > Critical Issues','Filtered view showing only Critical and Error findings - the ones that will prevent CORENET-X/NBeS submission.'],
                ['Design Code','Sidebar > Design Code','Findings from dimensional checks: room sizes, door widths, travel distances, fire ratings, U-values.'],
                ['3D Viewer','Sidebar > 3D Viewer','WebGL viewer showing the IFC model with elements colour-coded by compliance status. Click any element to see its properties and findings.'],
                ['Export Reports','Sidebar > Export Reports','Generate compliance reports in Word, PDF, Excel, CSV, JSON, HTML, XML, Markdown, or BCF format.'],
                ['Rules Database','Settings > Rules Database','Browse all rules embedded in VERIFIQ: Singapore agencies, Malaysia UBBL by-laws, design code dimensions, all 20 check levels.'],
                ['Licence','Settings > Licence','View your current licence tier, activate a new key, see tier comparison table.'],
                ['Settings','Settings > Settings','Network settings, update check, proxy configuration.'],
                ['About','Settings > About VERIFIQ','Version information, technology stack, regulatory codes covered, version history.'],
              ].map(([s,l,d]) => `<tr>
                <td style="font-weight:600;font-size:12px">${s}</td>
                <td style="font-size:11px;color:var(--mid-grey);white-space:nowrap">${l}</td>
                <td style="font-size:11px;color:var(--mid-grey)">${d}</td>
              </tr>`).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 5. Loading IFC Files -->
      <div class="card" id="ug-4" style="margin-top:12px">
        <div class="card-header"><span class="card-title">5. Loading IFC Files</span></div>
        <div class="two-col">
          <div>
            <h3>Supported File Formats</h3>
            <div style="font-size:12px;color:var(--mid-grey)">
              <div style="padding:8px;background:var(--teal)10;border-radius:6px;margin-bottom:6px">
                <strong style="color:var(--teal)">Fully validated (IFC compliance checking)</strong><br>
                <span style="line-height:1.9">
                  <strong>.ifc</strong> - Standard IFC STEP format (recommended for CORENET-X)<br>
                  <strong>.ifczip</strong> - Compressed IFC (smaller file size, same validation)<br>
                  <strong>.ifcxml</strong> - IFC in XML format (same validation as .ifc)
                </span>
              </div>
              <div style="padding:8px;background:var(--amber)10;border-radius:6px;margin-bottom:6px">
                <strong style="color:var(--amber)">Opens with export instructions (cannot validate directly)</strong><br>
                <span style="line-height:1.9">
                  <strong>.rvt</strong> - Revit: VERIFIQ shows IFC+SG export instructions<br>
                  <strong>.pln</strong> - ArchiCAD: VERIFIQ shows IFC+SG translator download link<br>
                  <strong>.dwg / .dxf</strong> - AutoCAD: export to IFC from your BIM software<br>
                  <strong>.nwd / .nwf</strong> - Navisworks: extract discipline IFCs first<br>
                  <strong>.skp</strong> - SketchUp: export via IFC-Manager plugin
                </span>
              </div>
              <div style="padding:8px;background:var(--border);border-radius:6px">
                <strong style="color:var(--mid-grey)">Reference / visual only (loaded but not validated)</strong><br>
                <span style="line-height:1.9">
                  <strong>.bcf</strong> - BIM Collaboration Format: issue tracking import<br>
                  <strong>.obj / .fbx / .stl</strong> - Mesh geometry: 3D visual reference only<br>
                  <strong>.e57 / .las</strong> - Point cloud: scan data visual reference<br>
                  <strong>.pdf</strong> - Drawing reference alongside IFC model<br>
                  <strong>.xlsx</strong> - IFC+SG Industry Mapping Excel: rules import
                </span>
              </div>
            </div>
            <h3 style="margin-top:14px">Preparing your IFC from ArchiCAD</h3>
            <ol style="font-size:12px;line-height:1.9;color:var(--mid-grey);padding-left:18px">
              <li>Download the IFC+SG Export Translator from info.corenet.gov.sg</li>
              <li>In ArchiCAD: File > Save as IFC</li>
              <li>Select the IFC+SG Export Translator scheme</li>
              <li>Schema: IFC4 Reference View</li>
              <li>Load the .ifc file into VERIFIQ</li>
            </ol>
          </div>
          <div>
            <h3>Preparing your IFC from Revit</h3>
            <ol style="font-size:12px;line-height:1.9;color:var(--mid-grey);padding-left:18px">
              <li>Download IFC+SG shared parameter files from info.corenet.gov.sg</li>
              <li>Load via Manage > Shared Parameters</li>
              <li>File > Export > IFC > IFC4 Reference View</li>
              <li>Apply the IFC+SG export settings (.json)</li>
              <li>Load the .ifc file into VERIFIQ</li>
            </ol>
            <h3 style="margin-top:14px">Federated Models</h3>
            <div style="font-size:12px;line-height:1.7;color:var(--mid-grey)">
              VERIFIQ supports federated BIM - load multiple IFC files
              (Architecture, Civil & Structural, M&E) at once. It checks GUID
              uniqueness across all files and cross-discipline rules. Use the
              Loaded Files page to add or remove discipline files.
            </div>
          </div>
        </div>
      </div>

      <!-- 6. Running Validation -->
      <div class="card" id="ug-5" style="margin-top:12px">
        <div class="card-header"><span class="card-title">6. Running Validation</span></div>
        <div class="two-col">
          <div>
            <h3>Country Modes</h3>
            <div class="detail-panel">
              <div class="detail-row"><span class="detail-label" style="width:120px">SG Singapore</span><span class="detail-value" style="font-size:11px">CORENET-X IFC+SG 2025. All 8 agencies. SVY21 georeferencing mandatory. 89 agency rules + 50+ design code rules.</span></div>
              <div class="detail-row"><span class="detail-label" style="width:120px">MY Malaysia</span><span class="detail-value" style="font-size:11px">NBeS 2024 / UBBL 1984. JBPM, CIDB, JKR. GDM2000 georeferencing. Purpose Group-specific rules.</span></div>
              <div class="detail-row"><span class="detail-label" style="width:120px">SG + MY</span><span class="detail-value" style="font-size:11px">Runs both rulesets simultaneously. Useful for firms with projects in both countries.</span></div>
            </div>
            <h3 style="margin-top:14px">Singapore Gateways</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              Select the target CORENET-X gateway to control which rules apply:<br>
              <strong>Design Gateway (G1)</strong> - Classification, space data, GFA<br>
              <strong>Piling Gateway</strong> - Pile classification and foundation data<br>
              <strong>Construction Gateway (G2)</strong> - Full data + fire ratings + structural<br>
              <strong>Completion Gateway (G3)</strong> - As-built data + CSC/TOP requirements<br>
              <strong>DSP</strong> - Direct Submission (simplified, smaller projects)
            </div>
          </div>
          <div>
            <h3>Malaysia Purpose Groups</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              Select the UBBL 1984 Purpose Group to apply occupancy-specific rules:<br>
              <strong>PG I</strong> - Residential (houses, flats)<br>
              <strong>PG II</strong> - Residential (flats, hotels)<br>
              <strong>PG III</strong> - Offices<br>
              <strong>PG IV</strong> - Shops and commercial<br>
              <strong>PG V</strong> - Assembly (cinemas, stadiums)<br>
              <strong>PG VI</strong> - Industrial (factories, warehouses)<br>
              <strong>PG VII</strong> - Storage<br>
              <strong>PG VIII</strong> - Healthcare and institutional<br>
              <strong>PG IX</strong> - Mixed use
            </div>
            <h3 style="margin-top:14px">How long does validation take?</h3>
            <div style="font-size:12px;color:var(--mid-grey)">
              Small model (under 500 elements): 2-5 seconds<br>
              Typical model (500-5,000 elements): 5-20 seconds<br>
              Large model (5,000-50,000 elements): 20-120 seconds<br>
              Very large federated model: up to 5 minutes
            </div>
          </div>
        </div>
      </div>

      <!-- 7. Understanding Results -->
      <div class="card" id="ug-6" style="margin-top:12px">
        <div class="card-header"><span class="card-title">7. Understanding Validation Results</span></div>
        <div class="two-col">
          <div>
            <h3>Severity Levels</h3>
            <div class="detail-panel">
              <div class="detail-row">
                <span><span class="badge badge-critical" style="margin-right:8px">Critical</span></span>
                <span class="detail-value" style="font-size:11px">Submission will definitely be rejected. Must fix before uploading to CORENET-X or NBeS.</span>
              </div>
              <div class="detail-row">
                <span><span class="badge badge-error" style="margin-right:8px">Error</span></span>
                <span class="detail-value" style="font-size:11px">Submission will likely be rejected or cause agency review comments. Strongly recommended to fix.</span>
              </div>
              <div class="detail-row">
                <span><span class="badge badge-warning" style="margin-right:8px">Warning</span></span>
                <span class="detail-value" style="font-size:11px">May cause agency queries. Review and fix where applicable before submission.</span>
              </div>
              <div class="detail-row">
                <span><span class="badge badge-pass" style="margin-right:8px">Pass</span></span>
                <span class="detail-value" style="font-size:11px">Element meets all requirements for this check level.</span>
              </div>
            </div>
          </div>
          <div>
            <h3>The Compliance Score</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              The compliance score (0-100) and grade (A-F) measure overall model quality:<br>
              <strong>A (90-100)</strong> - Excellent. Ready to submit.<br>
              <strong>B (75-89)</strong> - Good. Minor issues only.<br>
              <strong>C (60-74)</strong> - Fair. Notable issues to resolve.<br>
              <strong>D (40-59)</strong> - Poor. Significant rework needed.<br>
              <strong>F (0-39)</strong> - Critical. Major compliance gaps.<br><br>
              The score is computed from two components: Data Compliance (20 levels)
              and Design Code (dimensional rules). Both are shown as separate bars.
            </div>
          </div>
        </div>
        <h3 style="margin-top:14px">What each result column means</h3>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Column</th><th>Meaning</th></tr></thead>
            <tbody>
              ${[
                ['Severity','Critical / Error / Warning / Pass'],
                ['Check Level','Which of the 20 data levels or design code rule triggered this finding'],
                ['Element Name','The name or identifier of the IFC element with the issue'],
                ['GUID','The GlobalId of the element - use this to locate it in your BIM tool'],
                ['Storey','Which building storey the element is on'],
                ['Agency','Which regulatory agency (BCA, URA, SCDF etc.) requires the missing data'],
                ['Property Set / Property','The specific IFC property set and property that is missing or incorrect'],
                ['Message','Plain-language description of what is wrong'],
                ['Fix','Specific guidance on what to change in your BIM model to resolve the issue'],
              ].map(([col,meaning]) => `<tr>
                <td style="font-weight:600;font-size:12px;white-space:nowrap">${col}</td>
                <td style="font-size:11px;color:var(--mid-grey)">${meaning}</td>
              </tr>`).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 8. 3D Viewer -->
      <div class="card" id="ug-7" style="margin-top:12px">
        <div class="card-header"><span class="card-title">8. The 3D Viewer</span></div>
        <div class="two-col">
          <div>
            <h3>Navigation</h3>
            <div style="font-size:12px;line-height:1.9;color:var(--mid-grey)">
              <strong>Orbit</strong> - Left mouse button drag<br>
              <strong>Pan</strong> - Right mouse button drag, or Shift + left drag<br>
              <strong>Zoom</strong> - Mouse scroll wheel<br>
              <strong>Reset view</strong> - Click the reset button in the viewer toolbar<br>
              <strong>Select element</strong> - Left click on any element<br>
              <strong>Deselect</strong> - Click empty space
            </div>
            <h3 style="margin-top:14px">Compliance colour coding</h3>
            <div class="detail-panel">
              <div class="detail-row"><span style="display:inline-block;width:12px;height:12px;background:#EF4444;border-radius:2px;margin-right:6px"></span><span class="detail-value" style="font-size:11px">Red - element has Critical or Error findings</span></div>
              <div class="detail-row"><span style="display:inline-block;width:12px;height:12px;background:#F59E0B;border-radius:2px;margin-right:6px"></span><span class="detail-value" style="font-size:11px">Amber - element has Warning findings only</span></div>
              <div class="detail-row"><span style="display:inline-block;width:12px;height:12px;background:#10B981;border-radius:2px;margin-right:6px"></span><span class="detail-value" style="font-size:11px">Green - element passes all checks</span></div>
              <div class="detail-row"><span style="display:inline-block;width:12px;height:12px;background:#6B7280;border-radius:2px;margin-right:6px"></span><span class="detail-value" style="font-size:11px">Grey - element not yet validated</span></div>
            </div>
          </div>
          <div>
            <div style="padding:8px;background:var(--amber)10;border-radius:6px;border:1px solid var(--amber)30;margin-bottom:10px">
              <div style="font-weight:700;font-size:11px;color:var(--amber);margin-bottom:4px">
                About "web-ifc: Cannot read properties of undefined"
              </div>
              <div style="font-size:11px;color:var(--mid-grey);line-height:1.7">
                This message in the status bar is <strong>normal</strong> - it means the web-ifc WASM
                engine was not used and the 3D viewer automatically switched to the C# geometry engine instead.
                The model will still display correctly. This happens when no IFC file has been loaded yet,
                or when the WASM engine encounters a geometry format it cannot process directly.
                It is a fallback notification, not an error.
              </div>
            </div>
            <h3>Element Inspector</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              Click any element in the 3D view to open the Element Inspector panel.
              It shows:<br>
              - IFC class and predefined type<br>
              - Element name and GUID<br>
              - Storey and spatial container<br>
              - All property sets and their values<br>
              - All validation findings for this element<br>
              - Specific fix guidance per finding<br><br>
              Use the GUID shown in the inspector to locate the element in ArchiCAD
              (Edit > Find and Select > by GlobalId) or Revit (Manage > Inquiry > IFC GUID).
            </div>
          </div>
        </div>
      </div>

      <!-- IFC Property Editor (new ug-8) -->
      <div class="card" id="ug-8" style="margin-top:12px">
        <div class="card-header"><span class="card-title">9. IFC Property Editor - Fix Without Going Back to ArchiCAD</span></div>
        <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
          <p>Fix missing or incorrect property values directly in VERIFIQ. Your original IFC is never modified.</p>
          <ol style="padding-left:20px;line-height:1.9;margin-top:6px">
            <li>Run validation, open Critical Issues or All Results</li>
            <li>Click <strong>✏️ Fix</strong> on any Pset_ or SGPset_ error row</li>
            <li>In the Property Editor page: enter the correct value for each queued property</li>
            <li>Click <strong>Apply Fixes and Save Corrected IFC</strong></li>
            <li>VERIFIQ saves <code>filename_VERIFIQ_FIXED_timestamp.ifc</code> next to your original</li>
            <li>Open the corrected file in VERIFIQ, re-validate, then submit to CORENET-X</li>
          </ol>
          <div style="padding:8px;background:var(--amber)10;border-radius:6px;margin-top:8px;font-size:11px">
            <strong>Fixable here:</strong> ThermalTransmittance, FireResistancePeriod, IsExternal, LoadBearing, GFACategory, WELSRating, ClearWidth, and all other IfcPropertySingleValue properties.<br>
            <strong>Must fix in ArchiCAD/Revit:</strong> IFC entity class, classification codes, spatial containment.
          </div>
        </div>
      </div>

      <!-- Director's Report (new ug-9) -->
      <div class="card" id="ug-9" style="margin-top:12px">
        <div class="card-header"><span class="card-title">10. Director's Report - One-Click Executive Submission Brief</span></div>
        <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
          <p>Click <strong>✅ Check All - Singapore</strong> on the Dashboard. The Director's Report appears below the KPI cards.</p>
          <ul style="padding-left:20px;line-height:1.9;margin-top:6px">
            <li><strong>Readiness Verdict</strong> - Ready / Conditionally Ready / Not Ready with 0-100 score</li>
            <li><strong>Agency Risk Table</strong> - all 8 SG agencies rated HIGH/MEDIUM/LOW/CLEAR</li>
            <li><strong>Top 5 Blockers</strong> - ranked by count, with fix guidance per agency</li>
            <li><strong>Action Plan</strong> - prioritised steps with rework time estimates</li>
            <li><strong>Effort Estimate</strong> - total hours broken down by Critical/Error/Warning</li>
            <li><strong>Gateway Readiness</strong> - which CORENET-X gateway the model qualifies for</li>
            <li><strong>Model Quality Grade</strong> - A/B/C/D/F from classification + pset + geometry coverage</li>
          </ul>
        </div>
      </div>

      <!-- Classification Library (new ug-10) -->
      <div class="card" id="ug-10" style="margin-top:12px">
        <div class="card-header"><span class="card-title">11. Classification Library - 128 IFC+SG Codes with Exact Property Rules</span></div>
        <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
          <p>VERIFIQ embeds 128 IFC+SG classification codes (COP3, 2025). For every code, VERIFIQ knows exactly
          which SGPset_ property sets and properties are required, by which agency, at which gateway.</p>
          <p style="margin-top:8px"><strong>Coverage:</strong> All architectural elements (walls, slabs, doors, windows, 17 space types, stairs, ramps, roofs, lifts),
          all structural elements (columns, beams, slabs, walls, foundations, 5 pile types),
          M&amp;E (HVAC, fire systems, electrical), plumbing (with PUB WELS ratings),
          civil, landscape, and full Malaysia NBeS codes (UBBL 1984 / MS 1184 / JBPM).</p>
          <p style="margin-top:8px"><strong>Import updates from BCA:</strong> Rules Database → Import BCA Industry Mapping Excel
          - browse to the official Excel from info.corenet.gov.sg. VERIFIQ auto-detects columns and
          merges codes into the runtime library immediately. Re-run validation to use the updated rules.</p>
        </div>
      </div>

      <!-- 9. Exporting Reports -->
      <div class="card" id="ug-11" style="margin-top:12px">
        <div class="card-header"><span class="card-title">9. Exporting Reports</span></div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Format</th><th>Best used for</th><th>Contents</th></tr></thead>
            <tbody>
              ${[
                ['Word (.docx)','Formal submission documentation, client reports','Executive summary, all findings with remediation guidance, agency summary, element tables'],
                ['PDF','Sharing, printing, archiving','Same as Word but in fixed-layout PDF format'],
                ['Excel (.xlsx)','Data analysis, tracking fixes, QA management','9 worksheets: summary, all findings, by agency, by element type, design code, passing elements, statistics, charts'],
                ['CSV','Database import, custom analysis tools','Raw findings data in comma-separated format'],
                ['BCF 2.1','Importing issues back into ArchiCAD, Revit, Tekla','Building Collaboration Format - each finding becomes a viewpoint that opens directly in your BIM tool'],
                ['JSON','API integration, custom reporting tools','Structured findings data in JSON format'],
                ['HTML','Web sharing, email attachments','Self-contained HTML report viewable in any browser'],
                ['XML','Enterprise data systems, BIM server integration','Structured XML with full element and finding data'],
                ['Markdown','Documentation systems, GitHub, Confluence','Plain text with formatting for technical documentation'],
                ['Text','Simple logging, archiving','Plain text summary for record-keeping'],
              ].map(([f,use,contents]) => `<tr>
                <td style="font-weight:600;font-size:12px;white-space:nowrap">${f}</td>
                <td style="font-size:11px;color:var(--mid-grey)">${use}</td>
                <td style="font-size:11px;color:var(--mid-grey)">${contents}</td>
              </tr>`).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 10. Singapore CORENET-X -->
      <div class="card" id="ug-12" style="margin-top:12px">
        <div class="card-header"><span class="card-title">10. Singapore CORENET-X Guide</span></div>
        <div class="two-col">
          <div>
            <h3>What CORENET-X expects</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              Every physical element in your IFC model must have:<br>
              <strong>1. Correct IFC entity class</strong> - e.g. IfcWall, IfcSlab, IfcDoor<br>
              <strong>2. Valid PredefinedType</strong> - e.g. SOLIDWALL, FLOOR, EXTERNAL<br>
              <strong>3. IFC+SG classification</strong> - the ItemReference from the Industry Mapping Excel<br>
              <strong>4. Standard property sets</strong> - Pset_WallCommon, Pset_SlabCommon etc.<br>
              <strong>5. SGPset_ property sets</strong> - Singapore-specific: SGPset_WallFireRating, SGPset_SpaceGFA etc.<br>
              <strong>6. All required property values</strong> - FireRating, IsExternal, GrossPlannedArea etc.<br>
              <strong>7. SVY21 georeferencing</strong> - EPSG:3414 via IfcMapConversion
            </div>
          </div>
          <div>
            <h3>Common CORENET-X issues and fixes</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              <strong>Missing classification</strong> - Open IFC Manager in ArchiCAD, assign the correct IFC+SG classification code from the Industry Mapping Excel to each element type.<br>
              <strong>Missing SGPset_ fire rating</strong> - In ArchiCAD IFC Manager, add SGPset_WallFireRating to fire-rated walls. Set FireResistancePeriod to the REI value (e.g. 60).<br>
              <strong>Missing space GFA category</strong> - For each IfcSpace, add SGPset_SpaceGFA and set GFACategory (e.g. RESIDENTIAL, CARPARK, VOID).<br>
              <strong>Missing georeferencing</strong> - Configure IfcMapConversion in ArchiCAD Project Preferences > IFC tab. Set the SVY21 coordinates.
            </div>
          </div>
        </div>
        <div style="margin-top:14px;padding:12px;background:var(--teal)08;border-radius:8px;border:1px solid var(--teal)20">
          <div style="font-weight:700;font-size:12px;color:var(--teal);margin-bottom:6px">Official Resources</div>
          <div style="font-size:12px;line-height:1.9;color:var(--mid-grey)">
            CORENET-X portal and IFC+SG toolkit: <strong>info.corenet.gov.sg</strong><br>
            Industry Mapping Excel (master rules reference): downloadable from the IFC+SG Resource Kit<br>
            CORENET-X COP 3rd Edition (October 2025): the definitive submission standard<br>
            Good Practices Guidebook (December 2025): practical workflow guidance
          </div>
        </div>
      </div>

      <!-- 11. Malaysia NBeS -->
      <div class="card" id="ug-13" style="margin-top:12px">
        <div class="card-header"><span class="card-title">11. Malaysia NBeS Guide</span></div>
        <div class="two-col">
          <div>
            <h3>What NBeS expects</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              Every element must have correct IFC entity class, NBeS classification, standard IFC4
              property sets, and all required values per UBBL 1984 and JBPM requirements.<br><br>
              Key requirements per element:<br>
              <strong>Walls</strong> - IsExternal, LoadBearing, FireRating<br>
              <strong>Slabs</strong> - IsExternal, LoadBearing, FireRating<br>
              <strong>Doors</strong> - IsExternal, HandicapAccessible, FireRating<br>
              <strong>Spaces</strong> - Category, GrossPlannedArea, Height<br>
              <strong>Stairs</strong> - RiserHeight, TreadLength<br>
              <strong>Columns and Beams</strong> - LoadBearing, FireRating
            </div>
          </div>
          <div>
            <h3>UBBL Purpose Group selection</h3>
            <div style="font-size:12px;line-height:1.8;color:var(--mid-grey)">
              Select the correct Purpose Group before running validation - different PGs have different
              minimum fire resistance periods per the UBBL Third Schedule.<br><br>
              <strong>Fire resistance requirements by PG:</strong><br>
              PG I-II (Residential): FRR 30-60 min<br>
              PG III-IV (Office/Shop): FRR 60-90 min<br>
              PG V (Assembly): FRR 90-120 min<br>
              PG VI (Industrial): FRR 120-180 min
            </div>
          </div>
        </div>
      </div>

      <!-- 12. Troubleshooting -->
      <div class="card" id="ug-14" style="margin-top:12px">
        <div class="card-header"><span class="card-title">12. Troubleshooting</span></div>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Problem</th><th>Cause</th><th>Solution</th></tr></thead>
            <tbody>
              ${[
                ['"Integrity Error" on launch','Previous installation left a stale integrity manifest','Delete C:\\Users\\[username]\\AppData\\Local\\VERIFIQ\\integrity.manifest and relaunch'],
                ['Licence key not accepted','Key format wrong or key not matching the embedded store','Keys must be in format VRFQ-XXXX-XXXX-XXXX-XXXX (29 characters). Check for extra spaces.'],
                ['IFC file fails to open','File is corrupt, not IFC4, or schema is IFC2X3','Export a fresh IFC4 file from your BIM tool. VERIFIQ requires IFC4 Reference View.'],
                ['0 elements found after opening','IFC file may be IFC2X3 or empty','Check schema version in Loaded Files. Re-export as IFC4 from ArchiCAD/Revit.'],
                ['3D Viewer shows blank/black','WebView2 runtime issue or very large model','Click the Reset View button. For very large models, use the All Results table instead.'],
                ['Validation takes very long','Very large model (50,000+ elements)','Split the model by discipline and validate each file separately. Or upgrade RAM.'],
                ['Export fails or is empty','No validation has been run yet','Run validation first, then export. The export uses the current session results.'],
                ['Missing SGPset_ findings','Singapore mode not selected','Switch to Singapore or SG+MY mode before running validation.'],
              ].map(([p,c,s]) => `<tr>
                <td style="font-size:11px;font-weight:600;color:var(--navy-dark)">${p}</td>
                <td style="font-size:11px;color:var(--mid-grey)">${c}</td>
                <td style="font-size:11px;color:var(--teal)">${s}</td>
              </tr>`).join('')}
            </tbody>
          </table>
        </div>
      </div>

      <!-- 13. Glossary -->
      <div class="card" id="ug-15" style="margin-top:12px">
        <div class="card-header"><span class="card-title">13. Glossary</span></div>
        <div class="two-col">
          <div>
            <div class="table-wrap">
              <table>
                <thead><tr><th>Term</th><th>Meaning</th></tr></thead>
                <tbody>
                  ${[
                    ['IFC','Industry Foundation Classes - open standard for BIM data exchange (ISO 16739)'],
                    ['IFC+SG','Singapore extension of IFC4 with CORENET-X specific property sets'],
                    ['SGPset_','Singapore-specific IFC property sets required by CORENET-X agencies'],
                    ['MVD','Model View Definition - a subset of IFC4 defining which entities are required'],
                    ['Reference View','The IFC4 MVD used by CORENET-X (IFC4 Reference View ADD2 TC1)'],
                    ['GlobalId / GUID','Unique identifier for every IFC element - used to locate elements in BIM tools'],
                    ['Pset_','Standard IFC property set (e.g. Pset_WallCommon)'],
                    ['PredefinedType','The specific sub-type of an IFC element (e.g. SOLIDWALL, FLOOR)'],
                    ['SVY21','Singapore coordinate reference system (EPSG:3414) - mandatory for CORENET-X'],
                    ['GDM2000','Malaysia coordinate reference system - recommended for NBeS'],
                    ['IfcMapConversion','IFC entity that stores the coordinate reference system offset for a model'],
                  ].map(([t,m]) => `<tr>
                    <td style="font-weight:600;font-size:11px;white-space:nowrap">${t}</td>
                    <td style="font-size:11px;color:var(--mid-grey)">${m}</td>
                  </tr>`).join('')}
                </tbody>
              </table>
            </div>
          </div>
          <div>
            <div class="table-wrap">
              <table>
                <thead><tr><th>Term</th><th>Meaning</th></tr></thead>
                <tbody>
                  ${[
                    ['CORENET-X','Singapore multi-agency building regulatory approval platform (BCA/URA/GovTech)'],
                    ['NBeS','National BIM e-Submission system for Malaysia (CIDB)'],
                    ['UBBL','Uniform Building By-Laws 1984 - primary building regulation in Malaysia'],
                    ['COP','Code of Practice - the CORENET-X submission guide (COP 3rd Edition, Oct 2025)'],
                    ['BCA','Building and Construction Authority (Singapore)'],
                    ['URA','Urban Redevelopment Authority (Singapore)'],
                    ['SCDF','Singapore Civil Defence Force (fire safety)'],
                    ['GFA','Gross Floor Area - computed by URA from IfcSpace.GrossPlannedArea'],
                    ['QP','Qualified Person - the registered architect/engineer responsible for submissions'],
                    ['BCF','BIM Collaboration Format - used to import issues into ArchiCAD, Revit, Tekla'],
                    ['REI','Fire resistance rating notation: R=load bearing, E=integrity, I=insulation (minutes)'],
                  ].map(([t,m]) => `<tr>
                    <td style="font-weight:600;font-size:11px;white-space:nowrap">${t}</td>
                    <td style="font-size:11px;color:var(--mid-grey)">${m}</td>
                  </tr>`).join('')}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <!-- 14. FAQ -->
      <div class="card" id="ug-16" style="margin-top:12px">
        <div class="card-header"><span class="card-title">14. Frequently Asked Questions</span></div>
        <div style="display:flex;flex-direction:column;gap:10px">
          ${[
            ['Does VERIFIQ submit my model to CORENET-X for me?',
             'No. VERIFIQ is a pre-submission checker only. You still upload your validated IFC file to the CORENET-X portal (info.corenet.gov.sg) yourself. VERIFIQ helps you identify and fix issues before submission.'],
            ['If VERIFIQ gives a passing result, does that mean my submission will be approved?',
             'No. A VERIFIQ pass means your IFC data meets the technical requirements checked by VERIFIQ. Regulatory approval is determined by BCA, URA, SCDF and other agencies reviewing the full submission. Your Qualified Person remains responsible for all compliance determinations.'],
            ['Can I use VERIFIQ without an internet connection?',
             'Yes. All validation, 3D viewing, and report export functions work 100% offline. The only feature that requires internet is the optional software update check, which runs silently in the background and can be disabled.'],
            ['My IFC file was exported from ArchiCAD but VERIFIQ shows many errors. What do I do?',
             'Ensure you are using the IFC+SG Export Translator from info.corenet.gov.sg (not the default ArchiCAD IFC export). The default export does not include SGPset_ property sets. Download and import the IFC+SG translator, then re-export.'],
            ['What is the difference between a Critical and an Error finding?',
             'Critical means the submission will definitely be rejected - the element is fundamentally non-compliant. Error means it will likely cause rejection or significant review comments. Both should be fixed before submission. Warnings are advisory.'],
            ['Can VERIFIQ check models from Tekla Structures?',
             'Yes. Export from Tekla using the IFC+SG property set definitions for Objects.inp. The IFC4 export must use IFC4 Reference View schema. Load the .ifc file into VERIFIQ as normal.'],
            ['How do I find an element in ArchiCAD from its GUID shown in VERIFIQ?',
             'In ArchiCAD: Edit > Find and Select > search by IFC Global ID (paste the GUID from VERIFIQ). The element will be selected and highlighted in the model.'],
            ['Can I validate multiple discipline files (Architecture, Structure, MEP) together?',
             'Yes. Use the Loaded Files page to open multiple IFC files. VERIFIQ checks GUID uniqueness across all files and runs all applicable rules on each file. This is the recommended workflow for CORENET-X federated submissions.'],
          ].map(([q,a]) => `
            <div style="padding:12px;background:var(--light-bg);border-radius:8px">
              <div style="font-weight:700;font-size:12px;color:var(--navy-dark);margin-bottom:5px">Q: ${q}</div>
              <div style="font-size:12px;color:var(--mid-grey);line-height:1.7">A: ${a}</div>
            </div>`).join('')}
        </div>
      </div>

      <!-- Footer -->
      <div class="card" style="margin-top:12px;background:var(--navy-dark);color:white;border:none">
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:12px">
          <div>
            <div style="font-weight:700;font-size:14px;margin-bottom:4px">Need more help?</div>
            <div style="font-size:12px;color:#93C5FD">Contact BBMW0 Technologies for support and licencing</div>
          </div>
          <div style="display:flex;gap:10px;flex-wrap:wrap">
            <a href="https://verifiq.bbmw0.com" target="_blank" style="text-decoration:none">
              <button style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3);
                border-radius:6px;padding:8px 16px;cursor:pointer;font-size:12px">🌐 verifiq.bbmw0.com</button>
            </a>
            <a href="mailto:bbmw0@hotmail.com" style="text-decoration:none">
              <button style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3);
                border-radius:6px;padding:8px 16px;cursor:pointer;font-size:12px">✉ bbmw0@hotmail.com</button>
            </a>
            <a href="https://github.com/bbmw96/verifiq" target="_blank" style="text-decoration:none">
              <button style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3);
                border-radius:6px;padding:8px 16px;cursor:pointer;font-size:12px">⭐ GitHub</button>
            </a>
          </div>
        </div>
      </div>
    </div>`;
  }

  function renderPropertyEditorPage() {
    // Render the page HTML
    const html = `<div>
      <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px">
        <div>
          <h1 style="margin:0">IFC Property Editor</h1>
          <p style="font-size:13px;color:var(--mid-grey);margin-top:3px">
            Fix missing or incorrect IFC property values directly - no return to ArchiCAD or Revit needed
          </p>
        </div>
        <div style="display:flex;gap:8px">
          <button class="btn btn-ghost" onclick="App.navigate('critical')">← Critical Issues</button>
          <button class="btn btn-ghost" onclick="App.navigate('results')">← All Results</button>
        </div>
      </div>

      <div style="padding:12px 16px;background:var(--navy-dark);color:white;border-radius:8px;margin-bottom:16px;display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:8px">
        <div style="font-size:12px">
          <strong style="color:#93C5FD">To add fixes:</strong>
          go to Critical Issues or All Results - find a property error - click <strong style="color:#FCD34D">✏️ Fix</strong> on that row.
          Each fix is added to the queue below.
        </div>
        <div style="display:flex;gap:8px">
          <button class="btn" style="background:var(--teal);color:white;font-size:12px;padding:6px 14px"
            onclick="App.navigate('critical')">🔴 Go to Critical Issues</button>
          <button class="btn" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.2);font-size:12px;padding:6px 14px"
            onclick="App.navigate('results')">📋 Go to All Results</button>
        </div>
      </div>

      <div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:12px;margin-bottom:16px">
        <div class="card" style="padding:14px">
          <div style="font-weight:700;font-size:12px;color:var(--teal);margin-bottom:6px">How it works</div>
          <div style="font-size:11px;color:var(--mid-grey);line-height:1.8">
            1. Go to <strong>Critical Issues</strong> or <strong>All Results</strong><br>
            2. Find a <span style="color:var(--red)">Critical</span> or Error finding with a property<br>
            3. Click <strong>✏️ Fix</strong> on that row<br>
            4. Enter the correct value below<br>
            5. Click <strong>Apply Fixes</strong><br>
            6. VERIFIQ saves a corrected IFC file<br>
            7. Re-validate the corrected file
          </div>
        </div>
        <div class="card" style="padding:14px">
          <div style="font-weight:700;font-size:12px;color:var(--amber);margin-bottom:6px">What can be fixed here</div>
          <div style="font-size:11px;color:var(--mid-grey);line-height:1.8">
            <strong>Yes</strong> - any IfcPropertySingleValue in Pset_ or SGPset_:<br>
            ThermalTransmittance, FireResistancePeriod, IsExternal, LoadBearing, GFACategory, WELSRating, ClearWidth, and all others<br><br>
            <strong>No</strong> - must fix in ArchiCAD/Revit:<br>
            IFC entity class, classification codes, spatial containment
          </div>
        </div>
        <div class="card" style="padding:14px">
          <div style="font-weight:700;font-size:12px;color:var(--green);margin-bottom:6px">Your original is always safe</div>
          <div style="font-size:11px;color:var(--mid-grey);line-height:1.8">
            VERIFIQ <strong>never</strong> modifies your original IFC file.<br><br>
            The corrected version is always saved as a new file:<br>
            <code style="font-size:10px;background:#F1F5F9;padding:2px 6px;border-radius:3px">filename_VERIFIQ_FIXED_timestamp.ifc</code><br><br>
            An edit log is saved alongside it listing every change made.
          </div>
        </div>
      </div>

      <div id="prop-editor-panel">
        <!-- Queue renders here -->
      </div>
    </div>`;

    // Schedule panel render after DOM is updated
    setTimeout(() => {
      if (window.PropertyEditor) PropertyEditor.renderPanel();
    }, 60);

    return html;
  }

  return { init, navigate, refresh, render };
})();


// ─── RULES DATABASE PAGE MODULE ──────────────────────────────────────────────
const RulesDbPage = (() => {

  function browseAndImport() {
    // Trigger file open dialog via bridge, then import
    VBridge.send('openFileForImport', { filter: 'xlsx', purpose: 'industryMapping' });
  }

  function importFile(filePath) {
    const panel = document.getElementById('industry-mapping-result');
    if (panel) panel.innerHTML = `
      <div style="font-size:12px;color:var(--mid-grey);padding:8px">
        ⏳ Importing ${VUtils.esc(filePath.split('\\\\').pop())}...
      </div>`;
    VBridge.send('importIndustryMapping', { path: filePath });
  }

  function onImportResult(data) {
    const panel = document.getElementById('industry-mapping-result');
    if (!panel) return;
    if (data.success) {
      panel.innerHTML = `
        <div style="padding:12px;background:#F0FDF4;border:1px solid #86EFAC;border-radius:8px">
          <div style="font-weight:700;color:#15803D;margin-bottom:6px">
            ✅ Import successful - ${data.codesImported} new codes, ${data.codesUpdated} updated, ${data.rulesImported} rules total
          </div>
          <div style="font-size:11px;color:#166534;margin-bottom:6px">
            Source: ${VUtils.esc(data.version)}
          </div>
          <div style="font-size:11px;color:var(--mid-grey);max-height:120px;overflow-y:auto">
            ${(data.importedCodes||[]).map(l => `<div>${VUtils.esc(l)}</div>`).join('')}
            ${(data.importedCodes||[]).length === 50 ? '<div>... and more</div>' : ''}
          </div>
          ${(data.warnings||[]).length > 0 ? `
            <div style="margin-top:6px;font-size:11px;color:var(--amber)">
              ${(data.warnings||[]).map(w => `<div>⚠ ${VUtils.esc(w)}</div>`).join('')}
            </div>` : ''}
          <div style="margin-top:8px;font-size:11px;color:#166534">
            Re-run validation on any loaded IFC file to use the updated rules.
          </div>
        </div>`;
    } else {
      panel.innerHTML = `
        <div style="padding:10px;background:#FEF2F2;border:1px solid #FCA5A5;border-radius:8px">
          <div style="font-weight:700;color:#B91C1C">Import failed: ${VUtils.esc(data.error||'Unknown error')}</div>
          ${(data.errors||[]).map(e => `<div style="font-size:11px">${VUtils.esc(e)}</div>`).join('')}
        </div>`;
    }
  }

  return { browseAndImport, importFile, onImportResult };
})();
window.RulesDbPage = RulesDbPage;

window.App = App;

// Bootstrap on DOM ready
document.addEventListener('DOMContentLoaded', App.init);
