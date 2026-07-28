using AgentContextOS.Data;
using AgentContextOS.DTOs;
using AgentContextOS.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AgentContextOS.Services;

public interface IContextRetrievalService
{
    Task<List<ContextQueryResultDto>> RetrieveAsync(string query, string projectHash, int limit = 10, CancellationToken ct = default);
}

public sealed class ContextRetrievalService(
    AcosDbContext dbContext,
    IEmbeddingTransformationService embeddingService,
    ILogger<ContextRetrievalService> logger) : IContextRetrievalService
{
    private static readonly TimeSpan TemporalWindow = TimeSpan.FromMinutes(10);

    public async Task<List<ContextQueryResultDto>> RetrieveAsync(
        string query, string projectHash, int limit = 10, CancellationToken ct = default)
    {
        // Step 1: Generate query embedding
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query, ct);

        List<ScoredMatch> seedMatches;

        if (queryEmbedding is not null)
        {
            // Step 2a: Vector search via sqlite-vec KNN
            seedMatches = await VectorSearchAsync(queryEmbedding, projectHash, 5, ct);
        }
        else
        {
            // Step 2b: Fallback to full-text LIKE search
            logger.LogInformation("No embedding available — falling back to text search");
            seedMatches = await TextSearchAsync(query, projectHash, 5, ct);
        }

        if (seedMatches.Count == 0)
            return [];

        // Step 3: Temporal expansion — fetch events ±10 min from each seed match
        var expandedEvents = await TemporalExpansionAsync(seedMatches, projectHash, ct);

        // Step 4: Rank by weighted combination of similarity and recency
        var ranked = RankResults(expandedEvents, limit);

        return ranked;
    }

    private async Task<List<ScoredMatch>> VectorSearchAsync(
        byte[] queryEmbedding, string projectHash, int topK, CancellationToken ct)
    {
        try
        {
            // KNN search on the vec virtual table, then filter by project hash via join
            var results = await dbContext.Database
                .SqlQuery<VecSearchRow>($"""
                    SELECT v.id AS Id, v.distance AS Distance
                    FROM vec_context_events v
                    WHERE v.embedding MATCH {queryEmbedding}
                        AND k = {topK * 3}
                    ORDER BY v.distance
                    LIMIT {topK * 3}
                    """)
                .ToListAsync(ct);

            // Join back to main table and filter by project hash
            var ids = results.Select(r => Guid.Parse(r.Id)).ToList();
            var events = await dbContext.ContextEvents
                .AsNoTracking()
                .Where(e => ids.Contains(e.Id) && e.ProjectHash == projectHash)
                .ToListAsync(ct);

            return events
                .Select(e =>
                {
                    var distance = results.First(r => r.Id == e.Id.ToString()).Distance;
                    var similarity = 1.0 - distance; // cosine distance → similarity
                    return new ScoredMatch(e, Math.Max(0, similarity));
                })
                .OrderByDescending(m => m.Similarity)
                .Take(topK)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Vector search failed — falling back to text search");
            return await TextSearchAsync("", projectHash, topK, ct);
        }
    }

    private async Task<List<ScoredMatch>> TextSearchAsync(
        string query, string projectHash, int topK, CancellationToken ct)
    {
        var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var queryable = dbContext.ContextEvents
            .AsNoTracking()
            .Where(e => e.ProjectHash == projectHash);

        if (keywords.Length > 0)
        {
            // Match any keyword
            queryable = queryable.Where(e =>
                keywords.Any(k => EF.Functions.Like(e.Content, "%" + k + "%")));
        }

        var events = await queryable
            .OrderByDescending(e => e.Id) // Avoid SQLite DateTimeOffset ORDER BY limitation
            .Take(topK)
            .ToListAsync(ct);

        // Sort by timestamp on the client side
        events = events.OrderByDescending(e => e.Timestamp).ToList();

        // Assign a basic similarity score based on keyword overlap
        return events.Select(e =>
        {
            var score = keywords.Length > 0
                ? (double)keywords.Count(k => e.Content.Contains(k, StringComparison.OrdinalIgnoreCase)) / keywords.Length
                : 0.5;
            return new ScoredMatch(e, score);
        }).ToList();
    }

    private async Task<List<ScoredMatch>> TemporalExpansionAsync(
        List<ScoredMatch> seeds, string projectHash, CancellationToken ct)
    {
        var allMatches = new Dictionary<Guid, ScoredMatch>();

        // Add seed matches
        foreach (var seed in seeds)
            allMatches[seed.Event.Id] = seed;

        // Load all project events once for temporal matching (client-side filter
        // because SQLite doesn't support DateTimeOffset comparisons in WHERE)
        var projectEvents = await dbContext.ContextEvents
            .AsNoTracking()
            .Where(e => e.ProjectHash == projectHash)
            .ToListAsync(ct);

        // Expand temporally around each seed
        foreach (var seed in seeds)
        {
            var windowStart = seed.Event.Timestamp - TemporalWindow;
            var windowEnd = seed.Event.Timestamp + TemporalWindow;

            var temporalNeighbors = projectEvents
                .Where(e => e.Timestamp >= windowStart
                    && e.Timestamp <= windowEnd
                    && e.Id != seed.Event.Id);

            foreach (var neighbor in temporalNeighbors)
            {
                if (!allMatches.ContainsKey(neighbor.Id))
                {
                    // Temporal neighbors get a decay based on time distance from seed
                    var timeDelta = Math.Abs((neighbor.Timestamp - seed.Event.Timestamp).TotalMinutes);
                    var temporalScore = seed.Similarity * (1.0 - timeDelta / TemporalWindow.TotalMinutes);
                    allMatches[neighbor.Id] = new ScoredMatch(neighbor, Math.Max(0, temporalScore));
                }
            }
        }

        return allMatches.Values.ToList();
    }

    private static List<ContextQueryResultDto> RankResults(List<ScoredMatch> matches, int limit)
    {
        var now = DateTimeOffset.UtcNow;

        return matches
            .Select(m =>
            {
                var hoursSince = Math.Max(0, (now - m.Event.Timestamp).TotalHours);
                var recencyDecay = 1.0 / (1.0 + hoursSince);
                var finalScore = 0.7 * m.Similarity + 0.3 * recencyDecay;

                return new ContextQueryResultDto(
                    m.Event.Id,
                    m.Event.Type,
                    m.Event.Content,
                    m.Event.Timestamp,
                    m.Event.Metadata,
                    Math.Round(finalScore, 4));
            })
            .OrderByDescending(r => r.Score)
            .Take(limit)
            .ToList();
    }

    private sealed record ScoredMatch(ContextEvent Event, double Similarity);

    // Row type for the raw SQL vector search query
    private sealed class VecSearchRow
    {
        public string Id { get; set; } = "";
        public double Distance { get; set; }
    }
}
