namespace Rag;

internal sealed class CsvSettings
{
    public string Delimiter { get; set; } = ",";

    public string Quote { get; set; } = "\"";

    public bool HasHeader { get; set; } = true;

    /// <summary>Max data rows per indexed CSV chunk (excluding overlap from the previous chunk). Higher values mean fewer chunks and fewer embeddings.</summary>
    public int BatchSize { get; set; } = 32;

    private const int CharsPerRowBudget = 512;

    internal int EffectiveMaxChunkChars(int ragChunkSizeChars) =>
        Math.Max(ragChunkSizeChars, BatchSize * CharsPerRowBudget);

    internal void Validate()
    {
        if (BatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(BatchSize), "Csv: BatchSize must be positive.");

        if (string.IsNullOrEmpty(Delimiter) || Delimiter.Length != 1)
            throw new ArgumentException("Csv: Delimiter must be exactly one character.", nameof(Delimiter));
        if (string.IsNullOrEmpty(Quote) || Quote.Length != 1)
            throw new ArgumentException("Csv: Quote must be exactly one character.", nameof(Quote));

        var d = Delimiter[0];
        var q = Quote[0];
        if (d == q)
            throw new ArgumentException("Csv: Delimiter and Quote must differ.");
        if (d is '\n' or '\r' || q is '\n' or '\r')
            throw new ArgumentException("Csv: Delimiter and Quote cannot be newline characters.");
    }
}
