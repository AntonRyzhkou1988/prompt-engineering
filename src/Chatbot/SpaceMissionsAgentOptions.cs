using PromptEngineering.Mcp;

namespace Chatbot;

public sealed class SpaceMissionsAgentOptions
{
    public const string SectionName = "SpaceMissionsAgent";

    public string InstanceName { get; set; } = "";

    public float Temperature { get; set; } = 0.2f;

    public int MaxFunctionIterations { get; set; } = 7;

    public McpTransportOptions SpaceMissionsMcp { get; set; } = new();
}
