# TRAE IDE 安装调试指南

创建日期：2026-03-17
适用场景：在 TRAE IDE 中开发 ScanAgent 项目

---

## 📋 目录

1. [环境要求](#环境要求)
2. [安装 .NET SDK](#安装-net-sdk)
3. [TRAE IDE 配置](#trae-ide-配置)
4. [运行项目](#运行项目)
5. [调试项目](#调试项目)
6. [常见问题](#常见问题)

---

## 环境要求

### 必需软件

| 软件 | 版本要求 | 用途 |
|------|----------|------|
| .NET SDK | 6.0 或更高 | 运行和编译项目 |
| TRAE IDE | 最新版本 | 代码编辑和调试 |

### 可选软件

| 软件 | 用途 |
|------|------|
| 扫描仪驱动 | 测试扫描功能 |
| curl / Postman | 测试 API 接口 |

---

## 安装 .NET SDK

### 1. 检查是否已安装

在 TRAE IDE 终端运行：

```bash
dotnet --version
```

**如果显示版本号**（如 `6.0.4xx`），说明已安装，跳过此步骤。

**如果提示命令不存在**，需要安装。

### 2. 下载 .NET SDK

**官方下载地址**：
- 中文：https://dotnet.microsoft.com/zh-cn/download/dotnet/6.0
- 英文：https://dotnet.microsoft.com/download/dotnet/6.0

**选择版本**：
- 下载 **.NET 6.0 SDK**（不是 Runtime）
- 选择 **Windows x64** 版本

### 3. 安装 .NET SDK

1. 双击下载的安装程序（如 `dotnet-sdk-6.0.4xx-win-x64.exe`）
2. 按提示完成安装（需要管理员权限）
3. 安装完成后重启 TRAE IDE

### 4. 验证安装

```bash
# 在 TRAE IDE 终端运行
dotnet --version

# 应该显示类似：
# 6.0.4xx
```

---

## TRAE IDE 配置

### 1. 打开项目

在 TRAE IDE 中打开项目目录：
```
D:\PrivatePrj\ScanAgent\scan-agent\ScanAgent
```

### 2. 验证项目结构

确认以下文件存在：
```
ScanAgent/
├── ScanAgent.csproj       # 项目文件
├── Program.cs             # 主入口
├── Services/             # 服务层
├── Models/              # 数据模型
└── Utils/               # 工具类
```

### 3. 配置 C# 扩展（如果需要）

TRAE IDE 通常内置 C# 支持，但如果需要额外功能：

- 安装 C# Dev Kit 扩展
- 安装 .NET Core Test Explorer（用于测试）

---

## 运行项目

### 方式 1：使用 dotnet run（开发模式）

**适用场景**：开发调试，代码修改后立即生效

#### 步骤 1：恢复依赖

```bash
# 在 TRAE IDE 终端运行
cd D:\PrivatePrj\ScanAgent\scan-agent\ScanAgent
dotnet restore
```

**预期输出**：
```
  Determining projects to restore...
  Restored D:\PrivatePrj\ScanAgent\scan-agent\ScanAgent\ScanAgent.csproj (in 1.2 sec).
```

#### 步骤 2：运行项目

```bash
dotnet run --urls "http://127.0.0.1:17289"
```

**预期输出**：
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://127.0.0.1:17289
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

**说明**：
- 项目启动成功
- 监听在 `http://127.0.0.1:17289`
- 按 `Ctrl+C` 停止服务

---

### 方式 2：使用 dotnet build + run（分离编译和运行）

**适用场景**：先编译检查错误，再运行

#### 步骤 1：编译项目

```bash
dotnet build
```

**预期输出**：
```
Microsoft (R) Build Engine version 17.x.x.x
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

**如果有错误**：
- 检查错误信息
- 修复代码后重新编译

#### 步骤 2：运行编译后的程序

```bash
dotnet bin/Debug/net6.0/ScanAgent.dll --urls "http://127.0.0.1:17289"
```

---

### 方式 3：发布后运行（生产模式）

**适用场景**：测试发布版本

#### 步骤 1：发布项目

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

#### 步骤 2：运行发布版本

```bash
cd bin/Release/net6.0/win-x64/publish
.\ScanAgent.exe
```

---

## 调试项目

### 1. 设置断点

在 TRAE IDE 中：
- 打开要调试的文件（如 `Program.cs`）
- 在代码行号左侧点击，设置红色断点

### 2. 启动调试

**方法 1：使用 F5**
- 按 `F5` 键启动调试
- 程序会在断点处暂停

**方法 2：使用调试命令**
```bash
# 如果 TRAE IDE 支持
dotnet run --urls "http://127.0.0.1:17289"
```

### 3. 调试操作

- **继续**：F5 或点击"继续"按钮
- **单步执行**：F10（跳过）或 F11（进入）
- **查看变量**：鼠标悬停在变量上
- **调用栈**：查看函数调用链
- **监视**：添加变量到监视窗口

### 4. 调试输出

在 TRAE IDE 的"输出"或"调试控制台"查看：
- Console.WriteLine 的输出
- 调试信息
- 异常信息

---

## 测试 API 接口

### 1. 健康检查

**在新终端运行**：
```bash
curl http://127.0.0.1:17289/ping
```

**预期响应**：
```json
{
  "status": "ok",
  "version": "1.0.0"
}
```

### 2. 枚举扫描仪

```bash
curl http://127.0.0.1:17289/scanners
```

**预期响应**：
```json
{
  "scanners": [
    {
      "id": "scanner_0",
      "name": "你的扫描仪名称",
      "default": true
    }
  ]
}
```

**注意**：
- 如果没有扫描仪，返回空数组
- 如果 TWAIN 不可用，返回 503 错误

### 3. 执行扫描（需要扫描仪）

```bash
curl -X POST http://127.0.0.1:17289/scan `
  -H "Content-Type: application/json" `
  -d "{\"dpi\":300,\"color_mode\":\"gray\"}"
```

**预期响应**：
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

---

## 常见问题

### Q1: dotnet 命令不存在

**错误信息**：
```
'dotnet' 不是内部或外部命令
```

**解决方法**：
1. 检查 .NET SDK 是否安装
2. 重启 TRAE IDE
3. 检查 PATH 环境变量

### Q2: 端口被占用

**错误信息**：
```
System.IO.IOException: Failed to bind to address http://127.0.0.1:17289
```

**解决方法**：
```bash
# 查找占用进程
netstat -ano | findstr :17289

# 杀死进程（替换 PID）
taskkill /PID <PID> /F

# 或更换端口
dotnet run --urls "http://127.0.0.1:17290"
```

### Q3: NTwain 库找不到

**错误信息**：
```
error CS0246: The type or namespace name 'NTwain' could not be found
```

**解决方法**：
```bash
# 恢复 NuGet 包
dotnet restore

# 清理并重新构建
dotnet clean
dotnet build
```

### Q4: 编译错误

**常见错误**：
- 缺少 using 引用
- 类型不匹配
- 方法不存在

**解决方法**：
1. 查看错误信息
2. 检查代码语法
3. 参考 C# 文档

### Q5: 调试无法启动

**可能原因**：
- 断点位置不正确
- 调试配置问题

**解决方法**：
1. 检查断点是否在可执行代码行
2. 清理并重新构建
3. 重启 TRAE IDE

---

## 开发工作流

### 推荐流程

```
1. 编辑代码
   ↓
2. 保存文件
   ↓
3. 运行项目（dotnet run）
   ↓
4. 测试 API（curl 或浏览器）
   ↓
5. 查看控制台输出
   ↓
6. 如有问题，设置断点调试
   ↓
7. 修复问题，重复流程
```

### 快捷键

| 操作 | 快捷键 |
|------|--------|
| 运行 | F5 |
| 停止 | Shift+F5 |
| 调试 | F5 |
| 继续调试 | F5 |
| 单步跳过 | F10 |
| 单步进入 | F11 |
| 设置断点 | F9 |
| 清除所有断点 | Ctrl+Shift+F9 |

---

## 性能优化

### 1. 编译优化

**开发模式**：
```bash
dotnet run  # Debug 模式，包含调试信息
```

**发布模式**：
```bash
dotnet run -c Release  # Release 模式，优化性能
```

### 2. 内存监控

```bash
# 在 TRAE IDE 终端监控内存
Get-Process -Name dotnet | Select-Object ProcessName, CPU, WorkingSet
```

---

## 下一步

完成环境配置后，可以：

1. ✅ 开始开发新功能
2. ✅ 修复已知问题
3. ✅ 添加单元测试
4. ✅ 性能优化

---

## 参考资源

- [.NET 官方文档](https://docs.microsoft.com/dotnet/)
- [ASP.NET Core 文档](https://docs.microsoft.com/aspnet/core/)
- [NTwain GitHub](https://github.com/soukoku/ntwain)
- [TRAE IDE 文档](https://help.trae.ai/)

---

**文档版本**：v1.0
**最后更新**：2026-03-17
**维护者**：ScanAgent 开发团队