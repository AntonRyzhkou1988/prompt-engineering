using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;

/// <summary>
///     Represents a metadata item within a folder.
/// </summary>
public record FileMetadataItem
{
    /// <summary>
    ///     Gets or sets the name of the file or folder.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    ///     Gets or sets the parent path of the file or folder.
    /// </summary>
    [JsonPropertyName("parentPath")]
    public string? ParentPath { get; set; }

    /// <summary>
    ///     Gets or sets the bucket ID.
    /// </summary>
    [JsonPropertyName("bucket")]
    public required string Bucket { get; set; }

    /// <summary>
    ///     Gets or sets the URL path to the file or folder.
    /// </summary>
    [JsonPropertyName("url")]
#pragma warning disable CA1056 // URI-like properties should not be strings
    public required string Url { get; set; }
#pragma warning restore CA1056 // URI-like properties should not be strings

    /// <summary>
    ///     Gets or sets the node type (e.g., "FOLDER", "ITEM").
    /// </summary>
    [JsonPropertyName("nodeType")]
    public required string NodeType { get; set; }

    /// <summary>
    ///     Gets or sets the resource type (e.g., "FILE").
    /// </summary>
    [JsonPropertyName("resourceType")]
    public required string ResourceType { get; set; }

    /// <summary>
    ///     Gets or sets the permissions applicable to the requestor.
    /// </summary>
    [JsonPropertyName("permissions")]
    public IEnumerable<string>? Permissions { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the item was last updated (milliseconds since epoch).
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public long? UpdatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the timestamp when the item was created (milliseconds since epoch).
    /// </summary>
    [JsonPropertyName("createdAt")]
    public long? CreatedAt { get; set; }

    /// <summary>
    ///     Gets or sets the content length in bytes (only for files).
    /// </summary>
    [JsonPropertyName("contentLength")]
    public long? ContentLength { get; set; }

    /// <summary>
    ///     Gets or sets the content type (only for files).
    /// </summary>
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }
}

