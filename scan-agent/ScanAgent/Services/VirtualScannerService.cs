using ScanAgent.Models;
using ScanAgent.Utils;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ScanAgent.Services;

public class VirtualScannerService : IScannerService
{
    private readonly TempFileManager _fileManager;
    private readonly List<ScannerInfo> _virtualScanners;
    private DateTime _lastScanTime = DateTime.MinValue;

    public VirtualScannerService(TempFileManager fileManager)
    {
        _fileManager = fileManager;
        _virtualScanners = new List<ScannerInfo>
        {
            new ScannerInfo
            {
                Id = "virtual_scanner_0",
                Name = "Virtual Scanner (Test Mode)",
                Default = true
            }
        };
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        Console.WriteLine("[Virtual] Returning virtual scanner for testing");
        return _virtualScanners;
    }

    public Task<ScanResult> ScanAsync(ScanRequest request)
    {
        Console.WriteLine("[Virtual] Starting virtual scan...");
        
        var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
        var images = new List<ImageInfo>();

        try
        {
            // 创建一个测试图像
            using (var bitmap = new Bitmap(800, 600))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                // 白色背景
                graphics.Clear(Color.White);

                // 绘制测试文本
                var text = $"Virtual Scan Test\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}\nDPI: {request.Dpi}";
                using (var font = new Font("Arial", 24))
                using (var brush = new SolidBrush(Color.Black))
                {
                    graphics.DrawString(text, font, brush, 100, 200);
                }

                // 保存到内存流
                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    var imageBytes = memoryStream.ToArray();

                    // 保存图像
                    var imageId = _fileManager.SaveImage(scanId, new MemoryStream(imageBytes));
                    images.Add(new ImageInfo
                    {
                        Id = imageId
                    });
                    Console.WriteLine($"[Virtual] Image saved: {imageId}");
                }
            }

            _lastScanTime = DateTime.Now;

            return Task.FromResult(new ScanResult
            {
                ScanId = scanId,
                Status = "completed",
                Images = images
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Virtual] Scan failed: {ex.Message}");
            throw;
        }
    }

    public ScannerServiceStatus GetStatus()
    {
        return new ScannerServiceStatus
        {
            Initialized = true,
            SessionActive = true,
            AvailableScanners = _virtualScanners.Count,
            KodakScannerAvailable = false, // 虚拟扫描仪不是Kodak
            LastScanTime = _lastScanTime,
            LastScannerName = "Virtual Scanner (Test Mode)",
            SystemArchitecture = Environment.Is64BitProcess ? "64-bit" : "32-bit",
            ServiceHealth = "Healthy"
        };
    }
}
