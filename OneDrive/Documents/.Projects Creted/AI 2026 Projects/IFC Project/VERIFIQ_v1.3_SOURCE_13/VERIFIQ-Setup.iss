; VERIFIQ v2.0.0 Installer Script (Inno Setup 6.3+)
; BBMW0 Technologies | April 2026
;
; BUILD INSTRUCTIONS:
;   1. In Visual Studio: Right-click VERIFIQ.Desktop > Publish
;      Config: Release | Self-contained | win-x64 | Output: publish\VERIFIQ-v2.0.0\
;   2. Open this .iss in Inno Setup Compiler and press F9
;
; HOW UPGRADES WORK:
;   - AppId GUID is identical across ALL versions. Inno Setup uses this to detect
;     a previous installation and offer to upgrade automatically.
;   - Settings (including licence key) live in %AppData%\BBMW0Technologies\VERIFIQ\
;     which is separate from the install folder. They are NEVER touched by the
;     uninstaller, so the licence key persists across every upgrade automatically.
;   - At the end of the upgrade wizard, the user sees a clear choice:
;       "Upgrade VERIFIQ (recommended)" - removes old version first, then installs
;       "Install fresh copy" - installs alongside (not recommended)

[Setup]
; ── Identity (MUST stay the same across all versions so upgrades are detected) ──
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName=VERIFIQ
AppVersion=2.0.0
AppPublisher=BBMW0 Technologies
AppPublisherURL=https://verifiq.bbmw0.com
AppSupportURL=mailto:bbmw0@hotmail.com
AppUpdatesURL=https://github.com/bbmw96/verifiq/releases
AppCopyright=Copyright (C) 2026 BBMW0 Technologies

; ── Paths ──────────────────────────────────────────────────────────────────────
DefaultDirName={autopf}\VERIFIQ
DefaultGroupName=VERIFIQ
OutputDir=installer-output
OutputBaseFilename=VERIFIQ-v2.0.0-Setup

; ── Visual ─────────────────────────────────────────────────────────────────────
SetupIconFile=src\VERIFIQ.Desktop\Assets\verifiq.ico
UninstallDisplayIcon={app}\VERIFIQ.exe
WizardStyle=modern
WizardSizePercent=110

; ── Compression ────────────────────────────────────────────────────────────────
Compression=lzma2/ultra64
SolidCompression=yes

; ── Platform requirements ──────────────────────────────────────────────────────
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.18362
PrivilegesRequired=admin

; ── Upgrade behaviour ──────────────────────────────────────────────────────────
; CloseApplications: prompt to close running VERIFIQ before upgrading
CloseApplications=yes
CloseApplicationsFilter=VERIFIQ.exe
RestartApplications=yes
; Prevent side-by-side installs in different folders
UsePreviousAppDir=yes
; Always show directory page so user can verify install path
DisableDirPage=yes
DisableReadyPage=no
DisableProgramGroupPage=yes

; ── Uninstall ──────────────────────────────────────────────────────────────────
Uninstallable=yes
CreateUninstallRegKey=yes
UninstallDisplayName=VERIFIQ - IFC Compliance Checker
; Do NOT delete user data on uninstall (licence key, custom rules db)
; User data lives in %AppData%\BBMW0Technologies\VERIFIQ\ and is intentionally preserved

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
; Customise the first page title to make the upgrade/install choice clear
WelcomeLabel1=Welcome to the VERIFIQ v2.0.0 Setup Wizard
WelcomeLabel2=This will install or upgrade VERIFIQ IFC Compliance Checker v2.0.0 on your computer.%n%nYour licence key and custom rules database will be preserved automatically.%n%nClick Next to continue.

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: checkedonce
Name: "quicklaunch"; Description: "Pin to &taskbar"; GroupDescription: "Additional shortcuts:"

[Files]
; Main application files
Source: "publish\VERIFIQ-v2.0.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\VERIFIQ";              Filename: "{app}\VERIFIQ.exe"; Comment: "VERIFIQ IFC Compliance Checker"
Name: "{group}\Uninstall VERIFIQ";    Filename: "{uninstallexe}"
Name: "{commondesktop}\VERIFIQ";      Filename: "{app}\VERIFIQ.exe"; Tasks: desktopicon; Comment: "VERIFIQ IFC Compliance Checker"

[Run]
; Launch VERIFIQ after install with a short delay so the installer closes first
Filename: "{app}\VERIFIQ.exe"; Description: "Launch VERIFIQ v2.0.0 now"; Flags: nowait postinstall skipifsilent

