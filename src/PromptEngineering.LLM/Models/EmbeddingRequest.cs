using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;

/// <summary>
/// OpenAI/Azure-compatible embeddings request. Supports batch <c>input</c> as a JSON array.
/// </summary>
public sealed record EmbeddingRequest
{
    /// <summary>
    /// Text inputs to embed; order is preserved in the response <c>data[].index</c>.
    /// </summary>
    [JsonPropertyName("input")]
    public required IReadOnlyList<string> Input { get; init; }

    /// <summary>
    /// Optional model id; often omitted when the deployment name in the URL selects the model.
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }
}
