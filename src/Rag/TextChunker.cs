namespace Rag;

internal static class TextChunker
{
    public static IReadOnlyList<DocumentChunk> ChunkText(string sourceFileName, string text, int chunkSize, int overlap)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFileName);
        if (chunkSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        if (overlap < 0 || overlap >= chunkSize)
            throw new ArgumentOutOfRangeException(nameof(overlap));

        text = text.Trim();
        if (text.Length == 0)
            return Array.Empty<DocumentChunk>();

        var step = chunkSize - overlap;
        var chunks = new List<DocumentChunk>();
        for (var start = 0; start < text.Length; start += step)
        {
            var len = Math.Min(chunkSize, text.Length - start);
            var piece = text.AsSpan(start, len).Trim().ToString();
            if (piece.Length > 0)
                chunks.Add(new DocumentChunk(sourceFileName, piece));

            if (start + len >= text.Length)
                break;
        }

        return chunks;
    }
}
