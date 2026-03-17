@echo off
REM ScanAgent 打包脚本
REM 用于将 ScanAgent 打包为可分发的安装包

setlocal enabledelayedexpansion

REM 配置变量
set VERSION=1.0.0
set BUILD_CONFIG=Release
set OUTPUT_DIR=dist
set PACKAGE_NAME=ScanAgent-%VERSION%-windows-x64

echo ========================================
echo ScanAgent 打包脚本
echo 版本: %VERSION%
echo 配置: %BUILD_CONFIG%
echo ========================================
echo.

REM 检查 .NET SDK
echo [1/6] 检查 .NET SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo 错误: 未找到 .NET SDK
    echo 请先安装 .NET 6.0 SDK: https://dotnet.microsoft.com/download/dotnet/6.0
    exit /b 1
)
echo .NET SDK 版本:
dotnet --version
echo.

REM 清理旧的构建
echo [2/6] 清理旧的构建...
if exist "%OUTPUT_DIR%" (
    echo 删除目录: %OUTPUT_DIR%
    rmdir /s /q "%OUTPUT_DIR%"
)
echo.

REM 构建项目
echo [3/6] 构建项目...
cd scan-agent\ScanAgent
dotnet clean -c %BUILD_CONFIG%
dotnet build -c %BUILD_CONFIG% -r win-x64
if errorlevel 1 (
    echo 错误: 构建失败
    exit /b 1
)
echo.

REM 发布项目
echo [4/6] 发布项目...
dotnet publish -c %BUILD_CONFIG% -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "..\..\%OUTPUT_DIR%\%PACKAGE_NAME%"
if errorlevel 1 (
    echo 错误: 发布失败
    exit /b 1
)
cd ..\..
echo.

REM 复制前端文件
echo [5/6] 复制前端文件...
if not exist "%OUTPUT_DIR%\%PACKAGE_NAME%\frontend" (
    mkdir "%OUTPUT_DIR%\%PACKAGE_NAME%\frontend"
)
xcopy /e /i /y frontend "%OUTPUT_DIR%\%PACKAGE_NAME%\frontend"
if errorlevel 1 (
    echo 警告: 前端文件复制失败
)
echo.

REM 复制文档文件
echo [6/6] 复制文档文件...
if not exist "%OUTPUT_DIR%\%PACKAGE_NAME%\docs" (
    mkdir "%OUTPUT_DIR%\%PACKAGE_NAME%\docs"
)
copy /y "README.md" "%OUTPUT_DIR%\%PACKAGE_NAME%\docs\" >nul 2>&1
copy /y "USER-MANUAL.md" "%OUTPUT_DIR%\%PACKAGE_NAME%\docs\" >nul 2>&1
copy /y "API-DOCUMENTATION.md" "%OUTPUT_DIR%\%PACKAGE_NAME%\docs\" >nul 2>&1
copy /y "ARCHITECTURE-DESIGN.md" "%OUTPUT_DIR%\%PACKAGE_NAME%\docs\" >nul 2>&1
copy /y "DEBUG-GUIDE.md" "%OUTPUT_DIR%\%PACKAGE_NAME%\docs\" >nul 2>&1
copy /y "EXTENSION-GUIDE.md" "%OUTPUT_DIR%\%PACKAGE_NAME%\docs\" >nul 2>&1
copy /y "CHANGELOG.md" "%OUTPUT_DIR%\%PACKAGE_NAME%\docs\" >nul 2>&1
echo.

REM 创建启动脚本
echo [额外] 创建启动脚本...
(
echo @echo off
echo echo Starting ScanAgent...
echo echo.
echo echo API 地址: http://127.0.0.1:17289
echo echo Web 界面: 请在浏览器中打开 frontend\index.html
echo echo.
echo echo 按 Ctrl+C 停止服务
echo echo.
echo ScanAgent.exe
) > "%OUTPUT_DIR%\%PACKAGE_NAME%\start.bat"
echo.

REM 创建安装说明
echo [额外] 创建安装说明...
(
echo ScanAgent 安装包
echo ====================
echo.
echo 版本: %VERSION%
echo 平台: Windows x64
echo.
echo 快速开始:
echo --------
echo 1. 双击 start.bat 启动服务
echo 2. 在浏览器中打开 frontend\index.html
echo 3. 开始扫描
echo.
echo 详细说明:
echo --------
echo 请查看 docs 目录下的文档:
echo - README.md: 项目概述
echo - USER-MANUAL.md: 用户手册
echo - API-DOCUMENTATION.md: API 接口文档
echo.
echo 系统要求:
echo --------
echo - Windows 10 或更高版本
echo - .NET 6.0 Runtime (已包含在安装包中)
echo - TWAIN 兼容的扫描仪
echo.
echo 技术支持:
echo --------
echo GitHub: https://github.com/flashday/ScanAgent
echo Issues: https://github.com/flashday/ScanAgent/issues
) > "%OUTPUT_DIR%\%PACKAGE_NAME%\INSTALL.txt"
echo.

REM 创建 ZIP 包
echo [额外] 创建 ZIP 包...
cd "%OUTPUT_DIR%"
powershell -Command "Compress-Archive -Path '%PACKAGE_NAME%' -DestinationPath '%PACKAGE_NAME%.zip' -Force"
cd ..
echo.

echo ========================================
echo 打包完成!
echo ========================================
echo.
echo 输出目录: %OUTPUT_DIR%
echo 安装包: %OUTPUT_DIR%\%PACKAGE_NAME%.zip
echo.
echo 文件列表:
dir /b "%OUTPUT_DIR%\%PACKAGE_NAME%"
echo.
echo 下一步:
echo 1. 测试安装包
echo 2. 上传到 GitHub Releases
echo 3. 通知用户下载
echo.

endlocal
