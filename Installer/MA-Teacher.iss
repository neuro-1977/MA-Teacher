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
DefaultDirName={localappdata}\Programs\MA-Teacher
DefaultGroupName=MA-Teacher
DisableProgramGroupPage=yes
OutputDir=bin
OutputBaseFilename=MA-Teacher-Setup
SetupIconFile=..\assets\MA-Teacher.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
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
Source: "deps\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
[Dirs]
Name: "{app}\data"; Permissions: users-modify
[Icons]
Name: "{autoprograms}\MA-Teacher"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\MA-Teacher"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"
[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "Installing the Microsoft Edge WebView2 Runtime..."; Flags: waituntilterminated runhidden; Check: not IsWebView2RuntimeInstalled
Filename: "{app}\{#MyAppExeName}"; Description: "Launch MA-Teacher"; Flags: nowait postinstall skipifsilent
[Code]
function IsWebView2RuntimeInstalled: Boolean;
var
  Version: String;
  ClientKey: String;
begin
  ClientKey := 'Software\Microsoft\EdgeUpdate\Clients\{F1E7E6E8-F5D4-4DAD-B28D-478D62A3E4AE}';
  Result := RegQueryStringValue(HKCU, ClientKey, 'pv', Version) and
    (Version <> '') and (Version <> '0.0.0.0');
  if not Result then
    Result := RegQueryStringValue(HKLM32, ClientKey, 'pv', Version) and
      (Version <> '') and (Version <> '0.0.0.0');
end;
