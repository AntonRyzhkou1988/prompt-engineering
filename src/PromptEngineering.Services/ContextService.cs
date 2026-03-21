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
/// dataset load, prompt construction, completion call, and completion persistence.
/// </summary>
public sealed class ContextService : IContextService
{
    private const string RequestMediaType = "application/json";
    private static readonly JsonSerializerOptions DefaultJsonOptions = new(JsonSerializerDefaults.General);
    private static readonly JsonSerializerOptions OutputJsonOptions = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true
    };
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
            $"completion_{safeInstanceName}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json");

        var completionJson = JsonSerializer.Serialize(completion, OutputJsonOptions);
        await File.WriteAllTextAsync(outputPath, completionJson, cancellationToken);

        return new ContextPipelineResult(outputPath, completion);
    }

    private ChatRequest BuildChatRequest(DatasetSnapshot datasetSnapshot)
    {
        var assistantRole = JoinSentences(_contextSettings.DefaultAssistantRole);
        var baseUserPrompt = JoinSentences(_contextSettings.DefaultUserPrompt);

        var datasetSection = $"""
                              Full dataset content loaded from file (header and all {datasetSnapshot.DataRowsCount} rows):
                              {datasetSnapshot.Content}
                              """;

        var userPrompt = new StringBuilder(baseUserPrompt)
            .AppendLine()
            .AppendLine()
            .Append(datasetSection)
            .ToString();

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
        var lines = new List<string>();

        await using var stream = new FileStream(
            datasetPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line != null)
            {
                lines.Add(line);
            }
        }

        if (lines.Count == 0)
        {
            throw new InvalidOperationException($"Dataset file '{datasetPath}' is empty.");
        }

        var dataRowsCount = Math.Max(0, lines.Count - 1);
        return new DatasetSnapshot(string.Join(Environment.NewLine, lines), dataRowsCount);
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

    private sealed record DatasetSnapshot(string Content, int DataRowsCount);

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
