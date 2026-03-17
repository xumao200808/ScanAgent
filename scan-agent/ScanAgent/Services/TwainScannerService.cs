using NTwain;
using NTwain.Data;
using ScanAgent.Models;
using ScanAgent.Utils;
using System.Reflection;

namespace ScanAgent.Services;

public class TwainScannerService : IScannerService
{
    private TwainSession? _session;
    private readonly TempFileManager _fileManager;
    private bool _initialized = false;

    public TwainScannerService(TempFileManager fileManager)
    {
        _fileManager = fileManager;
        InitializeSession();
    }

    private void InitializeSession()
    {
        try
        {
            var appId = TWIdentity.CreateFromAssembly(
                DataGroups.Image,
                Assembly.GetExecutingAssembly()
            );
            _session = new TwainSession(appId);
            _session.Open();
            _initialized = true;
            Console.WriteLine("[TWAIN] Session initialized successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TWAIN] Initialization failed: {ex.Message}");
            _initialized = false;
        }
    }

    public List<ScannerInfo> GetAvailableScanners()
    {
        if (!_initialized || _session == null)
        {
            Console.WriteLine("[TWAIN] Session not initialized");
            return new List<ScannerInfo>();
        }

        var scanners = new List<ScannerInfo>();
        int index = 0;
        foreach (var source in _session)
        {
            scanners.Add(new ScannerInfo
            {
                Id = $"scanner_{index}",
                Name = source.Name,
                Default = index == 0
            });
            index++;
        }

        Console.WriteLine($"[TWAIN] Found {scanners.Count} scanner(s)");
        return scanners;
    }

    public async Task<ScanResult> ScanAsync(ScanRequest request)
    {
        if (!_initialized || _session == null)
        {
            throw new Exception("TWAIN session not initialized");
        }

        var source = FindScanner(request.ScannerId);
        if (source == null)
            throw new ScannerNotFoundException();

        source.Open();

        try
        {
            SetScanParameters(source, request);

            var images = new List<ImageInfo>();
            var scanId = $"scan_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
            var scanCompleted = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            EventHandler<TransferReadyEventArgs> transferReadyHandler = (sender, e) =>
            {
                Console.WriteLine("[TWAIN] Transfer ready");
            };

            EventHandler<DataTransferredEventArgs> dataTransferredHandler = (sender, e) =>
            {
                try
                {
                    using var stream = e.GetNativeImageStream();
                    if (stream != null)
                    {
                        var imageId = _fileManager.SaveImage(scanId, stream);
                        images.Add(new ImageInfo
                        {
                            Id = imageId
                        });
                        Console.WriteLine($"[TWAIN] Image saved: {imageId}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TWAIN] Error saving image: {ex.Message}");
                }
            };

            EventHandler<TransferErrorEventArgs> transferErrorHandler = (sender, e) =>
            {
                Console.WriteLine($"[TWAIN] Transfer error");
                scanCompleted.TrySetException(new Exception("Scan failed"));
            };

            EventHandler sourceDisabledHandler = (sender, e) =>
            {
                Console.WriteLine("[TWAIN] Source disabled - scan completed");
                scanCompleted.TrySetResult(true);
            };

            _session.TransferReady += transferReadyHandler;
            _session.DataTransferred += dataTransferredHandler;
            _session.TransferError += transferErrorHandler;
            _session.SourceDisabled += sourceDisabledHandler;

            try
            {
                Console.WriteLine("[TWAIN] Starting scan...");
                source.Enable(SourceEnableMode.NoUI, false, IntPtr.Zero);

                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2), timeoutCts.Token);
                var completedTask = await Task.WhenAny(scanCompleted.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    Console.WriteLine("[TWAIN] Scan timeout after 2 minutes");
                    throw new TimeoutException("Scan operation timed out after 2 minutes");
                }

                return new ScanResult
                {
                    ScanId = scanId,
                    Status = "completed",
                    Images = images
                };
            }
            finally
            {
                timeoutCts.Dispose();
                _session.TransferReady -= transferReadyHandler;
                _session.DataTransferred -= dataTransferredHandler;
                _session.TransferError -= transferErrorHandler;
                _session.SourceDisabled -= sourceDisabledHandler;
            }
        }
        finally
        {
            source.Close();
        }
    }

    private void SetScanParameters(DataSource source, ScanRequest request)
    {
        try
        {
            source.Capabilities.ICapXResolution.SetValue((float)request.Dpi);
            source.Capabilities.ICapYResolution.SetValue((float)request.Dpi);
            Console.WriteLine($"[TWAIN] DPI set to {request.Dpi}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TWAIN] Failed to set DPI: {ex.Message}");
        }

        try
        {
            var pixelType = request.ColorMode switch
            {
                "color" => PixelType.RGB,
                "gray" => PixelType.Gray,
                "bw" => PixelType.BlackWhite,
                _ => PixelType.Gray
            };
            source.Capabilities.ICapPixelType.SetValue(pixelType);
            Console.WriteLine($"[TWAIN] Color mode set to {request.ColorMode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TWAIN] Failed to set color mode: {ex.Message}");
        }

        try
        {
            if (request.Duplex)
            {
                source.Capabilities.CapDuplexEnabled.SetValue(BoolType.True);
                Console.WriteLine("[TWAIN] Duplex enabled");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TWAIN] Failed to set duplex: {ex.Message}");
        }

        try
        {
            if (request.AutoFeed)
            {
                source.Capabilities.CapFeederEnabled.SetValue(BoolType.True);
                source.Capabilities.CapAutoFeed.SetValue(BoolType.True);
                Console.WriteLine("[TWAIN] Auto feed enabled");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TWAIN] Failed to set auto feed: {ex.Message}");
        }

        try
        {
            Console.WriteLine($"[TWAIN] Paper size requested: {request.PaperSize}");
            
            var paperSize = request.PaperSize.ToLowerInvariant();
            var supportedSizes = source.Capabilities.ICapSupportedSizes;
            
            if (supportedSizes != null && supportedSizes.IsSupported)
            {
                var sizeEnum = paperSize switch
                {
                    "a4" => SupportedSize.A4,
                    "a3" => SupportedSize.A3,
                    "letter" => SupportedSize.USLetter,
                    "legal" => SupportedSize.USLegal,
                    _ => SupportedSize.None
                };
                
                if (sizeEnum != SupportedSize.None)
                {
                    supportedSizes.SetValue(sizeEnum);
                    Console.WriteLine($"[TWAIN] Paper size set to {request.PaperSize}");
                }
                else
                {
                    Console.WriteLine($"[TWAIN] Paper size {request.PaperSize} not mapped to TWAIN enum");
                }
            }
            else
            {
                Console.WriteLine("[TWAIN] Paper size setting not supported by scanner");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TWAIN] Failed to set paper size: {ex.Message}");
        }
    }

    private DataSource? FindScanner(string? scannerId)
    {
        if (_session == null)
            return null;

        if (string.IsNullOrEmpty(scannerId))
            return _session.FirstOrDefault();

        int index = 0;
        foreach (var source in _session)
        {
            if ($"scanner_{index}" == scannerId)
                return source;
            index++;
        }
        return null;
    }
}