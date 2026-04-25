using System.Text.Json;
using System.Text.Json.Serialization;

namespace PromptEngineering.LLM.Models;

/// <summary>OpenAI-style tool definition for chat completions (<c>tools</c> array).</summary>
public sealed class ChatToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public ChatToolFunctionDefinition Function { get; set; } = null!;
}

public sealed class ChatToolFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>JSON Schema for tool parameters (object schema).</summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? Parameters { get; set; }
}

/// <summary>Assistant message tool call entry (<c>tool_calls[]</c>).</summary>
public sealed class ChatToolCall
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("function")]
    public ChatToolCallFunction? Function { get; set; }
}

public sealed class ChatToolCallFunction
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arguments")]
    public string? Arguments { get; set; }
}
