using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;


public record ChatRequest
{
    private readonly List<ChatMessage> _messages = new();

    /// <summary>
    /// A list of messages comprising the conversation so far.
    /// </summary>
    /// <remarks>Required.</remarks>
    [JsonPropertyName("messages")]
    public IEnumerable<ChatMessage> Messages => _messages;

    /// <summary>
    /// The maximum number of tokens that can be generated in the chat completion.
    /// The total length of input tokens and generated tokens is limited by the model's context length.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int? MaxTokens { get; set; }

    /// <summary>
    /// ID of the model to use. See the model endpoint compatibility
    /// table for details on which models work with the Chat API.
    /// </summary>
    /// <remarks>Required.</remarks>
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    /// <summary>
    /// Set Temperature of the LLM response.
    /// 0 means the model will make the most deterministic choice.
    /// </summary>
    [JsonPropertyName("temperature")]
    public float? Temperature { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the response should be streamed (chunked response) or returned as one model with all information in it.
    /// By default , it is set to false.
    /// </summary>
    [JsonPropertyName("stream")]
    public bool Stream { get; set; } = false;

    // <see cref="ResponseFormat"/>
    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; set; }

    /// <summary>
    /// Tools available to the model (OpenAI chat completions <c>tools</c>).
    /// </summary>
    [JsonPropertyName("tools")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ChatToolDefinition>? Tools { get; set; }

    public void AddMessage(ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        _messages.Add(message);
    }
}
