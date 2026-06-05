using System.Text;
using System.Text.RegularExpressions;

namespace Chatbot.Services;

public static partial class SpaceMissionToolRoutingHints
{
    private static readonly string[] CountPatterns =
    [
        "how many", "count ", "number of", "total launches", "total missions"
    ];

    private static readonly string[] DistinctPatterns =
    [
        "distinct", "unique", "contain", "contains", "which companies", "what companies",
        "which rockets", "what rocket", "rocket names", "list all", "what values",
        "which locations", "what locations", "mission names"
    ];

    private static readonly string[] SuccessRatePatterns =
    [
        "success rate", "mission success rate", "msr"
    ];

    private static readonly string[] CountryPatterns =
    [
        "by country", "launch country", "countries", "country share", "per country"
    ];

    private static readonly string[] AggregatePatterns =
    [
        "breakdown", "distribution", "grouped by", "group by", "aggregate", "share of"
    ];

    private static readonly string[] FilterPatterns =
    [
        "show me", "list missions", "find missions", "details of", "give me the first",
        "next page", "paginated"
    ];

    private static readonly string[] SummaryPatterns =
    [
        "overview", "high-level", "whole dataset", "entire dataset", "summary of the dataset"
    ];

    private static readonly string[] SchemaPatterns =
    [
        "what columns", "which columns", "column definitions", "valid values for",
        "what fields", "schema"
    ];

    public static string BuildHints(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return string.Empty;

        var normalized = question.Trim();
        var lower = normalized.ToLowerInvariant();
        var hints = new List<string>();

        if (ContainsAny(lower, SchemaPatterns))
            hints.Add("Call get_space_missions_schema for column names, row count, date range, and MissionStatus values.");

        if (ContainsAny(lower, SummaryPatterns))
            hints.Add("Call get_space_missions_summary for whole-dataset overview and outcome mix.");

        if (ContainsAny(lower, DistinctPatterns))
        {
            var search = ExtractQuotedSubstring(normalized);
            var column = InferDistinctColumn(lower);
            var searchHint = search is null ? "" : $" with search=\"{search}\"";
            hints.Add(
                $"Call list_space_mission_distinct_values on column \"{column}\"{searchHint} to enumerate matching values from the full dataset.");
        }

        if (ContainsAny(lower, CountPatterns))
            hints.Add("Call count_space_missions for exact totals; do not infer counts from retrieved chunks alone.");

        if (ContainsAny(lower, SuccessRatePatterns))
            hints.Add("Call compute_space_mission_success_rate instead of dividing counts manually.");

        if (ContainsAny(lower, CountryPatterns))
            hints.Add("Call aggregate_space_missions_by_launch_country for country-level shares (last comma segment of Location).");

        if (ContainsAny(lower, AggregatePatterns) && !ContainsAny(lower, CountryPatterns))
            hints.Add("Call aggregate_space_missions with an appropriate groupBy column for grouped statistics.");

        if (ContainsAny(lower, FilterPatterns))
            hints.Add("Call filter_space_missions for row-level evidence; use offset for pagination when results may exceed the row cap.");

        if (hints.Count == 0)
            return string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine("## Tool routing hints");
        builder.AppendLine("Use MCP tools for the following based on the user question:");
        foreach (var hint in hints.Distinct(StringComparer.Ordinal))
            builder.AppendLine($"- {hint}");

        return builder.ToString().TrimEnd();
    }

    private static string InferDistinctColumn(string lowerQuestion)
    {
        if (lowerQuestion.Contains("rocket", StringComparison.Ordinal))
            return "Rocket";
        if (lowerQuestion.Contains("compan", StringComparison.Ordinal))
            return "Company";
        if (lowerQuestion.Contains("location", StringComparison.Ordinal) || lowerQuestion.Contains("launch site", StringComparison.Ordinal))
            return "Location";
        if (lowerQuestion.Contains("mission status", StringComparison.Ordinal) || lowerQuestion.Contains("outcome", StringComparison.Ordinal))
            return "MissionStatus";
        if (lowerQuestion.Contains("mission", StringComparison.Ordinal))
            return "Mission";
        if (lowerQuestion.Contains("rocket status", StringComparison.Ordinal))
            return "RocketStatus";

        return "Rocket";
    }

    private static string? ExtractQuotedSubstring(string question)
    {
        var match = QuotedSubstringRegex().Match(question);
        if (!match.Success)
            return null;

        return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
    }

    private static bool ContainsAny(string text, IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (text.Contains(pattern, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    [GeneratedRegex("\"([^\"]+)\"|'([^']+)'")]
    private static partial Regex QuotedSubstringRegex();
}
