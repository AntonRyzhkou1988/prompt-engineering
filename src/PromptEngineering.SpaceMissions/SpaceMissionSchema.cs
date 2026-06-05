namespace PromptEngineering.SpaceMissions;

public sealed record SchemaColumn(string Name, string Description);

public static class SpaceMissionSchema
{
    public static IReadOnlyList<SchemaColumn> Columns { get; } =
    [
        new("Company", "Company responsible for the space mission."),
        new("Location", "Location of the launch."),
        new("Date", "Date of the launch (YYYY-MM-DD)."),
        new("Time", "Time of the launch (UTC)."),
        new("Rocket", "Name of the rocket used for the mission."),
        new("Mission", "Name of the space mission."),
        new("RocketStatus", "Status of the rocket as of August 2022 (typically Retired or Active in the dataset)."),
        new("Price", "Cost of the rocket in millions of US dollars."),
        new("MissionStatus", "Outcome: Success, Failure, Partial Failure, or Prelaunch Failure.")
    ];

    public static readonly HashSet<string> ValidGroupByColumns = new(
        Columns.Select(c => c.Name),
        StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlyList<string> KnownMissionStatusValues =
    [
        "Success",
        "Failure",
        "Partial Failure",
        "Prelaunch Failure"
    ];

    public static bool TryNormalizeColumnName(string? column, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(column)) return false;

        var match = Columns.FirstOrDefault(c => c.Name.Equals(column.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null) return false;

        normalized = match.Name;
        return true;
    }
}
