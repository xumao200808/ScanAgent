# ScanAgent 安装指南

**版本**: 1.0.0
**更新日期**: 2026-03-17

---

## 📋 目录

1. [安装方式](#安装方式)
2. [方法一：使用安装程序](#方法一使用安装程序)
3. [方法二：使用预编译版本](#方法二使用预编译版本)
4. [方法三：从源代码编译](#方法三从源代码编译)
5. [编译安装程序](#编译安装程序)
6. [卸载指南](#卸载指南)
7. [常见问题](#常见问题)

---

## 安装方式

ScanAgent 提供三种安装方式：

| 方式 | 适用场景 | 优点 | 缺点 |
|------|----------|------|------|
| 安装程序 | 生产环境、正式部署 | 一键安装、自动配置防火墙、开始菜单快捷方式 | 需要管理员权限 |
| 预编译版本 | 便携使用、测试环境 | 无需安装、可移动、无需管理员权限 | 需要手动配置 |
| 源代码编译 | 开发环境、自定义修改 | 可自定义代码、最新功能 | 需要 .NET SDK、需要编译 |

---

## 方法一：使用安装程序

### 前置要求

- Windows 10 (64位) 或 Windows 11 (64位)
- 管理员权限（用于安装）
- .NET 6.0 Runtime（安装程序会提示）

### 安装步骤

#### 步骤 1：下载安装程序

1. 访问 [ScanAgent GitHub Releases](https://github.com/flashday/ScanAgent/releases)
2. 下载最新版本的 `ScanAgent-1.0.0-Setup.exe`

#### 步骤 2：运行安装程序

1. 右键点击 `ScanAgent-1.0.0-Setup.exe`
2. 选择"以管理员身份运行"
3. 在 Windows SmartScreen 提示时点击"更多信息" → "仍要运行"

#### 步骤 3：安装向导

1. **欢迎界面**
   - 点击"下一步"

2. **选择安装位置**
   - 默认：`C:\Program Files\ScanAgent`
   - 点击"浏览"可更改位置
   - 点击"下一步"

3. **准备安装**
   - 确认安装信息
   - 点击"安装"

4. **安装进度**
   - 安装程序会自动：
     - 复制文件到安装目录
     - 配置防火墙规则（允许端口 17289）
     - 创建开始菜单快捷方式
     - 注册卸载程序

5. **安装完成**
   - 点击"完成"
   - ScanAgent 会自动启动（以普通用户身份）

### 安装后验证

安装完成后，您应该看到：

1. **系统托盘图标**
   - 蓝色扫描仪图标
   - 气泡提示："ScanAgent 已启动，API: http://127.0.0.1:17289"

2. **开始菜单**
   - 开始菜单 → ScanAgent
   - 包含"启动 ScanAgent 扫描服务"和"卸载 ScanAgent"

3. **防火墙规则**
   - 打开"高级安全 Windows Defender 防火墙"
   - 入站规则 → ScanAgent
   - 规则已启用，允许 TCP 端口 17289

### 使用方法

#### 打开扫描界面

- **方法一**：双击系统托盘中的 ScanAgent 图标
- **方法二**：右键点击托盘图标 → "打开扫描界面"

#### 查看状态

右键点击托盘图标 → "查看状态"

#### 设置开机自启动

右键点击托盘图标 → "开机自启动"（勾选）

#### 退出程序

右键点击托盘图标 → "退出"

---

## 方法二：使用预编译版本

### 前置要求

- Windows 10 (64位) 或 Windows 11 (64位)
- .NET 6.0 Runtime

### 安装步骤

#### 步骤 1：下载预编译版本

1. 访问 [ScanAgent GitHub Releases](https://github.com/flashday/ScanAgent/releases)
2. 下载最新版本的 `ScanAgent-v1.0.0-win-x64.zip`

#### 步骤 2：解压文件

1. 右键点击 `ScanAgent-v1.0.0-win-x64.zip`
2. 选择"解压到..."
3. 选择目标目录（例如 `C:\ScanAgent`）

#### 步骤 3：验证 .NET Runtime

打开命令提示符或 PowerShell，运行：

```bash
dotnet --list-runtimes
```

如果输出中包含 `Microsoft.NETCore.App 6.0.x`，则已安装 .NET 6.0 Runtime。

如果未安装，请访问 [.NET 6.0 下载页面](https://dotnet.microsoft.com/download/dotnet/6.0) 下载并安装 ".NET 6.0 Runtime"。

#### 步骤 4：启动程序

双击 `ScanAgent.exe` 启动程序。

成功启动后，ScanAgent 会在系统托盘中显示扫描仪图标，并弹出气泡提示："ScanAgent 已启动，API: http://127.0.0.1:17289"。

### 使用方法

与安装版相同，参见"方法一：使用安装程序"中的"使用方法"部分。

### 手动配置防火墙（可选）

如果需要远程访问 ScanAgent，需要手动配置防火墙：

1. 打开"高级安全 Windows Defender 防火墙"
2. 点击"入站规则" → "新建规则"
3. 选择"端口" → "TCP" → 特定本地端口：17289
4. 选择"允许连接"
5. 应用于：域、专用、公用
6. 名称：ScanAgent

---

## 方法三：从源代码编译

### 前置要求

- Windows 10 (64位) 或 Windows 11 (64位)
- .NET 6.0 SDK
- Git（可选，用于克隆仓库）

### 编译步骤

#### 步骤 1：克隆仓库

```bash
git clone https://github.com/flashday/ScanAgent.git
cd scan-agent
```

#### 步骤 2：安装 .NET SDK

访问 [.NET 6.0 SDK 下载页面](https://dotnet.microsoft.com/download/dotnet/6.0) 下载并安装 .NET 6.0 SDK。

#### 步骤 3：编译后端

```bash
cd scan-agent/ScanAgent
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

编译输出目录：`scan-agent/ScanAgent/bin/Release/net6.0-windows/win-x64/publish/`

#### 步骤 4：编译前端

```bash
cd frontend
npm install
npm run build
```

编译输出目录：`frontend/dist/`

#### 步骤 5：复制文件

将以下文件复制到目标目录（例如 `C:\ScanAgent`）：

```
scan-agent/ScanAgent/bin/Release/net6.0-windows/win-x64/publish/ScanAgent.exe → C:\ScanAgent\
frontend/dist/* → C:\ScanAgent\frontend\
```

#### 步骤 6：启动程序

双击 `ScanAgent.exe` 启动程序。

---

## 编译安装程序

### 前置要求

- Inno Setup 6 或更高版本
- 已完成后端和前端编译

### 编译步骤

#### 步骤 1：完成后端编译

```bash
cd scan-agent/ScanAgent
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

输出目录：`scan-agent/ScanAgent/bin/Release/net6.0-windows/win-x64/publish/`

#### 步骤 2：完成前端编译

```bash
cd frontend
npm install
npm run build
```

输出目录：`frontend/dist/`

#### 步骤 3：编译安装程序

1. 下载并安装 [Inno Setup](https://jrsoftware.org/isdl.php)
2. 打开 `installer/ScanAgent.iss`
3. 点击"编译"（或按 F9）
4. 等待编译完成

编译输出：`installer/ScanAgent-1.0.0-Setup.exe`

#### 步骤 4：测试安装程序

1. 右键点击 `ScanAgent-1.0.0-Setup.exe`
2. 选择"以管理员身份运行"
3. 按照安装向导完成安装
4. 验证安装是否成功

---

## 卸载指南

### 方法一：使用卸载程序

1. 打开"控制面板" → "程序和功能"
2. 找到"ScanAgent"
3. 右键点击 → "卸载"
4. 按照卸载向导完成卸载

卸载程序会自动：
- 删除安装目录
- 删除防火墙规则
- 删除开机自启动注册表项
- 删除开始菜单快捷方式

### 方法二：手动卸载（预编译版本）

1. 右键点击系统托盘中的 ScanAgent 图标
2. 选择"退出"
3. 删除安装目录（例如 `C:\ScanAgent`）
4. 手动删除防火墙规则（如果已配置）

### 方法三：手动卸载（源代码编译版）

1. 右键点击系统托盘中的 ScanAgent 图标
2. 选择"退出"
3. 删除编译输出目录
4. 手动删除防火墙规则（如果已配置）

---

## 常见问题

### Q1: 安装程序无法启动

**A**: 请检查以下几点：

1. 是否右键点击并选择"以管理员身份运行"
2. Windows SmartScreen 是否阻止了程序
3. 杀毒软件是否阻止了安装程序

### Q2: 安装后 ScanAgent 未启动

**A**: 请尝试以下方法：

1. 检查系统托盘是否有 ScanAgent 图标
2. 点击"显示隐藏图标"按钮
3. 手动启动：开始菜单 → ScanAgent → 启动 ScanAgent 扫描服务
4. 检查 Windows 事件查看器中的应用程序日志

### Q3: 防火墙规则未添加

**A**: 请尝试以下方法：

1. 手动添加防火墙规则（参见"方法二：使用预编译版本"中的"手动配置防火墙"）
2. 检查是否有其他安全软件阻止
3. 以管理员身份运行安装程序

### Q4: 开机自启动不工作

**A**: 请尝试以下方法：

1. 右键点击托盘图标 → 确认"开机自启动"已勾选
2. 检查注册表：`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
3. 检查杀毒软件是否阻止了自启动

### Q5: 卸载后文件残留

**A**: 请手动删除以下内容：

1. 安装目录：`C:\Program Files\ScanAgent`
2. 临时文件：`%TEMP%\ScanAgent`
3. 注册表项：`HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run\ScanAgent`

### Q6: 编译安装程序失败

**A**: 请检查以下几点：

1. 是否已完成后端和前端编译
2. 输出目录是否正确
3. Inno Setup 版本是否为 6 或更高
4. 检查 ScanAgent.iss 中的路径是否正确

---

## 技术支持

如果以上方法无法解决您的问题，请：

1. 查看 [用户手册](USER-MANUAL.md)
2. 查看 [API 文档](API-DOCUMENTATION.md)
3. 查看 [GitHub Issues](https://github.com/flashday/ScanAgent/issues)
4. 提交新的 Issue 并附上详细的错误信息
