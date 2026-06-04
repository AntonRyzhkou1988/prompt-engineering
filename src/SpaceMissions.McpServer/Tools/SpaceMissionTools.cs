using System.ComponentModel;
using ModelContextProtocol.Server;
using PromptEngineering.SpaceMissions;

namespace SpaceMissions.McpServer.Tools;

[McpServerToolType]
public sealed class SpaceMissionTools(ISpaceMissionQueryService queryService)
{
    [McpServerTool(Name = "get_space_missions_schema"), Description("Return column definitions, dataset row count, date range, and known MissionStatus values for dataset/space_missions.csv.")]
    public string GetSpaceMissionsSchema()
    {
        var summary = queryService.GetSummary();
        return SpaceMissionToolResponses.Serialize(new
        {
            columns = queryService.GetSchema().Select(c => new { c.Name, c.Description }),
            datasetRowCount = summary.TotalRows,
            dateRange = new { min = summary.DateMin, max = summary.DateMax },
            knownMissionStatusValues = SpaceMissionSchema.KnownMissionStatusValues
        });
    }

    [McpServerTool(Name = "get_space_missions_summary"), Description("Return dataset overview: total rows, date range, and full-dataset MissionStatus outcome mix.")]
    public string GetSpaceMissionsSummary()
    {
        var summary = queryService.GetSummary();
        return SpaceMissionToolResponses.Serialize(new
        {
            totalRows = summary.TotalRows,
            dateMin = summary.DateMin,
            dateMax = summary.DateMax,
            missionStatusBreakdown = summary.MissionStatusBreakdown
        });
    }

