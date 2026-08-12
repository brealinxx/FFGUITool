#define MyAppName "FFGUITool"
#ifndef MyAppVersion
#define MyAppVersion "1.9.0"
#endif
#define MyAppPublisher "brealin"
#define MyAppExeName "FFGUITool.exe"
#ifndef RuntimeId
#define RuntimeId "windows-x64"
#endif
#ifndef SourceDir
#define SourceDir "..\\FFGUITool\\bin\\publish\\FFGUITool-win-x64"
#endif
#ifndef OutputDir
#define OutputDir "..\\FFGUITool\\bin\\publish\\installer"
#endif
#ifndef IconFile
#define IconFile "..\\FFGUITool\\Resources\\icon.ico"
#endif

[Setup]
AppId={{7A0C0D18-EE7A-4964-97E5-2F8D4D92C518}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#OutputDir}
OutputBaseFilename=FFGUITool-v{#MyAppVersion}-{#RuntimeId}-Installer
Compression=lzma
SolidCompression=yes
WizardStyle=modern
SetupIconFile={#IconFile}
#if RuntimeId == "windows-x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
#if RuntimeId == "windows-arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#endif
UninstallDisplayIcon={app}\Resources\icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "chinesesimp"; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "{#IconFile}"; DestDir: "{app}\Resources"; DestName: "icon.ico"; Flags: ignoreversion
Source: "uninstall.cmd"; DestDir: "{app}"; DestName: "{cm:UninstallLauncherFile}.cmd"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Resources\icon.ico"
Name: "{group}\{cm:UninstallShortcut}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\Resources\icon.ico"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\FFGUITool"; ValueType: string; ValueName: "InstallPath"; ValueData: "{app}"; Flags: uninsdeletekey

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{userappdata}\FFGUITool"

[CustomMessages]
english.UninstallLauncherFile=uninstall
english.UninstallShortcut=Uninstall FFGUITool
english.AppLanguagePageTitle=Application language
english.AppLanguagePageSubtitle=Choose the language FFGUITool uses after startup
english.AppLanguagePageDescription=This only controls the app interface language. It does not change the setup wizard language.
english.AppLanguageChinese=Simplified Chinese
english.AppLanguageEnglish=English
chinesesimp.UninstallLauncherFile=卸载
chinesesimp.UninstallShortcut=卸载 FFGUITool
chinesesimp.AppLanguagePageTitle=软件语言
chinesesimp.AppLanguagePageSubtitle=选择 FFGUITool 打开后的界面语言
chinesesimp.AppLanguagePageDescription=这个选项只影响软件界面语言，不会改变安装向导语言。
chinesesimp.AppLanguageChinese=简体中文
chinesesimp.AppLanguageEnglish=English

[Code]
var
  AppLanguagePage: TInputOptionWizardPage;

procedure InitializeWizard();
begin
  AppLanguagePage := CreateInputOptionPage(
    wpSelectDir,
    ExpandConstant('{cm:AppLanguagePageTitle}'),
    ExpandConstant('{cm:AppLanguagePageSubtitle}'),
    ExpandConstant('{cm:AppLanguagePageDescription}'),
    True,
    False);

  AppLanguagePage.Add(ExpandConstant('{cm:AppLanguageChinese}'));
  AppLanguagePage.Add(ExpandConstant('{cm:AppLanguageEnglish}'));

  if ActiveLanguage = 'english' then
    AppLanguagePage.Values[1] := True
  else
    AppLanguagePage.Values[0] := True;
end;

function GetSelectedAppLanguage(): String;
begin
  if AppLanguagePage.Values[1] then
    Result := 'en-US'
  else
    Result := 'zh-CN';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigDir: String;
  ConfigPath: String;
  Json: String;
begin
  if CurStep = ssPostInstall then
  begin
    ConfigDir := ExpandConstant('{userappdata}\FFGUITool');
    ConfigPath := ConfigDir + '\config.json';
    Json := '{' + #13#10 +
      '  "Theme": "Default",' + #13#10 +
      '  "Language": "' + GetSelectedAppLanguage() + '"' + #13#10 +
      '}' + #13#10;

    if ForceDirectories(ConfigDir) then
      SaveStringToFile(ConfigPath, Json, False);
  end;
end;
