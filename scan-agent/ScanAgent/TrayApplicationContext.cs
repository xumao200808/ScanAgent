using Microsoft.Win32;
using ScanAgent.Utils;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ScanAgent;

public class TrayApplicationContext : ApplicationContext
{
    private const string AppName = "ScanAgent";
    private const string AppVersion = "1.0.0";
    private const int Port = 17289;
    private const string AutoStartRegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly CancellationTokenSource _cts;
    private readonly TempFileManager _fileManager;
    private readonly NotifyIcon _notifyIcon;
    private readonly DateTime _startTime = DateTime.Now;
    private Icon? _trayIcon;

    public TrayApplicationContext(CancellationTokenSource cts, TempFileManager fileManager)
    {
        _cts = cts;
        _fileManager = fileManager;

        _trayIcon = CreateTrayIcon();
        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = $"{AppName} v{AppVersion}",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => OpenFrontend();

        _notifyIcon.ShowBalloonTip(
            3000,
            AppName,
            $"ScanAgent 已启动\nAPI: http://127.0.0.1:{Port}",
            ToolTipIcon.Info);
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var labelItem = new ToolStripMenuItem($"{AppName} v{AppVersion}") { Enabled = false };
        menu.Items.Add(labelItem);
        menu.Items.Add(new ToolStripSeparator());

        var openItem = new ToolStripMenuItem("打开扫描界面");
        openItem.Click += (_, _) => OpenFrontend();
        menu.Items.Add(openItem);

        var statusItem = new ToolStripMenuItem("查看状态");
        statusItem.Click += (_, _) => ShowStatus();
        menu.Items.Add(statusItem);

        menu.Items.Add(new ToolStripSeparator());

        var autoStartItem = new ToolStripMenuItem("开机自启动") { CheckOnClick = false };
        autoStartItem.Click += (_, _) => ToggleAutoStart(autoStartItem);
        menu.Items.Add(autoStartItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => ExitApp();
        menu.Items.Add(exitItem);

        menu.Opening += (_, _) => autoStartItem.Checked = IsAutoStartEnabled();

        return menu;
    }

    private void OpenFrontend()
    {
        var exeDir = Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty;
        var frontendPath = Path.Combine(exeDir, "frontend", "index.html");

        if (File.Exists(frontendPath))
        {
            Process.Start(new ProcessStartInfo(frontendPath) { UseShellExecute = true });
        }
        else
        {
            MessageBox.Show(
                $"未找到前端文件：\n{frontendPath}\n\n请确认 frontend 目录已正确部署。",
                AppName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ShowStatus()
    {
        var uptime = DateTime.Now - _startTime;
        var scanCount = _fileManager.GetTotalScanCount();
        var uptimeStr = uptime.TotalHours >= 1
            ? $"{(int)uptime.TotalHours} 小时 {uptime.Minutes} 分钟"
            : $"{uptime.Minutes} 分钟 {uptime.Seconds} 秒";

        MessageBox.Show(
            $"状态：运行中\n" +
            $"API 地址：http://127.0.0.1:{Port}\n" +
            $"已扫描次数：{scanCount}\n" +
            $"运行时长：{uptimeStr}",
            $"{AppName} 状态",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegKey, false);
        return key?.GetValue(AppName) != null;
    }

    private static void ToggleAutoStart(ToolStripMenuItem item)
    {
        using var key = Registry.CurrentUser.OpenSubKey(AutoStartRegKey, true);
        if (key == null) return;

        if (IsAutoStartEnabled())
        {
            key.DeleteValue(AppName, throwOnMissingValue: false);
            item.Checked = false;
        }
        else
        {
            var exePath = Application.ExecutablePath;
            key.SetValue(AppName, $"\"{exePath}\"");
            item.Checked = true;
        }
    }

    private void ExitApp()
    {
        _notifyIcon.Visible = false;
        _cts.Cancel();
        Application.Exit();
    }

    private static Icon CreateTrayIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.Transparent);
        // Scanner body
        g.FillRectangle(new SolidBrush(Color.FromArgb(41, 98, 255)), 1, 4, 14, 9);
        // Scanner glass/platen
        g.FillRectangle(Brushes.White, 3, 6, 10, 5);
        // Scan line indicator
        g.FillEllipse(new SolidBrush(Color.FromArgb(100, 180, 255)), 5, 7, 3, 3);
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayIcon?.Dispose();
            _trayIcon = null;
        }
        base.Dispose(disposing);
    }
}
