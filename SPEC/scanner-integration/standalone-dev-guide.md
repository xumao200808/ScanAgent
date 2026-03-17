# Scan Agent 独立开发指南

创建日期：2026-03-17
版本：v1.0
适用对象：Scan Agent 开发者（独立开发，无需接触主项目）

---

## 1. 你要做什么

你需要开发一个 **Windows 本地 HTTP 服务程序**（`ScanAgent.exe`），它的职责是：

- 驱动本机连接的扫描仪（通过 TWAIN / WIA 协议）
- 对外提供 5 个 HTTP API 接口
- 浏览器前端通过这些接口控制扫描仪完成扫描

最终交付一个**双击即可运行的单文件 exe**，不需要安装，不需要管理员权限。

### 架构位置

```
PC 浏览器 (React 前端)
    │
    │  HTTP 请求 (127.0.0.1:17289)
    ▼
┌──────────────────────────┐
│  ScanAgent.exe  ◄── 你负责这个
│  - HTTP API 服务
│  - TWAIN / WIA 驱动封装
│  - 临时图像文件管理
└──────────┬───────────────┘
           │ TWAIN / WIA
           ▼
       物理扫描仪
```

你只需要关注 `ScanAgent.exe` 这一层，不需要了解前端和后端的实现。

---

## 2. 开发环境搭建

### 2.1 硬件要求

- Windows 10 或 Windows 11 电脑
- 至少一台 TWAIN 兼容扫描仪（Canon / HP / Epson 等主流品牌均可）
- 扫描仪需安装官方驱动，在"设备和打印机"中可见

### 2.2 安装 Visual Studio

1. 下载 **Visual Studio 2022 Community**（免费）
   - 官网：https://visualstudio.microsoft.com/
2. 安装时勾选工作负载：**ASP.NET 和 Web 开发**
   - 这会自动安装 .NET 6+ SDK
3. 安装完成后验证：
   - 打开"开发者命令提示符"或 PowerShell
   - 运行 `dotnet --version`，应显示 6.0 或更高版本

### 2.3 创建项目

**方式一：Visual Studio 图形界面（推荐）**

1. 打开 Visual Studio → 创建新项目
2. 搜索模板：**ASP.NET Core 空**（ASP.NET Core Empty）
3. 项目配置：
   - 项目名称：`ScanAgent`
   - 位置：选择你的工作目录（如 `D:\Projects\`）
   - 解决方案名称：`ScanAgent`
4. 其他信息：
   - 框架：**.NET 6.0**（或更高）
   - 不勾选"配置 HTTPS"
   - 不勾选"启用 Docker"
5. 点击"创建"

**方式二：命令行**

```powershell
mkdir D:\Projects\ScanAgent
cd D:\Projects\ScanAgent
dotnet new web -n ScanAgent
cd ScanAgent
```

### 2.4 安装 NuGet 依赖

**Visual Studio 中**：
- 右键项目 → 管理 NuGet 程序包
- 搜索并安装 `NTwain`（版本 3.7.5 或更高）

**或命令行**：
```powershell
cd D:\Projects\ScanAgent\ScanAgent
dotnet add package NTwain --version 3.7.5
```

### 2.5 配置目标框架

**重要**：由于 TWAIN 协议是 Windows-only 的，需要将目标框架设置为 `net6.0-windows`。

**Visual Studio 中**：
- 右键项目 → 属性
- 在"应用程序"选项卡中，目标框架选择：**.NET 6.0 (Windows)**
- 或直接编辑 `.csproj` 文件，将 `<TargetFramework>net6.0</TargetFramework>` 改为 `<TargetFramework>net6.0-windows</TargetFramework>`

**命令行**：
```powershell
cd D:\Projects\ScanAgent\ScanAgent
# 编辑 ScanAgent.csproj 文件
# 将 <TargetFramework>net6.0</TargetFramework> 改为 <TargetFramework>net6.0-windows</TargetFramework>
```

**为什么需要这个修改**：
- NTwain 3.7.5 正式支持 `net6.0-windows7.0`
- 使用 `net6.0-windows` 可以消除编译警告
- 明确声明 Windows 平台依赖，防止误部署到非 Windows 环境

### 2.6 最终项目结构

```
D:\Projects\ScanAgent\
└── ScanAgent\
    ├── ScanAgent.csproj       # 项目文件
    ├── Program.cs             # 主入口 + HTTP 路由
    ├── Services\
    │   ├── IScannerService.cs     # 扫描服务接口
    │   ├── TwainScannerService.cs # TWAIN 实现
    │   ├── WiaScannerService.cs   # WIA 兜底实现（可选）
    │   └── ScannerFactory.cs      # 驱动选择工厂
    ├── Models\
    │   ├── ScanRequest.cs         # 扫描请求模型
    │   ├── ScanResult.cs          # 扫描结果模型
    │   └── ScannerInfo.cs         # 扫描仪信息模型
    └── Utils\
        ├── TempFileManager.cs     # 临时文件管理
        └── ImageConverter.cs      # 图像格式转换（可选）
