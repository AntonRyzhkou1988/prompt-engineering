using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;
public record BucketResponse
{
    /// <summary>
    /// Unique ID of the bucket.
    /// This ID is used to reference the bucket in subsequent API calls.
    /// </summary>
    [JsonPropertyName("bucket")]
    public required string BucketId { get; set; }
}
