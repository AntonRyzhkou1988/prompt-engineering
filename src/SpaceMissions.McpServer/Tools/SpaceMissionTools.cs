using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using PromptEngineering.SpaceMissions;

namespace SpaceMissions.McpServer.Tools;

[McpServerToolType]
public sealed class SpaceMissionTools
{
    private readonly ISpaceMissionQueryService _queryService;

    public SpaceMissionTools(ISpaceMissionQueryService queryService)
    {
        _queryService = queryService;
    }

    [McpServerTool(Name = "get_space_missions_schema"), Description("Return column names and descriptions for dataset/space_missions.csv.")]
    public string GetSpaceMissionsSchema()
    {
        var schema = _queryService.GetSchema()
            .Select(c => new { c.Name, c.Description });
        return JsonSerializer.Serialize(schema);
    }

    [McpServerTool(Name = "filter_space_missions"), Description("Return matching space mission rows (capped at 200).")]
    public string FilterSpaceMissions(
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null,
        [Description("Maximum rows to return (default 50, max 200).")] int limit = SpaceMissionQueryService.DefaultLimit)
    {
        var filter = BuildFilter(company, locationContains, rocket, mission, rocketStatus, missionStatus, dateFrom, dateTo);
        var rows = _queryService.Filter(filter, limit);
        return JsonSerializer.Serialize(new
        {
            returned = rows.Count,
            limit = Math.Min(limit <= 0 ? SpaceMissionQueryService.DefaultLimit : limit, SpaceMissionQueryService.MaxLimit),
            rows
        });
    }

    [McpServerTool(Name = "aggregate_space_missions"), Description("Group matching rows by a column and return counts and percentages.")]
    public string AggregateSpaceMissions(
        [Description("Column to group by: Company, Location, Date, Time, Rocket, Mission, RocketStatus, Price, or MissionStatus.")] string groupBy,
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null)
    {
        if (string.IsNullOrWhiteSpace(groupBy))
            return JsonSerializer.Serialize(new { error = "groupBy is required." });

        try
        {
            var filter = BuildFilter(company, locationContains, rocket, mission, rocketStatus, missionStatus, dateFrom, dateTo);
            var result = _queryService.Aggregate(groupBy, filter);
            return JsonSerializer.Serialize(result);
        }
        catch (ArgumentException ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    [McpServerTool(Name = "count_space_missions"), Description("Count rows matching optional filters.")]
    public string CountSpaceMissions(
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null)
    {
        var filter = BuildFilter(company, locationContains, rocket, mission, rocketStatus, missionStatus, dateFrom, dateTo);
        return JsonSerializer.Serialize(new { count = _queryService.Count(filter) });
    }

    private static SpaceMissionFilter? BuildFilter(
        string? company,
        string? locationContains,
        string? rocket,
        string? mission,
        string? rocketStatus,
        string? missionStatus,
        string? dateFrom,
        string? dateTo)
    {
        DateOnly? from = null;
        DateOnly? to = null;
        if (!string.IsNullOrWhiteSpace(dateFrom) && DateOnly.TryParse(dateFrom, out var parsedFrom))
            from = parsedFrom;
        if (!string.IsNullOrWhiteSpace(dateTo) && DateOnly.TryParse(dateTo, out var parsedTo))
            to = parsedTo;

        var filter = new SpaceMissionFilter
        {
            Company = company,
            LocationContains = locationContains,
            Rocket = rocket,
            Mission = mission,
            RocketStatus = rocketStatus,
            MissionStatus = missionStatus,
            DateFrom = from,
            DateTo = to
        };

        if (string.IsNullOrWhiteSpace(filter.Company)
            && string.IsNullOrWhiteSpace(filter.LocationContains)
            && string.IsNullOrWhiteSpace(filter.Rocket)
            && string.IsNullOrWhiteSpace(filter.Mission)
            && string.IsNullOrWhiteSpace(filter.RocketStatus)
            && string.IsNullOrWhiteSpace(filter.MissionStatus)
            && filter.DateFrom is null
            && filter.DateTo is null)
        {
            return null;
        }

        return filter;
    }
}
