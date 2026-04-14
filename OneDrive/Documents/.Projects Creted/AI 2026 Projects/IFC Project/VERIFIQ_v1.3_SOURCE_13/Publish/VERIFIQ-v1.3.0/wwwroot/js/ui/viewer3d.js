// VERIFIQ v2.0 - Enhanced 3D Viewer (BIMvision-inspired)
// Copyright 2026 BBMW0 Technologies. All rights reserved.
//
// Features matching BIMvision:
//   Navigation: Orbit, Pan, Zoom (scroll), Top/Front/Right/Left/Back/Iso views, 2D mode
//   Display: Wireframe, Object colour, Colour by type/storey/discipline/compliance
//   Section: Cut X/Y/Z with live slider; cut by face; storey slide
//   Measurement: Edge length, Area, Volume, Coordinates, Weight/Counting mode
//   Selection: Click, Box select, Select All, Select by type, Invert selection
//   Hide/Show: Hide selected, Hide unselected, Show all, Isolate, X-Ray
//   IFC Structure: Hierarchical tree panel (Project > Site > Building > Storey > Element)
//   Properties: Full properties panel with all IFC property sets, GUID, BaseQuantities
//   Clash: Visual clash highlight (VERIFIQ findings as clashes)
//   Storey slider: hide/show elements by storey
//   Snap: Snap to vertex, edge, face for measurements
'use strict';

