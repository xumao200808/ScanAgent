# ScanAgent

ScanAgent 是一个完整的扫描仪驱动解决方案，包含后端服务、前端界面、安装程序和完整的文档体系。

## 项目结构

```
ScanAgent/
├── scan-agent/              # 后端服务（C# .NET 6.0）
│   ├── ScanAgent/           # 主程序
│   ├── ScanAgent.Tests/     # 单元测试
│   ├── docs/                # 核心文档
│   │   ├── USER-MANUAL.md
│   │   ├── API-DOCUMENTATION.md
│   │   ├── INSTALL-GUIDE.md
│   │   ├── ARCHITECTURE-DESIGN.md
│   │   ├── EXTENSION-GUIDE.md
│   │   ├── DEBUG-GUIDE.md
│   │   ├── RELEASE-NOTES.md
│   │   ├── TRAE-IDE-DEBUG-GUIDE.md
│   │   └── VERSION-RELEASE-CHECKLIST.md
│   ├── reports/             # 报告文档
│   │   ├── phase/          # Phase 完成报告
│   │   └── code-review/    # 代码审查报告
│   ├── other/              # 其他报告
│   ├── README.md           # 后端项目文档
│   └── CHANGELOG.md        # 版本变更记录
├── frontend/               # 前端界面（HTML/CSS/JavaScript）
│   ├── src/
│   ├── public/
│   ├── package.json
│   └── README.md           # 前端项目文档
├── installer/              # 安装程序（Inno Setup）
│   └── ScanAgent.iss       # 安装脚本
├── SPEC/                   # 规格文档
│   └── scanner-integration/
│       ├── README.md
│       ├── design.md
│       ├── implementation.md
│       ├── tasks.md
│       ├── collaboration-guide.md
│       ├── debug-guide.md
│       └── standalone-dev-guide.md
├── build-frontend.bat      # 前端构建脚本
├── deploy.bat              # 部署脚本
├── package.bat             # 打包脚本
└── README.md              # 项目主文档（本文件）
```

## 功能特性

### 后端服务（scan-agent/）
- 支持 TWAIN 协议驱动扫描仪
- 提供 5 个 HTTP API 接口
- 支持多种扫描参数配置（DPI、颜色模式、双面扫描等）
- 临时文件自动管理
- WIA 兜底方案
- 系统托盘应用，支持开机自启动

### 前端界面（frontend/）
- 简洁的扫描界面
- 实时扫描状态显示
- 图像预览和管理
- 键盘快捷键支持

### 安装程序（installer/）
- Inno Setup 6 安装程序
- 自动配置防火墙规则
- 创建开始菜单快捷方式
- 卸载时自动清理

## 快速开始

### 安装

#### 方法一：使用安装程序（推荐）

1. 下载 `ScanAgent-1.0.0-Setup.exe`
2. 右键点击，选择"以管理员身份运行"
3. 按照安装向导完成安装
4. 安装完成后，ScanAgent 会自动启动并在系统托盘中显示图标

#### 方法二：使用预编译版本（便携版）

1. 下载 `ScanAgent-v1.0.0-win-x64.zip`
2. 解压到任意目录
3. 双击 `ScanAgent.exe` 启动

### 从源代码构建

#### 1. 构建后端

```bash
cd scan-agent/ScanAgent
dotnet restore
dotnet build
```

#### 2. 构建前端

```bash
cd frontend
npm install
npm run build
```

#### 3. 运行

```bash
cd scan-agent/ScanAgent
dotnet run --urls "http://127.0.0.1:17289"
```

### 编译安装程序

如果要生成安装程序：

```bash
# 1. 编译后端（自包含版本）
cd scan-agent/ScanAgent
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# 2. 编译前端
cd ../frontend
npm install
npm run build

# 3. 编译安装程序（需要 Inno Setup）
cd ../installer
# 使用 Inno Setup 编译 ScanAgent.iss
```

## 文档

### 核心文档

- [用户手册](scan-agent/docs/USER-MANUAL.md) - 详细的使用说明
- [API 文档](scan-agent/docs/API-DOCUMENTATION.md) - HTTP API 接口说明
- [安装指南](scan-agent/docs/INSTALL-GUIDE.md) - 安装和部署指南
- [架构设计](scan-agent/docs/ARCHITECTURE-DESIGN.md) - 系统架构说明
- [调试指南](scan-agent/docs/DEBUG-GUIDE.md) - 调试和故障排除
- [发布说明](scan-agent/docs/RELEASE-NOTES.md) - 版本发布说明

### 开发文档

- [扩展开发指南](scan-agent/docs/EXTENSION-GUIDE.md) - 如何扩展功能
- [TRAE IDE 调试指南](scan-agent/docs/TRAE-IDE-DEBUG-GUIDE.md) - TRAE IDE 使用指南
- [版本发布检查清单](scan-agent/docs/VERSION-RELEASE-CHECKLIST.md) - 发布前检查项

### 项目文档

- [后端项目文档](scan-agent/README.md) - 后端项目详细说明
- [前端项目文档](frontend/README.md) - 前端项目详细说明
- [规格文档](SPEC/scanner-integration/README.md) - 项目规格说明

### 报告文档

- [Phase 完成报告](scan-agent/reports/phase/) - 各阶段完成情况
- [代码审查报告](scan-agent/reports/code-review/) - 代码审查和修复记录
- [其他报告](scan-agent/other/) - 文档更新、平台优化等报告

## 系统要求

- Windows 10 或 Windows 11
- .NET 6.0 Runtime 或更高版本
- 至少一台 TWAIN 兼容扫描仪
- 扫描仪官方驱动已安装

## 依赖项

### 后端

| 包名 | 版本 | 用途 |
|------|------|------|
| NTwain | 3.7.5 | TWAIN 协议驱动支持 |
| Microsoft.NET.Test.Sdk | 17.1.0 | 单元测试框架 |
| xunit | 2.4.1 | 单元测试框架 |
| xunit.runner.visualstudio | 2.4.3 | Visual Studio 测试运行器 |
| coverlet.collector | 3.1.2 | 代码覆盖率收集器 |

### 前端

| 包名 | 版本 | 用途 |
|------|------|------|
| Vite | 5.x | 构建工具 |
| TypeScript | 5.x | 类型系统 |

## 开发环境

### 后端

- .NET 6.0 SDK
- Visual Studio 2022 或 VS Code（推荐）
- Windows 10/11

### 前端

- Node.js 18+ 和 npm
- 现代浏览器（Chrome、Edge、Firefox）

### 安装程序

- Inno Setup 6（用于编译安装程序）

## 测试

### 运行单元测试

```bash
cd scan-agent/ScanAgent.Tests
dotnet test
```

### 手动测试

1. 启动 ScanAgent
2. 打开浏览器访问 `http://127.0.0.1:17289`
3. 测试扫描功能

## 部署

### 使用部署脚本

```bash
# 部署到本地
deploy.bat

# 打包为便携版
package.bat
```

### 手动部署

1. 编译后端（自包含版本）
2. 编译前端
3. 将文件复制到目标目录
4. 配置防火墙规则（如果需要）

## 版本历史

详细的版本变更记录请查看 [CHANGELOG](CHANGELOG.md)

## 贡献

欢迎提交 Issue 和 Pull Request。

## 许可证

本项目仅供内部使用。

## 联系方式

如有问题，请提交 Issue 或联系开发团队。
