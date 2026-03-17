@echo off
REM ScanAgent 前端构建脚本
REM 用于优化和构建前端资源

setlocal enabledelayedexpansion

echo ========================================
echo ScanAgent 前端构建脚本
echo ========================================
echo.

REM 配置变量
set FRONTEND_DIR=frontend
set OUTPUT_DIR=frontend\dist
set MINIFY_JS=true
set MINIFY_CSS=true

echo [1/5] 检查前端目录...
if not exist "%FRONTEND_DIR%" (
    echo 错误: 前端目录不存在: %FRONTEND_DIR%
    exit /b 1
)
echo 前端目录: %FRONTEND_DIR%
echo.

echo [2/5] 清理旧的构建...
if exist "%OUTPUT_DIR%" (
    echo 删除目录: %OUTPUT_DIR%
    rmdir /s /q "%OUTPUT_DIR%"
)
mkdir "%OUTPUT_DIR%"
echo.

echo [3/5] 复制文件到输出目录...
xcopy /e /i /y "%FRONTEND_DIR%\*" "%OUTPUT_DIR%"
echo.

echo [4/5] 跳过 HTML 压缩...
echo 注意: HTML 压缩可能破坏代码格式，已跳过
echo.

if "%MINIFY_JS%"=="true" (
    echo [5/5] 压缩 JavaScript 文件...
    echo 注意: 需要安装 Node.js 和 terser
    echo npm install -g terser
    echo.
    echo 压缩 scan.js...
    if exist "%OUTPUT_DIR%\scan.js" (
        terser "%OUTPUT_DIR%\scan.js" -c -m -o "%OUTPUT_DIR%\scan.min.js"
        del "%OUTPUT_DIR%\scan.js"
        ren "%OUTPUT_DIR%\scan.min.js" "scan.js"
        echo 已压缩: scan.js
    )
    echo.
) else (
    echo [5/5] 跳过 JavaScript 压缩...
    echo.
)

echo ========================================
echo 前端构建完成!
echo ========================================
echo.
echo 输出目录: %OUTPUT_DIR%
echo.
echo 构建产物:
dir /b "%OUTPUT_DIR%"
echo.

echo 下一步:
echo 1. 测试前端界面
echo 2. 部署到服务器
echo 3. 打包为安装包
echo.

endlocal
