; VERIFIQ v2.0.0 Installer Script (Inno Setup 6+)
; BBMW0 Technologies
; BEFORE RUNNING: publish the app in Visual Studio first:
;   Right-click VERIFIQ.Desktop → Publish → Folder → publish\VERIFIQ-v2.0.0
;   Configuration: Release | Self-contained | win-x64
; Then open this file in Inno Setup Compiler and press F9 to build.

[Setup]
AppName=VERIFIQ
AppVersion=2.0.0
AppPublisher=BBMW0 Technologies
AppPublisherURL=https://verifiq.bbmw0.com
AppSupportURL=mailto:bbmw0@hotmail.com
DefaultDirName={autopf}\VERIFIQ
DefaultGroupName=VERIFIQ
OutputDir=installer-output
OutputBaseFilename=VERIFIQ-v2.0.0-Setup
SetupIconFile=src\VERIFIQ.Desktop\Assets\verifiq.ico
UninstallDisplayIcon={app}\VERIFIQ.exe
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
MinVersion=10.0.18362
PrivilegesRequired=admin
WizardStyle=modern
DisableDirPage=yes
DisableReadyPage=yes
DisableProgramGroupPage=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Files]
Source: "publish\VERIFIQ-v2.0.0\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs

[Icons]
Name: "{group}\VERIFIQ";              Filename: "{app}\VERIFIQ.exe"
Name: "{group}\Uninstall VERIFIQ";    Filename: "{uninstallexe}"
Name: "{commondesktop}\VERIFIQ";      Filename: "{app}\VERIFIQ.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\VERIFIQ.exe"; Description: "Launch VERIFIQ"; Flags: nowait postinstall skipifsilent

[InstallDelete]
Type: files; Name: "{app}\integrity.manifest"

[Code]
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
