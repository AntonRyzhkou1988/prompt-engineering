using System.Text;

namespace PromptEngineering.SpaceMissions;

internal static class CsvRecordParser
{
    internal static bool UpdateQuoteState(string line, bool insideQuotes)
    {
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] != '"') continue;
            if (insideQuotes && i + 1 < line.Length && line[i + 1] == '"') { i++; continue; }
            insideQuotes = !insideQuotes;
        }

        return insideQuotes;
    }

    internal static string[] ParseRecord(string record)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var insideQuotes = false;

        for (var i = 0; i < record.Length; i++)
        {
            var ch = record[i];
            if (ch == '"')
            {
                if (insideQuotes && i + 1 < record.Length && record[i + 1] == '"') { current.Append('"'); i++; }
                else insideQuotes = !insideQuotes;
                continue;
            }

            if (ch == ',' && !insideQuotes) { fields.Add(current.ToString()); current.Clear(); continue; }
            current.Append(ch);
        }

        fields.Add(current.ToString());
        return [.. fields];
    }

    internal static async Task<string?> ReadRawRecordAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        if (reader.EndOfStream) return null;

        var buffer = new StringBuilder();
        var insideQuotes = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;

            if (buffer.Length > 0) buffer.Append('\n');
            buffer.Append(line);
            insideQuotes = UpdateQuoteState(line, insideQuotes);
            if (!insideQuotes) break;
        }

        if (insideQuotes)
            throw new InvalidOperationException("CSV parsing failed due to unclosed quote sequence.");

        return buffer.ToString();
    }
}
