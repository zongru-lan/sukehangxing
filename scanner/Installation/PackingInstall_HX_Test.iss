#define MyAppName "HXSecurityScanner_MH_Test"
#define OemPath "..\Oem\Test"
#include "PackingInstall_Base.iss"

[Setup]
AppName={#MyAppName}
OutputBaseFilename={#MyAppName} {#MyAppVersion}
DefaultDirName={pf}\\HT\{#MyAppName}{#MyAppVersion}

[Files]
; ¿½±´Æ¤·ôÍ¼Æ¬
Source: "{#OemPath}\skin\*"; DestDir: "{app}\Skin"; Flags: ignoreversion recursesubdirs createallsubdirs

; ¿½±´ÓïÑÔÎÄ¼þ
Source: "..\Language\*"; DestDir: "{app}\Language"; Flags: ignoreversion
 
