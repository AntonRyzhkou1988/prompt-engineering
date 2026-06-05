using System.Text.Json;
using System.Text.Json.Serialization;
using PromptEngineering.SpaceMissions;

namespace SpaceMissions.McpServer.Tools;

internal static class SpaceMissionToolResponses
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    internal static string Error(string message) => Serialize(new { error = message });

    internal static FilterBuildResult BuildFilter(SpaceMissionFilterInput input)
    {
        var warnings = new List<string>();
        DateOnly? from = null;
        DateOnly? to = null;

        if (!string.IsNullOrWhiteSpace(input.DateFrom))
        {
            if (DateOnly.TryParse(input.DateFrom, out var parsedFrom))
                from = parsedFrom;
            else
                warnings.Add($"dateFrom '{input.DateFrom}' is not a valid YYYY-MM-DD date and was ignored.");
        }

        if (!string.IsNullOrWhiteSpace(input.DateTo))
        {
            if (DateOnly.TryParse(input.DateTo, out var parsedTo))
                to = parsedTo;
            else
                warnings.Add($"dateTo '{input.DateTo}' is not a valid YYYY-MM-DD date and was ignored.");
        }

        var filter = new SpaceMissionFilter
        {
            Company = input.Company,
            CompanyContains = input.CompanyContains,
            LocationContains = input.LocationContains,
            Rocket = input.Rocket,
            RocketContains = input.RocketContains,
            Mission = input.Mission,
            MissionContains = input.MissionContains,
            RocketStatus = input.RocketStatus,
            MissionStatus = input.MissionStatus,
            DateFrom = from,
            DateTo = to
        };

        return new FilterBuildResult(filter.IsEmpty ? null : filter, warnings);
    }
}

internal sealed record SpaceMissionFilterInput(
    string? Company = null,
    string? CompanyContains = null,
    string? LocationContains = null,
    string? Rocket = null,
    string? RocketContains = null,
    string? Mission = null,
    string? MissionContains = null,
    string? RocketStatus = null,
    string? MissionStatus = null,
    string? DateFrom = null,
    string? DateTo = null);

internal sealed record FilterBuildResult(SpaceMissionFilter? Filter, IReadOnlyList<string> Warnings);
