using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;

public record FileUploadResponse
{
    /// <summary>
    /// Name of the file that was uploaded.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// Url of the file in the OpenAI system.
    /// It build as "file/{BucketId}/{folderName}/{fileName}".
    /// </summary>
    /// <example>files/Gd5YKyPA568W54QzwrxdTxF3QUedfX5geTsqvYRRSwb/IncomingDocuments/file_3421</example>>
    [JsonPropertyName("url")]
    public required string Url { get; set; }

    /// <summary>
    /// Contant type of the file that was uploaded.
    /// </summary>
    /// <example>image/jpeg</example>>
    [JsonPropertyName("contentType")]
    public required string ContentType { get; set; }
}
