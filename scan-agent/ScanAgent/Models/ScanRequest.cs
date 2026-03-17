namespace ScanAgent.Models;

public class ScanRequest
{
    public string? ScannerId { get; set; }
    public int Dpi { get; set; } = 300;
    public string ColorMode { get; set; } = "gray";
    public bool Duplex { get; set; } = false;
    public bool AutoFeed { get; set; } = true;
    public string PaperSize { get; set; } = "A4";
}