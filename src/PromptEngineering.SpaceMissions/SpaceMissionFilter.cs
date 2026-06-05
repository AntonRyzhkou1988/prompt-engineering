namespace PromptEngineering.SpaceMissions;

public sealed class SpaceMissionFilter
{
    public string? Company { get; set; }
    public string? CompanyContains { get; set; }
    public string? LocationContains { get; set; }
    public string? Rocket { get; set; }
    public string? RocketContains { get; set; }
    public string? Mission { get; set; }
    public string? MissionContains { get; set; }
    public string? RocketStatus { get; set; }
    public string? MissionStatus { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Company)
        && string.IsNullOrWhiteSpace(CompanyContains)
        && string.IsNullOrWhiteSpace(LocationContains)
        && string.IsNullOrWhiteSpace(Rocket)
        && string.IsNullOrWhiteSpace(RocketContains)
        && string.IsNullOrWhiteSpace(Mission)
        && string.IsNullOrWhiteSpace(MissionContains)
        && string.IsNullOrWhiteSpace(RocketStatus)
        && string.IsNullOrWhiteSpace(MissionStatus)
        && DateFrom is null
        && DateTo is null;
}
