namespace PromptEngineering.Mcp;

public sealed class McpTransportOptions
{
    /// <summary>Executable, e.g. npx or dotnet.</summary>
    public string Command { get; set; } = "npx";

    /// <summary>Arguments excluding the executable.</summary>
    public List<string> Arguments { get; set; } = new();

    public string Name { get; set; } = "mcp";

    /// <summary>Optional working directory for the child process.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Extra environment variables for the MCP server process.</summary>
    public Dictionary<string, string> Environment { get; set; } = new();
}
