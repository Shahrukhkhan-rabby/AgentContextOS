using AgentContextOS.Configurations;
using Microsoft.Extensions.Options;

namespace AgentContextOS.Workers;

public sealed class GitPulseWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<AcosOptions> options,
    ILogger<GitPulseWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(options.Value.GitPollIntervalMinutes);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("GitPulseWorker started — polling every {Minutes} minutes",
            options.Value.GitPollIntervalMinutes);

        // Small delay on startup to let the rest of the app initialize
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var gitService = scope.ServiceProvider.GetRequiredService<Services.IGitIngestionService>();

                var repoPath = Directory.GetCurrentDirectory();
                var count = await gitService.SyncRepositoryAsync(repoPath, stoppingToken);

                if (count > 0)
                    logger.LogInformation("GitPulseWorker ingested {Count} new commits", count);
                else
                    logger.LogDebug("GitPulseWorker: no new commits");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GitPulseWorker encountered an error during sync");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        logger.LogInformation("GitPulseWorker stopped");
    }
}
