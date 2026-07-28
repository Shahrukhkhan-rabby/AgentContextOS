using AgentContextOS.Configurations;
using AgentContextOS.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration ---
builder.Services.Configure<AcosOptions>(
    builder.Configuration.GetSection(AcosOptions.SectionName));

var acosOptions = builder.Configuration
    .GetSection(AcosOptions.SectionName)
    .Get<AcosOptions>() ?? new AcosOptions();

// --- Source-Generated JSON ---
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Add(AcosJsonSerializerContext.Default));

// --- Service Registration ---
builder.Services
    .AddAcosPersistence(acosOptions)
    .AddAcosEmbeddings(acosOptions)
    .AddAcosServices()
    .AddAcosMcp();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- Startup Initialization ---
await app.InitializeDatabaseAsync();

// --- Pipeline & Endpoints ---
app.UseAcosPipeline();
app.MapAcosEndpoints();

app.Run();