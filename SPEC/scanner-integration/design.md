# 扫描仪采集整合方案设计文档

创建日期：2026-03-15
版本：v1.0
状态：设计阶段
当前模式：开发模式

---

## 1. 背景与目标

### 1.1 现状

- ✅ 已实现：手机 H5 拍照上传（基于 `MobilePhotoUpload` 组件）
- ⏳ 待实现：扫描仪采集整合
- 📋 参考文档：`MDFiles/Enhancement/BS_扫描与手机影像采集方案整理_v2.md`

### 1.2 目标

在现有 B/S 架构下，整合扫描仪采集能力，实现：

1. PC 浏览器通过本地 Scan Agent 驱动扫描仪
2. 扫描图像统一进入现有 OCR 处理流程
3. 与手机拍照采集共用后端接口
4. 提供统一的前端交互体验

### 1.3 核心约束

- 浏览器无法直接访问扫描仪硬件
- 必须通过本地 Scan Agent 作为硬件隔离层
- Agent 以普通用户权限运行，不常驻后台
- 数据全程内网流转，不依赖公网

---

## 2. 架构设计

### 2.1 整体架构

```
┌─────────────────────────────────────────────────────────┐
│                    PC 浏览器 (React)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ 文件上传     │  │ 手机拍照     │  │ 扫描仪采集   │  │
│  │ (原有)       │  │ (已实现)     │  │ (新增)       │  │
│  └──────────────┘  └──────────────┘  └──────┬───────┘  │
└────────────────────────────────────────────┼───────────┘
                                             │ HTTP
                                             v
                              ┌──────────────────────────┐
                              │   Scan Agent (本地)      │
                              │   - 127.0.0.1:17289      │
                              │   - TWAIN / WIA 驱动     │
                              │   - 普通用户权限         │
                              └──────────┬───────────────┘
                                         │ HTTPS (内网)
                                         v
                              ┌──────────────────────────┐
                              │   后端 Python (Flask)    │
                              │   - /api/convert         │
                              │   - /api/v3/multipage/*  │
                              │   - OCR / 分类 / 抽取    │
                              └──────────────────────────┘
```

### 2.2 数据流设计

#### 方案 A：直接上传模式（推荐）

```
1. 浏览器 → Scan Agent: POST /scan (扫描参数)
2. Scan Agent → 扫描仪: TWAIN 驱动扫描
3. Scan Agent → 本地磁盘: 保存临时图像
4. Scan Agent → 浏览器: 返回本地文件路径列表
5. 浏览器 → Scan Agent: GET /files/{id} (逐个获取图像)
6. 浏览器 → 后端: POST /api/convert (上传图像)
7. 后端 → OCR 流程: 标准多页处理
```

**优点**：
- 浏览器完全控制上传时机
- 可预览扫描结果后再上传
- 与现有上传流程无缝对接

**缺点**：
- 需要两次网络传输（Agent→浏览器→后端）

#### 方案 B：代理上传模式

```
1. 浏览器 → 后端: POST /api/v3/scan/prepare (创建扫描任务)
2. 后端 → 浏览器: 返回 task_id + upload_token
3. 浏览器 → Scan Agent: POST /scan-and-upload (task_id + token + 后端地址)
4. Scan Agent → 扫描仪: TWAIN 驱动扫描
5. Scan Agent → 后端: POST /api/v3/scan/upload (直接上传图像)
6. 后端 → OCR 流程: 标准多页处理
7. 浏览器 → 后端: GET /api/v3/scan/status (轮询状态)
```

**优点**：
- 只需一次网络传输（Agent→后端）
- 适合大批量扫描场景

**缺点**：
- 浏览器无法预览扫描结果
- 需要新增后端接口

**推荐**：方案 A（直接上传模式），理由：
- 与现有架构对齐（手机拍照也是浏览器上传）
- 用户体验更好（可预览）
- 实现复杂度更低

---

## 3. Scan Agent 设计

### 3.1 技术选型

- **语言**：C# (.NET 6+)
- **目标框架**：net6.0-windows
- **扫描驱动**：TWAIN（主）+ WIA（兜底）
- **TWAIN 库**：NTwain 3.7.5 (NuGet)
- **HTTP 服务**：ASP.NET Core Minimal API
- **端口**：127.0.0.1:17289（固定端口，避免冲突）

### 3.2 核心接口设计

