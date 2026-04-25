using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agent;

public sealed class EvalDataset
{
    [JsonPropertyName("items")]
    public List<EvalItemDto> Items { get; set; } = new();
}

public sealed class EvalItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("question")]
    public string Question { get; set; } = "";

    [JsonPropertyName("expected_domains")]
    public List<string> ExpectedDomains { get; set; } = new();
}

public static class EvalDatasetLoader
{
    public static async Task<EvalDataset> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<EvalDataset>(stream, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        return data ?? throw new InvalidOperationException("Eval dataset is empty or invalid.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };
}
