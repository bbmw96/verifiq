// VERIFIQ v1.3 - Dashboard with Check All (Singapore) + Director's Report
// Copyright 2026 BBMW0 Technologies. All rights reserved.

'use strict';

const DashboardPage = (() => {

  // Director's Report state - populated when executiveSummary arrives from bridge
  let _executiveSummary = null;

  function render() {
    const state    = VState.get();
    const session  = state.session;
    const mode     = state.countryMode;
    const modeInfo = VUtils.countryDisplay(mode);
    const files    = state.filesLoaded || [];

    return `<div>
      <div style="display:flex;align-items:flex-start;justify-content:space-between;margin-bottom:20px;flex-wrap:wrap;gap:10px">
        <div>
          <h1 style="margin:0">Compliance Dashboard</h1>
          <p class="${modeInfo.cls}" style="font-size:13px;font-weight:600;margin:3px 0 0">${modeInfo.label}</p>
        </div>
        <div style="display:flex;gap:8px;flex-wrap:wrap">
          <button class="btn btn-primary" onclick="VBridge.openFile()">📂 Open IFC File</button>
          ${files.length > 0 ? `
            <button class="btn btn-teal" onclick="VBridge.runValidation()">▶ Run Validation</button>
            <button class="btn" style="background:var(--navy-dark);color:white;font-weight:700;border:none"
              onclick="DashboardPage.runCheckAll()"
              title="Runs all 20 data levels + all design code rules then generates the Director's Report">
              ✅ Check All - Singapore
            </button>` : ''}
          ${session ? `<button class="btn btn-outline" onclick="VBridge.send('export',{})">📤 Export</button>` : ''}
        </div>
      </div>
      ${!session ? renderWelcome(state) : renderResults(session, state)}
    </div>`;
  }

  // ── Check All ─────────────────────────────────────────────────────────────
  function runCheckAll() {
    const state = VState.get();
    if (!state.filesLoaded || state.filesLoaded.length === 0) {
      alert('Please open an IFC file before running Check All.');
      return;
    }
    _executiveSummary = null;
    VBridge.send('setCountryMode', { mode: 'Singapore' });
    setTimeout(() => {
      VBridge.runValidation();
      // Request Director's Report after validation - allow 4s for large models
      setTimeout(() => VBridge.send('getExecutiveSummary', {}), 4000);
    }, 300);
  }

  // Called by bridge.js when C# sends back the executive summary
  function onExecutiveSummary(data) {
    _executiveSummary = data;
    if (window.App && App.navigate) App.navigate('dashboard');
  }

  // ── Welcome state ─────────────────────────────────────────────────────────
  function renderWelcome(state) {
    const files    = state.filesLoaded || [];
    const hasFiles = files.length > 0;
    return `
      ${hasFiles ? renderFilesReady(files) : ''}
      <div class="card">
        <div class="card-header">
          <span class="card-title">⚡ What VERIFIQ Checks</span>
          <span style="font-size:11px;color:var(--mid-grey)">v1.3.0 - IFC+SG 2025.1 (COP3) · NBeS 2024.1</span>
        </div>
        <div class="three-col">
          ${capCard('20 IFC Data Levels + 128 Codes','20 IFC data levels (entity class → classification → SGPset_ → property values → georeferencing → geometry) PLUS 128 embedded IFC+SG classification codes with exact SGPset_ requirements per code per agency','var(--teal)')}
          ${capCard('89 Singapore + 60 Malaysia Rules','SG: URA room sizes, BCA Accessibility 2025, SCDF Fire Code 2018/2023, BCA Green Mark 2021, LTA parking, NEA ventilation, PUB drainage. MY: UBBL 1984 Parts I-IX, MS 1184:2014, JBPM Fire Safety 2020, GBI thermal','var(--navy)')}
          ${capCard('8 Agencies','BCA · URA · SCDF · LTA · NEA · NParks · PUB · SLA - every element mapped to every agency that regulates it','var(--amber)')}
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
      <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:10px">
        <div>
          <div style="font-weight:700;font-size:14px;color:var(--navy-dark)">
            ✅ ${files.length} IFC file${files.length>1?'s':''} ready
          </div>
          <div style="font-size:12px;color:var(--mid-grey);margin-top:4px">
            ${files.map(f=>`<span style="margin-right:12px">📄 ${VUtils.esc(f.name)} (${VUtils.fmt(f.elements)} elements)</span>`).join('')}
          </div>
        </div>
        <div style="display:flex;gap:8px">
          <button class="btn btn-teal" onclick="VBridge.runValidation()">▶ Run Validation</button>
          <button class="btn" style="background:var(--navy-dark);color:white;font-weight:700;border:none"
            onclick="DashboardPage.runCheckAll()">✅ Check All - Singapore</button>
        </div>
      </div>
    </div>`;
  }

  function renderOnboarding() {
    return `<div class="card">
      <div class="card-header"><span class="card-title">🚀 Getting Started</span></div>
      <div class="two-col">
        <div>
          <div style="font-size:13px;font-weight:600;color:var(--navy-dark);margin-bottom:8px">Step 1 - Open an IFC file</div>
          <p style="font-size:12px">Export from ArchiCAD using the IFC+SG Translator (IFC4 Reference View). Open .ifc, .ifczip, or .ifcxml files.</p>
          <button class="btn btn-primary" style="margin-top:10px" onclick="VBridge.openFile()">📂 Open IFC File</button>
        </div>
        <div>
          <div style="font-size:13px;font-weight:600;color:var(--navy-dark);margin-bottom:8px">Step 2 - Check All (Singapore)</div>
          <p style="font-size:12px">Click <strong>Check All - Singapore</strong> to run every check in one pass and receive the Director's Report - submission readiness, agency risk, top blockers, action plan, and effort estimate.</p>
        </div>
      </div>
      <div style="margin-top:14px;padding-top:14px;border-top:1px solid var(--border)">
        <div style="font-size:11px;color:var(--mid-grey)">
          <strong>Export formats:</strong> Word · PDF · Excel · CSV · HTML · JSON · XML · BCF
          &nbsp;|&nbsp; <strong>100% offline</strong> - no internet required
        </div>
      </div>
    </div>`;
  }

  // ── Results state ─────────────────────────────────────────────────────────
  function renderResults(session, state) {
    const dc   = session.designStats;
    const files = state.filesLoaded || [];
    const exec  = _executiveSummary || session.executiveSummary || null;

    return `
      ${renderHealthBanner(session, dc)}
      <div class="stat-grid" style="margin-bottom:16px">
        ${kpi(VUtils.fmt(session.total||0),    'Total Elements', 'teal')}
        ${kpi(VUtils.fmt(session.critical||0), 'Critical',       'red')}
        ${kpi(VUtils.fmt(session.errors||0),   'Errors',         'amber')}
        ${kpi(VUtils.fmt(session.warnings||0), 'Warnings',       'amber')}
        ${kpi(VUtils.fmt(session.passed||0),   'Compliant',      'green')}
        ${kpi(VUtils.fmt(session.proxies||0),  'Proxy Elements', session.proxies>0?'red':'teal')}
      </div>
      ${exec ? renderDirectorsReport(exec) : renderDirectorsReportPrompt()}
      <div class="two-col" style="margin-bottom:16px">
        ${renderAgencyChart(session)}
        ${renderQuickFixes(session)}
      </div>
      <div class="two-col">
        ${renderFilesSummary(files, session)}
        ${dc ? renderDesignSummary(dc) : ''}
      </div>`;
  }

  // ── Director's Report prompt ──────────────────────────────────────────────
  function renderDirectorsReportPrompt() {
    return `<div class="card" style="margin-bottom:16px;border:2px dashed var(--navy);background:var(--navy)08">
      <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:12px">
        <div>
          <div style="font-weight:700;font-size:14px;color:var(--navy-dark)">📊 Director's Report</div>
          <div style="font-size:12px;color:var(--mid-grey);margin-top:3px">
            Submission readiness verdict · Agency risk table · Top blockers · Action plan · Effort estimate
          </div>
        </div>
        <button class="btn" style="background:var(--navy-dark);color:white;font-weight:700;border:none;white-space:nowrap"
          onclick="DashboardPage.runCheckAll()">✅ Generate Director's Report</button>
      </div>
    </div>`;
  }

  // ── Director's Report full panel ──────────────────────────────────────────
  function renderDirectorsReport(exec) {
    const verdictCol  = exec.verdict==='ReadyToSubmit'?'var(--green)':exec.verdict==='ConditionallyReady'?'var(--amber)':'var(--red)';
    const verdictIcon = exec.verdict==='ReadyToSubmit'?'✅':exec.verdict==='ConditionallyReady'?'⚠️':'🚫';
    const gradeCol    = exec.quality
      ? exec.quality.overallGrade==='A'?'var(--green)':exec.quality.overallGrade==='B'?'var(--teal)':exec.quality.overallGrade==='C'?'var(--amber)':'var(--red)'
      : 'var(--mid-grey)';

    return `<div class="card" style="margin-bottom:16px;border-left:5px solid ${verdictCol}">

      <!-- Header -->
      <div style="display:flex;align-items:flex-start;justify-content:space-between;margin-bottom:16px;flex-wrap:wrap;gap:12px">
        <div style="flex:1">
          <div style="font-size:11px;font-weight:700;color:var(--mid-grey);text-transform:uppercase;letter-spacing:.8px">
            Director's Report - Singapore CORENET-X
          </div>
          <div style="font-size:20px;font-weight:900;color:${verdictCol};margin-top:4px">
            ${verdictIcon} ${VUtils.esc(exec.verdictMessage||'')}
          </div>
          <div style="font-size:12px;color:var(--mid-grey);margin-top:4px;max-width:620px">
            ${VUtils.esc(exec.verdictDetail||'')}
          </div>
        </div>
        <div style="text-align:center;flex-shrink:0">
          <div style="font-size:52px;font-weight:900;color:${verdictCol};line-height:1">${exec.overallScore||0}</div>
          <div style="font-size:11px;color:var(--mid-grey)">/ 100 score</div>
          ${exec.quality ? `<div style="font-size:14px;font-weight:700;color:${gradeCol}">Grade ${exec.quality.overallGrade}</div>` : ''}
        </div>
      </div>

      <!-- Model quality bars -->
      ${exec.quality ? renderQualityBars(exec.quality) : ''}

      <!-- Agency risk table -->
      ${exec.agencyRisk && exec.agencyRisk.length > 0 ? `
      <div style="margin-top:18px">
        <div style="font-size:11px;font-weight:700;color:var(--navy-dark);margin-bottom:8px;text-transform:uppercase;letter-spacing:.6px">
          Agency Risk Breakdown
        </div>
        ${renderAgencyRiskTable(exec.agencyRisk)}
      </div>` : ''}

      <!-- Top blockers -->
      ${exec.topBlockers && exec.topBlockers.length > 0 ? `
      <div style="margin-top:18px">
        <div style="font-size:11px;font-weight:700;color:var(--navy-dark);margin-bottom:8px;text-transform:uppercase;letter-spacing:.6px">
          Top ${exec.topBlockers.length} Blockers
        </div>
        ${renderTopBlockers(exec.topBlockers)}
      </div>` : ''}

      <!-- Action plan -->
      ${exec.actionPlan && exec.actionPlan.length > 0 ? `
      <div style="margin-top:18px">
        <div style="font-size:11px;font-weight:700;color:var(--navy-dark);margin-bottom:8px;text-transform:uppercase;letter-spacing:.6px">
          Recommended Action Plan
        </div>
        ${renderActionPlan(exec.actionPlan)}
      </div>` : ''}

      <!-- Effort estimate -->
      ${exec.effort ? `
      <div style="margin-top:16px;padding:12px;background:var(--border)40;border-radius:8px">
        <div style="font-size:11px;font-weight:700;color:var(--mid-grey);text-transform:uppercase;letter-spacing:.6px;margin-bottom:8px">
          Estimated Rework Effort
        </div>
        <div style="display:flex;gap:20px;flex-wrap:wrap;align-items:flex-end">
          <div>
            <div style="font-size:11px;color:var(--mid-grey)">Total</div>
            <div style="font-size:20px;font-weight:700;color:var(--navy-dark)">${VUtils.esc(exec.effort.total)}</div>
          </div>
          <div>
            <div style="font-size:11px;color:var(--mid-grey)">Critical issues</div>
            <div style="font-size:14px;font-weight:600;color:var(--red)">${VUtils.esc(exec.effort.blockerEffort)}</div>
          </div>
          <div>
            <div style="font-size:11px;color:var(--mid-grey)">Errors</div>
            <div style="font-size:14px;font-weight:600;color:var(--amber)">${VUtils.esc(exec.effort.errorEffort)}</div>
          </div>
          <div>
            <div style="font-size:11px;color:var(--mid-grey)">Warnings</div>
            <div style="font-size:14px;font-weight:600;color:var(--teal)">${VUtils.esc(exec.effort.warningEffort)}</div>
          </div>
          <div style="flex:1;min-width:160px">
            <div style="font-size:11px;color:var(--mid-grey)">${VUtils.esc(exec.effort.confidence)}</div>
            <div style="font-size:10px;color:var(--mid-grey);margin-top:2px">${VUtils.esc(exec.effort.note||'')}</div>
          </div>
        </div>
      </div>` : ''}

      <!-- Gateway readiness -->
      ${exec.gatewayStatus ? renderGatewayReadiness(exec.gatewayStatus) : ''}

      <div style="margin-top:14px;display:flex;gap:8px;flex-wrap:wrap">
        <button class="btn btn-teal" onclick="App.navigate('critical')">🚨 Critical Issues</button>
        <button class="btn btn-outline" onclick="VBridge.send('export',{})">📤 Export Report</button>
        <button class="btn btn-ghost" onclick="DashboardPage.runCheckAll()">🔄 Re-run Check All</button>
      </div>
    </div>`;
  }

  function renderQualityBars(q) {
    const bars = [
      ['Classification Coverage', q.classificationCoverage, 'var(--teal)'],
      ['Property Set Coverage',   q.propertySetCoverage,    'var(--navy)'],
      ['Property Value Coverage', q.propertyValueCoverage,  'var(--amber)'],
      ['Geometry Health',         q.geometryHealth,         'var(--green)'],
      ['Naming Convention',       q.namingConventionScore,  'var(--mid-grey)'],
    ];
    return `<div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(190px,1fr));gap:10px">
      ${bars.map(([label, pct, col]) => `
        <div>
          <div style="display:flex;justify-content:space-between;font-size:11px;margin-bottom:3px">
            <span style="color:var(--mid-grey)">${label}</span>
            <span style="font-weight:700;color:${col}">${pct}%</span>
          </div>
          <div style="background:var(--border);border-radius:3px;height:5px">
            <div style="background:${col};height:100%;width:${Math.min(100,pct)}%;border-radius:3px"></div>
          </div>
        </div>`).join('')}
    </div>`;
  }

  function renderAgencyRiskTable(agencyRisk) {
    const riskCol = { HIGH:'var(--red)', MEDIUM:'var(--amber)', LOW:'#F59E0B', CLEAR:'var(--green)' };
    const riskBg  = { HIGH:'#FEF2F2',   MEDIUM:'#FFFBEB',      LOW:'#FEFCE8', CLEAR:'#F0FDF4' };
    return `<div style="overflow-x:auto">
      <table style="width:100%;border-collapse:collapse;font-size:12px">
        <thead>
          <tr style="border-bottom:2px solid var(--border)">
            <th style="text-align:left;padding:6px 8px;font-size:11px;color:var(--mid-grey)">Agency</th>
            <th style="text-align:center;padding:6px;font-size:11px;color:var(--mid-grey)">Risk</th>
            <th style="text-align:center;padding:6px;font-size:11px;color:var(--red)">Critical</th>
            <th style="text-align:center;padding:6px;font-size:11px;color:var(--amber)">Errors</th>
            <th style="text-align:center;padding:6px;font-size:11px;color:var(--mid-grey)">Warnings</th>
            <th style="text-align:left;padding:6px 8px;font-size:11px;color:var(--mid-grey)">Recommended Fix</th>
          </tr>
        </thead>
        <tbody>
          ${agencyRisk.map(a => `
            <tr style="border-bottom:1px solid var(--border);background:${riskBg[a.riskLevel]||'transparent'}">
              <td style="padding:7px 8px">
                <div style="font-weight:700;color:var(--navy-dark)">${VUtils.esc(a.agency)}</div>
                <div style="font-size:10px;color:var(--mid-grey)">${VUtils.esc(a.agencyFullName)}</div>
              </td>
              <td style="text-align:center;padding:7px 6px">
                <span style="font-size:10px;font-weight:700;color:${riskCol[a.riskLevel]||'var(--mid-grey)'};
                  border:1px solid ${riskCol[a.riskLevel]||'var(--border)'};border-radius:4px;padding:2px 6px">
                  ${VUtils.esc(a.riskLevel)}
                </span>
              </td>
              <td style="text-align:center;padding:7px 6px;font-weight:${a.blockerCount>0?'700':'400'};color:${a.blockerCount>0?'var(--red)':'var(--mid-grey)'}">
                ${a.blockerCount>0?a.blockerCount:'-'}
              </td>
              <td style="text-align:center;padding:7px 6px;font-weight:${a.errorCount>0?'700':'400'};color:${a.errorCount>0?'var(--amber)':'var(--mid-grey)'}">
                ${a.errorCount>0?a.errorCount:'-'}
              </td>
              <td style="text-align:center;padding:7px 6px;color:var(--mid-grey)">
                ${a.warningCount>0?a.warningCount:'-'}
              </td>
              <td style="padding:7px 8px;font-size:11px;color:var(--mid-grey);max-width:200px">
                <div style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap"
                  title="${VUtils.esc(a.recommendedFix)}">
                  ${VUtils.esc((a.recommendedFix||'').substring(0,70))}${(a.recommendedFix||'').length>70?'…':''}
                </div>
              </td>
            </tr>`).join('')}
        </tbody>
      </table>
    </div>`;
  }

  function renderTopBlockers(blockers) {
    return blockers.map(b => `
      <div style="display:flex;gap:10px;padding:9px 0;border-bottom:1px solid var(--border)">
        <div style="width:24px;height:24px;border-radius:50%;flex-shrink:0;
          background:${b.severity==='Critical'?'var(--red)':'var(--amber)'};
          color:white;font-size:11px;font-weight:700;display:flex;align-items:center;justify-content:center">
          ${b.rank}
        </div>
        <div style="flex:1;min-width:0">
          <div style="font-size:12px;font-weight:600;color:var(--navy-dark)">
            ${VUtils.esc(b.checkLevel)}
            <span style="font-size:10px;font-weight:400;color:var(--mid-grey);margin-left:6px">${VUtils.esc(b.agency)}</span>
            <span style="font-size:10px;font-weight:700;color:var(--red);margin-left:6px">${b.affectedCount} elements</span>
          </div>
          <div style="font-size:11px;color:var(--mid-grey);margin-top:2px">
            ${VUtils.esc((b.issue||'').substring(0,130))}${(b.issue||'').length>130?'…':''}
          </div>
          <div style="font-size:11px;color:var(--teal);margin-top:2px;font-style:italic">
            Fix: ${VUtils.esc((b.fix||'').substring(0,120))}${(b.fix||'').length>120?'…':''}
          </div>
        </div>
      </div>`).join('');
  }

  function renderActionPlan(actions) {
    const bgMap = { 1:'var(--navy)12', 2:'var(--red)08', 3:'var(--amber)08' };
    const dotMap = { 1:'var(--navy-dark)', 2:'var(--red)', 3:'var(--amber)' };
    return `<div style="display:flex;flex-direction:column;gap:8px">
      ${actions.map(a => `
        <div style="display:flex;gap:12px;padding:10px 12px;border-radius:8px;background:${bgMap[a.priority]||'var(--border)30'}">
          <div style="width:28px;height:28px;border-radius:50%;flex-shrink:0;
            background:${dotMap[a.priority]||'var(--mid-grey)'};
            color:white;font-size:12px;font-weight:700;display:flex;align-items:center;justify-content:center">
            ${a.priority}
          </div>
          <div style="flex:1">
            <div style="font-size:13px;font-weight:700;color:var(--navy-dark)">${VUtils.esc(a.action)}</div>
            <div style="font-size:11px;color:var(--mid-grey);margin-top:3px">${VUtils.esc(a.why)}</div>
            <div style="display:flex;gap:12px;margin-top:5px;font-size:11px;flex-wrap:wrap">
              <span style="color:var(--teal);font-weight:600">⏱ ${VUtils.esc(a.estimatedTime)}</span>
              ${a.issuesResolved>0?`<span style="color:var(--green);font-weight:600">✓ Resolves ${a.issuesResolved} issue(s)</span>`:''}
              <span style="color:var(--mid-grey)">${VUtils.esc(a.agency)}</span>
            </div>
          </div>
        </div>`).join('')}
    </div>`;
  }

  function renderGatewayReadiness(gw) {
    const gate = (ready, label) =>
      `<div style="display:flex;align-items:center;gap:6px;font-size:12px">
        <span>${ready?'✅':'🚫'}</span>
        <span style="color:var(--navy-dark);font-weight:${ready?'400':'600'}">${label}</span>
      </div>`;
    return `<div style="margin-top:14px;padding:12px;background:var(--border)40;border-radius:8px">
      <div style="font-size:11px;font-weight:700;color:var(--mid-grey);text-transform:uppercase;letter-spacing:.6px;margin-bottom:8px">
        CORENET-X Gateway Readiness
      </div>
      <div style="display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:6px">
        ${gate(gw.designGateway,       'Gateway 1 - Design')}
        ${gate(gw.pilingGateway,       'Piling Gateway')}
        ${gate(gw.constructionGateway, 'Gateway 2 - Construction')}
        ${gate(gw.completionGateway,   'Gateway 3 - Completion')}
      </div>
      <div style="font-size:11px;color:var(--teal);margin-top:8px;font-weight:600">${VUtils.esc(gw.recommendedGateway)}</div>
      <div style="font-size:10px;color:var(--mid-grey);margin-top:3px">${VUtils.esc(gw.gatewayNote)}</div>
    </div>`;
  }

  // ── Health Score banner ───────────────────────────────────────────────────
  function renderHealthBanner(session, dc) {
    const dataScore   = session.score || 0;
    const designScore = dc ? (dc.score||0) : null;
    const overall     = designScore !== null ? (dataScore + designScore)/2 : dataScore;
    const grade       = overall>=90?'A':overall>=75?'B':overall>=60?'C':overall>=40?'D':'F';
    const gradeCol    = overall>=90?'var(--green)':overall>=60?'var(--amber)':'var(--red)';
    const label       = overall>=90?'Excellent':overall>=75?'Good':overall>=60?'Fair':overall>=40?'Poor':'Critical';

    return `<div class="card" style="margin-bottom:16px;background:linear-gradient(135deg,var(--navy-dark),var(--navy));color:white;border:none">
      <div style="display:flex;align-items:center;gap:24px;flex-wrap:wrap">
        <div style="text-align:center;flex-shrink:0">
          <div style="font-size:52px;font-weight:900;color:${gradeCol};line-height:1">${grade}</div>
          <div style="font-size:11px;color:#93C5FD;margin-top:2px">${label}</div>
        </div>
        <div style="flex:1;min-width:200px">
          <div style="font-size:16px;font-weight:700;margin-bottom:10px">
            Overall: <span style="color:${gradeCol}">${overall.toFixed(1)}%</span>
          </div>
          ${scoreBar('Data Compliance', dataScore, '#38BDF8')}
          ${designScore!==null?scoreBar('Design Code', designScore, '#A78BFA'):''}
        </div>
        <div style="display:flex;flex-direction:column;gap:8px;flex-shrink:0">
          <button class="btn" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3)"
            onclick="App.navigate('results')">📋 All Findings</button>
          <button class="btn" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3)"
            onclick="App.navigate('critical')">🚨 Critical Issues</button>
          <button class="btn" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3)"
            onclick="VBridge.send('export',{})">📤 Export</button>
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
    const colMap={teal:'var(--teal)',red:'var(--red)',amber:'var(--amber)',green:'var(--green)'};
    return `<div class="stat-card">
      <div class="stat-val" style="color:${colMap[colour]||'var(--navy-dark)'}">${value}</div>
      <div class="stat-lbl">${label}</div>
    </div>`;
  }

  function renderAgencyChart(session) {
    const byAgency = session.errorsByAgency || {};
    const entries  = Object.entries(byAgency).sort((a,b)=>b[1]-a[1]);
    const maxVal   = entries.length ? Math.max(...entries.map(e=>e[1])) : 1;
    const cols = {BCA:'#1D4ED8',URA:'#15803D',SCDF:'#B91C1C',LTA:'#B45309',NEA:'#6D28D9',NParks:'#065F46',PUB:'#0284C7',SLA:'#92400E'};
    const bars = entries.slice(0,8).map(([ag,n])=>{
      const pct=maxVal>0?n/maxVal*100:0; const col=cols[ag]||'#6B7280';
      return `<div style="display:flex;align-items:center;gap:8px;margin-bottom:7px">
        <div style="width:50px;font-size:11px;font-weight:700;color:${col};text-align:right;flex-shrink:0">${VUtils.esc(ag)}</div>
        <div style="flex:1;background:var(--border);border-radius:3px;height:18px;overflow:hidden">
          <div style="background:${col};height:100%;width:${pct}%;border-radius:3px;
               display:flex;align-items:center;padding-left:6px;transition:width .6s ease">
            <span style="font-size:10px;color:white;font-weight:700">${n}</span>
          </div>
        </div>
      </div>`;
    }).join('');
    return `<div class="card">
      <div class="card-header"><span class="card-title">🏛 Issues by Agency</span></div>
      ${entries.length===0?'<div style="color:var(--mid-grey);font-size:12px;text-align:center;padding:20px">No agency errors</div>':bars}
    </div>`;
  }

  function renderQuickFixes(session) {
    const findings=(session.findings||[]).filter(f=>f.severity==='Critical'||f.severity==='Error');
    const byCheck={};
    findings.forEach(f=>{if(!byCheck[f.check])byCheck[f.check]={check:f.check,count:0,fix:f.fix};byCheck[f.check].count++;});
    const top5=Object.values(byCheck).sort((a,b)=>b.count-a.count).slice(0,5);
    const items=top5.map((item,i)=>`
      <div style="display:flex;gap:10px;padding:8px 0;border-bottom:1px solid var(--border)">
        <div style="width:22px;height:22px;border-radius:50%;background:var(--navy-dark);color:white;
          font-size:11px;font-weight:700;display:flex;align-items:center;justify-content:center;flex-shrink:0">${i+1}</div>
        <div style="flex:1;min-width:0">
          <div style="font-size:12px;font-weight:600;color:var(--navy-dark)">${VUtils.esc(item.check)} <span style="color:var(--red)">(${item.count})</span></div>
          <div style="font-size:11px;color:var(--mid-grey);margin-top:2px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap"
            title="${VUtils.esc(item.fix)}">${VUtils.esc((item.fix||'').substring(0,80))}${(item.fix||'').length>80?'…':''}</div>
        </div>
      </div>`).join('');
    return `<div class="card">
      <div class="card-header"><span class="card-title">⚡ Top 5 Quick Fixes</span></div>
      ${top5.length===0?'<div style="color:var(--mid-grey);font-size:12px;text-align:center;padding:20px">No critical findings</div>':
        `<div>${items}</div><button class="btn btn-teal" style="margin-top:12px;width:100%" onclick="App.navigate('critical')">View All Critical Issues →</button>`}
    </div>`;
  }

  function renderFilesSummary(files, session) {
    if(!files.length) return '<div></div>';
    const byType={};
    (session.findings||[]).forEach(f=>{if(!byType[f.cls])byType[f.cls]=0;byType[f.cls]++;});
    const topTypes=Object.entries(byType).sort((a,b)=>b[1]-a[1]).slice(0,6);
    return `<div class="card">
      <div class="card-header"><span class="card-title">📁 Loaded Files</span></div>
      ${files.map(f=>`
        <div style="padding:8px 0;border-bottom:1px solid var(--border);display:flex;justify-content:space-between;align-items:center">
          <div>
            <div style="font-size:13px;font-weight:600">${VUtils.esc(f.name)}</div>
            <div style="font-size:11px;color:var(--mid-grey)">${VUtils.esc(f.schema)} · ${VUtils.fmt(f.elements)} elements
              ${f.proxies>0?`· <span style="color:var(--amber)">⚠ ${f.proxies} proxy</span>`:''}
            </div>
          </div>
          <button class="btn btn-ghost" style="font-size:11px;padding:3px 10px"
            onclick="App.navigate('3d');setTimeout(()=>{if(window.Viewer3DPage)Viewer3DPage.loadFile('${VUtils.esc(f.name)}')},400)">🧊 3D</button>
        </div>`).join('')}
      ${topTypes.length>0?`
        <div style="margin-top:10px">
          <div style="font-size:11px;font-weight:600;color:var(--mid-grey);margin-bottom:6px;text-transform:uppercase;letter-spacing:.5px">Most Issues By Type</div>
          ${topTypes.map(([cls,n])=>`
            <div style="display:flex;justify-content:space-between;font-size:11px;padding:2px 0">
              <span style="color:var(--mid-grey)">${VUtils.esc(cls)}</span>
              <span style="font-weight:700;color:var(--red)">${n}</span>
            </div>`).join('')}
        </div>`:''}
    </div>`;
  }

  function renderDesignSummary(dc) {
    if(!dc) return '<div></div>';
    const catEntries=Object.entries(dc.failsByCategory||{}).sort((a,b)=>b[1]-a[1]).slice(0,6);
    return `<div class="card">
      <div class="card-header">
        <span class="card-title">📐 Design Code</span>
        <span style="font-size:12px;font-weight:600;color:${dc.score>=70?'var(--green)':'var(--red)'}">${(dc.score||0).toFixed(1)}% pass</span>
      </div>
      <div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:10px">
        ${kpi(VUtils.fmt(dc.total||0),'Checks','teal')}${kpi(VUtils.fmt(dc.passed||0),'Passed','green')}
        ${kpi(VUtils.fmt(dc.failed||0),'Failed','red')}${kpi(VUtils.fmt(dc.critical||0),'Critical','red')}
      </div>
      ${catEntries.length>0?`
        <div style="font-size:11px;font-weight:600;color:var(--mid-grey);margin-bottom:6px;text-transform:uppercase;letter-spacing:.5px">By Category</div>
        ${catEntries.map(([cat,n])=>`
          <div style="display:flex;justify-content:space-between;font-size:11px;padding:2px 0">
            <span style="color:var(--mid-grey)">${VUtils.esc(cat.replace('_',' '))}</span>
            <span style="font-weight:700;color:var(--amber)">${n} fails</span>
          </div>`).join('')}`:''}
      <button class="btn btn-outline" style="margin-top:10px;width:100%;font-size:12px" onclick="App.navigate('design')">
        View Design Code Findings →
      </button>
    </div>`;
  }

  return { render, runCheckAll, onExecutiveSummary };
})();

window.DashboardPage = DashboardPage;
