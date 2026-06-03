namespace PromptEngineering.SpaceMissions;

public interface ISpaceMissionQueryService
{
    IReadOnlyList<SchemaColumn> GetSchema();
    IReadOnlyList<SpaceMission> Filter(SpaceMissionFilter? filter, int limit);
    int Count(SpaceMissionFilter? filter);
    AggregateResult Aggregate(string groupByColumn, SpaceMissionFilter? filter);
}

public sealed class SpaceMissionQueryService : ISpaceMissionQueryService
{
    public const int DefaultLimit = 50;
    public const int MaxLimit = 200;

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

    public IReadOnlyList<SpaceMission> Filter(SpaceMissionFilter? filter, int limit)
    {
        var effectiveLimit = limit <= 0 ? DefaultLimit : Math.Min(limit, MaxLimit);
        return ApplyFilter(filter).Take(effectiveLimit).ToList();
    }

    public int Count(SpaceMissionFilter? filter) => ApplyFilter(filter).Count();

    public AggregateResult Aggregate(string groupByColumn, SpaceMissionFilter? filter)
    {
        if (!SpaceMissionSchema.TryNormalizeColumnName(groupByColumn, out var column))
            throw new ArgumentException($"Invalid groupBy column '{groupByColumn}'. Valid columns: {string.Join(", ", SpaceMissionSchema.Columns.Select(c => c.Name))}.");

        var rows = ApplyFilter(filter).ToList();
        var total = rows.Count;
        var buckets = rows
            .GroupBy(m => GetColumnValue(m, column), StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new AggregateBucket(
                g.Key,
                g.Count(),
                total == 0 ? 0 : Math.Round(g.Count() * 100.0 / total, 2)))
            .ToList();

        return new AggregateResult(column, total, buckets);
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

    private IEnumerable<SpaceMission> ApplyFilter(SpaceMissionFilter? filter)
    {
        if (filter is null) return _missions;

        IEnumerable<SpaceMission> query = _missions;

        if (!string.IsNullOrWhiteSpace(filter.Company))
            query = query.Where(m => m.Company.Equals(filter.Company.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.LocationContains))
        {
            var needle = filter.LocationContains.Trim();
            query = query.Where(m => m.Location.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Rocket))
            query = query.Where(m => m.Rocket.Equals(filter.Rocket.Trim(), StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter.Mission))
            query = query.Where(m => m.Mission.Equals(filter.Mission.Trim(), StringComparison.OrdinalIgnoreCase));

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
