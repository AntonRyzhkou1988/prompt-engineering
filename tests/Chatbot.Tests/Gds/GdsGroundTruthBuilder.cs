using System.Text.Json;
using Chatbot;
using Chatbot.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using PromptEngineering.Mcp;

namespace Chatbot.Tests.Gds;

internal static class GdsGroundTruthBuilder
{
    private static readonly JsonSerializerOptions ParseOptions = new(JsonSerializerDefaults.Web);

    public static async Task BuildAllAsync(
        IMcpBackendSession session,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        await WriteAsync(session, outputDirectory, "gds-001", BuildGds001Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-002", BuildGds002Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-003", BuildGds003Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-004", BuildGds004Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-005", BuildGds005Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-006", BuildGds006Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-007", BuildGds007Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-008", BuildGds008Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-009", BuildGds009Async, cancellationToken);
        await WriteAsync(session, outputDirectory, "gds-010", BuildGds010Async, cancellationToken);
    }

    public static async Task<IMcpBackendSession> ConnectMcpSessionAsync()
    {
        var repoRoot = GdsPaths.FindRepoRoot();
        var options = new SpaceMissionsAgentOptions
        {
            McpProjectPath = "src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj",
            DatasetPath = "dataset/space_missions.csv",
        };

        SpaceMissionsPathResolver.ApplyAbsolutePaths(
            options,
            Path.Combine(repoRoot, "src", "Chatbot"),
            AppContext.BaseDirectory);

        EnsureMcpServerBuilt(repoRoot);

        var service = new SpaceMissionsMcpAgentService(
            Options.Create(options),
            NullLoggerFactory.Instance,
            NullLogger<SpaceMissionsMcpAgentService>.Instance);

        return await service.ConnectAsync().ConfigureAwait(false);
    }

    private static void EnsureMcpServerBuilt(string repoRoot)
    {
        var builtDll = Path.Combine(
            repoRoot,
            "src",
            "SpaceMissions.McpServer",
            "bin",
            "Debug",
            "net8.0",
            SpaceMissionsPathResolver.McpServerAssemblyFileName);

        if (!File.Exists(builtDll)
            && SpaceMissionsPathResolver.GetBundledMcpServerDllPath(AppContext.BaseDirectory) is null)
        {
            throw new InvalidOperationException(
                "Build SpaceMissions.McpServer before generating GDS ground truth.");
        }
    }