```

在 Visual Studio 中右键项目 → 添加 → 新建文件夹，依次创建 `Services`、`Models`、`Utils` 三个文件夹。

---

## 3. API 接口契约（必须严格遵守）

所有接口监听地址：`http://127.0.0.1:17289`

这是你和前端之间的唯一约定，**接口格式不可擅自修改**。如需调整，必须先沟通确认。

### 3.1 GET /ping — 健康检查

前端用这个接口判断 Agent 是否在运行。

**请求**：无参数

**响应**（200 OK）：
```json
{
  "status": "ok",
  "version": "1.0.0"
}
```

---

### 3.2 GET /scanners — 枚举扫描仪

列出本机所有可用的扫描仪。

**请求**：无参数

**响应**（200 OK）：
```json
{
  "scanners": [
    {
      "id": "scanner_0",
      "name": "Canon LiDE 300",
      "default": true
    },
    {
      "id": "scanner_1",
      "name": "HP ScanJet Pro",
      "default": false
    }
  ]
}
```

**错误响应**（503 Service Unavailable）：
```json
{
  "error": "twain_not_available",
  "message": "TWAIN 驱动不可用"
}
```

---

### 3.3 POST /scan — 执行扫描

驱动扫描仪执行扫描，将结果保存为临时图像文件。

**请求**（Content-Type: application/json）：
```json
{
  "scanner_id": "scanner_0",   // 可选，不传则使用第一个扫描仪
  "dpi": 300,                  // 必填，范围 [150, 600]
  "color_mode": "gray",        // 必填，枚举: "color" | "gray" | "bw"
  "duplex": false,             // 可选，默认 false（双面扫描）
  "auto_feed": true,           // 可选，默认 true（自动进纸器）
  "paper_size": "A4"           // 可选，默认 "A4"
}
```

**响应**（200 OK）：
```json
{
  "scan_id": "scan_20260315_143022",
  "status": "completed",
  "images": [
    { "id": "img_001", "path": "/temp/scan_xxx/page_001.png" },
    { "id": "img_002", "path": "/temp/scan_xxx/page_002.png" }
  ]
}
```

**错误响应**：
| 状态码 | error 字段 | 含义 |
|--------|-----------|------|
| 404 | `scanner_not_found` | 指定的扫描仪未找到 |
| 409 | `scanner_busy` | 扫描仪正在被占用 |
| 500 | `scan_failed` | 扫描过程中出错 |

错误响应统一格式：
```json
{
  "error": "scanner_not_found",
  "message": "未找到可用的扫描仪"
}
```

---

### 3.4 GET /files/{image_id} — 获取扫描图像

前端通过此接口逐张获取扫描后的图像。

**请求**：URL 路径中的 `image_id` 对应 `/scan` 响应中 `images[].id`

**响应**（200 OK）：
- Content-Type: `image/png`
- Body: PNG 图像二进制数据

**错误响应**（404 Not Found）：图像不存在

---

### 3.5 DELETE /scans/{scan_id} — 清理临时文件

前端上传完成后调用此接口清理临时文件。

**请求**：URL 路径中的 `scan_id` 对应 `/scan` 响应中的 `scan_id`

**响应**（200 OK）：
```json
{
  "status": "ok"
}
```

**错误响应**（404 Not Found）：扫描任务不存在

---

### 3.6 CORS 配置（必须）

