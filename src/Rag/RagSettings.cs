namespace Rag;

internal sealed class RagSettings
{
    public string DocumentsPath { get; set; } = "documents";

    public string QuestionsPath { get; set; } = "questions";

    public string AnswersPath { get; set; } = "answers";

    public int ChunkSizeChars { get; set; } = 600;

    public int ChunkOverlapChars { get; set; } = 100;

    public int TopK { get; set; } = 4;

    /// <summary>Minimum number of retrieved chunks that must come from .md / .txt files (by best cosine score among prose), capped by available prose and TopK.</summary>
    public int MinProseChunks { get; set; } = 1;

    public int EmbeddingBatchSize { get; set; } = 16;

    public string InstanceName { get; set; } = "AI Architect";

    public CsvSettings Csv { get; set; } = new();

    internal void Validate()
    {
        if (ChunkSizeChars <= 0)
            throw new ArgumentOutOfRangeException(nameof(ChunkSizeChars), "Chunk size must be positive.");
        if (ChunkOverlapChars < 0 || ChunkOverlapChars >= ChunkSizeChars)
            throw new ArgumentOutOfRangeException(nameof(ChunkOverlapChars), "Overlap must be non-negative and less than chunk size.");
        if (TopK <= 0)
            throw new ArgumentOutOfRangeException(nameof(TopK), "TopK must be positive.");
        if (MinProseChunks < 0 || MinProseChunks > TopK)
            throw new ArgumentOutOfRangeException(
                nameof(MinProseChunks),
                "MinProseChunks must be between 0 and TopK (inclusive).");
        if (EmbeddingBatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(EmbeddingBatchSize), "Embedding batch size must be positive.");
        if (string.IsNullOrWhiteSpace(InstanceName))
            throw new ArgumentException("Instance name is required.", nameof(InstanceName));
        if (string.IsNullOrWhiteSpace(DocumentsPath))
            throw new ArgumentException("Documents path is required.", nameof(DocumentsPath));
        if (string.IsNullOrWhiteSpace(QuestionsPath))
            throw new ArgumentException("Questions path is required.", nameof(QuestionsPath));
        if (string.IsNullOrWhiteSpace(AnswersPath))
            throw new ArgumentException("Answers path is required.", nameof(AnswersPath));

        Csv ??= new CsvSettings();
        Csv.Validate();
    }
}
