namespace Chatbot.Tests.Gds;

internal sealed class GdsJudgeOptions
{
    public const string SectionName = "Gds";

    public string? JudgeInstanceName { get; set; }

    public string? AgentInstanceName { get; set; }

    public string? RagInstanceName { get; set; }

    public int InterItemDelaySeconds { get; set; } = 25;

    public int RateLimitMaxAttempts { get; set; } = 10;
}
