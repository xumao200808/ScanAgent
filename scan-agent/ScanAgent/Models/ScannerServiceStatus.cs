namespace ScanAgent.Models;

public class ScannerServiceStatus
{
    public bool Initialized { get; set; }
    public bool SessionActive { get; set; }
    public int AvailableScanners { get; set; }
    public bool KodakScannerAvailable { get; set; }
    public DateTime LastScanTime { get; set; }
    public string LastScannerName { get; set; } = string.Empty;
    public string SystemArchitecture { get; set; } = string.Empty;
    public string ServiceHealth { get; set; } = string.Empty;
    public string CurrentServiceType { get; set; } = string.Empty;
}
