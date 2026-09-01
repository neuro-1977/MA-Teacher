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
Name: "classroomnetwork"; Description: "Allow supervised student links on this school's private network (TCP 5202)"; GroupDescription: "School network (optional):"; Flags: unchecked; Check: IsAdminInstallMode
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

function ClassroomNetworkMarker: String;
begin
  Result := ExpandConstant('{app}\data\classroom-network.owner');
end;

function RunNetsh(const Parameters: String): Boolean;
var
  ResultCode: Integer;
begin
  Result := Exec(ExpandConstant('{sys}\netsh.exe'), Parameters, '', SW_HIDE,
    ewWaitUntilTerminated, ResultCode) and (ResultCode = 0);
  Log(Format('MA-Teacher classroom network command result=%d parameters=%s', [ResultCode, Parameters]));
end;

procedure RemoveOwnedClassroomNetwork;
begin
  if not FileExists(ClassroomNetworkMarker) then
    exit;

  RunNetsh('advfirewall firewall delete rule name="MA-Teacher Classroom Relay"');
  RunNetsh('http delete urlacl url=http://+:5202/');
  DeleteFile(ClassroomNetworkMarker);
end;

function ConfigureClassroomNetwork: Boolean;
var
  UrlReady: Boolean;
  FirewallReady: Boolean;
begin
  Result := False;
  RemoveOwnedClassroomNetwork;

  { BUILTIN\Users may register the listener, while the firewall admits only
    this installed executable on managed Domain/Private profiles. This keeps
    the URL ACL independent of localised account names and UAC credentials. }
  UrlReady := RunNetsh('http add urlacl url=http://+:5202/ sddl=D:(A;;GX;;;BU)');
  if not UrlReady then
  begin
    Log('Classroom URL reservation was not created. Existing unowned reservations are left untouched.');
    exit;
  end;

  FirewallReady := RunNetsh(
    'advfirewall firewall add rule name="MA-Teacher Classroom Relay" dir=in action=allow ' +
    'protocol=TCP localport=5202 profile=private,domain program="' +
    ExpandConstant('{app}\{#MyAppExeName}') + '" enable=yes');
  if not FirewallReady then
  begin
    RunNetsh('http delete urlacl url=http://+:5202/');
    Log('Classroom firewall rule was not created; the new URL reservation was rolled back.');
    exit;
  end;

  Result := SaveStringToFile(ClassroomNetworkMarker,
    'MA-Teacher owns the exact TCP 5202 URL reservation and firewall rule created by this installation.'#13#10,
    False);
  if not Result then
  begin
    RunNetsh('advfirewall firewall delete rule name="MA-Teacher Classroom Relay"');
    RunNetsh('http delete urlacl url=http://+:5202/');
    Log('Classroom ownership marker could not be written; network changes were rolled back.');
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and IsAdminInstallMode and
     WizardIsTaskSelected('classroomnetwork') then
  begin
    if not ConfigureClassroomNetwork then
      SuppressibleMsgBox(
        'MA-Teacher was installed, but the optional classroom network rule could not be created.'#13#10#13#10 +
        'No broad firewall access was granted. Ask school IT to check whether TCP 5202 or the URL reservation is already owned, then run Setup again in administrator mode.',
        mbError, MB_OK, IDOK);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
    RemoveOwnedClassroomNetwork;
end;
