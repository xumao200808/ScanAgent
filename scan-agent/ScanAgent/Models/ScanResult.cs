namespace ScanAgent.Models;

public class ScanResult
{
    public string ScanId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<ImageInfo> Images { get; set; } = new();
}

public class ImageInfo
{
    public string Id { get; set; } = string.Empty;
}