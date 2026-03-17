# ScanAgent v1.0.0 Release Notes

**发布日期**: 2026-03-17

---

## 🎉 首次发布

ScanAgent v1.0.0 是首个正式版本，提供了完整的扫描仪采集功能，包括 TWAIN 协议支持、HTTP API、Web 界面等。

---

## ✨ 主要功能

### 核心功能

- **TWAIN 扫描仪支持**: 支持所有 TWAIN 兼容的扫描仪
- **HTTP API**: 提供 RESTful API 接口，方便第三方应用集成
- **Web 界面**: 友好的 Web 界面，支持扫描参数配置和实时预览
- **多页扫描**: 支持多页文档扫描
- **双面扫描**: 支持双面扫描功能
- **自动文件管理**: 自动管理临时文件，定期清理过期文件

### 扫描参数

- **DPI 设置**: 支持自定义 DPI（默认 300）
- **颜色模式**: 支持黑白、灰度、彩色
- **纸张尺寸**: 支持 A4、A3、Letter、Legal 等常用纸张尺寸
- **双面扫描**: 支持双面扫描功能
- **自动进纸**: 支持自动进纸功能

### API 接口

- `GET /ping`: 健康检查
- `GET /scanners`: 枚举可用扫描仪
- `POST /scan`: 执行扫描操作
- `GET /files/{imageId}`: 下载扫描图像
- `DELETE /scans/{scanId}`: 删除扫描结果

---

## 🚀 性能优化

- **文件 I/O 优化**: 使用 80KB 缓冲区，提高文件读写性能
- **扫描仪缓存**: 缓存扫描仪列表 5 秒，减少硬件枚举开销
- **并发控制**: 同一时间只允许一个扫描任务，避免资源冲突
- **内存管理**: 及时释放 Blob URL，防止内存泄漏
- **自动清理**: 每小时自动清理 24 小时前的扫描文件

---

## 🛡️ 安全增强

- **XSS 防护**: 前端使用 textContent 替代 innerHTML，防止 XSS 攻击
- **输入验证**: 所有 API 输入参数都经过验证
- **错误处理**: 不暴露敏感信息，提供友好的错误消息
- **CORS 配置**: 限制跨域来源，只允许受信任的域名

---

## 🔧 技术栈

### 后端

- **.NET 6.0**: 现代化的跨平台运行时
- **ASP.NET Core 6.0**: 高性能的 Web 框架
- **NTwain 3.7.5**: TWAIN 协议库
- **C# 10.0**: 最新的 C# 语言特性

### 前端

- **HTML5**: 现代化的页面结构
- **CSS3**: 响应式样式设计
- **JavaScript ES6+**: 现代化的交互逻辑
- **Fetch API**: 原生的 HTTP 请求

---

## 📦 安装方式

### 方式一：使用安装程序（推荐）

1. 从 GitHub Releases 下载 `ScanAgent-1.0.0-Setup.exe`
2. 双击运行安装程序，按照提示完成安装
3. 安装完成后，ScanAgent 会自动启动
4. 系统托盘中会出现扫描仪图标
5. 双击托盘图标可打开前端界面，或直接访问 http://127.0.0.1:17289/frontend/index.html

### 方式二：使用部署脚本

1. 下载 ZIP 包并解压
2. 右键点击 `deploy.bat`，选择"以管理员身份运行"
3. 等待部署完成
4. 服务将自动启动

### 方式三：从源码构建

```bash
# 克隆仓库
git clone https://github.com/flashday/ScanAgent.git
cd ScanAgent

# 构建项目
dotnet build -c Release

# 运行项目
dotnet run --project ScanAgent
```

---

## 📖 文档

- **[用户手册](USER-MANUAL.md)**: 详细的安装和使用说明
- **[API 文档](API-DOCUMENTATION.md)**: 完整的 API 接口文档
- **[架构设计](ARCHITECTURE-DESIGN.md)**: 系统架构和设计说明
- **[调试指南](DEBUG-GUIDE.md)**: 调试和故障排除指南
- **[扩展指南](EXTENSION-GUIDE.md)**: 如何扩展 ScanAgent 功能

---

## 🐛 已知问题

1. **WIA 扫描仪支持**: 当前版本主要支持 TWAIN 协议，WIA 支持有限
2. **网络扫描仪**: 部分网络扫描仪可能需要额外配置
3. **扫描仪驱动**: 需要安装正确的扫描仪驱动程序

---

## 🔄 升级说明

这是首个版本，无需升级。

---

## 📝 变更日志

### 新增

- 初始版本发布
- TWAIN 扫描仪支持
- HTTP API 接口
- Web 用户界面
- 文件管理功能
- 自动清理功能

---

## 💡 使用示例

### 扫描文档

```bash
curl -X POST http://127.0.0.1:17289/scan \
  -H "Content-Type: application/json" \
  -d '{
    "dpi": 300,
    "colorMode": "gray",
    "paperSize": "A4"
  }'
```

### 枚举扫描仪

```bash
curl http://127.0.0.1:17289/scanners
```

### 下载图像

```bash
curl http://127.0.0.1:17289/files/img_001 --output image.png
```

---

## 🤝 贡献

欢迎贡献代码、报告问题或提出建议！

- **GitHub**: https://github.com/flashday/ScanAgent
- **Issues**: https://github.com/flashday/ScanAgent/issues
- **Pull Requests**: https://github.com/flashday/ScanAgent/pulls

---

## 📄 许可证

本项目采用 MIT 许可证。详见 [LICENSE](LICENSE) 文件。

---

## 📞 技术支持

如果您遇到问题或有任何疑问，请通过以下方式联系我们：

- **GitHub Issues**: https://github.com/flashday/ScanAgent/issues
- **Email**: support@example.com

---

## 🙏 致谢

感谢所有为 ScanAgent 做出贡献的开发者和用户！

---

**下载链接**: [ScanAgent-1.0.0-windows-x64.zip](https://github.com/flashday/ScanAgent/releases/download/v1.0.0/ScanAgent-1.0.0-windows-x64.zip)

**完整文档**: [https://github.com/flashday/ScanAgent](https://github.com/flashday/ScanAgent)
