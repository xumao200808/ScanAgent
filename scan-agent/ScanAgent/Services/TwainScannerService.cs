using NTwain;
using NTwain.Data;
using ScanAgent.Models;
using ScanAgent.Utils;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;

namespace ScanAgent.Services;

public class TwainScannerService : IScannerService
{
    private TwainSession? _session;
    private readonly TempFileManager _fileManager;
    private bool _initialized = false;
    private int _retryCount = 3;
    private TimeSpan _retryDelay = TimeSpan.FromSeconds(2);
    private DateTime _lastScanTime = DateTime.MinValue;
    private string _lastScannerName = string.Empty;

    public TwainScannerService(TempFileManager fileManager)
    {
        _fileManager = fileManager;
        InitializeSession();
    }

    private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ScanAgent.log");

    private static void Log(string message)
    {
        try
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(LogFile, logMessage + Environment.NewLine);
        }
        catch { }
    }

    private void InitializeSession()
    {
        try
        {
            Log("[TWAIN] Starting initialization...");
            
            // 检查系统架构
            Log($"[TWAIN] System architecture: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
            
            // 检查 TWAIN 目录
            string twain32Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "twain_32");
            string twain64Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "twain_64");
            Log($"[TWAIN] TWAIN 32 path: {twain32Path} - {(Directory.Exists(twain32Path) ? "Exists" : "Not found")}");
            Log($"[TWAIN] TWAIN 64 path: {twain64Path} - {(Directory.Exists(twain64Path) ? "Exists" : "Not found")}");
            
            // 检查 TWAIN 驱动
            if (Directory.Exists(twain32Path))
            {
                var driverFolders = Directory.GetDirectories(twain32Path);
                Log($"[TWAIN] Found {driverFolders.Length} TWAIN drivers in twain_32:");
                foreach (var folder in driverFolders)
                {
                    Log($"[TWAIN] - {Path.GetFileName(folder)}");
                }
            }
            
            // 检查 Kodak i1400 驱动
            CheckKodakDriver(twain32Path, "KODAK");
            CheckKodakDriver(twain32Path, "Kodak");
            
            // 检查 64位 TWAIN 驱动
            if (Environment.Is64BitProcess && Directory.Exists(twain64Path))
            {
                var driverFolders = Directory.GetDirectories(twain64Path);
                Log($"[TWAIN] Found {driverFolders.Length} TWAIN drivers in twain_64:");
                foreach (var folder in driverFolders)
                {
                    Log($"[TWAIN] - {Path.GetFileName(folder)}");
                }
                
                CheckKodakDriver(twain64Path, "KODAK");
                CheckKodakDriver(twain64Path, "Kodak");
            }
            
            // 检查注册表中的TWAIN数据源
            CheckTwainRegistry();
            
            // 确保在 STA 线程中初始化 TWAIN
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                Log("[TWAIN] Switching to STA thread for initialization");
                var staThread = new Thread(() =>
                {
                    try
                    {
                        InitializeTwainSession();
                    }
                    catch (Exception ex)
                    {
                        Log($"[TWAIN] STA thread initialization failed: {ex.Message}");
                        Log($"[TWAIN] Stack trace: {ex.StackTrace}");
                        _initialized = false;
                    }
                });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
            }
            else
            {
                InitializeTwainSession();
            }
        }
        catch (Exception ex)
        {
            Log($"[TWAIN] Initialization failed: {ex.Message}");
            Log($"[TWAIN] Stack trace: {ex.StackTrace}");
            _initialized = false;
        }
    }

    private void CheckKodakDriver(string basePath, string folderName)
    {
        string kodakPath = Path.Combine(basePath, folderName);
        if (Directory.Exists(kodakPath))
        {
            var kodakDrivers = Directory.GetDirectories(kodakPath);
            Log($"[TWAIN] Found {kodakDrivers.Length} KODAK drivers in {folderName}:");
            foreach (var driver in kodakDrivers)
            {
                Log($"[TWAIN] - {Path.GetFileName(driver)}");
                
                // 查找kds_i1400.dll
                string dllPath = Path.Combine(driver, "kds_i1400.dll");
                if (File.Exists(dllPath))
                {
                    Log($"[TWAIN] Found kds_i1400.dll at: {dllPath}");
                }
            }
        }
    }

    private void CheckTwainRegistry()
    {
        try
        {
            Log("[TWAIN] Checking TWAIN registry...");
            
            // 检查64位注册表
            if (Environment.Is64BitOperatingSystem)
            {
                Log("[TWAIN] Checking 64-bit TWAIN registry...");
                using (var key64 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\TWAIN Working Group\TWAIN Data Sources"))
                {
                    if (key64 != null)
                    {
                        var subKeys = key64.GetSubKeyNames();
                        Log($"[TWAIN] Found {subKeys.Length} 64-bit TWAIN data sources in registry:");
                        foreach (var subKey in subKeys)
                        {
                            Log($"[TWAIN] - {subKey}");
                            using (var sourceKey = key64.OpenSubKey(subKey))
                            {
                                if (sourceKey != null)
                                {
                                    var pathValue = sourceKey.GetValue("Path");
                                    if (pathValue != null)
                                    {
                                        Log($"[TWAIN]   Path: {pathValue}");
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Log("[TWAIN] No 64-bit TWAIN registry key found");
                    }
                }
            }
            
            // 检查32位注册表
            Log("[TWAIN] Checking 32-bit TWAIN registry...");
            using (var key32 = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\TWAIN Working Group\TWAIN Data Sources"))
            {
                if (key32 != null)
                {
                    var subKeys = key32.GetSubKeyNames();
                    Log($"[TWAIN] Found {subKeys.Length} 32-bit TWAIN data sources in registry:");
                    foreach (var subKey in subKeys)
                    {
                        Log($"[TWAIN] - {subKey}");
                        using (var sourceKey = key32.OpenSubKey(subKey))
                        {
                            if (sourceKey != null)
                            {
                                var pathValue = sourceKey.GetValue("Path");
                                if (pathValue != null)
                                {
                                    Log($"[TWAIN]   Path: {pathValue}");
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log("[TWAIN] No 32-bit TWAIN registry key found");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[TWAIN] Error checking registry: {ex.Message}");
        }
    }

    private void InitializeTwainSession()
    {
        try
        {
            // 手动创建TWIdentity对象，避免CreateFromAssembly可能导致的路径问题
            var appId = new TWIdentity();
            appId.Manufacturer = "ScanAgent";
            appId.ProductFamily = "Scanner";
            appId.ProductName = "ScanAgent";
            Log("[TWAIN] Created appId: " + appId.ProductName);
            Log("[TWAIN] AppId details: Manufacturer=" + appId.Manufacturer + ", ProductFamily=" + appId.ProductFamily);
            
            _session = new TwainSession(appId);
            Log("[TWAIN] Created TwainSession");
            
            _session.Open();
            Log("[TWAIN] Session opened successfully");
            
            _initialized = true;
            Log("[TWAIN] Session initialized successfully");
        }
        catch (Exception ex)
        {
            Log($"[TWAIN] TwainSession initialization failed: {ex.Message}");
            Log($"[TWAIN] Stack trace: {ex.StackTrace}");
            _initialized = false;
        }
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        if (!_initialized || _session == null)
        {
            Log("[TWAIN] Session not initialized");
            return new List<ScannerInfo>();
        }

        var scanners = new List<ScannerInfo>();
        var scannerSources = new List<DataSource>();
        
        // TWAIN operations must run on STA thread
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            Log("[TWAIN] Switching to STA thread for scanner enumeration");
            var staThread = new Thread(() =>
            {
                try
                {
                    EnumerateScanners(scannerSources);
                }
                catch (Exception ex)
                {
                    Log($"[TWAIN] Error enumerating data sources: {ex.Message}");
                    Log($"[TWAIN] Stack trace: {ex.StackTrace}");
                }
            });
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }
        else
        {
            try
            {
                EnumerateScanners(scannerSources);
            }
            catch (Exception ex)
            {
                Log($"[TWAIN] Error enumerating data sources: {ex.Message}");
                Log($"[TWAIN] Stack trace: {ex.StackTrace}");
            }
        }
 
        // 智能排序：Kodak i1405 优先
        var sortedSources = scannerSources.OrderByDescending(source => 
            source.Name.Contains("i1405", StringComparison.OrdinalIgnoreCase) ||
            source.Name.Contains("i1400", StringComparison.OrdinalIgnoreCase) ||
            source.Name.Contains("Kodak", StringComparison.OrdinalIgnoreCase) ||
            source.Name.Contains("kds_i1400", StringComparison.OrdinalIgnoreCase)
        ).ToList();
 
        int index = 0;
        foreach (var source in sortedSources)
        {
            scanners.Add(new ScannerInfo
            {
                Id = $"scanner_{index}",
                Name = source.Name,
                Default = index == 0
            });
            Log($"[TWAIN] Scanner {index}: {source.Name} - {(index == 0 ? "Default" : "")}");
            index++;
        }
 
        Log($"[TWAIN] Found {scanners.Count} scanner(s)");
        return scanners;
    }

    private void EnumerateScanners(List<DataSource> scannerSources)
    {
        // 方法1：先获取默认数据源
        Log("[TWAIN] Method 1: Trying to get default data source...");
        try
        {
            var defaultSource = _session.DefaultSource;
            if (defaultSource != null)
            {
                Log($"[TWAIN] Default data source found: {defaultSource.Name}, IsOpen: {defaultSource.IsOpen}");
                if (!scannerSources.Any(s => s.Name == defaultSource.Name))
                {
                    scannerSources.Add(defaultSource);
                    Log("[TWAIN] Added default source to list");
                }
            }
            else
            {
                Log("[TWAIN] No default data source found");
            }
        }
        catch (Exception ex)
        {
            Log($"[TWAIN] Error getting default source: {ex.Message}");
        }
        
        // 方法2：枚举所有数据源
        Log("[TWAIN] Method 2: Enumerating all data sources...");
        try
        {
            int count = 0;
            foreach (var source in _session)
            {
                Log($"[TWAIN] Found source: {source.Name}, IsOpen: {source.IsOpen}");
                if (!scannerSources.Any(s => s.Name == source.Name))
                {
                    scannerSources.Add(source);
                    Log($"[TWAIN] Added source: {source.Name}");
                }
                count++;
            }
            Log($"[TWAIN] Total sources enumerated: {count}");
        }
        catch (Exception ex)
        {
            Log($"[TWAIN] Error enumerating sources: {ex.Message}");
            Log($"[TWAIN] Stack trace: {ex.StackTrace}");
        }
        
        // 方法3：尝试使用GetSources()方法
        Log("[TWAIN] Method 3: Trying GetSources() method...");
        try
        {
            var sources = _session.GetSources();
            Log($"[TWAIN] GetSources() returned {(sources != null ? sources.Count() : 0)} sources");
            foreach (var source in sources)
            {
                Log($"[TWAIN] Found source via GetSources(): {source.Name}");
                if (!scannerSources.Any(s => s.Name == source.Name))
                {
                    scannerSources.Add(source);
                    Log($"[TWAIN] Added source via GetSources(): {source.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Log($"[TWAIN] Error using GetSources(): {ex.Message}");
        }
        
        Log($"[TWAIN] Total unique sources after all methods: {scannerSources.Count}");
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        int attempt = 0;
        while (attempt < _retryCount)
        {
            try
            {
                if (!_initialized || _session == null)
                {
                    Log("[TWAIN] Session not initialized for scan, reinitializing...");
                    InitializeSession();
                    if (!_initialized || _session == null)
                    {
                        throw new Exception("TWAIN session not initialized");
                    }
                }

                var source = FindScanner(request.ScannerId);
                if (source == null)
                {
                    Log("[TWAIN] No scanner found for scan request");
                    throw new ScannerNotFoundException();
                }

                Log("[TWAIN] Opening scanner: " + source.Name);
                source.Open();

                try
                {
                    SetScanParameters(source, request);

                    var images = new List<ImageInfo>();
                    var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
                    var scanCompleted = new TaskCompletionSource<bool>();
                    var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

                    EventHandler<TransferReadyEventArgs> transferReadyHandler = (sender, e) =>
                    {
                        Log("[TWAIN] Transfer ready");
                    };

                    EventHandler<DataTransferredEventArgs> dataTransferredHandler = (sender, e) =>
                    {
                        try
                        {
                            using var stream = e.GetNativeImageStream();
                            if (stream != null)
                            {
                                var imageId = _fileManager.SaveImage(scanId, stream);
                                images.Add(new ImageInfo
                                {
                                    Id = imageId
                                });
                                Log($"[TWAIN] Image saved: {imageId}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"[TWAIN] Error saving image: {ex.Message}");
                        }
                    };

                    EventHandler<TransferErrorEventArgs> transferErrorHandler = (sender, e) =>
                    {
                        Log("[TWAIN] Transfer error");
                        scanCompleted.TrySetException(new Exception("Scan failed"));
                    };

                    EventHandler sourceDisabledHandler = (sender, e) =>
                    {
                        Log("[TWAIN] Source disabled - scan completed");
                        scanCompleted.TrySetResult(true);
                    };

                    _session.TransferReady += transferReadyHandler;
                    _session.DataTransferred += dataTransferredHandler;
                    _session.TransferError += transferErrorHandler;
                    _session.SourceDisabled += sourceDisabledHandler;

                    try
                    {
                        Log("[TWAIN] Starting scan...");
                        source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);

                        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2), timeoutCts.Token);
                        var completedTask = await Task.WhenAny(scanCompleted.Task, timeoutTask);

                        if (completedTask == timeoutTask)
                        {
                            Log("[TWAIN] Scan timeout after 2 minutes");
                            throw new TimeoutException("Scan operation timed out after 2 minutes");
                        }

                        Log($"[TWAIN] Scan completed, {images.Count} images captured");
                        _lastScanTime = DateTime.Now;
                        _lastScannerName = source.Name;
                        
                        return new ScanResult
                        {
                            ScanId = scanId,
                            Status = "completed",
                            Images = images
                        };
                    }
                    finally
                    {
                        timeoutCts.Dispose();
                        _session.TransferReady -= transferReadyHandler;
                        _session.DataTransferred -= dataTransferredHandler;
                        _session.TransferError -= transferErrorHandler;
                        _session.SourceDisabled -= sourceDisabledHandler;
                    }
                }
                finally
                {
                    Log("[TWAIN] Closing scanner: " + source.Name);
                    source.Close();
                }
            }
            catch (Exception ex) when (attempt < _retryCount - 1)
            {
                attempt++;
                Log($"[TWAIN] Scan attempt {attempt} failed: {ex.Message}. Retrying in {_retryDelay.TotalSeconds} seconds...");
                await Task.Delay(_retryDelay);
                // 重新初始化会话以尝试重新连接
                Log("[TWAIN] Reinitializing session for retry...");
                InitializeSession();
            }
        }
        
        Log("[TWAIN] All scan attempts failed");
        throw new Exception("All scan attempts failed");
    }

    private DataSource? FindScanner(string? scannerId)
    {
        if (_session == null)
            return null;

        if (string.IsNullOrEmpty(scannerId))
        {
            // 优先选择 Kodak i1405
            var kodakScanner = _session.FirstOrDefault(source => 
                source.Name.Contains("i1405", StringComparison.OrdinalIgnoreCase) ||
                source.Name.Contains("i1400", StringComparison.OrdinalIgnoreCase) ||
                source.Name.Contains("Kodak", StringComparison.OrdinalIgnoreCase) ||
                source.Name.Contains("kds_i1400", StringComparison.OrdinalIgnoreCase)
            );
            
            if (kodakScanner != null)
            {
                Log("[TWAIN] Auto-selected Kodak scanner: " + kodakScanner.Name);
                return kodakScanner;
            }
            
            // 如果没有 Kodak 扫描仪，选择第一个
            var firstScanner = _session.FirstOrDefault();
            if (firstScanner != null)
            {
                Log("[TWAIN] Auto-selected first scanner: " + firstScanner.Name);
            }
            return firstScanner;
        }

        int index = 0;
        foreach (var source in _session)
        {
            if ($"scanner_{index}" == scannerId)
            {
                Log("[TWAIN] Selected scanner by ID: " + source.Name);
                return source;
            }
            index++;
        }
        
        Log("[TWAIN] Scanner not found for ID: " + scannerId);
        return null;
    }

    private void SetScanParameters(DataSource source, ScanRequest request)
    {
        Log("[TWAIN] Setting scan parameters...");
        
        try
        {
            if (source.Capabilities.ICapPixelType.CanSet)
            {
                var pixelType = request.ColorMode switch
                {
                    "color" => PixelType.RGB,
                    "gray" => PixelType.Gray,
                    "bw" => PixelType.BlackWhite,
                    _ => PixelType.RGB
                };
                source.Capabilities.ICapPixelType.SetValue(pixelType);
                Log("[TWAIN] Pixel type set to: " + pixelType);
            }
        }
        catch (Exception ex)
        {
            Log("[TWAIN] Error setting pixel type: " + ex.Message);
        }

        try
        {
            if (source.Capabilities.ICapXResolution.CanSet)
            {
                source.Capabilities.ICapXResolution.SetValue(request.Dpi);
                Log("[TWAIN] X resolution set to: " + request.Dpi);
            }
        }
        catch (Exception ex)
        {
            Log("[TWAIN] Error setting X resolution: " + ex.Message);
        }

        try
        {
            if (source.Capabilities.ICapYResolution.CanSet)
            {
                source.Capabilities.ICapYResolution.SetValue(request.Dpi);
                Log("[TWAIN] Y resolution set to: " + request.Dpi);
            }
        }
        catch (Exception ex)
        {
            Log("[TWAIN] Error setting Y resolution: " + ex.Message);
        }

        Log("[TWAIN] Scan parameters set");
    }

    public ScannerServiceStatus GetStatus()
    {
        var scanners = GetAvailableScanners();
        var kodakScanner = scanners.FirstOrDefault(s => 
            s.Name.Contains("i1405", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Contains("i1400", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Contains("Kodak", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Contains("kds_i1400", StringComparison.OrdinalIgnoreCase)
        );

        return new ScannerServiceStatus
        {
            Initialized = _initialized,
            SessionActive = _session != null,
            AvailableScanners = scanners.Count,
            KodakScannerAvailable = kodakScanner != null,
            LastScanTime = _lastScanTime,
            LastScannerName = _lastScannerName ?? string.Empty,
            SystemArchitecture = Environment.Is64BitProcess ? "64-bit" : "32-bit",
            ServiceHealth = _initialized && _session != null ? "Healthy" : "Unhealthy"
        };
    }
}
