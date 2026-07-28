using AgentContextOS.Data;
using AgentContextOS.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentContextOS.Repositories;

public interface IEventRepository
{
    Task<ContextEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ContextEvent>> GetByProjectHashAsync(string projectHash, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(ContextEvent contextEvent, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public sealed class EventRepository(AcosDbContext db) : IEventRepository
{
    public async Task<ContextEvent?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.ContextEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<List<ContextEvent>> GetByProjectHashAsync(
        string projectHash, int page, int pageSize, CancellationToken ct = default) =>
        await db.ContextEvents
            .AsNoTracking()
            .Where(e => e.ProjectHash == projectHash)
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public async Task AddAsync(ContextEvent contextEvent, CancellationToken ct = default) =>
        await db.ContextEvents.AddAsync(contextEvent, ct);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
