// VERIFIQ v1.3 - 3D Viewer
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// Dual-engine rendering pipeline:
//   PRIMARY  - web-ifc WASM geometry engine (real IFC mesh triangles)
//   FALLBACK - C# IfcStepParser mesh data (Brep + ExtrudedAreaSolid + MappedItem)
//   MINIMUM  - C# bounding boxes (always available, colour-coded by compliance)
//
// The primary engine uses the same web-ifc library as viewer.sortdesk.com.
// It is initialised with a blob-URL workaround to bypass WebView2's
// application/octet-stream MIME type for .wasm files.
//
// C# → JS pipeline:
//   sendIfcForViewer → ifcFileData (base64 IFC bytes) → web-ifc renders
//   modelData        → elements3d (C# mesh/bbox data)  → Three.js fallback

'use strict';

const Viewer3DPage = (() => {

  // ── State ──────────────────────────────────────────────────────────────────
  let _scene      = null;
  let _camera     = null;
  let _renderer   = null;
  let _animFrame  = null;
  let _ifcApi     = null;     // web-ifc IfcAPI instance
  let _modelId    = null;     // web-ifc model handle
  let _raycaster  = null;
  let _mouse      = null;
  let _meshMap    = new Map(); // guid → THREE.Mesh
  let _guidMap    = new Map(); // mesh.uuid → guid
  let _selected   = null;
  let _isDragging = false;
  let _dragStart  = { x:0, y:0 };
  let _mouseBtn   = -1;
  let _lastMouse  = { x:0, y:0 };
  let _wireMode   = false;
  let _usingWebIfc = false;

  // Camera orbit state
  let _phi    = Math.PI / 3;
  let _theta  = Math.PI / 4;
  let _dist   = 50;
  let _target = { x:0, y:0, z:0 };

  // Compliance colours (Three.js hex)
  const COL = {
    Critical : 0xEF4444,
    Error    : 0xF97316,
    Warning  : 0xEAB308,
    Pass     : 0x22C55E,
    NoCheck  : 0x6B7280,
    Selected : 0x38BDF8,
  };

  // ── Page HTML ─────────────────────────────────────────────────────────────
  // ── Fullscreen (CSS-only — no DOM node moving) ─────────────────────────
  // Simply expand the #viewer-wrap div to cover the entire screen.
  // The Three.js canvas stays exactly where it is — no orphaned nodes.

  let _fullscreen = false;

  function enterFullscreen() {
    const wrap = document.getElementById('viewer-wrap');
    if (!wrap) return;
    _fullscreen = true;
    wrap.style.cssText = [
      'position:fixed', 'inset:0', 'z-index:9999',
      'background:#0A0F1A', 'display:flex', 'flex-direction:column',
    ].join(';');
    // Show the exit button
    const exitBtn = document.getElementById('viewer-exit-fs-btn');
    if (exitBtn) exitBtn.style.display = 'flex';
    // Resize renderer to fill screen
    if (_renderer && _camera) {
      const w = window.innerWidth;
      const h = window.innerHeight;
      _renderer.setSize(w, h);
      _camera.aspect = w / h;
      _camera.updateProjectionMatrix();
    }
    document.addEventListener('keydown', _fsKeyHandler);
  }

  function exitFullscreen() {
    const wrap = document.getElementById('viewer-wrap');
    if (!wrap) return;
    _fullscreen = false;
    wrap.style.cssText = '';
    const exitBtn = document.getElementById('viewer-exit-fs-btn');
    if (exitBtn) exitBtn.style.display = 'none';
    // Restore renderer to container size
    if (_renderer && _camera) {
      const canvas = _renderer.domElement;
      const container = canvas.parentElement;
      if (container) {
        const w = container.clientWidth || 800;
        const h = container.clientHeight || 520;
        _renderer.setSize(w, h);
        _camera.aspect = w / h;
        _camera.updateProjectionMatrix();
      }
    }
    document.removeEventListener('keydown', _fsKeyHandler);
  }

  const _fsKeyHandler = (e) => { if (e.key === 'Escape' && _fullscreen) exitFullscreen(); };

  function render() {
    const state = VState.get();
    const files = state.filesLoaded || [];
    const session = state.session;

    if (!files.length) {
      return `<div><h1>3D Viewer</h1>
        ${VUtils.emptyState('🧊','No IFC file loaded',
          'Open an IFC file, then return to the 3D Viewer.',
          '<button class="btn btn-primary" style="margin-top:16px" onclick="VBridge.openFile()">📂 Open IFC File</button>')}
      </div>`;
    }

    const fileOpts = files.map((f,i) =>
      `<option value="${VUtils.esc(f.name)}" ${i===0?'selected':''}>${VUtils.esc(f.name)}</option>`
    ).join('');

    return `
<div id="viewer-wrap" style="height:calc(100vh - 180px);display:flex;flex-direction:column;position:relative">
  <!-- Exit fullscreen button — hidden by default, shown only in fullscreen mode -->
  <div id="viewer-exit-fs-btn"
    style="display:none;position:absolute;top:12px;right:16px;z-index:10000;gap:8px;align-items:center">
    <span style="color:rgba(255,255,255,.5);font-size:11px">🧊 3D Viewer — Fullscreen &nbsp;|&nbsp; Press Esc to exit</span>
    <button onclick="Viewer3DPage.exitFullscreen()"
      style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3);
             border-radius:6px;padding:6px 16px;font-size:12px;cursor:pointer;font-family:inherit;font-weight:700">
      ✕ Exit Fullscreen
    </button>
  </div>
  <div style="display:flex;align-items:center;justify-content:space-between;padding-bottom:12px;flex-shrink:0">
    <div>
      <h1 style="margin:0">3D Viewer</h1>
      <p id="viewer-engine-label" style="font-size:11px;color:var(--mid-grey);margin:2px 0 0">
        Initialising…
      </p>
    </div>
    <div style="display:flex;gap:8px;align-items:center">
      ${files.length > 1 ? `
      <select id="viewer-file-select" style="height:32px;padding:0 10px;border:1px solid var(--border);border-radius:5px;font-size:12px;font-family:inherit"
              onchange="Viewer3DPage.switchFile(this.value)">${fileOpts}</select>` : ''}
      <button class="btn btn-ghost" style="height:32px;padding:0 12px;font-size:12px" onclick="Viewer3DPage.resetCamera()">⟳ Reset</button>
      <button class="btn btn-teal" style="height:32px;padding:0 14px;font-size:12px;font-weight:700;margin-left:4px"
        onclick="Viewer3DPage.enterFullscreen()" title="Full screen (Esc to exit)">⛶ Fullscreen</button>
      <button class="btn btn-ghost" id="wire-btn" style="height:32px;padding:0 12px;font-size:12px" onclick="Viewer3DPage.toggleWire()">⬡ Wire</button>
      <button class="btn btn-ghost" style="height:32px;padding:0 12px;font-size:12px" onclick="Viewer3DPage.showCriticalOnly()">⚠ Critical</button>
      <button class="btn btn-ghost" style="height:32px;padding:0 12px;font-size:12px" onclick="Viewer3DPage.showAll()">◎ All</button>
    </div>
  </div>

  <div style="display:flex;gap:12px;flex:1;min-height:0">
    <div style="flex:1;position:relative;min-width:0">
      <canvas id="verifiq-canvas"
        style="width:100%;height:100%;display:block;border-radius:8px;background:#111827;cursor:grab"></canvas>
      <div id="viewer-status"
        style="position:absolute;bottom:10px;left:12px;font-size:11px;color:#94A3B8;pointer-events:none">
        ☁ Left drag: orbit · Right drag: pan · Scroll: zoom · Click: inspect
      </div>
      <div id="viewer-loading"
        style="position:absolute;inset:0;display:none;flex-direction:column;align-items:center;
               justify-content:center;background:rgba(17,24,39,.88);border-radius:8px">
        <div style="color:white;font-size:15px;font-weight:600;margin-bottom:12px" id="viewer-loading-title">
          Loading IFC geometry…
        </div>
        <div id="viewer-progress" style="color:#94A3B8;font-size:12px">Initialising…</div>
      </div>
    </div>

    <div style="width:220px;display:flex;flex-direction:column;gap:10px;flex-shrink:0;overflow-y:auto">
      <div class="card" style="padding:12px">
        <div style="font-weight:700;font-size:12px;color:var(--navy-dark);margin-bottom:8px">
          Compliance Colours
        </div>
        ${[['#EF4444','Critical'],['#F97316','Error'],['#EAB308','Warning'],
           ['#22C55E','Pass'],['#6B7280','Not Checked']].map(([col,lbl]) => `
          <label style="font-size:11px;display:flex;align-items:center;gap:7px;cursor:pointer;margin-bottom:5px">
            <input type="checkbox" checked onchange="Viewer3DPage.toggleSev('${lbl}',this.checked)">
            <span style="width:10px;height:10px;border-radius:2px;background:${col};display:inline-block"></span>
            ${lbl}
          </label>`).join('')}
      </div>

      <div class="card" style="padding:12px;flex:1">
        <div style="font-weight:700;font-size:12px;color:var(--navy-dark);margin-bottom:8px">
          Element Inspector
        </div>
        <div id="inspector" style="font-size:11px;color:var(--light-grey)">
          Click an element to inspect it.
        </div>
      </div>

      <div class="card" style="padding:12px">
        <div style="font-weight:700;font-size:12px;color:var(--navy-dark);margin-bottom:8px">
          Statistics
        </div>
        <div id="viewer-stats" style="font-size:11px">${_renderStats(session)}</div>
      </div>
    </div>
  </div>
</div>`;
  }

  function _renderStats(session) {
    if (!session) return '<span style="color:var(--light-grey)">Run validation to see statistics.</span>';
    const t = session.total || 0;
    return `<div style="display:grid;grid-template-columns:1fr auto;gap:2px 10px;line-height:1.8">
      <span style="color:var(--mid-grey)">Total</span><b>${t.toLocaleString()}</b>
      <span style="color:#EF4444">Critical</span><b style="color:#EF4444">${(session.critical||0).toLocaleString()}</b>
      <span style="color:#F97316">Errors</span><b style="color:#F97316">${(session.errors||0).toLocaleString()}</b>
      <span style="color:#EAB308">Warnings</span><b style="color:#EAB308">${(session.warnings||0).toLocaleString()}</b>
      <span style="color:#22C55E">Pass</span><b style="color:#22C55E">${(session.passed||0).toLocaleString()}</b>
    </div>`;
  }

  // ── Page lifecycle ─────────────────────────────────────────────────────────
  function onNavigate() {
    if (_animFrame) { cancelAnimationFrame(_animFrame); _animFrame = null; }
    setTimeout(() => {
      _initThree();
      // Try web-ifc first; if not available fall back to C# mesh data
      const state = VState.get();
      const files = state.filesLoaded || [];
      if (files.length > 0) {
        if (typeof WebIFC !== 'undefined' && typeof WebIFC.IfcAPI !== 'undefined') {
          _setEngineLabel('web-ifc geometry engine (real IFC mesh triangles) · offline');
          _loadViaWebIfc(files[0].name);
        } else {
          _setEngineLabel('C# geometry engine (bounding-box fallback) · offline');
          _loadFromCSharpData();
        }
      }
    }, 150);
  }

  // ── Three.js scene ─────────────────────────────────────────────────────────
  function _initThree() {
    if (typeof THREE === 'undefined') {
      _setStatus('Three.js not loaded - libs/three.min.js must be Three.js r128 UMD build');
      return;
    }
    const canvas = document.getElementById('verifiq-canvas');
    if (!canvas) return;
    const W = canvas.clientWidth || canvas.offsetWidth || 800;
    const H = canvas.clientHeight || canvas.offsetHeight || 500;
    if (W < 10 || H < 10) { setTimeout(_initThree, 200); return; }

    if (_renderer) { _renderer.dispose(); _renderer = null; }

    _scene    = new THREE.Scene();
    _scene.background = new THREE.Color(0x111827);
    _camera   = new THREE.PerspectiveCamera(45, W/H, 0.01, 10000);
    _renderer = new THREE.WebGLRenderer({ canvas, antialias: true });
    _renderer.setSize(W, H);
    _renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    _raycaster = new THREE.Raycaster();
    _mouse     = new THREE.Vector2();

    // Lights
    _scene.add(new THREE.AmbientLight(0xffffff, 0.65));
    const sun = new THREE.DirectionalLight(0xffffff, 0.9);
    sun.position.set(50, 100, 50); _scene.add(sun);
    const fill = new THREE.DirectionalLight(0x8ec5ff, 0.25);
    fill.position.set(-50, -30, -50); _scene.add(fill);

    canvas.addEventListener('mousedown',   _onMouseDown);
    canvas.addEventListener('mousemove',   _onMouseMove);
    canvas.addEventListener('mouseup',     _onMouseUp);
    canvas.addEventListener('wheel',       _onWheel, { passive:false });
    canvas.addEventListener('click',       _onClick);
    canvas.addEventListener('contextmenu', e => e.preventDefault());

    new ResizeObserver(() => {
      if (!_renderer || !_camera) return;
      const c = document.getElementById('verifiq-canvas');
      if (!c) return;
      const w = c.clientWidth, h = c.clientHeight;
      if (w > 10 && h > 10) {
        _renderer.setSize(w, h);
        _camera.aspect = w / h;
        _camera.updateProjectionMatrix();
      }
    }).observe(canvas.parentElement || canvas);

    _startLoop();
    _setStatus('');
  }

  // ── web-ifc path (primary) ─────────────────────────────────────────────────
  async function _loadViaWebIfc(filename) {
    _setLoading(true, 'Requesting IFC file…');
    VBridge.send('sendIfcForViewer', { name: filename });
    // C# responds → bridge routes to onIfcData()
  }

  // Called by bridge when C# sends the IFC file bytes
  async function onIfcData(data) {
    if (data.error) {
      _setLoading(false); _setStatus('File error: ' + data.error);
      _loadFromCSharpData(); return;
    }
    _setLoading(true, 'Decoding IFC file…');
    try {
      // base64 → Uint8Array
      const bin   = atob(data.data);
      const bytes = new Uint8Array(bin.length);
      for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);

      _setLoading(true, 'Initialising geometry engine…');
      await _ensureIfcApi();

      // Clear previous
      if (_modelId !== null) { try { _ifcApi.CloseModel(_modelId); } catch(e){} }
      _clearScene();

      _setLoading(true, 'Loading IFC model…');
      _modelId = _ifcApi.OpenModel(bytes, {
        OPTIMIZE_PROFILES: true,
        COORDINATE_TO_ORIGIN: true,
        USE_FAST_BOOLS: true,
      });

      _setLoading(true, 'Extracting geometry…');
      await _extractAndRender();
      _usingWebIfc = true;
      _setEngineLabel('web-ifc geometry engine · ' + (data.schema || 'IFC4') + ' · offline');
      _applyComplianceColours();
      _fitCamera();
      _setLoading(false);
    } catch (err) {
      console.error('[VERIFIQ 3D web-ifc]', err);
      _setLoading(false);
      _setStatus('web-ifc: ' + (err.message || err) + ' - using C# geometry');
      _setEngineLabel('C# geometry engine (fallback) · offline');
      _loadFromCSharpData();
    }
  }

  async function _ensureIfcApi() {
    if (_ifcApi) return;
    _ifcApi = new WebIFC.IfcAPI();
    try {
      // Primary: rely on C# WebResourceRequested handler for correct MIME type
      _ifcApi.SetWasmPath('https://verifiq.local/libs/');
      await _ifcApi.Init();
    } catch (e) {
      console.warn('[VERIFIQ] Streaming WASM failed, using blob URL fallback:', e.message);
      // Fallback: fetch WASM as ArrayBuffer, wrap in blob with correct type
      const resp   = await fetch('https://verifiq.local/libs/web-ifc.wasm');
      const buf    = await resp.arrayBuffer();
      const blob   = new Blob([buf], { type: 'application/wasm' });
      const url    = URL.createObjectURL(blob);
      _ifcApi = new WebIFC.IfcAPI();
      await _ifcApi.Init((path) => {
        if (path && path.endsWith('.wasm')) return url;
        return 'https://verifiq.local/libs/' + path;
      });
    }
  }

  async function _extractAndRender() {
    if (_modelId === null || !_ifcApi) return;
    const geom = _ifcApi.LoadAllGeometry(_modelId);
    const sz   = geom.size();
    for (let i = 0; i < sz; i++) {
      const placed = geom.get(i);
      const mesh   = _meshFromPlaced(placed);
      if (!mesh) continue;
      // GUID lookup
      let guid = String(placed.expressID);
      try {
        const line = _ifcApi.GetLine(_modelId, placed.expressID);
        if (line && line.GlobalId && line.GlobalId.value) guid = line.GlobalId.value;
      } catch(_) {}
      mesh.userData.guid      = guid;
      mesh.userData.expressId = placed.expressID;
      _meshMap.set(guid, mesh);
      _guidMap.set(mesh.uuid, guid);
      _scene.add(mesh);
      if (i % 300 === 0) await new Promise(r => setTimeout(r, 0));
    }
    geom.delete();
  }

  function _meshFromPlaced(placed) {
    const gc = placed.geometries.size();
    if (!gc) return null;
    const pos = [], nor = [], idx = [];
    let offset = 0;
    for (let g = 0; g < gc; g++) {
      const flat = placed.geometries.get(g);
      const gd   = _ifcApi.GetGeometry(_modelId, flat.geometryExpressID);
      if (!gd) continue;
      const verts = _ifcApi.GetVertexArray(gd.GetVertexData(), gd.GetVertexDataSize());
      const idxs  = _ifcApi.GetIndexArray (gd.GetIndexData(),  gd.GetIndexDataSize());
      const m4    = new THREE.Matrix4().fromArray(flat.flatTransformation);
      const nm    = new THREE.Matrix3().getNormalMatrix(m4);
      for (let j = 0; j < verts.length; j += 6) {
        const v = new THREE.Vector3(verts[j], verts[j+1], verts[j+2]).applyMatrix4(m4);
        pos.push(v.x, v.y, v.z);
        const n = new THREE.Vector3(verts[j+3], verts[j+4], verts[j+5]).applyMatrix3(nm).normalize();
        nor.push(n.x, n.y, n.z);
      }
      for (let j = 0; j < idxs.length; j++) idx.push(idxs[j] + offset);
      offset += verts.length / 6;
      gd.delete();
    }
    if (!pos.length) return null;
    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(new Float32Array(pos), 3));
    geo.setAttribute('normal',   new THREE.BufferAttribute(new Float32Array(nor), 3));
    geo.setIndex(idx);
    const mat = new THREE.MeshPhongMaterial({
      color: 0x6B7280, specular: 0x111111, shininess: 30, side: THREE.DoubleSide
    });
    return new THREE.Mesh(geo, mat);
  }

  // ── C# mesh data path (fallback) ───────────────────────────────────────────
  function _loadFromCSharpData() {
    const state   = VState.get();
    const elems   = state.elements3d || [];
    if (!elems.length) {
      _setStatus('No geometry data - run validation or reload file');
      return;
    }
    _clearScene();
    _buildFromElements(elems);
    _applyComplianceColours();
    _fitCamera();
    _setEngineLabel('C# geometry engine (bounding boxes + mesh data) · offline');
  }

  function _buildFromElements(elems) {
    if (typeof THREE === 'undefined') return;
    elems.forEach(e => {
      let mesh;
      if (e.m && e.m.v && e.m.v.length >= 9) {
        // Real mesh triangles from C# parser
        const geo = new THREE.BufferGeometry();
        geo.setAttribute('position', new THREE.BufferAttribute(new Float32Array(e.m.v), 3));
        geo.setIndex(e.m.i);
        geo.computeVertexNormals();
        const mat = new THREE.MeshPhongMaterial({
          color: 0x6B7280, specular: 0x111111, shininess: 30, side: THREE.DoubleSide
        });
        mesh = new THREE.Mesh(geo, mat);
      } else if (e.bb) {
        // Bounding box fallback
        const b   = e.bb;
        const geo = new THREE.BoxGeometry(
          Math.max(0.05, b.maxX - b.minX),
          Math.max(0.05, b.maxZ - b.minZ),
          Math.max(0.05, b.maxY - b.minY)
        );
        const mat = new THREE.MeshPhongMaterial({ color: 0x6B7280, transparent: true, opacity: 0.85 });
        mesh = new THREE.Mesh(geo, mat);
        mesh.position.set(
          (b.minX + b.maxX) / 2,
          (b.minZ + b.maxZ) / 2,
          (b.minY + b.maxY) / 2
        );
      }
      if (!mesh) return;
      mesh.userData.guid = e.guid || '';
      mesh.userData.cls  = e.cls  || '';
      mesh.userData.name = e.name || '';
      _meshMap.set(e.guid || mesh.uuid, mesh);
      _guidMap.set(mesh.uuid, e.guid || '');
      _scene.add(mesh);
    });
  }

  // Called from bridge when C# sends modelData (elements with mesh/bbox)
  function loadElements() { if (!_usingWebIfc) _loadFromCSharpData(); }

  // ── Compliance colours ─────────────────────────────────────────────────────
  function _applyComplianceColours() {
    const state    = VState.get();
    const findings = (state.session && state.session.findings) || [];
    const sevRank  = { Pass:0, Warning:1, Error:2, Critical:3 };
    const sevMap   = {};
    findings.forEach(f => {
      const cur = sevMap[f.guid] ? sevRank[sevMap[f.guid]] : -1;
      if ((sevRank[f.severity] || 0) > cur) sevMap[f.guid] = f.severity;
    });
    _meshMap.forEach((mesh, guid) => {
      const sev = sevMap[guid] || 'NoCheck';
      mesh.material.color.setHex(COL[sev] || COL.NoCheck);
      mesh.userData.severity = sev;
    });
  }
  function refreshColours() { _applyComplianceColours(); }

  // ── Camera fit ─────────────────────────────────────────────────────────────
  function _fitCamera() {
    if (!_scene || !_camera || !_meshMap.size) return;
    const box = new THREE.Box3();
    _scene.traverse(o => { if (o.isMesh) box.expandByObject(o); });
    if (box.isEmpty()) return;
    const cen  = new THREE.Vector3(); box.getCenter(cen);
    const size = new THREE.Vector3(); box.getSize(size);
    _target = { x:cen.x, y:cen.y, z:cen.z };
    _dist   = Math.max(size.x, size.y, size.z) * 2.2 || 50;
    _phi    = Math.PI / 3; _theta = Math.PI / 4;
    _updateCamera();
  }

  function _updateCamera() {
    if (!_camera) return;
    _camera.position.set(
      _target.x + _dist * Math.sin(_phi) * Math.cos(_theta),
      _target.y + _dist * Math.cos(_phi),
      _target.z + _dist * Math.sin(_phi) * Math.sin(_theta)
    );
    _camera.lookAt(_target.x, _target.y, _target.z);
  }

  function _startLoop() {
    if (_animFrame) return;
    const loop = () => {
      _animFrame = requestAnimationFrame(loop);
      if (_renderer && _scene && _camera) _renderer.render(_scene, _camera);
    };
    _animFrame = requestAnimationFrame(loop);
  }

  function _clearScene() {
    if (!_scene) return;
    const rm = [];
    _scene.traverse(o => { if (o.isMesh) rm.push(o); });
    rm.forEach(m => { m.geometry.dispose(); if (m.material) m.material.dispose(); _scene.remove(m); });
    _meshMap.clear(); _guidMap.clear(); _selected = null;
  }

  // ── Mouse controls ─────────────────────────────────────────────────────────
  function _onMouseDown(e) {
    _mouseBtn = e.button; _lastMouse = { x:e.clientX, y:e.clientY };
    _isDragging = false; _dragStart = { x:e.clientX, y:e.clientY };
    const c = document.getElementById('verifiq-canvas');
    if (c) c.style.cursor = e.button===0 ? 'grabbing' : 'move';
  }
  function _onMouseMove(e) {
    const dx = e.clientX - _lastMouse.x, dy = e.clientY - _lastMouse.y;
    if (Math.abs(e.clientX-_dragStart.x) > 3 || Math.abs(e.clientY-_dragStart.y) > 3) _isDragging = true;
    if (_mouseBtn === 0) {
      _theta -= dx * 0.005; _phi -= dy * 0.005;
      _phi = Math.max(0.05, Math.min(Math.PI-0.05, _phi)); _updateCamera();
    } else if (_mouseBtn === 2) {
      if (!_camera) return;
      const fwd = new THREE.Vector3(_target.x, _target.y, _target.z).sub(_camera.position).normalize();
      const right = new THREE.Vector3().crossVectors(fwd, _camera.up).normalize();
      const speed = _dist * 0.001;
      _target.x -= right.x*dx*speed - _camera.up.x*dy*speed;
      _target.y -= right.y*dx*speed - _camera.up.y*dy*speed;
      _target.z -= right.z*dx*speed - _camera.up.z*dy*speed;
      _updateCamera();
    }
    _lastMouse = { x:e.clientX, y:e.clientY };
  }
  function _onMouseUp() {
    _mouseBtn = -1;
    const c = document.getElementById('verifiq-canvas');
    if (c) c.style.cursor = 'grab';
  }
  function _onWheel(e) {
    e.preventDefault();
    _dist *= e.deltaY > 0 ? 1.1 : 0.9;
    _dist = Math.max(0.1, Math.min(5000, _dist)); _updateCamera();
  }

  // ── Element picking ────────────────────────────────────────────────────────
  function _onClick(e) {
    if (_isDragging || !_renderer || !_camera || !_raycaster) return;
    const c = document.getElementById('verifiq-canvas');
    if (!c) return;
    const r = c.getBoundingClientRect();
    _mouse.x =  ((e.clientX-r.left)/r.width)  * 2 - 1;
    _mouse.y = -((e.clientY-r.top) /r.height) * 2 + 1;
    _raycaster.setFromCamera(_mouse, _camera);
    const objs = []; _scene.traverse(o => { if (o.isMesh) objs.push(o); });
    const hits = _raycaster.intersectObjects(objs);
    if (!hits.length) {
      if (_selected) { const s = _selected.userData.severity||'NoCheck'; _selected.material.color.setHex(COL[s]||COL.NoCheck); _selected=null; }
      _updateInspector(null); return;
    }
    const hit = hits[0].object;
    if (_selected && _selected !== hit) { const s=_selected.userData.severity||'NoCheck'; _selected.material.color.setHex(COL[s]||COL.NoCheck); }
    _selected = hit; hit.material.color.setHex(COL.Selected);
    _updateInspector(hit);
  }

  function _updateInspector(mesh) {
    const el = document.getElementById('inspector');
    if (!el) return;
    if (!mesh) { el.innerHTML = '<span style="color:var(--light-grey)">Click an element to inspect it.</span>'; return; }
    const guid = mesh.userData.guid || '-';
    const sev  = mesh.userData.severity || 'NoCheck';
    const col  = {Critical:'#EF4444',Error:'#F97316',Warning:'#EAB308',Pass:'#22C55E',NoCheck:'#6B7280'}[sev]||'#6B7280';
    const state    = VState.get();
    const findings = ((state.session && state.session.findings)||[]).filter(f => f.guid === guid);
    const rows = findings.slice(0,5).map(f => `
      <div style="margin-top:6px;padding:5px 6px;background:#F8FAFC;border-radius:4px;border-left:3px solid ${col}">
        <div style="font-weight:700;font-size:10px;color:${col}">${VUtils.esc(f.severity)} - ${VUtils.esc(f.check)}</div>
        <div style="font-size:10px;color:#374151;margin-top:2px">${VUtils.esc((f.message||'').substring(0,80))}${(f.message||'').length>80?'…':''}</div>
      </div>`).join('');
    el.innerHTML = `
      <div style="margin-bottom:5px">
        <span style="font-size:10px;font-weight:700;padding:2px 7px;border-radius:3px;background:${col}20;color:${col}">${sev}</span>
      </div>
      <div style="font-size:10px;font-family:monospace;margin-bottom:5px;word-break:break-all">${VUtils.esc(guid)}</div>
      <div style="font-size:11px;color:var(--mid-grey);margin-bottom:6px">${VUtils.esc(mesh.userData.cls||mesh.userData.name||'')}</div>
      ${findings.length ? rows + (findings.length>5?`<div style="font-size:10px;color:var(--mid-grey);margin-top:4px">+${findings.length-5} more</div>`:'')
        : '<div style="color:var(--light-grey);font-size:11px">No findings for this element.</div>'}`;
  }

  // ── Controls ───────────────────────────────────────────────────────────────
  function resetCamera()      { _fitCamera(); }
  function switchFile(name)   { _usingWebIfc=false; _clearScene(); if (typeof WebIFC!=='undefined') { _loadViaWebIfc(name); } else { _loadFromCSharpData(); } }
  function toggleWire()       { _wireMode=!_wireMode; _scene&&_scene.traverse(o=>{if(o.isMesh)o.material.wireframe=_wireMode;}); const b=document.getElementById('wire-btn'); if(b)b.style.background=_wireMode?'var(--teal)':''; }
  function showCriticalOnly() { _scene&&_scene.traverse(o=>{if(o.isMesh)o.visible=o.userData.severity==='Critical'||o.userData.severity==='Error';}); }
  function showAll()          { _scene&&_scene.traverse(o=>{if(o.isMesh)o.visible=true;}); }
  function toggleSev(sev, vis){ const k={Critical:'Critical',Error:'Error',Warning:'Warning',Pass:'Pass','Not Checked':'NoCheck'}[sev]||sev; _scene&&_scene.traverse(o=>{if(o.isMesh&&(o.userData.severity||'NoCheck')===k)o.visible=vis;}); }
  function applyVisibility()  { showAll(); }

  // ── Helpers ────────────────────────────────────────────────────────────────
  function _setStatus(msg)           { const e=document.getElementById('viewer-status');   if(e) e.textContent=msg; }
  function _setEngineLabel(msg)      { const e=document.getElementById('viewer-engine-label'); if(e) e.textContent=msg; }
  function _setLoading(show, msg) {
    const el = document.getElementById('viewer-loading');
    const pr = document.getElementById('viewer-progress');
    if (el) el.style.display = show ? 'flex' : 'none';
    if (pr && msg) pr.textContent = msg;
  }

  return {
    render, onNavigate, onIfcData, loadElements, refreshColours,
    resetCamera, switchFile, toggleWire, showCriticalOnly, showAll,
    toggleSev, applyVisibility,
    enterFullscreen, exitFullscreen,
  };
})();

window.Viewer3DPage = Viewer3DPage;
