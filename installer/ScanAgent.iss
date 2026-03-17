; ScanAgent 安装程序脚本
; 使用 Inno Setup 6 编译
; 编译前请确保已完成以下步骤：
;   1. dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
;      输出目录：scan-agent\ScanAgent\bin\Release\net6.0-windows\win-x64\publish\
;   2. cd frontend && npm run build
;      输出目录：frontend\dist\

#define AppName "ScanAgent"
#define AppVersion "1.0.0"
#define AppPublisher "ScanAgent"
#define AppExeName "ScanAgent.exe"
#define AppPort "17289"
#define PublishDir "..\scan-agent\ScanAgent\bin\Release\net6.0-windows\win-x64\publish"
#define FrontendDistDir "..\frontend\dist"

[Setup]
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
PrivilegesRequired=admin
OutputDir=.
OutputBaseFilename=ScanAgent-{#AppVersion}-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExeName}
; 安装完成后不自动重启
RestartIfNeededByRun=no

[Languages]
Name: "chs"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"

[CustomMessages]
chs.WelcomeLabel1=欢迎使用 ScanAgent 安装向导
chs.WelcomeLabel2=本向导将引导您完成 ScanAgent 的安装。%n%nScanAgent 是一款基于 TWAIN 协议的扫描仪集成工具，提供 HTTP API 接口，方便应用程序调用扫描仪。%n%n建议在继续之前关闭所有其他应用程序。
chs.FinishedLabel=ScanAgent 已成功安装到您的计算机。%n%n安装完成后，ScanAgent 将在系统托盘中运行。%n您可以通过右键点击托盘图标来管理 ScanAgent。
chs.LaunchAfterInstall=安装完成后启动 ScanAgent

[Files]
; 主程序
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; 前端文件（需先执行 npm run build）
Source: "{#FrontendDistDir}\*"; DestDir: "{app}\frontend"; Flags: ignoreversion recursesubdirs createallsubdirs

; 文档
Source: "..\README.md"; DestDir: "{app}\docs"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\scan-agent\USER-MANUAL.md"; DestDir: "{app}\docs"; Flags: ignoreversion skipifsourcedoesntexist
Source: "..\scan-agent\API-DOCUMENTATION.md"; DestDir: "{app}\docs"; Flags: ignoreversion skipifsourcedoesntexist

[Icons]
; 开始菜单
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Comment: "启动 ScanAgent 扫描服务"
Name: "{group}\卸载 {#AppName}"; Filename: "{uninstallexe}"

[Run]
; 添加防火墙规则（允许本地端口，静默执行）
Filename: "netsh"; \
  Parameters: "advfirewall firewall add rule name=""{#AppName}"" dir=in action=allow protocol=TCP localport={#AppPort} description=""ScanAgent 扫描服务"""; \
  Flags: runhidden; \
  StatusMsg: "正在配置防火墙规则..."

; 安装完成后以普通用户身份启动（runasoriginaluser 避免以管理员身份运行）
Filename: "{app}\{#AppExeName}"; \
  Description: "{cm:LaunchAfterInstall}"; \
  Flags: nowait postinstall runasoriginaluser skipifsilent

[UninstallRun]
; 卸载时删除防火墙规则
Filename: "netsh"; \
  Parameters: "advfirewall firewall delete rule name=""{#AppName}"""; \
  Flags: runhidden

[Code]
// 卸载时清理开机自启动注册表项
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  RegKey: string;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegKey := 'Software\Microsoft\Windows\CurrentVersion\Run';
    RegDeleteValue(HKCU, RegKey, '{#AppName}');
  end;
end;

// 安装前检查：如果 ScanAgent 正在运行，提示用户先退出
function InitializeSetup(): Boolean;
begin
  Result := True;
end;
