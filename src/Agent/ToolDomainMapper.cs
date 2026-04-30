using Microsoft.Extensions.Options;

namespace Agent;

public enum ToolDomain
{
    Weather,
    News
}

public sealed class ToolDomainMapper
{
    private readonly ToolRoutingMapOptions _options;

    public ToolDomainMapper(IOptions<AgentOptions> options)
    {
        _options = options.Value.ToolRouting;
    }

    public IReadOnlySet<ToolDomain> DomainsForToolName(string toolName)
    {
        var name = toolName ?? string.Empty;
        var set = new HashSet<ToolDomain>();
        foreach (var s in _options.WeatherToolNameSubstrings)
        {
            if (name.Contains(s, StringComparison.OrdinalIgnoreCase))
                set.Add(ToolDomain.Weather);
        }

        foreach (var s in _options.NewsToolNameSubstrings)
        {
            if (name.Contains(s, StringComparison.OrdinalIgnoreCase))
                set.Add(ToolDomain.News);
        }

        return set;
    }
}
