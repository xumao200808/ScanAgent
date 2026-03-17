# ScanAgent API 接口文档

**版本**: 1.0.0
**更新日期**: 2026-03-17
**基础 URL**: `http://127.0.0.1:17289`

---

## 📋 目录

1. [概述](#概述)
2. [认证](#认证)
3. [错误处理](#错误处理)
4. [接口列表](#接口列表)
5. [数据模型](#数据模型)
6. [示例代码](#示例代码)

---

## 概述

ScanAgent 提供基于 HTTP 的 RESTful API，用于扫描仪枚举、扫描执行和文件管理。

### 技术栈

- **框架**: ASP.NET Core 6.0
- **协议**: HTTP/1.1
- **数据格式**: JSON
- **字符编码**: UTF-8

### 跨域支持

ScanAgent 支持 CORS，允许来自以下源的请求：

- `http://localhost:5173`
- `http://127.0.0.1:5173`

---

## 认证

当前版本不需要认证。所有接口均可直接访问。

**注意**: 在生产环境中，建议添加 API Key 或 Token 认证机制。

---

## 错误处理

所有错误响应遵循统一格式：

```json
{
  "error": "error_code",
  "message": "Human readable error message"
}
```

### 错误码

| HTTP 状态码 | 错误码 | 说明 |
|-------------|---------|------|
| 404 | scanner_not_found | 扫描仪未找到 |
| 409 | scanner_busy | 扫描仪正在被占用 |
| 500 | scan_failed | 扫描失败 |
| 503 | twain_not_available | TWAIN 驱动不可用 |

---

## 接口列表

### 1. 健康检查

检查 ScanAgent 是否正常运行。

**请求**:
```http
GET /ping
```

**响应**:
```json
{
  "status": "ok",
  "version": "1.0.0"
}
```

**字段说明**:
- `status`: 服务状态，固定为 "ok"
- `version`: ScanAgent 版本号

**示例**:
```bash
curl http://127.0.0.1:17289/ping
```

---

### 2. 枚举扫描仪

获取所有可用的扫描仪列表。

**请求**:
```http
GET /scanners
```

**响应**:
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
      "name": "HP ScanJet Pro 2500",
      "default": false
    }
  ]
}
```

**字段说明**:
- `scanners`: 扫描仪数组
  - `id`: 扫描仪唯一标识符
  - `name`: 扫描仪名称
  - `default`: 是否为默认扫描仪

**错误响应**:
```json
{
  "error": "twain_not_available",
  "message": "TWAIN session not initialized"
}
```

**示例**:
```bash
curl http://127.0.0.1:17289/scanners
```

---

### 3. 执行扫描

执行扫描操作并返回扫描结果。

**请求**:
```http
POST /scan
Content-Type: application/json

{
  "scannerId": "scanner_0",
  "dpi": 300,
  "colorMode": "gray",
  "paperSize": "A4",
  "duplex": false,
  "autoFeed": true
}
```

**请求参数**:

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| scannerId | string | 否 | 扫描仪 ID，默认使用第一个扫描仪 |
| dpi | integer | 是 | 扫描分辨率（150, 300, 600） |
| colorMode | string | 是 | 颜色模式（gray, color, bw） |
| paperSize | string | 是 | 纸张尺寸（A4, A3, Letter, Legal） |
| duplex | boolean | 否 | 是否双面扫描，默认 false |
| autoFeed | boolean | 否 | 是否自动进纸，默认 true |

**响应**:
```json
{
  "scanId": "scan_20260317_143022_123",
  "status": "completed",
  "images": [
    {
      "id": "img_001"
    }
  ]
}
```

**字段说明**:
- `scanId`: 扫描任务唯一标识符
- `status`: 扫描状态（completed）
- `images`: 扫描图像数组
  - `id`: 图像唯一标识符

**错误响应**:

**404 - 扫描仪未找到**:
```json
{
  "error": "scanner_not_found",
  "message": "未找到可用的扫描仪"
}
```

**409 - 扫描仪忙碌**:
```json
{
  "error": "scanner_busy",
  "message": "扫描仪正在被占用"
}
```

**500 - 扫描失败**:
```json
{
  "error": "scan_failed",
  "message": "Scan operation failed"
}
```

**示例**:
```bash
curl -X POST http://127.0.0.1:17289/scan \
  -H "Content-Type: application/json" \
  -d '{
    "dpi": 300,
    "colorMode": "gray",
    "paperSize": "A4"
  }'
```

---

### 4. 获取扫描图像

获取指定 ID 的扫描图像。

**请求**:
```http
GET /files/{image_id}
```

**路径参数**:
- `image_id`: 图像 ID（例如：img_001）

**响应**:
- **Content-Type**: image/png
- **Body**: PNG 图像二进制数据

**错误响应**:

**404 - 图像未找到**:
```
HTTP 404 Not Found
```

**404 - 扫描任务未找到**:
```
HTTP 404 Not Found
```

**示例**:
```bash
curl http://127.0.0.1:17289/files/img_001 --output scan.png
```

---

### 5. 清理扫描文件

删除指定扫描任务的所有临时文件。

**请求**:
```http
DELETE /scans/{scan_id}
```

**路径参数**:
- `scan_id`: 扫描任务 ID（例如：scan_20260317_143022_123）

**响应**:
```json
{
  "status": "ok"
}
```

**错误响应**:

**404 - 扫描任务未找到**:
```
HTTP 404 Not Found
```

**示例**:
```bash
curl -X DELETE http://127.0.0.1:17289/scans/scan_20260317_143022_123
```

---

## 数据模型

### ScanRequest

扫描请求模型。

```typescript
interface ScanRequest {
  scannerId?: string;
  dpi: number;
  colorMode: "gray" | "color" | "bw";
  paperSize: "A4" | "A3" | "Letter" | "Legal";
  duplex?: boolean;
  autoFeed?: boolean;
}
```

### ScanResult

扫描结果模型。

```typescript
interface ScanResult {
  scan_id: string;
  status: "completed";
  images: ImageInfo[];
}
```

### ImageInfo

图像信息模型。

```typescript
interface ImageInfo {
  id: string;
  width: number;
  height: number;
}
```

### ScannerInfo

扫描仪信息模型。

```typescript
interface ScannerInfo {
  id: string;
  name: string;
  default: boolean;
}
```

### ErrorResponse

错误响应模型。

```typescript
interface ErrorResponse {
  error: string;
  message: string;
}
```

---

## 示例代码

### JavaScript / TypeScript

#### 健康检查

```typescript
async function checkHealth() {
  const response = await fetch('http://127.0.0.1:17289/ping');
  const data = await response.json();
  console.log('Agent status:', data.status);
  console.log('Version:', data.version);
}
```

#### 枚举扫描仪

```typescript
async function getScanners() {
  const response = await fetch('http://127.0.0.1:17289/scanners');
  const data = await response.json();
  console.log('Available scanners:', data.scanners);
  return data.scanners;
}
```

#### 执行扫描

```typescript
async function scanDocument() {
  const request = {
    dpi: 300,
    colorMode: 'gray',
    paperSize: 'A4',
    autoFeed: true
  };

  const response = await fetch('http://127.0.0.1:17289/scan', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(request)
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message);
  }

  const result = await response.json();
  console.log('Scan ID:', result.scan_id);
  console.log('Images:', result.images);
  return result;
}
```

#### 获取图像

```typescript
async function getImage(imageId: string): Promise<Blob> {
  const response = await fetch(`http://127.0.0.1:17289/files/${imageId}`);
  if (!response.ok) {
    throw new Error('Image not found');
  }
  return await response.blob();
}
```

#### 清理文件

```typescript
async function cleanupScan(scanId: string) {
  const response = await fetch(`http://127.0.0.1:17289/scans/${scanId}`, {
    method: 'DELETE'
  });
  const data = await response.json();
  console.log('Cleanup status:', data.status);
}
```

### Python

```python
import requests

BASE_URL = "http://127.0.0.1:17289"

def check_health():
    response = requests.get(f"{BASE_URL}/ping")
    return response.json()

def get_scanners():
    response = requests.get(f"{BASE_URL}/scanners")
    return response.json()

def scan_document():
    request = {
        "dpi": 300,
        "colorMode": "gray",
        "paperSize": "A4",
        "autoFeed": True
    }
    response = requests.post(f"{BASE_URL}/scan", json=request)
    return response.json()

def get_image(image_id):
    response = requests.get(f"{BASE_URL}/files/{image_id}")
    return response.content

def cleanup_scan(scan_id):
    response = requests.delete(f"{BASE_URL}/scans/{scan_id}")
    return response.json()
```

### C#

```csharp
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

var httpClient = new HttpClient();
var baseUrl = "http://127.0.0.1:17289";

// 健康检查
var healthResponse = await httpClient.GetAsync($"{baseUrl}/ping");
var healthData = await healthResponse.Content.ReadAsStringAsync();
Console.WriteLine(healthData);

// 枚举扫描仪
var scannersResponse = await httpClient.GetAsync($"{baseUrl}/scanners");
var scannersData = await scannersResponse.Content.ReadAsStringAsync();
Console.WriteLine(scannersData);

// 执行扫描
var scanRequest = new
{
    dpi = 300,
    colorMode = "gray",
    paperSize = "A4",
    autoFeed = true
};
var jsonRequest = JsonSerializer.Serialize(scanRequest);
var scanResponse = await httpClient.PostAsync($"{baseUrl}/scan",
    new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
var scanData = await scanResponse.Content.ReadAsStringAsync();
Console.WriteLine(scanData);
```

---

## 性能考虑

### 超时设置

- **扫描操作**: 默认 2 分钟超时
- **其他操作**: 默认 30 秒超时

建议客户端设置合理的超时时间：

```typescript
const response = await fetch('http://127.0.0.1:17289/scan', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(request),
  signal: AbortSignal.timeout(120000) // 2 分钟超时
});
```

### 并发限制

- 同一时间只能执行一个扫描任务
- 如果扫描仪忙碌，会返回 409 错误

建议客户端实现扫描队列：

```typescript
class ScanQueue {
  private queue: (() => Promise<void>)[] = [];
  private isScanning = false;

  async enqueue(scanFn: () => Promise<void>) {
    this.queue.push(scanFn);
    if (!this.isScanning) {
      this.process();
    }
  }

  private async process() {
    if (this.queue.length === 0) {
      this.isScanning = false;
      return;
    }

    this.isScanning = true;
    const scanFn = this.queue.shift()!;
    try {
      await scanFn();
    } finally {
      this.process();
    }
  }
}
```

---

## 安全建议

1. **添加认证**: 在生产环境中添加 API Key 或 Token 认证
2. **HTTPS**: 使用 HTTPS 加密传输
3. **输入验证**: 验证所有输入参数
4. **速率限制**: 实现速率限制防止滥用
5. **日志记录**: 记录所有 API 调用用于审计

---

## 版本历史

| 版本 | 日期 | 变更 |
|------|------|------|
| 1.0.0 | 2026-03-17 | 初始版本 |

---

**文档结束**
