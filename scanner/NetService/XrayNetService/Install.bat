%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil.exe "%~dp0TRS_InformationService.exe"
Net start XrayNetService
sc config XrayNetService start = auto