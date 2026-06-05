using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Rag;

public sealed class RagOrchestrator
{
    private static readonly MediaTypeHeaderValue JsonMedia = new("application/json");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.General);

    private readonly IAiService _ai;
    private readonly RagSettings _settings;
    private readonly ILogger<RagOrchestrator> _logger;

    public RagOrchestrator(IAiService ai, IOptions<RagSettings> options, ILogger<RagOrchestrator> logger)
    {
        _ai = ai;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<InMemoryVectorStore> BuildIndexAsync(CancellationToken cancellationToken)
    {
        var datasetPath = _settings.ResolveDatasetPath(AppContext.BaseDirectory);
        var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".md", ".txt", ".csv" };

        if (File.Exists(datasetPath))
        {
            var ext = Path.GetExtension(datasetPath);
            if (!extensions.Contains(ext))
                throw new InvalidOperationException(
                    $"Rag:DatasetPath points to a file with unsupported extension '{ext}'. Use .md, .txt, or .csv: {datasetPath}");
        }
        else
        {
            throw new FileNotFoundException(
                $"Rag dataset not found (Rag:DatasetPath). Expected an existing file or directory: {datasetPath}");
        }

        var chunks = new List<DocumentChunk>();
        var name = Path.GetFileName(datasetPath);
        if (string.Equals(Path.GetExtension(datasetPath), ".csv", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var c in await CsvDocumentChunker.ChunkFileAsync(
                         datasetPath,
                         name,
                         _settings.Csv,
                         _settings.ChunkSizeChars,
                         _settings.ChunkOverlapChars,
                         cancellationToken))
                chunks.Add(c);
        }
        else
        {
            var text = await File.ReadAllTextAsync(datasetPath, cancellationToken);
            foreach (var c in TextChunker.ChunkText(name, text, _settings.ChunkSizeChars, _settings.ChunkOverlapChars))
                chunks.Add(c);
        }

        if (chunks.Count == 0)
            throw new InvalidOperationException(
                $"No text chunks produced for Rag:DatasetPath '{datasetPath}'. Add .md, .txt, or non-empty .csv files.");

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
                var chunk = batch[j];
                store.Add(new VectorRecord(chunk.SourceFileName, chunk.Text, floats));
                var chunkNumber = i + j + 1;
                _logger.LogInformation(
                    "Indexed chunk {ChunkNumber}/{TotalChunks}: source={Source}, chars={CharCount}, embeddingDims={Dims}",
                    chunkNumber,
                    chunks.Count,
                    chunk.SourceFileName,
                    chunk.Text.Length,
                    floats.Length);
            }
        }

        return store;
    }

    public async Task<RagRetrievalResult> RetrieveContextAsync(
        InMemoryVectorStore store,
        string question,
        CancellationToken cancellationToken)
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
        var top = store.SearchTopKWithProseReserve(
            queryVector,
            _settings.TopK,
            _settings.MinProseChunks);

        for (var i = 0; i < top.Count; i++)
        {
            var (r, score) = top[i];
            _logger.LogInformation(
                "Retrieval rank={Rank}/{RetrievedCount}: similarity={Similarity:F4}, source={Source}",
                i + 1,
                top.Count,
                score,
                r.SourceFileName);
        }

        var contextBlocks = top
            .Select((x, idx) => $"[{idx + 1}] (source: {x.Record.SourceFileName})\n{x.Record.Text.Trim()}");
        var contextText = string.Join("\n---\n", contextBlocks);

        return new RagRetrievalResult(contextText, top);
    }

    public async Task<string> AnswerAsync(InMemoryVectorStore store, string question, CancellationToken cancellationToken)
    {
        var retrieval = await RetrieveContextAsync(store, question, cancellationToken).ConfigureAwait(false);

        var userMessage =
            "Use only the context below to answer. If the answer is not contained in the context, say you do not know and say what is missing.\n\n" +
            "For every non-obvious factual claim (field meanings, units, URIs, dates, names, numbers), add a bracket citation like [1] or [2] pointing to the context block that supports it.\n\n" +
            "Context:\n" +
            retrieval.ContextText +
            "\n\nQuestion:\n" +
            question.Trim();

        var chatRequest = new ChatRequest { Temperature = 0.2f };
        chatRequest.AddSystemMessage(
            "You are a precise assistant. Use only the provided context. Ground every factual claim in that context with [n] citations where n is the context block index. Do not invent policies, numbers, units, or contacts. If the context is insufficient, say so clearly.");
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
