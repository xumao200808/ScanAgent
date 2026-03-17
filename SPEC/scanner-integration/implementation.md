# 扫描仪采集实现细节文档

创建日期：2026-03-15
版本：v1.0
状态：实施指南
当前模式：开发模式

---

## 1. Scan Agent 实现细节

### 1.1 技术栈

- **语言**：C# (.NET 6+)
- **目标框架**：net6.0-windows
- **TWAIN 库**：NTwain 3.7.5（正式支持 net6.0-windows7.0）
- **HTTP 服务**：ASP.NET Core Minimal API

### 1.2 项目结构

```
ScanAgent/
├── ScanAgent.csproj
├── Program.cs                 # 主入口 + HTTP 服务
├── Services/
│   ├── IScannerService.cs     # 扫描服务接口
│   ├── TwainScannerService.cs # TWAIN 实现
│   ├── WiaScannerService.cs   # WIA 实现（兜底）
│   └── ScannerFactory.cs      # 工厂模式选择驱动
├── Models/
│   ├── ScanRequest.cs
│   ├── ScanResult.cs
│   └── ScannerInfo.cs
└── Utils/
    ├── TempFileManager.cs     # 临时文件管理
    └── ImageConverter.cs      # 图像格式转换
```

### 1.2 核心代码示例

#### Program.cs（HTTP 服务）

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IScannerService, TwainScannerService>();
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());

// 健康检查
app.MapGet("/ping", () => Results.Ok(new { status = "ok", version = "1.0.0" }));

// 枚举扫描仪
app.MapGet("/scanners", (IScannerService scanner) =>
{
    var scanners = scanner.GetAvailableScanners();
    return Results.Ok(new { scanners });
});