#### 3.2.1 健康检查

```http
GET /ping
Response: { "status": "ok", "version": "1.0.0" }
```

#### 3.2.2 枚举扫描仪

```http
GET /scanners
Response: {
  "scanners": [
    { "id": "scanner_0", "name": "Canon LiDE 300", "default": true }
  ]
}
```

#### 3.2.3 执行扫描

```http
POST /scan
Content-Type: application/json

{
  "scanner_id": "scanner_0",  // 可选，默认使用第一个
  "dpi": 300,
  "color_mode": "gray",       // "color" | "gray" | "bw"
  "duplex": false,
  "auto_feed": true,
  "paper_size": "A4"          // "A4" | "Letter" | "Auto"
}

Response: {
  "scan_id": "scan_20260315_143022",
  "status": "completed",
  "images": [
    { "id": "img_001", "path": "/temp/scan_xxx/page_001.png" }
  ]
}
```

#### 3.2.4 获取扫描图像

```http
GET /files/{image_id}
Response: image/png (二进制流)
```

#### 3.2.5 清理临时文件

```http
DELETE /scans/{scan_id}
Response: { "status": "ok" }
```

### 3.3 TWAIN 参数映射

| 业务参数 | TWAIN 能力 | 默认值 |
|---------|-----------|--------|
| dpi | ICapXResolution / ICapYResolution | 300 |
| color_mode | ICapPixelType (RGB/Gray/BW) | Gray |
| duplex | CapDuplexEnabled | false |
| auto_feed | CapFeederEnabled | true |
| paper_size | ICapSupportedSizes | A4 |

### 3.4 错误处理

```json
{
  "error": "scanner_not_found",
  "message": "未找到可用的扫描仪",
  "code": 404
}
```

常见错误码：
- `scanner_not_found` (404)
- `scanner_busy` (409)
- `scan_failed` (500)
- `twain_not_available` (503)

---

## 4. 前端集成设计

### 4.1 组件结构

```
frontend/src/components/scanner-upload/
├── ScannerUpload.tsx          # 主组件（类似 MobilePhotoUpload）
├── ScannerUpload.css
├── hooks/
│   ├── useScannerAgent.ts     # Agent 连接与健康检查
│   ├── useScannerList.ts      # 扫描仪枚举
│   └── useScanTask.ts         # 扫描任务管理
└── types.ts
```

### 4.2 交互流程

```
1. 用户点击"扫描仪采集"按钮
2. 前端检测 Scan Agent 是否运行 (GET /ping)
   - 未运行：提示用户启动 Agent
   - 已运行：进入扫描界面
3. 枚举可用扫描仪 (GET /scanners)
4. 用户选择扫描仪 + 配置参数（DPI、颜色模式等）
5. 点击"开始扫描"(POST /scan)
6. 显示扫描进度（轮询或 WebSocket）
7. 扫描完成后显示缩略图预览
8. 用户确认后，逐个获取图像 (GET /files/{id})
9. 上传到后端 (POST /api/convert)
10. 进入标准 OCR 流程
```

### 4.3 状态管理

```typescript
type ScannerStatus =
  | 'idle'           // 初始状态
  | 'checking'       // 检测 Agent
  | 'agent_offline'  // Agent 未运行
  | 'ready'          // Agent 就绪
  | 'scanning'       // 扫描中
  | 'preview'        // 预览扫描结果
  | 'uploading'      // 上传到后端
  | 'error';         // 错误状态
```

---

## 5. 后端集成设计

### 5.1 接口复用

扫描图像通过现有接口上传：

```python
# backend/api/upload_routes.py
@upload_bp.route('/convert', methods=['POST'])
def convert_document():
    # 现有逻辑，支持 PDF / 图片上传
    # 扫描图像作为普通图片上传，无需修改
```

### 5.2 元数据扩展（可选）

在 `job_meta.json` 中记录采集来源：

```json
{
  "job_id": "job_001",
  "source": "scanner",  // "upload" | "mobile" | "scanner"
  "scanner_info": {
    "scanner_name": "Canon LiDE 300",
    "dpi": 300,
    "color_mode": "gray"
  }
}
```

---

## 6. 部署与调试

### 6.1 Scan Agent 部署

#### 开发环境

