# 扫描仪采集本机调试指南

创建日期：2026-03-15
版本：v1.0
适用场景：开发环境本机调试

---

## 1. 环境准备

### 1.1 必需软件

**后端（已有）**
- Python 3.8+
- 依赖包（requirements.txt）

**前端（已有）**
- Node.js 16+
- npm 或 yarn

**Scan Agent（新增）**
- .NET 6 SDK 或更高版本
- Windows 操作系统（TWAIN/WIA 仅支持 Windows）

### 1.2 安装 .NET SDK

```bash
# 检查是否已安装
dotnet --version

# 如果未安装，下载安装：
# https://dotnet.microsoft.com/download/dotnet/6.0
```

### 1.3 扫描仪准备（可选）

**有真实扫描仪**：
- 确保扫描仪已连接
- 安装官方驱动程序
- 在"设备和打印机"中确认可见

**无扫描仪**：
- 使用 Mock 模式调试前端
- Agent 部分需要真机测试

---

## 2. 项目结构

```
pdf-to-editable-web/
├── backend/              # Python 后端（已有）
├── frontend/             # React 前端（已有）
└── scan-agent/           # C# Scan Agent（待创建）
    ├── ScanAgent.csproj
    ├── Program.cs
    ├── Services/
    ├── Models/
    └── Utils/
```

---

## 3. Scan Agent 开发环境搭建

### 3.1 创建项目

```bash
# 在项目根目录
mkdir scan-agent
cd scan-agent

# 创建 ASP.NET Core 项目
dotnet new web -n ScanAgent
cd ScanAgent

# 添加 NuGet 依赖
dotnet add package NTwain
dotnet add package Microsoft.AspNetCore.Cors
```

### 3.2 项目配置

编辑 `ScanAgent.csproj`，确保：

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="NTwain" Version="3.7.2" />
  </ItemGroup>
</Project>
```

### 3.3 启动 Agent

```bash
cd scan-agent/ScanAgent
dotnet run --urls "http://127.0.0.1:17289"
```

看到以下输出表示成功：
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://127.0.0.1:17289
```

---

## 4. 三服务联调步骤

### 4.1 启动顺序

**终端 1：启动后端**
```bash
cd backend
python app.py
# 默认运行在 http://localhost:5000
```

**终端 2：启动前端**
```bash
cd frontend
npm run dev
# 默认运行在 http://localhost:5173
```

**终端 3：启动 Scan Agent**
```bash
cd scan-agent/ScanAgent
dotnet run --urls "http://127.0.0.1:17289"
```

### 4.2 验证各服务

```bash
# 验证后端
curl http://localhost:5000/api/health

# 验证前端
# 浏览器访问 http://localhost:5173

# 验证 Scan Agent
curl http://127.0.0.1:17289/ping
# 应返回: {"status":"ok","version":"1.0.0"}
```

---

## 5. 前端 Mock 模式（无扫描仪时）

### 5.1 启用 Mock

编辑 `frontend/src/components/scanner-upload/hooks/useScannerAgent.ts`：

```typescript
const ENABLE_MOCK = true; // 开发时强制启用 Mock

export function useScannerAgent() {
  if (ENABLE_MOCK) {
    return {
      isOnline: true,
      checking: false,
      checkAgent: async () => true,
    };
  }
  // 正常逻辑...
}
```

### 5.2 Mock 扫描数据

创建 `frontend/src/components/scanner-upload/mocks/mockScanData.ts`：

```typescript
export function mockScanTask() {
  return {
    status: 'idle' as const,
    images: [],
    errorMsg: '',
    startScan: async (params: any) => {
      // 模拟扫描延迟
      await new Promise(resolve => setTimeout(resolve, 2000));

      // 返回 Mock 图像
      const mockBlob = await fetch('/mock-scan-page.png').then(r => r.blob());
      return {
        status: 'preview',
        images: [
          { id: 'mock_001', blob: mockBlob },
          { id: 'mock_002', blob: mockBlob },
        ],
      };
    },
    reset: () => {},
  };
}
```

### 5.3 准备 Mock 图像

```bash
# 在 frontend/public/ 下放置测试图像
cp ~/Downloads/test-document.png frontend/public/mock-scan-page.png
```

---

## 6. 调试技巧

### 6.1 Agent 调试

**方法 1：Visual Studio Code**
```bash
# 安装 C# 扩展
# 打开 scan-agent 目录
code scan-agent/

# F5 启动调试
```

**方法 2：命令行日志**
```csharp
// Program.cs 中添加日志
app.MapPost("/scan", async (ScanRequest request) =>
{
    Console.WriteLine($"[DEBUG] Scan request: {JsonSerializer.Serialize(request)}");
    // ...
});
```

### 6.2 前端调试

**浏览器 DevTools**
```javascript
// 在 Hook 中添加日志
console.log('[Scanner] Agent status:', isOnline);
console.log('[Scanner] Scan params:', params);
```

