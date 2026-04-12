// VERIFIQ - Results Page UI
// Copyright 2026 BBMW0 Technologies. All rights reserved.

'use strict';

const ResultsPage = (() => {

  function render(filterFn) {
    const state   = VState.get();
    const session = state.session;
    const mode    = state.countryMode;

    const isCritical = filterFn !== undefined;
    const pageTitle  = isCritical ? 'Critical Issues' : 'All Compliance Findings';

    if (!session || !session.findings) {
      const emptyMsg = isCritical
        ? 'Run validation on a loaded IFC file to see critical issues and errors.'
        : 'Run validation on a loaded IFC file to see all compliance findings.';
      const emptyIcon = isCritical ? '🚨' : '📋';
      const emptyTitle = isCritical ? 'No critical issues yet' : 'No results yet';
      return `
        <div>
          <h1>${pageTitle}</h1>
          <p style="color:var(--mid-grey);font-size:13px;margin-bottom:16px">
            ${isCritical
              ? 'Critical and Error findings causing CORENET-X / NBeS rejection. Click ✏️ Fix on any property finding to queue it in the IFC Property Editor.'
              : 'All findings across 20 IFC data levels + 128 classification code rules + 89 SG + 60 MY design code rules. Click ✏️ Fix on any property set finding to open the Property Editor.'}
          </p>
          ${VUtils.emptyState(emptyIcon, emptyTitle, emptyMsg,
              '<button class="btn btn-primary" style="margin-top:16px" onclick="VBridge.openFile()">📂 Open IFC File</button>')}
        </div>`;
    }

    const findings = filterFn
      ? session.findings.filter(filterFn)
      : session.findings;

    const title = isCritical
      ? `Critical Issues (${findings.length})`
      : `All Compliance Findings (${findings.length})`;

    // Unique agencies for filter (Singapore only)
    const agencies = mode !== 'Malaysia'
      ? [...new Set(findings.map(f => f.agency).filter(a => a && a !== 'None'))]
      : [];

    // Unique check levels
    const checks = [...new Set(findings.map(f => f.check))].sort();

    const rows = findings.map(f => `
      <tr class="${VUtils.rowClass(f.severity)}"
          data-severity="${VUtils.esc(f.severity)}"
          data-agency="${VUtils.esc(f.agency)}"
          data-check="${VUtils.esc(f.check)}">
        <td>${VUtils.severityBadge(f.severity)}</td>
        <td style="font-size:11px;white-space:nowrap">${VUtils.esc(f.check)}</td>
        <td>
          <strong>${VUtils.esc(f.name)}</strong>
          <div style="font-size:11px;color:var(--mid-grey)">${VUtils.esc(f.cls)}</div>
        </td>
        <td class="guid" title="${VUtils.esc(f.guid)}">${VUtils.shortGuid(f.guid)}</td>
        <td style="font-size:11px">${VUtils.esc(f.storey)}</td>
        <td>${VUtils.agencyBadge(f.agency)}</td>
        <td style="font-family:monospace;font-size:11px">
          ${f.pset ? `${VUtils.esc(f.pset)}.<br>${VUtils.esc(f.prop)}` : '-'}
        </td>
        <td style="max-width:220px;font-size:12px">${VUtils.esc(f.message)}</td>
        <td class="cell-wrap" style="font-size:12px;color:var(--teal)">${VUtils.esc(f.fix)}</td>
        <td>
          ${f.pset && f.prop && f.severity !== 'Pass' ? `
          <button class="btn btn-ghost" style="font-size:10px;padding:2px 8px;white-space:nowrap;color:var(--teal);border-color:var(--teal)"
            onclick="PropertyEditor.addToQueue(${JSON.stringify({
              stepId: f.stepId||0,
              pset: f.pset, prop: f.prop, guid: f.guid,
              message: f.message, fix: f.fix, severity: f.severity
            }).replace(/"/g,'&quot;')});PropertyEditor.showPanel();App.navigate('propertyeditor')">
            ✏️ Fix
          </button>` : ''}
        </td>
      </tr>`).join('');

    const agencyOptions = agencies.map(a =>
      `<option value="${VUtils.esc(a)}">${VUtils.esc(a)}</option>`
    ).join('');

    const checkOptions = checks.map(c =>
      `<option value="${VUtils.esc(c)}">${VUtils.esc(c)}</option>`
    ).join('');

    return `
      <div>
        <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px">
          <h1>${title}</h1>
          <button class="btn btn-outline" onclick="VBridge.send('export',{})">📤 Export</button>
        </div>

        <!-- Filter bar -->
        <div class="filter-bar">
          <select id="sev-filter" onchange="ResultsPage.applyFilters()">
            <option value="">All Severities</option>
            <option value="Critical">Critical</option>
            <option value="Error">Error</option>
            <option value="Warning">Warning</option>
            <option value="Pass">Pass</option>
          </select>
          ${agencies.length > 0 ? `
          <select id="agency-filter" onchange="ResultsPage.applyFilters()">
            <option value="">All Agencies</option>
            ${agencyOptions}
          </select>` : ''}
          <select id="check-filter" onchange="ResultsPage.applyFilters()">
            <option value="">All Check Levels</option>
            ${checkOptions}
          </select>
          <input id="search-filter" placeholder="Search element name, GUID, message..."
                 oninput="ResultsPage.applySearch()"/>
          <button class="btn btn-ghost" onclick="ResultsPage.clearFilters()">Clear</button>
        </div>

        <!-- Results table -->
        <div class="table-wrap">
          <table id="findings-table">
            <thead><tr>
              <th>Severity</th>
              <th>Check Level</th>
              <th>Element</th>
              <th>GUID</th>
              <th>Storey</th>
              ${mode !== 'Malaysia' ? '<th>Agency</th>' : ''}
              <th>Property</th>
              <th>Issue</th>
              <th>Remediation</th>
            </tr></thead>
            <tbody>${rows}</tbody>
          </table>
        </div>

        <div style="margin-top:10px;font-size:11px;color:var(--light-grey)">
          Showing first ${findings.length} findings.
          ${session.findings.length > findings.length
            ? `${session.findings.length - findings.length} additional findings not shown.`
            : ''}
          Export reports for the complete dataset.
        </div>
      </div>`;
  }

  function applyFilters() {
    const sev    = (document.getElementById('sev-filter')?.value    || '').toLowerCase();
    const agency = (document.getElementById('agency-filter')?.value || '').toLowerCase();
    const check  = (document.getElementById('check-filter')?.value  || '').toLowerCase();

    document.querySelectorAll('#findings-table tbody tr').forEach(row => {
      const rowSev    = (row.dataset.severity || '').toLowerCase();
      const rowAgency = (row.dataset.agency   || '').toLowerCase();
      const rowCheck  = (row.dataset.check    || '').toLowerCase();

      const ok = (!sev    || rowSev    === sev)
              && (!agency || rowAgency === agency)
              && (!check  || rowCheck  === check);

      row.style.display = ok ? '' : 'none';
    });
  }

  function applySearch() {
    const q = (document.getElementById('search-filter')?.value || '').toLowerCase();
    VUtils.searchTable('findings-table', q);
  }

  function clearFilters() {
    ['sev-filter','agency-filter','check-filter','search-filter']
      .forEach(id => { const el = document.getElementById(id); if (el) el.value = ''; });
    document.querySelectorAll('#findings-table tbody tr')
      .forEach(row => { row.style.display = ''; });
  }

  // Critical issues view - pre-filtered to Critical+Error only
  function renderCritical() {
    return render(f => f.severity === 'Critical' || f.severity === 'Error');
  }

  // ── DESIGN CODE RESULTS PAGE ───────────────────────────────────────────────

  function renderDesignCode() {
    const state   = VState.get();
    const session = state.session;

    if (!session || !session.designStats) return `
      <div>
        <h1>Design Code Compliance</h1>
        ${VUtils.emptyState('📐', 'No design code results yet',
          'Run validation to check actual dimensions, areas and distances against regulatory requirements.',
          '<button class="btn btn-teal" style="margin-top:16px" onclick="VBridge.runValidation()">▶ Run Validation</button>')}
      </div>`;

    const dc       = session.designStats;
    const findings = session.designFindings || [];
    const mode     = state.countryMode;

    // Unique categories and severities
    const categories = [...new Set(findings.map(f => f.category).filter(Boolean))].sort();
    const catOptions  = categories.map(c =>
      `<option value="${VUtils.esc(c)}">${VUtils.esc(c.replace(/([A-Z])/g,' $1').trim())}</option>`
    ).join('');

    // Group failures by code reference for the code-grouped view
    const byCode = {};
    findings.filter(f => !f.complies).forEach(f => {
      if (!byCode[f.codeRef]) byCode[f.codeRef] = [];
      byCode[f.codeRef].push(f);
    });

    const rows = findings.map(f => `
      <tr class="${f.complies ? '' : f.severity === 'Critical' ? 'row-critical' : 'row-error'}"
          data-sev="${VUtils.esc(f.severity)}" data-complies="${f.complies}"
          data-cat="${VUtils.esc(f.category || '')}"
          data-search="${VUtils.esc((f.ruleId + ' ' + f.name + ' ' + f.codeRef).toLowerCase())}">
        <td>${VUtils.severityBadge(f.complies ? 'Pass' : f.severity)}</td>
        <td class="guid" style="font-size:11px">${VUtils.esc(f.ruleId)}</td>
        <td style="font-size:11px;max-width:200px">${VUtils.esc(f.ruleName)}</td>
        <td style="font-size:11px;max-width:180px;color:var(--mid-grey)">${VUtils.esc(f.codeRef)}</td>
        <td><strong>${VUtils.esc(f.name)}</strong><br><small class="grey">${VUtils.esc(f.cls)}</small></td>
        <td><span style="font-family:monospace;font-size:12px;font-weight:600;color:${f.complies?'var(--green)':'var(--red)'}">${VUtils.esc(f.actual)}</span></td>
        <td><span style="font-family:monospace;font-size:11px">${VUtils.esc(f.required)}</span></td>
        <td style="font-family:monospace;font-size:10px;color:var(--mid-grey)">${VUtils.esc(f.formula)}</td>
        <td style="font-family:monospace;font-size:10px;font-weight:600;color:${f.complies?'var(--green)':'var(--red)'}">${VUtils.esc(f.result)}</td>
        <td style="font-size:11px;color:var(--teal)">${f.complies ? '-' : VUtils.esc(f.fix)}</td>
      </tr>`).join('');

    return `<div>
      <div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:16px">
        <h1>Design Code Compliance</h1>
        <button class="btn btn-outline" onclick="VBridge.send('export',{})">📤 Export</button>
      </div>

      <!-- Score formula -->
      <div class="card" style="margin-bottom:16px">
        <div style="font-family:'Courier New',monospace;font-size:12px;background:#F0F9FF;padding:12px 16px;border-radius:6px;border:1px solid #BAE6FD;line-height:2">
          <b>Design Score</b> = (${VUtils.fmt(dc.passed)} Passed ÷ ${VUtils.fmt(dc.total)} Total Checks) × 100
          = <b style="font-size:20px;color:var(--${VUtils.scoreColour(dc.score) === 'green' ? 'green' : VUtils.scoreColour(dc.score) === 'amber' ? 'amber' : 'red'})">${VUtils.pct(dc.score)}</b>
          &nbsp; | &nbsp; ${VUtils.fmt(dc.failed)} failures &nbsp; | &nbsp; ${VUtils.fmt(dc.critical)} critical
        </div>
      </div>

      <!-- Stat cards -->
      <div class="stat-grid" style="margin-bottom:16px">
        ${VUtils.statCard(VUtils.pct(dc.score),     'Design Score',   VUtils.scoreColour(dc.score))}
        ${VUtils.statCard(VUtils.fmt(dc.total),     'Total Checks')}
        ${VUtils.statCard(VUtils.fmt(dc.passed),    'Passed',         'green')}
        ${VUtils.statCard(VUtils.fmt(dc.failed),    'Failed',         'amber')}
        ${VUtils.statCard(VUtils.fmt(dc.critical),  'Critical',       'red')}
      </div>

      <!-- Code-grouped failures summary -->
      ${Object.keys(byCode).length > 0 ? `
      <div class="card" style="margin-bottom:16px">
        <div class="card-header"><span class="card-title">Failures by Code Reference</span></div>
        <div class="table-wrap"><table><thead><tr>
          <th>Code Reference</th><th>Failures</th><th>Sample Element</th>
        </tr></thead><tbody>
        ${Object.entries(byCode).sort((a,b) => b[1].length - a[1].length).map(([code, items]) => `
          <tr>
            <td style="font-size:11px;font-weight:600">${VUtils.esc(code)}</td>
            <td style="font-weight:700;color:var(--red)">${items.length}</td>
            <td style="font-size:11px">${VUtils.esc(items[0].name)} - ${VUtils.esc(items[0].message)}</td>
          </tr>`).join('')}
        </tbody></table></div>
      </div>` : ''}

      <!-- Full findings table -->
      <h2>All Design Code Checks</h2>
      <div class="filter-bar">
        <select id="dc-sev-f" onchange="ResultsPage.applyDesignFilters()">
          <option value="">All Results</option>
          <option value="false">Failures Only</option>
          <option value="true">Passes Only</option>
        </select>
        <select id="dc-sev-s" onchange="ResultsPage.applyDesignFilters()">
          <option value="">All Severities</option>
          <option>Critical</option><option>Error</option><option>Warning</option><option>Pass</option>
        </select>
        ${catOptions ? `<select id="dc-cat-f" onchange="ResultsPage.applyDesignFilters()">
          <option value="">All Categories</option>${catOptions}
        </select>` : ''}
        <input id="dc-search-f" placeholder="Rule ID, element, code reference..."
               oninput="ResultsPage.applyDesignFilters()"/>
        <button class="btn btn-ghost" onclick="ResultsPage.clearDesignFilters()">Clear</button>
      </div>

      <div class="table-wrap">
        <table id="design-table">
          <thead><tr>
            <th>Result</th><th>Rule ID</th><th>Rule Name</th><th>Code Reference</th>
            <th>Element</th><th>Actual Value</th><th>Required</th>
            <th>Formula</th><th>Formula Result</th><th>Remediation</th>
          </tr></thead>
          <tbody>${rows}</tbody>
        </table>
      </div>
      <div style="margin-top:10px;font-size:11px;color:var(--light-grey)">
        Showing ${findings.length} checks. Export for full dataset.
      </div>
    </div>`;
  }

  function applyDesignFilters() {
    const sev     = (document.getElementById('dc-sev-s')?.value || '').toLowerCase();
    const comp    = document.getElementById('dc-sev-f')?.value || '';
    const cat     = (document.getElementById('dc-cat-f')?.value || '').toLowerCase();
    const q       = (document.getElementById('dc-search-f')?.value || '').toLowerCase();
    document.querySelectorAll('#design-table tbody tr').forEach(row => {
      const ok = (!sev  || (row.dataset.sev  || '').toLowerCase() === sev)
              && (!comp || (row.dataset.complies || '') === comp)
              && (!cat  || (row.dataset.cat  || '').toLowerCase().includes(cat))
              && (!q    || (row.dataset.search || '').includes(q));
      row.style.display = ok ? '' : 'none';
    });
  }

  function clearDesignFilters() {
    ['dc-sev-f','dc-sev-s','dc-cat-f','dc-search-f']
      .forEach(id => { const el = document.getElementById(id); if (el) el.value = ''; });
    document.querySelectorAll('#design-table tbody tr').forEach(r => { r.style.display = ''; });
  }

  return { render, renderCritical, renderDesignCode, applyFilters, applySearch, clearFilters, applyDesignFilters, clearDesignFilters };
})();

