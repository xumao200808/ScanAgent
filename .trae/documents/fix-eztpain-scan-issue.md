# EZTwain 扫描问题修复计划

## 问题分析

### 当前情况
1. **EZTwain 驱动能找到 3 个扫描仪**：
   - KODAK Scanner: i1410/i1420/i1440（ID: eztwain_0）
   - WIA-KODAK i1405 Scanner（ID: eztwain_1）
   - WIA-HP HP LaserJet Pro M304-M305（ID: eztwain_2）

2. **存在的问题**：
   - 选择第一个 KODAK Scanner 点击扫描会弹出扫描仪工具界面，点击 Scan 报错
   - 选择第二个 WIA-KODAK i1405 Scanner 提示 "EZTWAIN: window handle is invalid"
   - 用户希望直接使用扫描仪，不需要弹出扫描仪工具界面（参考 Delphi 程序只有预热提示）

### 根本原因
1. **窗口句柄无效问题**：
   - EZTwain 需要有效的窗口句柄来显示扫描 UI
   - 当前代码使用 `GetConsoleWindow()` 获取控制台窗口句柄，但这可能不是最佳选择
   - 应该使用 WinForms 的主窗口句柄或创建一个隐藏的窗口

2. **扫描 UI 弹出问题**：
   - EZTwain 的 `TWAIN_Acquire` 或 `TWAIN_AcquireNative` 函数会显示扫描仪 UI
   - Delphi 程序可能使用了不同的参数或调用了不同的函数来避免显示 UI
   - 需要检查 EZTwain32.pas 中 Delphi 是如何调用的

3. **TWAIN 数据源枚举问题**：
   - 从日志看，TWAIN 驱动枚举到了 Kodak 驱动文件
   - 但注册表中没有找到 TWAIN 数据源
   - EZTwain 能枚举到扫描仪说明它使用了不同的枚举方式

## 解决方案

### 步骤 1：修复窗口句柄问题
1. 在 `Program.cs` 中获取 WinForms 主窗口的句柄
2. 将句柄传递给 `EZTwainScannerService`
3. 确保在 STA 线程中创建和使用窗口句柄

### 步骤 2：优化 EZTwain 扫描方式
1. 检查 Delphi 代码中 `TWAIN_Acquire` 的调用方式
2. 尝试使用 `TWAIN_AcquireToFilename` 直接扫描到文件，避免显示 UI
3. 如果必须显示 UI，确保传递正确的窗口句柄

### 步骤 3：改进扫描仪选择逻辑
1. 区分真正的 TWAIN 扫描仪和 WIA 扫描仪
2. 对于 WIA 扫描仪，使用 WIA 驱动而不是 EZTwain
3. 对于 TWAIN 扫描仪，使用 EZTwain 驱动

### 步骤 4：参考 Delphi 实现
1. 查看 Delphi 代码中如何初始化 TWAIN
2. 查看 Delphi 如何调用扫描函数
3. 确保 C# 实现与 Delphi 保持一致

## 实施步骤

### 第一阶段：代码分析与准备
1. ✅ 分析日志文件，确认问题现象
2. 查看 Delphi 代码中 TWAIN 扫描的实现
3. 查看当前 `EZTwainScannerService.cs` 的实现

### 第二阶段：修复窗口句柄问题
1. 修改 `Program.cs`，获取 WinForms 窗口句柄
2. 修改 `ScannerFactory`，传递窗口句柄给 EZTwainService
3. 修改 `EZTwainScannerService`，使用正确的窗口句柄

### 第三阶段：优化扫描流程
1. 尝试使用 `TWAIN_AcquireToFilename` 避免显示 UI
2. 如果必须显示 UI，确保只在必要时显示
3. 添加扫描参数设置（分辨率、颜色模式等）

### 第四阶段：测试与验证
1. 重新编译打包
2. 在扫描电脑上测试 KODAK i1405 扫描仪
3. 验证扫描流程是否正常

## 预期结果
1. 选择 KODAK Scanner 后能正常扫描，不弹出错误或只显示必要的提示
2. 窗口句柄有效，不再报 "window handle is invalid" 错误
3. 扫描流程与 Delphi 程序类似，用户体验一致
