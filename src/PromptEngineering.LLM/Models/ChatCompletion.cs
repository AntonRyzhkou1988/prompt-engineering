using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models
{
    public record ChatCompletion
    {
        /// <summary>
        /// A unique identifier for the chat completion.
        /// </summary>
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// The object type, which is always chat.completion.
        /// </summary>
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        /// <summary>
        /// The Unix timestamp (in seconds) of when the chat completion was created.
        /// </summary>
        [JsonPropertyName("created")]
        public ulong? Created { get; set; }

        /// <summary>
        /// The model used for the chat completion.
        /// </summary>
        [JsonPropertyName("model")]
        public string? Model { get; set; }

        /// <summary>
        /// A list of chat completion choices. Can be more than one if n is greater than 1.
        /// </summary>
        [JsonPropertyName("choices")]
        public IEnumerable<ChatCompletionChoice>? Choices { get; set; }

        /// <summary>
        /// Usage statistics for the completion request.
        /// </summary>
        [JsonPropertyName("usage")]
        public ChatCompletionUsage? Usage { get; set; }

        /// <summary>
        /// This fingerprint represents the backend configuration that the model runs with.
        /// Can be used in conjunction with the seed request parameter to understand when
        /// backend changes have been made that might impact determinism.
        /// </summary>
        [JsonPropertyName("system_fingerprint")]
        public string? SystemFingerprint { get; set; }

        /// <summary>
        /// The service tier used for processing the request. This field is only included
        /// if the service_tier parameter is specified in the request.
        /// </summary>
        [JsonPropertyName("service_tier")]
        public string? ServiceTier { get; set; }
    }
}
