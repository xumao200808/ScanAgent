@echo off
REM Scan Agent 兼容性测试脚本
REM 用于测试不同环境下的兼容性

echo ========================================
echo Scan Agent 兼容性测试
echo ========================================
echo.

REM 设置变量
set AGENT_URL=http://127.0.0.1:17289
set TEST_DIR=%TEMP%\ScanAgent_Compatibility_Test
set TEST_OUTPUT=%TEST_DIR%\compatibility_results.txt

REM 创建测试目录
if not exist "%TEST_DIR%" mkdir "%TEST_DIR%"
echo 测试目录: %TEST_DIR%
echo 测试输出: %TEST_OUTPUT%
echo.

REM 清空测试输出文件
echo Scan Agent 兼容性测试报告 > "%TEST_OUTPUT%"
echo 测试日期: %date% %time% >> "%TEST_OUTPUT%"
echo ======================================== >> "%TEST_OUTPUT%"
echo. >> "%TEST_OUTPUT%"

REM 获取系统信息
echo [系统信息] 正在收集系统信息...
echo. >> "%TEST_OUTPUT%"
echo === 系统信息 === >> "%TEST_OUTPUT%"
ver >> "%TEST_OUTPUT%"
echo. >> "%TEST_OUTPUT%"
dotnet --info >> "%TEST_OUTPUT%"
echo. >> "%TEST_OUTPUT%"
echo [系统信息] 完成
echo.

