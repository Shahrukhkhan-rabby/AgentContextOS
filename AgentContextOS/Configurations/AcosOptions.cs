namespace AgentContextOS.Configurations;

public sealed class AcosOptions
{
    public const string SectionName = "Acos";

    public string DatabasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".acos", "acos.db");

    public string OllamaUrl { get; set; } = "http://localhost:11434";

    public string EmbeddingModel { get; set; } = "nomic-embed-text";

    public int TokenBudget { get; set; } = 8192;

    public int GitPollIntervalMinutes { get; set; } = 5;
}
