namespace Rag;

internal static class VectorMath
{
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Embeddings must have the same dimension.");

        double dot = 0;
        double na = 0;
        double nb = 0;
        for (var i = 0; i < a.Length; i++)
        {
            var x = a[i];
            var y = b[i];
            dot += x * y;
            na += x * x;
            nb += y * y;
        }

        var denom = Math.Sqrt(na) * Math.Sqrt(nb);
        if (denom < 1e-12)
            return 0f;

        return (float)(dot / denom);
    }
}
