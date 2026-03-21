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
    private const string RequestMediaType = "application/json";
    private const string DataStartTag = "<data>";
    private const string DataEndTag = "</data>";
    private const string YearHeader = "Year";
    private const string CountryHeader = "Country";
    private const string TypeHeader = "Type";
    private const string ActivityHeader = "Activity";
    private const string InjuryHeader = "Injury";
    private const string FatalYnHeader = "Fatal (Y/N)";
    private const string AgeHeader = "Age";
    private const string TimeHeader = "Time";
    private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.General);
    private readonly SystemSettings _systemSettings;
    private readonly ContextSettings _contextSettings;
    private readonly string _instanceName;
    private readonly IAiService _aiService;

    public ContextService(
        IOptions<SystemSettings> systemSettings,
        IOptions<ContextSettings> contextSettings,
        IOptions<ContextPromptsOptions> prompts,
        IAiService aiService)
    {
        ArgumentNullException.ThrowIfNull(systemSettings);
        ArgumentNullException.ThrowIfNull(contextSettings);
        ArgumentNullException.ThrowIfNull(prompts);
        ArgumentNullException.ThrowIfNull(aiService);

        if (systemSettings.Value.AiServiceSettings.Instances.Count == 0)
            throw new ArgumentException("AiServiceSettings instance count is 0.");

        _contextSettings = contextSettings.Value;
        _instanceName = systemSettings.Value.AiServiceSettings.Instances.First().Name;
        _systemSettings = systemSettings.Value;
        _aiService = aiService;
    }

    public async Task<IReadOnlyList<ContextPipelineResult>> RunReActAsync(CancellationToken cancellationToken)
    {
        var promptPaths = PromptJsonDiscovery.GetOrderedPromptJsonFullPaths(_contextSettings.PromptPath);

        var results = new List<ContextPipelineResult>(promptPaths.Count);

        foreach (var promptFilePath in promptPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Console.WriteLine($"Running pipeline for {promptFilePath}...");

            var pipelineResult = await RunFromPromptPathAsync(promptFilePath, cancellationToken);
            results.Add(pipelineResult);

            var completion = pipelineResult.Completion;

            if (completion.Choices == null || !completion.Choices.Any())
            {
                Console.WriteLine($"First choice saved to {pipelineResult.OutputPath} (no choices returned).");
                continue;
            }

            var choice = completion.Choices.First();
            var messageContent = choice?.Message?.Content;

            if (!string.IsNullOrWhiteSpace(messageContent))
            {
                Console.WriteLine(messageContent);
            }

            Console.WriteLine($"Saved assistant Markdown: {pipelineResult.OutputPath}");
        }

        return results;
    }


    private Task<ContextPipelineResult> RunFromPromptPathAsync(
        string promptPath,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(promptPath);

        var resolved = ContextSettingsPromptPathResolver.ResolveExistingFilePath(promptPath)
            ?? throw new InvalidOperationException(
                $"Context prompts JSON was not found. Tried path '{promptPath}' (resolved from app base and parent directories).");

        var loaded = ContextPromptsJsonLoader.LoadFromResolvedPath(resolved);
        var stem = Path.GetFileNameWithoutExtension(resolved);
        return RunCoreAsync(loaded, stem, cancellationToken);
    }

    private async Task<ContextPipelineResult> RunCoreAsync(
        ContextPromptsOptions prompts,
        string? outputFileStem,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var datasetSnapshot = await LoadDatasetAsync(
            _contextSettings.DatasetPath,
            _systemSettings.MaximumDatasetRecordCount,
            cancellationToken);

        var chatRequest = BuildChatRequest(datasetSnapshot, prompts, _contextSettings.Temperature);

        var completion = await _aiService.CompleteChatAsync(
            _instanceName,
            chatRequest,
            new MediaTypeHeaderValue(RequestMediaType),
            DefaultJsonOptions,
            cancellationToken);

        if (completion == null)
        {
            throw new InvalidOperationException("Completion is null.");
        }


        var promptStem = string.IsNullOrWhiteSpace(outputFileStem) ? "prompt" : outputFileStem;
        var fileName = $"completion_{promptStem}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.md";
        var outputPath = Path.Combine(_contextSettings.OutputDirectory, fileName);

        var firstChoice = completion.Choices?.FirstOrDefault();
        var markdown = BuildFirstChoiceAssistantMarkdown(firstChoice);
        await File.WriteAllTextAsync(outputPath, markdown, cancellationToken);

        return new ContextPipelineResult(outputPath, completion);
    }

    /// <summary>
    /// Returns only the first choice assistant <see cref="ChatMessage.Content"/> (Markdown as returned by the model).
    /// </summary>
    private static string BuildFirstChoiceAssistantMarkdown(ChatCompletionChoice? firstChoice)
    {
        var content = firstChoice?.Message?.Content;
        return string.IsNullOrWhiteSpace(content) ? string.Empty : content;
    }

    private static ChatRequest BuildChatRequest(
        DatasetSnapshot datasetSnapshot,
        ContextPromptsOptions prompts,
        float temperature)
    {
        var assistantRole = JoinSentences(prompts.DefaultAssistantRole);
        var baseUserPrompt = JoinSentences(prompts.DefaultUserPrompt);
        var recordsMarkup = BuildDataXmlMarkup(datasetSnapshot.Records);
        var userPrompt = InjectDataMarkup(baseUserPrompt, recordsMarkup);

        var chatRequest = new ChatRequest
        {
            Temperature = temperature
        };
        chatRequest.AddSystemMessage(assistantRole);
        chatRequest.AddUserMessage(userPrompt);

        return chatRequest;
    }

    private static async Task<DatasetSnapshot> LoadDatasetAsync(
        string datasetPath,
        int maximumDatasetRecordCount,
        CancellationToken cancellationToken)
    {
        var records = new List<AttackRecord>();

        await using var stream = new FileStream(
            datasetPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        using var reader = new StreamReader(stream);

        var headerRow = await ReadNextCsvRecordAsync(reader, cancellationToken);
        if (headerRow == null || headerRow.Length == 0)
        {
            throw new InvalidOperationException($"Dataset file '{datasetPath}' is empty.");
        }

        var headerIndexLookup = BuildHeaderIndexLookup(headerRow);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dataRow = await ReadNextCsvRecordAsync(reader, cancellationToken);
            if (dataRow == null)
            {
                break;
            }

            if (dataRow.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            records.Add(MapRecord(dataRow, headerIndexLookup));

            if (maximumDatasetRecordCount > 0 && records.Count >= maximumDatasetRecordCount)
            {
                break;
            }
        }

        return new DatasetSnapshot(records, records.Count);
    }

    private static Dictionary<string, int> BuildHeaderIndexLookup(string[] headers)
    {
        var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < headers.Length; index++)
        {
            var normalizedHeader = NormalizeField(headers[index]);
            if (!string.IsNullOrWhiteSpace(normalizedHeader) && !lookup.ContainsKey(normalizedHeader))
            {
                lookup.Add(normalizedHeader, index);
            }
        }

        return lookup;
    }

    private static AttackRecord MapRecord(
        string[] row,
        IReadOnlyDictionary<string, int> headerIndexLookup)
    {
        return new AttackRecord
        {
            Year = GetFieldValue(row, headerIndexLookup, YearHeader),
            Country = GetFieldValue(row, headerIndexLookup, CountryHeader),
            Type = GetFieldValue(row, headerIndexLookup, TypeHeader),
            Activity = GetFieldValue(row, headerIndexLookup, ActivityHeader),
            Injury = GetFieldValue(row, headerIndexLookup, InjuryHeader),
            FatalYn = GetFieldValue(row, headerIndexLookup, FatalYnHeader),
            Age = GetFieldValue(row, headerIndexLookup, AgeHeader),
            Time = GetFieldValue(row, headerIndexLookup, TimeHeader)
        };
    }

    private static string? GetFieldValue(
        IReadOnlyList<string> row,
        IReadOnlyDictionary<string, int> headerIndexLookup,
        string headerName)
    {
        if (!headerIndexLookup.TryGetValue(headerName, out var index))
        {
            return null;
        }

        if (index < 0 || index >= row.Count)
        {
            return null;
        }

        var field = NormalizeField(row[index]);
        return string.IsNullOrWhiteSpace(field) ? null : field;
    }

    private static async Task<string[]?> ReadNextCsvRecordAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        var rawRecord = await ReadRawCsvRecordAsync(reader, cancellationToken);
        return rawRecord == null ? null : ParseCsvRecord(rawRecord);
    }

    private static async Task<string?> ReadRawCsvRecordAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        if (reader.EndOfStream)
        {
            return null;
        }

        var buffer = new StringBuilder();
        var insideQuotes = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
            {
                break;
            }

            if (buffer.Length > 0)
            {
                buffer.Append('\n');
            }

            buffer.Append(line);
            insideQuotes = UpdateQuoteState(line, insideQuotes);
            if (!insideQuotes)
            {
                break;
            }
        }

        if (insideQuotes)
        {
            throw new InvalidOperationException("CSV parsing failed due to unclosed quote sequence.");
        }

        return buffer.ToString();
    }

    private static bool UpdateQuoteState(string line, bool insideQuotes)
    {
        for (var index = 0; index < line.Length; index++)
        {
            if (line[index] != '"')
            {
                continue;
            }

            if (insideQuotes && index + 1 < line.Length && line[index + 1] == '"')
            {
                index++;
                continue;
            }

            insideQuotes = !insideQuotes;
        }

        return insideQuotes;
    }

    private static string[] ParseCsvRecord(string record)
    {
        var fields = new List<string>();
        var currentField = new StringBuilder();
        var insideQuotes = false;

        for (var index = 0; index < record.Length; index++)
        {
            var character = record[index];
            if (character == '"')
            {
                if (insideQuotes && index + 1 < record.Length && record[index + 1] == '"')
                {
                    currentField.Append('"');
                    index++;
                }
                else
                {
                    insideQuotes = !insideQuotes;
                }

                continue;
            }

            if (character == ',' && !insideQuotes)
            {
                fields.Add(currentField.ToString());
                currentField.Clear();
                continue;
            }

            currentField.Append(character);
        }

        fields.Add(currentField.ToString());
        return fields.ToArray();
    }

    /// <summary>
    /// Builds XML for all dataset rows injected between the user prompt's &lt;data&gt; and &lt;/data&gt; tags.
    /// Concatenates one &lt;record&gt;...&lt;/record&gt; element per CSV data row (child element names match configuration).
    /// </summary>
    private static string BuildDataXmlMarkup(IReadOnlyList<AttackRecord> records)
    {
        if (records.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(records.Count * 120);
        foreach (var record in records)
        {
            builder.Append("<record>");
            AppendXmlElement(builder, "Year", record.Year);
            AppendXmlElement(builder, "Country", record.Country);
            AppendXmlElement(builder, "Type", record.Type);
            AppendXmlElement(builder, "Activity", record.Activity);
            AppendXmlElement(builder, "Injury", record.Injury);
            AppendXmlElement(builder, "FatalYN", record.FatalYn);
            AppendXmlElement(builder, "Age", record.Age);
            AppendXmlElement(builder, "Time", record.Time);
            builder.Append("</record>");
        }

        return builder.ToString();
    }

    private static void AppendXmlElement(StringBuilder builder, string elementName, string? value)
    {
        builder.Append('<').Append(elementName).Append('>');
        builder.Append(EscapeXmlText(value));
        builder.Append("</").Append(elementName).Append('>');
    }

    /// <summary>
    /// Escapes text for use as XML element character data.
    /// </summary>
    private static string EscapeXmlText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        ReadOnlySpan<char> span = value.AsSpan();
        var needsEscape = false;
        foreach (var ch in span)
        {
            if (ch is '<' or '>' or '&' or '"' or '\'')
            {
                needsEscape = true;
                break;
            }
        }

        if (!needsEscape)
        {
            return value;
        }

        var sb = new StringBuilder(value.Length + 8);
        foreach (var ch in span)
        {
            switch (ch)
            {
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '&':
                    sb.Append("&amp;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&apos;");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }

        return sb.ToString();
    }

    private static string InjectDataMarkup(string baseUserPrompt, string dataListMarkup)
    {
        var startTagIndex = baseUserPrompt.IndexOf(DataStartTag, StringComparison.OrdinalIgnoreCase);
        var endTagIndex = baseUserPrompt.IndexOf(DataEndTag, StringComparison.OrdinalIgnoreCase);

        if (startTagIndex < 0 || endTagIndex < 0 || endTagIndex < startTagIndex)
        {
            throw new InvalidOperationException(
                $"User prompt must contain a valid '{DataStartTag}...{DataEndTag}' section.");
        }

        var contentStartIndex = startTagIndex + DataStartTag.Length;
        var startContent = baseUserPrompt[..contentStartIndex];
        var endContent = baseUserPrompt[endTagIndex..];
        var builder = new StringBuilder(startContent.Length + endContent.Length + dataListMarkup.Length + 8);

        builder.Append(startContent);
        if (!startContent.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            builder.AppendLine();
        }

        builder.AppendLine(dataListMarkup);
        builder.Append(endContent);

        return builder.ToString();
    }

    private static string NormalizeField(string? value) => value?.Trim() ?? string.Empty;

    private sealed record DatasetSnapshot(IReadOnlyList<AttackRecord> Records, int DataRowsCount);

    private static string JoinSentences(IEnumerable<string>? sentences)
    {
        if (sentences == null)
        {
            return string.Empty;
        }

        return string.Join(
            Environment.NewLine,
            sentences.Where(sentence => !string.IsNullOrWhiteSpace(sentence)));
    }
}
