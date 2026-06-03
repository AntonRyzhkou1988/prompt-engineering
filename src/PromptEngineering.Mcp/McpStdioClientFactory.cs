using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace PromptEngineering.Mcp;

public static class McpStdioClientFactory
{
    public static async Task<McpClient> CreateAsync(
        McpTransportOptions mcp,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken = default)
    {
        var env = MergeEnvironmentFromProcess(mcp.Environment);
        var transportOptions = new StdioClientTransportOptions
        {
            Name = mcp.Name,
            Command = mcp.Command,
            Arguments = mcp.Arguments.ToArray(),
        };

        if (!string.IsNullOrWhiteSpace(mcp.WorkingDirectory))
            transportOptions.WorkingDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, mcp.WorkingDirectory));

        if (env.Count > 0)
            transportOptions.EnvironmentVariables = env.ToDictionary(kv => kv.Key, kv => (string?)kv.Value);

        var transport = new StdioClientTransport(transportOptions, loggerFactory);
        return await McpClient.CreateAsync(transport, null, loggerFactory, cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, string> MergeEnvironmentFromProcess(Dictionary<string, string> configured)
    {
        var env = new Dictionary<string, string>(configured, StringComparer.Ordinal);
        foreach (var kv in configured.ToList())
        {
            if (string.IsNullOrWhiteSpace(kv.Value))
            {
                var fromOs = Environment.GetEnvironmentVariable(kv.Key);
                if (!string.IsNullOrWhiteSpace(fromOs))
                    env[kv.Key] = fromOs;
            }
        }

        return env;
    }
}
