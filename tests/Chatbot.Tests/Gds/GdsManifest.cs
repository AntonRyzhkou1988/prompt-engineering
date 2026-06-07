using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chatbot.Tests.Gds;

internal sealed class GdsManifest
{
    public int Version { get; init; }

    public IReadOnlyList<GdsManifestItem> Items { get; init; } = [];

    public static GdsManifest Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GdsManifest>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize GDS manifest: {path}");
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}

internal sealed class GdsManifestItem
{
    public string ItemId { get; init; } = "";

    public int SourceQuestionNumber { get; init; }

    public string Question { get; init; } = "";

    public IReadOnlyList<string> ExpectedTools { get; init; } = [];

    /// <summary>"all" (default) requires every listed tool; "any" passes when at least one matches.</summary>
    public string ExpectedToolsMode { get; init; } = "all";

    public IReadOnlyList<string> VerificationCriteria { get; init; } = [];

    public string GroundTruthRef { get; init; } = "";
}

internal sealed class GdsGroundTruthDocument
{
    public string ItemId { get; init; } = "";

    public string GeneratedUtc { get; init; } = "";

    public IReadOnlyList<GdsMcpCallRecord> McpCalls { get; init; } = [];

    public Dictionary<string, JsonElement> KeyFacts { get; init; } = new(StringComparer.Ordinal);

    public static GdsGroundTruthDocument Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GdsGroundTruthDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize ground truth: {path}");
    }

    public string ToJson() => JsonSerializer.Serialize(this, WriteOptions);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };
}

internal sealed class GdsMcpCallRecord
{
    public string Tool { get; init; } = "";

    public JsonElement Arguments { get; init; }

    public JsonElement Response { get; init; }
}

internal sealed class GdsJudgeResult
{
    public string ItemId { get; init; } = "";

    public double Score { get; init; }

    public bool Passed { get; init; }

    public string Reasoning { get; init; } = "";

    public IReadOnlyList<string> ToolsInvoked { get; init; } = [];

    public bool ToolRoutingPassed { get; init; }

    public string GeneratedUtc { get; init; } = "";

    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    });
}

internal sealed class GdsJudgeResponse
{
    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("passed")]
    public bool Passed { get; init; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = "";
}
