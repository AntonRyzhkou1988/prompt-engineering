using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptEngineering.Mcp;

namespace Agent;

public sealed class OpenMeteoWeatherAgentService : IWeatherAgentService
{
    private readonly IOptions<AgentOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public OpenMeteoWeatherAgentService(IOptions<AgentOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public async Task<IMcpBackendSession> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = await McpStdioClientFactory.CreateAsync(
                _options.Value.OpenMeteo,
                _loggerFactory,
                cancellationToken)
            .ConfigureAwait(false);
        var tools = (await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();
        var definitions = McpChatToolMapper.ToDefinitions(tools);
        return new McpBackendSession(client, tools, definitions);
    }
}
