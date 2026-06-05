namespace Rag;

public sealed record RagRetrievalResult(
    string ContextText,
    IReadOnlyList<(VectorRecord Record, float Similarity)> RankedChunks);
