using Microsoft.Extensions.AI;

namespace AgentContextOS.Services;

public interface IEmbeddingTransformationService
{
    Task<byte[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
}

public sealed class EmbeddingTransformationService(
    IServiceProvider serviceProvider,
    ILogger<EmbeddingTransformationService> logger) : IEmbeddingTransformationService
{
    public async Task<byte[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var embeddingGenerator = serviceProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();

        if (embeddingGenerator is null)
        {
            logger.LogWarning("No embedding generator configured — skipping embedding");
            return null;
        }

        try
        {
            var result = await embeddingGenerator.GenerateAsync(
                [text], cancellationToken: ct);

            if (result is { Count: > 0 })
            {
                var vector = result[0].Vector;
                logger.LogDebug("Generated embedding with {Dimensions} dimensions", vector.Length);
                return VectorHelper.ToBlob(vector);
            }

            logger.LogWarning("Embedding generator returned empty result for text of length {Length}", text.Length);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to generate embedding — event will be saved without vector");
            return null;
        }
    }
}
