using ScanAgent.Models;
using ScanAgent.Utils;
using System.Runtime.InteropServices;

namespace ScanAgent.Services;

public class WiaScannerService : IScannerService
{
    private readonly TempFileManager _fileManager;
    private DateTime _lastScanTime = DateTime.MinValue;
    private string _lastScannerName = string.Empty;

    public WiaScannerService(TempFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        Console.WriteLine("[WIA] Enumerating WIA scanners...");
        var scanners = new List<ScannerInfo>();

        try
        {
            // 使用 WIA DeviceManager COM 对象
            Type wiaType = Type.GetTypeFromProgID("WIA.DeviceManager");
            if (wiaType == null)
            {
                Console.WriteLine("[WIA] WIA DeviceManager not available");
                return scanners;
            }

            dynamic deviceManager = Activator.CreateInstance(wiaType);
            var deviceInfos = deviceManager.DeviceInfos;

            int index = 0;
            for (int i = 1; i <= deviceInfos.Count; i++)
            {
                var deviceInfo = deviceInfos[i];
                // 检查设备类型是否为扫描仪
                if (deviceInfo.Type == 1) // 1 = Scanner
                {
                    var name = deviceInfo.Name;
                    Console.WriteLine($"[WIA] Found scanner: {name}");

                    scanners.Add(new ScannerInfo
                    {
                        Id = $"scanner_{index}",
                        Name = name,
                        Default = index == 0
                    });
                    index++;
                }
            }

            Console.WriteLine($"[WIA] Found {scanners.Count} WIA scanner(s)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WIA] Enumeration failed: {ex.Message}");
            Console.WriteLine($"[WIA] Stack trace: {ex.StackTrace}");
        }

        return scanners;
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        Console.WriteLine("[WIA] Starting WIA scan...");

        var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
        var images = new List<ImageInfo>();

        try
        {
            Type wiaType = Type.GetTypeFromProgID("WIA.DeviceManager");
            if (wiaType == null)
            {
                throw new Exception("WIA DeviceManager not available");
            }

            dynamic deviceManager = Activator.CreateInstance(wiaType);
            var deviceInfos = deviceManager.DeviceInfos;

            if (deviceInfos.Count == 0)
            {
                throw new Exception("No WIA scanner found");
            }

            var deviceInfo = deviceInfos[1];
            var device = deviceManager.CreateDevice(deviceInfo.DeviceID);
            var scannerName = deviceInfo.Name;
            Console.WriteLine($"[WIA] Using scanner: {scannerName}");

            var imageBytes = ScanImage(device);

            if (imageBytes != null && imageBytes.Length > 0)
            {
                var imageId = _fileManager.SaveImage(scanId, new MemoryStream(imageBytes));
                images.Add(new ImageInfo
                {
                    Id = imageId
                });
                Console.WriteLine($"[WIA] Image saved: {imageId}");
            }

            _lastScanTime = DateTime.Now;
            _lastScannerName = scannerName;

            return new ScanResult
            {
                ScanId = scanId,
                Status = "completed",
                Images = images
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WIA] Scan failed: {ex.Message}");
            throw;
        }
    }

    private byte[] ScanImage(dynamic device)
    {
        try
        {
            Console.WriteLine("[WIA] Starting scan using WIA CommonDialog...");
            Type wiaCommonDialogType = Type.GetTypeFromProgID("WIA.CommonDialog");
            if (wiaCommonDialogType != null)
            {
                dynamic commonDialog = Activator.CreateInstance(wiaCommonDialogType);
                var imageFile = commonDialog.ShowAcquireImage(
                    1, // Scanner device type
                    0, // Intent: Unspecified
                    0, // Bias: 0
                    false, // Don't show dialog
                    false, // Don't use common UI
                    false // Don't show preview
                );

                if (imageFile != null)
                {
                    Console.WriteLine("[WIA] Scan successful, processing image...");
                    var imageBytes = (byte[])imageFile.FileData.BinaryData;
                    return imageBytes;
                }
            }

            Console.WriteLine("[WIA] CommonDialog failed, trying direct device access...");
            return ScanImageDirect(device);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WIA] Scan error with CommonDialog: {ex.Message}");
            try
            {
                return ScanImageDirect(device);
            }
            catch (Exception ex2)
            {
                Console.WriteLine($"[WIA] Direct scan also failed: {ex2.Message}");
                return Array.Empty<byte>();
            }
        }
    }

    private byte[] ScanImageDirect(dynamic device)
    {
        try
        {
            Console.WriteLine("[WIA] Using direct device scan...");
            dynamic item = device.Items[1];
            
            var properties = item.Properties;
            foreach (dynamic prop in properties)
            {
                if (prop.PropertyID == 6146) // Horizontal Resolution (DPI)
                {
                    prop.Value = 300;
                }
                else if (prop.PropertyID == 6147) // Vertical Resolution (DPI)
                {
                    prop.Value = 300;
                }
                else if (prop.PropertyID == 6149) // Bits per pixel
                {
                    prop.Value = 24; // Color
                }
            }

            dynamic imageFile = item.Transfer("{B9673C42-DE40-11D1-8B24-00A0C91853CA}");
            if (imageFile != null)
            {
                var imageBytes = (byte[])imageFile.FileData.BinaryData;
                return imageBytes;
            }

            return Array.Empty<byte>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WIA] Direct scan error: {ex.Message}");
            Console.WriteLine($"[WIA] Stack trace: {ex.StackTrace}");
            return Array.Empty<byte>();
        }
    }

    public ScannerServiceStatus GetStatus()
    {
        var scanners = GetAvailableScanners();
        var kodakScanner = scanners.FirstOrDefault(s => 
            s.Name.Contains("i1405", StringComparison.OrdinalIgnoreCase) ||
            s.Name.Contains("Kodak", StringComparison.OrdinalIgnoreCase)
        );

        return new ScannerServiceStatus
        {
            Initialized = true, // WIA服务总是初始化的
            SessionActive = true,
            AvailableScanners = scanners.Count,
            KodakScannerAvailable = kodakScanner != null,
            LastScanTime = _lastScanTime,
            LastScannerName = _lastScannerName,
            SystemArchitecture = Environment.Is64BitProcess ? "64-bit" : "32-bit",
            ServiceHealth = scanners.Count > 0 ? "Healthy" : "Unhealthy"
        };
    }
}
