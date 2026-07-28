using System.Text.Json;
using AgentContextOS.Data;
using AgentContextOS.DTOs;
using AgentContextOS.Models;
using AgentContextOS.Services;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;

namespace AgentContextOS.Services;

public interface IGitIngestionService
{
    Task<int> SyncRepositoryAsync(string repoPath, CancellationToken ct = default);
}

public sealed class GitIngestionService(
    IServiceScopeFactory scopeFactory,
    IProjectHashService projectHashService,
    ILogger<GitIngestionService> logger) : IGitIngestionService
{
    public async Task<int> SyncRepositoryAsync(string repoPath, CancellationToken ct = default)
    {
        var resolvedPath = Repository.Discover(repoPath);
        if (string.IsNullOrEmpty(resolvedPath))
        {
            logger.LogWarning("No Git repository found at {Path}", repoPath);
            return 0;
        }

        var projectHash = projectHashService.ComputeHash(repoPath);

        using var repo = new Repository(resolvedPath);
        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AcosDbContext>();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingTransformationService>();

        var syncState = await dbContext.Set<SyncState>()
            .FirstOrDefaultAsync(s => s.ProjectHash == projectHash, ct);

        var commits = GetNewCommits(repo, syncState?.LastCommitSha);

        if (commits.Count == 0)
        {
            logger.LogDebug("No new commits for project {ProjectHash}", projectHash);
            return 0;
        }

        var ingested = 0;

        foreach (var commit in commits)
        {
            if (ct.IsCancellationRequested) break;

            // Idempotency: skip if commit SHA already exists
            var sha = commit.Sha;
            var exists = await dbContext.ContextEvents
                .AnyAsync(e => e.ProjectHash == projectHash
                    && e.Type == ContextEventType.Commit
                    && e.Metadata != null
                    && e.Metadata.Contains(sha), ct);

            if (exists) continue;

            var metadata = JsonSerializer.Serialize(new
            {
                sha = commit.Sha,
                author = commit.Author.Name,
                email = commit.Author.Email,
                authoredAt = commit.Author.When,
                files = GetChangedFiles(repo, commit)
            });

            var content = $"[{commit.Sha[..7]}] {commit.MessageShort}\n\n{commit.Message}";

            var embedding = await embeddingService.GenerateEmbeddingAsync(content, ct);

            var entity = new ContextEvent
            {
                Type = ContextEventType.Commit,
                Content = content,
                Timestamp = commit.Author.When,
                Metadata = metadata,
                ProjectHash = projectHash,
                Embedding = embedding
            };

            await dbContext.ContextEvents.AddAsync(entity, ct);

            // Insert into vector index if embedding was generated
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
                    logger.LogWarning(ex, "Failed to insert vector for commit {Sha}", sha);
                }
            }

            ingested++;
        }

        await dbContext.SaveChangesAsync(ct);

        // Update sync state
        var latestSha = commits[0].Sha;
        if (syncState is null)
        {
            await dbContext.Set<SyncState>().AddAsync(new SyncState
            {
                ProjectHash = projectHash,
                LastCommitSha = latestSha,
                LastSyncedAt = DateTimeOffset.UtcNow
            }, ct);
        }
        else
        {
            syncState.LastCommitSha = latestSha;
            syncState.LastSyncedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("Synced {Count} new commits for project {ProjectHash}", ingested, projectHash);
        return ingested;
    }

    private static List<Commit> GetNewCommits(Repository repo, string? lastSha)
    {
        var commits = new List<Commit>();

        foreach (var commit in repo.Commits)
        {
            if (commit.Sha == lastSha) break;
            commits.Add(commit);
        }

        return commits;
    }

    private static List<string> GetChangedFiles(Repository repo, Commit commit)
    {
        try
        {
            if (commit.Parents.Any())
            {
                var parent = commit.Parents.First();
                var changes = repo.Diff.Compare<TreeChanges>(parent.Tree, commit.Tree);
                return changes.Select(c => c.Path).Take(50).ToList();
            }

            // Root commit — list all tree entries
            return commit.Tree
                .Select(e => e.Path)
                .Take(50)
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
