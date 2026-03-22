namespace PromptEngineering.LLM.Models;

/// <summary>
/// Typed projection of one shark-attack dataset row used for prompt injection.
/// Fields align with common columns documented in README.
/// </summary>
public sealed record AttackRecord
{
    public string? Year { get; init; }

    public string? Country { get; init; }

    public string? Area { get; init; }

    public string? Type { get; init; }

    public string? Activity { get; init; }

    public string? Injury { get; init; }

    public string? FatalYn { get; init; }

    public string? Sex { get; init; }

    public string? Age { get; init; }

    public string? Time { get; init; }

    public string? Species { get; init; }

    public string? InvestigatorSource { get; init; }
}
