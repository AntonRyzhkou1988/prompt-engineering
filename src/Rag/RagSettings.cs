namespace Rag;

/// <summary>
/// <see cref="DocumentsFolderPath"/> is the shared parent for corpus, questions, and answers. It may be an <b>absolute</b> path or relative to <see cref="AppContext.BaseDirectory"/>.
/// <see cref="DocumentsPath"/>, <see cref="QuestionsPath"/>, and <see cref="AnswersPath"/> are folder names (or relative segments) under that parent, normalized with <see cref="Path.GetFullPath(string)"/>.
/// Metric specs live in repository-root <c>metrics/</c>; that folder is not configured here—copy into the corpus folder or change paths if metrics files must be retrieved.
/// </summary>
internal sealed class RagSettings
{
    /// <summary>Shared parent directory for corpus, questions, and answers (committed default: absolute repository root on this machine).</summary>
    public string DocumentsFolderPath { get; set; } = @"C:\Work\learn\ai-architect-practice\prompt-engineering";

    /// <summary>Subfolder under <see cref="DocumentsFolderPath"/> for the indexed corpus (default <c>dataset</c>; includes <c>space_missions.csv</c> and <c>attacks.csv</c> when both are present).</summary>
    public string DocumentsPath { get; set; } = "dataset";

    /// <summary>Subfolder under <see cref="DocumentsFolderPath"/> for prefilled question <c>.md</c> files (default <c>questions</c>).</summary>
    public string QuestionsPath { get; set; } = "questions";

    /// <summary>Subfolder under <see cref="DocumentsFolderPath"/> for saved answers (default <c>answers</c>).</summary>
    public string AnswersPath { get; set; } = "answers";

    public int ChunkSizeChars { get; set; } = 600;

    public int ChunkOverlapChars { get; set; } = 100;

    public int TopK { get; set; } = 4;

    /// <summary>Minimum number of retrieved chunks that must come from .md / .txt files (by best cosine score among prose), capped by available prose and TopK.</summary>
    public int MinProseChunks { get; set; } = 1;

    public int EmbeddingBatchSize { get; set; } = 16;

    public string InstanceName { get; set; } = "AI Architect";

    public CsvSettings Csv { get; set; } = new();

    internal string ResolveDocumentsRoot(string baseDirectory) =>
        Path.GetFullPath(Path.Combine(ResolveContentRoot(baseDirectory), DocumentsPath));

    internal string ResolveQuestionsRoot(string baseDirectory) =>
        Path.GetFullPath(Path.Combine(ResolveContentRoot(baseDirectory), QuestionsPath));

    internal string ResolveAnswersRoot(string baseDirectory) =>
        Path.GetFullPath(Path.Combine(ResolveContentRoot(baseDirectory), AnswersPath));

    /// <summary>When <see cref="DocumentsFolderPath"/> is rooted, it is the content root; otherwise it is combined with <paramref name="baseDirectory"/>.</summary>
    private string ResolveContentRoot(string baseDirectory) =>
        Path.IsPathRooted(DocumentsFolderPath)
            ? DocumentsFolderPath
            : Path.Combine(baseDirectory, DocumentsFolderPath);

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
        if (string.IsNullOrWhiteSpace(DocumentsFolderPath))
            throw new ArgumentException("Documents folder path is required.", nameof(DocumentsFolderPath));
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
