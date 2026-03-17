@echo off
REM ScanAgent 一键部署脚本
REM 用于快速部署 ScanAgent 到本地或远程服务器

setlocal enabledelayedexpansion

echo ========================================
echo ScanAgent 一键部署脚本
echo ========================================
echo.

REM 配置变量
set VERSION=1.0.0
set DEPLOY_DIR=C:\ScanAgent
set PORT=17289

echo 部署配置:
echo 版本: %VERSION%
echo 部署目录: %DEPLOY_DIR%
echo 端口: %PORT%
echo.

REM 检查管理员权限
echo [1/7] 检查管理员权限...
net session >nul 2>&1
if errorlevel 1 (
    echo 警告: 未检测到管理员权限
    echo 某些步骤可能需要管理员权限（清理旧文件、配置防火墙）
    echo.
) else (
    echo 管理员权限: 已确认
    echo.
)
set HAS_ADMIN=%errorlevel%

REM 清理旧文件
echo [2/7] 清理旧文件...
if exist "%DEPLOY_DIR%" (
    echo 删除目录: %DEPLOY_DIR%
    if errorlevel 1 (
        echo 错误: 需要管理员权限删除旧文件
        echo 请手动删除 %DEPLOY_DIR% 后重试
        pause
        exit /b 1
    )
    rmdir /s /q "%DEPLOY_DIR%"
)
mkdir "%DEPLOY_DIR%"
echo.

REM 构建项目
echo [3/7] 构建项目...
cd scan-agent\ScanAgent
dotnet clean -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "..\..\%DEPLOY_DIR%"
if errorlevel 1 (
    echo 错误: 构建失败
    cd ..\..
    pause
    exit /b 1
)
cd ..\..
echo.

REM 复制前端文件
echo [4/7] 复制前端文件...
if not exist "%DEPLOY_DIR%\frontend" (
    mkdir "%DEPLOY_DIR%\frontend"
)
xcopy /e /i /y frontend "%DEPLOY_DIR%\frontend"
echo.

REM 复制文档文件
echo [5/7] 复制文档文件...
if not exist "%DEPLOY_DIR%\docs" (
    mkdir "%DEPLOY_DIR%\docs"
)
copy /y "README.md" "%DEPLOY_DIR%\docs\" >nul 2>&1
copy /y "USER-MANUAL.md" "%DEPLOY_DIR%\docs\" >nul 2>&1
copy /y "API-DOCUMENTATION.md" "%DEPLOY_DIR%\docs\" >nul 2>&1
echo.

REM 配置防火墙
echo [6/7] 配置防火墙...
if "%HAS_ADMIN%"=="0" (
    netsh advfirewall firewall show rule name="ScanAgent" >nul 2>&1
    if errorlevel 1 (
        echo 添加防火墙规则: ScanAgent
        netsh advfirewall firewall add rule name="ScanAgent" dir=in action=allow protocol=TCP localport=%PORT%
    ) else (
        echo 防火墙规则已存在
    )
    echo.
) else (
    echo 跳过防火墙配置（需要管理员权限）
    echo.
)

REM 创建启动脚本
echo [7/7] 创建启动脚本...
(
echo @echo off
echo echo Starting ScanAgent...
echo echo.
echo echo API 地址: http://127.0.0.1:%PORT%
echo echo Web 界面: 请在浏览器中打开 frontend\index.html
echo echo.
echo echo 按 Ctrl+C 停止服务
echo echo.
echo "%DEPLOY_DIR%\ScanAgent.exe"
) > "%DEPLOY_DIR%\start.bat"
echo.

echo ========================================
echo 部署完成!
echo ========================================
echo.
echo 以控制台模式运行
echo.
echo 访问地址:
echo API: http://127.0.0.1:%PORT%
echo Web 界面: 请在浏览器中打开 %DEPLOY_DIR%\frontend\index.html
echo.
echo 启动服务:
echo 双击: %DEPLOY_DIR%\start.bat
echo 或在命令行中运行: %DEPLOY_DIR%\ScanAgent.exe
echo.
echo 重要提示:
echo ScanAgent 使用 TWAIN 协议，需要在有桌面会话的用户进程中运行。
echo 不能作为 Windows Service 运行（Session 0 无桌面会话）。
echo 请确保以普通用户身份运行，不要使用系统服务。
echo.

echo 日志位置:
echo 临时文件: %TEMP%\ScanAgent
echo 日志输出: 控制台
echo.

echo 下一步:
echo 1. 双击 %DEPLOY_DIR%\start.bat 启动服务
echo 2. 测试 API: http://127.0.0.1:%PORT%/ping
echo 3. 打开 Web 界面
echo 4. 开始扫描
echo.

pause

endlocal
