# ScanAgent 扩展指南

**版本**: 1.0.0
**更新日期**: 2026-03-17

---

## 📋 目录

1. [扩展概述](#扩展概述)
2. [添加新的扫描仪服务](#添加新的扫描仪服务)
3. [扩展 API 接口](#扩展-api-接口)
4. [自定义扫描参数](#自定义扫描参数)
5. [添加新的功能模块](#添加新的功能模块)
6. [集成第三方服务](#集成第三方服务)
7. [性能优化](#性能优化)
8. [安全增强](#安全增强)

---

## 扩展概述

ScanAgent 设计为可扩展的架构，支持以下扩展方式：

- 添加新的扫描仪服务（TWAIN, WIA, 自定义）
- 扩展 API 接口
- 自定义扫描参数
- 添加新的功能模块
- 集成第三方服务

### 扩展原则

1. **开闭原则**: 对扩展开放，对修改关闭
2. **单一职责**: 每个组件只负责一个功能
3. **依赖注入**: 使用依赖注入管理组件
4. **接口隔离**: 使用接口定义契约
5. **最小知识**: 组件之间保持最小依赖

---

## 添加新的扫描仪服务

### 步骤 1：实现 IScannerService 接口

创建新的扫描仪服务类：

```csharp
using ScanAgent.Models;
using ScanAgent.Utils;

namespace ScanAgent.Services;

public class CustomScannerService : IScannerService
{
    private readonly TempFileManager _fileManager;
    private bool _initialized = false;

    public CustomScannerService(TempFileManager fileManager)
    {
        _fileManager = fileManager;
        InitializeService();
    }

    private void InitializeService()
    {
        try
        {
            // 初始化自定义扫描仪服务
            _initialized = true;
            Console.WriteLine("[CustomScanner] Service initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CustomScanner] Initialization failed: {ex.Message}");
            _initialized = false;
        }
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        if (!_initialized)
        {
            Console.WriteLine("[CustomScanner] Service not initialized");
            return new List<ScannerInfo>();
        }

        var scanners = new List<ScannerInfo>();
        int index = 0;

        foreach (var scanner in EnumerateScanners())
        {
            scanners.Add(new ScannerInfo
            {
                Id = $"custom_scanner_{index}",
                Name = scanner.Name,
                Default = index == 0
            });
            index++;
        }

        Console.WriteLine($"[CustomScanner] Found {scanners.Count} scanner(s)");
        return scanners;
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        if (!_initialized)
        {
            throw new Exception("Custom scanner service not initialized");
        }

        var scanner = FindScanner(request.ScannerId);
        if (scanner == null)
            throw new ScannerNotFoundException();

        try
        {
            var images = new List<ImageInfo>();
            var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fff}";

            foreach (var page in await ScanPages(scanner, request))
            {
                var imageId = _fileManager.SaveImage(scanId, page.Stream);
                images.Add(new ImageInfo
                {
                    Id = imageId,
                    Width = page.Width,
                    Height = page.Height
                });
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
            CloseScanner(scanner);
        }
    }

    private List<CustomScanner> EnumerateScanners()
    {
        // 实现扫描仪枚举逻辑
        return new List<CustomScanner>();
    }

    private CustomScanner? FindScanner(string? scannerId)
    {
        // 实现扫描仪查找逻辑
        return null;
    }

    private async Task<List<ScannedPage>> ScanPages(CustomScanner scanner, ScanRequest request)
    {
        // 实现扫描逻辑
        return new List<ScannedPage>();
    }

    private void CloseScanner(CustomScanner scanner)
    {
        // 实现扫描仪关闭逻辑
    }
}
```

### 步骤 2：在 ScannerFactory 中注册

修改 `ScannerFactory.cs`：

```csharp
public class ScannerFactory
{
    private IScannerService? _primaryService;
    private readonly TempFileManager _fileManager;
    private readonly object _lock = new();

    public ScannerFactory(TempFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public IScannerService GetScannerService(string? scannerType = "twain")
    {
        lock (_lock)
        {
            if (_primaryService == null)
            {
                switch (scannerType.ToLowerInvariant())
                {
                    case "twain":
                        _primaryService = new TwainScannerService(_fileManager);
                        break;
                    case "wia":
                        _primaryService = new WiaScannerService(_fileManager);
                        break;
                    case "custom":
                        _primaryService = new CustomScannerService(_fileManager);
                        break;
                    default:
                        _primaryService = new TwainScannerService(_fileManager);
                        break;
                }
            }

            return _primaryService;
        }
    }
}
```

### 步骤 3：更新 API 端点

修改 `Program.cs`：

```csharp
app.MapPost("/scan", async (ScanRequest request, HttpContext context, ScannerFactory factory) =>
{
    try
    {
        var scannerType = context.Request.Query["scanner_type"];
        var scanner = factory.GetScannerService(scannerType);
        var result = await scanner.ScanAsync(request);
        return Results.Ok(result);
    }
    catch (ScannerNotFoundException)
    {
        return Results.Json(
            new { error = "scanner_not_found", message = "未找到可用的扫描仪" },
            statusCode: 404);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Scan failed: {ex.Message}");
        return Results.Json(
            new { error = "scan_failed", message = ex.Message },
            statusCode: 500);
    }
});
```

---

## 扩展 API 接口

### 添加新的 API 端点

在 `Program.cs` 中添加新的路由：

```csharp
// 获取扫描仪详细信息
app.MapGet("/scanners/{scannerId}", (string scannerId, ScannerFactory factory) =>
{
    try
    {
        var scanner = factory.GetScannerService();
        var scanners = scanner.GetAvailableScanners();
        var scannerInfo = scanners.FirstOrDefault(s => s.Id == scannerId);

        if (scannerInfo == null)
            return Results.NotFound();

        var details = new
        {
            id = scannerInfo.Id,
            name = scannerInfo.Name,
            capabilities = new
            {
                supportsDuplex = true,
                supportsAutoFeed = true,
                maxDpi = 600,
                supportedPaperSizes = new[] { "A4", "A3", "Letter", "Legal" }
            }
        };

        return Results.Ok(details);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to get scanner details: {ex.Message}");
        return Results.Json(
            new { error = "internal_error", message = ex.Message },
            statusCode: 500);
    }
});

// 获取扫描历史
app.MapGet("/scans/history", (TempFileManager fileManager) =>
{
    try
    {
        var scanDirs = Directory.GetDirectories(Path.Combine(Path.GetTempPath(), "ScanAgent"));
        var history = scanDirs.Select(dir => new
        {
            scanId = Path.GetFileName(dir),
            createdAt = Directory.GetCreationTime(dir),
            imageCount = Directory.GetFiles(dir).Length
        }).OrderByDescending(h => h.CreatedAt);

        return Results.Ok(new { history });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to get scan history: {ex.Message}");
        return Results.Json(
            new { error = "internal_error", message = ex.Message },
            statusCode: 500);
    }
});

// 批量删除扫描
app.MapDelete("/scans/batch", (List<string> scanIds, TempFileManager fileManager) =>
{
    try
    {
        var deleted = new List<string>();
        foreach (var scanId in scanIds)
        {
            if (fileManager.CleanupScan(scanId))
            {
                deleted.Add(scanId);
            }
        }

        return Results.Ok(new { deleted, count = deleted.Count });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to batch delete scans: {ex.Message}");
        return Results.Json(
            new { error = "internal_error", message = ex.Message },
            statusCode: 500);
    }
});
```

### 添加中间件

```csharp
// 请求日志中间件
app.Use(async (context, next) =>
{
    Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");
    await next();
    Console.WriteLine($"[RESPONSE] {context.Response.StatusCode}");
});

// 认证中间件
app.Use(async (context, next) =>
{
    var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
    if (string.IsNullOrEmpty(apiKey))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized");
        return;
    }

    if (!ValidateApiKey(apiKey))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Forbidden");
        return;
    }

    await next();
});

bool ValidateApiKey(string apiKey)
{
    // 实现 API Key 验证逻辑
    return apiKey == "your-secret-key";
}
```

---

## 自定义扫描参数

### 扩展 ScanRequest 模型

```csharp
namespace ScanAgent.Models;

public class ScanRequest
{
    public string? ScannerId { get; set; }
    public int Dpi { get; set; }
    public string ColorMode { get; set; }
    public string PaperSize { get; set; }
    public bool? Duplex { get; set; }
    public bool? AutoFeed { get; set; }

    // 新增参数
    public int? Brightness { get; set; }
    public int? Contrast { get; set; }
    public int? Threshold { get; set; }
    public string? OutputFormat { get; set; }
    public bool? AutoCrop { get; set; }
    public bool? AutoDeskew { get; set; }
}
```

### 在 TwainScannerService 中处理新参数

```csharp
private void SetScanParameters(DataSource source, ScanRequest request)
{
    // 现有参数设置...
    SetDpi(source, request.Dpi);
    SetColorMode(source, request.ColorMode);
    SetPaperSize(source, request.PaperSize);
    SetDuplex(source, request.Duplex);
    SetAutoFeed(source, request.AutoFeed);

    // 新增参数设置
    SetBrightness(source, request.Brightness);
    SetContrast(source, request.Contrast);
    SetThreshold(source, request.Threshold);
    SetOutputFormat(source, request.OutputFormat);
    SetAutoCrop(source, request.AutoCrop);
    SetAutoDeskew(source, request.AutoDeskew);
}

private void SetBrightness(DataSource source, int? brightness)
{
    if (!brightness.HasValue)
        return;

    try
    {
        source.Capabilities.ICapBrightness.SetValue((float)brightness.Value);
        Console.WriteLine($"[TWAIN] Brightness set to {brightness}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TWAIN] Failed to set brightness: {ex.Message}");
    }
}

private void SetContrast(DataSource source, int? contrast)
{
    if (!contrast.HasValue)
        return;

    try
    {
        source.Capabilities.ICapContrast.SetValue((float)contrast.Value);
        Console.WriteLine($"[TWAIN] Contrast set to {contrast}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TWAIN] Failed to set contrast: {ex.Message}");
    }
}

private void SetThreshold(DataSource source, int? threshold)
{
    if (!threshold.HasValue)
        return;

    try
    {
        source.Capabilities.ICapThreshold.SetValue((float)threshold.Value);
        Console.WriteLine($"[TWAIN] Threshold set to {threshold}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TWAIN] Failed to set threshold: {ex.Message}");
    }
}

private void SetOutputFormat(DataSource source, string? outputFormat)
{
    if (string.IsNullOrEmpty(outputFormat))
        return;

    try
    {
        var format = outputFormat.ToLowerInvariant() switch
        {
            "png" => ImageFileFormat.Png,
            "jpg" => ImageFileFormat.Jpeg,
            "tiff" => ImageFileFormat.Tiff,
            _ => ImageFileFormat.Png
        };

        source.Capabilities.ICapImageFileFormat.SetValue(format);
        Console.WriteLine($"[TWAIN] Output format set to {outputFormat}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TWAIN] Failed to set output format: {ex.Message}");
    }
}

private void SetAutoCrop(DataSource source, bool? autoCrop)
{
    if (!autoCrop.HasValue)
        return;

    try
    {
        source.Capabilities.ICapAutomaticCrop.SetValue(
            autoCrop.Value ? BoolType.True : BoolType.False);
        Console.WriteLine($"[TWAIN] Auto crop set to {autoCrop}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TWAIN] Failed to set auto crop: {ex.Message}");
    }
}

private void SetAutoDeskew(DataSource source, bool? autoDeskew)
{
    if (!autoDeskew.HasValue)
        return;

    try
    {
        source.Capabilities.ICapAutomaticDeskew.SetValue(
            autoDeskew.Value ? BoolType.True : BoolType.False);
        Console.WriteLine($"[TWAIN] Auto deskew set to {autoDeskew}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[TWAIN] Failed to set auto deskew: {ex.Message}");
    }
}
```

---

## 添加新的功能模块

### 图像处理模块

创建图像处理服务：

```csharp
using System.Drawing;
using System.Drawing.Imaging;

namespace ScanAgent.Services;

public interface IImageProcessor
{
    Stream ProcessImage(Stream inputStream, ImageProcessingOptions options);
}

public class ImageProcessingOptions
{
    public int? Brightness { get; set; }
    public int? Contrast { get; set; }
    public bool? AutoRotate { get; set; }
    public bool? AutoCrop { get; set; }
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
}

public class BasicImageProcessor : IImageProcessor
{
    public Stream ProcessImage(Stream inputStream, ImageProcessingOptions options)
    {
        using var image = Image.FromStream(inputStream);
        using var bitmap = new Bitmap(image);

        // 应用亮度调整
        if (options.Brightness.HasValue)
        {
            ApplyBrightness(bitmap, options.Brightness.Value);
        }

        // 应用对比度调整
        if (options.Contrast.HasValue)
        {
            ApplyContrast(bitmap, options.Contrast.Value);
        }

        // 应用自动旋转
        if (options.AutoRotate == true)
        {
            ApplyAutoRotate(bitmap);
        }

        // 应用自动裁剪
        if (options.AutoCrop == true)
        {
            ApplyAutoCrop(bitmap);
        }

        // 调整大小
        if (options.MaxWidth.HasValue || options.MaxHeight.HasValue)
        {
            ResizeImage(bitmap, options.MaxWidth, options.MaxHeight);
        }

        // 保存到输出流
        var outputStream = new MemoryStream();
        bitmap.Save(outputStream, ImageFormat.Png);
        outputStream.Position = 0;

        return outputStream;
    }

    private void ApplyBrightness(Bitmap bitmap, int brightness)
    {
        // 实现亮度调整逻辑
    }

    private void ApplyContrast(Bitmap bitmap, int contrast)
    {
        // 实现对比度调整逻辑
    }

    private void ApplyAutoRotate(Bitmap bitmap)
    {
        // 实现自动旋转逻辑
    }

    private void ApplyAutoCrop(Bitmap bitmap)
    {
        // 实现自动裁剪逻辑
    }

    private void ResizeImage(Bitmap bitmap, int? maxWidth, int? maxHeight)
    {
        // 实现大小调整逻辑
    }
}
```

### OCR 集成模块

创建 OCR 服务：

```csharp
using Tesseract;

namespace ScanAgent.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(Stream imageStream, string language = "eng");
}

public class TesseractOcrService : IOcrService
{
    private readonly string _tessDataPath;

    public TesseractOcrService(string tessDataPath)
    {
        _tessDataPath = tessDataPath;
    }

    public async Task<string> ExtractTextAsync(Stream imageStream, string language = "eng")
    {
        return await Task.Run(() =>
        {
            using var engine = new TesseractEngine(_tessDataPath, language, EngineMode.Default);
            using var img = Pix.LoadFromMemoryStream(imageStream);
            using var page = engine.Process(img);

            var text = page.GetText();
            Console.WriteLine($"[OCR] Extracted {text.Length} characters");

            return text;
        });
    }
}
```

### 注册新服务

在 `Program.cs` 中注册：

```csharp
builder.Services.AddSingleton<IImageProcessor, BasicImageProcessor>();
builder.Services.AddSingleton<IOcrService, TesseractOcrService>(sp =>
{
    var tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
    return new TesseractOcrService(tessDataPath);
});
```

---

## 集成第三方服务

### 云存储集成

```csharp
using Azure.Storage.Blobs;

namespace ScanAgent.Services;

public interface ICloudStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName);
    Task<Stream> DownloadAsync(string fileName);
    Task<bool> DeleteAsync(string fileName);
}

public class AzureBlobStorageService : ICloudStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(string connectionString, string containerName)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    }

    public async Task<string> UploadAsync(Stream stream, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(stream);
        Console.WriteLine($"[CloudStorage] Uploaded {fileName}");

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    public async Task<bool> DeleteAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.DeleteAsync();
        Console.WriteLine($"[CloudStorage] Deleted {fileName}");

        return true;
    }
}
```

### 消息队列集成

```csharp
using Azure.Storage.Queues;

namespace ScanAgent.Services;

public interface IMessageQueueService
{
    Task SendMessageAsync<T>(T message);
}

public class AzureQueueService : IMessageQueueService
{
    private readonly QueueClient _queueClient;

    public AzureQueueService(string connectionString, string queueName)
    {
        _queueClient = new QueueClient(connectionString, queueName);
    }

    public async Task SendMessageAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        await _queueClient.SendMessageAsync(json);
        Console.WriteLine($"[MessageQueue] Sent message: {json}");
    }
}
```

---

## 性能优化

### 并发处理

```csharp
public class ConcurrentScanProcessor
{
    private readonly SemaphoreSlim _semaphore;
    private readonly IScannerService _scannerService;

    public ConcurrentScanProcessor(IScannerService scannerService, int maxConcurrentScans = 1)
    {
        _scannerService = scannerService;
        _semaphore = new SemaphoreSlim(maxConcurrentScans, maxConcurrentScans);
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        await _semaphore.WaitAsync();
        try
        {
            return await _scannerService.ScanAsync(request);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

### 缓存优化

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
}

public class MemoryCacheService : ICacheService
{
    private readonly MemoryCache _cache;

    public MemoryCacheService()
    {
        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = 1024,
            CompactionPercentage = 0.25
        });
    }

    public Task<T?> GetAsync<T>(string key)
    {
        return Task.FromResult(_cache.Get<T>(key));
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };
        _cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}
```

---

## 安全增强

### API Key 认证

```csharp
public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, string apiKey)
    {
        _next = next;
        _apiKey = apiKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var providedKey = context.Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(providedKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key is required");
            return;
        }

        if (providedKey != _apiKey)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        await _next(context);
    }
}
```

### 速率限制

```csharp
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Dictionary<string, RateLimitInfo> _rateLimits;
    private readonly object _lock = new();

    public RateLimitMiddleware(RequestDelegate next, int maxRequests, TimeSpan window)
    {
        _next = next;
        _rateLimits = new Dictionary<string, RateLimitInfo>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientId = GetClientId(context);
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            if (!_rateLimits.ContainsKey(clientId))
            {
                _rateLimits[clientId] = new RateLimitInfo();
            }

            var info = _rateLimits[clientId];

            if (now - info.WindowStart > TimeSpan.FromMinutes(1))
            {
                info.Reset(now);
            }

            if (info.RequestCount >= 100)
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60";
                await context.Response.WriteAsync("Rate limit exceeded");
                return;
            }

            info.RequestCount++;
        }

        await _next(context);
    }

    private string GetClientId(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private class RateLimitInfo
    {
        public int RequestCount { get; set; }
        public DateTime WindowStart { get; set; }

        public void Reset(DateTime now)
        {
            RequestCount = 1;
            WindowStart = now;
        }
    }
}
```

---

## 扩展检查清单

在提交扩展代码前，确保：

- [ ] 遵循现有代码风格
- [ ] 添加适当的单元测试
- [ ] 更新 API 文档
- [ ] 更新用户手册
- [ ] 处理所有异常
- [ ] 添加日志记录
- [ ] 性能测试通过
- [ ] 安全审查通过
- [ ] 向后兼容

---

## 获取帮助

如果需要扩展方面的帮助：

1. 查看 [架构设计文档](file:///d:\PrivatePrj\ScanAgent\scan-agent\ARCHITECTURE-DESIGN.md)
2. 查看 [API 接口文档](file:///d:\PrivatePrj\ScanAgent\scan-agent\API-DOCUMENTATION.md)
3. 提交 GitHub Issue 或 Pull Request

---

**文档结束**
