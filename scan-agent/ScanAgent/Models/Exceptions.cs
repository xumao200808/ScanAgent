namespace ScanAgent.Models;

public class ScannerNotFoundException : Exception
{
    public ScannerNotFoundException() : base("未找到可用的扫描仪") { }
}

public class ScannerBusyException : Exception
{
    public ScannerBusyException() : base("扫描仪正在被占用") { }
}