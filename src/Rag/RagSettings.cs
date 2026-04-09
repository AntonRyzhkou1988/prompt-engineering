namespace Rag;

/// <summary>
/// <see cref="DocumentsFolderPath"/> is the shared parent for questions and answers (and the default anchor for relative <see cref="DatasetPath"/>). It may be an <b>absolute</b> path or relative to <see cref="AppContext.BaseDirectory"/>.
/// <see cref="DatasetPath"/> is the corpus entry: a <b>single file</b> (<c>.md</c>, <c>.txt</c>, <c>.csv</c>) or a <b>directory</b> whose matching files are indexed recursively.
/// <see cref="QuestionsPath"/> and <see cref="AnswersPath"/> are folder names (or relative segments) under <see cref="DocumentsFolderPath"/>.
/// </summary>
internal sealed class RagSettings
{
    /// <summary>Shared parent directory for questions, answers, and relative <see cref="DatasetPath"/> (committed default: absolute repository root on this machine).</summary>
    public string DocumentsFolderPath { get; set; } = @"C:\Work\learn\ai-architect-practice\prompt-engineering";

    /// <summary>
    /// Indexed corpus: path to one file or one directory. Relative paths are under <see cref="DocumentsFolderPath"/>; rooted paths are used as-is.
    /// Use a file path (for example <c>dataset/space_missions.csv</c>) to index <b>only</b> that dataset; use a folder (for example <c>dataset</c>) to index every <c>.md</c>/<c>.txt</c>/<c>.csv</c> under it.
    /// </summary>
    public string DatasetPath { get; set; } = "dataset/space_missions.csv";

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

    /// <summary>Resolves <see cref="DatasetPath"/> to an absolute file or directory path.</summary>
    internal string ResolveDatasetPath(string baseDirectory) =>
        Path.IsPathRooted(DatasetPath)
            ? Path.GetFullPath(DatasetPath)
            : Path.GetFullPath(Path.Combine(ResolveContentRoot(baseDirectory), DatasetPath));

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
        if (string.IsNullOrWhiteSpace(DatasetPath))
            throw new ArgumentException("Dataset path is required.", nameof(DatasetPath));
        if (string.IsNullOrWhiteSpace(QuestionsPath))
            throw new ArgumentException("Questions path is required.", nameof(QuestionsPath));
        if (string.IsNullOrWhiteSpace(AnswersPath))
            throw new ArgumentException("Answers path is required.", nameof(AnswersPath));

        Csv ??= new CsvSettings();
        Csv.Validate();
    }

    /// <summary>Throws if <see cref="ResolveDatasetPath"/> does not exist as a file or directory.</summary>
    internal void EnsureDatasetExists(string baseDirectory)
    {
        var path = ResolveDatasetPath(baseDirectory);
        if (File.Exists(path) || Directory.Exists(path))
            return;

        throw new FileNotFoundException(
            $"Rag dataset not found (Rag:DatasetPath). Expected an existing file or directory: {path}");
    }
}
