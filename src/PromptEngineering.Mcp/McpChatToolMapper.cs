using System.Text.Json;
using ModelContextProtocol.Client;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.Mcp;

public static class McpChatToolMapper
{
    public static List<ChatToolDefinition> ToDefinitions(IReadOnlyList<McpClientTool> tools)
    {
        var list = new List<ChatToolDefinition>(tools.Count);
        foreach (var t in tools)
            list.Add(ToChatToolDefinition(t));
        return list;
    }

    public static ChatToolDefinition ToChatToolDefinition(McpClientTool t)
    {
        JsonElement? parameters = t.JsonSchema is { } schema ? schema : JsonDocument.Parse("{}").RootElement;
        return new ChatToolDefinition
        {
            Function = new ChatToolFunctionDefinition
            {
                Name = t.Name,
                Description = t.Description,
                Parameters = parameters
            }
        };
    }

    public static IReadOnlyDictionary<string, object?>? ParseToolArguments(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}")
            return null;

        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (dict is null)
                return null;
            return dict.ToDictionary(kv => kv.Key, kv => (object?)kv.Value);
        }
        catch
        {
            return null;
        }
    }
}
