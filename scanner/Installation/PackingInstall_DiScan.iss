#define MyAppName "DiScan"
#define OemPath "..\Oem\DiScan"
#include "PackingInstall_Base.iss"

[Setup]
AppName={#MyAppName}
OutputBaseFilename={#MyAppName} {#MyAppVersion}
DefaultDirName={pf}\\NPO\{#MyAppName}{#MyAppVersion}

[Files]
; ����Ƥ��ͼƬ
Source: "{#OemPath}\skin\*"; DestDir: "{app}\Skin"; Flags: ignoreversion recursesubdirs createallsubdirs

; ���������ļ�
;Source: "..\Language\English.xml"; DestDir: "{app}\Language"; Flags: ignoreversion
Source: "..\Language\Russian.xml"; DestDir: "{app}\Language"; Flags: ignoreversion

; �������ͺŵĲ��������ļ�
Source: "..\Models\DefaultSetting\*"; DestDir: "{app}\Models\DefaultSetting"; Flags: ignoreversion

[Run]
Filename: {app}\bin\UI.XRay.Security.Configer.exe; Parameters: "-i {tmp}\DiScanDefault.xml";
