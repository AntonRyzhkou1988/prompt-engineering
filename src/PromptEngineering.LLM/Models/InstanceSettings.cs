namespace PromptEngineering.LLM.Models;

public record InstanceSettings
{
    public string Name { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public string Deployment { get; set; } = null!;

    /// <summary>
    /// Optional deployment name for embeddings when it differs from <see cref="Deployment"/> (chat).
    /// </summary>
    public string? EmbeddingDeployment { get; set; }
}