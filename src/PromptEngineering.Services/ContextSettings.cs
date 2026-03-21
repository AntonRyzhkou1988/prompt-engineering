namespace PromptEngineering.Services;

public sealed record ContextSettings
{
    public string AiInstanceName { get; init; } = "AIArchitect.PromptEngineering";

    public string[] DefaultAssistantRole { get; init; } =
    [
        "You are a senior incident data analyst for shark-attack CSV data.",
        "The user message contains <data>...</data> filled at runtime with XML: repeated <record> elements, each with child elements Year, Country, Type, Activity, Injury, FatalYN (from CSV Fatal (Y/N)), Age, Time.",
        "Use only those <record> elements as row-level evidence; empty element text means missing source data."
    ];

    public string[] DefaultUserPrompt { get; init; } =
    [
        "Analyze shark attack incidents from dataset/attacks.csv.",
        "<data>",
        "<!-- Runtime injects <record>...</record> XML per row here. -->",
        "</data>",
        "Use only the XML inside <data> as evidence."
    ];

    public float Temperature { get; init; } = 0.3f;

    public string DatasetPath { get; init; } = "dataset/attacks.csv";

    public string OutputDirectory { get; init; } = "output";
}
