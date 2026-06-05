namespace PromptEngineering.SpaceMissions;

public sealed record DistinctValuesResult(
    string Column,
    int TotalDistinct,
    IReadOnlyList<string> Values);
