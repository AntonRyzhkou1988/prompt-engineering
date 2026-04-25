namespace Agent;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>
    /// Name of the instance entry in <c>SystemSettings:AiServiceSettings:Instances</c>
    /// (HTTP client + deployment / model id).
    /// </summary>
    public string InstanceName { get; set; } = "";

    public float Temperature { get; set; } = 0.2f;

    public int MaxFunctionIterations { get; set; } = 12;

    public McpTransportOptions OpenMeteo { get; set; } = new();

    /// <summary>Stdio MCP transport for DuckDuckGo search (e.g. npm <c>@ericthered926/duckduckgo-mcp-server</c>).</summary>
    public McpTransportOptions DuckDuckGo { get; set; } = new();

    public ToolRoutingMapOptions ToolRouting { get; set; } = new();
}

public sealed class McpTransportOptions
{
    /// <summary>Executable, e.g. npx or node.</summary>
    public string Command { get; set; } = "npx";

    /// <summary>Arguments excluding the executable.</summary>
    public List<string> Arguments { get; set; } = new();

    public string Name { get; set; } = "mcp";

    /// <summary>Optional working directory for the child process.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Extra environment variables for the MCP server process.</summary>
    public Dictionary<string, string> Environment { get; set; } = new();
}

public sealed class ToolRoutingMapOptions
{
    /// <summary>Substring matches (ordinal ignore-case) that map a tool name to the weather domain.</summary>
    public List<string> WeatherToolNameSubstrings { get; set; } =
    [
        "openmeteo", "open_meteo", "forecast", "weather", "meteo", "ensemble", "climate", "air_quality", "geocoding"
    ];

    /// <summary>Substring matches that map a tool name to the news/search domain (DuckDuckGo MCP tools).</summary>
    public List<string> NewsToolNameSubstrings { get; set; } =
    [
        "duckduckgo",
        "ddg",
        "web_search"
    ];
}
