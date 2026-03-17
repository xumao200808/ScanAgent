# Scan Agent

Scan Agent 是一个 Windows 本地 HTTP 服务程序，用于驱动扫描仪并通过 HTTP API 提供扫描服务。

## 功能特性

- 支持 TWAIN 协议驱动扫描仪
- 提供 5 个 HTTP API 接口
- 支持多种扫描参数配置（DPI、颜色模式、双面扫描等）
- 临时文件自动管理
- WIA 兜底方案
- 系统托盘应用，支持开机自启动
- Inno Setup 安装程序，一键安装

## 系统要求

- Windows 10 或 Windows 11
- .NET 6.0 Runtime 或更高版本
- 至少一台 TWAIN 兼容扫描仪
- 扫描仪官方驱动已安装

## 依赖项

| 包名 | 版本 | 用途 |
|------|------|------|
| NTwain | 3.7.5 | TWAIN 协议驱动支持 |
| Microsoft.NET.Test.Sdk | 17.1.0 | 单元测试框架 |
| xunit | 2.4.1 | 单元测试框架 |
| xunit.runner.visualstudio | 2.4.3 | Visual Studio 测试运行器 |
| coverlet.collector | 3.1.2 | 代码覆盖率收集器 |

## 安装 .NET Runtime

如果尚未安装 .NET Runtime，请访问 https://dotnet.microsoft.com/download/dotnet/6.0 下载并安装 ".NET 6.0 Runtime"。

## 快速开始

### 方法一：使用安装程序（推荐）

1. 下载 `ScanAgent-1.0.0-Setup.exe`
2. 右键点击，选择"以管理员身份运行"
3. 按照安装向导完成安装
4. 安装完成后，ScanAgent 会自动启动并在系统托盘中显示图标

### 方法二：使用预编译版本（便携版）

1. 下载 `ScanAgent-v1.0.0-win-x64.zip`
2. 解压到任意目录
3. 双击 `ScanAgent.exe` 启动

### 方法三：从源代码运行

#### 1. 安装依赖

```bash
cd scan-agent/ScanAgent
dotnet restore
```

#### 2. 运行服务

```bash
dotnet run --urls "http://127.0.0.1:17289"
```

看到系统托盘中出现扫描仪图标，并弹出气泡提示："ScanAgent 已启动，API: http://127.0.0.1:17289"。

#### 3. 测试接口

```bash
# 健康检查
curl http://127.0.0.1:17289/ping

# 枚举扫描仪
curl http://127.0.0.1:17289/scanners

# 执行扫描
curl -X POST http://127.0.0.1:17289/scan -H "Content-Type: application/json" -d "{\"dpi\":300,\"colorMode\":\"gray\"}"
```

---

## 编译和发布

### 编译版本说明

ScanAgent 提供多种编译方式，适用于不同的使用场景：

| 编译方式 | 命令 | 输出目录 | 可运行 | 需要Runtime | 文件大小 | 适用场景 |
|---------|------|----------|--------|-------------|----------|----------|
| Debug 构建 | `dotnet build` | `bin/Debug/net6.0-windows/` | ✅ | ✅ | ~5MB | 开发调试 |
| Release 构建 | `dotnet build -c Release` | `bin/Release/net6.0-windows/` | ✅ | ✅ | ~5MB | 测试 |
| Publish (依赖Runtime) | `dotnet publish -c Release -r win-x64` | `bin/Release/net6.0-windows/win-x64/publish/` | ✅ | ✅ | ~5MB | 生产环境（用户已安装Runtime） |
| Publish (自包含) | `dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` | `bin/Release/net6.0-windows/win-x64/publish/` | ✅ | ❌ | ~80MB | 生产环境（便携版） |

### 编译方式详解

#### 1. Debug 构建（开发调试）

```bash
cd scan-agent/ScanAgent
dotnet build
```

**特点**:
- ✅ 可以直接运行
- ⚠️ Debug 版本，性能较差
- ⚠️ 依赖 .NET 6.0 Runtime（需要用户安装）
- ⚠️ 包含调试符号（.pdb 文件）
- ⚠️ 不适合生产环境

**适用场景**: 开发和调试

#### 2. Release 构建（测试）

```bash
cd scan-agent/ScanAgent
dotnet build -c Release
```

**特点**:
- ✅ 可以直接运行
- ✅ Release 版本，性能优化
- ⚠️ 依赖 .NET 6.0 Runtime（需要用户安装）
- ⚠️ 不是单文件版本

**适用场景**: 功能测试

#### 3. Publish（依赖 Runtime 版本）

```bash
cd scan-agent/ScanAgent
dotnet publish -c Release -r win-x64
```

