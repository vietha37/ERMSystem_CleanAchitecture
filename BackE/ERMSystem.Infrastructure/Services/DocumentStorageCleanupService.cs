using ERMSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ERMSystem.Infrastructure.Services;

public class DocumentStorageCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHospitalDocumentStorageService _documentStorageService;
    private readonly LocalDocumentStorageOptions _options;
    private readonly BackgroundWorkerHealthRegistry _workerHealthRegistry;
    private readonly ILogger<DocumentStorageCleanupService> _logger;

    public DocumentStorageCleanupService(
        IServiceScopeFactory serviceScopeFactory,
        IHospitalDocumentStorageService documentStorageService,
        IOptions<LocalDocumentStorageOptions> options,
        BackgroundWorkerHealthRegistry workerHealthRegistry,
        ILogger<DocumentStorageCleanupService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _documentStorageService = documentStorageService;
        _options = options.Value;
        _workerHealthRegistry = workerHealthRegistry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = Math.Max(1, _options.CleanupIntervalHours);
        _logger.LogInformation("Khoi dong worker document storage cleanup.");
        _workerHealthRegistry.Report("document-storage-cleanup", "Starting", "Worker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromHours(intervalHours));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker cleanup document storage gap loi khong mong muon.");
                _workerHealthRegistry.Report("document-storage-cleanup", "Unhealthy", ex.Message, errorAtUtc: DateTime.UtcNow);
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var encounterRepository = scope.ServiceProvider.GetRequiredService<IHospitalEncounterRepository>();
        var referencedUris = await encounterRepository.GetReferencedAttachmentUrisAsync(ct);
        var deletedCount = await _documentStorageService.CleanupOrphanedFilesAsync(referencedUris, ct);

        _workerHealthRegistry.Report(
            "document-storage-cleanup",
            "Healthy",
            $"Cleanup completed. deletedOrphanFiles={deletedCount}.",
            successAtUtc: DateTime.UtcNow);
    }
}