你的 HTTP 服务必须允许浏览器跨域访问，否则前端无法调用。

在 `Program.cs` 中配置：
```csharp
app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());
```

如果不配置 CORS，浏览器会拦截所有请求，这是浏览器安全策略，不是 bug。

---

## 4. 核心实现指南

### 4.1 Program.cs — HTTP 服务入口

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// 注册服务
builder.Services.AddSingleton<TempFileManager>();
builder.Services.AddSingleton<IScannerService, TwainScannerService>();
builder.Services.AddCors();

var app = builder.Build();

// CORS 配置（必须在路由之前）
app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());

// 健康检查
app.MapGet("/ping", () => Results.Ok(new { status = "ok", version = "1.0.0" }));

// 枚举扫描仪
app.MapGet("/scanners", (IScannerService scanner) =>
{
    try
    {
        var scanners = scanner.GetAvailableScanners();
        return Results.Ok(new { scanners });
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { error = "twain_not_available", message = ex.Message },
            statusCode: 503);
    }
});

// 执行扫描
app.MapPost("/scan", async (ScanRequest request, IScannerService scanner) =>
{
    try
    {
        var result = await scanner.ScanAsync(request);
        return Results.Ok(result);
    }
    catch (ScannerNotFoundException)
    {
        return Results.Json(
            new { error = "scanner_not_found", message = "未找到可用的扫描仪" },
            statusCode: 404);
    }
    catch (ScannerBusyException)
    {
        return Results.Json(
            new { error = "scanner_busy", message = "扫描仪正在被占用" },
            statusCode: 409);
    }
    catch (Exception ex)
    {
        return Results.Json(
            new { error = "scan_failed", message = ex.Message },
            statusCode: 500);
    }
});

// 获取图像
app.MapGet("/files/{imageId}", (string imageId, TempFileManager fileManager) =>
{
    var filePath = fileManager.GetFilePath(imageId);
    if (filePath == null || !File.Exists(filePath))
        return Results.NotFound();
    return Results.File(filePath, "image/png");
});

// 清理临时文件
app.MapDelete("/scans/{scanId}", (string scanId, TempFileManager fileManager) =>
{
    var deleted = fileManager.CleanupScan(scanId);
    if (!deleted)
        return Results.NotFound();
    return Results.Ok(new { status = "ok" });
});

app.Run("http://127.0.0.1:17289");
```

### 4.2 Models — 数据模型

**ScanRequest.cs**：
```csharp
public class ScanRequest
{
    public string? ScannerId { get; set; }    // scanner_id，可选
    public int Dpi { get; set; } = 300;
    public string ColorMode { get; set; } = "gray";  // color_mode
    public bool Duplex { get; set; } = false;
    public bool AutoFeed { get; set; } = true;        // auto_feed
    public string PaperSize { get; set; } = "A4";     // paper_size
}
```

**ScanResult.cs**：
```csharp
public class ScanResult
{
    public string ScanId { get; set; }     // scan_id
    public string Status { get; set; }
    public List<ImageInfo> Images { get; set; } = new();
}

public class ImageInfo
{
    public string Id { get; set; }
    public string Path { get; set; }
}
```

**ScannerInfo.cs**：
```csharp
public class ScannerInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool Default { get; set; }
}
```

> **注意 JSON 序列化**：API 契约中字段名使用 snake_case（如 `scan_id`、`color_mode`），
> 但 C# 属性通常用 PascalCase。你需要配置 JSON 序列化使用 snake_case，
> 或者在属性上加 `[JsonPropertyName("scan_id")]` 注解。
>
> 推荐在 `Program.cs` 中全局配置：
> ```csharp
> builder.Services.ConfigureHttpJsonOptions(options =>
> {
>     options.SerializerOptions.PropertyNamingPolicy =
>         JsonNamingPolicy.SnakeCaseLower;
> });
> ```

### 4.3 IScannerService.cs — 扫描服务接口

```csharp
public interface IScannerService
{
    List<ScannerInfo> GetAvailableScanners();
    Task<ScanResult> ScanAsync(ScanRequest request);
}
```

### 4.4 TwainScannerService.cs — TWAIN 核心实现

这是整个项目最核心的部分。以下是参考实现框架：

```csharp
using NTwain;
using NTwain.Data;
using System.Reflection;

