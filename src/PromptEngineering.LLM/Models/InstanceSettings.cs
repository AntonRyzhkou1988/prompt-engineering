namespace PromptEngineering.LLM.Models;

public record InstanceSettings
{
    public string Name { get; set; } = null!;

    public string ApiKey { get; set; } = null!;

    public string Deployment { get; set; } = null!;
}