#define MyAppVersion StringChange(GetDateTimeString(" yy.mmdd", "", ""), " ", "1.0.")
;5030紫外消杀
#define MyAppPublisher ""
#define MyAppURL ""
#define MyAppExeName "UI.XRay.Security.Scanner.exe";#define MyAppName "SecurityScanner"

[Setup]
AppId={{EEAF8CD4-78C4-4668-8EF8-76096CB3E1C1}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DisableProgramGroupPage=yes
OutputDir=..\..\XRayScanner_Install
Compression=lzma
SolidCompression=yes
;AppName={#MyAppName}
;DefaultDirName={pf}\\HT\{#MyAppName}{#MyAppVersion}
DefaultGroupName={#MyAppName}
;OutputBaseFilename={#MyAppName} {#MyAppVersion}PrivilegesRequired=admin

[Languages]
Name: "chinesesimp"; MessagesFile: "compiler:Default.isl"
;Name: "english"; MessagesFile: "compiler:Languages\English.isl"

[Tasks]
Name: desktopicon; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce;
Name: quicklaunchicon; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Dirs]
;Name: "{app}\bin\Extensions"; Flags: uninsalwaysuninstall

[Files]
Source: ".\Database\*"; DestDir: "D:\SecurityScanner\Database"; Flags: ignoreversion onlyifdoesntexist uninsneveruninstall
Source: ".\Database\*"; DestDir: "{app}\backup\database\"; Flags:ignoreversion
Source: ".\Class\*"; DestDir: "{app}\bin\"; Flags: ignoreversion
Source: "..\Models\*"; DestDir: "{app}\Models\"; Flags: ignoreversion
Source: "..\Models\DefaultSetting\*"; DestDir: "{app}\Models\DefaultSetting\"; Flags: ignoreversion
Source: "..\Models\DefaultSetting\*"; DestDir: "{tmp}\"; Flags: ignoreversion
Source: "..\Models\Mat\*"; DestDir: "{app}\Models\Mat\"; Flags: ignoreversion recursesubdirs
;Source: "..\Language\*"; DestDir: "{app}\Language"; Flags: ignoreversion
Source: "..\bin\\x86\*"; DestDir: "{app}\bin\x86"; Flags: igNoreversion recursesubdirs createallsubdirs
Source: "..\bin\\*.exe"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\bin\\*.dll"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\bin\\*.config"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\skin\\*"; DestDir: "{app}\skin\"; Flags: ignoreversion

Source: "..\lib\DTCapture_x86\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\lib\RenderEngine\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\lib\PBCapture_x86\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\lib\PBCapture_x86\imageformats\*"; DestDir: "{app}\bin\imageformats"; Flags: ignoreversion
Source: "..\lib\PBCapture_x86\platforms\*"; DestDir: "{app}\bin\platforms"; Flags: ignoreversion
Source: "..\lib\Trace\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\lib\UIControl_x86\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\lib\NetWork\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\lib\EmguLib\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\lib\Configure\*"; DestDir: "{app}\bin"; Flags: ignoreversion

Source: "..\Lib\Keyboard\*"; DestDir: "{app}\bin"; Flags: ignoreversion
Source: "..\Lib\Keyboard\*"; DestDir: "{sys}"
Source: "..\Lib\Keyboard\*"; DestDir: "{syswow64}"; Flags: 64bit;Check:IsWin64
Source: "..\Lib\Env\*"; DestDir: "{app}\Env"; Flags: ignoreversion



[UninstallDelete]
Type: files; Name: "{app}\bin\DTLog.dat"

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"; Tasks: desktopicon;Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\bin\{#MyAppExeName}"; Tasks: quicklaunchicon
Name: "{commondesktop}\Configer";Filename:"{app}\bin\UI.XRay.Security.Configer.exe"; Tasks: desktopicon
;Name: {app}\bin\RunXRayScanner; Filename: {app}\bin\RunXRayScanner.vbs

[Run]
Filename: {app}\Env\VC_redist.x64.exe;  Flags: postinstall runascurrentuser
Filename: {app}\Env\VC_redist.x86.exe;  Flags: postinstall runascurrentuser      
Filename: {app}\bin\UI.XRay.Security.Configer.exe; Parameters: "--dbupdate";
Filename: {app}\bin\UI.XRay.Security.Configer.exe; Parameters: "-i {tmp}\Default.xml";
Filename: {app}\bin\UI.XRay.Security.Configer.exe; Flags: postinstall runascurrentuser

[Code]
// 检查系统是否已安装 .netFrame4.5.1function CheckInstallDotNetFx():boolean;
begin
  if RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\.NETFramework\Policy\v4.0') 
  and RegKeyExists(HKEY_LOCAL_MACHINE, 'SOFTWARE\Microsoft\.NETFramework\v4.0.30319\SKUs\.NETFramework,Version=v4.5.1')then
    begin
      Result := true
    end
  else
    begin
      msgbox('.Net Framework 4.5.1 should be installed.',mbCriticalError,MB_OK)
      Result := false
    end
end;

//安装前，检查.net4.5.1是否已经安装
function InitializeSetup :Boolean;begin
  Result := false  if CheckInstallDotNetFx() then
  Result := true
end; 