// 执行扫描
app.MapPost("/scan", async (ScanRequest request, IScannerService scanner) =>
{
    try
    {
        var result = await scanner.ScanAsync(request);
        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

// 获取图像
app.MapGet("/files/{imageId}", (string imageId, TempFileManager fileManager) =>
{
    var filePath = fileManager.GetFilePath(imageId);
    if (!File.Exists(filePath))
        return Results.NotFound();
    return Results.File(filePath, "image/png");
});

app.Run("http://127.0.0.1:17289");
```

#### TwainScannerService.cs（核心扫描逻辑）

```csharp
using NTwain;
using NTwain.Data;

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
        foreach (var source in _session)
        {
            scanners.Add(new ScannerInfo
            {
                Id = source.Name,
                Name = source.Name,
                IsDefault = scanners.Count == 0
            });
        }
        return scanners;
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        var source = _session.FirstOrDefault(s => s.Name == request.ScannerId)
                     ?? _session.FirstOrDefault();

        if (source == null)
            throw new Exception("未找到可用的扫描仪");

        source.Open();

        // 设置扫描参数
        SetScanParameters(source, request);

        var images = new List<ImageInfo>();
        var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss}";

        source.DataTransferred += (sender, e) =>
        {
            var bitmap = e.GetNativeImageStream();
            var imagePath = _fileManager.SaveImage(scanId, bitmap);
            images.Add(new ImageInfo
            {
                Id = Path.GetFileNameWithoutExtension(imagePath),
                Path = imagePath
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

    private void SetScanParameters(DataSource source, ScanRequest request)
    {
        // DPI
        source.Capabilities.ICapXResolution.SetValue((float)request.Dpi);
        source.Capabilities.ICapYResolution.SetValue((float)request.Dpi);

        // 颜色模式
        var pixelType = request.ColorMode switch
        {
            "color" => PixelType.RGB,
            "gray" => PixelType.Gray,
            "bw" => PixelType.BlackWhite,
            _ => PixelType.Gray
        };
        source.Capabilities.ICapPixelType.SetValue(pixelType);

        // 双面扫描
        if (request.Duplex)
        {
            source.Capabilities.CapDuplexEnabled.SetValue(BoolType.True);
        }

        // 自动进纸
        if (request.AutoFeed)
        {
            source.Capabilities.CapFeederEnabled.SetValue(BoolType.True);
            source.Capabilities.CapAutoFeed.SetValue(BoolType.True);
        }
    }
}
```

---

## 2. 前端实现细节

### 2.1 useScannerAgent Hook

```typescript
// frontend/src/components/scanner-upload/hooks/useScannerAgent.ts
import { useState, useCallback } from 'react';

const AGENT_BASE_URL = 'http://127.0.0.1:17289';

export function useScannerAgent() {
  const [isOnline, setIsOnline] = useState(false);
  const [checking, setChecking] = useState(false);

  const checkAgent = useCallback(async () => {
    setChecking(true);
    try {
      const response = await fetch(`${AGENT_BASE_URL}/ping`, {
        method: 'GET',
        signal: AbortSignal.timeout(2000),
      });
      const online = response.ok;
      setIsOnline(online);
      return online;
    } catch {
      setIsOnline(false);
      return false;
    } finally {
      setChecking(false);
    }
  }, []);

  return { isOnline, checking, checkAgent };
}
```

### 2.2 useScanTask Hook

```typescript
// frontend/src/components/scanner-upload/hooks/useScanTask.ts
import { useState, useCallback } from 'react';

interface ScanParams {
  scannerId?: string;
  dpi: number;
  colorMode: 'color' | 'gray' | 'bw';
  duplex: boolean;
  autoFeed: boolean;
}

export function useScanTask() {
  const [status, setStatus] = useState<'idle' | 'scanning' | 'preview' | 'error'>('idle');
  const [images, setImages] = useState<Array<{ id: string; blob: Blob }>>([]);
  const [errorMsg, setErrorMsg] = useState('');

  const startScan = useCallback(async (params: ScanParams) => {
    setStatus('scanning');
    setErrorMsg('');

    try {
      // 1. 调用 Agent 扫描
      const scanResponse = await fetch('http://127.0.0.1:17289/scan', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          scanner_id: params.scannerId,
          dpi: params.dpi,
          color_mode: params.colorMode,
          duplex: params.duplex,
          auto_feed: params.autoFeed,
        }),
      });

      if (!scanResponse.ok) {
        throw new Error('扫描失败');
      }

      const scanResult = await scanResponse.json();

      // 2. 获取扫描图像
      const imageBlobs: Array<{ id: string; blob: Blob }> = [];
      for (const img of scanResult.images) {
        const fileResponse = await fetch(`http://127.0.0.1:17289/files/${img.id}`);
        const blob = await fileResponse.blob();
        imageBlobs.push({ id: img.id, blob });
      }

      setImages(imageBlobs);
      setStatus('preview');
    } catch (error) {
      setErrorMsg(error instanceof Error ? error.message : '扫描失败');
      setStatus('error');
    }
  }, []);

  const reset = useCallback(() => {
    setStatus('idle');
    setImages([]);
    setErrorMsg('');
  }, []);

  return { status, images, errorMsg, startScan, reset };
}
```

### 2.3 ScannerUpload 组件

```typescript
// frontend/src/components/scanner-upload/ScannerUpload.tsx
import { useState, useEffect } from 'react';
import { useScannerAgent } from './hooks/useScannerAgent';
import { useScanTask } from './hooks/useScanTask';

interface Props {
  onJobReady: (jobId: string) => void;
  onClose: () => void;
}

export function ScannerUpload({ onJobReady, onClose }: Props) {
  const { isOnline, checking, checkAgent } = useScannerAgent();
  const { status, images, errorMsg, startScan, reset } = useScanTask();
  const [scanParams, setScanParams] = useState({
    dpi: 300,
    colorMode: 'gray' as const,
    duplex: false,
    autoFeed: true,
  });

  useEffect(() => {
    checkAgent();
  }, [checkAgent]);

  async function handleStartScan() {
    await startScan(scanParams);
  }

  async function handleUpload() {
    // 上传到后端
    const formData = new FormData();
    images.forEach((img, index) => {
      formData.append('files', img.blob, `page_${index + 1}.png`);
    });

    const response = await fetch('/api/convert', {
      method: 'POST',
      body: formData,
    });

    const result = await response.json();
    if (result.job_id) {
      onJobReady(result.job_id);
    }
  }

  return (
    <div className="scanner-upload-modal">
      {/* Agent 离线提示 */}
      {!isOnline && !checking && (
        <div className="agent-offline">
          <p>⚠️ 扫描代理未运行</p>
          <p>请启动 ScanAgent.exe 后重试</p>
          <button onClick={checkAgent}>重新检测</button>
        </div>
      )}

      {/* 扫描参数配置 */}
      {isOnline && status === 'idle' && (
        <div className="scan-config">
          <label>
            DPI:
            <select value={scanParams.dpi} onChange={(e) => setScanParams({ ...scanParams, dpi: Number(e.target.value) })}>
              <option value={150}>150</option>
              <option value={300}>300</option>
              <option value={600}>600</option>
            </select>
          </label>
          <label>
            颜色模式:
            <select value={scanParams.colorMode} onChange={(e) => setScanParams({ ...scanParams, colorMode: e.target.value as any })}>
              <option value="color">彩色</option>
              <option value="gray">灰度</option>
              <option value="bw">黑白</option>
            </select>
          </label>
          <button onClick={handleStartScan}>开始扫描</button>
        </div>
      )}

      {/* 扫描中 */}
      {status === 'scanning' && <div>⏳ 扫描中...</div>}

      {/* 预览 */}
      {status === 'preview' && (
        <div className="scan-preview">
          <div className="preview-grid">
            {images.map((img) => (
              <img key={img.id} src={URL.createObjectURL(img.blob)} alt="扫描预览" />
            ))}
          </div>
          <button onClick={handleUpload}>上传并识别</button>
          <button onClick={reset}>重新扫描</button>
        </div>
      )}

      {/* 错误 */}
      {status === 'error' && (
        <div className="scan-error">
          <p>❌ {errorMsg}</p>
          <button onClick={reset}>重试</button>
        </div>
      )}
    </div>
  );
}
```

---

## 3. 集成到 WorkflowPage

### 3.1 添加扫描按钮

```typescript
// frontend/src/app/components/WorkflowPage.tsx
import { ScannerUpload } from '../../components/scanner-upload/ScannerUpload';

const WorkflowPage: React.FC = () => {
  const [showScannerUpload, setShowScannerUpload] = useState(false);

  // ... 现有代码 ...

  return (
    <div className="workflow-page">
      {/* 上传区域 */}
      {currentStep <= 2 && (
        <div className="upload-section">
          <button onClick={() => setShowMobileUpload(true)}>
            📱 手机拍照
          </button>
          <button onClick={() => setShowScannerUpload(true)}>
            🖨️ 扫描仪采集
          </button>
        </div>
      )}

      {/* 扫描仪上传弹窗 */}
      {showScannerUpload && (
        <ScannerUpload
          onJobReady={(jobId) => {
            setShowScannerUpload(false);
            // 进入 OCR 流程
            setCurrentStep(3);
          }}
          onClose={() => setShowScannerUpload(false)}
        />
      )}
    </div>
  );
};
```

---

## 4. 测试策略

### 4.1 单元测试

#### Agent 测试

```csharp
// ScanAgent.Tests/TwainScannerServiceTests.cs
[Fact]
public void GetAvailableScanners_ShouldReturnList()
{
    var service = new TwainScannerService(new TempFileManager());
    var scanners = service.GetAvailableScanners();
    Assert.NotNull(scanners);
}
```

#### 前端测试

```typescript
// frontend/src/components/scanner-upload/__tests__/useScannerAgent.test.ts
import { renderHook, waitFor } from '@testing-library/react';
import { useScannerAgent } from '../hooks/useScannerAgent';

test('checkAgent should detect online status', async () => {
  const { result } = renderHook(() => useScannerAgent());

  await waitFor(() => {
    expect(result.current.checking).toBe(false);
  });
});
```

### 4.2 集成测试

```bash
# 1. 启动所有服务
./scripts/start-all.sh

# 2. 运行 E2E 测试
npm run test:e2e -- scanner-upload.spec.ts
```

---

## 5. 部署清单

### 5.1 Scan Agent 打包

```bash
# Windows x64 单文件发布
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true
```

输出：`ScanAgent/bin/Release/net6.0/win-x64/publish/ScanAgent.exe`

### 5.2 用户手册

创建 `ScanAgent_用户手册.md`：

```markdown
# 扫描代理使用说明

## 安装

1. 下载 `ScanAgent.exe`
2. 放置到任意目录（如 `C:\ScanAgent\`）
3. 无需安装，双击即可运行

## 使用

1. 确保扫描仪已连接并安装驱动
2. 双击 `ScanAgent.exe` 启动代理
3. 看到"Agent is running on http://127.0.0.1:17289"提示即成功
4. 在浏览器中点击"扫描仪采集"按钮
5. 使用完毕后可关闭 Agent 窗口

## 常见问题

Q: 提示"未找到扫描仪"？
A: 请检查扫描仪驱动是否正确安装

Q: 端口被占用？
A: 检查是否已有 Agent 实例运行，或端口 17289 被其他程序占用
```

---

**文档结束**
