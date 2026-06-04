namespace PromptEngineering.SpaceMissions;

public interface ISpaceMissionQueryService
{
    IReadOnlyList<SchemaColumn> GetSchema();
    DatasetSummary GetSummary();
    IReadOnlyList<SpaceMission> Filter(SpaceMissionFilter? filter, int limit, int offset = 0);
    int Count(SpaceMissionFilter? filter);
    AggregateResult Aggregate(string groupByColumn, SpaceMissionFilter? filter, int maxBuckets);
    AggregateResult AggregateByLaunchCountry(SpaceMissionFilter? filter, int maxBuckets);
    DistinctValuesResult DistinctValues(string column, SpaceMissionFilter? filter, string? search, int limit);
    SuccessRateResult ComputeSuccessRate(SpaceMissionFilter? filter);
}

public sealed class SpaceMissionQueryService : ISpaceMissionQueryService
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;
    public const int DefaultMaxBuckets = 50;
    public const int MaxMaxBuckets = 200;
    public const int DefaultDistinctLimit = 25;
    public const int MaxDistinctLimit = 100;

    private const string SuccessRateFormula =
        "successRate = count(MissionStatus == 'Success') / count(non-empty MissionStatus); percentages use the filtered slice only.";

    private readonly IReadOnlyList<SpaceMission> _missions;

    public SpaceMissionQueryService(string datasetPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(datasetPath);
        _missions = LoadMissions(datasetPath);
    }

    public SpaceMissionQueryService(IReadOnlyList<SpaceMission> missions)
    {
        _missions = missions ?? throw new ArgumentNullException(nameof(missions));
    }

    public IReadOnlyList<SchemaColumn> GetSchema() => SpaceMissionSchema.Columns;

    public DatasetSummary GetSummary()
    {
        var dates = _missions
            .Select(m => m.Date)
            .Where(d => TryParseDate(d, out _))
            .Select(d =>
            {
                TryParseDate(d, out var parsed);
                return parsed;
            })
            .ToList();

        var breakdown = Aggregate("MissionStatus", null, DefaultMaxBuckets);
        return new DatasetSummary(
            _missions.Count,
            dates.Count > 0 ? dates.Min().ToString("yyyy-MM-dd") : null,
            dates.Count > 0 ? dates.Max().ToString("yyyy-MM-dd") : null,
            breakdown.Buckets);
    }

    public IReadOnlyList<SpaceMission> Filter(SpaceMissionFilter? filter, int limit, int offset = 0)
    {
        var effectiveLimit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
        var effectiveOffset = Math.Max(0, offset);
        return ApplyFilter(filter)
            .OrderBy(m => m.Date, StringComparer.Ordinal)
            .ThenBy(m => m.Time, StringComparer.Ordinal)
            .ThenBy(m => m.Mission, StringComparer.Ordinal)
            .Skip(effectiveOffset)
            .Take(effectiveLimit)
            .ToList();
    }

    public int Count(SpaceMissionFilter? filter) => ApplyFilter(filter).Count();

    public AggregateResult Aggregate(string groupByColumn, SpaceMissionFilter? filter, int maxBuckets = DefaultMaxBuckets)
    {
        if (!SpaceMissionSchema.TryNormalizeColumnName(groupByColumn, out var column))
            throw new ArgumentException($"Invalid groupBy column '{groupByColumn}'. Valid columns: {string.Join(", ", SpaceMissionSchema.Columns.Select(c => c.Name))}.");

        var rows = ApplyFilter(filter).ToList();
        var buckets = rows
            .GroupBy(m => GetColumnValue(m, column), StringComparer.Ordinal)
            .Select(g => (Key: g.Key, Count: g.Count()));

        return BuildAggregateResult(column, rows.Count, buckets, maxBuckets);
    }

    public AggregateResult AggregateByLaunchCountry(SpaceMissionFilter? filter, int maxBuckets = DefaultMaxBuckets)
    {
        var rows = ApplyFilter(filter).ToList();
        var buckets = rows
            .GroupBy(m => LaunchCountryParser.DeriveCountry(m.Location), StringComparer.Ordinal)
            .Select(g => (Key: g.Key, Count: g.Count()));

        return BuildAggregateResult("LaunchCountry", rows.Count, buckets, maxBuckets);
    }

    public DistinctValuesResult DistinctValues(string column, SpaceMissionFilter? filter, string? search, int limit)
    {
        if (!SpaceMissionSchema.TryNormalizeColumnName(column, out var normalizedColumn))
            throw new ArgumentException($"Invalid column '{column}'. Valid columns: {string.Join(", ", SpaceMissionSchema.Columns.Select(c => c.Name))}.");

        var effectiveLimit = limit <= 0 ? DefaultDistinctLimit : Math.Min(limit, MaxDistinctLimit);
        var values = ApplyFilter(filter)
            .Select(m => GetColumnValue(m, normalizedColumn))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var needle = search.Trim();
            values = values.Where(v => v.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = values
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var totalDistinct = ordered.Count;
        var page = ordered.Take(effectiveLimit).ToList();
        return new DistinctValuesResult(normalizedColumn, totalDistinct, page);
    }

    public SuccessRateResult ComputeSuccessRate(SpaceMissionFilter? filter)
    {
        var rows = ApplyFilter(filter).ToList();
        var withStatus = rows.Where(m => !string.IsNullOrWhiteSpace(m.MissionStatus)).ToList();
        var successCount = withStatus.Count(m =>
            m.MissionStatus.Trim().Equals("Success", StringComparison.OrdinalIgnoreCase));
        var denominator = withStatus.Count;
        var rate = denominator == 0 ? 0 : Math.Round(successCount * 100.0 / denominator, 2);

        return new SuccessRateResult(rows.Count, successCount, denominator, rate, SuccessRateFormula);
    }

    internal static IReadOnlyList<SpaceMission> LoadMissions(string datasetPath)
    {
        var fullPath = Path.GetFullPath(datasetPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Space missions dataset not found: {fullPath}", fullPath);

        var missions = new List<SpaceMission>();
        using var reader = new StreamReader(fullPath);
        var headerRaw = CsvRecordParser.ReadRawRecordAsync(reader, CancellationToken.None).GetAwaiter().GetResult()
            ?? throw new InvalidOperationException("Dataset is empty.");

        var headers = CsvRecordParser.ParseRecord(headerRaw);
        var index = BuildHeaderIndex(headers);

        while (true)
        {
            var raw = CsvRecordParser.ReadRawRecordAsync(reader, CancellationToken.None).GetAwaiter().GetResult();
            if (raw is null) break;

            var fields = CsvRecordParser.ParseRecord(raw);
            if (fields.Length == 0 || fields.All(string.IsNullOrWhiteSpace)) continue;

            missions.Add(new SpaceMission(
                GetField(fields, index, "Company"),
                GetField(fields, index, "Location"),
                GetField(fields, index, "Date"),
                GetField(fields, index, "Time"),
                GetField(fields, index, "Rocket"),
                GetField(fields, index, "Mission"),
                GetField(fields, index, "RocketStatus"),
                GetField(fields, index, "Price"),
                GetField(fields, index, "MissionStatus")));
        }

        return missions;
    }

    private static AggregateResult BuildAggregateResult(
        string groupByColumn,
        int total,
        IEnumerable<(string Key, int Count)> buckets,
        int maxBuckets)
    {
        var effectiveMax = maxBuckets <= 0 ? DefaultMaxBuckets : Math.Min(maxBuckets, MaxMaxBuckets);
        var ordered = buckets
            .OrderByDescending(b => b.Count)
            .ThenBy(b => b.Key, StringComparer.Ordinal)
            .ToList();

        AggregateBucket? other = null;
        IReadOnlyList<AggregateBucket> topBuckets;

        if (ordered.Count > effectiveMax)
        {
            var shown = ordered.Take(effectiveMax).ToList();
            var remainder = ordered.Skip(effectiveMax);
            var otherCount = remainder.Sum(b => b.Count);
            topBuckets = shown
                .Select(b => ToBucket(b.Key, b.Count, total))
                .ToList();
            other = ToBucket("Other", otherCount, total);
        }
        else
        {
            topBuckets = ordered
                .Select(b => ToBucket(b.Key, b.Count, total))
                .ToList();
        }

        return new AggregateResult(groupByColumn, total, topBuckets, other);
    }

    private static AggregateBucket ToBucket(string key, int count, int total) =>
        new(key, count, total == 0 ? 0 : Math.Round(count * 100.0 / total, 2));

    private IEnumerable<SpaceMission> ApplyFilter(SpaceMissionFilter? filter)
    {
        if (filter is null || filter.IsEmpty) return _missions;

        IEnumerable<SpaceMission> query = _missions;

        if (!string.IsNullOrWhiteSpace(filter.Company))
            query = query.Where(m => m.Company.Equals(filter.Company.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.CompanyContains))
        {
            var needle = filter.CompanyContains.Trim();
            query = query.Where(m => m.Company.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.LocationContains))
        {
            var needle = filter.LocationContains.Trim();
            query = query.Where(m => m.Location.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Rocket))
            query = query.Where(m => m.Rocket.Equals(filter.Rocket.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.RocketContains))
        {
            var needle = filter.RocketContains.Trim();
            query = query.Where(m => m.Rocket.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Mission))
            query = query.Where(m => m.Mission.Equals(filter.Mission.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.MissionContains))
        {
            var needle = filter.MissionContains.Trim();
            query = query.Where(m => m.Mission.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.RocketStatus))
            query = query.Where(m => m.RocketStatus.Equals(filter.RocketStatus.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.MissionStatus))
            query = query.Where(m => m.MissionStatus.Equals(filter.MissionStatus.Trim(), StringComparison.OrdinalIgnoreCase));

        if (filter.DateFrom is { } from)
            query = query.Where(m => TryParseDate(m.Date, out var d) && d >= from);

        if (filter.DateTo is { } to)
            query = query.Where(m => TryParseDate(m.Date, out var d) && d <= to);

        return query;
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
            index[headers[i].Trim()] = i;
        return index;
    }

    private static string GetField(string[] fields, IReadOnlyDictionary<string, int> index, string name)
    {
        if (!index.TryGetValue(name, out var i) || i < 0 || i >= fields.Length) return string.Empty;
        return fields[i].Trim();
    }

    private static string GetColumnValue(SpaceMission mission, string column) => column switch
    {
        "Company" => mission.Company,
        "Location" => mission.Location,
        "Date" => mission.Date,
        "Time" => mission.Time,
        "Rocket" => mission.Rocket,
        "Mission" => mission.Mission,
        "RocketStatus" => mission.RocketStatus,
        "Price" => mission.Price,
        "MissionStatus" => mission.MissionStatus,
        _ => string.Empty
    };

    private static bool TryParseDate(string value, out DateOnly date)
    {
        date = default;
        return !string.IsNullOrWhiteSpace(value) && DateOnly.TryParse(value.Trim(), out date);
    }
}
