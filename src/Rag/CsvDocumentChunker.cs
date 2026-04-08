using System.Globalization;
using System.Text;

namespace Rag;

internal static class CsvDocumentChunker
{
    public static async Task<IReadOnlyList<DocumentChunk>> ChunkFileAsync(
        string path,
        string sourceFileName,
        CsvSettings csv,
        int chunkSizeChars,
        int chunkOverlapChars,
        CancellationToken cancellationToken)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        var maxChunkChars = csv.EffectiveMaxChunkChars(chunkSizeChars);
        return ChunkContent(text, sourceFileName, csv, maxChunkChars, chunkOverlapChars);
    }

    internal static IReadOnlyList<DocumentChunk> ChunkContent(
        string text,
        string sourceFileName,
        CsvSettings csv,
        int maxChunkChars,
        int chunkOverlapChars)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceFileName);
        ArgumentNullException.ThrowIfNull(csv);
        if (maxChunkChars <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxChunkChars));
        if (chunkOverlapChars < 0 || chunkOverlapChars >= maxChunkChars)
            throw new ArgumentOutOfRangeException(nameof(chunkOverlapChars));

        var delim = csv.Delimiter[0];
        var quote = csv.Quote[0];

        var logicalLines = ToLogicalRecordLines(text, quote);
        if (logicalLines.Count == 0)
            return Array.Empty<DocumentChunk>();

        var rows = new List<IReadOnlyList<string>>();
        for (var li = 0; li < logicalLines.Count; li++)
        {
            try
            {
                rows.Add(SplitRecordFields(logicalLines[li], delim, quote));
            }
            catch (FormatException ex)
            {
                throw new FormatException($"CSV parse error in '{sourceFileName}' at logical row {li + 1}: {ex.Message}", ex);
            }
        }

        if (rows.Count == 0)
            return Array.Empty<DocumentChunk>();

        IReadOnlyList<string> header;
        var dataStart = 0;

        if (csv.HasHeader)
        {
            header = rows[0];
            if (header.Count == 0)
                return Array.Empty<DocumentChunk>();
            dataStart = 1;
        }
        else
        {
            var n = rows[0].Count;
            header = Enumerable.Range(0, n).Select(i => "Field" + i.ToString(CultureInfo.InvariantCulture)).ToList();
        }

        var expected = header.Count;
        var formattedRows = new List<string>();
        var dataRowNumber = 0;

        for (var r = dataStart; r < rows.Count; r++)
        {
            var fields = rows[r];
            if (fields.Count != expected)
                throw new FormatException(
                    $"CSV row count mismatch in '{sourceFileName}' at logical row {r + 1}: expected {expected} fields, got {fields.Count}.");

            dataRowNumber++;
            formattedRows.Add(FormatDataRow(sourceFileName, dataRowNumber, header, fields));
        }

        if (formattedRows.Count == 0)
            return Array.Empty<DocumentChunk>();

        return ChunkFormattedRows(sourceFileName, formattedRows, maxChunkChars, chunkOverlapChars, csv.BatchSize);
    }

    private static string FormatDataRow(
        string sourceFileName,
        int dataRowNumber,
        IReadOnlyList<string> header,
        IReadOnlyList<string> fields)
    {
        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"source: {sourceFileName} row: {dataRowNumber}");
        for (var i = 0; i < header.Count; i++)
        {
            sb.AppendLine();
            sb.Append(header[i]);
            sb.Append(": ");
            sb.Append(fields[i]);
        }

        return sb.ToString();
    }

    private static IReadOnlyList<DocumentChunk> ChunkFormattedRows(
        string sourceFileName,
        List<string> formattedRows,
        int maxChunkChars,
        int chunkOverlapChars,
        int batchSize)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize));

        var chunks = new List<DocumentChunk>();
        var overlapRows = new List<string>();
        var i = 0;

        while (i < formattedRows.Count)
        {
            var buffer = new List<string>(overlapRows);
            var skipEmitBecauseOversized = false;
            var newRowsAdded = 0;

            while (i < formattedRows.Count)
            {
                if (newRowsAdded >= batchSize)
                    break;

                var row = formattedRows[i];
                var candidateLen = buffer.Count == 0
                    ? row.Length
                    : JoinRows(buffer).Length + 2 + row.Length;

                if (candidateLen <= maxChunkChars)
                {
                    buffer.Add(row);
                    i++;
                    newRowsAdded++;
                    if (newRowsAdded >= batchSize)
                        break;
                    continue;
                }

                if (buffer.Count > overlapRows.Count)
                    break;

                if (row.Length > maxChunkChars)
                {
                    chunks.Add(new DocumentChunk(sourceFileName, row.Trim()));
                    i++;
                    overlapRows = OverlapSuffix(new List<string> { row }, chunkOverlapChars);
                    skipEmitBecauseOversized = true;
                    break;
                }

                if (overlapRows.Count > 0)
                {
                    overlapRows = [];
                    buffer.Clear();
                    newRowsAdded = 0;
                    continue;
                }

                buffer.Add(row);
                i++;
                newRowsAdded++;
                break;
            }

            if (skipEmitBecauseOversized)
                continue;

            if (buffer.Count == 0)
                continue;

            var text = JoinRows(buffer).Trim();
            if (text.Length > 0)
                chunks.Add(new DocumentChunk(sourceFileName, text));

            overlapRows = OverlapSuffix(buffer, chunkOverlapChars);
        }

        return chunks;
    }

    private static string JoinRows(List<string> rows) => string.Join("\n\n", rows);

    private static List<string> OverlapSuffix(List<string> rows, int maxOverlapChars)
    {
        var acc = new List<string>();
        var len = 0;
        for (var idx = rows.Count - 1; idx >= 0; idx--)
        {
            var r = rows[idx];
            var add = len == 0 ? r.Length : r.Length + 2;
            if (len + add > maxOverlapChars)
                break;
            acc.Insert(0, r);
            len += add;
        }

        return acc;
    }

    private static List<string> ToLogicalRecordLines(string text, char quote)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
        var physical = normalized.Split('\n');
        var logical = new List<string>();
        var buffer = new StringBuilder();

        foreach (var line in physical)
        {
            if (buffer.Length > 0)
                buffer.Append('\n');
            buffer.Append(line);

            if (!EndsWithOpenQuotedField(buffer.ToString().AsSpan(), quote))
            {
                var record = buffer.ToString();
                buffer.Clear();
                if (string.IsNullOrWhiteSpace(record))
                    continue;
                logical.Add(record);
            }
        }

        if (buffer.Length > 0)
            throw new FormatException("CSV ends with an unclosed quoted field.");

        return logical;
    }

    private static bool EndsWithOpenQuotedField(ReadOnlySpan<char> s, char quote)
    {
        var inQuotes = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (!inQuotes)
            {
                if (c == quote)
                    inQuotes = true;
            }
            else if (c == quote)
            {
                if (i + 1 < s.Length && s[i + 1] == quote)
                {
                    i++;
                    continue;
                }

                inQuotes = false;
            }
        }

        return inQuotes;
    }

    private static IReadOnlyList<string> SplitRecordFields(string record, char delimiter, char quote)
    {
        var fields = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < record.Length; i++)
        {
            var c = record[i];
            if (!inQuotes)
            {
                if (c == delimiter)
                {
                    fields.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (c == quote)
                {
                    inQuotes = true;
                    continue;
                }

                field.Append(c);
            }
            else if (c == quote)
            {
                if (i + 1 < record.Length && record[i + 1] == quote)
                {
                    field.Append(quote);
                    i++;
                    continue;
                }

                inQuotes = false;
            }
            else
            {
                field.Append(c);
            }
        }

        if (inQuotes)
            throw new FormatException("Unclosed quote in CSV record.");

        fields.Add(field.ToString());
        return fields;
    }
}
