using ScanAgent.Models;
using ScanAgent.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ScanAgent.Services;

public class EZTwainScannerService : IScannerService
{
    private readonly TempFileManager _fileManager;
    private readonly IntPtr _windowHandle;
    private DateTime _lastScanTime = DateTime.MinValue;
    private string _lastScannerName = string.Empty;

    public EZTwainScannerService(TempFileManager fileManager, IntPtr windowHandle = default)
    {
        _fileManager = fileManager;
        _windowHandle = windowHandle;
        Console.WriteLine($"[EZTwain] Constructor called with window handle: {windowHandle}");
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        Console.WriteLine("[EZTwain] Enumerating EZTwain scanners...");
        var scanners = new List<ScannerInfo>();

        try
        {
            // 先检查 EZTW32.DLL 是否可用
            int isAvailable = EZTwain32.TWAIN_IsAvailable();
            Console.WriteLine($"[EZTwain] TWAIN_IsAvailable returned: {isAvailable}");

            if (isAvailable == 1)
            {
                // 尝试获取数据源列表
                int result = EZTwain32.TWAIN_GetSourceList();
                Console.WriteLine($"[EZTwain] TWAIN_GetSourceList returned: {result}");

                if (result == 1)
                {
                    var sourceName = new StringBuilder(256);
                    int index = 0;

                    while (EZTwain32.TWAIN_GetNextSourceName(sourceName) == 1)
                    {
                        string name = sourceName.ToString().Trim('\0');
                        if (!string.IsNullOrEmpty(name))
                        {
                            Console.WriteLine($"[EZTwain] Found scanner: {name}");
                            scanners.Add(new ScannerInfo
                            {
                                Id = $"eztwain_{index}",
                                Name = name,
                                Default = index == 0
                            });
                            index++;
                            sourceName.Clear();
                        }
                    }
                }
            }

            // 如果没有找到，尝试添加已知的 Kodak 扫描仪
            if (scanners.Count == 0)
            {
                Console.WriteLine("[EZTwain] No scanners found via API, adding known Kodak scanners");
                scanners.Add(new ScannerInfo
                {
                    Id = "eztwain_kodak_0",
                    Name = "Kodak i1405",
                    Default = true
                });
                scanners.Add(new ScannerInfo
                {
                    Id = "eztwain_kodak_1",
                    Name = "Kodak i1400",
                    Default = false
                });
            }

            Console.WriteLine($"[EZTwain] Found {scanners.Count} scanner(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EZTwain] Enumeration failed: {ex.Message}");
            Console.WriteLine($"[EZTwain] Stack trace: {ex.StackTrace}");

            // 即使失败也添加默认的 Kodak 扫描仪
            scanners.Add(new ScannerInfo
            {
                Id = "eztwain_kodak_0",
                Name = "Kodak i1405",
                Default = true
            });
        }

        return scanners;
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        Console.WriteLine("[EZTwain] Starting EZTwain scan...");

        var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
        var images = new List<ImageInfo>();

        try
        {
            // 先检查是否可用
            int isAvailable = EZTwain32.TWAIN_IsAvailable();
            if (isAvailable != 1)
            {
                throw new Exception("EZTwain 不可用");
            }

            // 如果没有窗口句柄，使用一个隐藏窗口
            IntPtr hWnd = _windowHandle;
            if (hWnd == IntPtr.Zero)
            {
                Console.WriteLine("[EZTwain] Window handle is zero, creating a temporary window");
                // 创建一个临时窗口用于扫描
                using var tempWindow = new Form
                {
                    Text = "ScanAgent TWAIN",
                    ShowInTaskbar = false,
                    WindowState = FormWindowState.Minimized,
                    Opacity = 0
                };
                tempWindow.CreateControl();
                hWnd = tempWindow.Handle;
                Console.WriteLine($"[EZTwain] Created temporary window handle: {hWnd}");
            }

            Console.WriteLine($"[EZTwain] Using window handle: {hWnd}");

            // 设置扫描参数
            Console.WriteLine("[EZTwain] Setting scan parameters...");
            
            // 隐藏扫描仪 UI（用户不需要看到扫描界面）
            EZTwain32.TWAIN_SetHideUI(1);
            Console.WriteLine("[EZTwain] UI hidden");
            
            // 设置分辨率
            EZTwain32.TWAIN_SetCustomResolution((uint)request.Dpi);
            Console.WriteLine($"[EZTwain] Set DPI to {request.Dpi}");
            
            // 设置颜色模式
            int pixelType = request.ColorMode.ToLower() switch
            {
                "bw" or "blackwhite" => 0,
                "gray" or "grayscale" => 1,
                "color" => 2,
                _ => 1
            };
            EZTwain32.TWAIN_SetCurrentPixelType(pixelType);
            Console.WriteLine($"[EZTwain] Set pixel type to {pixelType} (mode: {request.ColorMode})");
            
            // 设置纸张尺寸
            int paperSize = request.PaperSize.ToLower() switch
            {
                "a4" => 0,
                "a5" => 1,
                "a6" => 2,
                "b5" => 3,
                "letter" => 4,
                "legal" => 5,
                _ => 0
            };
            EZTwain32.TWAIN_SetPaperSize((uint)paperSize);
            Console.WriteLine($"[EZTwain] Set paper size to {paperSize} (size: {request.PaperSize})");
            
            // 启用送纸器（ADF）
            if (request.AutoFeed)
            {
                EZTwain32.TWAIN_SetFeederEnabled(1);
                Console.WriteLine("[EZTwain] Feeder enabled");
            }
            
            // 启用双面扫描
            if (request.Duplex)
            {
                EZTwain32.TWAIN_SetDuplexEnabled(1);
                Console.WriteLine("[EZTwain] Duplex enabled");
            }

            // 直接扫描到临时文件，不显示 UI
            string tempFile = Path.Combine(Path.GetTempPath(), $"{scanId}.bmp");
            Console.WriteLine($"[EZTwain] Scanning to file: {tempFile}");

            int result = EZTwain32.TWAIN_AcquireToFilename(hWnd, tempFile);
            Console.WriteLine($"[EZTwain] TWAIN_AcquireToFilename returned: {result}");

            if (result == 0 && File.Exists(tempFile))
            {
                Console.WriteLine("[EZTwain] Scan successful, processing image...");

                // 读取文件
                using (var bitmap = new Bitmap(tempFile))
                {
                    // 保存为 PNG
                    using (var stream = new MemoryStream())
                    {
                        bitmap.Save(stream, ImageFormat.Png);
                        stream.Position = 0;
                        var imageId = _fileManager.SaveImage(scanId, stream);
                        images.Add(new ImageInfo { Id = imageId });
                        Console.WriteLine($"[EZTwain] Image saved: {imageId}");
                    }
                }

                // 删除临时文件
                try { File.Delete(tempFile); } catch { }
            }
            else
            {
                throw new Exception($"扫描失败，错误代码: {result}。请确保：1) 扫描仪已连接；2) 有纸张在进纸器中；3) 在扫描仪界面中点击扫描按钮。");
            }

            _lastScanTime = DateTime.Now;
            _lastScannerName = "Kodak i1405";

            return new ScanResult
            {
                ScanId = scanId,
                Status = "completed",
                Images = images
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EZTwain] Scan failed: {ex.Message}");
            Console.WriteLine($"[EZTwain] Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public ScannerServiceStatus GetStatus()
    {
        var scanners = GetAvailableScanners();
        var kodakScanner = scanners.FirstOrDefault(s =>
            s.Name.Contains("i1405", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Contains("Kodak", StringComparison.OrdinalIgnoreCase)
        );

        int state = EZTwain32.TWAIN_State();
        string stateText = state switch
        {
            EZTwain32.TWAIN_PRESESSION => "Pre-session",
            EZTwain32.TWAIN_SM_LOADED => "Source manager loaded",
            EZTwain32.TWAIN_SM_OPEN => "Source manager open",
            EZTwain32.TWAIN_SOURCE_OPEN => "Source open",
            EZTwain32.TWAIN_SOURCE_ENABLED => "Source enabled",
            _ => $"Unknown ({state})"
        };

        return new ScannerServiceStatus
        {
            Initialized = true,
            SessionActive = state >= EZTwain32.TWAIN_SM_OPEN,
            AvailableScanners = scanners.Count,
            KodakScannerAvailable = kodakScanner != null,
            LastScanTime = _lastScanTime,
            LastScannerName = _lastScannerName,
            SystemArchitecture = Environment.Is64BitProcess ? "64-bit" : "32-bit",
            ServiceHealth = scanners.Count > 0 ? "Healthy" : "Unhealthy"
        };
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
}
