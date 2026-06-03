namespace PromptEngineering.SpaceMissions;

public sealed record SpaceMission(
    string Company,
    string Location,
    string Date,
    string Time,
    string Rocket,
    string Mission,
    string RocketStatus,
    string Price,
    string MissionStatus);
