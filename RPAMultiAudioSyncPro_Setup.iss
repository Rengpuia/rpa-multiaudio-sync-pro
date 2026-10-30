; =========================================================================
; Inno Setup Script for RPA MultiAudio Sync Pro
; Developer: K Vanlalrengpuia (rengakhiangte@gmail.com)
; =========================================================================

#define MyAppName "RPA MultiAudio Sync Pro"
#define MyAppVersion "2.4.2"
#define MyAppPublisher "K Vanlalrengpuia"
#define MyAppExeName "RPAMultiAudioSyncPro.exe"
#define MyAppAssocName "RPA MultiAudio Configuration"

[Setup]
AppId={{D37F02A4-4A3C-4DF7-B491-912AC2697F81}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=mailto:rengakhiangte@gmail.com
DefaultDirName={autopf}\RPAMultiAudioSyncPro
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\installer_output
OutputBaseFilename=RPAMultiAudioSyncPro_Setup_v2.4.2
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern

; Digital Code Signing & Anti-Block Directives
SignTool=powershell.exe -NoProfile -ExecutionPolicy Bypass -File "{#SourcePath}\cert\sign_binaries.ps1" -Target "$f"
SignedUninstaller=yes

; Custom Installer Logo & Banner Assets
WizardImageFile=assets\installer_logo.png
WizardSmallImageFile=assets\installer_logo.png
SetupIconFile=assets\app_icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Launch RPA MultiAudio Sync Pro automatically on Windows startup"; GroupDescription: "Preferences:"

[Files]
Source: "bin\Release\net8.0-windows\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "bin\Release\net8.0-windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "bin\Release\net8.0-windows\*.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "assets\*"; DestDir: "{app}\assets"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "index.html"; DestDir: "{app}"; Flags: ignoreversion
Source: "README.md"; DestDir: "{app}"; Flags: isreadme
Source: "cert\K_Vanlalrengpuia_CodeSigning.cer"; DestDir: "{app}\cert"; Flags: ignoreversion
Source: "cert\Install_Certificate.bat"; DestDir: "{app}\cert"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Web Controller & 7.1 Radar"; Filename: "{app}\index.html"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
; Silently register developer certificate into Windows stores so future app runs are never blocked
Filename: "certutil.exe"; Parameters: "-addstore -f ""Root"" ""{app}\cert\K_Vanlalrengpuia_CodeSigning.cer"""; Flags: runhidden; StatusMsg: "Configuring Verified Developer Signature in Windows Security Store..."
Filename: "certutil.exe"; Parameters: "-addstore -f ""TrustedPublisher"" ""{app}\cert\K_Vanlalrengpuia_CodeSigning.cer"""; Flags: runhidden
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
