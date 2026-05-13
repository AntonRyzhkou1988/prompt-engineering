namespace Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Name of the instance entry in <c>SystemSettings:AiServiceSettings:Instances</c>
    /// (HTTP client + deployment / model id).
    /// </summary>
    public string InstanceName { get; set; } = "";

    public float Temperature { get; set; } = 0.2f;
}
