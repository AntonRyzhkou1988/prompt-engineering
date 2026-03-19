using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;

public class JsonSchema
{
    /// <summary>
    /// Description of the JSON schema.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Name of the JSON schema.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The JSON schema object.
    /// </summary>
    [JsonPropertyName("schema")]
    public object? Schema { get; set; }

    /// <summary>
    /// Option 'strict' of the JSON schema.
    /// </summary>
    [JsonPropertyName("strict")]
    public bool Strict { get; set; }
}
