#define MyAppName "B2Scan"
#define OemPath "..\Oem\B2Scan"
#include "PackingInstall_Base.iss"

[Setup]
AppName={#MyAppName}
OutputBaseFilename={#MyAppName} {#MyAppVersion}
DefaultDirName={pf}\\B2Scan\{#MyAppName}{#MyAppVersion}

[Files]
; ¿½±´Æ¤·ôÍ¼Æ¬
Source: "{#OemPath}\skin\*"; DestDir: "{app}\Skin"; Flags: ignoreversion recursesubdirs createallsubdirs

; ¿½±´ÓïÑÔÎÄ¼þ
Source: "..\Language\English.xml"; DestDir: "{app}\Language"; Flags: ignoreversion
Source: "..\Language\Russian.xml"; DestDir: "{app}\Language"; Flags: ignoreversion

Source: "{#OemPath}\Models\DefaultSetting\*"; DestDir: "{app}\Models\DefaultSetting"; Flags: ignoreversion
Source: "{#OemPath}\Models\DefaultSetting\*"; DestDir: "{tmp}\"; Flags: ignoreversion

[Run]
Filename: {app}\bin\UI.XRay.Security.Configer.exe; Parameters: "-i {tmp}\B2ScanDefault.xml";
