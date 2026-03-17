namespace ScanAgent.Utils;

public class TempFileManager
{
    private readonly string _baseDir;
    private readonly Dictionary<string, string> _fileMap = new();
    private readonly object _lock = new();
    private const int BufferSize = 81920; // 80KB buffer for optimal performance

    public TempFileManager()
    {
        _baseDir = Path.Combine(Path.GetTempPath(), "ScanAgent");
        Directory.CreateDirectory(_baseDir);
    }

    public string SaveImage(string scanId, Stream imageStream)
    {
        lock (_lock)
        {
            var scanDir = Path.Combine(_baseDir, scanId);
            Directory.CreateDirectory(scanDir);

            var imageIndex = Directory.GetFiles(scanDir, "*.png").Length + 1;
            var imageId = $"img_{imageIndex:D3}";
            var filePath = Path.Combine(scanDir, $"page_{imageIndex:D3}.png");

            using var fileStream = File.Create(filePath);
            var buffer = new byte[BufferSize];
            int bytesRead;
            
            while ((bytesRead = imageStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
            }

            _fileMap[imageId] = filePath;
            return imageId;
        }
    }

    public string? GetFilePath(string imageId)
    {
        lock (_lock)
        {
            return _fileMap.TryGetValue(imageId, out var path) ? path : null;
        }
    }

    public bool CleanupScan(string scanId)
    {
        lock (_lock)
        {
            var scanDir = Path.Combine(_baseDir, scanId);
            if (!Directory.Exists(scanDir))
                return false;

            var files = Directory.GetFiles(scanDir);
            foreach (var file in files)
            {
                var id = Path.GetFileNameWithoutExtension(file)
                    .Replace("page_", "img_");
                _fileMap.Remove(id);
            }

            Directory.Delete(scanDir, true);
            return true;
        }
    }

    public void CleanupOldScans(TimeSpan maxAge)
    {
        lock (_lock)
        {
            var now = DateTime.Now;
            var scanDirs = Directory.GetDirectories(_baseDir);

            foreach (var scanDir in scanDirs)
            {
                var dirInfo = new DirectoryInfo(scanDir);
                if (now - dirInfo.CreationTime > maxAge)
                {
                    try
                    {
                        var files = Directory.GetFiles(scanDir);
                        foreach (var file in files)
                        {
                            var id = Path.GetFileNameWithoutExtension(file)
                                .Replace("page_", "img_");
                            _fileMap.Remove(id);
                        }

                        Directory.Delete(scanDir, true);
                        Console.WriteLine($"[TempFileManager] Cleaned up old scan: {scanDir}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TempFileManager] Error cleaning up {scanDir}: {ex.Message}");
                    }
                }
            }
        }
    }

    public int GetTotalScanCount()
    {
        lock (_lock)
        {
            return Directory.GetDirectories(_baseDir).Length;
        }
    }
}