# 扫描仪采集整合方案

创建日期：2026-03-15
版本：v1.0
状态：设计完成，待实施

---

## 📋 文档导航

本 SPEC 包含以下文档：

1. **[standalone-dev-guide.md](./standalone-dev-guide.md)** - Scan Agent 独立开发指南（给同事）
   - 从零搭建开发环境（Visual Studio）
   - API 接口契约（完整定义）
   - 核心代码实现指南
   - 打包发布与交付清单
   - 测试报告模板

2. **[design.md](./design.md)** - 架构设计文档
   - 背景与目标
   - 整体架构设计
   - 数据流设计
   - Scan Agent 设计
   - 前端集成设计
   - 后端集成设计
   - 部署与调试
   - 安全与权限
   - 风险与限制
   - 实施计划

2. **[implementation.md](./implementation.md)** - 实现细节文档
   - Scan Agent 核心代码示例
   - 前端 Hook 实现
   - 组件实现
   - 测试策略
   - 部署清单

3. **[tasks.md](./tasks.md)** - 任务清单
   - Phase 1: Scan Agent 开发（2 周）
   - Phase 2: 前端集成（1 周）
   - Phase 3: 联调与优化（1 周）
   - Phase 4: 文档与部署（3 天）

4. **[debug-guide.md](./debug-guide.md)** - 本机调试指南
   - 环境准备（.NET SDK 安装）
   - 三服务联调步骤
   - Mock 模式配置（无扫描仪时）
   - 常见问题排查
   - 快速测试脚本

5. **[collaboration-guide.md](./collaboration-guide.md)** - 协作开发指南
   - 任务拆分方案（Agent 独立开发）
   - 接口契约定义（API Contract）
   - 外包开发者交付清单
   - 整合步骤与验收标准
   - 风险控制与沟通协议

---

## 🎯 快速开始

### 阅读顺序建议

**Scan Agent 独立开发者（同事）**：
> 只需阅读一份文档，从环境搭建到交付全覆盖：
1. **[standalone-dev-guide.md](./standalone-dev-guide.md)** — 独立开发指南（唯一必读）
2. design.md §3（可选，了解架构背景）

**项目负责人（你自己）**：
1. design.md（完整阅读）
2. collaboration-guide.md（协作与整合流程）
3. tasks.md（任务跟踪）

**前端开发**：
1. design.md §4（前端集成设计）
2. implementation.md §2（前端实现细节）
3. debug-guide.md §5（Mock 模式）
4. tasks.md Phase 2

---

## 🏗️ 方案概览

### 核心思路

在现有 B/S 架构下，通过本地 **Scan Agent** 作为硬件隔离层，实现浏览器驱动扫描仪的能力。

### 架构图

```
PC 浏览器 (React)
    ↓ HTTP
Scan Agent (C# + TWAIN)
    ↓ HTTPS
后端 Python (Flask)
    ↓
OCR / 分类 / 抽取
```

### 关键特性

- ✅ 与手机拍照共用后端接口
- ✅ Agent 普通用户权限运行
- ✅ 数据全程内网流转
- ✅ 支持多种扫描参数配置
- ✅ 预览后再上传

---

## 📊 实施计划

| 阶段 | 工期 | 产出 |
|------|------|------|
| Phase 1: Scan Agent 开发 | 2 周 | 可独立运行的 Agent |
| Phase 2: 前端集成 | 1 周 | 完整 UI 交互 |
| Phase 3: 联调与优化 | 1 周 | 生产就绪 |
| Phase 4: 文档与部署 | 3 天 | 用户手册 + 安装包 |
| **总计** | **4.5 周** | **可交付系统** |

---

## 🔗 相关资料

- 原始调研文档：`MDFiles/Enhancement/BS_扫描与手机影像采集方案整理_v2.md`
- 现有手机拍照实现：`frontend/src/components/mobile-upload/MobilePhotoUpload.tsx`
- 后端上传接口：`backend/api/upload_routes.py`

---

## 📝 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|---------|
| 2026-03-15 | v1.0 | 初始版本，完成设计文档 |

---

**文档结束**
