using AgentContextOS.Configurations;
using AgentContextOS.Data;
using AgentContextOS.Mcp;
using AgentContextOS.Repositories;
using AgentContextOS.Services;
using AgentContextOS.Workers;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using OllamaSharp;

namespace AgentContextOS.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAcosPersistence(
        this IServiceCollection services, AcosOptions options)
    {
        var dbDirectory = Path.GetDirectoryName(options.DatabasePath)!;
        Directory.CreateDirectory(dbDirectory);

        services.AddDbContext<AcosDbContext>(opt =>
            opt.UseSqlite($"Data Source={options.DatabasePath}"));

        return services;
    }

    public static IServiceCollection AddAcosServices(this IServiceCollection services)
    {
        services.AddSingleton<IProjectHashService, ProjectHashService>();
        services.AddScoped<IEmbeddingTransformationService, EmbeddingTransformationService>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IGitIngestionService, GitIngestionService>();
        services.AddScoped<IContextRetrievalService, ContextRetrievalService>();
        services.AddSingleton<ITokenBudgetService, TokenBudgetService>();
        services.AddScoped<IContextComposerService, ContextComposerService>();
        services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();
        services.AddHostedService<GitPulseWorker>();

        return services;
    }

    public static IServiceCollection AddAcosEmbeddings(
        this IServiceCollection services, AcosOptions options)
    {
        try
        {
            var client = new OllamaApiClient(new Uri(options.OllamaUrl), options.EmbeddingModel);
            services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(client);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Ollama client init failed ({ex.Message}) — embeddings disabled");
        }

        return services;
    }

    public static IServiceCollection AddAcosMcp(this IServiceCollection services)
    {
        services.AddMcpServer(options =>
        {
            options.ServerInfo = new() { Name = "AgentContextOS", Version = "1.0.0" };
            options.ServerInstructions =
                "Engineering memory layer for AI agents. " +
                "Call GetMemory before starting any task to retrieve relevant past decisions. " +
                "Call RecordSession after completing a task to archive what was done and why.";
        })
        .WithHttpTransport()
        .WithTools<McpContextTools>();

        return services;
    }
}
