namespace PromptEngineering.LLM.Models;

public record AiServiceSettings
{
    /// <summary>
    /// Address of the OpenAI service.
    /// </summary>
    public string BaseAddress { get; set; } = null!;

    /// <summary>
    /// Time in seconds that indicate how much the HTTPHandler can be reused.
    /// </summary>
    public int HandlerLifetimeInSeconds { get; set; } = 120;

    /// <summary>
    /// TimeOut of HTTP Requests in seconds.
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 60;

    /// <summary>
    /// NameOfTheFieldWithFile of the system, such as "openai"
    /// </summary>
    public required string SystemName { get; set; }

    /// <summary>
    /// Path to the deployments endpoint.
    /// Just a relative name. eg. "deployments".
    /// </summary>
    public required string DeploymentsUrl { get; set; }

    /// <summary>
    /// Path to the chat endpoint.
    /// Just a relative name. eg. "chat".
    /// </summary>
    public required string ChatUrl { get; set; }

    /// <summary>
    /// Path to the completion endpoint.
    /// Just a relative name. eg. "completions".
    /// </summary>
    public required string CompletionsUrl { get; set; }

    /// <summary>
    /// Array of instances that can be used to connect to the OpenAI service.
    /// </summary>
    public required List<InstanceSettings> Instances { get; set; }
}
