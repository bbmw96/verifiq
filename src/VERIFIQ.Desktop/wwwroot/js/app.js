// VERIFIQ — Main Application
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.

'use strict';

const App = (() => {
  const container = () => document.getElementById('page-container');

  // Page registry — every sidebar nav button must have an entry here
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
          <div style="font-weight:700;color:#B91C1C;margin-bottom:8px">⚠ Page render error — ${page}</div>
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
              <span class="detail-value">Singapore and Malaysia — all tiers include both countries</span>
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
            To purchase a licence: <strong>bbmw0@hotmail.com</strong> | <strong>bbmw0.com</strong><br>
            <span style="font-size:11px">
              Example keys by tier:<br>
              Trial: <code>VRFQ-TRIAL-DEMO0-0000-00000001</code><br>
              Individual: <code>VRFQ-IND1-0001-0000-6C6A84BB</code><br>
              Practice: <code>VRFQ-PRAC-0251-0000-F3E3C137</code><br>
              Enterprise: <code>VRFQ-ENT1-0501-0000-9303B434</code><br>
              Site Licence: <code>VRFQ-ENTX-0751-0000-AB60C977</code>
            </span>
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
                  <td>Site licence — deploy to entire organisation, perpetual</td></tr>
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
      <p style="color:var(--mid-grey);font-size:13px;margin-bottom:16px">
        Complete regulatory knowledge embedded in VERIFIQ v1.2 — fully offline, no internet required.
      </p>

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
              <b>Coordinate:</b> SVY21 (EPSG:3414) — mandatory IfcMapConversion<br>
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
              <b>Coordinate:</b> GDM2000 (per-state projection) — recommended<br>
              <b>Purpose Groups:</b> PG I–IX per UBBL 1984 Third Schedule<br>
              <b>Agencies:</b> JBPM · CIDB · JKR · Local Authority (PBT)
            </div>
          </div>
          <div class="card">
            <div class="card-header"><span class="card-title">📋 Legislation</span></div>
            <div style="font-size:12px;line-height:1.8">
              Street, Drainage and Building Act 1974 (Act 133)<br>
              Uniform Building By-Laws 1984 (Parts I–IX)<br>
              MS 1184:2014 — Access for Disabled People<br>
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
       rules:['Structural adequacy (BC 2:2021 / SS EN 1992)', 'Code on Accessibility 2025 — accessible routes', 'BCA Green Mark 2021 — ETTV/RETV/LPD/WWR', 'Building Control Act — structural submission', 'Foundation — piling gateway requirements']},
      {id:'URA',  name:'Urban Redevelopment Authority',
       rules:['GFA computation from IfcSpace.GrossPlannedArea', 'Plot ratio compliance (Master Plan 2019)', 'Balcony ≤ 10% of unit GFA', 'Setback distances (road reserve categories)', 'Space category enumeration (50+ permitted values)']},
      {id:'SCDF', name:'Singapore Civil Defence Force',
       rules:['Fire compartment size (7,000m² sprinklered / 3,500m² non-sprinklered)', 'Travel distance (60m / 30m)', 'Exit widths — ≥750mm / 1,050mm (60+ occupants)', 'Escape stair widths — 1,100mm / 1,200mm (high-rise)', 'Fire resistance ratings (FRR) per SCDF Table 4.2']},
      {id:'LTA',  name:'Land Transport Authority',
       rules:['Parking quantum per use type', 'Standard bay 2.5m × 5.0m', 'Accessible bay 3.6m × 5.0m', 'Loading/unloading bay 3.5m × 12m × 4.2m clear height']},
      {id:'NEA',  name:'National Environment Agency',
       rules:['Natural ventilation ≥ 5% of floor area', 'Mechanical ventilation — SS 553:2016 fresh air rates', 'Office: 10 L/s/person · Carpark: 7.5 ACH']},
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
       bylaws:['By-Law 47 — Ceiling heights (habitable 2.6m, bathroom 2.3m)', 'By-Law 48 — Room sizes (bedroom ≥6.5m², habitable ≥11m²)', 'By-Law 38 — Natural lighting (window ≥10% floor area)', 'By-Law 39 — Natural ventilation (openable ≥5% floor area)', 'By-Law 55 — Corridor width ≥1.5m']},
      {part:'Part V', name:'Structural Requirements',
       bylaws:['By-Law 95 — Design by registered Professional Engineer', 'By-Law 96 — Loading per MS 1553 / Eurocode 1', 'By-Law 101 — Foundation approval for piling']},
      {part:'Part VI',name:'Constructional Requirements',
       bylaws:['By-Law 112 — Stair: riser ≤175mm, tread ≥255mm', 'By-Law 113 — Stair width ≥900mm private / ≥1,100mm shared', 'By-Law 117 — Weatherproof roof with drainage', 'By-Law 120 — Party walls for fire separation']},
      {part:'Part VII',name:'Fire Requirements (JBPM)',
       bylaws:['By-Law 121 — FRR per Third Schedule (30–240 min by PG)', 'By-Law 122 — Compartmentation', 'By-Law 125 — ≥2 separate exits per floor', 'By-Law 126 — Exit doors ≥900mm clear, outward opening', 'By-Law 127 — Travel distance ≤30m (non-sprinklered)', 'By-Law 133 — Fire doors FD30 minimum', 'By-Law 137 — Smoke-stop lobbies for high-rise']},
      {part:'Part IX',name:'Special Requirements',
       bylaws:['By-Law 180 — Disabled access per MS 1184:2014', 'MS 1184:2014 §5.3 — Accessible door ≥800mm clear', 'MS 1184:2014 §5.2 — Ramp ≤1:12 gradient']},
    ];
    return `<div class="card" style="margin-top:16px">
      <div class="card-header"><span class="card-title">📖 UBBL 1984 — Key By-Laws Covered</span></div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:12px">
        ${parts.map(p => `
          <div style="padding:12px;background:var(--light-bg);border-radius:6px">
            <div style="font-weight:700;font-size:12px;color:var(--my-red);margin-bottom:6px">${p.part} — ${p.name}</div>
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
      [7,'SGPset_ (Singapore)','Validates Singapore-specific SGPset_ property sets required by CORENET-X agencies.'],
      [8,'Property Values','Checks each required property is populated (not empty or NOTDEFINED).'],
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
              <li>Select your country mode (Singapore, Malaysia, or Combined)</li>
              <li>Click <strong>Open IFC File</strong> and select your .ifc file</li>
              <li>Click <strong>Run Validation</strong> to check compliance</li>
              <li>Review findings on the Dashboard and Results pages</li>
              <li>Click <strong>Export Reports</strong> to generate compliance documents</li>
              <li>Use BCF export to import issues back into ArchiCAD, Revit or Tekla</li>
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

  return { init, navigate, refresh, render };
})();

window.App = App;

// Bootstrap on DOM ready
document.addEventListener('DOMContentLoaded', App.init);