REM 测试 1: .NET 版本检查
echo [测试 1] .NET 版本检查...
echo. >> "%TEST_OUTPUT%"
echo === .NET 版本检查 === >> "%TEST_OUTPUT%"
dotnet --list-runtimes >> "%TEST_OUTPUT%"
if %ERRORLEVEL% EQU 0 (
    echo ✓ .NET 运行时已安装
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ .NET 运行时未安装
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试 2: 操作系统版本检查
echo [测试 2] 操作系统版本检查...
echo. >> "%TEST_OUTPUT%"
echo === 操作系统版本检查 === >> "%TEST_OUTPUT%"
systeminfo | findstr /B /C:"OS Name" /C:"OS Version" >> "%TEST_OUTPUT%"
echo 状态: 通过 >> "%TEST_OUTPUT%"
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试 3: Agent 健康检查
echo [测试 3] Agent 健康检查...
echo. >> "%TEST_OUTPUT%"
echo === Agent 健康检查 === >> "%TEST_OUTPUT%"
curl -s %AGENT_URL%/ping > "%TEST_DIR%\ping_result.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ Agent 运行正常
    type "%TEST_DIR%\ping_result.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ Agent 未运行，请先启动 ScanAgent.exe
    echo 状态: 失败 - Agent 未运行 >> "%TEST_OUTPUT%"
    echo.
    echo 请先启动 ScanAgent.exe:
    echo   cd scan-agent/ScanAgent
    echo   dotnet run --urls "http://127.0.0.1:17289"
    echo.
    pause
    exit /b 1
)
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试 4: 扫描仪枚举测试
echo [测试 4] 扫描仪枚举测试...
echo. >> "%TEST_OUTPUT%"
echo === 扫描仪枚举测试 === >> "%TEST_OUTPUT%"
curl -s %AGENT_URL%/scanners > "%TEST_DIR%\scanners_result.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 扫描仪枚举成功
    type "%TEST_DIR%\scanners_result.json" >> "%TEST_OUTPUT%"
    
    REM 检查是否有扫描仪
    findstr /C:"\"scanners\"" "%TEST_DIR%\scanners_result.json" > nul
    if %ERRORLEVEL% EQU 0 (
        echo 状态: 通过 >> "%TEST_OUTPUT%"
    ) else (
        echo ⚠ 未找到扫描仪，请检查扫描仪连接
        echo 状态: 警告 - 未找到扫描仪 >> "%TEST_OUTPUT%"
    )
) else (
    echo ✗ 扫描仪枚举失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试 5: 不同 DPI 测试
echo [测试 5] 不同 DPI 测试...
echo. >> "%TEST_OUTPUT%"
echo === 不同 DPI 测试 === >> "%TEST_OUTPUT%"

REM 150 DPI 测试
echo 测试 150 DPI 扫描...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":150,\"color_mode\":\"gray\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_150dpi.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 150 DPI 扫描成功
    type "%TEST_DIR%\scan_150dpi.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ 150 DPI 扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"

REM 300 DPI 测试
echo 测试 300 DPI 扫描...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"gray\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_300dpi.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 300 DPI 扫描成功
    type "%TEST_DIR%\scan_300dpi.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ 300 DPI 扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"

REM 600 DPI 测试
echo 测试 600 DPI 扫描...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":600,\"color_mode\":\"gray\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_600dpi.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 600 DPI 扫描成功
    type "%TEST_DIR%\scan_600dpi.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ 600 DPI 扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试 6: 不同颜色模式测试
echo [测试 6] 不同颜色模式测试...
echo. >> "%TEST_OUTPUT%"
echo === 不同颜色模式测试 === >> "%TEST_OUTPUT%"

REM 灰度模式测试
echo 测试灰度模式...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"gray\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_gray.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 灰度模式扫描成功
    type "%TEST_DIR%\scan_gray.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ 灰度模式扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"

REM 彩色模式测试
echo 测试彩色模式...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"color\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_color.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 彩色模式扫描成功
    type "%TEST_DIR%\scan_color.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ 彩色模式扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"

REM 黑白模式测试
echo 测试黑白模式...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"bw\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_bw.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 黑白模式扫描成功
    type "%TEST_DIR%\scan_bw.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ 黑白模式扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试 7: 不同纸张尺寸测试
echo [测试 7] 不同纸张尺寸测试...
echo. >> "%TEST_OUTPUT%"
echo === 不同纸张尺寸测试 === >> "%TEST_OUTPUT%"

REM A4 纸张测试
echo 测试 A4 纸张...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"gray\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_a4.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ A4 纸张扫描成功
    type "%TEST_DIR%\scan_a4.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ A4 纸张扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"

REM Letter 纸张测试
echo 测试 Letter 纸张...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"gray\",\"paper_size\":\"Letter\"}" ^
  > "%TEST_DIR%\scan_letter.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ Letter 纸张扫描成功
    type "%TEST_DIR%\scan_letter.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ✗ Letter 纸张扫描失败
    echo 状态: 失败 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试 8: 错误处理测试
echo [测试 8] 错误处理测试...
echo. >> "%TEST_OUTPUT%"
echo === 错误处理测试 === >> "%TEST_OUTPUT%"

REM 测试无效扫描仪 ID
echo 测试无效扫描仪 ID...
curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"gray\",\"paper_size\":\"A4\",\"scanner_id\":\"invalid_scanner\"}" ^
  > "%TEST_DIR%\scan_invalid.json"
if %ERRORLEVEL% NEQ 0 (
    echo ✓ 无效扫描仪 ID 正确返回错误
    type "%TEST_DIR%\scan_invalid.json" >> "%TEST_OUTPUT%"
    echo 状态: 通过 >> "%TEST_OUTPUT%"
) else (
    echo ⚠ 无效扫描仪 ID 未返回错误
    echo 状态: 警告 >> "%TEST_OUTPUT%"
)
echo. >> "%TEST_OUTPUT%"
echo.

REM 测试总结
echo ========================================
echo 测试完成
echo ========================================
echo.
echo 测试结果已保存到: %TEST_OUTPUT%
echo 详细日志保存在: %TEST_DIR%
echo.

REM 显示测试结果摘要
echo === 测试结果摘要 ===
type "%TEST_OUTPUT%"
echo.

REM 打开测试结果目录
explorer "%TEST_DIR%"

pause