**React DevTools**
- 安装 React DevTools 扩展
- 查看组件状态和 Props

### 6.3 网络调试

**查看 Agent 请求**
```bash
# Chrome DevTools -> Network
# 筛选 127.0.0.1:17289
```

**CORS 问题排查**
```bash
# 确认 Agent 的 CORS 配置
# Program.cs 中应有：
app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());
```

---

## 7. 常见问题排查

### Q1: Agent 启动失败 "端口被占用"

**原因**：端口 17289 已被占用

**解决**：
```bash
# Windows 查找占用进程
netstat -ano | findstr :17289

# 杀死进程（替换 PID）
taskkill /PID <PID> /F

# 或更换端口
dotnet run --urls "http://127.0.0.1:17290"
```

### Q2: 前端提示 "Agent 离线"

**排查步骤**：
1. 确认 Agent 是否运行
   ```bash
   curl http://127.0.0.1:17289/ping
   ```

2. 检查 CORS 配置
   ```bash
   # 浏览器 Console 是否有 CORS 错误
   ```

3. 检查防火墙
   ```bash
   # Windows 防火墙可能阻止本地连接
   ```

### Q3: 扫描时提示 "未找到扫描仪"

**排查步骤**：
1. 确认扫描仪驱动已安装
   ```bash
   # 打开"设备和打印机"查看
   ```

2. 测试 TWAIN 可用性
   ```csharp
   // 在 Agent 中添加测试代码
   var session = new TwainSession(appId);
   session.Open();
   Console.WriteLine($"Found {session.Count()} scanners");
   ```

3. 尝试 WIA 兜底
   ```csharp
   // 如果 TWAIN 不可用，切换到 WIA
   ```

### Q4: 扫描图像无法上传到后端

**排查步骤**：
1. 检查图像 Blob 是否有效
   ```javascript
   console.log('Image blob size:', blob.size);
   ```

2. 检查 FormData 构造
   ```javascript
   console.log('FormData entries:', [...formData.entries()]);
   ```

3. 检查后端接口
   ```bash
   # 直接测试上传
   curl -X POST http://localhost:5000/api/convert \
     -F "file=@test.png"
   ```

---

## 8. 快速测试脚本

### 8.1 一键启动所有服务

创建 `scripts/start-dev.sh`：

```bash
#!/bin/bash

# 启动后端
cd backend
python app.py &
BACKEND_PID=$!

# 启动前端
cd ../frontend
npm run dev &
FRONTEND_PID=$!

# 启动 Scan Agent
cd ../scan-agent/ScanAgent
dotnet run --urls "http://127.0.0.1:17289" &
AGENT_PID=$!

echo "Services started:"
echo "  Backend PID: $BACKEND_PID"
echo "  Frontend PID: $FRONTEND_PID"
echo "  Agent PID: $AGENT_PID"
echo ""
echo "Press Ctrl+C to stop all services"

# 等待中断信号
trap "kill $BACKEND_PID $FRONTEND_PID $AGENT_PID" EXIT
wait
```

使用：
```bash
chmod +x scripts/start-dev.sh
./scripts/start-dev.sh
```

### 8.2 健康检查脚本

创建 `scripts/check-services.sh`：

```bash
#!/bin/bash

echo "Checking services..."

# 检查后端
if curl -s http://localhost:5000/api/health > /dev/null; then
    echo "✅ Backend: OK"
else
    echo "❌ Backend: FAIL"
fi

# 检查前端
if curl -s http://localhost:5173 > /dev/null; then
    echo "✅ Frontend: OK"
else
    echo "❌ Frontend: FAIL"
fi

# 检查 Scan Agent
if curl -s http://127.0.0.1:17289/ping > /dev/null; then
    echo "✅ Scan Agent: OK"
else
    echo "❌ Scan Agent: FAIL"
fi
```

---

## 9. 推荐开发流程

### 阶段 1：Agent 独立开发
1. 只启动 Agent
2. 使用 curl 或 Postman 测试接口
3. 确保 /ping, /scanners, /scan 都正常

### 阶段 2：前端 Mock 开发
1. 启动前端（Mock 模式）
2. 完成 UI 交互逻辑
3. 不依赖真实 Agent

### 阶段 3：前后端联调
1. 启动 Agent + 前端
2. 测试完整扫描流程
3. 不上传到后端

### 阶段 4：全链路测试
1. 启动所有服务
2. 测试扫描 → 上传 → OCR 完整流程

---

## 10. 性能监控

### 10.1 Agent 性能

```csharp
// 添加性能日志
var sw = Stopwatch.StartNew();
var result = await scanner.ScanAsync(request);
sw.Stop();
Console.WriteLine($"[PERF] Scan took {sw.ElapsedMilliseconds}ms");
```

### 10.2 前端性能

```javascript
// 监控上传速度
const startTime = Date.now();
await uploadToBackend(images);
const duration = Date.now() - startTime;
console.log(`[PERF] Upload took ${duration}ms`);
```

---

**文档结束**
