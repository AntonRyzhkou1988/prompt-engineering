using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;
public record Attachment
{
    /// <summary>
    /// Title of the attachment. Usually just a file name.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Type of the attachment - "Content Type".
    /// Such as "image/jpg", "application/pdf", etc.
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    /// <summary>
    /// Internal path in the OpenAI system.
    /// eg. files/Gd5YKyPA568WQxQzwrxdTxF3QUriajX5geTsqvYRRSwb/IncomingDocuments/file_7087.jpg
    /// </summary>
    [JsonPropertyName("url")]
    public required string Url { get; set; }
}
