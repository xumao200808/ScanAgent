# ScanAgent 前端

ScanAgent 前端是一个简洁的扫描界面，提供友好的用户体验和实时扫描状态显示。

## 功能特性

- **实时状态显示**: 显示 ScanAgent 服务在线/离线状态
- **扫描仪枚举**: 自动枚举并列出所有可用的扫描仪
- **扫描参数配置**: 支持配置 DPI、颜色模式、纸张尺寸等参数
- **实时预览**: 扫描完成后实时显示预览图像
- **图像管理**: 支持查看、下载、删除扫描图像
- **键盘快捷键**: 支持快捷键操作，提高效率
- **错误提示**: 友好的错误提示和故障排除建议

## 技术栈

- **HTML5**: 现代化的页面结构
- **CSS3**: 响应式样式设计
- **JavaScript ES6+**: 现代化的交互逻辑
- **Fetch API**: 原生的 HTTP 请求

## 文件结构

```
frontend/
├── index.html          # 主界面文件
├── public/             # 静态资源
│   ├── favicon.svg
│   └── icons.svg
└── src/               # 源代码（未使用，保留用于扩展）
    ├── assets/
    ├── App.tsx
    ├── App.css
    ├── main.tsx
    └── index.css
```

## 快速开始

### 1. 启动后端服务

确保 ScanAgent 后端服务正在运行：

```bash
# Windows
cd scan-agent/ScanAgent
dotnet run --urls "http://127.0.0.1:17289"
```

看到系统托盘中出现扫描仪图标，并弹出气泡提示："ScanAgent 已启动，API: http://127.0.0.1:17289"。

### 2. 打开前端界面

在浏览器中打开：

```
http://127.0.0.1:17289/frontend/index.html
```

或者双击系统托盘图标，自动打开前端界面。

### 3. 使用扫描功能

1. **选择扫描仪**: 从下拉列表中选择要使用的扫描仪
2. **配置参数**: 设置 DPI、颜色模式、纸张尺寸等参数
3. **开始扫描**: 点击"开始扫描"按钮
4. **查看结果**: 扫描完成后，图像会显示在预览区域
5. **下载图像**: 点击图像可以下载或删除

## 键盘快捷键

| 快捷键 | 功能 |
|---------|------|
| Esc | 关闭当前对话框 |
| Ctrl+Enter | 开始扫描 |
| Ctrl+R | 重新加载扫描仪列表 |
| Ctrl+U | 上传图像（演示模式） |

## 扫描参数

### DPI 设置

- **75**: 低质量，快速扫描
- **150**: 中等质量
- **300**: 高质量（推荐）
- **600**: 超高质量，扫描较慢

### 颜色模式

- **color**: 彩色扫描
- **gray**: 灰度扫描
- **black**: 黑白扫描

### 纸张尺寸

- **A4**: A4 纸张（210mm × 297mm）
- **A3**: A3 纸张（297mm × 420mm）
- **Letter**: Letter 纸张（8.5in × 11in）
- **Legal**: Legal 纸张（8.5in × 14in）

### 其他参数

- **双面扫描**: 支持双面文档扫描
- **自动进纸**: 支持自动进纸功能

## API 集成

前端通过 Fetch API 与后端通信：

### 健康检查

```javascript
fetch('http://127.0.0.1:17289/ping')
  .then(response => response.json())
  .then(data => console.log(data));
```

### 枚举扫描仪

```javascript
fetch('http://127.0.0.1:17289/scanners')
  .then(response => response.json())
  .then(data => console.log(data.scanners));
```

### 执行扫描

```javascript
fetch('http://127.0.0.1:17289/scan', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    scannerId: 'scanner_0',
    dpi: 300,
    colorMode: 'gray',
    paperSize: 'A4',
    duplex: false,
    autoFeed: true
  })
})
.then(response => response.json())
.then(data => console.log(data));
```

### 下载图像

```javascript
fetch(`http://127.0.0.1:17289/files/${imageId}`)
  .then(response => response.blob())
  .then(blob => {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'scan.png';
    a.click();
  });
```

## 浏览器兼容性

- **Chrome 90+**: ✅ 完全支持
- **Edge 90+**: ✅ 完全支持
- **Firefox 88+**: ✅ 完全支持
- **Safari 14+**: ✅ 完全支持

## 开发

### 本地开发

1. 启动后端服务：

```bash
cd scan-agent/ScanAgent
dotnet run --urls "http://127.0.0.1:17289"
```

2. 在浏览器中打开 `index.html`：

```
http://127.0.0.1:17289/frontend/index.html
```

3. 修改代码后刷新浏览器即可看到效果

### 构建优化

前端已经过优化，无需额外构建步骤。如果需要进一步优化，可以使用：

```bash
# 使用 build-frontend.bat 脚本
build-frontend.bat
```

## 故障排除

### 问题 1: 无法连接到 ScanAgent

**症状**: 前端显示"离线"状态

**解决方案**:
1. 检查 ScanAgent 后端是否正在运行
2. 检查防火墙是否阻止了 17289 端口
3. 检查浏览器控制台是否有错误信息

### 问题 2: 扫描失败

**症状**: 点击"开始扫描"后显示错误

**解决方案**:
1. 检查扫描仪是否正确连接
2. 检查扫描仪驱动是否已安装
3. 查看浏览器控制台的详细错误信息
4. 参考 [调试指南](../scan-agent/docs/DEBUG-GUIDE.md)

### 问题 3: 图像无法显示

**症状**: 扫描成功但图像不显示

**解决方案**:
1. 检查浏览器是否支持 PNG 格式
2. 检查网络连接
3. 查看浏览器控制台的错误信息

## 扩展开发

如果需要扩展前端功能，可以参考：

- [扩展开发指南](../scan-agent/docs/EXTENSION-GUIDE.md)
- [API 文档](../scan-agent/docs/API-DOCUMENTATION.md)

## 许可证

与主项目保持一致。

## 贡献

欢迎贡献代码、报告问题或提出建议！

- **GitHub**: https://github.com/flashday/ScanAgent
- **Issues**: https://github.com/flashday/ScanAgent/issues
