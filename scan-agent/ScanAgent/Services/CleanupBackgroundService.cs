using ScanAgent.Utils;

namespace ScanAgent.Services;

public class CleanupBackgroundService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private Task? _cleanupTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public CleanupBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _cleanupTask = RunCleanupAsync(_cancellationTokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }

        if (_cleanupTask != null)
        {
            await Task.WhenAny(_cleanupTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
                
                using var scope = _serviceProvider.CreateScope();
                var fileManager = scope.ServiceProvider.GetRequiredService<TempFileManager>();
                fileManager.CleanupOldScans(TimeSpan.FromHours(24));
                Console.WriteLine("[Cleanup] Old scans cleaned up");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Cleanup] Background service stopped");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Error: {ex.Message}");
            }
        }
    }
}
