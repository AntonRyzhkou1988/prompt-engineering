using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptEngineering.Mcp;

namespace Chatbot.Services;

public sealed class SpaceMissionsMcpAgentService : ISpaceMissionsMcpAgentService
{
    private readonly IOptions<SpaceMissionsAgentOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public SpaceMissionsMcpAgentService(IOptions<SpaceMissionsAgentOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public async Task<IMcpBackendSession> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = await McpStdioClientFactory.CreateAsync(
                _options.Value.SpaceMissionsMcp,
                _loggerFactory,
                cancellationToken)
            .ConfigureAwait(false);
        var tools = (await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();
        var definitions = McpChatToolMapper.ToDefinitions(tools);
        return new McpBackendSession(client, tools, definitions);
    }
}
