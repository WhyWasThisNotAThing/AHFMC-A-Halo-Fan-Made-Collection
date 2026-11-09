
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define AppName     "AHFMC"
#define AppExe      "AHFMC.exe"
#define PublishDir  "..\bin\Release\net10.0-windows\win-x64\publish"

[Setup]
AppId={{6E1B0C58-3D0A-4F5B-9B65-5A1F7C2E4D91}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName=AHFMC: A Halo Fan-Made Collection {#AppVersion}
AppPublisher=WhyWasThisNotAThing
AppPublisherURL=https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection
AppSupportURL=https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/issues
AppUpdatesURL=https://github.com/WhyWasThisNotAThing/AHFMC-A-Halo-Fan-Made-Collection/releases
VersionInfoVersion={#AppVersion}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\release
OutputBaseFilename=AHFMC-Setup-{#AppVersion}
SetupIconFile=..\Assets\logo.ico
UninstallDisplayIcon={app}\{#AppExe}
Compression=lzma2/ultra
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=*.exe
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Start {#AppName}"; Flags: nowait postinstall skipifsilent
Filename: "{app}\{#AppExe}"; Flags: nowait; Check: RelaunchRequested

[UninstallDelete]
Type: filesandordirs; Name: "{app}"

[Code]
function RelaunchRequested: Boolean;
begin
  Result := ExpandConstant('{param:RELAUNCH|0}') = '1';
end;
