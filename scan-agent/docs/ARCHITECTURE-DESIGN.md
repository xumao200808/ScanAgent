# ScanAgent 架构设计文档

**版本**: 1.0.0
**更新日期**: 2026-03-17

---

## 📋 目录

1. [系统概述](#系统概述)
2. [架构设计](#架构设计)
3. [核心组件](#核心组件)
4. [数据流](#数据流)
5. [技术栈](#技术栈)
6. [设计模式](#设计模式)
7. [扩展性](#扩展性)

---

## 系统概述

ScanAgent 是一个基于 TWAIN 协议的扫描仪采集系统，采用客户端-服务器架构，提供 HTTP API 和 Web 界面。

### 系统目标

- 提供统一的扫描仪访问接口
- 支持多种扫描仪型号
- 简化扫描操作流程
- 提供友好的用户界面

### 系统边界

```
┌─────────────────────────────────────────────────────────────┐
│                     用户层                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐│
│  │  Web 界面  │  │  第三方应用  │  │  开发者工具  ││
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘│
└─────────┼──────────────────┼──────────────────┼──────────┘
          │                  │                  │
          └──────────────────┼──────────────────┘
                             │
┌────────────────────────────────┼─────────────────────────────────┐
│                    API 层   │                           │
│  ┌────────────────────────┼──────────────────────────┐    │
│  │   HTTP API 端点     │                          │    │
│  │  (ASP.NET Core)      │                          │    │
│  └────────────────────────┼──────────────────────────┘    │
└───────────────────────────┼──────────────────────────────┘
                            │
┌───────────────────────────┼──────────────────────────────────┐
│                  服务层   │                          │
│  ┌──────────────────────┼──────────────────────────┐    │
│  │  ScannerFactory     │                          │    │
│  │  IScannerService    │                          │    │
│  │  TempFileManager    │                          │    │
│  └──────────────────────┼──────────────────────────┘    │
└───────────────────────────┼──────────────────────────────┘
                            │
┌───────────────────────────┼──────────────────────────────────┐
│                  驱动层   │                          │
│  ┌──────────────────────┼──────────────────────────┐    │
│  │  TwainScannerService│                          │    │
│  │  NTwain 库         │                          │    │
│  │  TWAIN 驱动        │                          │    │
│  └──────────────────────┼──────────────────────────┘    │
└───────────────────────────┼──────────────────────────────┘
                            │
┌───────────────────────────┼──────────────────────────────────┐
│                  硬件层   │                          │
│  ┌──────────────────────┼──────────────────────────┐    │
│  │  扫描仪硬件        │                          │    │
│  └──────────────────────┼──────────────────────────┘    │
└───────────────────────────┴──────────────────────────────┘
```

---

## 架构设计

### 分层架构

ScanAgent 采用经典的分层架构：

1. **表现层 (Presentation Layer)**
   - HTTP API 端点
   - Web 界面

2. **业务逻辑层 (Business Logic Layer)**
   - 扫描服务
   - 扫描仪工厂
   - 文件管理

3. **数据访问层 (Data Access Layer)**
   - 文件系统操作
   - 临时文件管理

4. **驱动层 (Driver Layer)**
   - TWAIN 驱动集成
   - 硬件访问

### 依赖注入

ScanAgent 使用 ASP.NET Core 的依赖注入容器：

```csharp
// 单例服务
builder.Services.AddSingleton<TempFileManager>();
builder.Services.AddSingleton<ScannerFactory>();

// 托管服务
builder.Services.AddHostedService<CleanupBackgroundService>();
```

**设计原则**:
- 单例服务：无状态的服务，整个应用生命周期只创建一次
- 托管服务：需要生命周期管理的后台服务

---

## 核心组件

### 1. Program.cs

应用入口点，负责：

- 配置依赖注入
- 配置中间件（CORS）
- 定义 HTTP 路由
- 启动应用

**关键代码**:
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TempFileManager>();
builder.Services.AddSingleton<ScannerFactory>();
builder.Services.AddHostedService<CleanupBackgroundService>();

var app = builder.Build();

app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());

app.MapGet("/ping", () => Results.Ok(new { status = "ok", version = "1.0.0" }));
app.MapGet("/scanners", (ScannerFactory factory) => { /* ... */ });
app.MapPost("/scan", async (ScanRequest request, ScannerFactory factory) => { /* ... */ });
app.MapGet("/files/{imageId}", (string imageId, TempFileManager fileManager) => { /* ... */ });
app.MapDelete("/scans/{scanId}", (string scanId, TempFileManager fileManager) => { /* ... */ });

app.Run("http://127.0.0.1:17289");
```

### 2. ScannerFactory

扫描仪工厂，负责：

- 选择合适的扫描仪服务（TWAIN 或 WIA）
- 缓存扫描仪列表
- 提供统一的扫描仪访问接口

**设计模式**: 工厂模式 + 策略模式

**关键代码**:
```csharp
public class ScannerFactory
{
    private IScannerService? _primaryService;
    private List<ScannerInfo>? _cachedScanners;
    private DateTime _cacheTime = DateTime.MinValue;
    private readonly object _lock = new();

    public IScannerService GetScannerService()
    {
        lock (_lock)
        {
            if (_primaryService == null)
            {
                _primaryService = new TwainScannerService(fileManager);
            }

            if (DateTime.Now - _cacheTime > TimeSpan.FromSeconds(5))
            {
                _cachedScanners = _primaryService.GetAvailableScanners();
                _cacheTime = DateTime.Now;
            }

            return _primaryService;
        }
    }
}
```

### 3. IScannerService

扫描仪服务接口，定义了扫描仪操作的统一接口：

```csharp
public interface IScannerService
{
    List<ScannerInfo> GetAvailableScanners();
    Task<ScanResult> ScanAsync(ScanRequest request);
}
```

**实现类**:
- `TwainScannerService`: TWAIN 协议实现
- `WiaScannerService`: WIA 协议实现（可选）

### 4. TwainScannerService

TWAIN 扫描仪服务，负责：

- 初始化 TWAIN 会话
- 枚举扫描仪
- 配置扫描参数
- 执行扫描操作
- 处理扫描事件

**关键特性**:
- 超时保护（2 分钟）
- 事件处理器管理
- 错误处理

**关键代码**:
```csharp
public class TwainScannerService : IScannerService
{
    private TwainSession? _session;
    private readonly TempFileManager _fileManager;
    private bool _initialized = false;

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        var source = FindScanner(request.ScannerId);
        source.Open();

        try
        {
            SetScanParameters(source, request);

            var images = new List<ImageInfo>();
            var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
            var scanCompleted = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            EventHandler<DataTransferredEventArgs> dataTransferredHandler = (sender, e) =>
            {
                using var stream = e.GetNativeImageStream();
                if (stream != null)
                {
                    var imageId = _fileManager.SaveImage(scanId, stream);
                    images.Add(new ImageInfo { Id = imageId });
                }
            };

            _session.DataTransferred += dataTransferredHandler;

            try
            {
                source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);

                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2), timeoutCts.Token);
                var completedTask = await Task.WhenAny(scanCompleted.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Scan operation timed out");
                }

                return new ScanResult
                {
                    ScanId = scanId,
                    Status = "completed",
                    Images = images
                };
            }
            finally
            {
                _session.DataTransferred -= dataTransferredHandler;
            }
        }
        finally
        {
            source.Close();
        }
    }
}
```

### 5. TempFileManager

临时文件管理器，负责：

- 管理扫描图像的临时存储
- 生成唯一的文件名
- 提供文件访问接口
- 清理过期文件

**设计特性**:
- 线程安全（使用 lock）
- 文件 I/O 缓冲区优化（80KB）
- 自动清理（24 小时）

**关键代码**:
```csharp
public class TempFileManager
{
    private readonly string _baseDir;
    private readonly Dictionary<string, string> _fileMap = new();
    private readonly object _lock = new();
    private const int BufferSize = 81920;

    public string SaveImage(string scanId, Stream imageStream)
    {
        lock (_lock)
        {
            var scanDir = Path.Combine(_baseDir, scanId);
            Directory.CreateDirectory(scanDir);

            var imageIndex = Directory.GetFiles(scanDir, "*.png").Length + 1;
            var imageId = $"img_{imageIndex:D3}";
            var filePath = Path.Combine(scanDir, $"page_{imageIndex:D3}.png");

            using var fileStream = File.Create(filePath);
            var buffer = new byte[BufferSize];
            int bytesRead;

            while ((bytesRead = imageStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
            }

            _fileMap[imageId] = filePath;
            return imageId;
        }
    }

    public void CleanupOldScans(TimeSpan maxAge)
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            var scanDirs = Directory.GetDirectories(_baseDir);

            foreach (var scanDir in scanDirs)
            {
                var dirInfo = new DirectoryInfo(scanDir);
                if (now - dirInfo.CreationTime > maxAge)
                {
                    try
                    {
                        Directory.Delete(scanDir, true);
                        Console.WriteLine($"[TempFileManager] Cleaned up old scan: {scanDir}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TempFileManager] Error cleaning up {scanDir}: {ex.Message}");
                    }
                }
            }
        }
    }
}
```

### 6. CleanupBackgroundService

后台清理服务，负责：

- 定期清理过期的扫描文件
- 优雅停止

**设计模式**: 托管服务模式

**关键代码**:
```csharp
public class CleanupBackgroundService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Task? _cleanupTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _cleanupTask = RunCleanupAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }

        if (_cleanupTask != null)
        {
            await Task.WhenAny(_cleanupTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);

                using var scope = _serviceProvider.CreateScope();
                var fileManager = scope.ServiceProvider.GetRequiredService<TempFileManager>();
                fileManager.CleanupOldScans(TimeSpan.FromHours(24));
                Console.WriteLine("[Cleanup] Old scans cleaned up");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Cleanup] Background service stopped");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Error: {ex.Message}");
            }
        }
    }
}
```

---

## 数据流

### 扫描流程

```
用户请求
    │
    ▼