    [McpServerTool(Name = "filter_space_missions"), Description("Return matching space mission rows sorted by date ascending. Supports exact and substring filters. Capped at 200 rows per call; use offset to paginate.")]
    public string FilterSpaceMissions(
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Company.")] string? companyContains = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional substring match on Rocket.")] string? rocketContains = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional substring match on Mission.")] string? missionContains = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null,
        [Description("Maximum rows to return (default 50, max 200).")] int limit = SpaceMissionQueryService.DefaultLimit,
        [Description("Number of matching rows to skip before returning results (default 0).")] int offset = 0)
    {
        var built = BuildFromParameters(company, companyContains, locationContains, rocket, rocketContains, mission, missionContains, rocketStatus, missionStatus, dateFrom, dateTo);
        var effectiveLimit = limit <= 0 ? SpaceMissionQueryService.DefaultLimit : Math.Min(limit, SpaceMissionQueryService.MaxLimit);
        var rows = queryService.Filter(built.Filter, effectiveLimit, offset);
        var totalMatching = queryService.Count(built.Filter);

        return SpaceMissionToolResponses.Serialize(new
        {
            returned = rows.Count,
            totalMatching,
            limit = effectiveLimit,
            offset = Math.Max(0, offset),
            warnings = built.Warnings,
            rows
        });
    }

    [McpServerTool(Name = "aggregate_space_missions"), Description("Group matching rows by Company, Location, Date, Time, Rocket, Mission, RocketStatus, Price, or MissionStatus. Returns counts and percentages; use maxBuckets (default 50) to cap buckets with an Other rollup.")]
    public string AggregateSpaceMissions(
        [Description("Column to group by: Company, Location, Date, Time, Rocket, Mission, RocketStatus, Price, or MissionStatus.")] string groupBy,
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Company.")] string? companyContains = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional substring match on Rocket.")] string? rocketContains = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional substring match on Mission.")] string? missionContains = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null,
        [Description("Maximum buckets to return before rolling remainder into Other (default 50, max 200).")] int maxBuckets = SpaceMissionQueryService.DefaultMaxBuckets)
    {
        if (string.IsNullOrWhiteSpace(groupBy))
            return SpaceMissionToolResponses.Error("groupBy is required.");

        var built = BuildFromParameters(company, companyContains, locationContains, rocket, rocketContains, mission, missionContains, rocketStatus, missionStatus, dateFrom, dateTo);

        try
        {
            var result = queryService.Aggregate(groupBy, built.Filter, maxBuckets);
            return SpaceMissionToolResponses.Serialize(new
            {
                result.GroupByColumn,
                result.TotalRows,
                result.Buckets,
                result.Other,
                warnings = built.Warnings
            });
        }
        catch (ArgumentException ex)
        {
            return SpaceMissionToolResponses.Error(ex.Message);
        }
    }

    [McpServerTool(Name = "aggregate_space_missions_by_launch_country"), Description("Group rows by launch country derived from Location (last comma-separated segment). Returns counts, percentages, and the derivation rule.")]
    public string AggregateSpaceMissionsByLaunchCountry(
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Company.")] string? companyContains = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional substring match on Rocket.")] string? rocketContains = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional substring match on Mission.")] string? missionContains = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null,
        [Description("Maximum country buckets before rolling remainder into Other (default 50, max 200).")] int maxBuckets = SpaceMissionQueryService.DefaultMaxBuckets)
    {
        var built = BuildFromParameters(company, companyContains, locationContains, rocket, rocketContains, mission, missionContains, rocketStatus, missionStatus, dateFrom, dateTo);
        var result = queryService.AggregateByLaunchCountry(built.Filter, maxBuckets);

        return SpaceMissionToolResponses.Serialize(new
        {
            groupByColumn = result.GroupByColumn,
            derivationRule = LaunchCountryParser.DerivationRule,
            result.TotalRows,
            result.Buckets,
            result.Other,
            warnings = built.Warnings
        });
    }

    [McpServerTool(Name = "compute_space_mission_success_rate"), Description("Compute mission success rate: Success count divided by rows with non-empty MissionStatus in the filtered slice.")]
    public string ComputeSpaceMissionSuccessRate(
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Company.")] string? companyContains = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional substring match on Rocket.")] string? rocketContains = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional substring match on Mission.")] string? missionContains = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null)
    {
        var built = BuildFromParameters(company, companyContains, locationContains, rocket, rocketContains, mission, missionContains, rocketStatus, missionStatus, dateFrom, dateTo);
        var result = queryService.ComputeSuccessRate(built.Filter);

        return SpaceMissionToolResponses.Serialize(new
        {
            result.TotalMatching,
            result.SuccessCount,
            result.Denominator,
            successRatePercent = result.SuccessRate,
            result.Formula,
            warnings = built.Warnings
        });
    }

    [McpServerTool(Name = "count_space_missions"), Description("Count rows matching optional filters.")]
    public string CountSpaceMissions(
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Company.")] string? companyContains = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional substring match on Rocket.")] string? rocketContains = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional substring match on Mission.")] string? missionContains = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null)
    {
        var built = BuildFromParameters(company, companyContains, locationContains, rocket, rocketContains, mission, missionContains, rocketStatus, missionStatus, dateFrom, dateTo);
        return SpaceMissionToolResponses.Serialize(new
        {
            count = queryService.Count(built.Filter),
            warnings = built.Warnings
        });
    }

    [McpServerTool(Name = "list_space_mission_distinct_values"), Description("List distinct values for a column (Company, Location, Date, Time, Rocket, Mission, RocketStatus, Price, MissionStatus) with optional filter and search.")]
    public string ListSpaceMissionDistinctValues(
        [Description("Column name: Company, Location, Date, Time, Rocket, Mission, RocketStatus, Price, or MissionStatus.")] string column,
        [Description("Optional substring filter on distinct values.")] string? search = null,
        [Description("Maximum values to return (default 25, max 100).")] int limit = SpaceMissionQueryService.DefaultDistinctLimit,
        [Description("Optional exact Company filter.")] string? company = null,
        [Description("Optional substring match on Company.")] string? companyContains = null,
        [Description("Optional substring match on Location.")] string? locationContains = null,
        [Description("Optional exact Rocket filter.")] string? rocket = null,
        [Description("Optional substring match on Rocket.")] string? rocketContains = null,
        [Description("Optional exact Mission filter.")] string? mission = null,
        [Description("Optional substring match on Mission.")] string? missionContains = null,
        [Description("Optional exact RocketStatus filter.")] string? rocketStatus = null,
        [Description("Optional exact MissionStatus filter.")] string? missionStatus = null,
        [Description("Optional inclusive start date (YYYY-MM-DD).")] string? dateFrom = null,
        [Description("Optional inclusive end date (YYYY-MM-DD).")] string? dateTo = null)
    {
        if (string.IsNullOrWhiteSpace(column))
            return SpaceMissionToolResponses.Error("column is required.");

        var built = BuildFromParameters(company, companyContains, locationContains, rocket, rocketContains, mission, missionContains, rocketStatus, missionStatus, dateFrom, dateTo);

        try
        {
            var result = queryService.DistinctValues(column, built.Filter, search, limit);
            return SpaceMissionToolResponses.Serialize(new
            {
                result.Column,
                result.TotalDistinct,
                returned = result.Values.Count,
                result.Values,
                warnings = built.Warnings
            });
        }
        catch (ArgumentException ex)
        {
            return SpaceMissionToolResponses.Error(ex.Message);
        }
    }

    private static FilterBuildResult BuildFromParameters(
        string? company,
        string? companyContains,
        string? locationContains,
        string? rocket,
        string? rocketContains,
        string? mission,
        string? missionContains,
        string? rocketStatus,
        string? missionStatus,
        string? dateFrom,
        string? dateTo) =>
        SpaceMissionToolResponses.BuildFilter(new SpaceMissionFilterInput(
            company,
            companyContains,
            locationContains,
            rocket,
            rocketContains,
            mission,
            missionContains,
            rocketStatus,
            missionStatus,
            dateFrom,
            dateTo));
}