public class TwainScannerService : IScannerService
{
    private TwainSession _session;
    private readonly TempFileManager _fileManager;

    public TwainScannerService(TempFileManager fileManager)
    {
        _fileManager = fileManager;
        InitializeSession();
    }

    private void InitializeSession()
    {
        var appId = TWIdentity.CreateFromAssembly(
            DataGroups.Image,
            Assembly.GetExecutingAssembly()
        );
        _session = new TwainSession(appId);
        _session.Open();
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        var scanners = new List<ScannerInfo>();
        int index = 0;
        foreach (var source in _session)
        {
            scanners.Add(new ScannerInfo
            {
                Id = $"scanner_{index}",
                Name = source.Name,
                Default = index == 0
            });
            index++;
        }
        return scanners;
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        // 1. 查找扫描仪
        var source = FindScanner(request.ScannerId);
        if (source == null)
            throw new ScannerNotFoundException();

        source.Open();

        try
        {
            // 2. 设置扫描参数
            SetScanParameters(source, request);

            // 3. 执行扫描
            var images = new List<ImageInfo>();
            var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}";

            source.DataTransferred += (sender, e) =>
            {
                // 保存扫描图像到临时目录
                using var stream = e.GetNativeImageStream();
                var imageId = _fileManager.SaveImage(scanId, stream);
                images.Add(new ImageInfo
                {
                    Id = imageId,
                    Path = _fileManager.GetFilePath(imageId)
                });
            };

            source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);

            return new ScanResult
            {
                ScanId = scanId,
                Status = "completed",
                Images = images
            };
        }
        finally
        {
            source.Close();
        }
    }

    private void SetScanParameters(DataSource source, ScanRequest request)
    {
        // DPI
        source.Capabilities.ICapXResolution.SetValue((float)request.Dpi);
        source.Capabilities.ICapYResolution.SetValue((float)request.Dpi);

        // 颜色模式
        var pixelType = request.ColorMode switch
        {
            "color" => PixelType.RGB,
            "gray"  => PixelType.Gray,
            "bw"    => PixelType.BlackWhite,
            _       => PixelType.Gray
        };
        source.Capabilities.ICapPixelType.SetValue(pixelType);

        // 双面扫描
        if (request.Duplex)
            source.Capabilities.CapDuplexEnabled.SetValue(BoolType.True);

        // 自动进纸
        if (request.AutoFeed)
        {
            source.Capabilities.CapFeederEnabled.SetValue(BoolType.True);
            source.Capabilities.CapAutoFeed.SetValue(BoolType.True);
        }
    }

    private DataSource? FindScanner(string? scannerId)
    {
        if (string.IsNullOrEmpty(scannerId))
            return _session.FirstOrDefault();

        int index = 0;
        foreach (var source in _session)
        {
            if ($"scanner_{index}" == scannerId)
                return source;
            index++;
        }
        return null;
    }
}
```

### 4.5 TempFileManager.cs — 临时文件管理

```csharp
public class TempFileManager
{
    private readonly string _baseDir;
    // imageId -> filePath 的映射
    private readonly Dictionary<string, string> _fileMap = new();

    public TempFileManager()
    {
        _baseDir = Path.Combine(Path.GetTempPath(), "ScanAgent");
        Directory.CreateDirectory(_baseDir);
    }

    public string SaveImage(string scanId, Stream imageStream)
    {
        var scanDir = Path.Combine(_baseDir, scanId);
        Directory.CreateDirectory(scanDir);

        var imageIndex = Directory.GetFiles(scanDir, "*.png").Length + 1;
        var imageId = $"img_{imageIndex:D3}";
        var filePath = Path.Combine(scanDir, $"page_{imageIndex:D3}.png");

        using var fileStream = File.Create(filePath);
        imageStream.CopyTo(fileStream);

        _fileMap[imageId] = filePath;
        return imageId;
    }

    public string? GetFilePath(string imageId)
    {
        return _fileMap.TryGetValue(imageId, out var path) ? path : null;
    }

