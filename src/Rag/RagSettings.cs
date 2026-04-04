namespace Rag;

internal sealed class RagSettings
{
    public string DocumentsPath { get; set; } = "documents";

    public int ChunkSizeChars { get; set; } = 600;

    public int ChunkOverlapChars { get; set; } = 100;

    public int TopK { get; set; } = 4;

    public int EmbeddingBatchSize { get; set; } = 16;

    public string InstanceName { get; set; } = "AI Architect";

    internal void Validate()
    {
        if (ChunkSizeChars <= 0)
            throw new ArgumentOutOfRangeException(nameof(ChunkSizeChars), "Chunk size must be positive.");
        if (ChunkOverlapChars < 0 || ChunkOverlapChars >= ChunkSizeChars)
            throw new ArgumentOutOfRangeException(nameof(ChunkOverlapChars), "Overlap must be non-negative and less than chunk size.");
        if (TopK <= 0)
            throw new ArgumentOutOfRangeException(nameof(TopK), "TopK must be positive.");
        if (EmbeddingBatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(EmbeddingBatchSize), "Embedding batch size must be positive.");
        if (string.IsNullOrWhiteSpace(InstanceName))
            throw new ArgumentException("Instance name is required.", nameof(InstanceName));
        if (string.IsNullOrWhiteSpace(DocumentsPath))
            throw new ArgumentException("Documents path is required.", nameof(DocumentsPath));
    }
}
