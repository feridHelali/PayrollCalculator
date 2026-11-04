; PayrollCalculator installer (Inno Setup 6). Built by dotnet\release.ps1, which passes:
;   /DAppVersion=1.0.0  /DSourceDir=<self-contained publish folder>  /DOutputDir=<release folder>
; Per-user install, no admin rights, nothing else to install (the .NET runtime ships inside the app).
; The data (database, backups, licence) lives in {app}\data and is never removed by an upgrade or an uninstall.

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef SourceDir
  #error SourceDir is required (the publish folder)
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

#define AppExe "PayrollCalculator.Wpf.exe"

[Setup]
; Never change the AppId: it identifies the product for upgrades and uninstall.
AppId={{6B3F2C1E-8D4A-4F7B-9E2C-5A1D3B7C9E40}
AppName=حاسبة الأجور (PayrollCalculator)
AppVersion={#AppVersion}
AppVerName=حاسبة الأجور {#AppVersion}
AppPublisher=AlfaComputers
AppCopyright=© 2026 AlfaComputers
VersionInfoVersion={#AppVersion}
DefaultDirName={localappdata}\Programs\PayrollCalculator
DefaultGroupName=حاسبة الأجور
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename=PayrollCalculator-Setup-{#AppVersion}
SetupIconFile=..\src\PayrollCalculator.Wpf\Assets\app.ico
UninstallDisplayIcon={app}\{#AppExe}
UninstallDisplayName=حاسبة الأجور {#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
ShowLanguageDialog=auto

[Languages]
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
; The user's data: kept on upgrade and on uninstall.
Name: "{app}\data"; Flags: uninsneveruninstall

[Icons]
Name: "{autoprograms}\حاسبة الأجور"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\حاسبة الأجور"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "{cm:LaunchProgram,حاسبة الأجور}"; Flags: nowait postinstall skipifsilent
