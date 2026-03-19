using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using PromptEngineering.LLM.Exceptions;
using PromptEngineering.LLM.Models;
using Flurl;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace PromptEngineering.LLM;

public class AiService : IAiService
{
    private const string BucketUrlPath = "v1/bucket";
    private const string FilesUrlPath = "v1/files";
    private const string MetadataUrlPath = "v1/metadata/files";
    private const string NameOfTheFieldWithFile = "data";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiService> _logger;
    private readonly AiServiceSettings _settings;
    private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;

    private readonly ConcurrentDictionary<string, BucketResponse> _bucketCache = new();

    private static readonly Action<ILogger, int, Exception?> LogErrorUpload =
        LoggerMessage.Define<int>(
            LogLevel.Error,
            new EventId(1, nameof(AiService)),
            "Error during Stream upload on attempt #{AttemptNumber}");

    public AiService(IHttpClientFactory httpClientFactory, IOptions<AiServiceSettings> settings,
        ILogger<AiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _settings = settings.Value;

        // Specific Retry Policy for Upload. Including "SocketException"
        var exceptionPolicy = Policy<HttpResponseMessage>
        .Handle<HttpRequestException>(ex => ex.InnerException is SocketException)
        .Or<HttpRequestException>(ex => ex.Message.Contains("forcibly closed by the remote host", StringComparison.OrdinalIgnoreCase))
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (ex, timespan, retryAttempt, context) =>
                {
                    LogErrorUpload(_logger, retryAttempt, ex.Exception);
                });
        var resultPolicy = Policy<HttpResponseMessage>
        .HandleResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (ex, timespan, retryAttempt, context) =>
                {
                    LogErrorUpload(_logger, retryAttempt, ex.Exception);
                });
        _retryPolicy = Policy.WrapAsync(exceptionPolicy, resultPolicy);
    }

    /// <inheritdoc />
    public async Task<ChatCompletion?> CompleteChatAsync(string instanceName, ChatRequest request,
        MediaTypeHeaderValue? mediaType,
        JsonSerializerOptions? options, CancellationToken cancellationToken)
    {
        var settings = ExtractInstanceSettings(instanceName);

        var requestUri =
            $"{_settings.SystemName}/{_settings.DeploymentsUrl}/{settings.Deployment}/{_settings.ChatUrl}/{_settings.CompletionsUrl}";
        var content = JsonContent.Create(request, mediaType, options);

        using var openAiClient = _httpClientFactory.CreateClient(instanceName);

        using var response = await openAiClient.PostAsync(requestUri, content, cancellationToken);

        await EnsureSuccessStatusCodeAsync(response, cancellationToken);

        if (request.Stream)
        {
            return await ProcessStreamChatResponseAsync(options, response, cancellationToken);
        }

        var openAiResponse =
            await response.Content.ReadFromJsonAsync<ChatCompletion>(options: null, cancellationToken);

        return openAiResponse;
    }

    /// <inheritdoc />
    public async Task<Attachment> UploadFileAsync(string instanceName, Stream stream, string contentType, CancellationToken cancellationToken)
    {
        var randomFileName = $"file_{Guid.NewGuid()}";

        // Specific Retry for Stream.
        using var fileUploadResponse = await _retryPolicy.ExecuteAsync(async () =>
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using var openAiClient = _httpClientFactory.CreateClient(instanceName);

            // Get BucketId
            var bucketId = await GetBucketId(instanceName, cancellationToken);

            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(streamContent, NameOfTheFieldWithFile, randomFileName);

            // We use "Instance Name" as BucketName
            var fileUploadUri =
                $"{FilesUrlPath}/{bucketId}/{instanceName}/{randomFileName}";
            return await openAiClient.PutAsync(fileUploadUri, content, cancellationToken);

        });

        await EnsureSuccessStatusCodeAsync(fileUploadResponse, cancellationToken);
        var fileResponse =
           await fileUploadResponse.Content.ReadFromJsonAsync<FileUploadResponse>(options: null, cancellationToken);

        // Return parameters
        return new Attachment
        {
            Title = fileResponse!.Name,
            Type = fileResponse.ContentType,
            Url = fileResponse.Url,
        };
    }

    /// <inheritdoc />
    public async Task RemoveFileAsync(string instanceName, Attachment attachment, CancellationToken cancellationToken)
    {
        using var openAiClient = _httpClientFactory.CreateClient(instanceName);

        var fileDeleteUri = $"v1/{attachment.Url}";
        await openAiClient.DeleteAsync(fileDeleteUri, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BucketResponse?> GetBucketAsync(string instanceName, CancellationToken cancellationToken)
    {
        if (_bucketCache.TryGetValue(instanceName, out var cachedResponse))
        {
            return cachedResponse;
        }

        var bucketResponse = await GetAsync<BucketResponse>(instanceName, BucketUrlPath, cancellationToken);

        if (bucketResponse != null && !string.IsNullOrEmpty(bucketResponse.BucketId))
        {
            _bucketCache.TryAdd(instanceName, bucketResponse);
        }

        return bucketResponse;
    }

    /// <inheritdoc />
    public async Task<FileMetadataResponse?> GetFileMetadataAsync(
        string instanceName,
        GetFileMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Bucket);

        var metadataUrl = CreateFileMetadataUrl(request);

        return await GetAsync<FileMetadataResponse>(instanceName, metadataUrl, cancellationToken);
    }

    private static string CreateFileMetadataUrl(GetFileMetadataRequest request)
    {
        var url = MetadataUrlPath.AppendPathSegment(request.Bucket);

        // Append path segment if provided
        if (!string.IsNullOrWhiteSpace(request.Path))
        {
            url.AppendPathSegments(request.Path.Split('/', StringSplitOptions.RemoveEmptyEntries) as object[]);
        }
        else
        {
            // Bucket required, that in the end of URL the trailing '/' is present, otherwise API returns 502
            url.Path += '/';
        }

        // Add query parameters
        if (!string.IsNullOrWhiteSpace(request.Token))
        {
            url.AppendQueryParam("token", request.Token);
        }
        if (request.Limit.HasValue)
        {
            url.AppendQueryParam("limit", request.Limit.Value);
        }
        if (request.Recursive.HasValue)
        {
            url.AppendQueryParam("recursive", request.Recursive.Value.ToString().ToLowerInvariant());
        }
        if (request.Permissions.HasValue)
        {
            url.AppendQueryParam("permissions", request.Permissions.Value.ToString().ToLowerInvariant());
        }

        return url.ToString();
    }

    private InstanceSettings ExtractInstanceSettings(string instanceName)
    {
        ArgumentNullException.ThrowIfNull(instanceName);

        var settings = _settings.Instances.FirstOrDefault(x => x.Name == instanceName);
        if (settings == null)
        {
            throw new OpenAiException("Unregistered instance name.");
        }

        return settings;
    }

    private async Task<string> GetBucketId(string instanceName, CancellationToken cancellationToken)
    {
        // Use GetBucketAsync which now has caching built-in
        var bucketResponse = await GetBucketAsync(instanceName, cancellationToken);

        // Bucket ID is required for attachment manipulations
        if (bucketResponse == null || string.IsNullOrEmpty(bucketResponse.BucketId))
        {
            throw new OpenAiException("Failed to retrieve bucket ID.");
        }

        return bucketResponse.BucketId;
    }

    /// <summary>
    ///     Performs a GET request and deserializes the response.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the response to.</typeparam>
    /// <param name="instanceName">The name of the instance to use for the request.</param>
    /// <param name="requestUri">The URI to request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response.</returns>
    private async Task<T?> GetAsync<T>(string instanceName, string requestUri, CancellationToken cancellationToken)
    {
        using var openAiClient = _httpClientFactory.CreateClient(instanceName);
        using var responseMessage = await openAiClient.GetAsync(requestUri, cancellationToken);

        await EnsureSuccessStatusCodeAsync(responseMessage, cancellationToken);

        return await responseMessage.Content.ReadFromJsonAsync<T>(options: null, cancellationToken);
    }

    private static async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var details = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.StatusCode}). Details: {details}",
                null,
                response.StatusCode);
        }
    }

    private static async Task<ChatCompletion?> ProcessStreamChatResponseAsync(JsonSerializerOptions? options, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();
        // Set to null, as "Thinking" comes in it
        var message = new ChatMessage() { CustomContent = null };
        var choice = new ChatCompletionChoice();
        var completion = new ChatCompletion();
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
            {
                continue;
            }
            var jsonData = line.Substring("data: ".Length).Trim();
            if (jsonData == "[DONE]")
            {
                break;
            }

            var chunk = JsonSerializer.Deserialize<ChatCompletion>(jsonData, options);
            if (chunk?.Choices != null)
            {
                completion.Id ??= chunk.Id;
                completion.Model ??= chunk.Model;
                completion.Object ??= chunk.Object;
                completion.Created ??= chunk.Created;
                completion.ServiceTier ??= chunk.ServiceTier;
                completion.SystemFingerprint ??= chunk.SystemFingerprint;
                completion.Usage ??= chunk.Usage;

                var mainChoice = chunk.Choices.FirstOrDefault();
                choice.Index ??= mainChoice?.Index;
                choice.FinishReason ??= mainChoice?.FinishReason;

                if (mainChoice?.Delta?.Role.HasValue == true)
                {
                    message.Role = mainChoice.Delta.Role.Value;
                }

                sb.Append(mainChoice?.Delta?.Content);
            }
        }

        message.Content = sb.ToString();
        choice.Message = message;
        completion.Choices = [choice];

        return completion;
    }
}