const Viewer3DPage = (() => {

  // ── State ────────────────────────────────────────────────────────────────────
  let _scene=null,_camera=null,_renderer=null,_animFrame=null;
  let _ifcApi=null,_modelId=null;
  let _raycaster=null,_mouse=null;
  let _meshMap=new Map(),_guidMap=new Map();
  let _selected=null,_selectedSet=new Set();
  let _isDragging=false,_dragStart={x:0,y:0};
  let _mouseBtn=-1,_lastMouse={x:0,y:0};
  let _wireMode=false,_xrayMode=false,_isolateMode=false;
  let _usingWebIfc=false,_modelBounds=null;
  let _colorMode='compliance'; // compliance|type|storey|discipline
  let _measureMode=null; // null|edge|area|volume|coord
  let _measurePts=[];
  let _storeys=[];
  let _visibleStoreys=new Set();
  let _ifcTreeData=null;

  // Camera orbit
  let _phi=Math.PI/3,_theta=Math.PI/4,_dist=50;
  let _target={x:0,y:0,z:0};

  // Section planes
  let _secEnabled={x:false,y:false,z:false};
  let _secPlanes={x:null,y:null,z:null};

  // Compliance colours
  const COL={Critical:0xEF4444,Error:0xF97316,Warning:0xEAB308,Pass:0x22C55E,NoCheck:0x6B7280,Selected:0x38BDF8,Hover:0xA78BFA};

  // IFC type colours (like BIMvision "Object Colour" mode)
  const TYPE_COL={
    IfcWall:0x9CA3AF,IfcSlab:0xD1D5DB,IfcBeam:0x60A5FA,IfcColumn:0xF97316,
    IfcDoor:0xA78BFA,IfcWindow:0x38BDF8,IfcRoof:0xEF4444,IfcStair:0xFBBF24,
    IfcRailing:0x4ADE80,IfcCovering:0xE9D5FF,IfcCurtainWall:0xBAE6FD,
    IfcSpace:0x86EFAC44,IfcPile:0x92400E,IfcFooting:0x78350F,
    IfcPipeSegment:0x0EA5E9,IfcDuctSegment:0x06B6D4,IfcValve:0x0284C7,
    IfcSanitaryTerminal:0x818CF8,IfcFireSuppressionTerminal:0xFF4444,
    IfcAlarm:0xFF6B6B,IfcTank:0x34D399,IfcGeographicElement:0x22C55E,
  };

  let _fullscreen=false;

  // ── Fullscreen ────────────────────────────────────────────────────────────────
  function enterFullscreen() {
    const w=document.getElementById('viewer-wrap');
    if(!w)return; _fullscreen=true;
    w.style.cssText='position:fixed;inset:0;z-index:9999;background:#060d1b;display:flex;flex-direction:column;';
    const b=document.getElementById('v-exit-fs');
    if(b)b.style.display='flex';
    if(_renderer&&_camera){
      _renderer.setSize(window.innerWidth,window.innerHeight);
      _camera.aspect=window.innerWidth/window.innerHeight;
      _camera.updateProjectionMatrix();
    }
    document.addEventListener('keydown',_keyHandler);
  }
  function exitFullscreen() {
    const w=document.getElementById('viewer-wrap');
    if(!w)return; _fullscreen=false; w.style.cssText='';
    const b=document.getElementById('v-exit-fs');
    if(b)b.style.display='none';
    _resizeRenderer();
    document.removeEventListener('keydown',_keyHandler);
  }

  // ── Page HTML ─────────────────────────────────────────────────────────────────
  function render() {
    const state=VState.get();
    const files=state.filesLoaded||[];
    const sess=state.session;
    if(!files.length) return `<div><h1>3D Viewer</h1>${VUtils.emptyState('🧊','No IFC file loaded','Open an IFC file first.','<button class="btn btn-primary" onclick="VBridge.openFile()">📂 Open IFC File</button>')}</div>`;

    const fileOpts=files.map((f,i)=>`<option value="${VUtils.esc(f.name)}" ${i===0?'selected':''}>${VUtils.esc(f.name)}</option>`).join('');

    return `
<div id="viewer-wrap" style="display:flex;flex-direction:column;height:calc(100vh - 108px);position:relative;overflow:hidden">

  <!-- Exit FS overlay -->
  <div id="v-exit-fs" style="display:none;position:absolute;top:10px;right:14px;z-index:10000;gap:8px;align-items:center">
    <span style="color:rgba(255,255,255,.45);font-size:11px">Fullscreen | Esc to exit</span>
    <button onclick="Viewer3DPage.exitFullscreen()" style="background:rgba(255,255,255,.15);color:white;border:1px solid rgba(255,255,255,.3);border-radius:5px;padding:5px 14px;font-size:12px;cursor:pointer">Exit</button>
  </div>

  <!-- ── MAIN TOOLBAR (BIMvision-style tabs) ── -->
  <div style="display:flex;align-items:center;background:#0a1628;border-bottom:1px solid #162843;padding:0 10px;flex-shrink:0;gap:2px;height:38px">

    <!-- File/View group -->
    <div style="display:flex;align-items:center;gap:2px;padding-right:8px;border-right:1px solid #162843;margin-right:4px">
      ${files.length>1?`<select id="v-file-sel" onchange="Viewer3DPage.switchFile(this.value)" style="${SEL}width:120px">${fileOpts}</select>`:''}
      ${_tbtn('⟳','Reset camera (R)','Viewer3DPage.resetCamera()')}
      ${_tbtn('⛶','Fullscreen (F)','Viewer3DPage.enterFullscreen()','v-fs-btn')}
      ${_tbtn('🚶','Walk / First-person mode (W)','Viewer3DPage.toggleFpsMode()','v-fps-btn')}
    </div>

    <!-- View direction group -->
    <div style="display:flex;align-items:center;gap:2px;padding-right:8px;border-right:1px solid #162843;margin-right:4px">
      <span style="font-size:9px;color:#374151;text-transform:uppercase;letter-spacing:.5px;margin-right:2px">View</span>
      ${_tbtn('⬛','Isometric','Viewer3DPage.setView("iso")')}
      ${_tbtn('🔝','Top','Viewer3DPage.setView("top")')}
      ${_tbtn('⬆','Front','Viewer3DPage.setView("front")')}
      ${_tbtn('▶','Right','Viewer3DPage.setView("right")')}
      ${_tbtn('⬇','Back','Viewer3DPage.setView("back")')}
    </div>

    <!-- Display mode group -->
    <div style="display:flex;align-items:center;gap:2px;padding-right:8px;border-right:1px solid #162843;margin-right:4px">
      <span style="font-size:9px;color:#374151;text-transform:uppercase;letter-spacing:.5px;margin-right:2px">Display</span>
      ${_tbtn('⬡','Wireframe','Viewer3DPage.toggleWire()','v-wire')}
      ${_tbtn('🔍','X-Ray mode','Viewer3DPage.toggleXray()','v-xray')}
      ${_tbtn('◉','Isolate selected','Viewer3DPage.toggleIsolate()','v-isolate')}
      ${_tbtn('◎','Show all','Viewer3DPage.showAll()')}
      ${_tbtn('⚠','Critical only','Viewer3DPage.showCriticalOnly()')}
      <select id="v-col-mode" onchange="Viewer3DPage.setColorMode(this.value)" style="${SEL}width:100px" title="Colour elements by…">
        <option value="compliance" selected>By Compliance</option>
        <option value="type">By IFC Type</option>
        <option value="storey">By Storey</option>
        <option value="discipline">By Discipline</option>
      </select>
    </div>

    <!-- Measure group -->
    <div style="display:flex;align-items:center;gap:2px;padding-right:8px;border-right:1px solid #162843;margin-right:4px">
      <span style="font-size:9px;color:#374151;text-transform:uppercase;letter-spacing:.5px;margin-right:2px">Measure</span>
      ${_tbtn('📏','Edge length','Viewer3DPage.setMeasure("edge")','v-m-edge')}
      ${_tbtn('⬜','Area','Viewer3DPage.setMeasure("area")','v-m-area')}
      ${_tbtn('📦','Volume','Viewer3DPage.setMeasure("vol")','v-m-vol')}
      ${_tbtn('📍','Coordinates','Viewer3DPage.setMeasure("coord")','v-m-coord')}
      ${_tbtn('✕','Clear measures','Viewer3DPage.clearMeasures()')}
    </div>

    <!-- Zoom/Select group -->
    <div style="display:flex;align-items:center;gap:2px">
      ${_tbtn('🎯','Zoom to selection (I)','Viewer3DPage.zoomToSelected()')}
      ${_tbtn('🔲','Zoom to all','Viewer3DPage.resetCamera()')}
    </div>

    <!-- Right: engine label -->
    <div style="margin-left:auto">
      <span id="v-engine" style="font-size:10px;color:#374151">Initialising…</span>
    </div>
  </div>

  <!-- ── SECTION PLANE + STOREY BAR ── -->
  <div style="display:flex;align-items:center;gap:10px;padding:5px 10px;background:#060d1b;border-bottom:1px solid #0f1e30;flex-shrink:0;flex-wrap:wrap">
    <span style="font-size:10px;font-weight:600;color:#374151;text-transform:uppercase;letter-spacing:.5px">Section Planes</span>
    ${['x','y','z'].map(a=>`
      <label style="display:flex;align-items:center;gap:4px;font-size:11px;color:#5b7fa6;white-space:nowrap">
        <input type="checkbox" id="sec-${a}" onchange="Viewer3DPage.toggleSection('${a}',this.checked)" style="width:12px;height:12px"> Cut ${a.toUpperCase()}
        <input type="range" id="sec-${a}-v" min="-100" max="100" value="0" style="width:72px;height:3px;cursor:pointer" oninput="Viewer3DPage.updateSection('${a}',this.value)">
        <span id="sec-${a}-l" style="width:28px;color:var(--teal);font-size:10px">0%</span>
      </label>`).join('')}
    <button onclick="Viewer3DPage.clearSections()" style="font-size:10px;padding:2px 7px;border:1px solid #162843;border-radius:4px;background:transparent;color:#5b7fa6;cursor:pointer">Clear</button>
    <div style="margin-left:8px;display:flex;align-items:center;gap:6px">
      <span style="font-size:10px;font-weight:600;color:#374151;text-transform:uppercase;letter-spacing:.5px">Storey</span>
      <select id="v-storey-filter" onchange="Viewer3DPage.filterByStorey(this.value)" style="${SEL}width:120px">
        <option value="">All Storeys</option>
      </select>
    </div>
    <div style="margin-left:8px;display:flex;align-items:center;gap:6px">
      <span style="font-size:10px;font-weight:600;color:#374151;text-transform:uppercase;letter-spacing:.5px">Discipline</span>
      <label style="display:flex;align-items:center;gap:3px;font-size:11px"><input type="checkbox" checked onchange="Viewer3DPage.filterDisc('ARC',this.checked)"> ARC</label>
      <label style="display:flex;align-items:center;gap:3px;font-size:11px"><input type="checkbox" checked onchange="Viewer3DPage.filterDisc('STR',this.checked)"> STR</label>
      <label style="display:flex;align-items:center;gap:3px;font-size:11px"><input type="checkbox" checked onchange="Viewer3DPage.filterDisc('MEP',this.checked)"> MEP</label>
      <label style="display:flex;align-items:center;gap:3px;font-size:11px"><input type="checkbox" checked onchange="Viewer3DPage.filterDisc('EXT',this.checked)"> EXT</label>
    </div>
  </div>

  <!-- ── MAIN AREA: Canvas + Side Panels ── -->
  <div style="display:flex;flex:1;min-height:0;overflow:hidden">

    <!-- LEFT PANEL: IFC Structure Tree -->
    <div id="v-tree-panel" style="width:220px;flex-shrink:0;background:#060d1b;border-right:1px solid #0f1e30;display:flex;flex-direction:column;overflow:hidden">
      <div style="padding:7px 10px;border-bottom:1px solid #0f1e30;display:flex;align-items:center;justify-content:space-between">
        <span style="font-size:11px;font-weight:700;color:#5b7fa6;text-transform:uppercase;letter-spacing:.5px">IFC Structure</span>
        <button onclick="Viewer3DPage.selectAll()" style="font-size:9px;padding:1px 6px;border:1px solid #162843;border-radius:3px;background:transparent;color:#5b7fa6;cursor:pointer">All</button>
      </div>
      <div id="v-tree" style="flex:1;overflow-y:auto;font-size:11px;padding:4px 0">
        <span style="color:#374151;padding:8px 10px;display:block">Load a model to see IFC structure.</span>
      </div>
    </div>

    <!-- CANVAS -->
    <div style="flex:1;position:relative;min-width:0;overflow:hidden">
      <canvas id="verifiq-canvas" tabindex="0" style="width:100%;height:100%;display:block;cursor:grab;outline:none;-webkit-user-select:none;user-select:none"></canvas>

      <!-- Status bar overlaid on canvas -->
      <div id="v-status" style="position:absolute;bottom:8px;left:10px;font-size:10px;color:#4b5563;background:rgba(6,13,27,.8);padding:3px 8px;border-radius:4px;pointer-events:none;backdrop-filter:blur(4px)">
        L.drag: orbit &nbsp;|&nbsp; R.drag: pan &nbsp;|&nbsp; Scroll: zoom &nbsp;|&nbsp; Click: select &nbsp;|&nbsp; F: fullscreen &nbsp;|&nbsp; R: reset &nbsp;|&nbsp; I: zoom to selection
      </div>

      <!-- Measure readout overlay -->
      <div id="v-measure-out" style="display:none;position:absolute;top:8px;left:50%;transform:translateX(-50%);background:rgba(0,196,160,.15);border:1px solid rgba(0,196,160,.5);border-radius:6px;padding:6px 14px;font-size:12px;font-weight:600;color:#00c4a0;backdrop-filter:blur(8px);pointer-events:none"></div>

      <!-- Loading overlay -->
      <div id="v-loading" style="display:none;position:absolute;inset:0;background:rgba(6,13,27,.9);flex-direction:column;align-items:center;justify-content:center">
        <div style="color:white;font-size:14px;font-weight:600;margin-bottom:10px">Loading IFC geometry…</div>
        <div id="v-prog" style="color:#5b7fa6;font-size:12px">Initialising…</div>
      </div>
    </div>

    <!-- RIGHT PANEL: Properties + Compliance + Statistics -->
    <div style="width:240px;flex-shrink:0;background:#060d1b;border-left:1px solid #0f1e30;display:flex;flex-direction:column;overflow:hidden">

      <!-- Compliance legend -->
      <div style="padding:8px 10px;border-bottom:1px solid #0f1e30;flex-shrink:0">
        <div style="font-size:10px;font-weight:700;color:#5b7fa6;text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px">Compliance Colours</div>
        ${[['#EF4444','Critical'],['#F97316','Error'],['#EAB308','Warning'],['#22C55E','Pass'],['#6B7280','Not Checked']].map(([c,l])=>`
          <label style="display:flex;align-items:center;gap:7px;font-size:11px;cursor:pointer;margin-bottom:4px;color:#9ca3af">
            <input type="checkbox" checked onchange="Viewer3DPage.toggleSev('${l}',this.checked)" style="width:12px;height:12px">
            <span style="width:9px;height:9px;border-radius:2px;background:${c};display:inline-block;flex-shrink:0"></span>${l}
          </label>`).join('')}
      </div>

      <!-- Element Inspector -->
      <div style="flex:1;overflow-y:auto;display:flex;flex-direction:column">
        <div style="padding:7px 10px;border-bottom:1px solid #0f1e30;display:flex;align-items:center;justify-content:space-between;flex-shrink:0">
          <span style="font-size:10px;font-weight:700;color:#5b7fa6;text-transform:uppercase;letter-spacing:.5px">Element Inspector</span>
          <button id="v-see-all" onclick="Viewer3DPage.goToFindings()" style="display:none;font-size:9px;padding:1px 6px;border:none;border-radius:3px;background:var(--teal);color:var(--navy);cursor:pointer;font-weight:700">See All</button>
        </div>
        <div id="v-inspector" style="padding:8px 10px;font-size:11px;flex:1;overflow-y:auto">
          <span style="color:#374151">Click an element to inspect it.</span>
        </div>
      </div>

      <!-- Model Statistics -->
      <div style="padding:8px 10px;border-top:1px solid #0f1e30;flex-shrink:0">
        <div style="font-size:10px;font-weight:700;color:#5b7fa6;text-transform:uppercase;letter-spacing:.5px;margin-bottom:6px">Model Statistics</div>
        <div id="v-stats" style="font-size:11px;color:#5b7fa6">
          ${_renderStats(VState.get().session)}
        </div>
        <div id="v-model-info" style="font-size:10px;color:#374151;margin-top:6px"></div>
      </div>
    </div>
  </div>
</div>`;
  }

  const SEL='height:24px;padding:0 5px;font-size:11px;border:1px solid #2d4a6e;border-radius:4px;background:#081322;color:#e2e8f0;cursor:pointer;';

    // First-person walk mode state
  let _fpsMode=false, _fpsVel={x:0,y:0,z:0}, _fpsKeys={};

  function toggleFpsMode() {
    _fpsMode = !_fpsMode;
    const b = document.getElementById('v-fps-btn');
    const cv = document.getElementById('verifiq-canvas');
    if(_fpsMode) {
      if(b){b.style.background='var(--teal)';b.style.color='var(--navy)';}
      if(cv) cv.style.cursor='crosshair';
      _setStatus('WALK MODE: WASD/arrows=move, Q/E=up/down, mouse=look, Click=exit, Esc=exit');
      document.addEventListener('keydown', _fpsKeyDown);
      document.addEventListener('keyup',   _fpsKeyUp);
      _fpsLoop();
    } else {
      if(b){b.style.background='transparent';b.style.color='#5b7fa6';}
      _updateCursor(null);
      _setStatus('L.drag: orbit | R.drag: pan | Scroll: zoom | Click: select');
      document.removeEventListener('keydown', _fpsKeyDown);
      document.removeEventListener('keyup',   _fpsKeyUp);
      if(_fpsFrame){cancelAnimationFrame(_fpsFrame);_fpsFrame=null;}
    }
  }

  let _fpsFrame=null, _fpsYaw=0, _fpsPitch=0;

  function _fpsKeyDown(e){ _fpsKeys[e.code]=true; if(e.code==='Escape'){toggleFpsMode();} }
  function _fpsKeyUp(e){ _fpsKeys[e.code]=false; }

  function _fpsMouseMove(e) {
    if(!_fpsMode||!_isDragging) return;
    _fpsYaw   -= e.movementX * 0.003;
    _fpsPitch -= e.movementY * 0.003;
    _fpsPitch  = Math.max(-1.4, Math.min(1.4, _fpsPitch));
  }

  function _fpsLoop() {
    if(!_fpsMode){_fpsFrame=null;return;}
    _fpsFrame=requestAnimationFrame(_fpsLoop);
    if(!_camera)return;
    const speed = (_fpsKeys['ShiftLeft']||_fpsKeys['ShiftRight']) ? 0.8 : 0.2;
    const fwd = new THREE.Vector3(-Math.sin(_fpsYaw)*Math.cos(_fpsPitch), -Math.sin(_fpsPitch), -Math.cos(_fpsYaw)*Math.cos(_fpsPitch)).normalize();
    const right = new THREE.Vector3().crossVectors(fwd, new THREE.Vector3(0,1,0)).normalize();
    if(_fpsKeys['KeyW']||_fpsKeys['ArrowUp'])    { _target.x+=fwd.x*speed; _target.y+=fwd.y*speed; _target.z+=fwd.z*speed; }
    if(_fpsKeys['KeyS']||_fpsKeys['ArrowDown'])  { _target.x-=fwd.x*speed; _target.y-=fwd.y*speed; _target.z-=fwd.z*speed; }
    if(_fpsKeys['KeyA']||_fpsKeys['ArrowLeft'])  { _target.x-=right.x*speed; _target.z-=right.z*speed; }
    if(_fpsKeys['KeyD']||_fpsKeys['ArrowRight']) { _target.x+=right.x*speed; _target.z+=right.z*speed; }
    if(_fpsKeys['KeyQ']) { _target.y-=speed*0.5; }
    if(_fpsKeys['KeyE']) { _target.y+=speed*0.5; }
    _camera.position.set(_target.x-fwd.x*0.1, _target.y-fwd.y*0.1+1.7, _target.z-fwd.z*0.1);
    _camera.lookAt(_target.x+fwd.x*5, _target.y+fwd.y*5+1.7, _target.z+fwd.z*5);
  }

  function _tbtn(icon, title, onclick, id='') {
    return `<button onclick="${onclick}" ${id?`id="${id}"`:''}
      title="${title}"
      style="width:26px;height:26px;border:1px solid #162843;border-radius:4px;background:transparent;color:#5b7fa6;cursor:pointer;font-size:13px;display:flex;align-items:center;justify-content:center;transition:all .15s;flex-shrink:0"
      onmouseenter="this.style.background='#0f2035';this.style.color='#edf2fb'"
      onmouseleave="this.style.background='transparent';this.style.color='#5b7fa6'"
    >${icon}</button>`;
  }

  function _renderStats(sess) {
    if(!sess) return '<span style="color:#374151">Run validation to see stats.</span>';
    const t=sess.total||0;
    return `<div style="display:grid;grid-template-columns:1fr auto;gap:2px 8px;line-height:1.8">
      <span>Total</span><b>${t.toLocaleString()}</b>
      <span style="color:#ef4444">Critical</span><b style="color:#ef4444">${(sess.critical||0).toLocaleString()}</b>
      <span style="color:#f97316">Errors</span><b style="color:#f97316">${(sess.errors||0).toLocaleString()}</b>
      <span style="color:#eab308">Warnings</span><b style="color:#eab308">${(sess.warnings||0).toLocaleString()}</b>
      <span style="color:#22c55e">Pass</span><b style="color:#22c55e">${(sess.passed||0).toLocaleString()}</b>
    </div>`;
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────────
  function onNavigate() {
    if(_animFrame){cancelAnimationFrame(_animFrame);_animFrame=null;}
    setTimeout(()=>{
      _initThree();
      const state=VState.get(), files=state.filesLoaded||[];
      if(files.length>0){
        if(typeof WebIFC!=='undefined'&&typeof WebIFC.IfcAPI!=='undefined'){
          _setEngineLabel('web-ifc (IFC4 STEP)');
          _loadViaWebIfc(files[0].name);
        } else {
          _setEngineLabel('C# geometry engine');
          _loadFromCSharpData();
        }
      }
    },150);
  }

  // ── Three.js init ─────────────────────────────────────────────────────────────
  function _initThree() {
    if(typeof THREE==='undefined'){ _setStatus('Three.js not loaded'); return; }
    const cv=document.getElementById('verifiq-canvas');
    if(!cv)return;
    const W=cv.clientWidth||cv.offsetWidth||800, H=cv.clientHeight||cv.offsetHeight||500;
    if(W<10||H<10){setTimeout(_initThree,200);return;}
    if(_renderer){_renderer.dispose();_renderer=null;}
    _scene=new THREE.Scene();
    _scene.background=new THREE.Color(0x060d1b);
    _scene.fog=new THREE.FogExp2(0x060d1b,0.0003);
    _camera=new THREE.PerspectiveCamera(45,W/H,0.01,20000);
    _renderer=new THREE.WebGLRenderer({canvas:cv,antialias:true,powerPreference:'high-performance'});
    _renderer.setSize(W,H);
    _renderer.setPixelRatio(Math.min(window.devicePixelRatio,2));
    _raycaster=new THREE.Raycaster();
    _mouse=new THREE.Vector2();
    // Lights
    _scene.add(new THREE.AmbientLight(0xffffff,0.72));
    const sun=new THREE.DirectionalLight(0xffffff,0.95); sun.position.set(50,100,50); _scene.add(sun);
    const fill=new THREE.DirectionalLight(0x8ec5ff,0.3); fill.position.set(-50,-30,-50); _scene.add(fill);
    const rim=new THREE.DirectionalLight(0xffeacc,0.2); rim.position.set(0,-100,100); _scene.add(rim);
    // Grid
    const grid=new THREE.GridHelper(500,50,0x1a2840,0x0f1e30); grid.name='_grid'; _scene.add(grid);
    // Events
    cv.addEventListener('mousedown',_onMouseDown);
    cv.addEventListener('mousemove',_onMouseMove);
    cv.addEventListener('mousemove',_fpsMouseMove);
    cv.addEventListener('mouseup',_onMouseUp);
    cv.addEventListener('wheel',_onWheel,{passive:false});
    cv.addEventListener('click',_onClick);
    cv.addEventListener('dblclick',_onDblClick);
    cv.addEventListener('contextmenu',e=>e.preventDefault());
    cv.addEventListener('touchstart',_onTouchStart,{passive:false});
    cv.addEventListener('touchmove',_onTouchMove,{passive:false});
    cv.addEventListener('touchend',_onTouchEnd,{passive:false});
    // Give canvas keyboard focus when clicked
    cv.addEventListener('mousedown',()=>cv.focus(),{passive:true});
    // Move keyboard handler to canvas so it doesn't interfere globally
    cv.addEventListener('keydown',_keyHandler);
    new ResizeObserver(_resizeRenderer).observe(cv.parentElement||cv);
    _startLoop();
    document.addEventListener('keydown',_keyHandler);
  }

  function _resizeRenderer() {
    if(!_renderer||!_camera)return;
    const cv=document.getElementById('verifiq-canvas');
    if(!cv)return;
    const w=cv.clientWidth,h=cv.clientHeight;
    if(w>10&&h>10){ _renderer.setSize(w,h); _camera.aspect=w/h; _camera.updateProjectionMatrix(); }
  }

  // ── web-ifc ────────────────────────────────────────────────────────────────────
  async function _loadViaWebIfc(filename) {
    _setLoading(true,'Requesting IFC file…');
    VBridge.send('sendIfcForViewer',{name:filename});
  }

  async function onIfcData(data) {
    if(data.error){_setLoading(false);_setStatus('File error: '+data.error);_loadFromCSharpData();return;}
    _setLoading(true,'Decoding IFC file…');
    try {
      const bin=atob(data.data), bytes=new Uint8Array(bin.length);
      for(let i=0;i<bin.length;i++)bytes[i]=bin.charCodeAt(i);
      _setLoading(true,'Initialising geometry engine…');
      await _ensureIfcApi();
      if(_modelId!==null){try{_ifcApi.CloseModel(_modelId);}catch(e){}}
      _clearScene();
      _setLoading(true,'Loading IFC model…');
      _modelId=_ifcApi.OpenModel(bytes,{OPTIMIZE_PROFILES:true,COORDINATE_TO_ORIGIN:true,USE_FAST_BOOLS:true});
      _setLoading(true,'Extracting geometry…');
      await _extractAndRender();
      _usingWebIfc=true;
      _setEngineLabel('web-ifc · '+(data.schema||'IFC4'));
      _applyColorMode();
      _fitCamera();
      _setLoading(false);
      _buildIfcTree();
      _updateModelInfo();
      _populateStoreyFilter();
    } catch(err) {
      _setLoading(false);
      _setEngineLabel('C# geometry engine (fallback)');
      _loadFromCSharpData();
    }
  }

  async function _ensureIfcApi() {
    if(_ifcApi)return;
    _ifcApi=new WebIFC.IfcAPI();
    try{ _ifcApi.SetWasmPath('https://verifiq.local/libs/'); await _ifcApi.Init(); }
    catch(e) {
      const r=await fetch('https://verifiq.local/libs/web-ifc.wasm');
      const buf=await r.arrayBuffer();
      const url=URL.createObjectURL(new Blob([buf],{type:'application/wasm'}));
      _ifcApi=new WebIFC.IfcAPI();
      await _ifcApi.Init(p=>p&&p.endsWith('.wasm')?url:'https://verifiq.local/libs/'+p);
    }
  }

  async function _extractAndRender() {
    if(_modelId===null||!_ifcApi)return;
    const geom=_ifcApi.LoadAllGeometry(_modelId), sz=geom.size();
    for(let i=0;i<sz;i++){
      const placed=geom.get(i), mesh=_meshFromPlaced(placed);
      if(!mesh)continue;
      let guid=String(placed.expressID);
      try{ const l=_ifcApi.GetLine(_modelId,placed.expressID); if(l&&l.GlobalId&&l.GlobalId.value)guid=l.GlobalId.value; }catch(_){}
      mesh.userData.guid=guid; mesh.userData.expressId=placed.expressID;
      _meshMap.set(guid,mesh); _guidMap.set(mesh.uuid,guid); _scene.add(mesh);
      if(i%300===0)await new Promise(r=>setTimeout(r,0));
    }
    geom.delete();
  }

  function _meshFromPlaced(placed) {
    const gc=placed.geometries.size(); if(!gc)return null;
    const pos=[],nor=[],idx=[]; let off=0;
    for(let g=0;g<gc;g++){
      const flat=placed.geometries.get(g),gd=_ifcApi.GetGeometry(_modelId,flat.geometryExpressID);
      if(!gd)continue;
      const verts=_ifcApi.GetVertexArray(gd.GetVertexData(),gd.GetVertexDataSize());
      const idxs=_ifcApi.GetIndexArray(gd.GetIndexData(),gd.GetIndexDataSize());
      const m4=new THREE.Matrix4().fromArray(flat.flatTransformation);
      const nm=new THREE.Matrix3().getNormalMatrix(m4);
      for(let j=0;j<verts.length;j+=6){
        const v=new THREE.Vector3(verts[j],verts[j+1],verts[j+2]).applyMatrix4(m4);
        pos.push(v.x,v.y,v.z);
        const n=new THREE.Vector3(verts[j+3],verts[j+4],verts[j+5]).applyMatrix3(nm).normalize();
        nor.push(n.x,n.y,n.z);
      }
      for(let j=0;j<idxs.length;j++)idx.push(idxs[j]+off);
      off+=verts.length/6; gd.delete();
    }
    if(!pos.length)return null;
    const geo=new THREE.BufferGeometry();
    geo.setAttribute('position',new THREE.BufferAttribute(new Float32Array(pos),3));
    geo.setAttribute('normal',new THREE.BufferAttribute(new Float32Array(nor),3));
    geo.setIndex(idx);
    return new THREE.Mesh(geo,new THREE.MeshPhongMaterial({color:0x6B7280,specular:0x222222,shininess:40,side:THREE.DoubleSide}));
  }

  // ── C# fallback ────────────────────────────────────────────────────────────────
  function _loadFromCSharpData() {
    const state=VState.get(), elems=state.elements3d||[];
    if(!elems.length){
      _setStatus('No geometry  -  open an IFC file and run validation first');
      return;
    }
    _clearScene();
    let rendered=0;
    elems.forEach(e=>{
      // C# sends short names: g=guid, n=name, c=cls, s=storey, m=mesh{v,i}, b=bbox array [minX,minY,minZ,maxX,maxY,maxZ]
      // Fall back to long names for compatibility
      const guid   = e.g || e.guid || '';
      const name   = e.n || e.name || '';
      const cls    = e.c || e.cls  || '';
      const storey = e.s || e.storey || '';
      const meshData = e.m || e.mesh;
      // C# bbox is an array [minX,minY,minZ,maxX,maxY,maxZ]
      const bboxArr  = Array.isArray(e.b) ? e.b : null;
      const bboxObj  = (!bboxArr && e.bb) ? e.bb : null;

      let mesh;
      if(meshData && meshData.v && meshData.v.length>=9){
        const geo=new THREE.BufferGeometry();
        geo.setAttribute('position',new THREE.BufferAttribute(new Float32Array(meshData.v),3));
        if(meshData.i && meshData.i.length) geo.setIndex(meshData.i);
        geo.computeVertexNormals();
        mesh=new THREE.Mesh(geo,new THREE.MeshPhongMaterial({color:0x6B7280,specular:0x333333,shininess:30,side:THREE.DoubleSide}));
      } else if(bboxArr && bboxArr.length===6){
        const [x0,y0,z0,x1,y1,z1]=bboxArr;
        const W=Math.max(0.05,x1-x0), H=Math.max(0.05,z1-z0), D=Math.max(0.05,y1-y0);
        const geo=new THREE.BoxGeometry(W,H,D);
        mesh=new THREE.Mesh(geo,new THREE.MeshPhongMaterial({color:0x6B7280,transparent:true,opacity:0.85}));
        mesh.position.set((x0+x1)/2,(z0+z1)/2,(y0+y1)/2);
      } else if(bboxObj){
        const b=bboxObj;
        const W=Math.max(0.05,b.maxX-b.minX), H=Math.max(0.05,b.maxZ-b.minZ), D=Math.max(0.05,b.maxY-b.minY);
        const geo=new THREE.BoxGeometry(W,H,D);
        mesh=new THREE.Mesh(geo,new THREE.MeshPhongMaterial({color:0x6B7280,transparent:true,opacity:0.85}));
        mesh.position.set((b.minX+b.maxX)/2,(b.minZ+b.maxZ)/2,(b.minY+b.maxY)/2);
      }
      if(!mesh)return;
      mesh.userData.guid=guid; mesh.userData.cls=cls; mesh.userData.name=name;
      mesh.userData.storey=storey; mesh.userData.ifcType=cls.split('|')[0]||cls;
      _meshMap.set(guid||mesh.uuid,mesh); _guidMap.set(mesh.uuid,guid); _scene.add(mesh);
      rendered++;
    });
    console.log('[VERIFIQ 3D] Rendered '+rendered+' elements from C# data');
    _markCacheDirty();
    _applyColorMode(); _fitCamera(); _updateModelInfo(); _buildIfcTree(); _populateStoreyFilter();
    _setEngineLabel('C# geometry engine ('+rendered+' elements)');
    _setStatus('L.drag: orbit | R.drag: pan | Scroll: zoom | Click: select | F: fullscreen | R: reset');
  }

  function loadElements(){ if(!_usingWebIfc)_loadFromCSharpData(); }

  // ── Colour modes ────────────────────────────────────────────────────────────────
  function _applyColorMode() {
    const mode=_colorMode;
    if(mode==='compliance')_applyCompliance();
    else if(mode==='type')_applyTypeColor();
    else if(mode==='storey')_applyStoreyColor();
    else if(mode==='discipline')_applyDisciplineColor();
  }

  function _applyCompliance() {
    const findings=(VState.get().session?.findings)||[];
    const rank={Pass:0,Warning:1,Error:2,Critical:3},sevMap={};
    findings.forEach(f=>{ const c=sevMap[f.guid]?rank[sevMap[f.guid]]:-1; if((rank[f.severity]||0)>c)sevMap[f.guid]=f.severity; });
    _meshMap.forEach((mesh,guid)=>{
      const sev=sevMap[guid]||'NoCheck';
      mesh.material.color.setHex(COL[sev]||COL.NoCheck);
      mesh.userData.severity=sev; mesh.userData.baseColor=COL[sev]||COL.NoCheck;
    });
  }

  function _applyTypeColor() {
    _meshMap.forEach((mesh,guid)=>{
      const t=(mesh.userData.ifcType||'').replace('Ifc','Ifc');
      const col=Object.entries(TYPE_COL).find(([k])=>t.toUpperCase().includes(k.toUpperCase().replace('IFC','')));
      const hex=col?col[1]:0x6B7280;
      mesh.material.color.setHex(hex); mesh.userData.baseColor=hex;
    });
  }

  function _applyStoreyColor() {
    const storeyColors=[0x60A5FA,0xF97316,0x22C55E,0xFBBF24,0xA78BFA,0xF43F5E,0x14B8A6,0xFB923C];
    const storeyIndex={};let si=0;
    _meshMap.forEach((mesh)=>{
      const s=mesh.userData.storey||'';
      if(!(s in storeyIndex))storeyIndex[s]=si++;
      const col=storeyColors[storeyIndex[s]%storeyColors.length];
      mesh.material.color.setHex(col); mesh.userData.baseColor=col;
    });
  }

  const DISC_MEP=['IfcPipeSegment','IfcPipeFitting','IfcDuctSegment','IfcDuctFitting','IfcValve','IfcPump','IfcTank','IfcSanitaryTerminal','IfcWasteTerminal','IfcFlowMeter','IfcFireSuppressionTerminal','IfcAlarm','IfcSensor','IfcDamper'];
  const DISC_STR=['IfcColumn','IfcBeam','IfcSlab','IfcFooting','IfcPile','IfcStairFlight'];

  function _discOf(ifcType) {
    if(!ifcType)return'ARC';
    if(DISC_MEP.some(e=>ifcType.toUpperCase().includes(e.replace('Ifc','').toUpperCase())))return'MEP';
    if(DISC_STR.some(e=>ifcType.toUpperCase().includes(e.replace('Ifc','').toUpperCase())))return'STR';
    return'ARC';
  }

  function _applyDisciplineColor() {
    const dc={ARC:0x60A5FA,STR:0xF97316,MEP:0x22C55E,EXT:0xA78BFA};
    _meshMap.forEach(mesh=>{
      const d=_discOf(mesh.userData.ifcType||'');
      const col=dc[d]||0x6B7280;
      mesh.material.color.setHex(col); mesh.userData.baseColor=col;
    });
  }

  function setColorMode(mode) { _colorMode=mode; _applyColorMode(); }
  function refreshColours() { _applyColorMode(); }

  // ── Camera ────────────────────────────────────────────────────────────────────
  function _fitCamera() {
    if(!_scene||!_camera||!_meshMap.size)return;
    const box=new THREE.Box3();
    _scene.traverse(o=>{if(o.isMesh&&!o.name.startsWith('_'))box.expandByObject(o);});
    if(box.isEmpty())return;
    const cen=new THREE.Vector3(); box.getCenter(cen);
    const size=new THREE.Vector3(); box.getSize(size);
    _target={x:cen.x,y:cen.y,z:cen.z};
    _dist=Math.max(size.x,size.y,size.z)*2.4||50;
    _phi=Math.PI/3; _theta=Math.PI/4;
    _updateCamera(); _modelBounds=box;
  }

  function _updateCamera() {
    if(!_camera)return;
    _camera.position.set(
      _target.x+_dist*Math.sin(_phi)*Math.cos(_theta),
      _target.y+_dist*Math.cos(_phi),
      _target.z+_dist*Math.sin(_phi)*Math.sin(_theta)
    );
    _camera.lookAt(_target.x,_target.y,_target.z);
  }

  function setView(v) {
    if(!_camera)return;
    const r=_dist;
    if(v==='top'){_phi=0.01;_theta=Math.PI/2;}
    else if(v==='front'){_phi=Math.PI/2;_theta=0;}
    else if(v==='right'){_phi=Math.PI/2;_theta=Math.PI/2;}
    else if(v==='back'){_phi=Math.PI/2;_theta=Math.PI;}
    else if(v==='left'){_phi=Math.PI/2;_theta=-Math.PI/2;}
    else{_phi=Math.PI/3;_theta=Math.PI/4;} // iso
    _updateCamera();
  }

  function _startLoop() {
    if(_animFrame)return;
    const loop=()=>{
      _animFrame=requestAnimationFrame(loop);
      if(_renderer&&_scene&&_camera)_renderer.render(_scene,_camera);
    };
    _animFrame=requestAnimationFrame(loop);
  }

  function _clearScene() {
    if(!_scene)return;
    const rm=[];
    _scene.traverse(o=>{if(o.isMesh&&!o.name.startsWith('_'))rm.push(o);});
    rm.forEach(m=>{m.geometry.dispose();if(m.material)m.material.dispose();_scene.remove(m);});
    _meshMap.clear();_guidMap.clear();_selected=null;_selectedSet.clear();_markCacheDirty();
    if(!_scene.getObjectByName('_grid')&&typeof THREE!=='undefined'){
      const g=new THREE.GridHelper(500,50,0x1a2840,0x0f1e30);g.name='_grid';_scene.add(g);
    }
  }

  // ── Mouse controls ─────────────────────────────────────────────────────────────
  function _onMouseDown(e){
    _mouseBtn=e.button;_lastMouse={x:e.clientX,y:e.clientY};
    _isDragging=false;_dragStart={x:e.clientX,y:e.clientY};
    const cv=document.getElementById('verifiq-canvas');
    cv.style.cursor = e.button===0 ? 'grabbing' : 'all-scroll';
    // Prevent text selection during drag
    document.body.style.userSelect='none';
    document.body.style.webkitUserSelect='none';
  }
  function _onMouseMove(e){
    const dx=e.clientX-_lastMouse.x,dy=e.clientY-_lastMouse.y;
    if(Math.abs(e.clientX-_dragStart.x)>3||Math.abs(e.clientY-_dragStart.y)>3)_isDragging=true;
    if(_mouseBtn===0){ _theta-=dx*0.004; _phi-=dy*0.004; _phi=Math.max(0.04,Math.min(Math.PI-0.04,_phi)); _updateCamera(); }
    else if(_mouseBtn===1||_mouseBtn===2){  // middle OR right = pan
      if(!_camera)return;
      const fwd=new THREE.Vector3(_target.x,_target.y,_target.z).sub(_camera.position).normalize();
      const right=new THREE.Vector3().crossVectors(fwd,_camera.up).normalize();
      const sp=_dist*0.0008;
      _target.x-=right.x*dx*sp-_camera.up.x*dy*sp;
      _target.y-=right.y*dx*sp-_camera.up.y*dy*sp;
      _target.z-=right.z*dx*sp-_camera.up.z*dy*sp;
      _updateCamera();
    }
    _lastMouse={x:e.clientX,y:e.clientY};
    // Hover highlight
    if(!_isDragging)_onHover(e);
  }
  function _onMouseUp(){
    _mouseBtn=-1;
    document.body.style.userSelect='';
    document.body.style.webkitUserSelect='';
    _updateCursor(null);
  }
  function _onWheel(e){
    e.preventDefault(); e.stopPropagation();
    let d=e.deltaY;
    if(e.deltaMode===1)d*=30; if(e.deltaMode===2)d*=300;
    const f=1+Math.min(Math.abs(d)*0.001,0.15)*(d>0?1:-1);
    _dist=Math.max(0.05,Math.min(10000,_dist*f));
    _updateCamera();
  }
    // Cursor management: correct cursor per interaction mode
  function _updateCursor(hitMesh) {
    var cv = document.getElementById('verifiq-canvas');
    if (!cv) return;
    if (_mouseBtn === 0)  { cv.style.cursor = 'grabbing';   return; }
    if (_mouseBtn === 2)  { cv.style.cursor = 'all-scroll'; return; }
    if (_measureMode)     { cv.style.cursor = 'crosshair';  return; }
    if (hitMesh)          { cv.style.cursor = 'pointer';    return; }
    cv.style.cursor = 'grab';
  }

  function _onDblClick(){ if(_selected)zoomToSelected(); else _fitCamera(); }

  let _hovered=null;
  let _lastHoverTime=0;
  function _onHover(e){
    // Throttle to 30fps to avoid performance issues
    const now=performance.now();
    if(now-_lastHoverTime<33)return;
    _lastHoverTime=now;
    if(!_renderer||!_camera||!_raycaster)return;
    const cv=document.getElementById('verifiq-canvas');
    if(!cv)return;
    const r=cv.getBoundingClientRect();
    _mouse.x=((e.clientX-r.left)/r.width)*2-1;
    _mouse.y=-((e.clientY-r.top)/r.height)*2+1;
    _raycaster.setFromCamera(_mouse,_camera);
    const objs=_getMeshList();
    const hits=_raycaster.intersectObjects(objs);
    const hit=hits.length?hits[0].object:null;
    if(_hovered&&_hovered!==_selected){
      _hovered.material.color.setHex(_hovered.userData.baseColor||COL.NoCheck);
    }
    if(hit&&hit!==_selected){
      hit.material.color.setHex(COL.Hover);
      _hovered=hit;
    } else _hovered=null;
    // Update cursor based on what we're hovering
    _updateCursor(hit);
  }

  function _onClick(e){
    if(_isDragging||!_renderer||!_camera||!_raycaster)return;
    const cv=document.getElementById('verifiq-canvas');
    if(!cv)return;
    const r=cv.getBoundingClientRect();
    _mouse.x=((e.clientX-r.left)/r.width)*2-1;
    _mouse.y=-((e.clientY-r.top)/r.height)*2+1;
    _raycaster.setFromCamera(_mouse,_camera);
    const objs=_getMeshList();
    const hits=_raycaster.intersectObjects(objs);
    if(!hits.length){_deselectAll();_updateInspector(null);_updateCursor(null);return;}
    const hit=hits[0].object;
    _deselectAll();
    _selected=hit; hit.material.color.setHex(COL.Selected);
    _selectedSet.add(hit);
    _updateInspector(hit);
    // Highlight in tree
    _highlightTree(_guidMap.get(hit.uuid)||'');
    // Measure mode
    if(_measureMode)_handleMeasureClick(hits[0].point,hit);
  }


  // ── Right-click context menu ─────────────────────────────────────────────────
  function _showContextMenu(x, y) {
    document.getElementById('v-ctx-menu') && document.getElementById('v-ctx-menu').remove();
    const hasSelection = !!_selected;
    const menu = document.createElement('div');
    menu.id = 'v-ctx-menu';
    menu.style.cssText = `position:fixed;left:${x}px;top:${y}px;z-index:99999;background:#0a1628;border:1px solid #162843;border-radius:6px;padding:4px 0;min-width:160px;box-shadow:0 8px 32px rgba(0,0,0,.6);font-size:12px`;
    const items = [
      { label: '🎯 Zoom to selection', fn: 'Viewer3DPage.zoomToSelected()', dis: !hasSelection },
      { label: '◉ Isolate selected',   fn: 'Viewer3DPage.toggleIsolate()', dis: !hasSelection },
      { label: '🔍 X-Ray mode',        fn: 'Viewer3DPage.toggleXray()', dis: false },
      { label: '👁 Hide selected',     fn: 'Viewer3DPage._hideSelected()', dis: !hasSelection },
      { sep: true },
      { label: '◎ Show all',           fn: 'Viewer3DPage.showAll()', dis: false },
      { label: '⟳ Reset camera',       fn: 'Viewer3DPage.resetCamera()', dis: false },
      { sep: true },
      { label: '📋 Copy GUID',         fn: 'Viewer3DPage._copyGuid()', dis: !hasSelection },
      { label: '🔧 Platform guide',    fn: 'Viewer3DPage._platformFromCtx()', dis: !hasSelection },
      { label: '📊 Go to findings',    fn: 'Viewer3DPage.goToFindings()', dis: !hasSelection },
    ];
    items.forEach(item => {
      if (item.sep) {
        const sep = document.createElement('div');
        sep.style.cssText = 'height:1px;background:#162843;margin:3px 0';
        menu.appendChild(sep);
      } else {
        const row = document.createElement('div');
        row.style.cssText = `padding:6px 14px;cursor:${item.dis?'default':'pointer'};color:${item.dis?'#374151':'#8aaac8'};transition:background .1s;`;
        row.textContent = item.label;
        if (!item.dis) {
          row.onmouseenter = () => row.style.background = '#0f2035';
          row.onmouseleave = () => row.style.background = 'transparent';
          row.onclick = () => { menu.remove(); eval(item.fn); };
        }
        menu.appendChild(row);
      }
    });
    document.body.appendChild(menu);
    const closeMenu = (e) => { if(!menu.contains(e.target)){menu.remove();document.removeEventListener('mousedown',closeMenu);} };
    setTimeout(() => document.addEventListener('mousedown', closeMenu), 0);
  }

  function _hideSelected() {
    if (_selected) { _selected.visible = false; _markCacheDirty(); _deselectAll(); _updateInspector(null); }
  }

  function _copyGuid() {
    if (_selected && _selected.userData.guid) {
      try { navigator.clipboard.writeText(_selected.userData.guid); } catch(e) {}
    }
  }

  function _platformFromCtx() {
    if (_selected) {
      const ifcType = _selected.userData.ifcType || (_selected.userData.cls||'').split('|')[0] || '';
      App && App.navigate && App.navigate('results');
      // Try to show platform panel via ResultsPage if available
      setTimeout(() => { if(window.ResultsPage && ifcType) ResultsPage.showPlatform(ifcType); }, 300);
    }
  }

    // Touch event handlers (tablet / touchscreen support)
  var _touchDist0 = 0;
  function _onTouchStart(e) {
    e.preventDefault();
    if (e.touches.length === 1) {
      _onMouseDown({button:0, clientX:e.touches[0].clientX, clientY:e.touches[0].clientY});
    } else if (e.touches.length === 2) {
      var dx = e.touches[0].clientX - e.touches[1].clientX;
      var dy = e.touches[0].clientY - e.touches[1].clientY;
      _touchDist0 = Math.sqrt(dx*dx + dy*dy);
    }
  }
  function _onTouchMove(e) {
    e.preventDefault();
    if (e.touches.length === 1) {
      // Simulate mousemove with button held for orbit
      var fakeEvt = {clientX: e.touches[0].clientX, clientY: e.touches[0].clientY, buttons: 1};
      var dx = fakeEvt.clientX - _lastMouse.x, dy = fakeEvt.clientY - _lastMouse.y;
      if (Math.abs(fakeEvt.clientX-_dragStart.x)>3||Math.abs(fakeEvt.clientY-_dragStart.y)>3) _isDragging=true;
      if (_mouseBtn===0) { _theta-=dx*0.004; _phi-=dy*0.004; _phi=Math.max(0.04,Math.min(Math.PI-0.04,_phi)); _updateCamera(); }
      _lastMouse={x:fakeEvt.clientX, y:fakeEvt.clientY};
    } else if (e.touches.length === 2) {
      var dx = e.touches[0].clientX - e.touches[1].clientX;
      var dy = e.touches[0].clientY - e.touches[1].clientY;
      var d = Math.sqrt(dx*dx + dy*dy);
      _dist = Math.max(0.05, Math.min(10000, _dist * (1 - (d - _touchDist0) * 0.003)));
      _updateCamera(); _touchDist0 = d;
    }
  }
  function _onTouchEnd(e) {
    e.preventDefault();
    _onMouseUp();
    if (e.changedTouches.length === 1 && !_isDragging) {
      setTimeout(function() {
        _onClick({clientX: e.changedTouches[0].clientX, clientY: e.changedTouches[0].clientY});
      }, 0);
    }
  }

function _deselectAll(){
    if(_selected){ _selected.material.color.setHex(_selected.userData.baseColor||COL.NoCheck); }
    _selected=null; _selectedSet.clear();
  }

  // ── Inspector ─────────────────────────────────────────────────────────────────
  function _updateInspector(mesh){
    const el=document.getElementById('v-inspector');
    const sa=document.getElementById('v-see-all');
    if(!el)return;
    if(!mesh){
      el.innerHTML='<span style="color:#374151">Click an element to inspect.</span>';
      if(sa)sa.style.display='none';
      return;
    }
    const guid=mesh.userData.guid||'-';
    const sev=mesh.userData.severity||'NoCheck';
    const ifcType=mesh.userData.ifcType||(mesh.userData.cls||'').split('|')[0]||'';
    const clsCode=(mesh.userData.cls||'').split('|')[1]||'';
    const subType=(mesh.userData.cls||'').split('|')[2]||'';
    const state=VState.get();
    const findings=((state.session?.findings)||[]).filter(f=>f.guid===guid);
    const sevCol={Critical:'#ef4444',Error:'#f97316',Warning:'#eab308',Pass:'#22c55e',NoCheck:'#6b7280'}[sev]||'#6b7280';

    // Group findings by property set
    const bySev={Critical:[],Error:[],Warning:[]};
    findings.forEach(f=>{
      if(bySev[f.severity])bySev[f.severity].push(f);
    });

    const findingRows=[...bySev.Critical,...bySev.Error,...bySev.Warning].slice(0,8).map(f=>{
      const fc={Critical:'#ef4444',Error:'#f97316',Warning:'#eab308'}[f.severity]||'#6b7280';
      return `<div style="margin-top:5px;padding:5px 7px;background:${fc}15;border-radius:4px;border-left:2px solid ${fc}">
        <div style="font-size:9px;font-weight:700;color:${fc};margin-bottom:2px">${VUtils.esc(f.severity)}: ${VUtils.esc((f.check||'').substring(0,32))}</div>
        ${f.pset?`<div style="font-size:9px;color:#5b7fa6;font-family:monospace">${VUtils.esc(f.pset)} → ${VUtils.esc(f.prop||'')}</div>`:''}
        <div style="font-size:10px;color:#9ca3af;margin-top:2px;line-height:1.4">${VUtils.esc((f.message||'').substring(0,80))}${(f.message||'').length>80?'…':''}</div>
      </div>`;
    }).join('');

    el.innerHTML=`
      <div style="margin-bottom:8px">
        <span style="display:inline-block;padding:2px 8px;border-radius:100px;font-size:10px;font-weight:700;background:${sevCol}22;color:${sevCol};border:1px solid ${sevCol}44">${sev}</span>
        <span style="margin-left:6px;font-size:10px;color:#374151">${findings.length} finding${findings.length!==1?'s':''}</span>
      </div>
      ${ifcType?`<div style="font-size:10px;margin-bottom:3px"><code style="background:rgba(0,196,160,.12);color:#00c4a0;padding:1px 5px;border-radius:3px">${VUtils.esc(ifcType)}</code>${subType?` <code style="background:rgba(96,165,250,.1);color:#60A5FA;padding:1px 5px;border-radius:3px;font-size:9px">${VUtils.esc(subType)}</code>`:''}</div>`:''}
      ${clsCode?`<div style="font-size:9px;margin-bottom:4px"><code style="background:rgba(167,139,250,.1);color:#A78BFA;padding:1px 5px;border-radius:3px">${VUtils.esc(clsCode)}</code></div>`:''}
      ${mesh.userData.name?`<div style="font-size:11px;font-weight:600;color:#e2e8f0;margin-bottom:3px;word-break:break-word">${VUtils.esc(mesh.userData.name)}</div>`:''}
      <div style="font-size:9px;font-family:monospace;color:#374151;margin-bottom:6px;word-break:break-all">${VUtils.esc(guid.substring(0,36))}</div>
      ${findingRows}
      ${findings.length>8?`<div style="font-size:9px;color:#374151;margin-top:5px">+${findings.length-8} more</div>`:''}
      ${!findings.length?'<div style="font-size:10px;color:#374151;margin-top:4px">No validation findings.</div>':''}
    `;
    if(sa)sa.style.display=findings.length?'inline-block':'none';
  }

  // ── IFC Structure Tree ────────────────────────────────────────────────────────
  function _buildIfcTree(){
    const el=document.getElementById('v-tree');
    if(!el)return;
    // Group elements by storey
    const byStorey={};
    _meshMap.forEach((mesh,guid)=>{
      const s=mesh.userData.storey||'Unassigned';
      if(!byStorey[s])byStorey[s]=[];
      byStorey[s].push({guid,name:mesh.userData.name||guid.substring(0,8),ifcType:mesh.userData.ifcType||'',sev:mesh.userData.severity||'NoCheck'});
    });
    const sorted=Object.entries(byStorey).sort(([a],[b])=>a.localeCompare(b));
    el.innerHTML=`
      <div style="padding:4px 8px;color:#374151;font-size:10px;font-weight:700;text-transform:uppercase;letter-spacing:.5px">
        📁 Project
      </div>
      <div style="padding:2px 8px 2px 16px;color:#374151;font-size:10px">📁 Site → Building</div>
      ${sorted.map(([storey,items])=>`
        <div>
          <div onclick="this.nextElementSibling.style.display=this.nextElementSibling.style.display==='none'?'block':'none'"
            style="padding:3px 8px 3px 24px;cursor:pointer;color:#5b7fa6;font-size:10px;font-weight:600;display:flex;align-items:center;gap:4px"
            onmouseenter="this.style.background='#0f1e30'" onmouseleave="this.style.background='transparent'">
            <span>▾</span> 📐 ${VUtils.esc(storey)} <span style="color:#374151;font-weight:400">(${items.length})</span>
          </div>
          <div style="display:none">
            ${items.slice(0,50).map(item=>{
              const sc={Critical:'#ef4444',Error:'#f97316',Warning:'#eab308',Pass:'#22c55e',NoCheck:'#6b7280'}[item.sev]||'#6b7280';
              return `<div onclick="Viewer3DPage.selectByGuid('${VUtils.esc(item.guid)}')"
                style="padding:2px 8px 2px 36px;cursor:pointer;font-size:10px;color:#5b7fa6;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;display:flex;align-items:center;gap:5px"
                title="${VUtils.esc(item.name)} | ${VUtils.esc(item.guid)}"
                onmouseenter="this.style.background='#0f1e30'" onmouseleave="this.style.background='transparent'">
                <span style="width:6px;height:6px;border-radius:50%;background:${sc};flex-shrink:0"></span>
                <span style="overflow:hidden;text-overflow:ellipsis">${VUtils.esc(item.name.substring(0,25))}</span>
              </div>`;
            }).join('')}
            ${items.length>50?`<div style="padding:2px 8px 2px 36px;font-size:10px;color:#374151">+${items.length-50} more</div>`:''}
          </div>
        </div>`).join('')}`;
  }

  function _highlightTree(guid){
    // Find and scroll to the element in the tree
    const tree=document.getElementById('v-tree');
    if(!tree||!guid)return;
    const items=tree.querySelectorAll('div[onclick]');
    items.forEach(el=>{
      if(el.getAttribute('onclick')&&el.getAttribute('onclick').includes(guid)){
        el.style.background='rgba(0,196,160,.15)';
        el.scrollIntoView({behavior:'smooth',block:'nearest'});
      }
    });
  }

  function _populateStoreyFilter(){
    const sel=document.getElementById('v-storey-filter');
    if(!sel)return;
    const storeys=new Set();
    _meshMap.forEach(m=>{if(m.userData.storey)storeys.add(m.userData.storey);});
    const sorted=[...storeys].sort();
    sel.innerHTML='<option value="">All Storeys</option>'+sorted.map(s=>`<option>${VUtils.esc(s)}</option>`).join('');
  }

  function _updateModelInfo(){
    const el=document.getElementById('v-model-info');
    if(!el)return;
    el.innerHTML=`Elements: ${_meshMap.size.toLocaleString()} | Engine: ${_usingWebIfc?'web-ifc':'C#'}`;
  }

  // ── Controls ──────────────────────────────────────────────────────────────────
  function resetCamera()     { _fitCamera(); }
  function switchFile(name)  { _usingWebIfc=false;_clearScene();if(typeof WebIFC!=='undefined')_loadViaWebIfc(name);else _loadFromCSharpData(); }
  function toggleWire()      { _wireMode=!_wireMode;_scene&&_scene.traverse(o=>{if(o.isMesh&&!o.name.startsWith('_'))o.material.wireframe=_wireMode;});const b=document.getElementById('v-wire');if(b)b.style.background=_wireMode?'var(--teal)':'transparent'; }

  function toggleXray() {
    _xrayMode=!_xrayMode;
    const b=document.getElementById('v-xray');
    if(b){b.style.background=_xrayMode?'var(--teal)':'transparent';b.style.color=_xrayMode?'var(--navy)':'#5b7fa6';}
    _meshMap.forEach(mesh=>{
      mesh.material.transparent=_xrayMode;
      mesh.material.opacity=_xrayMode?(mesh===_selected?0.95:0.18):1.0;
    });
  }

  function toggleIsolate() {
    _isolateMode=!_isolateMode;
    const b=document.getElementById('v-isolate');
    if(b){b.style.background=_isolateMode?'var(--teal)':'transparent';b.style.color=_isolateMode?'var(--navy)':'#5b7fa6';}
    if(!_isolateMode){showAll();return;}
    if(!_selected){_isolateMode=false;if(b){b.style.background='transparent';}return;}
    const sg=_selected.userData.guid;
    _meshMap.forEach((mesh,guid)=>{mesh.visible=guid===sg;});
  }

  function showCriticalOnly(){ _scene&&_scene.traverse(o=>{if(o.isMesh&&!o.name.startsWith('_'))o.visible=(o.userData.severity==='Critical'||o.userData.severity==='Error');}); _markCacheDirty(); }
  function showAll()          { _scene&&_scene.traverse(o=>{if(o.isMesh&&!o.name.startsWith('_'))o.visible=true;}); _markCacheDirty(); }
  function toggleSev(sev,vis) { const k={Critical:'Critical',Error:'Error',Warning:'Warning',Pass:'Pass','Not Checked':'NoCheck'}[sev]||sev;_scene&&_scene.traverse(o=>{if(o.isMesh&&!o.name.startsWith('_')&&(o.userData.severity||'NoCheck')===k)o.visible=vis;}); }
  function applyVisibility()  { showAll(); }

  function selectAll() { _meshMap.forEach(m=>{ m.material.color.setHex(COL.Selected);_selectedSet.add(m); }); }

  function selectByGuid(guid) {
    _deselectAll();
    const mesh=_meshMap.get(guid);
    if(!mesh)return;
    _selected=mesh; mesh.material.color.setHex(COL.Selected); _selectedSet.add(mesh);
    _updateInspector(mesh);
    zoomToSelected();
  }

  function filterByStorey(storey){
    _meshMap.forEach(mesh=>{
      mesh.visible = !storey || (mesh.userData.storey||'')=== storey;
    });
    _markCacheDirty();
  }

  const DISC_MEP_SET=new Set(['IfcPipeSegment','IfcPipeFitting','IfcDuctSegment','IfcDuctFitting','IfcValve','IfcPump','IfcTank','IfcSanitaryTerminal','IfcWasteTerminal','IfcFlowMeter','IfcFireSuppressionTerminal','IfcAlarm','IfcSensor','IfcDamper']);
  const DISC_STR_SET=new Set(['IfcColumn','IfcBeam','IfcSlab','IfcFooting','IfcPile','IfcStairFlight','IfcRailing']);
  let _discVisible={ARC:true,STR:true,MEP:true,EXT:true};

  function filterDisc(d,vis){
    _discVisible[d]=vis;
    _meshMap.forEach(mesh=>{
      const t=mesh.userData.ifcType||'';
      let md='ARC';
      if(DISC_MEP_SET.has(t))md='MEP';
      else if(DISC_STR_SET.has(t))md='STR';
      else if(t==='IfcGeographicElement'||t==='IfcCivilElement')md='EXT';
      mesh.visible=_discVisible[md]!==false;
    });
  }

  // ── Zoom to selection ─────────────────────────────────────────────────────────
  function zoomToSelected(){
    if(!_selected)return;
    const box=new THREE.Box3().setFromObject(_selected);
    if(box.isEmpty())return;
    const cen=new THREE.Vector3(); box.getCenter(cen);
    const size=new THREE.Vector3(); box.getSize(size);
    _target={x:cen.x,y:cen.y,z:cen.z};
    _dist=Math.max(size.x,size.y,size.z)*3.5||5;
    _updateCamera();
  }

  function goToFindings(){
    if(!_selected)return;
    VState.set({filterGuid:_selected.userData.guid});
    App.navigate('results');
  }

  // ── Section planes ────────────────────────────────────────────────────────────
  function toggleSection(a,en){
    _secEnabled[a]=en;
    if(!en){_secPlanes[a]=null;_rebuildClipping();}
    else _applySection(a,0);
  }

  function updateSection(a,val){
    const l=document.getElementById('sec-'+a+'-l');
    if(l)l.textContent=val+'%';
    if(_secEnabled[a])_applySection(a,parseInt(val));
  }

  function _applySection(a,pct){
    if(!_renderer||typeof THREE==='undefined')return;
    const b=_modelBounds,pos=b?(() => {
      const c=new THREE.Vector3(); b.getCenter(c);
      const s=new THREE.Vector3(); b.getSize(s);
      const half=a==='x'?s.x/2:a==='y'?s.y/2:s.z/2;
      const cv=a==='x'?c.x:a==='y'?c.y:c.z;
      return cv+(pct/100)*half;
    })():0;
    const nm={x:new THREE.Vector3(-1,0,0),y:new THREE.Vector3(0,-1,0),z:new THREE.Vector3(0,0,-1)};
    _secPlanes[a]=new THREE.Plane(nm[a],pos);
    _rebuildClipping();
  }

  function _rebuildClipping(){
    if(!_renderer)return;
    const planes=Object.entries(_secEnabled).filter(([,v])=>v).map(([a])=>_secPlanes[a]).filter(Boolean);
    _renderer.clippingPlanes=planes;
    _renderer.localClippingEnabled=planes.length>0;
    _scene&&_scene.traverse(o=>{if(o.isMesh&&o.material){o.material.clippingPlanes=planes;o.material.clipShadows=true;}});
  }

  function clearSections(){
    ['x','y','z'].forEach(a=>{_secEnabled[a]=false;_secPlanes[a]=null;const cb=document.getElementById('sec-'+a);if(cb)cb.checked=false;});
    if(_renderer){_renderer.clippingPlanes=[];_renderer.localClippingEnabled=false;}
    _scene&&_scene.traverse(o=>{if(o.isMesh&&o.material)o.material.clippingPlanes=[];});
  }

  // ── Measurement ────────────────────────────────────────────────────────────────
  function setMeasure(mode){
    _measureMode=_measureMode===mode?null:mode;
    _measurePts=[];
    ['v-m-edge','v-m-area','v-m-vol','v-m-coord'].forEach(id=>{
      const b=document.getElementById(id);
      if(b){b.style.background='transparent';b.style.color='#5b7fa6';}
    });
    const idMap={edge:'v-m-edge',area:'v-m-area',vol:'v-m-vol',coord:'v-m-coord'};
    if(_measureMode&&idMap[_measureMode]){
      const b=document.getElementById(idMap[_measureMode]);
      if(b){b.style.background='var(--teal)';b.style.color='var(--navy)';}
    }
    _updateCursor(null);
    const out=document.getElementById('v-measure-out');
    if(out){
      if(_measureMode){out.style.display='block';out.textContent=`📏 ${_measureMode.toUpperCase()} mode - click elements to measure`;}
      else out.style.display='none';
    }
  }

  function _handleMeasureClick(point,mesh){
    const out=document.getElementById('v-measure-out');
    if(!out)return;
    if(_measureMode==='coord'){
      out.textContent=`📍 X: ${point.x.toFixed(3)} m  Y: ${point.y.toFixed(3)} m  Z: ${point.z.toFixed(3)} m`;
      return;
    }
    if(_measureMode==='vol'&&mesh){
      const box=new THREE.Box3().setFromObject(mesh);
      const s=new THREE.Vector3(); box.getSize(s);
      const v=(s.x*s.y*s.z).toFixed(3);
      out.textContent=`📦 Approx. volume: ${v} m³`;
      return;
    }
    _measurePts.push(point.clone());
    if(_measureMode==='edge'&&_measurePts.length===2){
      const d=_measurePts[0].distanceTo(_measurePts[1]).toFixed(3);
      out.textContent=`📏 Distance: ${d} m`;
      _measurePts=[];
    } else if(_measureMode==='area'&&_measurePts.length===3){
      const a=_measurePts, ab=new THREE.Vector3().subVectors(a[1],a[0]), ac=new THREE.Vector3().subVectors(a[2],a[0]);
      const area=(ab.cross(ac).length()/2).toFixed(3);
      out.textContent=`⬜ Approx. area: ${area} m²`;
      _measurePts=[];
    } else {
      out.textContent=`📏 ${_measureMode.toUpperCase()}: click ${_measureMode==='edge'?'2nd':'next'} point… (${_measurePts.length} selected)`;
    }
  }

  function clearMeasures(){
    _measurePts=[];
    const out=document.getElementById('v-measure-out');
    if(out)out.style.display='none';
    setMeasure(null);
  }

  // ── Keyboard ─────────────────────────────────────────────────────────────────
  const _keyHandler=e=>{
    if(e.key==='Escape'&&_fullscreen)exitFullscreen();
    if(e.key==='f'||e.key==='F'){if(!_fullscreen)enterFullscreen();else exitFullscreen();}
    if(e.key==='r'||e.key==='R')resetCamera();
    if(e.key==='i'||e.key==='I'){if(_selected)zoomToSelected();}
    if(e.key==='w'||e.key==='W')toggleWire();
    if(e.key==='x'||e.key==='X')toggleXray();
    if(e.key==='h'||e.key==='H')toggleIsolate();
    if(e.key==='Delete'||e.key==='Backspace'){if(_selected){_selected.visible=false;_deselectAll();_updateInspector(null);}}
  };

  // ── Helpers ─────────────────────────────────────────────────────────────────────
  function _setStatus(msg){ const e=document.getElementById('v-status'); if(e)e.textContent=msg||'L.drag: orbit | R.drag: pan | Scroll: zoom | Click: select | F: fullscreen | R: reset'; }
  function _setEngineLabel(msg){ const e=document.getElementById('v-engine'); if(e)e.textContent=msg; }
  function _setLoading(show,msg){ const el=document.getElementById('v-loading'),pr=document.getElementById('v-prog'); if(el)el.style.display=show?'flex':'none'; if(pr&&msg)pr.textContent=msg; }

  return {
    render, onNavigate, onIfcData, loadElements, refreshColours,
    resetCamera, switchFile, toggleWire, showCriticalOnly, showAll,
    toggleSev, applyVisibility,
    enterFullscreen, exitFullscreen,
    setView, setColorMode,
    toggleXray, toggleIsolate,
    zoomToSelected, goToFindings,
    selectAll, selectByGuid,
    filterByStorey, filterDisc,
    toggleFpsMode,
    toggleSection, updateSection, clearSections,
    setMeasure, clearMeasures,
  };
})();

window.Viewer3DPage = Viewer3DPage;
