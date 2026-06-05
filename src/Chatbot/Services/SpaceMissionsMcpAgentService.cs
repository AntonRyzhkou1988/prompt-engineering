using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptEngineering.Mcp;

namespace Chatbot.Services;

public sealed class SpaceMissionsMcpAgentService : ISpaceMissionsMcpAgentService
{
    private readonly IOptions<SpaceMissionsAgentOptions> _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SpaceMissionsMcpAgentService> _logger;

    public SpaceMissionsMcpAgentService(
        IOptions<SpaceMissionsAgentOptions> options,
        ILoggerFactory loggerFactory,
        ILogger<SpaceMissionsMcpAgentService> logger)
    {
        _options = options;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    public async Task<IMcpBackendSession> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var mcp = _options.Value.SpaceMissionsMcp;
        _logger.LogInformation(
            "Connecting to SpaceMissions MCP server: Command={Command}, Args={Arguments}, WorkingDirectory={WorkingDirectory}",
            mcp.Command,
            string.Join(' ', mcp.Arguments),
            mcp.WorkingDirectory);

        var client = await McpStdioClientFactory.CreateAsync(
                mcp,
                _loggerFactory,
                cancellationToken)
            .ConfigureAwait(false);
        var tools = (await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();

        _logger.LogInformation(
            "SpaceMissions MCP connected with {ToolCount} tools: {ToolNames}",
            tools.Count,
            string.Join(", ", tools.Select(t => t.Name)));

        var definitions = McpChatToolMapper.ToDefinitions(tools);
        return new McpBackendSession(client, tools, definitions);
    }
}
