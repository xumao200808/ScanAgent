using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ScanAgent.Models;
using ScanAgent.Services;
using ScanAgent.Utils;
using System.Windows.Forms;

namespace ScanAgent;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<TempFileManager>();
        builder.Services.AddSingleton<ScannerFactory>();
        builder.Services.AddHostedService<CleanupBackgroundService>();
        builder.Services.AddControllers();
        builder.Services.AddCors();

        var app = builder.Build();

        app.UseCors(policy => policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyMethod()
            .AllowAnyHeader());

        app.MapGet("/ping", () =>
            Results.Ok(new { status = "ok", version = "1.0.0" }));

        app.MapGet("/scanners", (ScannerFactory factory) =>
        {
            try
            {
                var scanner = factory.GetScannerService();
                var scanners = scanner.GetAvailableScanners();
                return Results.Ok(new { scanners });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { error = "twain_not_available", message = ex.Message },
                    statusCode: 503);
            }
        });

        app.MapPost("/scan", async (ScanRequest request, ScannerFactory factory) =>
        {
            try
            {
                var scanner = factory.GetScannerService();
                var result = await scanner.ScanAsync(request);
                return Results.Ok(result);
            }
            catch (ScannerNotFoundException)
            {
                return Results.Json(
                    new { error = "scanner_not_found", message = "未找到可用的扫描仪" },
                    statusCode: 404);
            }
            catch (ScannerBusyException)
            {
                return Results.Json(
                    new { error = "scanner_busy", message = "扫描仪正在被占用" },
                    statusCode: 409);
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new { error = "scan_failed", message = ex.Message },
                    statusCode: 500);
            }
        });

        app.MapGet("/files/{imageId}", (string imageId, TempFileManager fileManager) =>
        {
            var filePath = fileManager.GetFilePath(imageId);
            if (filePath == null || !File.Exists(filePath))
                return Results.NotFound();
            return Results.File(filePath, "image/png");
        });

        app.MapDelete("/scans/{scanId}", (string scanId, TempFileManager fileManager) =>
        {
            var deleted = fileManager.CleanupScan(scanId);
            if (!deleted)
                return Results.NotFound();
            return Results.Ok(new { status = "ok" });
        });

        var cts = new CancellationTokenSource();

        // Start Kestrel on a background thread — main STA thread is reserved for WinForms
        Task.Run(() => app.RunAsync("http://127.0.0.1:17289", cts.Token));

        // Run WinForms message loop on main STA thread (required for TWAIN + NotifyIcon)
        var fileManager = app.Services.GetRequiredService<TempFileManager>();
        var trayContext = new TrayApplicationContext(cts, fileManager);
        Application.Run(trayContext);

        // Graceful shutdown after tray exits
        if (!cts.IsCancellationRequested)
            cts.Cancel();
        app.StopAsync().GetAwaiter().GetResult();
    }
}
