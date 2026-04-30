using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Agent;

public sealed class DuckDuckGoNewsAgentService : INewsAgentService
{
    private readonly IOptions<AgentOptions> _options;
    private readonly ILoggerFactory _loggerFactory;

    public DuckDuckGoNewsAgentService(IOptions<AgentOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options;
        _loggerFactory = loggerFactory;
    }

    public async Task<IMcpBackendSession> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var client = await McpStdioClientFactory.CreateAsync(
                _options.Value.DuckDuckGo,
                _loggerFactory,
                cancellationToken)
            .ConfigureAwait(false);
        var tools = (await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false)).ToList();
        var definitions = McpChatToolMapper.ToDefinitions(tools);
        return new McpBackendSession(client, tools, definitions);
    }
}
