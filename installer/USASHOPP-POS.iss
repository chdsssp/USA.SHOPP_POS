; Instalador de USASHOPP POS (Inno Setup)
; Requisitos: Inno Setup 6+ (winget install JRSoftware.InnoSetup)
; 1) Publicar primero:  ./build/publish.ps1   (genera publish/win-x64)
; 2) Compilar el instalador:  ISCC installer/USASHOPP-POS.iss
;    (o abrir este archivo en Inno Setup y presionar Compilar)

#define AppName "USASHOPP POS"
#define AppVersion "1.0.0"
#define AppPublisher "USASHOPP"
#define AppExe "USASHOPP POS.exe"

[Setup]
AppId={{F40C9191-03C5-451F-8543-4605B113BEDC}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir=..\dist
OutputBaseFilename=USASHOPP-POS-Setup-{#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
; Requiere permisos de administrador para instalar en Archivos de programa
PrivilegesRequired=admin

[Languages]
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"

[Files]
; Copia toda la publicación self-contained
Source: "..\publish\win-x64\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion

[Dirs]
; Carpeta de datos compartida (base, respaldos y logs). La app también la crea sola.
Name: "{commonappdata}\USASHOPP POS\data"
Name: "{commonappdata}\USASHOPP POS\backups"
Name: "{commonappdata}\USASHOPP POS\logs"

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExe}"; Description: "Iniciar {#AppName}"; Flags: nowait postinstall skipifsilent
