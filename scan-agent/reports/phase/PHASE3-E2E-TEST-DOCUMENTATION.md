# Phase 3 端到端测试文档

**日期**: 2026-03-17
**阶段**: Phase 3 - 联调与优化
**任务**: T3.1 端到端测试

---

## 📋 测试目标

验证完整的扫描流程：启动 Agent → 扫描 → 上传 → OCR

---

## 🧪 测试场景

### 场景 1: 完整流程测试

**前置条件**:
- ✅ ScanAgent.exe 正在运行（监听 127.0.0.1:17289）
- ✅ 扫描仪已连接并开机
- ✅ 扫描仪驱动已安装
- ✅ 后端服务运行在 localhost:5000（可选）

**测试步骤**:

1. **启动 Agent**
   ```bash
   cd scan-agent/ScanAgent
   dotnet run --urls "http://127.0.0.1:17289"
   ```
   预期输出: `Now listening on: http://127.0.0.1:17289`

2. **健康检查**
   ```bash
   curl http://127.0.0.1:17289/ping
   ```
   预期响应:
   ```json
   {
     "status": "ok",
     "version": "1.0.0"
   }
   ```

3. **枚举扫描仪**
   ```bash
   curl http://127.0.0.1:17289/scanners
   ```
   预期响应:
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

4. **执行扫描**
   ```bash
   curl -X POST http://127.0.0.1:17289/scan \
     -H "Content-Type: application/json" \
     -d "{\"dpi\":300,\"color_mode\":\"gray\",\"paper_size\":\"A4\"}"
   ```
   预期响应:
   ```json
   {
     "scan_id": "scan_20260317_143022_123",
     "status": "completed",
     "images": [
       {
         "id": "img_001",
         "width": 2480,
         "height": 3508
       }
     ]
   }
   ```

5. **获取扫描图像**
   ```bash
   curl http://127.0.0.1:17289/files/img_001 --output test.png
   ```
   预期结果: 下载到 test.png 文件，文件大小 > 0

6. **上传到后端**（可选）
   ```bash
   curl -X POST http://localhost:5000/api/convert \
     -F "files=@test.png"
   ```
   预期响应:
   ```json
   {
     "job_id": "job_001",
     "status": "processing"
   }
   ```

7. **清理临时文件**
   ```bash
   curl -X DELETE http://127.0.0.1:17289/scans/scan_20260317_143022_123
   ```
   预期响应:
   ```json
   {
     "status": "ok"
   }
   ```

**验收标准**:
- ✅ 所有 API 调用成功
- ✅ 扫描图像可正常下载
- ✅ 图像文件大小合理（> 0）
- ✅ 上传到后端成功（如果后端运行）
- ✅ 临时文件可正常清理

---

### 场景 2: 多页扫描测试

**前置条件**:
- ✅ ScanAgent.exe 正在运行
- ✅ 支持自动进纸的扫描仪
- ✅ 扫描仪中放置多页文档

**测试步骤**:

1. **执行多页扫描**
   ```bash
   curl -X POST http://127.0.0.1:17289/scan \
     -H "Content-Type: application/json" \
     -d "{\"dpi\":300,\"color_mode\":\"gray\",\"auto_feed\":true}"
   ```

2. **验证多页结果**
   - 检查响应中的 `images` 数组长度
   - 预期: `images.length > 1`

3. **获取所有图像**
   ```bash
   # 获取第一页
   curl http://127.0.0.1:17289/files/img_001 --output page1.png
   
   # 获取第二页
   curl http://127.0.0.1:17289/files/img_002 --output page2.png
   
   # 获取第三页
   curl http://127.0.0.1:17289/files/img_003 --output page3.png
   ```

**验收标准**:
- ✅ 所有页面都被正确扫描
- ✅ 每个图像文件都有效
- ✅ 图像顺序正确

---

### 场景 3: 不同 DPI 和颜色模式测试

**测试矩阵**:

| DPI | 颜色模式 | 预期文件大小 |
|-----|-----------|--------------|
| 150 | gray | ~500KB |
| 300 | gray | ~2MB |
| 600 | gray | ~8MB |
| 300 | color | ~6MB |
| 300 | bw | ~1MB |

**测试步骤**:

1. **150 DPI 灰度**
   ```bash
   curl -X POST http://127.0.0.1:17289/scan \
     -H "Content-Type: application/json" \
     -d "{\"dpi\":150,\"color_mode\":\"gray\"}"
   ```

2. **300 DPI 灰度**
   ```bash
   curl -X POST http://127.0.0.1:17289/scan \
     -H "Content-Type: application/json" \
     -d "{\"dpi\":300,\"color_mode\":\"gray\"}"
   ```

3. **600 DPI 灰度**
   ```bash
   curl -X POST http://127.0.0.1:17289/scan \
     -H "Content-Type: application/json" \
     -d "{\"dpi\":600,\"color_mode\":\"gray\"}"
   ```

4. **300 DPI 彩色**
   ```bash
   curl -X POST http://127.0.0.1:17289/scan \
     -H "Content-Type: application/json" \
     -d "{\"dpi\":300,\"color_mode\":\"color\"}"
   ```

5. **300 DPI 黑白**
   ```bash
   curl -X POST http://127.0.0.1:17289/scan \
     -H "Content-Type: application/json" \
     -d "{\"dpi\":300,\"color_mode\":\"bw\"}"
   ```