    private static async Task WriteAsync(
        IMcpBackendSession session,
        string outputDirectory,
        string itemId,
        Func<IMcpBackendSession, CancellationToken, Task<GdsGroundTruthDocument>> builder,
        CancellationToken cancellationToken)
    {
        var document = await builder(session, cancellationToken).ConfigureAwait(false);
        var path = Path.Combine(outputDirectory, $"{itemId}.json");
        await File.WriteAllTextAsync(path, document.ToJson(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds001Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "get_space_missions_schema";
        var response = await CallAsync(session, tool, "{}", cancellationToken).ConfigureAwait(false);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["datasetRowCount"] = JsonElementFrom(response.GetProperty("datasetRowCount")),
            ["dateRangeMin"] = JsonElementFrom(response.GetProperty("dateRange").GetProperty("min")),
            ["dateRangeMax"] = JsonElementFrom(response.GetProperty("dateRange").GetProperty("max")),
            ["columnNames"] = JsonElementFrom(
                JsonSerializer.SerializeToElement(
                    response.GetProperty("columns").EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList())),
            ["knownMissionStatusValues"] = JsonElementFrom(response.GetProperty("knownMissionStatusValues")),
        };

        return CreateDocument("gds-001", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse("{}"), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds002Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "get_space_missions_summary";
        var response = await CallAsync(session, tool, "{}", cancellationToken).ConfigureAwait(false);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["totalRows"] = JsonElementFrom(response.GetProperty("totalRows")),
            ["dateMin"] = JsonElementFrom(response.GetProperty("dateMin")),
            ["dateMax"] = JsonElementFrom(response.GetProperty("dateMax")),
            ["missionStatusBreakdown"] = JsonElementFrom(response.GetProperty("missionStatusBreakdown")),
        };

        return CreateDocument("gds-002", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse("{}"), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds003Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "list_space_mission_distinct_values";
        const string args = """{"column":"Rocket","search":"Falcon","limit":100}""";
        var response = await CallAsync(session, tool, args, cancellationToken).ConfigureAwait(false);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["column"] = JsonElementFrom(response.GetProperty("column")),
            ["totalDistinct"] = JsonElementFrom(response.GetProperty("totalDistinct")),
            ["returned"] = JsonElementFrom(response.GetProperty("returned")),
            ["values"] = JsonElementFrom(response.GetProperty("values")),
        };

        return CreateDocument("gds-003", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse(args), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds004Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "filter_space_missions";
        const string args = """{"companyContains":"SpaceX","dateFrom":"2020-01-01","limit":10}""";
        var response = await CallAsync(session, tool, args, cancellationToken).ConfigureAwait(false);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["returned"] = JsonElementFrom(response.GetProperty("returned")),
            ["totalMatching"] = JsonElementFrom(response.GetProperty("totalMatching")),
            ["limit"] = JsonElementFrom(response.GetProperty("limit")),
            ["sampleRows"] = JsonElementFrom(response.GetProperty("rows")),
        };

        return CreateDocument("gds-004", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse(args), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds005Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "count_space_missions";
        const string args = """{"company":"SpaceX"}""";
        var response = await CallAsync(session, tool, args, cancellationToken).ConfigureAwait(false);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["spacexLaunchCount"] = JsonElementFrom(response.GetProperty("count")),
        };

        return CreateDocument("gds-005", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse(args), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds006Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "aggregate_space_missions";
        const string args = """{"groupBy":"MissionStatus"}""";
        var response = await CallAsync(session, tool, args, cancellationToken).ConfigureAwait(false);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["groupByColumn"] = JsonElementFrom(response.GetProperty("groupByColumn")),
            ["totalRows"] = JsonElementFrom(response.GetProperty("totalRows")),
            ["buckets"] = JsonElementFrom(response.GetProperty("buckets")),
            ["other"] = response.TryGetProperty("other", out var other)
                ? JsonElementFrom(other)
                : JsonSerializer.SerializeToElement((object?)null),
        };

        return CreateDocument("gds-006", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse(args), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds007Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "aggregate_space_missions_by_launch_country";
        const string args = "{}";
        var response = await CallAsync(session, tool, args, cancellationToken).ConfigureAwait(false);

        var usaBucket = response.GetProperty("buckets").EnumerateArray()
            .FirstOrDefault(b => b.GetProperty("bucket").GetString()?.Equals("USA", StringComparison.OrdinalIgnoreCase) == true);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["derivationRule"] = JsonElementFrom(response.GetProperty("derivationRule")),
            ["totalRows"] = JsonElementFrom(response.GetProperty("totalRows")),
            ["usaBucket"] = usaBucket.ValueKind == JsonValueKind.Undefined
                ? JsonSerializer.SerializeToElement((object?)null)
                : JsonElementFrom(usaBucket),
        };

        return CreateDocument("gds-007", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse(args), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds008Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string tool = "compute_space_mission_success_rate";
        const string args = """{"company":"SpaceX"}""";
        var response = await CallAsync(session, tool, args, cancellationToken).ConfigureAwait(false);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["totalMatching"] = JsonElementFrom(response.GetProperty("totalMatching")),
            ["successCount"] = JsonElementFrom(response.GetProperty("successCount")),
            ["denominator"] = JsonElementFrom(response.GetProperty("denominator")),
            ["successRatePercent"] = JsonElementFrom(response.GetProperty("successRatePercent")),
            ["formula"] = JsonElementFrom(response.GetProperty("formula")),
        };

        return CreateDocument("gds-008", [new GdsMcpCallRecord { Tool = tool, Arguments = Parse(args), Response = response }], keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds009Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string rateTool = "compute_space_mission_success_rate";
        const string rateArgs = """{"company":"SpaceX","dateFrom":"2020-01-01"}""";
        const string filterTool = "filter_space_missions";
        const string filterArgs = """{"company":"SpaceX","dateFrom":"2020-01-01","limit":5}""";

        var rateResponse = await CallAsync(session, rateTool, rateArgs, cancellationToken).ConfigureAwait(false);
        var filterResponse = await CallAsync(session, filterTool, filterArgs, cancellationToken).ConfigureAwait(false);
        filterResponse = TrimFilterRows(filterResponse, 3);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["successRatePercent"] = JsonElementFrom(rateResponse.GetProperty("successRatePercent")),
            ["totalMatching"] = JsonElementFrom(rateResponse.GetProperty("totalMatching")),
            ["successCount"] = JsonElementFrom(rateResponse.GetProperty("successCount")),
            ["denominator"] = JsonElementFrom(rateResponse.GetProperty("denominator")),
            ["exampleRows"] = JsonElementFrom(filterResponse.GetProperty("rows")),
            ["filterTotalMatching"] = JsonElementFrom(filterResponse.GetProperty("totalMatching")),
        };

        return CreateDocument(
            "gds-009",
            [
                new GdsMcpCallRecord { Tool = rateTool, Arguments = Parse(rateArgs), Response = rateResponse },
                new GdsMcpCallRecord { Tool = filterTool, Arguments = Parse(filterArgs), Response = filterResponse },
            ],
            keyFacts);
    }

    private static async Task<GdsGroundTruthDocument> BuildGds010Async(
        IMcpBackendSession session,
        CancellationToken cancellationToken)
    {
        const string countTool = "count_space_missions";
        const string countArgs = """{"company":"SpaceX"}""";
        const string filterTool = "filter_space_missions";
        const string filterArgs = """{"company":"SpaceX","limit":200}""";

        var countResponse = await CallAsync(session, countTool, countArgs, cancellationToken).ConfigureAwait(false);
        var filterResponse = await CallAsync(session, filterTool, filterArgs, cancellationToken).ConfigureAwait(false);
        filterResponse = TrimFilterRows(filterResponse, 3);

        var keyFacts = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["actualSpacexCount"] = JsonElementFrom(countResponse.GetProperty("count")),
            ["userClaimedCount"] = JsonSerializer.SerializeToElement(5000),
            ["filterReturned"] = JsonElementFrom(filterResponse.GetProperty("returned")),
            ["filterTotalMatching"] = JsonElementFrom(filterResponse.GetProperty("totalMatching")),
            ["filterLimit"] = JsonElementFrom(filterResponse.GetProperty("limit")),
            ["maxFilterLimit"] = JsonSerializer.SerializeToElement(200),
        };

        return CreateDocument(
            "gds-010",
            [
                new GdsMcpCallRecord { Tool = countTool, Arguments = Parse(countArgs), Response = countResponse },
                new GdsMcpCallRecord { Tool = filterTool, Arguments = Parse(filterArgs), Response = filterResponse },
            ],
            keyFacts);
    }

