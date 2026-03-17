# 扫描仪采集协作开发指南

创建日期：2026-03-15
更新日期：2026-03-17
版本：v1.1
适用对象：项目负责人（你自己）
说明：本文档是你的整合与管理视角，同事的开发指南见 [standalone-dev-guide.md](./standalone-dev-guide.md)

---

## 1. 任务拆分

### 1.1 两个独立任务

| | 任务 A：Scan Agent | 任务 B：前端集成 |
|---|---|---|
| 负责人 | 同事 | 你 |
| 技术栈 | C# .NET 6+ | React + TypeScript |
| 交付物 | ScanAgent.exe + 源码 + 测试报告 | ScannerUpload 组件 |
| 开发文档 | [standalone-dev-guide.md](./standalone-dev-guide.md) | implementation.md §2 |
| 可并行 | ✅ 通过 API 契约解耦 | ✅ Mock 模式开发 |

### 1.2 给同事的资料清单

只需要发给同事 **一个文件**：

- `standalone-dev-guide.md`

这份文档已包含：环境搭建、API 契约、代码示例、测试要求、交付清单。同事不需要接触你的主项目代码。

如果同事想了解更多架构背景，可以额外提供 `design.md`。

---

## 2. 接口契约摘要

完整契约定义在 `standalone-dev-guide.md` §3，这里只列摘要供你快速参考：

| 接口 | 方法 | 用途 |
|------|------|------|
| `/ping` | GET | 健康检查 |
| `/scanners` | GET | 枚举扫描仪 |
| `/scan` | POST | 执行扫描 |
| `/files/{image_id}` | GET | 获取扫描图像 |
| `/scans/{scan_id}` | DELETE | 清理临时文件 |

监听地址：`http://127.0.0.1:17289`

**接口变更流程**：同事提出变更 → 你评估前端影响 → 双方确认 → 各自调整代码。

---

## 3. 你的前端开发（Mock 模式）

在同事开发 Agent 期间，你可以用 Mock 模式并行开发前端：

1. 参考 `debug-guide.md` §5 配置 Mock
2. 完成 ScannerUpload 组件和相关 Hook
3. 集成到 WorkflowPage
4. 等 Agent 交付后联调

---

## 4. 整合步骤

### Step 1：验收 Agent

收到同事交付的 `ScanAgent.exe` 后：

```powershell
# 运行 Agent
.\ScanAgent.exe

# 逐个测试接口
curl http://127.0.0.1:17289/ping
curl http://127.0.0.1:17289/scanners
curl -X POST http://127.0.0.1:17289/scan -H "Content-Type: application/json" -d "{\"dpi\":300,\"color_mode\":\"gray\"}"
```

### Step 2：关闭前端 Mock

```typescript
// frontend/src/components/scanner-upload/hooks/useScannerAgent.ts
const ENABLE_MOCK = false;
```

### Step 3：三服务联调

```bash
# 终端 1: 后端
cd backend && python app.py

# 终端 2: 前端
cd frontend && npm run dev

# 终端 3: Agent
./ScanAgent.exe
```

### Step 4：端到端验证

- [ ] Agent 可正常启动
- [ ] 前端可检测到 Agent
- [ ] 可枚举扫描仪
- [ ] 可执行扫描并预览
- [ ] 可上传到后端
- [ ] 完整流程进入 OCR

---

## 5. 沟通协议

### 进度同步
建议每周同步一次，同事汇报 Agent 进度，你汇报前端进度。

### 问题反馈格式

**同事反馈问题时需提供**：
- 错误日志（控制台输出）
- 复现步骤
- 扫描仪品牌/型号

**你反馈问题时需提供**：
- 浏览器 Network 请求截图
- 前端控制台错误
- 预期行为 vs 实际行为

---

## 6. 风险控制

| 风险 | 缓解措施 |
|------|---------|
| 接口不匹配 | API 契约已在 standalone-dev-guide.md 中明确定义 |
| 进度延期 | 前端用 Mock 并行开发，预留 1 周整合缓冲 |
| 质量问题 | 要求提供测试报告（模板已在 standalone-dev-guide.md §8.2） |

---

**文档结束**