**验收标准**:
- ✅ 所有配置都能正常扫描
- ✅ 文件大小符合预期范围
- ✅ 图像质量符合配置

---

### 场景 4: 错误场景测试

#### 4.1 Agent 离线测试

**测试步骤**:
1. 停止 ScanAgent.exe
2. 访问前端页面
3. 尝试执行扫描

**预期结果**:
- ✅ 前端显示 Agent 离线提示
- ✅ 提供"重新检测"按钮
- ✅ 显示帮助信息

#### 4.2 扫描仪未连接测试

**测试步骤**:
1. 断开扫描仪连接
2. 调用 `/scanners` 接口

**预期结果**:
```json
{
  "scanners": []
}
```

#### 4.3 扫描失败测试

**测试步骤**:
1. 扫描仪中不放置文档
2. 执行扫描

**预期结果**:
- ✅ 返回错误响应
- ✅ 错误信息清晰易懂
- ✅ 前端显示错误提示

---

## 🚀 自动化测试脚本

### Windows 批处理脚本

使用提供的 `test-e2e.bat` 脚本进行自动化测试：

```bash
cd scan-agent
test-e2e.bat
```

脚本将自动执行以下测试：
1. Agent 健康检查
2. 枚举扫描仪
3. 执行扫描（需要手动确认）
4. 获取扫描图像
5. 清理临时文件
6. 上传到后端（可选）

### PowerShell 脚本（可选）

对于更高级的测试场景，可以使用 PowerShell 脚本：

```powershell
# 测试 Agent 健康检查
$response = Invoke-RestMethod -Uri "http://127.0.0.1:17289/ping" -Method Get
Write-Host "Agent 状态: $($response.status)"

# 测试扫描仪枚举
$scanners = Invoke-RestMethod -Uri "http://127.0.0.1:17289/scanners" -Method Get
Write-Host "扫描仪数量: $($scanners.scanners.Count)"

# 执行扫描
$scanRequest = @{
    dpi = 300
    color_mode = "gray"
    paper_size = "A4"
} | ConvertTo-Json

$scanResult = Invoke-RestMethod -Uri "http://127.0.0.1:17289/scan" -Method Post -Body $scanRequest -ContentType "application/json"
Write-Host "Scan ID: $($scanResult.scan_id)"
Write-Host "图像数量: $($scanResult.images.Count)"
```

---

## 📊 测试报告模板

### 测试执行记录

| 测试场景 | 测试人员 | 执行日期 | 结果 | 备注 |
|---------|---------|---------|------|------|
| 完整流程测试 | | | | |
| 多页扫描测试 | | | | |
| 不同 DPI 测试 | | | | |
| 不同颜色模式测试 | | | | |
| Agent 离线测试 | | | | |
| 扫描仪未连接测试 | | | | |
| 扫描失败测试 | | | | |

### 性能测试记录

| 测试场景 | 页数 | DPI | 颜色模式 | 扫描时间 | 内存占用 | 文件大小 |
|---------|------|-----|-----------|---------|---------|---------|
| 单页扫描 | 1 | 300 | gray | | | |
| 多页扫描 | 10 | 300 | gray | | | |
| 高 DPI 扫描 | 1 | 600 | color | | | |
| 批量扫描 | 50 | 300 | gray | | | |

---

## ✅ 验收标准

### 功能验收
- ✅ 完整流程无阻塞
- ✅ 所有场景覆盖
- ✅ 错误处理正确
- ✅ 临时文件清理正常

### 性能验收
- ✅ 单页扫描（300 DPI 灰度）< 5 秒
- ✅ 连续扫描 50 页不崩溃
- ✅ 运行时内存占用 < 500MB

### 用户体验验收
- ✅ 错误提示清晰易懂
- ✅ 用户可自助解决常见问题
- ✅ 操作流程流畅自然

---

## 📝 测试注意事项

1. **硬件要求**
   - 至少一台 TWAIN 兼容扫描仪
   - 扫描仪驱动已正确安装
   - 测试文档（A4 纸张）

2. **环境要求**
   - Windows 10 或 Windows 11
   - .NET 6.0 SDK 或运行时
   - 至少 1GB 可用内存

3. **网络要求**
   - 端口 17289 未被占用
   - 后端服务（可选）运行在 localhost:5000

4. **测试数据**
   - 准备不同类型的测试文档
   - 准备多页文档用于多页测试
   - 准备彩色文档用于彩色扫描测试

---

## 🐛 问题记录

### 已知问题

| 问题 ID | 问题描述 | 严重程度 | 状态 | 解决方案 |
|---------|---------|---------|------|---------|
| | | | | |

### 测试中发现的问题

| 问题 ID | 问题描述 | 严重程度 | 状态 | 解决方案 |
|---------|---------|---------|------|---------|
| | | | | |

---

## 📄 相关文档

- [Phase 3 完成报告](file:///d:\PrivatePrj\ScanAgent\scan-agent\PHASE3-COMPLETION-REPORT.md)
- [端到端测试脚本](file:///d:\PrivatePrj\ScanAgent\scan-agent\test-e2e.bat)
- [API 接口文档](file:///d:\PrivatePrj\ScanAgent\SPEC\scanner-integration\design.md#32-核心接口设计)

---

**文档结束**
