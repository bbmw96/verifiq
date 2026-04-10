// VERIFIQ — IFC Compliance Checker
// Copyright 2026 BBMW0 Technologies. Developed by Jia Wen Gan.
// MainWindow code-behind — orchestrates the desktop shell

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using VERIFIQ.Core.Enums;
using VERIFIQ.Core.Models;
using VERIFIQ.Parser;
using VERIFIQ.Rules;
using VERIFIQ.Rules.Common;
using VERIFIQ.Reports;
using VERIFIQ.Security;
using JsonSerializer      = System.Text.Json.JsonSerializer;
// UseWindowsForms=true brings System.Windows.Forms into scope — disambiguate WPF types explicitly.
using Application         = System.Windows.Application;
using MessageBox          = System.Windows.MessageBox;
using MessageBoxButton    = System.Windows.MessageBoxButton;
using MessageBoxImage     = System.Windows.MessageBoxImage;
using MessageBoxResult    = System.Windows.MessageBoxResult;
using OpenFileDialog      = Microsoft.Win32.OpenFileDialog;

namespace VERIFIQ.Desktop;

public partial class MainWindow : Window
{
    // Current application state
    private CountryMode          _countryMode = CountryMode.Singapore;
    private CorenetGateway       _sgGateway   = CorenetGateway.Construction;
    private MalaysiaPurposeGroup _myPG        = MalaysiaPurposeGroup.All;

    private List<IfcFile>         _loadedFiles  = new();
    private ValidationSession?    _lastSession;
    private readonly ReportGenerator _reporter  = new();
    private CancellationTokenSource? _cts;
    private bool                  _isValidating = false;

    // Rules infrastructure
    private SqliteRulesDatabase? _rulesDb;
    private ValidationEngine?    _engine;
    private DesignCodeEngine?    _designEngine;

    // Stored ProgressChanged delegates — kept as fields so they can be
    // removed before each run, preventing handler accumulation.
    private Action<int, string>? _engineProgressHandler;
    private Action<int, string>? _designProgressHandler;

    // WebView2 initialised flag
    private bool _webViewReady = false;

    public MainWindow()
    {
        InitializeComponent();
        Icon = VqIcon.Get();          // Set VQ logo in title bar and taskbar
        Loaded += MainWindow_Loaded;
    }

    // ─── STARTUP ─────────────────────────────────────────────────────────────

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Update licence labels
        if (App.CurrentLicence is { IsValid: true } lic)
        {
            LicenceTierLabel.Text  = $"[{lic.TierDisplay}]";
            LicencedToLabel.Text   = lic.LicencedOrg.Length > 0
                ? $"  {lic.LicencedOrg}" : string.Empty;
        }

        // Initialise WebView2
        SetStatus("Initialising WebView2...");
        try
        {
            await ContentWebView.EnsureCoreWebView2Async();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"WebView2 initialisation failed: {ex.Message}\n\n" +
                "Please install Microsoft WebView2 Runtime from:\n" +
                "https://developer.microsoft.com/en-us/microsoft-edge/webview2/",
                "VERIFIQ: WebView2 Required",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // Initialise rules database
        SetStatus("Loading rules database...");
        await Task.Run(() =>
        {
            try
            {
                var dbPath = Path.Combine(App.DataPath, "verifiq_rules.db");
                _rulesDb = new SqliteRulesDatabase(dbPath);
                _rulesDb.Initialise();

                _engine = new ValidationEngine(
                    _rulesDb,
                    new EntityClassRules(),
                    new SgRules(),
                    new MyRules(),
                    new BasicGeometryChecker()
                );
                _designEngine = new DesignCodeEngine();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show($"Failed to load rules database: {ex.Message}",
                        "VERIFIQ: Rules Database Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning));
            }
        });

        // Update rules version in status bar
        if (_rulesDb != null)
            RulesVersionText.Text = $"Rules: {_rulesDb.GetRulesDbVersion(_countryMode)}";

        SetStatus("Ready - load an IFC file to begin");
        LoadingOverlay.Visibility = Visibility.Collapsed;

        // ── Network connectivity indicator ─────────────────────────────────
        // Send current status to JS frontend immediately after WebView is ready.
        SendNetworkStatus();

        // Subscribe to connectivity changes so the indicator updates live when
        // the network drops (e.g. power cut on site) or comes back.
        Services.NetworkService.Instance.ConnectivityChanged += online =>
        {
            SendNetworkStatus();
            if (!online)
                SetStatus("Network connection lost — all validation and export features continue offline.");
            else
                SetStatus("Network connection restored.");
        };

        // ── Background services (non-blocking) ────────────────────────────
        // Update check: runs silently in the background with an 8-second timeout.
        // Shows a notification banner inside the app if a newer version is found.
        // The 3D Viewer no longer requires xeokit — it uses a built-in WebGL
        // renderer with zero internet dependency, so no download prompt is needed.
        _ = Task.Run(async () =>
        {
            var checker = new Services.UpdateChecker();
            checker.UpdateAvailable += info =>
            {
                Dispatcher.Invoke(() =>
                    SendToFrontend("updateAvailable", new
                    {
                        current = info.Current,
                        latest  = info.Latest,
                        url     = info.Url,
                        notes   = info.Notes
                    }));
            };
            await checker.CheckAsync();
        });
    }

    private void WebView_Initialized(object? sender,
        CoreWebView2InitializationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;

        // Security settings: keep script dialogs off, enable messaging for bridge,
        // block host object access.
        ContentWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
        ContentWebView.CoreWebView2.Settings.IsWebMessageEnabled             = true;
        ContentWebView.CoreWebView2.Settings.AreHostObjectsAllowed           = false;

        // Navigation whitelist: only allow our virtual host and blank/local pages.
        // verifiq.local is the virtual hostname mapped to the wwwroot folder.
        ContentWebView.CoreWebView2.NavigationStarting += (s, args) =>
        {
            if (!args.Uri.StartsWith("https://verifiq.local") &&
                !args.Uri.StartsWith("http://localhost") &&
                !args.Uri.StartsWith("https://localhost") &&
                !args.Uri.StartsWith("file://") &&
                !args.Uri.StartsWith("about:blank"))
            {
                args.Cancel = true;
            }
        };

        // Bridge: receive messages sent from the JS frontend via VBridge.send()
        ContentWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        // Map the wwwroot folder to the virtual hostname verifiq.local so that
        // scripts, CSS and HTML are served securely without exposing disk paths.
        var wwwroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        ContentWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "verifiq.local", wwwroot,
            CoreWebView2HostResourceAccessKind.Allow);

        // ── WASM MIME type fix ────────────────────────────────────────────────
        // WebView2 virtual host serves .wasm as application/octet-stream, but
        // WebAssembly.instantiateStreaming() requires application/wasm.
        // This handler intercepts .wasm requests and re-serves them with the
        // correct MIME type so web-ifc can load its geometry engine.
        ContentWebView.CoreWebView2.AddWebResourceRequestedFilter(
            "https://verifiq.local/libs/*.wasm",
            Microsoft.Web.WebView2.Core.CoreWebView2WebResourceContext.All);

        ContentWebView.CoreWebView2.WebResourceRequested += (s, ev) =>
        {
            if (!ev.Request.Uri.EndsWith(".wasm",
                System.StringComparison.OrdinalIgnoreCase)) return;

            var wasmFile = ev.Request.Uri
                .Replace("https://verifiq.local/", string.Empty, System.StringComparison.OrdinalIgnoreCase)
                .Replace('/', System.IO.Path.DirectorySeparatorChar);
            var wasmPath = System.IO.Path.Combine(wwwroot, wasmFile);

            if (!System.IO.File.Exists(wasmPath)) return;

            try
            {
                var stream = System.IO.File.OpenRead(wasmPath);
                ev.Response = ContentWebView.CoreWebView2.Environment
                    .CreateWebResourceResponse(stream, 200, "OK",
                        "Content-Type: application/wasm\r\nAccess-Control-Allow-Origin: *");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[VERIFIQ] WASM serve error: {ex.Message}");
            }
        };

        _webViewReady = true;
        NavigateTo("dashboard");
    }