┌─────────────────────────────────────────┐
│  Web 界面 / 第三方应用          │
│  发送 POST /scan 请求             │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  HTTP API (Program.cs)             │
│  接收请求并验证参数               │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  ScannerFactory                    │
│  获取扫描仪服务                   │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  TwainScannerService               │
│  1. 初始化 TWAIN 会话            │
│  2. 枚举扫描仪                   │
│  3. 配置扫描参数                 │
│  4. 执行扫描                     │
│  5. 处理扫描事件                 │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  TWAIN 驱动                      │
│  访问扫描仪硬件                   │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  扫描仪硬件                      │
│  执行物理扫描                     │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  TwainScannerService               │
│  接收扫描数据                     │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  TempFileManager                  │
│  保存图像到临时文件               │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  文件系统                        │
│  存储 PNG 文件                    │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  HTTP API (Program.cs)             │
│  返回扫描结果                     │
└────────────────┬────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  Web 界面 / 第三方应用          │
│  接收扫描结果                     │
│  通过 GET /files/{id} 获取图像      │
└─────────────────────────────────────────┘
```

### 文件清理流程

```
应用启动
    │
    ▼
┌─────────────────────────────────────────┐
│  CleanupBackgroundService 启动      │
└────────────────┬────────────────────────┘
                 │
                 ▼
         ┌───────────────┐
         │  等待 1 小时  │
         └───────┬───────┘
                 │
                 ▼