    private static GdsGroundTruthDocument CreateDocument(
        string itemId,
        IReadOnlyList<GdsMcpCallRecord> calls,
        Dictionary<string, JsonElement> keyFacts) =>
        new()
        {
            ItemId = itemId,
            GeneratedUtc = DateTime.UtcNow.ToString("O"),
            McpCalls = calls,
            KeyFacts = keyFacts,
        };

    private static JsonElement TrimFilterRows(JsonElement response, int maxRows)
    {
        if (!response.TryGetProperty("rows", out var rows) || rows.ValueKind != JsonValueKind.Array)
            return response;

        if (rows.GetArrayLength() <= maxRows)
            return response;

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in response.EnumerateObject())
            {
                if (property.NameEquals("rows"))
                {
                    writer.WritePropertyName("rows");
                    writer.WriteStartArray();
                    var index = 0;
                    foreach (var row in property.Value.EnumerateArray())
                    {
                        if (index++ >= maxRows)
                            break;
                        row.WriteTo(writer);
                    }
                    writer.WriteEndArray();
                    continue;
                }

                property.WriteTo(writer);
            }
            writer.WriteEndObject();
        }

        using var trimmed = JsonDocument.Parse(stream.ToArray());
        return trimmed.RootElement.Clone();
    }

    private static async Task<JsonElement> CallAsync(
        IMcpBackendSession session,
        string tool,
        string argsJson,
        CancellationToken cancellationToken)
    {
        var result = await session.CallToolAsync(tool, argsJson, cancellationToken).ConfigureAwait(false);
        if (result.IsError == true)
            throw new InvalidOperationException($"MCP tool '{tool}' returned an error.");

        var text = result.Content?.OfType<TextContentBlock>().FirstOrDefault()?.Text
            ?? throw new InvalidOperationException($"MCP tool '{tool}' returned no text content.");

        using var doc = JsonDocument.Parse(text);
        if (doc.RootElement.TryGetProperty("error", out var error))
            throw new InvalidOperationException($"MCP tool '{tool}' error: {error.GetString()}");

        return doc.RootElement.Clone();
    }

    private static JsonElement Parse(string json) =>
        JsonDocument.Parse(json).RootElement.Clone();

    private static JsonElement JsonElementFrom(JsonElement element) => element.Clone();
}
