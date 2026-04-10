// VERIFIQ v1.2 — Enhanced Dashboard with Charts, Health Score, Quick Fixes
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.

'use strict';

const DashboardPage = (() => {

  function render() {
    const state    = VState.get();
    const session  = state.session;
    const mode     = state.countryMode;
    const modeInfo = VUtils.countryDisplay(mode);
    const files    = state.filesLoaded || [];

    return `<div>
      <!-- Page header -->
      <div style="display:flex;align-items:flex-start;justify-content:space-between;margin-bottom:20px">
        <div>
          <h1 style="margin:0">Compliance Dashboard</h1>
          <p class="${modeInfo.cls}" style="font-size:13px;font-weight:600;margin:3px 0 0">
            ${modeInfo.label}
          </p>
        </div>
        <div style="display:flex;gap:8px">
          <button class="btn btn-primary" onclick="VBridge.openFile()">📂 Open IFC File</button>
          ${files.length > 0 ? `<button class="btn btn-teal" onclick="VBridge.runValidation()">▶ Run Validation</button>` : ''}
          ${session ? `<button class="btn btn-outline" onclick="VBridge.send('export',{})">📤 Export Report</button>` : ''}
        </div>
      </div>

      ${!session ? renderWelcome(state) : renderResults(session, state)}
    </div>`;
  }

  // ── Welcome / no-results state ────────────────────────────────────────────
  function renderWelcome(state) {
    const files = state.filesLoaded || [];
    const hasFiles = files.length > 0;

    return `
      ${hasFiles ? renderFilesReady(files) : ''}

      <!-- Capability overview -->
      <div class="card">
        <div class="card-header">
          <span class="card-title">⚡ What VERIFIQ Checks</span>
          <span style="font-size:11px;color:var(--mid-grey)">v1.2.0 — Rules: IFC+SG 2025.1 (COP3) · NBeS 2024.1 (CIDB)</span>
        </div>
        <div class="three-col">
          ${capCard('20 Data Levels','Entity class, PredefinedType, Classification, 13 Property Set levels, Georeferencing SVY21/GDM2000, GUID uniqueness, Geometry validity','var(--teal)')}
          ${capCard('50+ Design Rules','URA room sizes, BCA Accessibility 2025, SCDF Fire Code 2018/2023, Green Mark 2021, UBBL 1984 Parts I–IX, MS 1184:2014, JBPM 2020','var(--navy)')}
          ${capCard('8 Agencies','BCA · URA · SCDF · LTA · NEA · NParks · PUB · SLA (Singapore) · JBPM · CIDB · JKR (Malaysia)','var(--amber)')}
        </div>
      </div>

      ${!hasFiles ? renderOnboarding() : ''}`;
  }

  function capCard(title, body, colour) {
    return `<div style="padding:14px;background:${colour}10;border-radius:8px;border:1px solid ${colour}30">
      <div style="font-weight:700;font-size:13px;color:${colour};margin-bottom:6px">${title}</div>
      <div style="font-size:11px;color:var(--mid-grey);line-height:1.6">${body}</div>
    </div>`;
  }

  function renderFilesReady(files) {
    return `<div class="card" style="margin-bottom:16px;border-left:4px solid var(--teal)">
      <div style="display:flex;align-items:center;justify-content:space-between">
        <div>
          <div style="font-weight:700;font-size:14px;color:var(--navy-dark)">
            ✅ ${files.length} IFC file${files.length>1?'s':''} ready for validation
          </div>
          <div style="font-size:12px;color:var(--mid-grey);margin-top:4px">
            ${files.map(f=>`<span style="margin-right:12px">📄 ${VUtils.esc(f.name)} (${VUtils.fmt(f.elements)} elements)</span>`).join('')}
          </div>
        </div>
        <button class="btn btn-teal" style="white-space:nowrap" onclick="VBridge.runValidation()">▶ Run Validation</button>
      </div>
    </div>`;
  }

  function renderOnboarding() {
    return `<div class="card">
      <div class="card-header"><span class="card-title">🚀 Getting Started</span></div>
      <div class="two-col">
        <div>
          <div style="font-size:13px;font-weight:600;color:var(--navy-dark);margin-bottom:8px">Step 1 — Open an IFC file</div>
          <p style="font-size:12px">Export from ArchiCAD (IFC4 Reference View) or Revit (IFC4 with IFC+SG shared parameters). 
          Open .ifc, .ifczip, or .ifcxml files.</p>
          <button class="btn btn-primary" style="margin-top:10px" onclick="VBridge.openFile()">📂 Open IFC File</button>
        </div>
        <div>
          <div style="font-size:13px;font-weight:600;color:var(--navy-dark);margin-bottom:8px">Step 2 — Select mode and run</div>
          <p style="font-size:12px">Choose Singapore (CORENET-X), Malaysia (NBeS), or Combined. 
          Select the CORENET-X gateway or UBBL Purpose Group, then click Run Validation.</p>
        </div>
      </div>
      <div style="margin-top:14px;padding-top:14px;border-top:1px solid var(--border)">
        <div style="font-size:11px;color:var(--mid-grey)">
          <strong>Supported export formats:</strong> Word · PDF · Excel · CSV · HTML · JSON · XML · Markdown · BCF
          &nbsp;|&nbsp; <strong>Offline:</strong> All validation, 3D viewing and reports work 100% without internet
        </div>
      </div>
    </div>`;
  }

  // ── Results dashboard ─────────────────────────────────────────────────────
  function renderResults(session, state) {
    const dc        = session.designStats;
    const files     = state.filesLoaded || [];
    const oScore    = session.overallScore || session.score || 0;
    const dScore    = dc ? (dc.score || 0) : null;
    const dataScore = session.score || 0;

    return `
      <!-- Health Score banner -->
      ${renderHealthBanner(session, dc)}

      <!-- KPI grid -->
      <div class="stat-grid" style="margin-bottom:16px">
        ${kpi(VUtils.fmt(session.total||0),     'Total Elements',    'teal')}
        ${kpi(VUtils.fmt(session.critical||0),  'Critical',          'red')}
        ${kpi(VUtils.fmt(session.errors||0),    'Errors',            'amber')}
        ${kpi(VUtils.fmt(session.warnings||0),  'Warnings',          'amber')}
        ${kpi(VUtils.fmt(session.passed||0),    'Compliant',         'green')}
        ${kpi(VUtils.fmt(session.proxies||0),   'Proxy Elements',    session.proxies>0?'red':'teal')}
      </div>

      <!-- Two column: charts + quick fixes -->
      <div class="two-col" style="margin-bottom:16px">
        ${renderAgencyChart(session)}
        ${renderQuickFixes(session)}
      </div>

      <!-- Files summary + design code -->
      <div class="two-col">
        ${renderFilesSummary(files, session)}
        ${dc ? renderDesignSummary(dc) : ''}
      </div>`;
  }

  function renderHealthBanner(session, dc) {
    const dataScore   = session.score || 0;
    const designScore = dc ? (dc.score || 0) : null;
    const overall     = designScore !== null ? (dataScore + designScore) / 2 : dataScore;
    const grade       = overall >= 90 ? 'A' : overall >= 75 ? 'B' : overall >= 60 ? 'C' : overall >= 40 ? 'D' : 'F';
    const gradeCol    = overall >= 90 ? 'var(--green)' : overall >= 60 ? 'var(--amber)' : 'var(--red)';
    const label       = overall >= 90 ? 'Excellent' : overall >= 75 ? 'Good' : overall >= 60 ? 'Fair' : overall >= 40 ? 'Poor' : 'Critical';

    return `<div class="card" style="margin-bottom:16px;background:linear-gradient(135deg,var(--navy-dark),var(--navy));color:white;border:none">
      <div style="display:flex;align-items:center;gap:24px;flex-wrap:wrap">
        <!-- Grade -->
        <div style="text-align:center;flex-shrink:0">
          <div style="font-size:52px;font-weight:900;color:${gradeCol};line-height:1">${grade}</div>
          <div style="font-size:11px;color:#93C5FD;margin-top:2px">${label}</div>
        </div>
        <!-- Score bars -->
        <div style="flex:1;min-width:200px">
          <div style="font-size:16px;font-weight:700;margin-bottom:10px">
            Overall: <span style="color:${gradeCol}">${overall.toFixed(1)}%</span>
          </div>
          ${scoreBar('Data Compliance', dataScore, '#38BDF8')}
          ${designScore !== null ? scoreBar('Design Code', designScore, '#A78BFA') : ''}
        </div>
        <!-- Actions -->
        <div style="display:flex;flex-direction:column;gap:8px;flex-shrink:0">
          <button class="btn" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3)"
            onclick="App.navigate('results')">📋 All Findings</button>
          <button class="btn" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3)"
            onclick="App.navigate('critical')">🚨 Critical Issues</button>
          <button class="btn" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3)"
            onclick="VBridge.send('export',{})">📤 Export Report</button>
        </div>
      </div>
    </div>`;
  }

  function scoreBar(label, score, colour) {
    const w = Math.max(0, Math.min(100, score));
    return `<div style="margin-bottom:8px">
      <div style="display:flex;justify-content:space-between;font-size:12px;margin-bottom:3px">
        <span style="color:#CBD5E1">${label}</span>
        <span style="font-weight:700;color:${colour}">${score.toFixed(1)}%</span>
      </div>
      <div style="background:rgba(255,255,255,.15);border-radius:3px;height:6px;overflow:hidden">
        <div style="background:${colour};height:100%;width:${w}%;border-radius:3px;transition:width .6s ease"></div>
      </div>
    </div>`;
  }

  function kpi(value, label, colour) {
    const colMap = {teal:'var(--teal)',red:'var(--red)',amber:'var(--amber)',green:'var(--green)'};
    return `<div class="stat-card">
      <div class="stat-val ${colour}" style="color:${colMap[colour]||'var(--navy-dark)'}">${value}</div>
      <div class="stat-lbl">${label}</div>
    </div>`;
  }

  function renderAgencyChart(session) {
    const byAgency = session.errorsByAgency || {};
    const entries  = Object.entries(byAgency).sort((a,b) => b[1]-a[1]);
    const maxVal   = entries.length ? Math.max(...entries.map(e=>e[1])) : 1;

    const agencyColours = {
      BCA:'#1D4ED8', URA:'#15803D', SCDF:'#B91C1C', LTA:'#B45309',
      NEA:'#6D28D9', NParks:'#065F46', PUB:'#1D4ED8', SLA:'#92400E', None:'#9CA3AF'
    };

    const bars = entries.slice(0, 8).map(([agency, count]) => {
      const pct = maxVal > 0 ? (count / maxVal * 100) : 0;
      const col = agencyColours[agency] || '#6B7280';
      return `<div style="display:flex;align-items:center;gap:8px;margin-bottom:7px">
        <div style="width:42px;font-size:11px;font-weight:700;color:${col};text-align:right;flex-shrink:0">${VUtils.esc(agency)}</div>
        <div style="flex:1;background:var(--border);border-radius:3px;height:18px;overflow:hidden">
          <div style="background:${col};height:100%;width:${pct}%;border-radius:3px;transition:width .6s .1s ease;
                      display:flex;align-items:center;padding-left:6px">
            <span style="font-size:10px;color:white;font-weight:700">${count}</span>
          </div>
        </div>
      </div>`;
    }).join('');

    return `<div class="card">
      <div class="card-header"><span class="card-title">🏛 Errors by Regulatory Agency</span></div>
      ${entries.length === 0
        ? '<div style="color:var(--mid-grey);font-size:12px;text-align:center;padding:20px">No agency-level errors</div>'
        : bars}
    </div>`;
  }

  function renderQuickFixes(session) {
    const findings = (session.findings || []).filter(f => f.severity === 'Critical' || f.severity === 'Error');
    
    // Group by check level → top issues
    const byCheck = {};
    findings.forEach(f => {
      if (!byCheck[f.check]) byCheck[f.check] = { check: f.check, count: 0, agency: f.agency, fix: f.fix };
      byCheck[f.check].count++;
    });
    const top5 = Object.values(byCheck).sort((a,b) => b.count-a.count).slice(0, 5);

    const items = top5.map((item, i) => `
      <div style="display:flex;gap:10px;padding:8px 0;border-bottom:1px solid var(--border)">
        <div style="width:22px;height:22px;border-radius:50%;background:var(--navy-dark);color:white;
                    font-size:11px;font-weight:700;display:flex;align-items:center;justify-content:center;flex-shrink:0">
          ${i+1}
        </div>
        <div style="flex:1;min-width:0">
          <div style="font-size:12px;font-weight:600;color:var(--navy-dark)">${VUtils.esc(item.check)} 
            <span style="color:var(--red);font-weight:400">(${item.count})</span>
          </div>
          <div style="font-size:11px;color:var(--mid-grey);margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap"
               title="${VUtils.esc(item.fix)}">
            ${VUtils.esc((item.fix||'').substring(0,80))}${item.fix&&item.fix.length>80?'…':''}
          </div>
        </div>
      </div>`).join('');

    return `<div class="card">
      <div class="card-header"><span class="card-title">⚡ Top 5 Quick Fixes</span></div>
      ${top5.length === 0
        ? '<div style="color:var(--mid-grey);font-size:12px;text-align:center;padding:20px">No critical findings</div>'
        : `<div>${items}</div>
           <button class="btn btn-teal" style="margin-top:12px;width:100%" onclick="App.navigate('critical')">
             View All Critical Issues →
           </button>`}
    </div>`;
  }

  function renderFilesSummary(files, session) {
    if (!files.length) return '<div></div>';
    const byType = {};
    (session.findings||[]).forEach(f => {
      if (!byType[f.cls]) byType[f.cls] = 0;
      byType[f.cls]++;
    });
    const topTypes = Object.entries(byType).sort((a,b)=>b[1]-a[1]).slice(0,6);

    return `<div class="card">
      <div class="card-header"><span class="card-title">📁 Loaded Files</span></div>
      ${files.map(f => `
        <div style="padding:8px 0;border-bottom:1px solid var(--border);display:flex;justify-content:space-between;align-items:center">
          <div>
            <div style="font-size:13px;font-weight:600">${VUtils.esc(f.name)}</div>
            <div style="font-size:11px;color:var(--mid-grey)">${VUtils.esc(f.schema)} · ${VUtils.fmt(f.elements)} elements
              ${f.proxies>0 ? `· <span style="color:var(--amber)">⚠ ${f.proxies} proxy</span>` : ''}
            </div>
          </div>
          <button class="btn btn-ghost" style="font-size:11px;padding:3px 10px"
            onclick="App.navigate('3d');setTimeout(()=>{if(window.Viewer3DPage)Viewer3DPage.loadFile('${VUtils.esc(f.name)}')},400)">
            🧊 3D
          </button>
        </div>`).join('')}
      ${topTypes.length > 0 ? `
        <div style="margin-top:10px">
          <div style="font-size:11px;font-weight:600;color:var(--mid-grey);margin-bottom:6px;text-transform:uppercase;letter-spacing:.5px">Most Issues By Type</div>
          ${topTypes.map(([cls,n]) => `
            <div style="display:flex;justify-content:space-between;font-size:11px;padding:2px 0">
              <span style="color:var(--mid-grey)">${VUtils.esc(cls)}</span>
              <span style="font-weight:700;color:var(--red)">${n}</span>
            </div>`).join('')}
        </div>` : ''}
    </div>`;
  }

  function renderDesignSummary(dc) {
    if (!dc) return '<div></div>';
    const categories = dc.failsByCategory || {};
    const catEntries = Object.entries(categories).sort((a,b)=>b[1]-a[1]).slice(0,6);

    return `<div class="card">
      <div class="card-header">
        <span class="card-title">📐 Design Code</span>
        <span style="font-size:12px;font-weight:600;color:${dc.score>=70?'var(--green)':'var(--red)'}">
          ${(dc.score||0).toFixed(1)}% pass
        </span>
      </div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:10px">
        ${kpi(VUtils.fmt(dc.total||0),    'Checks',  'teal')}
        ${kpi(VUtils.fmt(dc.passed||0),   'Passed',  'green')}
        ${kpi(VUtils.fmt(dc.failed||0),   'Failed',  'red')}
        ${kpi(VUtils.fmt(dc.critical||0), 'Critical','red')}
      </div>
      ${catEntries.length > 0 ? `
        <div style="font-size:11px;font-weight:600;color:var(--mid-grey);margin-bottom:6px;text-transform:uppercase;letter-spacing:.5px">By Category</div>
        ${catEntries.map(([cat,n]) => `
          <div style="display:flex;justify-content:space-between;font-size:11px;padding:2px 0">
            <span style="color:var(--mid-grey)">${VUtils.esc(cat.replace('_',' '))}</span>
            <span style="font-weight:700;color:var(--amber)">${n} fails</span>
          </div>`).join('')}` : ''}
      <button class="btn btn-outline" style="margin-top:10px;width:100%;font-size:12px"
        onclick="App.navigate('design')">View Design Code Findings →</button>
    </div>`;
  }

  return { render };
})();

window.DashboardPage = DashboardPage;