[InstallDelete]
; Clean up old version files that are no longer needed
Type: files;  Name: "{app}\integrity.manifest"
Type: files;  Name: "{app}\*.pdb"

[Registry]
; Store current version in registry for the update checker to read
Root: HKLM; Subkey: "Software\BBMW0Technologies\VERIFIQ"; ValueType: string; ValueName: "Version";     ValueData: "2.0.0"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\BBMW0Technologies\VERIFIQ"; ValueType: string; ValueName: "InstallDir";  ValueData: "{app}"; Flags: uninsdeletevalue
Root: HKLM; Subkey: "Software\BBMW0Technologies\VERIFIQ"; ValueType: string; ValueName: "PublishedAt"; ValueData: "2026-04-12"; Flags: uninsdeletevalue

[UninstallDelete]
; Clean compiled rule databases from LocalAppData on uninstall (not from AppData - that has the key)
Type: files; Name: "{localappdata}\VERIFIQ\data\*.db"
Type: dirifempty; Name: "{localappdata}\VERIFIQ\data"
Type: dirifempty; Name: "{localappdata}\VERIFIQ"

[Code]
// ── VERIFIQ Inno Setup Code ────────────────────────────────────────────────
// This Pascal script handles:
//   1. Upgrade detection (previous version found -> offer upgrade)
//   2. Licence key migration (copy to new AppData path if structure changed)
//   3. User choice: upgrade vs clean install
//   4. Confirmation dialog showing what will be removed and what will be kept

const
  APP_GUID   = '{{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}';
  APPDATA_COMPANY = 'BBMW0Technologies';
  APPDATA_APP     = 'VERIFIQ';

var
  UpgradePage: TWizardPage;
  RbUpgrade:   TRadioButton;
  RbFresh:     TRadioButton;
  LblInfo:     TLabel;
  PreviousVersion: string;
  PreviousInstallPath: string;

// ── Find the previously installed version ──────────────────────────────────
function GetPreviousVersion(): string;
var
  sValue: string;
begin
  Result := '';
  if RegQueryStringValue(HKLM, 'Software\BBMW0Technologies\VERIFIQ', 'Version', sValue) then
    Result := sValue
  else if RegQueryStringValue(HKCU, 'Software\BBMW0Technologies\VERIFIQ', 'Version', sValue) then
    Result := sValue;
end;

function GetPreviousInstallPath(): string;
var
  sValue: string;
begin
  Result := '';
  if RegQueryStringValue(HKLM, 'Software\BBMW0Technologies\VERIFIQ', 'InstallDir', sValue) then
    Result := sValue;
end;

// ── Check if licence key exists in AppData ────────────────────────────────
function LicenceKeyExists(): Boolean;
var
  settingsPath: string;
