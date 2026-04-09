using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;

/// <summary>
/// OpenAI/Azure-compatible embeddings response.
/// </summary>
public sealed record EmbeddingResponse
{
    [JsonPropertyName("object")]
    public string? Object { get; init; }

    [JsonPropertyName("data")]
    public IReadOnlyList<EmbeddingDataItem>? Data { get; init; }

    [JsonPropertyName("model")]
    public string? Model { get; init; }

    [JsonPropertyName("usage")]
    public EmbeddingUsage? Usage { get; init; }
}

public sealed record EmbeddingDataItem
{
    [JsonPropertyName("object")]
    public string? Object { get; init; }

    [JsonPropertyName("index")]
    public int Index { get; init; }

    [JsonPropertyName("embedding")]
    public IReadOnlyList<float> Embedding { get; init; } = Array.Empty<float>();
}

public sealed record EmbeddingUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; init; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; init; }
}
