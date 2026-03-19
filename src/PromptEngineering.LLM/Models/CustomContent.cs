using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;
public record CustomContent
{
    /// <summary>
    /// Array of attachments related to the chat message.
    /// </summary>
    [JsonPropertyName("attachments")]
    public Attachment[] Attachments { get; set; } = Array.Empty<Attachment>();
}
