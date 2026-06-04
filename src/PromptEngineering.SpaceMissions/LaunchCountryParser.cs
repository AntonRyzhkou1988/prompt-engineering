namespace PromptEngineering.SpaceMissions;

public static class LaunchCountryParser
{
    public const string UnparseableBucket = "Unparseable / missing";

    public const string DerivationRule =
        "Country is the last comma-separated segment of Location after trimming whitespace; empty Location yields 'Unparseable / missing'.";

    public static string DeriveCountry(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
            return UnparseableBucket;

        var trimmed = location.Trim();
        var lastComma = trimmed.LastIndexOf(',');
        if (lastComma < 0 || lastComma >= trimmed.Length - 1)
            return UnparseableBucket;

        var segment = trimmed[(lastComma + 1)..].Trim();
        return string.IsNullOrWhiteSpace(segment) ? UnparseableBucket : segment;
    }
}
