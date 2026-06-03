namespace PromptEngineering.SpaceMissions;

public sealed class SpaceMissionFilter
{
    public string? Company { get; set; }
    public string? LocationContains { get; set; }
    public string? Rocket { get; set; }
    public string? Mission { get; set; }
    public string? RocketStatus { get; set; }
    public string? MissionStatus { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