    // ─── COUNTRY MODE ─────────────────────────────────────────────────────────

    private void CountryMode_Changed(object sender, RoutedEventArgs e)
    {
        // IMPORTANT: ModeSingapore has IsChecked="True" in XAML, which fires this
        // handler during InitializeComponent() — BEFORE ActiveModeText and the
        // toolbar buttons in later Grid columns have been instantiated by the
        // XAML parser. Guard against this to prevent NullReferenceException.
        if (ActiveModeText   == null) return;
        if (ValidateButton   == null) return;
        if (ExportButton     == null) return;
        if (StatusText       == null) return;

        if (ModeSingapore.IsChecked == true)
        {
            _countryMode = CountryMode.Singapore;
            ActiveModeText.Text = "Singapore: CORENET-X / IFC+SG";
        }
        else if (ModeMalaysia.IsChecked == true)
        {
            _countryMode = CountryMode.Malaysia;
            ActiveModeText.Text = "Malaysia: NBeS / UBBL 1984";
        }
        else
        {
            _countryMode = CountryMode.Combined;
            ActiveModeText.Text = "Singapore + Malaysia: Combined";
        }

        // Update rules DB version label
        if (_rulesDb != null)
            RulesVersionText.Text = $"Rules: {_rulesDb.GetRulesDbVersion(_countryMode)}";

        // Notify frontend
        if (_webViewReady)
            SendToFrontend("countryModeChanged", new { mode = _countryMode.ToString() });

        // Clear previous results when country mode changes
        if (_lastSession != null)
        {
            _lastSession = null;
            UpdateScorePanel(null);
            ExportButton.IsEnabled = false;
        }

        SetStatus($"Country mode changed to: {_countryMode}");
    }

    // ─── FILE LOADING ─────────────────────────────────────────────────────────

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title  = "Open IFC File: VERIFIQ",
            Filter = "All Supported Files|*.ifc;*.ifcxml;*.ifczip;*.dwg;*.dxf;*.rvt;*.pln;*.nwd;*.bcf;*.e57;*.las;*.obj;*.pdf;*.xlsx;*.json;*.gltf;*.glb;*.usd;*.usdz|" +
                     "IFC Files (*.ifc;*.ifcxml;*.ifczip)|*.ifc;*.ifcxml;*.ifczip|" +
                     "CAD Files (*.dwg;*.dxf;*.dwf;*.dgn)|*.dwg;*.dxf;*.dwf;*.dgn|" +
                     "Native BIM (*.rvt;*.pln;*.skp;*.bimx)|*.rvt;*.pln;*.skp;*.bimx|" +
                     "Coordination (*.nwd;*.nwf;*.nwc;*.bcf)|*.nwd;*.nwf;*.nwc;*.bcf|" +
                     "Point Cloud (*.e57;*.las;*.laz;*.pts;*.xyz)|*.e57;*.las;*.laz;*.pts;*.xyz|" +
                     "3D Exchange (*.obj;*.fbx;*.stl;*.gltf;*.glb;*.usd;*.usdz)|*.obj;*.fbx;*.stl;*.gltf;*.glb;*.usd;*.usdz|" +
                     "Document (*.pdf;*.xlsx;*.json)|*.pdf;*.xlsx;*.json|" +
                     "All Files (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog() != true) return;

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        SetStatus("Loading files...");
        LoadingOverlay.Visibility = Visibility.Visible;

