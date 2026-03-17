using ScanAgent.Models;
using ScanAgent.Utils;
using System.Runtime.InteropServices;

namespace ScanAgent.Services;

public class WiaScannerService : IScannerService
{
    private readonly TempFileManager _fileManager;

    public WiaScannerService(TempFileManager fileManager)
    {
        _fileManager = fileManager;
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        Console.WriteLine("[WIA] WIA fallback mode - no scanners available");
        return new List<ScannerInfo>();
    }

    public Task<ScanResult> ScanAsync(ScanRequest request)
    {
        Console.WriteLine("[WIA] WIA scan not implemented - this is a fallback service");
        throw new NotImplementedException("WIA scan is not implemented. Please use TWAIN driver.");
    }
}