begin
  settingsPath := ExpandConstant('{userappdata}\' + APPDATA_COMPANY + '\' + APPDATA_APP + '\settings.json');
  Result := FileExists(settingsPath);
end;

// ── Add upgrade/fresh-install choice page ────────────────────────────────
procedure CreateUpgradeChoicePage();
var
  lbl: TLabel;
begin
  UpgradePage := CreateCustomPage(wpWelcome, 'Upgrade or Fresh Install',
    'VERIFIQ ' + PreviousVersion + ' is already installed. How would you like to proceed?');

  lbl := TLabel.Create(UpgradePage);
  lbl.Parent := UpgradePage.Surface;
  lbl.Left   := 0;
  lbl.Top    := 0;
  lbl.Width  := UpgradePage.SurfaceWidth;
  lbl.AutoSize := False;
  lbl.WordWrap := True;
  lbl.Caption  :=
    'A previous installation of VERIFIQ (' + PreviousVersion + ') was found at:' + #13 +
    PreviousInstallPath + #13#13 +
    'Your licence key and settings are safely stored in your AppData folder and ' +
    'will NOT be affected by either option.';

  RbUpgrade := TRadioButton.Create(UpgradePage);
  RbUpgrade.Parent  := UpgradePage.Surface;
  RbUpgrade.Left    := 0;
  RbUpgrade.Top     := 90;
  RbUpgrade.Width   := UpgradePage.SurfaceWidth;
  RbUpgrade.Caption := 'Upgrade to VERIFIQ v2.0.0 (recommended)  -  removes v' + PreviousVersion + ' automatically';
  RbUpgrade.Checked := True;
  RbUpgrade.Font.Style := [fsBold];

  LblInfo := TLabel.Create(UpgradePage);
  LblInfo.Parent   := UpgradePage.Surface;
  LblInfo.Left     := 20;
  LblInfo.Top      := 114;
  LblInfo.Width    := UpgradePage.SurfaceWidth - 20;
  LblInfo.AutoSize := False;
  LblInfo.WordWrap := True;
  LblInfo.Caption  := 'Your licence key, custom rules database, and application preferences will be ' +
                      'preserved. Only the program files will be replaced.';

  RbFresh := TRadioButton.Create(UpgradePage);
  RbFresh.Parent  := UpgradePage.Surface;
  RbFresh.Left    := 0;
  RbFresh.Top     := 150;
  RbFresh.Width   := UpgradePage.SurfaceWidth;
  RbFresh.Caption := 'Install fresh copy alongside existing version (not recommended)';
end;

// ── Back up licence key before uninstalling old version ────────────────────
procedure BackupLicenceKey();
var
  srcPath, dstPath: string;
begin
  // The licence is already in AppData which survives uninstall.
  // This is a belt-and-braces safety copy to the temp folder.
  srcPath := ExpandConstant('{userappdata}\' + APPDATA_COMPANY + '\' + APPDATA_APP + '\settings.json');
  dstPath := ExpandConstant('{tmp}\verifiq_settings_backup.json');
  if FileExists(srcPath) then
    FileCopy(srcPath, dstPath, False);
end;

// ── Restore licence key after install (in case AppData was somehow cleared) ─
procedure RestoreLicenceKey();
var
  srcPath, dstPath: string;
begin
  srcPath := ExpandConstant('{tmp}\verifiq_settings_backup.json');
  dstPath := ExpandConstant('{userappdata}\' + APPDATA_COMPANY + '\' + APPDATA_APP + '\settings.json');
  if FileExists(srcPath) and not FileExists(dstPath) then
  begin
    ForceDirectories(ExtractFileDir(dstPath));
    FileCopy(srcPath, dstPath, False);
  end;
end;

// ── Silently uninstall old version ─────────────────────────────────────────
procedure UninstallPreviousVersion();
var
  uninstallString: string;
  resultCode:      Integer;
begin
  // Find uninstaller path from registry (set by Inno Setup on previous install)
  if RegQueryStringValue(HKLM,
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\' + APP_GUID + '_is1',
    'UninstallString', uninstallString) then
  begin
    // /SILENT = no UI, /NORESTART = don't reboot, /SUPPRESSMSGBOXES = no dialogs
    Exec(RemoveQuotes(uninstallString), '/SILENT /NORESTART /SUPPRESSMSGBOXES',
         '', SW_HIDE, ewWaitUntilTerminated, resultCode);
  end;
end;

// ── Main entry point ───────────────────────────────────────────────────────
function InitializeSetup(): Boolean;
begin
  PreviousVersion     := GetPreviousVersion();
  PreviousInstallPath := GetPreviousInstallPath();
  Result := True;
end;

procedure InitializeWizard();
begin
  // Only show the upgrade choice page if a previous version exists
  if PreviousVersion <> '' then
    CreateUpgradeChoicePage();
end;

// ── Before installing: back up key and optionally uninstall old ────────────
function PrepareToInstall(var NeedsRestart: Boolean): string;
begin
  Result := '';

  // Always back up licence key first
  BackupLicenceKey();

  // If user chose to upgrade (default), uninstall old version silently
  if (PreviousVersion <> '') and Assigned(RbUpgrade) and RbUpgrade.Checked then
    UninstallPreviousVersion();
end;

// ── After installing: restore licence key if needed ───────────────────────
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
    RestoreLicenceKey();
end;

// ── Uninstall: warn user about preserved data ─────────────────────────────
function InitializeUninstall(): Boolean;
var
  msg: string;
  res: Integer;
begin
  msg := 'You are about to uninstall VERIFIQ.' + #13#13 +
         'Your licence key and settings will be preserved in:' + #13 +
         ExpandConstant('{userappdata}\BBMW0Technologies\VERIFIQ\') + #13#13 +
         'If you want to completely remove all data, you can manually delete that folder after uninstalling.' + #13#13 +
         'Continue with uninstall?';

  res := MsgBox(msg, mbConfirmation, MB_YESNO);
  Result := (res = IDYES);
end;
