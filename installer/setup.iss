[Setup]
AppId={{D37F219A-8C3D-4A5B-9B21-8E4B024A51D1}
AppName=Quick Access Hub
AppVersion=1.0.0
AppPublisher=QuickAccessHub
DefaultDirName={localappdata}\Programs\QuickAccessHub
DefaultGroupName=Quick Access Hub
DisableProgramGroupPage=yes
OutputDir=..\installer_output
OutputBaseFilename=QuickAccessHub-Setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
UninstallDisplayIcon={app}\QuickAccessHub.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "autostart"; Description: "Start Quick Access Hub automatically when Windows starts"; GroupDescription: "Startup options:"

[Files]
Source: "..\QuickAccessHub\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\Quick Access Hub"; Filename: "{app}\QuickAccessHub.exe"
Name: "{autodesktop}\Quick Access Hub"; Filename: "{app}\QuickAccessHub.exe"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "QuickAccessHub"; ValueData: """{app}\QuickAccessHub.exe"" --autostart"; Tasks: autostart; Flags: uninsdeletevalue

[Run]
Filename: "{app}\QuickAccessHub.exe"; Description: "{cm:LaunchProgram,Quick Access Hub}"; Flags: nowait postinstall skipifsilent
