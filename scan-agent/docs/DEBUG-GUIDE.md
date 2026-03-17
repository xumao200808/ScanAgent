# ScanAgent 调试指南

**版本**: 1.0.0
**更新日期**: 2026-03-17

---

## 📋 目录

1. [调试环境准备](#调试环境准备)
2. [日志系统](#日志系统)
3. [常见问题调试](#常见问题调试)
4. [性能调试](#性能调试)
5. [远程调试](#远程调试)
6. [调试工具](#调试工具)

---

## 调试环境准备

### 开发环境配置

#### 1. 安装开发工具

```bash
# 安装 .NET 6.0 SDK
dotnet --version

# 安装 Visual Studio Code
code --version

# 安装 C# Dev Kit 扩展（VS Code）
```

#### 2. 配置启动配置

在 `.vscode/launch.json` 中配置调试配置：

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Launch ScanAgent",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/ScanAgent/bin/Debug/net6.0-windows/ScanAgent.dll",
      "args": [],
      "cwd": "${workspaceFolder}/ScanAgent",
      "stopAtEntry": false,
      "serverReadyAction": {
        "pattern": "Now listening on:",
        "uriFormat": "%s",
        "action": "debugWithChrome"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://127.0.0.1:17289"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    }
  ]
}
```

#### 3. 配置任务配置

在 `.vscode/tasks.json` 中配置构建任务：

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/ScanAgent/ScanAgent.csproj",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile",
      "group": {
        "kind": "build",
        "isDefault": true
      }
    },
    {
      "label": "publish",
      "command": "dotnet",
      "type": "process",
      "args": [
        "publish",
        "${workspaceFolder}/ScanAgent/ScanAgent.csproj",
        "/property:GenerateFullPaths=true",
        "/consoleloggerparameters:NoSummary"
      ],
      "problemMatcher": "$msCompile",
      "group": "build"
    }
  ]
}
```

### 启用详细日志

在 `appsettings.json` 中配置日志级别：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ScanAgent": "Debug",
      "ScanAgent.Services": "Debug",
      "ScanAgent.Utils": "Debug"
    }
  }
}
```

---

## 日志系统

### 日志级别

ScanAgent 使用以下日志级别：

| 级别 | 用途 | 示例 |
|--------|------|------|
| Debug | 详细的调试信息 | `[TWAIN] Setting DPI to 300` |
| Information | 一般信息 | `[TWAIN] Scan completed` |
| Warning | 警告信息 | `[TWAIN] Failed to set duplex` |
| Error | 错误信息 | `[ERROR] Scan failed` |

### 日志输出位置

1. **控制台输出**: 默认输出到运行 ScanAgent 的命令窗口
2. **文件输出**: 可以重定向到文件

```bash
# 重定向到文件
ScanAgent.exe > scanagent.log 2>&1
```

### 关键日志标记

#### TWAIN 相关日志

```
[TWAIN] Session initialized successfully
[TWAIN] Found 1 scanner(s)
[TWAIN] Setting DPI to 300
[TWAIN] Setting color mode to gray
[TWAIN] Starting scan...
[TWAIN] Image saved: img_001
[TWAIN] Source disabled - scan completed
```

#### 错误日志

```
[ERROR] Failed to get scanners: TWAIN not available
[ERROR] Scan failed: Scanner not found
[ERROR] Transfer error
```

#### 清理日志

```
[Cleanup] Old scans cleaned up
[Cleanup] Background service stopped
```

### 日志分析技巧

#### 1. 查找 TWAIN 初始化失败

搜索日志中的 `[TWAIN] Session initialized`：

```bash
# Windows PowerShell
Select-String -Path scanagent.log -Pattern "Session initialized"

# Linux/Mac
grep "Session initialized" scanagent.log
```

如果找不到此日志，说明 TWAIN 初始化失败。

#### 2. 查找扫描仪枚举

搜索日志中的 `[TWAIN] Found`：

```bash
# Windows PowerShell
Select-String -Path scanagent.log -Pattern "Found.*scanner"

# Linux/Mac
grep "Found.*scanner" scanagent.log
```

#### 3. 查找扫描错误

搜索日志中的 `[ERROR]`：

```bash
# Windows PowerShell
Select-String -Path scanagent.log -Pattern "\[ERROR\]"

# Linux/Mac
grep "\[ERROR\]" scanagent.log
```

---

## 常见问题调试

### 问题 1：TWAIN 初始化失败

**症状**:
```
[TWAIN] Initialization failed: TWAIN data source manager not found
```

**调试步骤**:

1. 检查 TWAIN 驱动是否安装
   ```bash
   # 检查 TWAIN 文件
   dir C:\Windows\twain_32.dll
   dir C:\Windows\twain_64.dll
   ```

2. 检查扫描仪驱动是否安装
   - 打开"设备管理器"
   - 查找"图像处理设备"
   - 确认扫描仪在列表中

3. 尝试以管理员身份运行
   ```bash
   # 右键点击 ScanAgent.exe
   # 选择"以管理员身份运行"
   ```

4. 检查 TWAIN 数据源管理器
   - 打开 `C:\Windows\System32\wiaacmgr.exe`
   - 查看是否有 TWAIN 数据源

**解决方案**:
- 重新安装扫描仪驱动
- 确保使用 64 位版本的 ScanAgent
- 检查 TWAIN 版本兼容性

### 问题 2：扫描仪枚举失败

**症状**:
```
[TWAIN] Found 0 scanner(s)
```

**调试步骤**:

1. 检查扫描仪连接
   ```bash
   # 使用 PowerShell 检查 USB 设备
   Get-PnpDevice | Where-Object { $_.Class -eq "Image" }
   ```

2. 检查扫描仪是否开机
   - 确认扫描仪电源指示灯亮起
   - 尝试重新插拔 USB 线

3. 测试 TWAIN 数据源
   - 使用其他 TWAIN 应用程序测试扫描仪
   - 例如：Windows 扫描和传真

4. 检查扫描仪驱动状态
   ```bash
   # 查看驱动服务状态
   sc query | findstr /i "scan"
   ```

**解决方案**:
- 重新连接扫描仪
- 重启扫描仪
- 重新安装扫描仪驱动
- 重启电脑

### 问题 3：扫描超时

**症状**:
```
[TWAIN] Scan timeout after 2 minutes
```

**调试步骤**:

1. 检查扫描仪状态
   - 扫描仪是否卡纸
   - 扫描仪是否响应

2. 检查扫描参数
   - DPI 是否过高（尝试降低到 150 或 300）
   - 颜色模式是否合适

3. 检查网络连接
   - 如果使用网络扫描仪，检查网络状态
   - 测试网络延迟

4. 检查扫描仪驱动
   - 驱动是否最新版本
   - 驱动是否稳定

**解决方案**:
- 降低 DPI 设置
- 检查扫描仪硬件
- 更新扫描仪驱动
- 增加超时时间（修改代码）

### 问题 4：图像保存失败

**症状**:
```
[TWAIN] Error saving image: Access to the path is denied
```

**调试步骤**:

1. 检查临时目录权限
   ```bash
   # 检查 %TEMP%\ScanAgent 目录
   dir %TEMP%\ScanAgent

   # 检查权限
   icacls %TEMP%\ScanAgent
   ```

2. 检查磁盘空间
   ```bash
   # 检查磁盘空间
   fsutil volume diskfree C:
   ```

3. 检查文件名冲突
   - 查看临时目录中的文件
   - 确认没有重复的文件名

**解决方案**:
- 检查临时目录权限
- 清理临时文件
- 增加磁盘空间
- 检查文件命名逻辑

### 问题 5：内存泄漏

**症状**:
- 内存占用持续增长
- 长时间运行后变慢

**调试步骤**:

1. 使用任务管理器监控内存
   - 打开"任务管理器"
   - 找到 ScanAgent.exe
   - 观察"内存"列

2. 使用 dotnet-counters 监控
   ```bash
   # 安装 dotnet-counters
   dotnet tool install --global dotnet-counters

   # 监控进程
   dotnet-counters monitor --process-id <PID>
   ```

3. 检查 Blob URL 泄漏
   - 查看前端代码
   - 确认 URL.revokeObjectURL 被调用

4. 检查事件处理器泄漏
   - 查看 TwainScannerService 代码
   - 确认事件处理器被正确注销

**解决方案**:
- 及时释放 Blob URL
- 正确注销事件处理器
- 清理临时文件
- 重启 ScanAgent

---

## 性能调试

### 使用 dotnet-trace

```bash
# 安装 dotnet-trace
dotnet tool install --global dotnet-trace

# 收集跟踪信息
dotnet-trace collect --process-id <PID> --output trace.nettrace

# 分析跟踪信息
dotnet-trace report trace.nettrace
```

### 使用 dotnet-dump

```bash
# 安装 dotnet-dump
dotnet tool install --global dotnet-dump

# 收集内存转储
dotnet-dump collect --process-id <PID> --output dump.dmp

# 分析内存转储
dotnet-dump analyze dump.dmp
```

### 性能计数器

在代码中添加性能计数器：

```csharp
using System.Diagnostics;

var stopwatch = Stopwatch.StartNew();

try
{
    await scanner.ScanAsync(request);
}
finally
{
    stopwatch.Stop();
    Console.WriteLine($"[PERF] Scan completed in {stopwatch.ElapsedMilliseconds}ms");
}
```

### 内存分析

```csharp
using System.Diagnostics;

var process = Process.GetCurrentProcess();
var memoryBefore = process.PrivateMemorySize64;

await scanner.ScanAsync(request);

var memoryAfter = process.PrivateMemorySize64;
var memoryUsed = memoryAfter - memoryBefore;

Console.WriteLine($"[PERF] Memory used: {memoryUsed / 1024 / 1024}MB");
```

---

## 远程调试

### 配置远程调试

#### 1. 在远程机器上启用调试

修改 `appsettings.json`：

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:17289"
      }
    }
  }
}
```

#### 2. 配置防火墙

允许端口 17289 的入站连接：

```bash
# Windows PowerShell
New-NetFirewallRule -DisplayName "ScanAgent" -Direction Inbound -LocalPort 17289 -Protocol TCP -Action Allow

# Linux
sudo ufw allow 17289/tcp
```

#### 3. 在本地机器上连接

修改 `.vscode/launch.json`：

```json
{
  "name": "Remote Debug ScanAgent",
  "type": "coreclr",
  "request": "attach",
  "processName": "ScanAgent",
  "pipeTransport": {
    "pipeProgram": "enter-the-full-path-to-the-remote-machine",
    "pipeArgs": ["remoteDebugPipe"],
    "debuggerPath": "C:/Program Files/dotnet/vsdbg/vsdbg.exe",
    "pipeCwd": "${workspaceFolder}"
  }
}
```

---

## 调试工具

### 1. Visual Studio Code

**推荐扩展**:
- C# Dev Kit
- .NET Core Test Explorer
- REST Client

**调试快捷键**:
- `F5`: 开始调试
- `F9`: 切换断点
- `F10`: 单步跳过
- `F11`: 单步进入
- `Shift + F11`: 单步跳出
- `Shift + F5`: 停止调试

### 2. dotnet CLI

```bash
# 构建项目
dotnet build

# 运行项目
dotnet run

# 运行测试
dotnet test

# 发布项目
dotnet publish -c Release -r win-x64
```

### 3. Postman

用于测试 API 端点：

1. 导入 API 集合
2. 创建请求
3. 发送请求
4. 查看响应

### 4. curl

用于快速测试 API：

```bash
# 健康检查
curl http://127.0.0.1:17289/ping

# 枚举扫描仪
curl http://127.0.0.1:17289/scanners

# 执行扫描
curl -X POST http://127.0.0.1:17289/scan \
  -H "Content-Type: application/json" \
  -d '{"dpi":300,"color_mode":"gray","paper_size":"A4"}'
```

### 5. Wireshark

用于网络调试：

1. 安装 Wireshark
2. 开始捕获
3. 执行扫描操作
4. 分析 HTTP 请求和响应

---

## 调试最佳实践

### 1. 使用有意义的日志

```csharp
Console.WriteLine($"[TWAIN] Setting DPI to {request.Dpi}");
Console.WriteLine($"[TWAIN] Scan completed in {elapsedTime}ms");
```

### 2. 使用异常处理

```csharp
try
{
    await scanner.ScanAsync(request);
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Scan failed: {ex.Message}");
    Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
    throw;
}
```

### 3. 使用断点

在关键位置设置断点：
- 扫描开始
- 扫描结束
- 错误处理

### 4. 使用单元测试

```csharp
[Fact]
public async Task ScanAsync_WithValidRequest_ReturnsResult()
{
    var service = new TwainScannerService(fileManager);
    var request = new ScanRequest { Dpi = 300 };

    var result = await service.ScanAsync(request);

    Assert.NotNull(result);
    Assert.Equal("completed", result.Status);
}
```

---

## 调试检查清单

在提交代码前，确保：

- [ ] 所有日志级别正确设置
- [ ] 异常处理完善
- [ ] 单元测试通过
- [ ] 性能测试通过
- [ ] 内存泄漏检查通过
- [ ] 日志输出清晰
- [ ] 错误消息友好

---

## 获取帮助

如果以上方法无法解决您的问题：

1. 查看 [GitHub Issues](https://github.com/flashday/ScanAgent/issues)
2. 提交新的 Issue，包含：
   - 详细的问题描述
   - 复现步骤
   - 日志文件
   - 环境信息（操作系统、.NET 版本等）

---

**文档结束**
