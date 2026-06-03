namespace PromptEngineering.SpaceMissions;

public sealed record AggregateBucket(string Bucket, int Count, double Percentage);

public sealed record AggregateResult(string GroupByColumn, int TotalRows, IReadOnlyList<AggregateBucket> Buckets);
