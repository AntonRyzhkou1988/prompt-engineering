using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace PromptEngineering.Mcp;

public static class McpCallToolResultFormatter
{
    public static string ToModelText(CallToolResult result)
    {
        if (result.IsError == true)
        {
            var err = ToPlainText(result);
            return string.IsNullOrWhiteSpace(err) ? "Tool error (no details)." : $"Tool error: {err}";
        }

        var text = ToPlainText(result);
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        if (result.StructuredContent is JsonElement e)
            return e.GetRawText();

        return JsonSerializer.Serialize(result);
    }

    private static string ToPlainText(CallToolResult result)
    {
        if (result.Content is not { Count: > 0 })
            return string.Empty;

        var parts = new List<string>();
        foreach (var block in result.Content)
        {
            if (block is TextContentBlock text)
                parts.Add(text.Text);
            else
                parts.Add(block.ToString() ?? string.Empty);
        }

        return string.Join("\n", parts.Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}
