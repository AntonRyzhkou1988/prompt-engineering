namespace PromptEngineering.LLM.Models;

/// <summary>
///     Request parameters for getting file metadata from a DIAL bucket.
/// </summary>
public record GetFileMetadataRequest
{
    /// <summary>
    ///     Gets or sets the target bucket ID.
    /// </summary>
    public required string Bucket { get; init; }

    /// <summary>
    ///     Gets or sets the path to the requested directory or file (e.g., "folder1/folder2/").
    ///     Can be empty or null for root folder.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    ///     Gets or sets the optional pagination token from the previous request to request next items.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    ///     Gets or sets the limit on the number of items in the response (1-1000, default: 100).
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether to return items recursively without nested folder metadata (default: false).
    /// </summary>
    public bool? Recursive { get; init; }

    /// <summary>
    ///     Gets or sets a value indicating whether to return the permissions applicable to the requestor (default: false).
    /// </summary>
    public bool? Permissions { get; init; }
}

