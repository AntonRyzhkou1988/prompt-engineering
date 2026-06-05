namespace PromptEngineering.SpaceMissions;

public sealed record DatasetSummary(
    int TotalRows,
    string? DateMin,
    string? DateMax,
    IReadOnlyList<AggregateBucket> MissionStatusBreakdown);
