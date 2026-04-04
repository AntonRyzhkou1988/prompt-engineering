using System.Net.Http.Headers;
using System.Text.Json;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.LLM;

public interface IAiService
{
    /// <summary>
    ///  Completes a chat request using the specified instance name and request parameters.
    /// </summary>
    /// <param name="instanceName">The name of the instance to use for the request.</param>
    /// <param name="request">The chat request parameters.</param>
    /// <param name="mediaType">The media type of the request.</param>
    /// <param name="options">The JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chat completion result.</returns>
    Task<ChatCompletion?> CompleteChatAsync(string instanceName, ChatRequest request, MediaTypeHeaderValue? mediaType,
        JsonSerializerOptions? options, CancellationToken cancellationToken);

    /// <summary>
    /// Creates embeddings for the given inputs (batch supported). Uses <see cref="InstanceSettings.EmbeddingDeployment"/>
    /// when set; otherwise <see cref="InstanceSettings.Deployment"/>.
    /// </summary>
    Task<EmbeddingResponse?> CreateEmbeddingsAsync(
        string instanceName,
        EmbeddingRequest request,
        MediaTypeHeaderValue? mediaType,
        JsonSerializerOptions? options,
        CancellationToken cancellationToken);

    /// <summary>
    /// Uploads a file to the specified instance.
    /// </summary>
    /// <param name="instanceName">The name of the instance to upload the file to.</param>
    /// <param name="stream">The stream to upload.</param>
    /// <param name="contentType">The content type of the file ("application/pdf", "image/jpeg").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The uploaded file attachment.</returns>
    Task<Attachment> UploadFileAsync(string instanceName, Stream stream, string contentType, CancellationToken cancellationToken);

    /// <summary>
    /// Remove the file from the OpenAI system.
    /// Even if the file is not removed, we don't care.
    /// </summary>
    /// <param name="instanceName">Instance of AiService</param>
    /// <param name="attachment">Attachment object that was received during file upload. In truth, we need only URL. But for simplicity, use whole object.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation should be canceled if the token is triggered.</param>
    /// <returns>Just a Task.</returns>
    Task RemoveFileAsync(string instanceName, Attachment attachment, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the bucket information for the specified instance.
    /// </summary>
    /// <param name="instanceName">The name of the instance to get the bucket for.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bucket response containing bucket ID and app data, or null if unauthorized (401).</returns>
    Task<BucketResponse?> GetBucketAsync(string instanceName, CancellationToken cancellationToken);

    /// <summary>
    /// Gets metadata for files and folders in the specified bucket and path.
    /// </summary>
    /// <param name="instanceName">The name of the instance to use for the request.</param>
    /// <param name="request">The request parameters for getting file metadata.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file metadata response, or null if unauthorized (401).</returns>
    Task<FileMetadataResponse?> GetFileMetadataAsync(
        string instanceName,
        GetFileMetadataRequest request,
        CancellationToken cancellationToken = default);
}
