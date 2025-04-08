#define MyAppName "DiScan"
#define OemPath "..\Oem\DiScan"
#include "PackingInstall_Base.iss"

[Setup]
AppName={#MyAppName}
OutputBaseFilename={#MyAppName} {#MyAppVersion}
DefaultDirName={pf}\\NPO\{#MyAppName}{#MyAppVersion}

[Files]
; 拷贝皮肤图片
Source: "{#OemPath}\skin\*"; DestDir: "{app}\Skin"; Flags: ignoreversion recursesubdirs createallsubdirs

; 拷贝语言文件
;Source: "..\Language\English.xml"; DestDir: "{app}\Language"; Flags: ignoreversion
Source: "..\Language\Russian.xml"; DestDir: "{app}\Language"; Flags: ignoreversion

; 拷贝各型号的参数配置文件
Source: "..\Models\DefaultSetting\*"; DestDir: "{app}\Models\DefaultSetting"; Flags: ignoreversion

[Run]
Filename: {app}\bin\UI.XRay.Security.Configer.exe; Parameters: "-i {tmp}\DiScanDefault.xml";
