using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ScanAgent.Utils;

public static class EZTwain32
{
    private const string EZTW32_DLL = "eztw32.dll";

    // 基本函数使用正确的函数名（来自 Delphi 的 EZTwain32.pas
    [DllImport(EZTW32_DLL, CharSet = CharSet.Ansi)]
    public static extern IntPtr TWAIN_Acquire(IntPtr hwndApp);

    [DllImport(EZTW32_DLL, CharSet = CharSet.Ansi)]
    public static extern IntPtr TWAIN_AcquireNative(IntPtr hwndApp, uint wPixTypes);

    [DllImport(EZTW32_DLL)]
    public static extern void TWAIN_FreeNative(IntPtr hdib);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_IsAvailable();

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_State();

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_GetSourceList();

    [DllImport(EZTW32_DLL, CharSet = CharSet.Ansi)]
    public static extern int TWAIN_GetNextSourceName([Out] StringBuilder sourceName);

    [DllImport(EZTW32_DLL, CharSet = CharSet.Ansi)]
    public static extern int TWAIN_OpenSource(string sourceName);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_OpenDefaultSource();

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_CloseSource();

    [DllImport(EZTW32_DLL)]
    public static extern void TWAIN_SetHideUI(int hide);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetCurrentUnits(int units);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetCurrentPixelType(int pixelType);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetCurrentResolution(double dRes);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetCustomResolution(uint dRes);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetPaperSize(uint paperSize);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetFeederEnabled(int enabled);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetDuplexEnabled(int enabled);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetAutoFeed(int enabled);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_SetImageLayout(int layout);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_EasyVersion();

    [DllImport(EZTW32_DLL, CharSet = CharSet.Ansi)]
    public static extern int TWAIN_AcquireToFilename(IntPtr hwndApp, string filename);

    // 像素类型
    public const int TWAIN_BW = 0x0001;
    public const int TWAIN_GRAY = 0x0002;
    public const int TWAIN_RGB = 0x0004;

    // 单位
    public const int TWUN_INCHES = 0;
    public const int TWUN_CENTIMETERS = 1;
    public const int TWUN_PIXELS = 5;

    // 状态
    public const int TWAIN_PRESESSION = 1;
    public const int TWAIN_SM_LOADED = 2;
    public const int TWAIN_SM_OPEN = 3;
    public const int TWAIN_SOURCE_OPEN = 4;
    public const int TWAIN_SOURCE_ENABLED = 5;
    public const int TWAIN_TRANSFER_READY = 6;

    // 纸张尺寸
    public const int TWAIN_PAPER_A4 = 0;
    public const int TWAIN_PAPER_A5 = 1;
    public const int TWAIN_PAPER_A6 = 2;
    public const int TWAIN_PAPER_B5 = 3;
    public const int TWAIN_PAPER_LETTER = 4;
    public const int TWAIN_PAPER_LEGAL = 5;

    // 图像布局
    public const int TWAIN_LAYOUT_AUTO = 0;
    public const int TWAIN_LAYOUT_PORTRAIT = 1;
    public const int TWAIN_LAYOUT_LANDSCAPE = 2;

    // DIB 处理函数（正确的函数名）
    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_DibWidth(IntPtr hdib);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_DibHeight(IntPtr hdib);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_DibDepth(IntPtr hdib);

    [DllImport(EZTW32_DLL)]
    public static extern int TWAIN_DibNumColors(IntPtr hdib);

    // GDI 相关函数
    [DllImport("gdi32.dll")]
    public static extern int GetObject(IntPtr hgdiobj, int cbBuffer, [Out] byte[] lpvObject);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateDIBSection(IntPtr hdc, IntPtr pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr hObject);
}
