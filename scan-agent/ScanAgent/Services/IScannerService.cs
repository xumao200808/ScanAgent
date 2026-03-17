using ScanAgent.Models;

namespace ScanAgent.Services;

public interface IScannerService
{
    List<ScannerInfo> GetAvailableScanners();
    Task<ScanResult> ScanAsync(ScanRequest request);
}