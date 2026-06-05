namespace PromptEngineering.SpaceMissions;

public sealed record SuccessRateResult(
    int TotalMatching,
    int SuccessCount,
    int Denominator,
    double SuccessRate,
    string Formula);
