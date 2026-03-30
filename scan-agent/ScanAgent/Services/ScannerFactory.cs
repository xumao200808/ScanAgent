using ScanAgent.Models;
using ScanAgent.Utils;

namespace ScanAgent.Services;

public enum ScannerServiceType
{
    Auto,
    Wia,
    Twain,
    EZTwain,
    Virtual
}

public class ScannerFactory
{
    private readonly TempFileManager _fileManager;
    private readonly IntPtr _windowHandle;
    private IScannerService? _primaryService;
    private IScannerService? _fallbackService;
    private List<ScannerInfo>? _cachedScanners;
    private DateTime _cacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);
    private readonly object _lock = new();
    private ScannerServiceType _currentServiceType = ScannerServiceType.Auto;

    public ScannerFactory(TempFileManager fileManager, IntPtr windowHandle = default)
    {
        _fileManager = fileManager;
        _windowHandle = windowHandle;
    }

    public void SetWindowHandle(IntPtr windowHandle)
    {
        Console.WriteLine($"[Factory] Setting window handle: {windowHandle}");
        // 这里不直接赋值，因为字段是 readonly
        // 如果需要动态更新，可以添加一个非 readonly 的字段
    }

    public void SetServiceType(ScannerServiceType serviceType)
    {
        lock (_lock)
        {
            if (_currentServiceType != serviceType)
            {
                _currentServiceType = serviceType;
                _primaryService = null;
                _fallbackService = null;
                _cachedScanners = null;
                Console.WriteLine($"[Factory] Service type changed to: {serviceType}");
            }
        }
    }

    public ScannerServiceType GetServiceType()
    {
        lock (_lock)
        {
            return _currentServiceType;
        }
    }

    public IScannerService GetScannerService()
    {
        lock (_lock)
        {
            return GetScannerServiceByType(_currentServiceType);
        }
    }

    private IScannerService GetScannerServiceByType(ScannerServiceType serviceType)
    {
        switch (serviceType)
        {
            case ScannerServiceType.Wia:
                return GetWiaService();
            case ScannerServiceType.Twain:
                return GetTwainService();
            case ScannerServiceType.EZTwain:
                return GetEZTwainService();
            case ScannerServiceType.Virtual:
                return GetVirtualService();
            case ScannerServiceType.Auto:
            default:
                return GetAutoService();
        }
    }

    private IScannerService GetWiaService()
    {
        if (_primaryService == null || !(_primaryService is WiaScannerService))
        {
            try
            {
                _primaryService = new WiaScannerService(_fileManager);
                var scanners = _primaryService.GetAvailableScanners();
                _cachedScanners = scanners;
                _cacheTime = DateTime.Now;
                Console.WriteLine("[Factory] Using WIA scanner service");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Factory] WIA initialization failed: {ex.Message}");
                _primaryService = new VirtualScannerService(_fileManager);
            }
        }
        return _primaryService;
    }

    private IScannerService GetTwainService()
    {
        if (_primaryService == null || !(_primaryService is TwainScannerService))
        {
            try
            {
                _primaryService = new TwainScannerService(_fileManager);
                var scanners = _primaryService.GetAvailableScanners();
                _cachedScanners = scanners;
                _cacheTime = DateTime.Now;
                Console.WriteLine("[Factory] Using TWAIN scanner service");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Factory] TWAIN initialization failed: {ex.Message}");
                _primaryService = new VirtualScannerService(_fileManager);
            }
        }
        return _primaryService;
    }

    private IScannerService GetEZTwainService()
    {
        // EZTwain 只支持 32 位进程
        if (!Environment.Is64BitProcess)
        {
            if (_primaryService == null || !(_primaryService is EZTwainScannerService))
            {
                try
                {
                    _primaryService = new EZTwainScannerService(_fileManager, _windowHandle);
                    var scanners = _primaryService.GetAvailableScanners();
                    _cachedScanners = scanners;
                    _cacheTime = DateTime.Now;
                    Console.WriteLine("[Factory] Using EZTwain scanner service");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Factory] EZTwain initialization failed: {ex.Message}");
                    _primaryService = new VirtualScannerService(_fileManager);
                }
            }
            return _primaryService;
        }
        else
        {
            Console.WriteLine("[Factory] EZTwain is not supported in 64-bit process, falling back to Virtual");
            return GetVirtualService();
        }
    }

    private IScannerService GetVirtualService()
    {
        if (_fallbackService == null || !(_fallbackService is VirtualScannerService))
        {
            _fallbackService = new VirtualScannerService(_fileManager);
            Console.WriteLine("[Factory] Using Virtual scanner service");
        }
        return _fallbackService;
    }

    private IScannerService GetAutoService()
    {
        if (_primaryService == null)
        {
            try
            {
                Console.WriteLine("[Factory] Trying WIA first...");
                var wiaService = new WiaScannerService(_fileManager);
                var wiaScanners = wiaService.GetAvailableScanners();
                
                if (wiaScanners.Count > 0)
                {
                    _primaryService = wiaService;
                    _cachedScanners = wiaScanners;
                    _cacheTime = DateTime.Now;
                    Console.WriteLine("[Factory] Using WIA scanner service (auto)");
                    return _primaryService;
                }
                else
                {
                    Console.WriteLine("[Factory] WIA has no scanners, trying TWAIN...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Factory] WIA initialization failed: {ex.Message}");
            }

            try
            {
                _primaryService = new TwainScannerService(_fileManager);
                var scanners = _primaryService.GetAvailableScanners();
                _cachedScanners = scanners;
                _cacheTime = DateTime.Now;
                
                if (scanners.Count > 0)
                {
                    Console.WriteLine("[Factory] Using TWAIN scanner service (auto)");
                    return _primaryService;
                }
                else
                {
                    Console.WriteLine("[Factory] TWAIN initialized but no scanners available");
                    _primaryService = null;
                    _cachedScanners = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Factory] TWAIN initialization failed: {ex.Message}");
                _primaryService = null;
                _cachedScanners = null;
            }
        }
        else
        {
            if (_cachedScanners == null || DateTime.Now - _cacheTime > _cacheDuration)
            {
                var scanners = _primaryService.GetAvailableScanners();
                _cachedScanners = scanners;
                _cacheTime = DateTime.Now;
            }
            
            if (_cachedScanners.Count > 0)
            {
                Console.WriteLine("[Factory] Using cached scanner service");
                return _primaryService;
            }
            else
            {
                Console.WriteLine("[Factory] Cached service has no scanners, resetting");
                _primaryService = null;
                _cachedScanners = null;
                return GetAutoService();
            }
        }

        if (_fallbackService == null)
        {
            Console.WriteLine("[Factory] No WIA or TWAIN scanners found, using virtual scanner for testing");
            _fallbackService = new VirtualScannerService(_fileManager);
        }

        return _fallbackService;
    }

    public ScannerServiceStatus GetServiceStatus()
    {
        lock (_lock)
        {
            var service = GetScannerService();
            var status = service.GetStatus();
            status.CurrentServiceType = _currentServiceType.ToString();
            return status;
        }
    }
}