window.ResultsPage = ResultsPage;

// ─── IFC PROPERTY EDITOR ──────────────────────────────────────────────────────
// Allows fixing missing or incorrect IFC property values directly in VERIFIQ
// without returning to ArchiCAD, Revit, or Tekla.
// The corrected model is saved as a new file next to the original.

const PropertyEditor = (() => {

  // Current edit queue - list of {stepId, psetName, propName, dataType, newValue, guid, display}
  let _queue = [];
  let _visible = false;

  // ── Public API ────────────────────────────────────────────────────────────

  function addToQueue(finding) {
    // finding must have: pset, prop, guid, message, fix
    if (!finding.pset || !finding.prop) return;
    const key = `${finding.guid}|${finding.pset}|${finding.prop}`;
    if (_queue.find(e => e.key === key)) return;  // already in queue
    _queue.push({
      key,
      stepId:   finding.stepId || 0,
      psetName: finding.pset,
      propName: finding.prop,
      dataType: inferDataType(finding.prop),
      newValue: '',
      guid:     finding.guid || '',
      display:  `${finding.pset}.${finding.prop}`,
      fix:      finding.fix || ''
    });
    renderPanel();
  }

  function clearQueue() {
    _queue = [];
    renderPanel();
  }

  function togglePanel() {
    _visible = !_visible;
    const panel = document.getElementById('prop-editor-panel');
    if (panel) panel.style.display = _visible ? 'block' : 'none';
  }

  function showPanel() {
    _visible = true;
    renderPanel();
  }

  function onResult(data) {
    const panel = document.getElementById('prop-editor-result');
    if (!panel) return;
    if (data.success) {
      panel.innerHTML = `
        <div style="padding:12px;background:#F0FDF4;border:1px solid #86EFAC;border-radius:8px;margin-top:12px">
          <div style="font-weight:700;color:#15803D;margin-bottom:6px">
            ✅ ${data.editsApplied} property edit(s) applied successfully
          </div>
          <div style="font-size:12px;color:var(--mid-grey)">
            Corrected file saved: <strong>${VUtils.esc(data.outputFile)}</strong><br>
            Edit log: <strong>${VUtils.esc(data.logPath)}</strong>
          </div>
          <div style="margin-top:8px;font-size:11px;color:var(--mid-grey)">
            ${(data.applied||[]).map(a => `<div>• ${VUtils.esc(a)}</div>`).join('')}
          </div>
          ${(data.errors||[]).length > 0 ? `
            <div style="margin-top:8px;font-size:11px;color:var(--amber)">
              ${(data.errors||[]).map(e => `<div>⚠ ${VUtils.esc(e)}</div>`).join('')}
            </div>` : ''}
          <div style="margin-top:10px;font-size:12px;padding:8px;background:#DCFCE7;border-radius:6px">
            <strong>Next step:</strong> Open the corrected file in VERIFIQ to re-validate and confirm all issues are resolved,
            then use this corrected IFC for your CORENET-X submission.
          </div>
        </div>`;
      _queue = [];  // clear after success
    } else {
      panel.innerHTML = `
        <div style="padding:12px;background:#FEF2F2;border:1px solid #FCA5A5;border-radius:8px;margin-top:12px">
          <div style="font-weight:700;color:#B91C1C">Edit failed: ${VUtils.esc(data.error || 'Unknown error')}</div>
        </div>`;
    }
  }

  // ── Render ────────────────────────────────────────────────────────────────

  function renderPanel() {
    let container = document.getElementById('prop-editor-panel');
    if (!container) return;

    if (_queue.length === 0) {
      container.innerHTML = `
        <div class="card" style="margin-top:16px">
          <div class="card-header">
            <span class="card-title">✏️ IFC Property Editor</span>
          </div>
          <div style="font-size:12px;color:var(--mid-grey);padding:8px 0">
            Click <strong>Fix Property</strong> on any finding in the results table to add it here.
            You can fix multiple properties at once and save a single corrected IFC file.
          </div>
        </div>`;
      return;
    }

    const rows = _queue.map((edit, i) => `
      <tr>
        <td style="font-size:11px;font-family:monospace">${VUtils.esc(edit.display)}</td>
        <td style="font-size:11px;color:var(--mid-grey);font-size:10px">${VUtils.esc(edit.guid.substring(0,12))}...</td>
        <td>
          <input type="text" value="${VUtils.esc(edit.newValue)}"
            placeholder="${getPlaceholder(edit)}"
            style="width:180px;padding:4px 8px;border:1px solid var(--border);border-radius:4px;
                   font-family:Courier New,monospace;font-size:12px"
            onchange="PropertyEditor.updateValue(${i}, this.value)"
            oninput="PropertyEditor.updateValue(${i}, this.value)"/>
          <span style="font-size:10px;color:var(--mid-grey);margin-left:4px">${VUtils.esc(edit.dataType)}</span>
        </td>
        <td style="font-size:10px;color:var(--teal);max-width:160px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap"
            title="${VUtils.esc(edit.fix)}">
          ${VUtils.esc(edit.fix.substring(0,60))}
        </td>
        <td>
          <button class="btn btn-ghost" style="font-size:11px;padding:2px 8px;color:var(--red)"
            onclick="PropertyEditor.removeFromQueue(${i})">✕</button>
        </td>
      </tr>`).join('');

    container.innerHTML = `
      <div class="card" style="margin-top:16px;border-left:4px solid var(--teal)">
        <div class="card-header">
          <span class="card-title">✏️ IFC Property Editor - ${_queue.length} fix(es) queued</span>
          <button class="btn btn-ghost" style="font-size:11px;color:var(--mid-grey)" onclick="PropertyEditor.clearQueue()">Clear all</button>
        </div>
        <p style="font-size:12px;color:var(--mid-grey);margin-bottom:12px">
          Enter the correct value for each property below, then click <strong>Apply Fixes</strong>.
          A corrected IFC file will be saved next to the original - your original is never modified.
        </p>
        <div class="table-wrap">
          <table class="results-table">
            <thead><tr>
              <th>Property</th><th>Element GUID</th><th>New Value</th><th>Guidance</th><th></th>
            </tr></thead>
            <tbody>${rows}</tbody>
          </table>
        </div>
        <div id="prop-editor-result"></div>
        <div style="margin-top:12px;display:flex;gap:10px;align-items:center">
          <button class="btn btn-teal" onclick="PropertyEditor.applyFixes()"
            style="padding:10px 24px;font-size:14px;font-weight:700">
            ✏️ Apply Fixes &amp; Save Corrected IFC
          </button>
          <div style="font-size:11px;color:var(--mid-grey)">
            The corrected file will be saved in the same folder as the original IFC
          </div>
        </div>
      </div>`;
  }

  function updateValue(index, val) {
    if (_queue[index]) _queue[index].newValue = val;
  }

  function removeFromQueue(index) {
    _queue.splice(index, 1);
    renderPanel();
  }

  function applyFixes() {
    const incomplete = _queue.filter(e => !e.newValue.trim());
    if (incomplete.length > 0) {
      alert(`Please fill in all values before applying. ${incomplete.length} value(s) are empty.`);
      return;
    }
    VBridge.send('applyPropertyEdits', {
      edits: _queue.map(e => ({
        stepId:   e.stepId,
        psetName: e.psetName,
        propName: e.propName,
        dataType: e.dataType,
        newValue: e.newValue,
        guid:     e.guid
      }))
    });
    const btn = document.querySelector('#prop-editor-panel button.btn-teal');
    if (btn) { btn.disabled = true; btn.textContent = 'Saving...'; }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  function inferDataType(propName) {
    const p = propName.toUpperCase();
    if (p.includes('ISEXTERNAL') || p.includes('LOADBEARING') || p.includes('HANDICAP') ||
        p.includes('SMOKESTOP') || p.includes('FIREEXIT') || p.includes('ISFLAMMABLE'))
      return 'IFCBOOLEAN';
    if (p.includes('AREA') || p.includes('HEIGHT') || p.includes('WIDTH') || p.includes('LENGTH') ||
        p.includes('THERMAL') || p.includes('UVALUE') || p.includes('GRADIENT') ||
        p.includes('RATIO') || p.includes('FACTOR') || p.includes('COEFFICIENT'))
      return 'IFCREAL';
    if (p.includes('COUNT') || p.includes('NUMBER') || p.includes('QUANTITY'))
      return 'IFCINTEGER';
    return 'IFCLABEL';
  }

  function getPlaceholder(edit) {
    switch (edit.dataType) {
      case 'IFCBOOLEAN': return 'TRUE or FALSE';
      case 'IFCREAL':    return 'e.g. 60.0';
      case 'IFCINTEGER': return 'e.g. 60';
      default:           return 'e.g. 60/60/60';
    }
  }

  return { addToQueue, clearQueue, togglePanel, showPanel, onResult, updateValue, removeFromQueue, applyFixes, renderPanel };
})();

window.PropertyEditor = PropertyEditor;
