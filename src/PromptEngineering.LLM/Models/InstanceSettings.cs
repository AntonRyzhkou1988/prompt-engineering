namespace PromptEngineering.LLM.Models;

public record InstanceSettings
{
    public string Name { get; set; }

    public string ApiKey { get; set; }

    public string Deployment { get; set; }
}