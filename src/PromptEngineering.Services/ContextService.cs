using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.Services;

/// <summary>
/// Orchestrates prompt-engineering workflow:
/// dataset load, prompt construction, completion call, and persisting the first choice assistant message as Markdown.
/// </summary>
public sealed class ContextService : IContextService
{
    private const string DataStartTag = "<data>";
    private const string DataEndTag = "</data>";
    private const string PriorRunStartTag = "<prior_run>";
    private const string PriorRunEndTag = "</prior_run>";
    private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.General);

    private readonly SystemSettings _systemSettings;
    private readonly ContextSettings _contextSettings;
    private readonly string _instanceName;
    private readonly IAiService _aiService;

    public ContextService(
        IOptions<SystemSettings> systemSettings,
        IOptions<ContextSettings> contextSettings,
        IAiService aiService)
    {
        ArgumentNullException.ThrowIfNull(systemSettings);
        ArgumentNullException.ThrowIfNull(contextSettings);
        ArgumentNullException.ThrowIfNull(aiService);

        if (systemSettings.Value.AiServiceSettings.Instances.Count == 0)
            throw new ArgumentException("AiServiceSettings instance count is 0.");

        _contextSettings = contextSettings.Value;
        _instanceName = systemSettings.Value.AiServiceSettings.Instances.First().Name;
        _systemSettings = systemSettings.Value;
        _aiService = aiService;
    }

    public async Task<IReadOnlyList<ContextPipelineResult>> RunReActAsync(
        CancellationToken cancellationToken = default)
    {
        if (_contextSettings.ReActSequence.Count == 0)
            throw new ArgumentException("ReActSequence must contain at least one entry.");

        var records = await LoadDatasetAsync(
            _contextSettings.DatasetPath,
            _systemSettings.MaximumDatasetRecordCount,
            cancellationToken);

        var results = new List<ContextPipelineResult>(_contextSettings.ReActSequence.Count);
        string? priorCompletion = null;

        foreach (var fileName in _contextSettings.ReActSequence)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var promptPath = Path.Combine(_contextSettings.PromptPath, fileName);
            var stem = Path.GetFileNameWithoutExtension(promptPath);

            WriteColored($"Running: {stem}...", ConsoleColor.DarkMagenta);

            try
            {
                var prompt = ContextPromptsJsonLoader.LoadFromResolvedPath(promptPath);
                var result = await RunCoreAsync(records, prompt, stem, priorCompletion, cancellationToken);
                results.Add(result);

                priorCompletion = result.Completion.Choices?.FirstOrDefault()?.Message?.Content;
                if (!string.IsNullOrWhiteSpace(priorCompletion))
                    WriteColored(priorCompletion, ConsoleColor.DarkGreen);

                WriteColored($"Saved: {result.OutputPath}", ConsoleColor.DarkCyan);
            }
            catch (Exception e)
            {
                WriteColored(e.ToString(), ConsoleColor.DarkRed);
                throw new Exception("Execution failed with system exception.", e);
            }
        }

        return results;
    }

    private async Task<ContextPipelineResult> RunCoreAsync(
        IReadOnlyList<AttackRecord> records,
        ContextPrompt prompt,
        string? stem,
        string? priorCompletion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var chatRequest = BuildChatRequest(records, prompt, _contextSettings.Temperature, priorCompletion);

        var completion = await _aiService.CompleteChatAsync(
            _instanceName,
            chatRequest,
            new MediaTypeHeaderValue("application/json"),
            DefaultJsonOptions,
            cancellationToken) ?? throw new InvalidOperationException("Completion is null.");

        var promptStem = string.IsNullOrWhiteSpace(stem) ? "prompt" : stem;
        var outputPath = Path.Combine(
            _contextSettings.OutputDirectory,
            $"completion_{promptStem}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.md");

        var markdown = completion.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
        await File.WriteAllTextAsync(outputPath, markdown, cancellationToken);

        return new ContextPipelineResult(outputPath, promptStem, completion);
    }

    // ── Prompt assembly ──────────────────────────────────────────────────────

    private static ChatRequest BuildChatRequest(
        IReadOnlyList<AttackRecord> records,
        ContextPrompt prompt,
        float temperature,
        string? priorCompletion = null)
    {
        var userPrompt = JoinLines(prompt.DefaultUserPrompt);
        userPrompt = InjectRegion(userPrompt, DataStartTag, DataEndTag, BuildDataXml(records), required: true);

        if (!string.IsNullOrWhiteSpace(priorCompletion))
            userPrompt = InjectRegion(userPrompt, PriorRunStartTag, PriorRunEndTag, priorCompletion, required: false);

        var request = new ChatRequest { Temperature = temperature };
        request.AddSystemMessage(JoinLines(prompt.DefaultAssistantRole));
        request.AddUserMessage(userPrompt);
        return request;
    }

    /// <summary>
    /// Replaces the inner content of a <paramref name="startTag"/>...<paramref name="endTag"/> region with
    /// <paramref name="content"/>. Throws when <paramref name="required"/> is true and the region is absent;
    /// returns the original prompt unchanged otherwise.
    /// </summary>
    internal static string InjectRegion(
        string prompt, string startTag, string endTag, string content, bool required)
    {
        var startIndex = prompt.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        var endIndex = prompt.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

        if (startIndex < 0 || endIndex < 0 || endIndex < startIndex)
        {
            if (required)
                throw new InvalidOperationException($"Prompt must contain a valid '{startTag}...{endTag}' section.");
            return prompt;
        }

        var before = prompt[..(startIndex + startTag.Length)];
        var after = prompt[endIndex..];
        return new StringBuilder(before.Length + content.Length + after.Length + 4)
            .Append(before).AppendLine()
            .AppendLine(content)
            .Append(after)
            .ToString();
    }

    // ── XML markup ───────────────────────────────────────────────────────────

    private static string BuildDataXml(IReadOnlyList<AttackRecord> records)
    {
        if (records.Count == 0) return string.Empty;

        var sb = new StringBuilder(records.Count * 120);
        foreach (var r in records)
        {
            sb.Append("<record>");
            Xml(sb, "Year", r.Year);
            Xml(sb, "Country", r.Country);
            Xml(sb, "Area", r.Area);
            Xml(sb, "Type", r.Type);
            Xml(sb, "Activity", r.Activity);
            Xml(sb, "Injury", r.Injury);
            Xml(sb, "FatalYN", r.FatalYn);
            Xml(sb, "Sex", r.Sex);
            Xml(sb, "Age", r.Age);
            Xml(sb, "Time", r.Time);
            Xml(sb, "Species", r.Species);
            Xml(sb, "InvestigatorSource", r.InvestigatorSource);
            sb.Append("</record>");
        }
        return sb.ToString();
    }

    private static void Xml(StringBuilder sb, string name, string? value)
    {
        sb.Append('<').Append(name).Append('>');
        sb.Append(EscapeXml(value));
        sb.Append("</").Append(name).Append('>');
    }

    /// <summary>Escapes text for use as XML element character data.</summary>
    internal static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.AsSpan().IndexOfAny("<>&\"'".AsSpan()) < 0) return value;

        var sb = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '&': sb.Append("&amp;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&apos;"); break;
                default: sb.Append(ch); break;
            }
        }
        return sb.ToString();
    }

    // ── CSV loading ──────────────────────────────────────────────────────────

    private static async Task<IReadOnlyList<AttackRecord>> LoadDatasetAsync(
        string datasetPath,
        int maxRecords,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(datasetPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);

        var headerRow = await ReadNextCsvRecordAsync(reader, cancellationToken);
        if (headerRow == null || headerRow.Length == 0)
            throw new InvalidOperationException($"Dataset file '{datasetPath}' is empty.");

        var headerIndex = BuildHeaderIndex(headerRow);
        var records = new List<AttackRecord>();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = await ReadNextCsvRecordAsync(reader, cancellationToken);
            if (row == null) break;
            if (row.All(string.IsNullOrWhiteSpace)) continue;

            records.Add(MapRecord(row, headerIndex));
            if (maxRecords > 0 && records.Count >= maxRecords) break;
        }

        return records;
    }

    private static Dictionary<string, int> BuildHeaderIndex(string[] headers)
    {
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            var name = headers[i].Trim();
            if (!string.IsNullOrWhiteSpace(name) && !index.ContainsKey(name))
                index[name] = i;
        }
        return index;
    }

    private static AttackRecord MapRecord(string[] row, IReadOnlyDictionary<string, int> index) => new()
    {
        Year                = Field(row, index, "Year"),
        Country             = Field(row, index, "Country"),
        Area                = Field(row, index, "Area"),
        Type                = Field(row, index, "Type"),
        Activity            = Field(row, index, "Activity"),
        Injury              = Field(row, index, "Injury"),
        FatalYn             = Field(row, index, "Fatal (Y/N)"),
        Sex                 = Field(row, index, "Sex"),
        Age                 = Field(row, index, "Age"),
        Time                = Field(row, index, "Time"),
        Species             = Field(row, index, "Species"),
        InvestigatorSource  = Field(row, index, "Investigator or Source")
    };

    private static string? Field(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> index, string name)
    {
        if (!index.TryGetValue(name, out var i) || i < 0 || i >= row.Count) return null;
        var value = row[i].Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static async Task<string[]?> ReadNextCsvRecordAsync(StreamReader reader, CancellationToken ct)
    {
        var raw = await ReadRawCsvRecordAsync(reader, ct);
        return raw == null ? null : ParseCsvRecord(raw);
    }

    private static async Task<string?> ReadRawCsvRecordAsync(StreamReader reader, CancellationToken ct)
    {
        if (reader.EndOfStream) return null;

        var buffer = new StringBuilder();
        var insideQuotes = false;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line == null) break;

            if (buffer.Length > 0) buffer.Append('\n');
            buffer.Append(line);
            insideQuotes = UpdateQuoteState(line, insideQuotes);
            if (!insideQuotes) break;
        }

        if (insideQuotes)
            throw new InvalidOperationException("CSV parsing failed due to unclosed quote sequence.");

        return buffer.ToString();
    }

    internal static bool UpdateQuoteState(string line, bool insideQuotes)
    {
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] != '"') continue;
            if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"') { i++; continue; }
            insideQuotes = !insideQuotes;
        }
        return insideQuotes;
    }

    internal static string[] ParseCsvRecord(string record)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var insideQuotes = false;

        for (var i = 0; i < record.Length; i++)
        {
            var ch = record[i];
            if (ch == '"')
            {
                if (insideQuotes && i + 1 < record.Length && record[i + 1] == '"') { current.Append('"'); i++; }
                else insideQuotes = !insideQuotes;
                continue;
            }
            if (ch == ',' && !insideQuotes) { fields.Add(current.ToString()); current.Clear(); continue; }
            current.Append(ch);
        }

        fields.Add(current.ToString());
        return [.. fields];
    }

    // ── Utilities ────────────────────────────────────────────────────────────

    private static string JoinLines(IEnumerable<string>? lines) =>
        lines == null
            ? string.Empty
            : string.Join(Environment.NewLine, lines.Where(s => !string.IsNullOrWhiteSpace(s)));

    private static void WriteColored(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = ConsoleColor.White;
    }
}
