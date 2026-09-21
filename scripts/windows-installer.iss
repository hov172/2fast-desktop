; Inno Setup script for the 2fast Windows desktop app.
;
; Invoked by scripts/package-windows.py, which supplies every define below.
; Build it directly with:
;   ISCC.exe /DAppVersion=1.5.4 /DArch=x64 /DSourceDir=..\dist\windows-x64 ^
;            /DOutputDir=..\dist /DIconFile=..\src\Project2FA.Uno\Platforms\Desktop\icon.ico ^
;            scripts\windows-installer.iss
;
; Two deliberate choices, both because this app holds TOTP secrets:
;
;   1. The user picks the scope. Setup defaults to an all-users install into
;      Program Files and elevates for it; the "Install for all users / for me
;      only" dialog offers a per-user install under
;      {localappdata}\Programs\2fast for anyone without admin rights. The
;      {auto*} constants below resolve to the machine-wide or per-user location
;      to match whichever was chosen, so one script serves both.
;      Scriptable as /ALLUSERS or /CURRENTUSER.
;   2. The uninstaller removes only what this installer wrote. There is no
;      [UninstallDelete] entry and none should be added: vaults (.2fa files)
;      live wherever the user chose to put them, and uninstalling the app must
;      never destroy the user's second factors.

#ifndef AppVersion
  #error AppVersion must be passed with /DAppVersion=
#endif
#ifndef Arch
  #error Arch must be passed with /DArch=
#endif
#ifndef SourceDir
  #error SourceDir must be passed with /DSourceDir=
#endif
#ifndef OutputDir
  #error OutputDir must be passed with /DOutputDir=
#endif

[Setup]
; Stable across releases so upgrades replace in place instead of stacking up.
; Never change this GUID.
AppId={{8F3A6C21-5D4E-4B7A-9C2F-1E6D8B4A7C03}
AppName=2fast
AppVersion={#AppVersion}
AppVerName=2fast {#AppVersion}
AppPublisher=2fast Desktop
AppPublisherURL=https://github.com/hov172/2fast-desktop
AppSupportURL=https://github.com/hov172/2fast-desktop/issues
AppUpdatesURL=https://github.com/hov172/2fast-desktop/releases
DefaultDirName={autopf}\2fast
DefaultGroupName=2fast
DisableProgramGroupPage=yes
LicenseFile={#SourceDir}\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename=2fast-windows-{#Arch}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; Default to a machine-wide install, but let anyone without admin rights choose
; a per-user one instead. Every {auto*} path below follows that choice.
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog commandline
#if Arch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
#ifdef IconFile
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\Project2FA.Uno.exe
#endif
; The app is not code-signed; say so rather than letting SmartScreen be the
; first mention of it.
AppComments=Unsigned build. Verify the SHA256 checksum from the release page.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; {autoprograms} and {autodesktop} land in the all-users locations for a
; machine-wide install and the current user's for a per-user one.
Name: "{autoprograms}\2fast"; Filename: "{app}\Project2FA.Uno.exe"
Name: "{autoprograms}\{cm:UninstallProgram,2fast}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\2fast"; Filename: "{app}\Project2FA.Uno.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Project2FA.Uno.exe"; Description: "{cm:LaunchProgram,2fast}"; Flags: nowait postinstall skipifsilent

[Code]
// Refuse to install over a running copy: replacing the exe underneath a live
// process leaves a half-updated install.
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  if Exec('cmd.exe', '/C tasklist /FI "IMAGENAME eq Project2FA.Uno.exe" | find /I "Project2FA.Uno.exe"',
          '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
    begin
      MsgBox('2fast is currently running. Close it before installing.', mbError, MB_OK);
      Result := False;
    end;
  end;
end;
