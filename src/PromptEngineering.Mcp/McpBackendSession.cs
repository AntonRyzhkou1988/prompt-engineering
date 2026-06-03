using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.Mcp;

public interface IMcpBackendSession : IAsyncDisposable
{
    IReadOnlyList<McpClientTool> Tools { get; }
    IReadOnlyList<ChatToolDefinition> ToolDefinitions { get; }
    Task<CallToolResult> CallToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default);
}

public sealed class McpBackendSession : IMcpBackendSession
{
    private readonly McpClient _client;

    public McpBackendSession(
        McpClient client,
        IReadOnlyList<McpClientTool> tools,
        IReadOnlyList<ChatToolDefinition> toolDefinitions)
    {
        _client = client;
        Tools = tools;
        ToolDefinitions = toolDefinitions;
    }

    public IReadOnlyList<McpClientTool> Tools { get; }
    public IReadOnlyList<ChatToolDefinition> ToolDefinitions { get; }

    public async Task<CallToolResult> CallToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
    {
        if (!Tools.Any(t => t.Name == toolName))
            throw new InvalidOperationException($"Tool '{toolName}' is not provided by this MCP session.");

        var args = McpChatToolMapper.ParseToolArguments(argumentsJson);
        return await _client.CallToolAsync(toolName, args, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => _client.DisposeAsync();
}
