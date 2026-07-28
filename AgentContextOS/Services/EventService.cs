using AgentContextOS.Data;
using AgentContextOS.DTOs;
using AgentContextOS.Models;
using AgentContextOS.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AgentContextOS.Services;

public interface IEventService
{
    Task<EventDto> IngestAsync(CreateEventRequestDto request, string? projectPath, CancellationToken ct = default);
    Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

public sealed class EventService(
    IEventRepository repository,
    IProjectHashService projectHashService,
    IEmbeddingTransformationService embeddingService,
    AcosDbContext dbContext,
    ILogger<EventService> logger) : IEventService
{
    public async Task<EventDto> IngestAsync(CreateEventRequestDto request, string? projectPath, CancellationToken ct = default)
    {
        var projectHash = projectHashService.ComputeHash(projectPath);

        var embedding = await embeddingService.GenerateEmbeddingAsync(request.Content, ct);

        var entity = new ContextEvent
        {
            Content = request.Content,
            Type = request.Type,
            Metadata = request.Metadata,
            ProjectHash = projectHash,
            Embedding = embedding
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        // Insert into sqlite-vec virtual table for KNN search
        if (embedding is not null)
        {
            try
            {
                await dbContext.Database.ExecuteSqlAsync(
                    $"INSERT INTO vec_context_events(id, embedding) VALUES ({entity.Id.ToString()}, {embedding})",
                    ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to insert embedding into vector index for event {EventId}", entity.Id);
            }
        }

        logger.LogInformation("Ingested {EventType} event {EventId} for project {ProjectHash} (embedded: {HasEmbedding})",
            entity.Type, entity.Id, projectHash, embedding is not null);

        return ToDto(entity);
    }

    public async Task<EventDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        return entity is null ? null : ToDto(entity);
    }

    private static EventDto ToDto(ContextEvent e) =>
        new(e.Id, e.Type, e.Content, e.Timestamp, e.Metadata, e.ProjectHash);
}
