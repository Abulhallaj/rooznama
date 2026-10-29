; Inno Setup script. Built by CI:
;   ISCC /DAppVersion=0.6.0 /DArch=x64 /DSourceDir=<publish folder> /O<output folder> installer\Rooznama.iss
#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif
#ifndef Arch
  #define Arch "x64"
#endif
#ifndef SourceDir
  #define SourceDir "..\publish\x64"
#endif

[Setup]
; Never change AppId: it identifies the app for upgrades and uninstall.
AppId={{8F3A6C52-1B7E-4D9A-A4C3-5E2D7B90F1A6}
AppName=Rooznama
AppVersion={#AppVersion}
AppPublisher=Abulhallaj
AppPublisherURL=https://github.com/Abulhallaj/rooznama
AppSupportURL=https://github.com/Abulhallaj/rooznama/issues
AppUpdatesURL=https://github.com/Abulhallaj/rooznama/releases
DefaultDirName={autopf}\Rooznama
DefaultGroupName=Rooznama
DisableProgramGroupPage=yes
; per-user install by default (no admin rights); the user may choose "for all users" in the dialog
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputBaseFilename=Rooznama-{#AppVersion}-setup-{#Arch}
SetupIconFile=..\Assets\Rooznama.ico
UninstallDisplayIcon={app}\Rooznama.exe
LicenseFile=..\LICENSE
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; the app holds this mutex while running: Setup asks to close it before upgrading
AppMutex=Rooznama.SingleInstance
#if Arch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Rooznama"; Filename: "{app}\Rooznama.exe"
Name: "{autodesktop}\Rooznama"; Filename: "{app}\Rooznama.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Rooznama.exe"; Description: "{cm:LaunchProgram,Rooznama}"; Flags: nowait postinstall skipifsilent

[Code]
// "Start with Windows" is written by the app itself (HKCU Run); remove it on uninstall.
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'Rooznama');
end;
