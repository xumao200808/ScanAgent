@echo off
REM Scan Agent 端到端测试脚本
REM 用于测试完整的扫描流程：启动 Agent → 扫描 → 上传 → OCR

echo ========================================
echo Scan Agent 端到端测试
echo ========================================
echo.

REM 设置变量
set AGENT_URL=http://127.0.0.1:17289
set BACKEND_URL=http://localhost:5000
set TEST_DIR=%TEMP%\ScanAgent_E2E_Test
set TEST_OUTPUT=%TEST_DIR%\test_results.txt

REM 创建测试目录
if not exist "%TEST_DIR%" mkdir "%TEST_DIR%"
echo 测试目录: %TEST_DIR%
echo.

REM 测试 1: Agent 健康检查
echo [测试 1] Agent 健康检查...
curl -s %AGENT_URL%/ping > "%TEST_DIR%\ping_result.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ Agent 运行正常
    type "%TEST_DIR%\ping_result.json"
) else (
    echo ✗ Agent 未运行，请先启动 ScanAgent.exe
    pause
    exit /b 1
)
echo.

REM 测试 2: 枚举扫描仪
echo [测试 2] 枚举扫描仪...
curl -s %AGENT_URL%/scanners > "%TEST_DIR%\scanners_result.json"
if %ERRORLEVEL% EQU 0 (
    echo ✓ 扫描仪枚举成功
    type "%TEST_DIR%\scanners_result.json"
) else (
    echo ✗ 扫描仪枚举失败
)
echo.

REM 测试 3: 执行扫描
echo [测试 3] 执行扫描...
echo 注意: 此步骤需要真实扫描仪，请确保扫描仪已连接并开机
echo 请在扫描仪上放置测试文档，然后按任意键继续...
pause > nul

curl -s -X POST %AGENT_URL%/scan ^
  -H "Content-Type: application/json" ^
  -d "{\"dpi\":300,\"color_mode\":\"gray\",\"paper_size\":\"A4\"}" ^
  > "%TEST_DIR%\scan_result.json"

if %ERRORLEVEL% EQU 0 (
    echo ✓ 扫描请求成功
    type "%TEST_DIR%\scan_result.json"
    
    REM 提取 scan_id
    for /f "tokens=2 delims=:\"" %%a in ('findstr "scan_id" "%TEST_DIR%\scan_result.json') do set SCAN_ID=%%a
    set SCAN_ID=%SCAN_ID:,=%
    echo.
    echo Scan ID: %SCAN_ID%
) else (
    echo ✗ 扫描请求失败
    pause
    exit /b 1
)
echo.

REM 测试 4: 获取扫描图像
echo [测试 4] 获取扫描图像...
if defined SCAN_ID (
    REM 解析 scan_result.json 获取图像 ID 列表
    REM 这里简化处理，假设只有一个图像
    
    echo 正在下载扫描图像...
    curl -s %AGENT_URL%/files/img_001 > "%TEST_DIR%\test_scan.png"
    
    if %ERRORLEVEL% EQU 0 (
        echo ✓ 图像下载成功
        echo 保存位置: %TEST_DIR%\test_scan.png
        
        REM 检查文件大小
        for %%A in ("%TEST_DIR%\test_scan.png") do set FILE_SIZE=%%~zA
        echo 文件大小: %FILE_SIZE% 字节
        
        if %FILE_SIZE% GTR 0 (
            echo ✓ 图像文件有效
        ) else (
            echo ✗ 图像文件为空
        )
    ) else (
        echo ✗ 图像下载失败
    )
) else (
    echo ✗ 未获取到有效的 Scan ID
)
echo.

REM 测试 5: 清理临时文件
echo [测试 5] 清理临时文件...
if defined SCAN_ID (
    curl -s -X DELETE %AGENT_URL%/scans/%SCAN_ID% > "%TEST_DIR%\cleanup_result.json"
    
    if %ERRORLEVEL% EQU 0 (
        echo ✓ 清理成功
        type "%TEST_DIR%\cleanup_result.json"
    ) else (
        echo ✗ 清理失败
    )
) else (
    echo ✗ 未获取到有效的 Scan ID
)
echo.

REM 测试 6: 后端上传测试（可选）
echo [测试 6] 后端上传测试...
echo 注意: 此步骤需要后端服务运行在 %BACKEND_URL%
echo 如果后端未运行，此测试将失败
echo.

if exist "%TEST_DIR%\test_scan.png" (
    echo 正在上传图像到后端...
    curl -s -X POST %BACKEND_URL%/api/convert ^
      -F "files=@%TEST_DIR%\test_scan.png" ^
      > "%TEST_DIR%\upload_result.json"
    
    if %ERRORLEVEL% EQU 0 (
        echo ✓ 上传成功
        type "%TEST_DIR%\upload_result.json"
    ) else (
        echo ✗ 上传失败（可能是后端未运行）
    )
) else (
    echo ✗ 测试图像不存在，跳过上传测试
)
echo.

REM 测试总结
echo ========================================
echo 测试完成
echo ========================================
echo.
echo 测试结果保存在: %TEST_DIR%
echo.
echo 测试文件列表:
dir /b "%TEST_DIR%"
echo.

REM 询问是否清理测试文件
echo 是否清理测试文件? (Y/N)
set /p CLEAN=
if /i "%CLEAN%"=="Y" (
    echo 正在清理测试文件...
    rmdir /s /q "%TEST_DIR%"
    echo ✓ 测试文件已清理
)

echo.
echo 按任意键退出...
pause > nul
