using NUnit.Framework;

namespace PromptEngineering.Services.Tests;

[TestFixture]
public sealed class ContextServiceParseCsvTests
{
    // ── ParseCsvRecord ────────────────────────────────────────────────────────

    [Test]
    public void ParseCsvRecord_SimpleLine_ReturnsSplitFields()
    {
        var result = ContextService.ParseCsvRecord("Year,Country,Area");

        Assert.That(result, Is.EqualTo(new[] { "Year", "Country", "Area" }));
    }

    [Test]
    public void ParseCsvRecord_QuotedField_ReturnsUnquotedValue()
    {
        var result = ContextService.ParseCsvRecord("2020,\"United States\",Surfing");

        Assert.That(result[1], Is.EqualTo("United States"));
    }

    [Test]
    public void ParseCsvRecord_QuotedFieldWithComma_TreatedAsSingleField()
    {
        var result = ContextService.ParseCsvRecord("2020,\"Florida, USA\",Laceration");

        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result[1], Is.EqualTo("Florida, USA"));
    }

    [Test]
    public void ParseCsvRecord_EscapedDoubleQuoteInsideQuotedField_ReturnsLiteralQuote()
    {
        var result = ContextService.ParseCsvRecord("2020,\"He said \"\"hello\"\"\",Surfing");

        Assert.That(result[1], Is.EqualTo("He said \"hello\""));
    }

    [Test]
    public void ParseCsvRecord_EmptyFields_ReturnsEmptyStrings()
    {
        var result = ContextService.ParseCsvRecord("2020,,");

        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result[1], Is.Empty);
        Assert.That(result[2], Is.Empty);
    }

    [Test]
    public void ParseCsvRecord_SingleField_ReturnsSingleElement()
    {
        var result = ContextService.ParseCsvRecord("OnlyOneField");

        Assert.That(result, Has.Length.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("OnlyOneField"));
    }

    [Test]
    public void ParseCsvRecord_AllQuotedFields_UnquotesAll()
    {
        var result = ContextService.ParseCsvRecord("\"2020\",\"USA\",\"Surfing\"");

        Assert.That(result, Is.EqualTo(new[] { "2020", "USA", "Surfing" }));
    }

    // ── UpdateQuoteState ──────────────────────────────────────────────────────

    [Test]
    public void UpdateQuoteState_NoQuotes_ReturnsFalse()
    {
        var result = ContextService.UpdateQuoteState("plain text without quotes", false);

        Assert.That(result, Is.False);
    }

    [Test]
    public void UpdateQuoteState_SingleOpenQuote_ReturnsTrue()
    {
        var result = ContextService.UpdateQuoteState("\"open", false);

        Assert.That(result, Is.True);
    }

    [Test]
    public void UpdateQuoteState_OpenThenCloseQuote_ReturnsFalse()
    {
        var result = ContextService.UpdateQuoteState("\"field\"", false);

        Assert.That(result, Is.False);
    }

    [Test]
    public void UpdateQuoteState_EscapedDoubleQuote_DoesNotToggleState()
    {
        var result = ContextService.UpdateQuoteState("\"he said \"\"hi\"\"\"", false);

        Assert.That(result, Is.False);
    }

    [Test]
    public void UpdateQuoteState_AlreadyInsideQuotes_ClosingQuoteReturnsFalse()
    {
        var result = ContextService.UpdateQuoteState("continuation\"", insideQuotes: true);

        Assert.That(result, Is.False);
    }

    [Test]
    public void UpdateQuoteState_AlreadyInsideQuotes_NoQuoteInLine_StaysTrue()
    {
        var result = ContextService.UpdateQuoteState("no quotes here", insideQuotes: true);

        Assert.That(result, Is.True);
    }
}
