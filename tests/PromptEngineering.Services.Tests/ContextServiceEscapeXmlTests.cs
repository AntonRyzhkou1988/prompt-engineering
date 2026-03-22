using NUnit.Framework;

namespace PromptEngineering.Services.Tests;

[TestFixture]
public sealed class ContextServiceEscapeXmlTests
{
    [Test]
    public void EscapeXml_NullValue_ReturnsEmpty()
    {
        var result = ContextService.EscapeXml(null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void EscapeXml_EmptyString_ReturnsEmpty()
    {
        var result = ContextService.EscapeXml(string.Empty);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void EscapeXml_PlainText_ReturnsUnchanged()
    {
        const string input = "Surfing in Florida";

        var result = ContextService.EscapeXml(input);

        Assert.That(result, Is.EqualTo(input));
    }

    [Test]
    public void EscapeXml_LessThan_Escapes()
    {
        var result = ContextService.EscapeXml("age < 18");

        Assert.That(result, Is.EqualTo("age &lt; 18"));
    }

    [Test]
    public void EscapeXml_GreaterThan_Escapes()
    {
        var result = ContextService.EscapeXml("depth > 5m");

        Assert.That(result, Is.EqualTo("depth &gt; 5m"));
    }

    [Test]
    public void EscapeXml_Ampersand_Escapes()
    {
        var result = ContextService.EscapeXml("fish & chips");

        Assert.That(result, Is.EqualTo("fish &amp; chips"));
    }

    [Test]
    public void EscapeXml_DoubleQuote_Escapes()
    {
        var result = ContextService.EscapeXml("he said \"hello\"");

        Assert.That(result, Is.EqualTo("he said &quot;hello&quot;"));
    }

    [Test]
    public void EscapeXml_SingleQuote_Escapes()
    {
        var result = ContextService.EscapeXml("it's fine");

        Assert.That(result, Is.EqualTo("it&apos;s fine"));
    }

    [Test]
    public void EscapeXml_MultipleSpecialChars_EscapesAll()
    {
        var result = ContextService.EscapeXml("<b>Tom & 'Jerry'</b>");

        Assert.That(result, Is.EqualTo("&lt;b&gt;Tom &amp; &apos;Jerry&apos;&lt;/b&gt;"));
    }

    [Test]
    public void EscapeXml_OnlyNumbers_ReturnsUnchanged()
    {
        const string input = "2023";

        var result = ContextService.EscapeXml(input);

        Assert.That(result, Is.EqualTo(input));
    }
}