    public bool CleanupScan(string scanId)
    {
        var scanDir = Path.Combine(_baseDir, scanId);
        if (!Directory.Exists(scanDir))
            return false;

        // 清理文件映射
        var files = Directory.GetFiles(scanDir);
        foreach (var file in files)
        {
            var id = Path.GetFileNameWithoutExtension(file)
                .Replace("page_", "img_");
            _fileMap.Remove(id);
        }

        Directory.Delete(scanDir, true);
        return true;
    }
}
```

### 4.6 自定义异常类

```csharp
public class ScannerNotFoundException : Exception
{
    public ScannerNotFoundException() : base("未找到可用的扫描仪") { }
}

public class ScannerBusyException : Exception
{
    public ScannerBusyException() : base("扫描仪正在被占用") { }
}
```

---

## 5. TWAIN 参数对照表

| 业务参数 | TWAIN 能力 | 取值范围 | 默认值 |
|---------|-----------|---------|--------|
| `dpi` | ICapXResolution / ICapYResolution | 150, 300, 600 | 300 |
| `color_mode` | ICapPixelType | RGB / Gray / BlackWhite | Gray |
| `duplex` | CapDuplexEnabled | true / false | false |
| `auto_feed` | CapFeederEnabled + CapAutoFeed | true / false | true |
| `paper_size` | ICapSupportedSizes | A4 / Letter / Auto | A4 |

**注意**：不是所有扫描仪都支持所有参数。如果某个参数设置失败，应该记录日志但不要中断扫描，使用扫描仪的默认值即可。

---

## 6. 运行与调试

### 6.1 在 Visual Studio 中运行

1. 打开解决方案 `ScanAgent.sln`
2. 按 **F5**（调试运行）或 **Ctrl+F5**（不调试运行）
3. 控制台应显示：
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: http://127.0.0.1:17289
   ```

> **注意**：Visual Studio 默认可能会用随机端口启动。
> 确保 `Program.cs` 最后一行是 `app.Run("http://127.0.0.1:17289");`，
> 或者在 `Properties/launchSettings.json` 中配置固定端口。

### 6.2 用 curl 或浏览器测试

打开 PowerShell 或命令提示符：

```powershell
# 1. 健康检查
curl http://127.0.0.1:17289/ping
# 预期: {"status":"ok","version":"1.0.0"}

# 2. 枚举扫描仪
curl http://127.0.0.1:17289/scanners
# 预期: {"scanners":[{"id":"scanner_0","name":"你的扫描仪名称","default":true}]}

# 3. 执行扫描
curl -X POST http://127.0.0.1:17289/scan -H "Content-Type: application/json" -d "{\"dpi\":300,\"color_mode\":\"gray\"}"
# 预期: {"scan_id":"scan_xxx","status":"completed","images":[...]}

# 4. 获取图像（用上一步返回的 image id）
curl http://127.0.0.1:17289/files/img_001 --output test.png
# 预期: 下载到 test.png 文件，可以用图片查看器打开

# 5. 清理
curl -X DELETE http://127.0.0.1:17289/scans/scan_xxx
# 预期: {"status":"ok"}
```

也可以使用 **Postman** 进行测试，更直观。

### 6.3 常见问题

**Q: 启动时报"端口被占用"**
```powershell
# 查找占用端口的进程
netstat -ano | findstr :17289
# 杀死进程（替换 PID）
taskkill /PID <PID> /F
```

**Q: 枚举扫描仪返回空列表**
- 检查扫描仪是否已连接并开机
- 检查驱动是否已安装（在"设备和打印机"中确认）
- 部分扫描仪需要安装 TWAIN 驱动（不是 WSD 驱动）

**Q: 扫描时报 TWAIN 错误**
- NTwain 需要在 STA 线程中运行，确保线程模型正确
- 参考 NTwain 官方文档：https://github.com/soukoku/ntwain

**Q: JSON 字段名不对（大小写问题）**
- API 契约要求 snake_case（如 `scan_id`），C# 默认输出 camelCase（如 `scanId`）
- 参考 §4.2 中的 JSON 序列化配置

---

## 7. 打包发布

### 7.1 在 Visual Studio 中发布

1. 右键项目 → 发布
2. 目标：文件夹
3. 配置：
   - 部署模式：**独立**
   - 目标运行时：**win-x64**
   - 勾选：**生成单个文件**
4. 点击"发布"

### 7.2 命令行发布

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

