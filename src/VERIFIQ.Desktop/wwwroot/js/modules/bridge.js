// VERIFIQ — WebView2 Bridge
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.

'use strict';

const Bridge = (() => {
  function send(action, data = {}) {
    if (window.chrome && window.chrome.webview) {
      // Always send as a JSON string. This is supported by ALL WebView2 versions.
      // The C# handler unwraps the outer string encoding before deserialising.
      window.chrome.webview.postMessage(JSON.stringify({ action, data }));
    }
  }

  function init() {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.addEventListener('message', e => {
        try {
          const msg = typeof e.data === 'string' ? JSON.parse(e.data) : e.data;
          handleIncoming(msg);
        } catch (err) {
          console.warn('[Bridge] Failed to parse incoming message:', err);
        }
      });
    }
    send('requestState');
  }

  function handleIncoming(msg) {
    if (!msg || !msg.action) return;
    const { action, data } = msg;

    switch (action) {

      case 'stateUpdate':
        VState.set({
          countryMode:    data.countryMode    || 'Singapore',
          sgGateway:      data.sgGateway      || 'Construction',
          myPurposeGroup: data.myPG           || 'All',
          filesLoaded:    data.filesLoaded    || [],
          hasResults:     !!data.hasResults,
          score:          data.score          || 0,
          licence:        data.licence        || 'Trial',
        });
        break;

      case 'countryModeChanged':
        VState.set({ countryMode: data.mode, hasResults: false, session: null });
        App.refresh();
        break;

      case 'filesLoaded':
        VState.set({
          filesLoaded:  data.files || [],
          hasResults:   false,
          session:      null,
          score:        0,
        });
        App.navigate('files');
        break;

      case 'validationStarted':
        VState.set({ loading: true, hasResults: false, session: null });
        App.refresh();
        break;

      case 'elementSeverities':
        // Full element severity map (guid → severity string) for 3D viewer colouring.
        // Separate from validationComplete findings which are capped at 500.
        VState.set({ elementSeverities: data.map || {} });
        if (window.Viewer3DPage) Viewer3DPage.refreshColours();
        break;

      case 'modelData':
        // Element geometry from C# — bounding boxes for every IFC element.
        // Store in VState then feed to the 3D viewer immediately if it is open.
        VState.set({ elements3d: data.elements || [] });
        if (window.Viewer3DPage) Viewer3DPage.loadElements();
        break;

      case 'validationComplete':
        // Merge data and design findings into unified session object
        VState.set({
          hasResults:   true,
          loading:      false,
          session:      data,
          score:        data.score        || 0,
          designScore:  data.designScore  || null,
          overallScore: data.overallScore || data.score || 0,
        });
        // Recolour the 3D viewer with compliance results (if already open)
        if (window.Viewer3DPage) Viewer3DPage.refreshColours();
        App.navigate('results');
        break;

      case 'validationProgress':
        // Live update of validation progress bar in the JS Validation page
        VState.set({ validationProgress: data });
        (function() {
          const bar = document.getElementById('val-progress-bar');
          const lbl = document.getElementById('val-progress-label');
          if (bar) bar.style.width = (data.pct || 0) + '%';
          if (lbl) lbl.textContent = data.step || '';
        })();
        break;

      case 'validationCancelled':
        VState.set({ loading: false });
        App.refresh();
        break;

      case 'validationFailed':
        VState.set({ loading: false });
        App.refresh();
        break;

      case 'ifcFileData':
        // C# responds with base64 IFC file data for the 3D viewer
        if (window.Viewer3DPage) Viewer3DPage.onIfcData(data);
        break;

      case 'networkStatus':
        VState.set({ online: data.online, proxySettings: data });
        App.refresh();
        break;

      case 'settingsChanged':
        // C# confirmed a gateway or purpose-group change — update VState and
        // re-render the settings page so the new selection is highlighted.
        if (data.sgGateway) VState.set({ sgGateway: data.sgGateway });
        if (data.myPG)      VState.set({ myPurposeGroup: data.myPG });
        App.refresh();
        break;

      case 'licenceActivated':
        VState.set({ licence: data.tier || 'Unknown' });
        App.navigate('licence');
        break;

      case 'licenceError':
        // Show the error inline — no alert dialog needed since the Licence
        // page input form handles error display itself.
        if (window._licenceErrorCallback) window._licenceErrorCallback(data.message);
        break;

      case 'navigateToPage':
        // C# sidebar button pressed — navigate the JS router without WebView reload.
        if (data && data.page) App.navigate(data.page);
        break;

      case 'updateAvailable':
        // Non-intrusive update banner shown at the top of the page.
        if (window._showUpdateBanner) window._showUpdateBanner(data);
        break;

      default:
        console.debug('[Bridge] unhandled action:', action);
    }
  }

  const openFile        = ()       => send('openFile');
  const removeFile      = name     => send('removeFile',        { name });
  const sendIfcForViewer = name    => send('sendIfcForViewer',  { name });
  const runValidate  = ()     => send('runValidation');
  const exportReport = ()     => send('export');
  const setMode      = mode   => send('setCountryMode',    { mode });
  const setGateway   = gw     => send('setGateway',        { gateway: gw });
  const setPG        = pg     => send('setPurposeGroup',   { pg });
  const navigateTo   = page   => send('navigateTo',        { page });
  const saveProxy    = cfg    => send('saveProxySettings', cfg);
  const downloadXeokit = ()   => send('downloadXeokit');

  const runValidation = runValidate;  // alias
  return { init, send, openFile, removeFile, sendIfcForViewer, runValidate, runValidation, exportReport, setMode, setGateway, setPG, navigateTo, saveProxy, downloadXeokit };
})();

window.VBridge = Bridge;
