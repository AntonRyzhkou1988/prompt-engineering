using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;
public record ChatMessage
{
    /// <summary>
    /// The role of the author of this message.
    /// </summary>
    [JsonPropertyName("role")]
    public Role Role { get; set; }

    /// <summary>
    /// The contents of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// The contents of the message.
    /// </summary>
    [JsonPropertyName("custom_content")]
    public CustomContent? CustomContent { get; set; }
}
