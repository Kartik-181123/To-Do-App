; =====================================================================
; Todo Widget - Inno Setup Installer Script
; Application: Todo Widget
; Publisher:   TodoWidget
; Version:     1.0.0
; Architecture: win-x64 (Self-Contained ReadyToRun)
; =====================================================================

#define MyAppName "To-Do"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Kartik"
#define MyAppExeName "TodoWidget.exe"
#define MyAppDescription "Ultra-lightweight desktop todo widget with streak gamification"

[Setup]
; Unique application GUID for To-Do
AppId={{7E1C87E2-65F9-44F0-99E7-BC8FA71B55A9}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}

; Target architecture: 64-bit Windows
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Installation directory:
DefaultDirName={autopf}\To-Do
DisableDirPage=no

; Start Menu Group
DefaultGroupName=To-Do
DisableProgramGroupPage=no

; Dual-mode installation:
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Output configuration
OutputDir=Output
OutputBaseFilename=ToDoSetup-{#MyAppVersion}

; Installer icon configuration:
; NOTE: Inno Setup requires a valid Windows .ico file for SetupIconFile (PNG is not supported directly).
; To use the desktop icon PNG ("..\Logos\Icon when app is in desktop.png") as the setup icon,
; it is converted to "..\Logos\app.ico".
; The accompanying 'build.ps1' script handles this conversion automatically.
SetupIconFile=..\Logos\app.ico

; Uninstaller configuration in Windows Add/Remove Programs
Uninstallable=yes
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

; Modern styling and high-ratio compression
WizardStyle=modern
Compression=lzma2/ultra64
SolidCompression=yes
CloseApplications=yes
CloseApplicationsFilter=TodoWidget.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
; Startup task: Checked by default (Flags: unchecked omitted)
Name: "autostart"; Description: "Launch To-Do on Windows startup"; GroupDescription: "Startup options:"

[Files]
; All compiled self-contained files from dotnet publish win-x64
Source: "..\src\TodoWidget\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Logo and brand assets
Source: "..\Logos\*"; DestDir: "{app}\Logos"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start Menu shortcuts (To-Do group)
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "{#MyAppDescription}"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
; Desktop shortcut (optional via task)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon; Comment: "{#MyAppDescription}"; IconFilename: "{app}\{#MyAppExeName}"

[Registry]
; Auto-start registration: Set app in Run key if user checked the startup option
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "TodoWidget"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "TodoWidget"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: autostart; Check: IsAdminInstallMode

; Uninstaller cleanup: Ensure Run registry values are cleaned up on uninstall
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "TodoWidget"; Flags: dontcreatekey uninsdeletevalue
Root: HKLM; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "TodoWidget"; Flags: dontcreatekey uninsdeletevalue; Check: IsAdminInstallMode

[Run]
; After installation, offer to launch the application
Filename: "{app}\{#MyAppExeName}"; WorkingDir: "{app}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent runascurrentuser

[Code]
// Check system requirements before installation proceeds
function InitializeSetup(): Boolean;
var
  Version: TWindowsVersion;
begin
  Result := True;
  GetWindowsVersionEx(Version);

  // Require Windows 10 build 19041 (20H1) or later for native WinRT notifications and .NET 8 desktop
  if (Version.Major < 10) or ((Version.Major = 10) and (Version.Build < 19041)) then
  begin
    MsgBox('This application requires Windows 10 (Version 2004, Build 19041) or Windows 11 64-bit.' + #13#10#13#10 +
           'Your current system: Windows ' + IntToStr(Version.Major) + '.' + IntToStr(Version.Minor) + ' (Build ' + IntToStr(Version.Build) + ').' + #13#10#13#10 +
           'Please update Windows to run To-Do.',
           mbCriticalError, MB_OK);
    Result := False;
    Exit;
  end;
end;

// Validate that installing into Program Files requires administrative privileges
function NextButtonClick(CurPageID: Integer): Boolean;
var
  SelectedDir: String;
  ProgramFilesDir: String;
begin
  Result := True;
  if CurPageID = wpSelectDir then
  begin
    SelectedDir := ExpandConstant('{app}');
    ProgramFilesDir := ExpandConstant('{commonpf}');

    // If not in administrative mode and user selected Program Files
    if not IsAdminInstallMode and (Pos(Uppercase(ProgramFilesDir), Uppercase(SelectedDir)) = 1) then
    begin
      MsgBox('Installing into "' + SelectedDir + '" requires administrator privileges.' + #13#10#13#10 +
             'Please select a user folder (such as ' + ExpandConstant('{localappdata}\Programs\TodoWidget') + ') ' +
             'or restart this installer choosing "Install for all users".',
             mbError, MB_OK);
      Result := False;
    end;
  end;
end;

// Uninstaller cleanup procedure: Remove registry Run keys on uninstall
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usUninstall then
  begin
    // Clean up startup Run key from Current User
    RegDeleteValue(HKEY_CURRENT_USER, 'Software\Microsoft\Windows\CurrentVersion\Run', 'TodoWidget');

    // Clean up startup Run key from Local Machine if running in admin mode
    if IsAdminInstallMode then
    begin
      RegDeleteValue(HKEY_LOCAL_MACHINE, 'Software\Microsoft\Windows\CurrentVersion\Run', 'TodoWidget');
    end;
  end;
end;
