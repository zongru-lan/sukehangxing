#define MyAppName "HXSecurityScanner-MH"
#include "PackingInstall_Base.iss"

[Setup]
AppName={#MyAppName}
OutputBaseFilename={#MyAppName} {#MyAppVersion}
DefaultDirName={pf}\\HT\{#MyAppName}

[Files]
; ¿½±´Æ¤·ôÍ¼Æ¬
Source: "..\skin\*"; DestDir: "{app}\Skin"; Flags: ignoreversion recursesubdirs createallsubdirs

; ¿½±´ÓïÑÔÎÄ¼þ
Source: "..\Language\*"; DestDir: "{app}\Language"; Flags: ignoreversion

 
