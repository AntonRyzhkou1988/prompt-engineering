namespace Chatbot.Services;

public static class RagContextFormatter
{
    public static string FormatContextBlocks(IReadOnlyList<(Rag.VectorRecord Record, float Similarity)> rankedChunks)
    {
        if (rankedChunks.Count == 0)
            return string.Empty;

        return string.Join(
            "\n---\n",
            rankedChunks.Select((x, idx) =>
                $"[{idx + 1}] (source: {x.Record.SourceFileName}, similarity: {x.Similarity:F3})\n{x.Record.Text.Trim()}"));
    }
}