┌─────────────────────────────────────────┐
│  TempFileManager                  │
│  检查过期文件（> 24 小时）      │
└────────────────┬────────────────────────┘
                 │
                 ▼
         ┌───────────────┐
         │  删除过期文件  │
         └───────┬───────┘
                 │
                 ▼
         ┌───────────────┐
         │  记录日志     │
         └───────┬───────┘
                 │
                 ▼
         ┌───────────────┐
         │  继续等待     │
         └───────────────┘
```

---

## 技术栈

### 后端

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 6.0 | 运行时框架 |
| ASP.NET Core | 6.0 | Web 框架 |
| NTwain | 3.7.5 | TWAIN 协议库 |
| C# | 10.0 | 编程语言 |

### 前端

| 技术 | 版本 | 用途 |
|------|------|------|
| HTML5 | - | 页面结构 |
| CSS3 | - | 样式设计 |
| JavaScript | ES6+ | 交互逻辑 |
| Fetch API | - | HTTP 请求 |

### 开发工具

| 工具 | 用途 |
|------|------|
| Visual Studio Code | 代码编辑 |
| .NET CLI | 编译和运行 |
| Git | 版本控制 |
| xUnit | 单元测试 |

---

## 设计模式

### 1. 工厂模式 (Factory Pattern)

**应用**: ScannerFactory

**目的**: 创建扫描仪服务实例，隐藏创建逻辑

**优点**:
- 解耦客户端和具体实现
- 易于扩展新的扫描仪服务
- 统一的创建接口

### 2. 策略模式 (Strategy Pattern)

**应用**: IScannerService 接口

**目的**: 定义扫描仪操作的算法族，使它们可以互换

**优点**:
- 算法可以独立于使用它的客户端变化
- 易于添加新的扫描仪实现
- 避免使用条件语句

### 3. 单例模式 (Singleton Pattern)

**应用**: TempFileManager, ScannerFactory

**目的**: 确保一个类只有一个实例，并提供全局访问点

**优点**:
- 节省资源
- 避免重复创建
- 保证状态一致性

### 4. 托管服务模式 (Hosted Service Pattern)

**应用**: CleanupBackgroundService

**目的**: 实现后台任务的生命周期管理

**优点**:
- 自动启动和停止
- 优雅关闭
- 与应用生命周期集成

---

## 扩展性

### 添加新的扫描仪服务

1. 实现 IScannerService 接口

```csharp
public class CustomScannerService : IScannerService
{
    public List<ScannerInfo> GetAvailableScanners()
    {
        // 实现扫描仪枚举
    }