输出位置：`bin\Release\net6.0\win-x64\publish\ScanAgent.exe`

### 7.3 验证发布结果

- `ScanAgent.exe` 文件大小应 < 50MB
- 复制到一台干净的 Windows 电脑上
- 双击运行，不需要安装 .NET 运行时
- 用 curl 测试所有接口

---

## 8. 交付清单

开发完成后，请提交以下内容：

### 8.1 必须交付

| 交付物 | 说明 |
|--------|------|
| 源代码 | 完整的 Visual Studio 解决方案（.sln + 所有 .cs 文件） |
| 可执行文件 | `ScanAgent.exe`（单文件，< 50MB） |
| 测试报告 | 见下方模板 |

### 8.2 测试报告模板

请按以下格式提交测试报告：

```
# ScanAgent 测试报告

## 测试环境
- 操作系统：Windows __ (版本号)
- .NET 版本：__
- 测试日期：____-__-__

## 扫描仪兼容性测试

| 扫描仪品牌/型号 | TWAIN 驱动版本 | /scanners | /scan (灰度) | /scan (彩色) | /scan (黑白) | 双面扫描 | 自动进纸 |
|----------------|---------------|-----------|-------------|-------------|-------------|---------|---------|
| Canon ___      | ___           | ✅/❌     | ✅/❌       | ✅/❌       | ✅/❌       | ✅/❌/NA | ✅/❌/NA |
| HP ___         | ___           | ✅/❌     | ✅/❌       | ✅/❌       | ✅/❌       | ✅/❌/NA | ✅/❌/NA |

## API 接口测试

| 接口 | 正常场景 | 异常场景 | 备注 |
|------|---------|---------|------|
| GET /ping | ✅/❌ | - | |
| GET /scanners | ✅/❌ | 无扫描仪时 ✅/❌ | |
| POST /scan | ✅/❌ | 扫描仪不存在 ✅/❌ | |
| GET /files/{id} | ✅/❌ | 文件不存在 ✅/❌ | |
| DELETE /scans/{id} | ✅/❌ | 任务不存在 ✅/❌ | |

## 性能测试

| 场景 | 耗时 | 内存峰值 | 结果 |
|------|------|---------|------|
| 单页 300DPI 灰度 | __ms | __MB | ✅/❌ |
| 单页 300DPI 彩色 | __ms | __MB | ✅/❌ |
| 连续 50 页 300DPI 灰度 | __s | __MB | ✅/❌ |

## 已知问题
- （如有，列出）

## 接口变更说明
- （如果对 API 契约有任何调整，必须在此说明）
```

### 8.3 验收标准

**功能要求**：
- 所有 5 个 API 接口正常工作
- 至少支持 2 种品牌扫描仪
- 错误场景返回正确的状态码和错误信息
- CORS 配置正确（前端可跨域调用）

**性能要求**：
- 单页扫描（300 DPI 灰度）< 5 秒
- 连续扫描 50 页不崩溃
- 运行时内存占用 < 500MB

**交付要求**：
- exe 文件双击可运行，无需安装任何依赖
- exe 文件大小 < 50MB

---

## 9. WIA 兜底实现（可选加分项）

如果某些扫描仪不支持 TWAIN，可以实现 WIA 作为兜底方案。

WIA（Windows Image Acquisition）是 Windows 内置的图像采集接口，兼容性更广但功能较少。

实现思路：
1. 创建 `WiaScannerService.cs` 实现 `IScannerService` 接口
2. 创建 `ScannerFactory.cs`，优先尝试 TWAIN，失败时降级到 WIA
3. 对外接口保持不变

这不是必须的，但如果你有余力，这会大大提升兼容性。

---

## 10. 参考资料

- NTwain 库文档：https://github.com/soukoku/ntwain
- TWAIN 规范：https://www.twain.org/
- WIA 文档：https://docs.microsoft.com/en-us/windows/win32/wia/
- ASP.NET Core Minimal API：https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis

---

## 11. 沟通约定

- 如果需要修改 API 接口格式，请先沟通确认，不要擅自调整
- 遇到技术问题随时沟通，提供：错误日志 + 复现步骤 + 扫描仪型号
- 建议每周同步一次进度

---

**文档结束**
