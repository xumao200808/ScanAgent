using ScanAgent.Models;
using ScanAgent.Utils;

namespace ScanAgent.Services;

public class ScannerFactory
{
    private readonly TempFileManager _fileManager;
    private IScannerService? _primaryService;
    private IScannerService? _fallbackService;
    private List<ScannerInfo>? _cachedScanners;
    private DateTime _cacheTime = DateTime.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(5);
    private readonly object _lock = new();

    public ScannerFactory(TempFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public IScannerService GetScannerService()
    {
        lock (_lock)
        {
            if (_primaryService == null)
            {
                try
                {
                    _primaryService = new TwainScannerService(_fileManager);
                    var scanners = _primaryService.GetAvailableScanners();
                    _cachedScanners = scanners;
                    _cacheTime = DateTime.Now;
                    
                    if (scanners.Count > 0)
                    {
                        Console.WriteLine("[Factory] Using TWAIN scanner service");
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
                    Console.WriteLine("[Factory] Using cached TWAIN scanner service");
                    return _primaryService;
                }
                else
                {
                    Console.WriteLine("[Factory] Cached TWAIN service has no scanners, falling back");
                    _primaryService = null;
                    _cachedScanners = null;
                }
            }

            if (_fallbackService == null)
            {
                _fallbackService = new WiaScannerService(_fileManager);
                Console.WriteLine("[Factory] Using WIA fallback service");
            }

            return _fallbackService;
        }
    }
}