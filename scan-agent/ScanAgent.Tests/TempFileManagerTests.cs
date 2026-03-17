using ScanAgent.Utils;
using Xunit;

namespace ScanAgent.Tests;

public class TempFileManagerTests
{
    public TempFileManagerTests()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ScanAgent");
        if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SaveImage_ShouldCreateFile()
    {
        var manager = new TempFileManager();
        var scanId = "test_scan_001";
        var testData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var stream = new MemoryStream(testData);

        var imageId = manager.SaveImage(scanId, stream);

        Assert.NotNull(imageId);
        var filePath = manager.GetFilePath(imageId);
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void GetFilePath_ShouldReturnCorrectPath()
    {
        var manager = new TempFileManager();
        var scanId = "test_scan_002";
        var testData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var stream = new MemoryStream(testData);

        var imageId = manager.SaveImage(scanId, stream);
        var filePath = manager.GetFilePath(imageId);

        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void GetFilePath_ShouldReturnNullForNonExistentImage()
    {
        var manager = new TempFileManager();
        var filePath = manager.GetFilePath("non_existent_id");

        Assert.Null(filePath);
    }

    [Fact]
    public void CleanupScan_ShouldRemoveScanDirectory()
    {
        var manager = new TempFileManager();
        var scanId = "test_scan_003";
        var testData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var stream = new MemoryStream(testData);

        manager.SaveImage(scanId, stream);
        var scanDir = Path.Combine(Path.GetTempPath(), "ScanAgent", scanId);
        Assert.True(Directory.Exists(scanDir));

        var result = manager.CleanupScan(scanId);

        Assert.True(result);
        Assert.False(Directory.Exists(scanDir));
    }

    [Fact]
    public void CleanupScan_ShouldReturnFalseForNonExistentScan()
    {
        var manager = new TempFileManager();
        var result = manager.CleanupScan("non_existent_scan");

        Assert.False(result);
    }
}