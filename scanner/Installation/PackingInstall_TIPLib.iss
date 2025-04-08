#define TIPPackName "TIP Lib"
#define TIPVersion GetDateTimeString("yyyy.mmdd", "", "")

[Setup]
AppName=TIP Lib
AppVersion=1.0
DefaultDirName=D:\SecurityScanner\TipLib
OutputDir=..\..\XRayScanner_Install
OutputBaseFilename={#TIPPackName} {#TIPVersion}
DisableDirPage=yes
Uninstallable=no

[InstallDelete]
Type: filesandordirs; Name: "D:\SecurityScanner\TipLib";

[Files]
Source: "\\192.168.99.10\XraySoftwareRelease\TipLib\Explosives\*"; DestDir: "D:\SecurityScanner\TipLib\Explosives\"; Flags: recursesubdirs;
Source: "\\192.168.99.10\XraySoftwareRelease\TipLib\Guns\*"; DestDir: "D:\SecurityScanner\TipLib\Guns\"; Flags: recursesubdirs;
Source: "\\192.168.99.10\XraySoftwareRelease\TipLib\Knives\*"; DestDir: "D:\SecurityScanner\TipLib\Knives\"; Flags: recursesubdirs;
Source: "\\192.168.99.10\XraySoftwareRelease\TipLib\Others\*"; DestDir: "D:\SecurityScanner\TipLib\Others\"; Flags: recursesubdirs;