        var dispatcher = new FileFormatDispatcher(OnParseProgress);
        var loadedIfcFiles = new List<IfcFile>();
        var messages       = new List<string>();

        foreach (var path in dlg.FileNames)
        {
            LoadingStatus.Text = $"Loading: {Path.GetFileName(path)}";
            try
            {
                var result = await dispatcher.DispatchAsync(path, _cts.Token);

                if (result.IfcFile != null)
                    loadedIfcFiles.Add(result.IfcFile);

                messages.Add($"{Path.GetFileName(path)}: {result.Message}");
            }
            catch (Exception ex)
            {
                messages.Add($"{Path.GetFileName(path)}: Error: {ex.Message}");
            }
        }

        LoadingOverlay.Visibility = Visibility.Collapsed;

        if (loadedIfcFiles.Count > 0)
        {
            _loadedFiles = loadedIfcFiles;
            ValidateButton.IsEnabled = true;

            int totalElements = _loadedFiles.Sum(f => f.TotalElementCount);
            SetStatus($"Loaded {_loadedFiles.Count} IFC file(s) - {totalElements:N0} elements. " +
                      "Click Run Validation to begin compliance check.");

            // Update frontend with file info
            SendToFrontend("filesLoaded", new
            {
                files = _loadedFiles.Select(f => new
                {
                    name     = f.FileName,
                    elements = f.TotalElementCount,
                    schema   = f.Schema.ToString(),
                    proxies  = f.ProxyElementCount
                }),
                messages
            });

            // Send element geometry for the 3D Viewer.
            // Compact format: g=guid, n=name, c=class, s=storey, b=bounding box [x0,y0,z0,x1,y1,z1].
            // Limited to 2000 elements for smooth rendering — covers all typical project sizes.
            // Works regardless of internet — pure local data from the parsed IFC file.
            var geom = _loadedFiles
                .SelectMany(f => f.Elements)
                .Take(2000)
                .Select(e => new
                {
                    g = e.GlobalId,
                    n = e.Name,
                    c = e.IfcClass,
                    s = e.StoreyName,
                    // Real triangle mesh — sent when available (Brep/SweptSolid models)
                    m = e.Mesh != null && e.Mesh.IsValid ? new
                    {
                        v = e.Mesh.Vertices,   // flat float array: x0,y0,z0,...
                        i = e.Mesh.Indices     // flat int array: i0,i1,i2,...
                    } : null,
                    // Bounding box fallback for models without mesh data
                    b = e.BoundingBox is not null && !e.BoundingBox.IsDegenerate
                        ? new double[]
                          {
                            e.BoundingBox.MinX, e.BoundingBox.MinY, e.BoundingBox.MinZ,
                            e.BoundingBox.MaxX, e.BoundingBox.MaxY, e.BoundingBox.MaxZ,
                          }
                        : null
                });

            SendToFrontend("modelData", new { elements = geom });
            // Navigation to 'files' is handled by the JS bridge when it
            // receives filesLoaded — no WebView reload, so VState is preserved.
        }
        else
        {
            SetStatus("No IFC files loaded. " + string.Join(" | ", messages));
            MessageBox.Show(string.Join("\n\n", messages),
                "VERIFIQ: File Loading Results",
                MessageBoxButton.OK,
                messages.Any(m => m.Contains("Error")) ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
    }

    private void OnParseProgress(int pct, string step)
    {
        Dispatcher.InvokeAsync(() =>
        {
            LoadingBar.IsIndeterminate = pct == 0;
            LoadingBar.Value = pct;
            LoadingStatus.Text = step;
        });
    }

    // ─── VALIDATION ───────────────────────────────────────────────────────────

    private async void RunValidation_Click(object sender, RoutedEventArgs e)
    {
        if (_loadedFiles.Count == 0 || _engine == null) return;
        if (_isValidating) return; // Prevent re-entry

        _isValidating = true;

        // Dispose previous CTS cleanly before creating a new one.
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        ValidateButton.IsEnabled = false;
        ExportButton.IsEnabled   = false;

        // Reset progress bar state from any previous run.
        LoadingBar.IsIndeterminate = true;
        LoadingBar.Value = 0;
        LoadingOverlay.Visibility = Visibility.Visible;
        LoadingStatus.Text = "Starting validation engine...";
        CancelValidationButton.Visibility = Visibility.Visible;
        if (MenuCancelValidation != null) MenuCancelValidation.IsEnabled = true;

        // Remove any previously registered ProgressChanged handlers to prevent
        // handlers accumulating across multiple validation runs.
        if (_engineProgressHandler != null && _engine != null)
            _engine.ProgressChanged -= _engineProgressHandler;
        if (_designProgressHandler != null && _designEngine != null)
            _designEngine.ProgressChanged -= _designProgressHandler;

        // Wire new handlers and store them for removal next run.
        _engineProgressHandler = (pct, step) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                LoadingBar.IsIndeterminate = false;
                LoadingBar.Value = pct;
                LoadingStatus.Text = step;
                // Also update the JS validation page progress indicator
                SendToFrontend("validationProgress", new { pct, step, phase = "data" });
            });
        };

        _designProgressHandler = (pct, step) =>
        {
            Dispatcher.InvokeAsync(() =>
            {
                LoadingBar.IsIndeterminate = false;
                LoadingBar.Value = 60 + pct * 0.4;
                LoadingStatus.Text = step;
                SendToFrontend("validationProgress", new { pct = 60 + (int)(pct * 0.4), step, phase = "design" });
            });
        };

        if (_engine != null)
            _engine.ProgressChanged += _engineProgressHandler;

        SetStatus("Running compliance validation...");

        // Notify the JS frontend that validation has started so the Validation
        // page can show the correct state if the user navigates there.
        SendToFrontend("validationStarted", new
        {
            files = _loadedFiles.Count,
            mode  = _countryMode.ToString()
        });

        try
        {
            // Apply trial element limit — Trial tier is capped at 10 elements per run.
            // All paid tiers (Individual, Practice, Enterprise, Unlimited) are unlimited.
            var licence     = App.CurrentLicence;
            var isTrialMode = licence == null || !licence.IsValid ||
                              licence.Tier == VERIFIQ.Security.LicenceTier.Trial;
            var filesToValidate = isTrialMode
                ? _loadedFiles.Select(f =>
                  {
                      // Create a shallow copy capped at the trial limit
                      var copy = new VERIFIQ.Core.Models.IfcFile
                      {
                          FilePath       = f.FilePath,
                          FileSizeBytes  = f.FileSizeBytes,
                          ParsedAt       = f.ParsedAt,
                          Format         = f.Format,
                          Schema         = f.Schema,
                          Header         = f.Header,
                          Project        = f.Project,
                          Georeference   = f.Georeference,
                      };
                      copy.Sites.AddRange(f.Sites);
                      copy.Buildings.AddRange(f.Buildings);
                      copy.Storeys.AddRange(f.Storeys);
                      copy.Spaces.AddRange(f.Spaces);
                      copy.Elements.AddRange(
                          f.Elements.Take(VERIFIQ.Security.LicenceValidator.TrialElementLimit));
                      return copy;
                  }).ToList()
                : _loadedFiles;

            // Notify UI of trial mode restriction
            if (isTrialMode && _loadedFiles.Sum(f => f.TotalElementCount) > VERIFIQ.Security.LicenceValidator.TrialElementLimit)
            {
                var totalElems = _loadedFiles.Sum(f => f.TotalElementCount);
                await Dispatcher.InvokeAsync(() =>
                    LoadingStatus.Text =
                        $"Trial mode: validating first {VERIFIQ.Security.LicenceValidator.TrialElementLimit} of {totalElems:N0} elements. Activate a licence for full validation.");
            }

            // _engine was checked for null at the top of this method and is
            // guaranteed non-null here. The null-forgiving operator suppresses
            // the CS8602 warning that the compiler cannot eliminate through async paths.
            var session = await _engine!.ValidateAsync(
                filesToValidate,
                _countryMode,
                _countryMode is CountryMode.Singapore or CountryMode.Combined ? _sgGateway : null,
                _countryMode is CountryMode.Malaysia  or CountryMode.Combined ? _myPG      : null,
                _cts.Token
            );

            // ── Run design code checks ────────────────────────────────────────
            if (_designEngine != null)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    LoadingStatus.Text = "Running design code checks...";
                    LoadingBar.IsIndeterminate = true;
                });

                _designEngine.ProgressChanged += _designProgressHandler;

                var designSession = await _designEngine.RunAsync(
                    _loadedFiles, _countryMode, _cts.Token);

                session.DesignCode = designSession;
            }

            _lastSession = session;

            LoadingBar.Value  = 100;
            LoadingBar.IsIndeterminate = false;
            await Task.Delay(200); // Brief pause so user sees 100%.
            LoadingOverlay.Visibility = Visibility.Collapsed;
            CancelValidationButton.Visibility = Visibility.Collapsed;
            if (MenuCancelValidation != null) MenuCancelValidation.IsEnabled = false;
            ValidateButton.IsEnabled = true;
            ExportButton.IsEnabled   = true;

            UpdateScorePanel(session);

            SetStatus(
                $"Validation complete - {session.ComplianceScore:F1}% compliant. " +
                $"{session.CriticalElements} critical, {session.ErrorElements} errors, " +
                $"{session.WarningElements} warnings.");

            // Serialise and send to frontend.
            var dc = session.DesignCode;
            SendToFrontend("validationComplete", new
            {
                score        = session.ComplianceScore,
                designScore  = dc?.DesignComplianceScore,
                overallScore = session.OverallScore,
                total        = session.TotalElements,
                passed       = session.PassedElements,
                warnings     = session.WarningElements,
                errors       = session.ErrorElements,
                critical     = session.CriticalElements,
                proxies      = session.ProxyElements,
                duration     = session.Duration.TotalSeconds,
                errorsByAgency = session.ErrorsByAgency
                    .ToDictionary(k => k.Key.ToString(), k => k.Value),
                errorsByCheck  = session.ErrorsByCheckLevel
                    .ToDictionary(k => k.Key.ToString(), k => k.Value),
                designStats  = dc == null ? null : (object)new
                {
                    total    = dc.TotalChecks,
                    passed   = dc.PassedChecks,
                    failed   = dc.FailedChecks,
                    critical = dc.CriticalChecks,
                    score    = dc.DesignComplianceScore,
                    failsByCategory = dc.FailuresByCategory
                        .ToDictionary(k => k.Key.ToString(), k => k.Value)
                },
                designFindings = dc?.Results
                    .Where(r => !r.Complies)
                    .OrderByDescending(r => r.Severity)
                    .Take(300)
                    .Select(d => new
                    {
                        ruleId   = d.RuleId,
                        ruleName = d.RuleName,
                        codeRef  = d.CodeReference,
                        guid     = d.ElementGuid,
                        name     = d.ElementName,
                        cls      = d.IfcClass,
                        category = d.SpaceCategory,
                        param    = d.CheckParameter,
                        unit     = d.CheckUnit,
                        actual   = d.ActualDisplay,
                        required = d.RequiredDisplay,
                        formula  = d.Formula,
                        result   = d.FormulaResult,
                        severity = d.Severity.ToString(),
                        complies = d.Complies,
                        message  = d.Message,
                        fix      = d.RemediationGuidance
                    }),
                findings = session.Results
                    .OrderByDescending(r => r.Severity)
                    .Take(500)
                    .Select(r => new
                    {
                        guid     = r.ElementGuid,
                        name     = r.ElementName,
                        cls      = r.IfcClass,
                        storey   = r.StoreyName,
                        check    = r.CheckLevel.ToString(),
                        severity = r.Severity.ToString(),
                        agency   = r.AffectedAgency.ToString(),
                        pset     = r.PropertySetName,
                        prop     = r.PropertyName,
                        expected = r.ExpectedValue,
                        actual   = r.ActualValue,
                        message  = r.Message,
                        fix      = r.RemediationGuidance
                    })
            });
            // Navigation to 'results' is handled by the JS bridge when it
            // receives the validationComplete message, so no WebView reload here.

            // Also send a compact element-severity map so the 3D viewer can
            // recolour every element, including those beyond the 500-finding limit.
            // Send severity as JS-compatible string names, not enum integers.
            // Severity enum: Pass=0, Info=1, Warning=2, Error=3, Critical=4
            var sevMap = session.Results
                .GroupBy(r => r.ElementGuid)
                .ToDictionary(
                    g => g.Key,
                    g => g.Max(r => (int)r.Severity) switch
                    {
                        4 => "Critical",
                        3 => "Error",
                        2 => "Warning",
                        _ => "Pass",
                    });
            SendToFrontend("elementSeverities", new { map = sevMap });
        }
        catch (OperationCanceledException)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoadingBar.IsIndeterminate = false;
            CancelValidationButton.Visibility = Visibility.Collapsed;
            if (MenuCancelValidation != null) MenuCancelValidation.IsEnabled = false;
            ValidateButton.IsEnabled  = true;
            SetStatus("Validation cancelled.");
            SendToFrontend("validationCancelled", new { });
        }
        catch (Exception ex)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            LoadingBar.IsIndeterminate = false;
            CancelValidationButton.Visibility = Visibility.Collapsed;
            if (MenuCancelValidation != null) MenuCancelValidation.IsEnabled = false;
            ValidateButton.IsEnabled  = true;
            SetStatus($"Validation error: {ex.Message}");
            MessageBox.Show(
                $"Validation failed: {ex.Message}\n\n{ex.StackTrace}",
                "VERIFIQ: Validation Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            SendToFrontend("validationFailed", new { message = ex.Message });
        }
        finally
        {
            _isValidating = false;
        }
    }

    // ─── EXPORT ───────────────────────────────────────────────────────────────

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        if (_lastSession == null) return;

        // Show export format chooser
        var exportWindow = new ExportWindow(_lastSession, _reporter);
        exportWindow.Owner = this;
        exportWindow.ShowDialog();
    }

    // ─── NAVIGATION ───────────────────────────────────────────────────────────

    private void NavigateTo(string page)
    {
        if (!_webViewReady) return;

        var wwwroot  = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
        var indexPath = Path.Combine(wwwroot, "index.html");
        var uri      = $"https://verifiq.local/index.html?page={page}";
        ContentWebView.CoreWebView2.Navigate(uri);

        // Update sidebar active states
        UpdateSidebarSelection(page);
    }

    private void UpdateSidebarSelection(string page)
    {
        var all = new[] { NavDashboard, NavFiles, NavValidation,
                          Nav3DViewer, NavResults, NavCritical, NavDesign,
                          NavExport, NavRules, NavLicence, NavSettings, NavAbout };

        foreach (var btn in all)
            btn.Style = (Style)FindResource("NavItem");

        var active = page switch
        {
            "dashboard"  => NavDashboard,
            "files"      => NavFiles,
            "validation" => NavValidation,
            "3d"         => Nav3DViewer,
            "results"    => NavResults,
            "critical"   => NavCritical,
            "design"     => NavDesign,
            "export"     => NavExport,
            "rules"      => NavRules,
            "licence"    => NavLicence,
            "settings"   => NavSettings,
            "about"      => NavAbout,
            _            => NavDashboard
        };

        active.Style = (Style)FindResource("NavItemActive");
    }

    // Sidebar navigation: tell JS to navigate without reloading the WebView.
    // This preserves session data (findings, design results) in JavaScript VState.
    private void NavToJs(string page)
    {
        UpdateSidebarSelection(page);
        SendToFrontend("navigateToPage", new { page });
    }

    private void Nav_Dashboard_Click (object s, RoutedEventArgs e) => NavToJs("dashboard");
    private void Nav_Files_Click     (object s, RoutedEventArgs e) => NavToJs("files");
    private void Nav_Validation_Click(object s, RoutedEventArgs e) => NavToJs("validation");
    private void Nav_3DViewer_Click  (object s, RoutedEventArgs e) => NavToJs("3d");
    private void Nav_Results_Click   (object s, RoutedEventArgs e) => NavToJs("results");
    private void Nav_Critical_Click  (object s, RoutedEventArgs e) => NavToJs("critical");
    private void Nav_Design_Click    (object s, RoutedEventArgs e) => NavToJs("design");
    private void Nav_Export_Click    (object s, RoutedEventArgs e) => NavToJs("export");
    private void Nav_Rules_Click     (object s, RoutedEventArgs e) => NavToJs("rules");
    private void Nav_Licence_Click   (object s, RoutedEventArgs e) => NavToJs("licence");
    private void Nav_Settings_Click  (object s, RoutedEventArgs e) => NavToJs("settings");
    private void Nav_About_Click     (object s, RoutedEventArgs e) => NavToJs("about");
    private void Help_Click          (object s, RoutedEventArgs e) => NavToJs("help");

    private void CancelValidation_Click(object s, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelValidationButton.Visibility = Visibility.Collapsed;
        SetStatus("Cancelling validation...");
    }

    private void OpenFile_Click_Sidebar(object s, RoutedEventArgs e) => OpenFile_Click(s, e);

    // ─── WEBVIEW2 MESSAGE BRIDGE ──────────────────────────────────────────────

    private void OnWebMessageReceived(object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            // bridge.js calls postMessage({action, data}) — a JavaScript OBJECT.
            // WebView2 serialises the object and delivers it via WebMessageAsJson as a
            // JSON object string, e.g. {"action":"openFile","data":{}}, ready to deserialise.
            // Handle BOTH postMessage(JSON.stringify(obj)) → string, and
            // postMessage(obj) → object. Both are used by different WebView2 versions.
            var raw = e.WebMessageAsJson ?? "{}";
            string json;
            // If JS sent postMessage(JSON.stringify(...)), WebMessageAsJson wraps it
            // in an extra JSON string layer (starts with '"'). Unwrap it.
            if (raw.Length >= 2 && raw[0] == '"'  && raw[raw.Length - 1] == '"')
            {
                json = JsonSerializer.Deserialize<string>(raw) ?? raw;
            }
            else
            {
                json = raw;
            }
            // PropertyNameCaseInsensitive ensures "action" from JS maps to Action in C#.
            var _opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var msg   = JsonSerializer.Deserialize<FrontendMessage>(json, _opts);
            if (msg == null) return;

            Dispatcher.Invoke(() =>
            {
                switch (msg.Action)
                {
                    case "openFile":
                        OpenFile_Click(this, new RoutedEventArgs());
                        break;
                    case "runValidation":
                        RunValidation_Click(this, new RoutedEventArgs());
                        break;
                    case "export":
                        Export_Click(this, new RoutedEventArgs());
                        break;

                    case "setCountryMode":
                        if (msg.Data?.GetProperty("mode").GetString() is string mode)
                        {
                            if      (mode == "Singapore") ModeSingapore.IsChecked = true;
                            else if (mode == "Malaysia")  ModeMalaysia.IsChecked  = true;
                            else                          ModeCombined.IsChecked   = true;
                        }
                        break;

                    case "setGateway":
                        if (msg.Data?.GetProperty("gateway").GetString() is string gw &&
                            Enum.TryParse<CorenetGateway>(gw, out var gateway))
                        {
                            _sgGateway = gateway;
                            // Confirm back to JS so settings page re-renders with new selection.
                            SendToFrontend("settingsChanged", new
                            {
                                sgGateway = _sgGateway.ToString(),
                                myPG      = _myPG.ToString()
                            });
                        }
                        break;

                    case "setPurposeGroup":
                        if (msg.Data?.GetProperty("pg").GetString() is string pg &&
                            Enum.TryParse<MalaysiaPurposeGroup>(pg, out var mpg))
                        {
                            _myPG = mpg;
                            SendToFrontend("settingsChanged", new
                            {
                                sgGateway = _sgGateway.ToString(),
                                myPG      = _myPG.ToString()
                            });
                        }
                        break;

                    case "activateLicence":
                    {
                        var rawKey     = msg.Data?.GetProperty("key").GetString() ?? string.Empty;
                        var validator  = new VERIFIQ.Security.LicenceValidator();
                        var newLicence = validator.Validate(rawKey);

                        if (newLicence.IsValid)
                        {
                            App.CurrentLicence    = newLicence;
                            LicenceTierLabel.Text = $"[{newLicence.TierDisplay}]";
                            LicencedToLabel.Text  = newLicence.LicencedOrg.Length > 0
                                ? $"  {newLicence.LicencedOrg}" : string.Empty;

                            try
                            {
                                var sp  = Path.Combine(App.AppDataPath, "settings.json");
                                var st  = new VERIFIQ.Core.Models.AppSettings { LicenceKey = rawKey };
                                File.WriteAllText(sp, JsonSerializer.Serialize(st));
                            }
                            catch { /* Non-critical save failure. */ }

                            SendToFrontend("licenceActivated", new
                            {
                                tier      = newLicence.TierDisplay,
                                org       = newLicence.LicencedOrg,
                                to        = newLicence.LicencedTo,
                                perpetual = newLicence.IsPerpetual,
                                maxUsers  = newLicence.MaxUsers == int.MaxValue
                                    ? "Unlimited" : newLicence.MaxUsers.ToString()
                            });
                        }
                        else
                        {
                            SendToFrontend("licenceError", new { message = newLicence.InvalidReason });
                        }
                        break;
                    }

                    case "saveProxySettings":
                    {
                        // Called from the Network Settings section on the Settings page.
                        var d  = msg.Data;
                        var ps = new Services.NetworkProxySettings
                        {
                            UseProxy            = d?.GetProperty("useProxy").GetBoolean() ?? false,
                            ProxyUrl            = d?.GetProperty("proxyUrl").GetString()          ?? string.Empty,
                            Username            = d?.GetProperty("username").GetString()           ?? string.Empty,
                            Password            = d?.GetProperty("password").GetString()           ?? string.Empty,
                            BypassList          = d?.GetProperty("bypassList").GetString()         ?? string.Empty,
                            IgnoreSslErrors     = d?.GetProperty("ignoreSslErrors").GetBoolean()   ?? false,
                            CustomUpdateServerUrl = d?.GetProperty("customUpdateUrl").GetString()  ?? string.Empty,
                        };
                        Services.NetworkService.Instance.SaveProxySettings(ps);
                        SendNetworkStatus();   // Reflect saved settings back to JS
                        break;
                    }

                    case "downloadXeokit":
                        // Legacy: 3D Viewer now uses built-in WebGL — no download needed.
                        System.Windows.MessageBox.Show(
                            "The VERIFIQ 3D Viewer now has a built-in WebGL renderer.\n" +
                            "It works fully offline with no download or internet required.",
                            "VERIFIQ: 3D Viewer",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Information);
                        break;

                    case "removeFile":
                        // Remove a loaded file by filename
                        if (msg.Data?.GetProperty("name").GetString() is string removeName)
                        {
                            _loadedFiles.RemoveAll(f => f.FileName == removeName);
                            // Resend filesLoaded with updated list
                            SendToFrontend("filesLoaded", new
                            {
                                files = _loadedFiles.Select(f => new
                                {
                                    name     = f.FileName,
                                    elements = f.TotalElementCount,
                                    schema   = f.Schema.ToString(),
                                    proxies  = f.ProxyElementCount
                                }),
                                messages = new string[0]
                            });
                            SetStatus(_loadedFiles.Count == 0
                                ? "Ready — load an IFC file to begin"
                                : $"Loaded {_loadedFiles.Count} file(s). Click Run Validation to begin.");
                        }
                        break;

                    case "sendIfcForViewer":
                        // JS requests the raw IFC bytes for a specific file (for web-ifc viewer)
                        if (msg.Data?.GetProperty("name").GetString() is string ifcName)
                        {
                            var targetFile = _loadedFiles.FirstOrDefault(f => f.FileName == ifcName)
                                          ?? _loadedFiles.FirstOrDefault();
                            if (targetFile != null && System.IO.File.Exists(targetFile.FilePath))
                            {
                                try
                                {
                                    var bytes  = System.IO.File.ReadAllBytes(targetFile.FilePath);
                                    var b64    = Convert.ToBase64String(bytes);
                                    SendToFrontend("ifcFileData", new
                                    {
                                        name    = targetFile.FileName,
                                        data    = b64,
                                        schema  = targetFile.Schema.ToString()
                                    });
                                }
                                catch (Exception ex)
                                {
                                    SendToFrontend("ifcFileData", new { error = ex.Message });
                                }
                            }
                        }
                        break;

                    case "navigateTo":
                        if (msg.Data?.GetProperty("page").GetString() is string navPage)
                            UpdateSidebarSelection(navPage);
                        break;

                    case "requestNetworkStatus":
                        SendNetworkStatus();
                        break;

                    case "requestState":
                        SendCurrentState();
                        break;
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Bridge] message parse error: {ex.Message}");
        }
    }

    private void SendToFrontend(string action, object data)
    {
        if (!_webViewReady) return;
        var json = JsonConvert.SerializeObject(new { action, data });
        ContentWebView.CoreWebView2.PostWebMessageAsJson(json);
    }

    private void SendNetworkStatus()
    {
        var ns  = Services.NetworkService.Instance;
        var ps  = ns.ProxySettings;
        SendToFrontend("networkStatus", new
        {
            online           = ns.IsOnline,
            useProxy         = ps.UseProxy,
            proxyUrl         = ps.ProxyUrl,
            username         = ps.Username,
            bypassList       = ps.BypassList,
            ignoreSslErrors  = ps.IgnoreSslErrors,
            customUpdateUrl  = ps.CustomUpdateServerUrl
        });
    }

    private void SendCurrentState()
    {
        SendToFrontend("stateUpdate", new
        {
            countryMode = _countryMode.ToString(),
            sgGateway   = _sgGateway.ToString(),
            myPG        = _myPG.ToString(),
            filesLoaded = _loadedFiles.Select(f => new
            {
                name     = f.FileName,
                elements = f.TotalElementCount,
                schema   = f.Schema.ToString(),
                proxies  = f.ProxyElementCount
            }).ToArray(),
            hasResults  = _lastSession != null,
            score       = _lastSession?.ComplianceScore ?? 0.0,
            licence     = App.CurrentLicence?.TierDisplay ?? "Trial"
        });
    }

    // ─── HELPERS ──────────────────────────────────────────────────────────────

    private void UpdateScorePanel(ValidationSession? session)
    {
        if (session == null)
        {
            ScoreValue.Text   = "-";
            ScoreDetails.Text = "Load a file to begin";
            ScoreValue.Foreground = System.Windows.Media.Brushes.White;
            return;
        }

        // Show overall score if design code results are available, otherwise data score
        double displayScore = session.DesignCode != null
            ? session.OverallScore
            : session.ComplianceScore;

        string scoreLine = session.DesignCode != null
            ? $"{displayScore:F1}% overall"
            : $"{displayScore:F1}%";

        ScoreValue.Text = scoreLine;

        string details = $"{session.CriticalElements} critical\r\n" +
                         $"{session.ErrorElements} errors\r\n" +
                         $"{session.WarningElements} warnings\r\n" +
                         $"{session.PassedElements} compliant";

        if (session.DesignCode != null)
            details += $"\r\nDesign: {session.DesignCode.DesignComplianceScore:F1}%";

        ScoreDetails.Text = details;

        // Colour-code the score: green >= 95%, amber >= 80%, red < 80%
        ScoreValue.Foreground = displayScore >= 95
            ? new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x6E, 0xE7, 0xB7))  // green-300
            : displayScore >= 80
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFC, 0xD3, 0x4D)) // amber-300
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xFC, 0xA5, 0xA5)); // red-300
    }

    private void SetStatus(string message)
    {
        Dispatcher.InvokeAsync(() => StatusText.Text = message);
    }

    private sealed class FrontendMessage
    {
        // JsonPropertyName ensures case-exact mapping regardless of serializer options.
        [System.Text.Json.Serialization.JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public System.Text.Json.JsonElement? Data { get; set; }
    }

    // ─── MENU BAR HANDLERS ────────────────────────────────────────────────

    private void MenuFile_Open(object s, RoutedEventArgs e)   => OpenFile_Click(s, e);
    private void MenuFile_Export(object s, RoutedEventArgs e) => Export_Click(s, e);
    private void MenuFile_Exit(object s, RoutedEventArgs e)   => System.Windows.Application.Current.Shutdown();

    private void MenuValidate_Cancel(object s, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelValidationButton.Visibility = Visibility.Collapsed;
        SetStatus("Cancelling validation...");
    }
    private void MenuValidate_ModeSG(object s, RoutedEventArgs e)
    {
        ModeSingapore.IsChecked = true;
        CountryMode_Changed(s, e);
    }
    private void MenuValidate_ModeMY(object s, RoutedEventArgs e)
    {
        ModeMalaysia.IsChecked = true;
        CountryMode_Changed(s, e);
    }
    private void MenuValidate_ModeCombined(object s, RoutedEventArgs e)
    {
        ModeCombined.IsChecked = true;
        CountryMode_Changed(s, e);
    }

    // Single-format report exports from the menu
    private void MenuReport_Word(object s, RoutedEventArgs e)   => ExportSingleFormat(ExportFormat.Word);
    private void MenuReport_Pdf(object s, RoutedEventArgs e)    => ExportSingleFormat(ExportFormat.PDF);
    private void MenuReport_Excel(object s, RoutedEventArgs e)  => ExportSingleFormat(ExportFormat.Excel);
    private void MenuReport_Csv(object s, RoutedEventArgs e)    => ExportSingleFormat(ExportFormat.CSV);
    private void MenuReport_Html(object s, RoutedEventArgs e)   => ExportSingleFormat(ExportFormat.HTML);
    private void MenuReport_Json(object s, RoutedEventArgs e)   => ExportSingleFormat(ExportFormat.JSON);
    private void MenuReport_Bcf(object s, RoutedEventArgs e)    => ExportSingleFormat(ExportFormat.BCF);

    private async void ExportSingleFormat(ExportFormat format)
    {
        if (_lastSession == null)
        {
            System.Windows.MessageBox.Show("Run validation first to generate a compliance report.",
                "VERIFIQ: No Results", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }
        var dlg = new System.Windows.Forms.SaveFileDialog
        {
            FileName  = _reporter.GetDefaultFileName(_lastSession, format),
            Filter    = GetFilterString(format),
            Title     = $"Save {format} Report"
        };
        if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
        try
        {
            SetStatus($"Exporting {format}...");
            await _reporter.ExportAsync(_lastSession, format, dlg.FileName);
            SetStatus($"Exported: {System.IO.Path.GetFileName(dlg.FileName)}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = System.IO.Path.GetDirectoryName(dlg.FileName), UseShellExecute = true });
        }
        catch (Exception ex) { SetStatus($"Export error: {ex.Message}"); }
    }

    private static string GetFilterString(ExportFormat f) => f switch
    {
        ExportFormat.Word     => "Word Document (*.docx)|*.docx",
        ExportFormat.PDF      => "PDF Report (*.html)|*.html",
        ExportFormat.Excel    => "Excel Workbook (*.xlsx)|*.xlsx",
        ExportFormat.CSV      => "CSV File (*.csv)|*.csv",
        ExportFormat.HTML     => "HTML Report (*.html)|*.html",
        ExportFormat.JSON     => "JSON File (*.json)|*.json",
        ExportFormat.BCF      => "BCF File (*.bcf)|*.bcf",
        _                     => "All Files (*.*)|*.*"
    };

    private void MenuTools_Network(object s, RoutedEventArgs e)       => NavToJs("settings");
    private void MenuTools_CheckUpdates(object s, RoutedEventArgs e)
    {
        _ = Task.Run(async () =>
        {
            var checker = new Services.UpdateChecker();
            checker.UpdateAvailable += info => Dispatcher.Invoke(() =>
                System.Windows.MessageBox.Show(
                    $"VERIFIQ {info.Latest} is available (you have {info.Current}).\n\nDownload from: {info.Url}",
                    "VERIFIQ: Update Available", System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information));
            await checker.CheckAsync();
        });
        SetStatus("Checking for updates...");
    }

    private void MenuHelp_ReportIssue(object s, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            { FileName = "https://bbmw0.com/verifiq/issues", UseShellExecute = true });
    }

}