    public Task<ScanResult> ScanAsync(ScanRequest request)
    {
        // 实现扫描逻辑
    }
}
```

2. 在 ScannerFactory 中注册

```csharp
public IScannerService GetScannerService()
{
    // 添加自定义逻辑
    if (useCustomService)
    {
        return new CustomScannerService(fileManager);
    }
    // ...
}
```

### 添加新的 API 端点

在 Program.cs 中添加新的路由：

```csharp
app.MapGet("/custom-endpoint", () =>
{
    return Results.Ok(new { message = "Custom endpoint" });
});
```

### 添加新的扫描参数

1. 扩展 ScanRequest 模型

```csharp
public class ScanRequest
{
    // 现有字段...
    public string? CustomParameter { get; set; }
}
```

2. 在 TwainScannerService 中处理

```csharp
private void SetScanParameters(DataSource source, ScanRequest request)
{
    // 现有参数设置...
    try
    {
        if (!string.IsNullOrEmpty(request.CustomParameter))
        {
            source.Capabilities.CustomCapability.SetValue(request.CustomParameter);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TWAIN] Failed to set custom parameter: {ex.Message}");
    }
}
```

---

## 性能考虑

### 内存管理

- 使用流式传输，避免一次性加载大文件
- 及时释放 Blob URL，防止内存泄漏
- 定期清理临时文件，防止磁盘空间耗尽

### 并发控制

- 使用 lock 保护共享资源
- 同一时间只允许一个扫描任务
- 扫描仪忙碌时返回 409 错误

### 缓存策略

- 扫描仪列表缓存 5 秒
- 避免频繁枚举扫描仪
- 提高响应速度

---

## 安全考虑

### 输入验证

- 验证所有 API 输入参数
- 防止路径遍历攻击
- 限制文件大小

### 错误处理

- 不暴露敏感信息
- 提供友好的错误消息
- 记录详细的错误日志

### 跨域控制

- 限制 CORS 来源
- 只允许受信任的域名
- 防止 CSRF 攻击

---

## 监控和日志

### 日志级别

- `[TWAIN]`: TWAIN 相关操作
- `[ERROR]`: 错误信息
- `[Cleanup]`: 清理任务信息

### 建议的监控指标

- 扫描成功率
- 平均扫描时间
- 错误率
- 内存使用情况
- 磁盘使用情况

---

**文档结束**
