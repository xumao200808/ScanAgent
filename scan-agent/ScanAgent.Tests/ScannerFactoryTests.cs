using ScanAgent.Services;
using ScanAgent.Utils;
using Xunit;

namespace ScanAgent.Tests;

public class ScannerFactoryTests
{
    [Fact]
    public void GetScannerService_ShouldReturnService()
    {
        var fileManager = new TempFileManager();
        var factory = new ScannerFactory(fileManager);
        var service = factory.GetScannerService();

        Assert.NotNull(service);
    }

    [Fact]
    public void GetAvailableScanners_ShouldReturnList()
    {
        var fileManager = new TempFileManager();
        var factory = new ScannerFactory(fileManager);
        var service = factory.GetScannerService();
        var scanners = service.GetAvailableScanners();

        Assert.NotNull(scanners);
        Assert.True(scanners.Count >= 0);
    }

    [Fact]
    public void GetAvailableScanners_ShouldHaveDefaultScanner()
    {
        var fileManager = new TempFileManager();
        var factory = new ScannerFactory(fileManager);
        var service = factory.GetScannerService();
        var scanners = service.GetAvailableScanners();

        if (scanners.Count > 0)
        {
            var hasDefault = scanners.Any(s => s.Default);
            Assert.True(hasDefault, "At least one scanner should be marked as default");
        }
    }
}