```bash
# 1. 安装 .NET 6+ SDK
# 2. 克隆 Scan Agent 代码
git clone <scan-agent-repo>
cd scan-agent

# 3. 安装依赖
dotnet restore

# 4. 运行
dotnet run --urls "http://127.0.0.1:17289"
```

#### 生产环境

```bash
# 1. 发布单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained

# 2. 分发 ScanAgent.exe
# 3. 用户双击启动（无需安装）
```

### 6.2 前端调试

#### Mock 模式

在 Agent 未运行时，使用 Mock 数据：

```typescript
// frontend/src/components/scanner-upload/hooks/useScannerAgent.ts
const ENABLE_MOCK = import.meta.env.DEV && !navigator.userAgent.includes('Windows');

if (ENABLE_MOCK) {
  return mockScannerAgent();
}
```

#### 跨域配置

Agent 需要支持 CORS：

```csharp
// Scan Agent Program.cs
app.UseCors(policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyMethod()
    .AllowAnyHeader());
```

### 6.3 集成测试

```bash
# 1. 启动后端
cd backend
python app.py

# 2. 启动前端
cd frontend
npm run dev

# 3. 启动 Scan Agent
cd scan-agent
dotnet run

# 4. 浏览器访问 http://localhost:5173
# 5. 点击"扫描仪采集"按钮测试完整流程
```

---

## 7. 安全与权限

### 7.1 Agent 权限模型

| 阶段 | 是否需要管理员 |
|------|----------------|
| 扫描仪驱动安装 | 是 |
| Agent 安装 | 否（绿色软件） |
| Agent 运行 | 否 |

### 7.2 网络安全

- Agent 仅监听 `127.0.0.1`，不暴露到局域网
- 浏览器通过 `http://127.0.0.1:17289` 访问
- 后端接口使用 HTTPS（内网自签名证书）

### 7.3 数据安全

- 扫描图像临时存储在 `%TEMP%/ScanAgent/`
- 上传完成后自动清理
- 不保留任何扫描历史

---

## 8. 风险与限制

### 8.1 已知限制

1. **浏览器限制**：无法直接访问扫描仪，必须通过 Agent
2. **平台限制**：Scan Agent 仅支持 Windows（TWAIN/WIA）
3. **驱动依赖**：需要预先安装扫描仪驱动
4. **单实例限制**：同一台 PC 只能运行一个 Agent 实例

### 8.2 风险缓解

| 风险 | 缓解措施 |
|------|---------|
| Agent 未启动 | 前端检测并提示用户启动 |
| 扫描仪驱动缺失 | Agent 返回友好错误信息 |
| 端口冲突 | 使用固定端口 17289，冲突概率低 |
| TWAIN 不可用 | 自动降级到 WIA |

---

## 9. 实施计划

### 9.1 阶段划分

**Phase 1：Scan Agent 开发（2 周）**
- [ ] 搭建 C# + ASP.NET Core 项目
- [ ] 集成 NTwain 库
- [ ] 实现核心接口（/ping, /scanners, /scan, /files）
- [ ] 单元测试 + 真机测试

**Phase 2：前端集成（1 周）**
- [ ] 创建 ScannerUpload 组件
- [ ] 实现 Agent 健康检查
- [ ] 实现扫描参数配置 UI
- [ ] 实现预览与上传逻辑

**Phase 3：联调与优化（1 周）**
- [ ] 端到端集成测试
- [ ] 错误处理优化
- [ ] 性能优化（大批量扫描）
- [ ] 用户体验优化

**Phase 4：文档与部署（3 天）**
- [ ] 用户手册（如何启动 Agent）
- [ ] 部署脚本
- [ ] 发布 Agent 安装包

### 9.2 里程碑

- **M1**（2 周后）：Scan Agent 可独立运行并扫描
- **M2**（3 周后）：前端可调用 Agent 并预览图像
- **M3**（4 周后）：完整流程打通，可进入 OCR
- **M4**（4.5 周后）：生产就绪，文档完善

---

## 10. 参考资料

- [NTwain GitHub](https://github.com/soukoku/ntwain)
- [TWAIN 规范](https://www.twain.org/)
- [WIA 文档](https://docs.microsoft.com/en-us/windows/win32/wia/)
- 现有文档：`MDFiles/Enhancement/BS_扫描与手机影像采集方案整理_v2.md`
- 现有实现：`frontend/src/components/mobile-upload/MobilePhotoUpload.tsx`

---

**文档结束**
