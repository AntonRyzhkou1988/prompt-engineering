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
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Content { get; set; }

    /// <summary>
    /// The contents of the message.
    /// </summary>
    [JsonPropertyName("custom_content")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CustomContent? CustomContent { get; set; }

    /// <summary>
    /// When <see cref="Role"/> is <see cref="Role.Tool"/>, the id of the tool call this message answers.
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToolCallId { get; set; }

    /// <summary>
    /// When the assistant requests tool invocations (OpenAI <c>tool_calls</c>).
    /// </summary>
    [JsonPropertyName("tool_calls")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ChatToolCall>? ToolCalls { get; set; }
}
