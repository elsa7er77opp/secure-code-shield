#define AppName "CyberBank Secure Code Shield"
#define AppVersion "1.0.0 Final"
#define AppPublisher "CyberBank Labs"
#define AppExeName "SecureCodeShield.Desktop.exe"
#define ProductFolder "CyberBank\Secure Code Shield"
#define BuildRoot "..\artifacts\SecureCodeShield-win-x64"
#define BrandIcon "..\src\SecureCodeShield.Desktop\Assets\Brand\secure-code-shield-brand-symbol.ico"

[Setup]
AppId={{CBE7B550-AE57-4DC6-9F01-2DEEC9B393F8}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={localappdata}\Programs\{#ProductFolder}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=CyberBank-Secure-Code-Shield-Final-Setup-win-x64
SetupIconFile={#BrandIcon}
UninstallDisplayIcon={app}\{#AppExeName}
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=lowest
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#BuildRoot}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
