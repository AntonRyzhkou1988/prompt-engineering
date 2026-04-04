using System.Net.Http.Headers;
using System.Text.Json;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;

namespace Rag;

internal sealed class RagOrchestrator
{
    private static readonly MediaTypeHeaderValue JsonMedia = new("application/json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

    private readonly IAiService _ai;
    private readonly RagSettings _settings;

    public RagOrchestrator(IAiService ai, RagSettings settings)
    {
        _ai = ai;
        _settings = settings;
    }

    public async Task<InMemoryVectorStore> BuildIndexAsync(CancellationToken cancellationToken)
    {
        var docRoot = Path.Combine(AppContext.BaseDirectory, _settings.DocumentsPath);
        if (!Directory.Exists(docRoot))
            throw new DirectoryNotFoundException($"Documents directory not found: {docRoot}");

        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".md", ".txt" };
        var paths = Directory
            .EnumerateFiles(docRoot, "*.*", SearchOption.AllDirectories)
            .Where(p => extensions.Contains(Path.GetExtension(p)))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var chunks = new List<DocumentChunk>();
        foreach (var path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var name = Path.GetFileName(path);
            var text = await File.ReadAllTextAsync(path, cancellationToken);
            foreach (var c in TextChunker.ChunkText(name, text, _settings.ChunkSizeChars, _settings.ChunkOverlapChars))
                chunks.Add(c);
        }

        if (chunks.Count == 0)
            throw new InvalidOperationException($"No text chunks produced under '{docRoot}'. Add .md or .txt files.");

        var store = new InMemoryVectorStore();
        var batchSize = _settings.EmbeddingBatchSize;

        for (var i = 0; i < chunks.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batch = chunks.Skip(i).Take(batchSize).ToList();
            var request = new EmbeddingRequest { Input = batch.Select(b => b.Text).ToList() };

            var response = await _ai.CreateEmbeddingsAsync(
                    _settings.InstanceName,
                    request,
                    JsonMedia,
                    JsonOptions,
                    cancellationToken)
                ?? throw new InvalidOperationException("Embedding API returned null.");

            if (response.Data is null || response.Data.Count != batch.Count)
                throw new InvalidOperationException(
                    $"Embedding batch size mismatch: expected {batch.Count}, got {response.Data?.Count ?? 0}.");

            var ordered = response.Data.OrderBy(d => d.Index).ToList();
            for (var j = 0; j < ordered.Count; j++)
            {
                var floats = ToFloatArray(ordered[j].Embedding);
                store.Add(new VectorRecord(batch[j].SourceFileName, batch[j].Text, floats));
            }
        }

        return store;
    }

    public async Task<string> AnswerAsync(InMemoryVectorStore store, string question, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.", nameof(question));

        var embedRequest = new EmbeddingRequest { Input = new List<string> { question.Trim() } };
        var embedResponse = await _ai.CreateEmbeddingsAsync(
                _settings.InstanceName,
                embedRequest,
                JsonMedia,
                JsonOptions,
                cancellationToken)
            ?? throw new InvalidOperationException("Embedding API returned null for the question.");

        var first = embedResponse.Data?.OrderBy(d => d.Index).FirstOrDefault()
            ?? throw new InvalidOperationException("No embedding returned for the question.");

        var queryVector = ToFloatArray(first.Embedding);
        var top = store.SearchTopK(queryVector, _settings.TopK);

        var contextBlocks = top
            .Select((r, idx) => $"[{idx + 1}] (source: {r.SourceFileName})\n{r.Text.Trim()}");
        var context = string.Join("\n---\n", contextBlocks);

        var userMessage =
            "Use only the context below to answer. If the answer is not contained in the context, say you do not know.\n\n" +
            "Context:\n" +
            context +
            "\n\nQuestion:\n" +
            question.Trim();

        var chatRequest = new ChatRequest { Temperature = 0.2f };
        chatRequest.AddSystemMessage(
            "You are a precise assistant. Ground every factual claim in the provided context. Do not invent policies, numbers, or contacts.");
        chatRequest.AddUserMessage(userMessage);

        var completion = await _ai.CompleteChatAsync(
                _settings.InstanceName,
                chatRequest,
                JsonMedia,
                JsonOptions,
                cancellationToken)
            ?? throw new InvalidOperationException("Chat API returned null.");

        return completion.Choices?.FirstOrDefault()?.Message?.Content?.Trim()
               ?? string.Empty;
    }

    private static float[] ToFloatArray(IReadOnlyList<float> embedding)
    {
        if (embedding is float[] arr)
            return (float[])arr.Clone();

        var copy = new float[embedding.Count];
        for (var i = 0; i < embedding.Count; i++)
            copy[i] = embedding[i];
        return copy;
    }
}
