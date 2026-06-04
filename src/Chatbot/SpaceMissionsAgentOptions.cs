using PromptEngineering.Mcp;

namespace Chatbot;

public sealed class SpaceMissionsAgentOptions
{
    public const string SectionName = "SpaceMissionsAgent";

    public string InstanceName { get; set; } = "";

    public float Temperature { get; set; } = 0.2f;

    public int MaxFunctionIterations { get; set; } = 7;

    /// <summary>Repo-relative or absolute path to the MCP server project, DLL, or executable.</summary>
    public string McpProjectPath { get; set; } = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj";

    /// <summary>Repo-relative or absolute path to <c>space_missions.csv</c>.</summary>
    public string DatasetPath { get; set; } = "dataset/space_missions.csv";

    /// <summary>Optional repo root override; when empty, discovered from <c>dataset/space_missions.csv</c>.</summary>
    public string? RepoRoot { get; set; }

    public McpTransportOptions SpaceMissionsMcp { get; set; } = new();
}
