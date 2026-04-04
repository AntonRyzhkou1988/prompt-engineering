namespace Rag;

internal sealed class InMemoryVectorStore
{
    private readonly List<VectorRecord> _records = new();

    public void Add(VectorRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
    }

    public int Count => _records.Count;

    public IReadOnlyList<VectorRecord> SearchTopK(float[] queryEmbedding, int k)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        if (k <= 0)
            return Array.Empty<VectorRecord>();

        return _records
            .Select(r => (Record: r, Score: VectorMath.CosineSimilarity(queryEmbedding, r.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(k)
            .Select(x => x.Record)
            .ToList();
    }
}
