; INNO SETUP SCRIPT FOR AJANS YONETIM SISTEMI
; Bu dosyayi derlemek icin once inno setup (jrsoftware.org) indirin ve kurun.
; Ardindan bu dosyaya cift tiklayin ve yukaridaki "Run (Compile)" tusuna basin.

[Setup]
AppName=Ajans Yonetim Sistemi
AppVersion=1.0.0
DefaultDirName={autopf}\AjansYonetim
DefaultGroupName=Ajans Yonetim Sistemi
OutputDir={userdesktop}\AjansYonetim_Kurulum
OutputBaseFilename=AjansYonetim_Setup
Compression=lzma2/ultra64
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=admin
SetupIconFile=logo\logo1.ico
UninstallDisplayIcon={app}\AjansYonetim.exe

[Files]
; Cikti_Al.ps1 tarafindan uretilen Self-Contained EXE'nin yolu
Source: "C:\Users\holog\OneDrive\Desktop\AjansYonetim_Yayin\AjansYonetim.exe"; DestDir: "{app}"; Flags: ignoreversion
; Eger farkli DLL'ler vs uretilirse hepsini almak isterseniz:
; Source: "{userdesktop}\AjansYonetim_Yayin\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Ajans Yonetim Sistemi"; Filename: "{app}\AjansYonetim.exe"; IconFilename: "{app}\AjansYonetim.exe"
Name: "{autodesktop}\Ajans Yonetim Sistemi"; Filename: "{app}\AjansYonetim.exe"; Tasks: desktopicon; IconFilename: "{app}\AjansYonetim.exe"

[Tasks]
Name: "desktopicon"; Description: "Masaustu Kisayolu Olustur"; GroupDescription: "Ek Gorevler:"

[Run]
Filename: "{app}\AjansYonetim.exe"; Description: "Ajans Yonetim Sistemini Calistir"; Flags: nowait postinstall skipifsilent
