#define MyAppName "HXSecurityScanner-MH"
#include "PackingInstall_Base.iss"

[Setup]
AppName={#MyAppName}
OutputBaseFilename={#MyAppName} {#MyAppVersion}
DefaultDirName={pf}\\HT\{#MyAppName}

[Files]
; ����Ƥ��ͼƬ
Source: "..\skin\*"; DestDir: "{app}\Skin"; Flags: ignoreversion recursesubdirs createallsubdirs

; ���������ļ�
Source: "..\Language\*"; DestDir: "{app}\Language"; Flags: ignoreversion

 
