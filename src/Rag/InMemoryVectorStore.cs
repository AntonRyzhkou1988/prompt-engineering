namespace Rag;

public sealed class InMemoryVectorStore
{
    private readonly List<VectorRecord> _records = new();

    public void Add(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
    }

    public int Count => _records.Count;

    /// <summary>
    /// Rank by cosine similarity only (no prose reservation).
    /// </summary>
    public IReadOnlyList<VectorRecord> SearchTopK(float[] queryEmbedding, int k) =>
        SearchTopKWithProseReserve(queryEmbedding, k, minProseChunks: 0)
            .Select(x => x.Record)
            .ToList();

    /// <summary>
    /// Cosine top-<paramref name="k"/> with up to <paramref name="minProseChunks"/> reserved for best-matching
    /// <c>.md</c>/<c>.txt</c> chunks; remaining slots filled from the global ranking without duplicates.
    /// </summary>
    public IReadOnlyList<(VectorRecord Record, float Similarity)> SearchTopKWithProseReserve(
        float[] queryEmbedding,
        int k,
        int minProseChunks)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        if (k <= 0)
            return Array.Empty<(VectorRecord, float)>();
        if (minProseChunks < 0)
            throw new ArgumentOutOfRangeException(nameof(minProseChunks));

        var ranked = _records
            .Select(r => (Record: r, Score: VectorMath.CosineSimilarity(queryEmbedding, r.Embedding)))
            .OrderByDescending(x => x.Score)
            .ToList();

        if (minProseChunks == 0)
            return ranked.Take(k).Select(x => (x.Record, x.Score)).ToList();

        var cap = Math.Min(minProseChunks, k);
        var picked = new HashSet<VectorRecord>();
        var results = new List<(VectorRecord Record, float Similarity)>();

        var proseOrdered = ranked.Where(x => IsProseSource(x.Record.SourceFileName)).ToList();
        foreach (var x in proseOrdered)
        {
            if (results.Count >= cap)
                break;
            if (picked.Add(x.Record))
                results.Add((x.Record, x.Score));
        }

        foreach (var x in ranked)
        {
            if (results.Count >= k)
                break;
            if (picked.Add(x.Record))
                results.Add((x.Record, x.Score));
        }

        return results;
    }

    private static bool IsProseSource(string sourceFileName)
    {
        var ext = Path.GetExtension(sourceFileName);
        return ext.Equals(".md", StringComparison.OrdinalIgnoreCase)
               || ext.Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }
}
