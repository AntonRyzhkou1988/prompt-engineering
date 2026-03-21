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

        var datasetExcerpt = await LoadDatasetExcerptAsync(
            datasetPath,
            _contextSettings.MaxDatasetRowsInPrompt,
            cancellationToken);

        var chatRequest = BuildChatRequest(datasetExcerpt);

        var completion = await _aiService.CompleteChatAsync(
            _contextSettings.AiInstanceName,
            chatRequest,
            new MediaTypeHeaderValue(RequestMediaType),
            new JsonSerializerOptions(JsonSerializerDefaults.General),
            cancellationToken);

        if (completion == null)
        {
            throw new InvalidOperationException("Completion is null.");
        }

        Directory.CreateDirectory(outputDirectoryPath);
        var outputPath = Path.Combine(
            outputDirectoryPath,
            $"completion_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.json");

        var completionJson = JsonSerializer.Serialize(
            completion,
            new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, completionJson, cancellationToken);

        return new ContextPipelineResult(outputPath, completion);
    }

    private ChatRequest BuildChatRequest(string datasetExcerpt)
    {
        var datasetSection = $"""
                              Dataset excerpt (header and first {_contextSettings.MaxDatasetRowsInPrompt} rows):
                              {datasetExcerpt}
                              """;

        var userPrompt = new StringBuilder(_contextSettings.DefaultUserPrompt)
            .AppendLine()
            .AppendLine()
            .Append(datasetSection)
            .ToString();

        var chatRequest = new ChatRequest
        {
            Temperature = _contextSettings.Temperature
        };
        chatRequest.AddSystemMessage(_contextSettings.DefaultAssistantRole);
        chatRequest.AddUserMessage(userPrompt);

        return chatRequest;
    }

    private static async Task<string> LoadDatasetExcerptAsync(
        string datasetPath,
        int maxRows,
        CancellationToken cancellationToken)
    {
        if (maxRows < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRows), "Max dataset rows should be greater than zero.");
        }

        var lines = new List<string>(maxRows + 1);

        await using var stream = new FileStream(
            datasetPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && lines.Count <= maxRows)
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

        return string.Join(Environment.NewLine, lines);
    }

    private static string ResolveExistingFilePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.IsPathRooted(path))
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Dataset file was not found.", path);
            }

            return path;
        }

        var resolvedPath = ResolvePathFromCurrentOrParents(path);
        if (resolvedPath == null || !File.Exists(resolvedPath))
        {
            throw new FileNotFoundException(
                $"Dataset file '{path}' was not found relative to current directory or its parents.",
                path);
        }

        return resolvedPath;
    }

    private static string ResolveDirectoryPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (Path.IsPathRooted(path))
        {
            return path;
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
}
