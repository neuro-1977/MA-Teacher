#ifndef MyAppVersion
  #define MyAppVersion "0.1.0"
#endif
#define MyAppName "MA-Teacher"
#define MyAppPublisher "CaptainNeuro"
#define MyAppExeName "MA-Teacher.exe"

[Setup]
AppId={{E29D5046-0AD7-4E5D-908F-3D376D0B2493}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\MA-Teacher
DefaultGroupName=MA-Teacher
DisableProgramGroupPage=yes
OutputDir=bin
OutputBaseFilename=MA-Teacher-Setup
SetupIconFile=..\assets\MA-Teacher.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
ChangesAssociations=no
ChangesEnvironment=no
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=MA-Teacher installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Dirs]
Name: "{app}\data"; Permissions: users-modify

[Icons]
Name: "{autoprograms}\MA-Teacher"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\MA-Teacher"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch MA-Teacher"; Flags: nowait postinstall skipifsilent
