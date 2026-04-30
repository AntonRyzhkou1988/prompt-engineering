namespace Agent;

public static class ToolRoutingEvaluator
{
    /// <summary>
    /// Returns true if every expected domain is covered by at least one invoked tool name
    /// according to <see cref="ToolDomainMapper"/>.
    /// </summary>
    public static bool Passes(
        IReadOnlyList<string> expectedDomains,
        IReadOnlyList<string> toolNamesInvoked,
        ToolDomainMapper mapper)
    {
        var covered = new HashSet<ToolDomain>();
        foreach (var name in toolNamesInvoked)
        {
            foreach (var d in mapper.DomainsForToolName(name))
                covered.Add(d);
        }

        foreach (var raw in expectedDomains)
        {
            var d = ParseDomain(raw);
            if (!covered.Contains(d))
                return false;
        }

        return true;
    }

    private static ToolDomain ParseDomain(string raw)
    {
        if (raw.Equals("weather", StringComparison.OrdinalIgnoreCase))
            return ToolDomain.Weather;
        if (raw.Equals("news", StringComparison.OrdinalIgnoreCase))
            return ToolDomain.News;
        throw new ArgumentException($"Unknown domain '{raw}'. Use 'weather' or 'news'.", nameof(raw));
    }
}