**特点**:
- ✅ Release 版本，性能优化
- ✅ 包含所有依赖
- ⚠️ 依赖 .NET 6.0 Runtime（需要用户安装）
- ✅ 文件较小（~5MB）

**适用场景**: 生产环境（用户已安装 .NET Runtime）

#### 4. Publish（自包含单文件版本，推荐）

```bash
cd scan-agent/ScanAgent
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**特点**:
- ✅ Release 版本，性能优化
- ✅ 自包含，无需安装 .NET Runtime
- ✅ 单文件，方便分发
- ✅ 包含所有依赖
- ⚠️ 文件较大（~80MB）

**适用场景**: 生产环境、便携版、安装程序

### 编译安装程序

如果要生成安装程序，需要先完成后端和前端编译：

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

**输出**: `installer/ScanAgent-1.0.0-Setup.exe`

---

## API 接口

### GET /ping

健康检查接口。

**响应**：
```json
{
  "status": "ok",
  "version": "1.0.0"
}
```

### GET /scanners

枚举所有可用的扫描仪。

**响应**：
```json
{
  "scanners": [
    {
      "id": "scanner_0",
      "name": "Canon LiDE 300",
      "default": true
    }
  ]
}
```

### POST /scan

执行扫描。

**请求**：
```json
{
  "scanner_id": "scanner_0",
  "dpi": 300,
  "color_mode": "gray",
  "duplex": false,
  "auto_feed": true,
  "paper_size": "A4"
}
```

**响应**：
```json
{
  "scan_id": "scan_20260317_143022",
  "status": "completed",
  "images": [
    {
      "id": "img_001",
      "path": "/temp/scan_xxx/page_001.png"
    }
  ]
}
```

### GET /files/{image_id}

获取扫描图像。

**响应**：PNG 图像二进制数据

### DELETE /scans/{scan_id}

清理临时文件。

**响应**：
```json
{
  "status": "ok"
}
```

## 项目结构

```
ScanAgent/
├── ScanAgent.csproj       # 项目文件
├── Program.cs             # 主入口 + HTTP 路由
├── Services/
│   ├── IScannerService.cs     # 扫描服务接口
│   ├── TwainScannerService.cs # TWAIN 实现
│   ├── WiaScannerService.cs   # WIA 兜底实现
│   └── ScannerFactory.cs      # 驱动选择工厂
├── Models/
│   ├── ScanRequest.cs         # 扫描请求模型
│   ├── ScanResult.cs          # 扫描结果模型
│   ├── ScannerInfo.cs         # 扫描仪信息模型
│   └── Exceptions.cs          # 自定义异常
└── Utils/
    └── TempFileManager.cs     # 临时文件管理
```

## 打包发布

### 单文件发布

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

输出位置：`bin/Release/net6.0-windows/win-x64/publish/ScanAgent.exe`

### 验证发布结果

- `ScanAgent.exe` 文件大小应 < 50MB
- 双击运行，不需要安装 .NET 运行时

## 常见问题

### Q: 启动时报"端口被占用"

```bash
# 查找占用端口的进程
netstat -ano | findstr :17289

# 杀死进程（替换 PID）
taskkill /PID <PID> /F
```

### Q: 枚举扫描仪返回空列表

- 检查扫描仪是否已连接并开机
- 检查驱动是否已安装（在"设备和打印机"中确认）
- 部分扫描仪需要安装 TWAIN 驱动（不是 WSD 驱动）

### Q: 扫描时报 TWAIN 错误

- NTwain 需要在 STA 线程中运行
- 参考 NTwain 官方文档：https://github.com/soukoku/ntwain

## 开发文档

详细的开发指南请参考：`../../SPEC/scanner-integration/standalone-dev-guide.md`

## 开发进度

### Phase 1: Scan Agent 开发 ✅
- **状态**：已完成（90%）
- **完成时间**：2026-03-17
- **已完成任务**：
  - ✅ T1.1 项目搭建
  - ✅ T1.2 健康检查接口
  - ✅ T1.3 TWAIN 初始化
  - ✅ T1.4 扫描仪枚举
  - ✅ T1.5 扫描参数映射
  - ✅ T1.6 扫描执行
  - ✅ T1.7 图像文件管理
  - ✅ T1.8 错误处理
  - ✅ T1.9 WIA 兜底实现
- **待完成任务**：
  - ⏳ T1.2 添加单元测试
  - ⏳ T1.5 实现纸张尺寸设置
  - ⏳ T1.10 Agent 打包与测试

### Phase 2: 前端集成
- **状态**：待开始

### Phase 3: 联调与优化
- **状态**：待开始

### Phase 4: 文档与部署
- **状态**：待开始

## 版本历史

详细的版本变更记录请查看 [CHANGELOG.md](./CHANGELOG.md)

## 许可证

本项目仅供内部使用。