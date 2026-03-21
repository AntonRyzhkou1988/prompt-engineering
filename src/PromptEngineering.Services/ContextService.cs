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
    private readonly ContextSettings _contextSettings;
    private readonly IAiService _aiService;

    public ContextService(
        IOptions<ContextSettings> contextSettings,
        IAiService aiService)
    {
        ArgumentNullException.ThrowIfNull(contextSettings);
        ArgumentNullException.ThrowIfNull(aiService);

        _contextSettings = contextSettings.Value;
        _aiService = aiService;
    }

    public async Task<ContextPipelineResult> RunAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var datasetPath = ResolveExistingFilePath(_contextSettings.DatasetPath);
        var outputDirectoryPath = ResolveDirectoryPath(_contextSettings.OutputDirectory);

        var datasetSnapshot = await LoadDatasetAsync(
            datasetPath,
            cancellationToken);

        var chatRequest = BuildChatRequest(datasetSnapshot);

        var completion = await _aiService.CompleteChatAsync(
            _contextSettings.AiInstanceName,
            chatRequest,
            new MediaTypeHeaderValue(RequestMediaType),
            DefaultJsonOptions,
            cancellationToken);

        if (completion == null)
        {
            throw new InvalidOperationException("Completion is null.");
        }

        Directory.CreateDirectory(outputDirectoryPath);
        var safeInstanceName = string.Concat(
            _contextSettings.AiInstanceName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        var outputPath = Path.Combine(
            outputDirectoryPath,
            $"completion_{safeInstanceName}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.md");

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

    private ChatRequest BuildChatRequest(DatasetSnapshot datasetSnapshot)
    {
        var assistantRole = JoinSentences(_contextSettings.DefaultAssistantRole);
        var baseUserPrompt = JoinSentences(_contextSettings.DefaultUserPrompt);
        var recordsMarkup = BuildDataXmlMarkup(datasetSnapshot.Records);
        var userPrompt = InjectDataMarkup(baseUserPrompt, recordsMarkup);

        var chatRequest = new ChatRequest
        {
            Temperature = _contextSettings.Temperature
        };
        chatRequest.AddSystemMessage(assistantRole);
        chatRequest.AddUserMessage(userPrompt);

        return chatRequest;
    }

    private static async Task<DatasetSnapshot> LoadDatasetAsync(
        string datasetPath,
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
        }

        return new DatasetSnapshot(records, records.Count);
    }

    private static string ResolveExistingFilePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Path.IsPathRooted(path))
        {
            throw new ArgumentException(
                $"DatasetPath must be an absolute path, but '{path}' is relative.",
                nameof(path));
        }

        var absolutePath = Path.GetFullPath(path);
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Dataset file was not found.", absolutePath);
        }

        return absolutePath;
    }

    private static string ResolveDirectoryPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var resolvedPath = ResolvePathFromCurrentOrParents(path);
        return resolvedPath ?? Path.GetFullPath(path, Directory.GetCurrentDirectory());
    }

    private static string? ResolvePathFromCurrentOrParents(string relativePath)
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current != null)
        {
            var candidate = Path.GetFullPath(relativePath, current.FullName);
            if (File.Exists(candidate) || Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
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
