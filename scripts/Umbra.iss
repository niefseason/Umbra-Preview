#define AppName "UMBRA"
#define AppVersion "0.1.0"
[Setup]
AppId={{8E1E5D4C-EA19-4DFC-A899-2CFDA339BE46}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=UMBRA Project
DefaultDirName={localappdata}\Programs\Umbra
DefaultGroupName=UMBRA
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\
OutputBaseFilename=Umbra-0.1.0-Windows-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\assets\umbra.ico
UninstallDisplayIcon={app}\Umbra.exe
LicenseFile=..\LICENSE
DisableProgramGroupPage=yes
CloseApplications=no
RestartApplications=no
SetupLogging=yes
[Files]
Source: "..\artifacts\app\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs
Source: "..\LICENSE"; DestDir: "{app}"; Flags: ignoreversion
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; Flags: unchecked
[Icons]
Name: "{group}\UMBRA"; Filename: "{app}\Umbra.exe"
Name: "{autodesktop}\UMBRA"; Filename: "{app}\Umbra.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\Umbra.exe"; Description: "Open UMBRA"; Flags: nowait postinstall skipifsilent
; No UninstallDelete entries: saves, BIOS, engine data and libraries stay in LocalAppData\Umbra.
