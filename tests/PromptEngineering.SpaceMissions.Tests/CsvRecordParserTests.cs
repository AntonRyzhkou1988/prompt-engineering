using NUnit.Framework;

namespace PromptEngineering.SpaceMissions.Tests;

[TestFixture]
public sealed class CsvRecordParserTests
{
    [Test]
    public void ParseRecord_QuotedFieldWithComma_TreatedAsSingleField()
    {
        var result = CsvRecordParser.ParseRecord("2020,\"Florida, USA\",Laceration");

        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result[1], Is.EqualTo("Florida, USA"));
    }

    [Test]
    public void ParseRecord_EscapedDoubleQuoteInsideQuotedField_ReturnsLiteralQuote()
    {
        var result = CsvRecordParser.ParseRecord("2020,\"He said \"\"hello\"\"\",Surfing");

        Assert.That(result[1], Is.EqualTo("He said \"hello\""));
    }

    [Test]
    public void ParseRecord_EmptyFields_ReturnsEmptyStrings()
    {
        var result = CsvRecordParser.ParseRecord("2020,,");

        Assert.That(result, Has.Length.EqualTo(3));
        Assert.That(result[1], Is.Empty);
        Assert.That(result[2], Is.Empty);
    }

    [Test]
    public void UpdateQuoteState_NoQuotes_ReturnsFalse()
    {
        Assert.That(CsvRecordParser.UpdateQuoteState("plain text without quotes", false), Is.False);
    }
}
