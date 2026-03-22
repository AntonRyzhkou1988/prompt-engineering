using NUnit.Framework;

namespace PromptEngineering.Services.Tests;

[TestFixture]
public sealed class ContextServiceInjectRegionTests
{
    private const string StartTag = "<data>";
    private const string EndTag = "</data>";

    [Test]
    public void InjectRegion_RequiredTagPresent_InjectsContent()
    {
        const string prompt = "Analyze: <data></data> Done.";

        var result = ContextService.InjectRegion(prompt, StartTag, EndTag, "record1", required: true);

        Assert.That(result, Does.Contain("<data>"));
        Assert.That(result, Does.Contain("record1"));
        Assert.That(result, Does.Contain("</data>"));
        Assert.That(result, Does.Contain("Done."));
    }

    [Test]
    public void InjectRegion_RequiredTagMissing_Throws()
    {
        const string prompt = "No data section here.";

        Assert.Throws<InvalidOperationException>(() =>
            ContextService.InjectRegion(prompt, StartTag, EndTag, "record1", required: true));
    }

    [Test]
    public void InjectRegion_OptionalTagMissing_ReturnsOriginalPrompt()
    {
        const string prompt = "No prior_run section.";

        var result = ContextService.InjectRegion(prompt, "<prior_run>", "</prior_run>", "v1 result", required: false);

        Assert.That(result, Is.EqualTo(prompt));
    }

    [Test]
    public void InjectRegion_OptionalTagPresent_InjectsContent()
    {
        const string prompt = "See prior: <prior_run></prior_run> End.";

        var result = ContextService.InjectRegion(prompt, "<prior_run>", "</prior_run>", "prior result", required: false);

        Assert.That(result, Does.Contain("prior result"));
        Assert.That(result, Does.Contain("End."));
    }

    [Test]
    public void InjectRegion_TagsInWrongOrder_RequiredTrue_Throws()
    {
        const string prompt = "End: </data> then start: <data>";

        Assert.Throws<InvalidOperationException>(() =>
            ContextService.InjectRegion(prompt, StartTag, EndTag, "content", required: true));
    }

    [Test]
    public void InjectRegion_TagsInWrongOrder_RequiredFalse_ReturnsOriginalPrompt()
    {
        const string prompt = "End: </data> then start: <data>";

        var result = ContextService.InjectRegion(prompt, StartTag, EndTag, "content", required: false);

        Assert.That(result, Is.EqualTo(prompt));
    }

    [Test]
    public void InjectRegion_InjectedContentReplacesExistingContent()
    {
        const string prompt = "Data: <data>old content</data> End.";

        var result = ContextService.InjectRegion(prompt, StartTag, EndTag, "new content", required: true);

        Assert.That(result, Does.Not.Contain("old content"));
        Assert.That(result, Does.Contain("new content"));
    }

    [Test]
    public void InjectRegion_TagMatchIsCaseInsensitive()
    {
        const string prompt = "Data: <DATA></DATA> End.";

        Assert.DoesNotThrow(() =>
            ContextService.InjectRegion(prompt, StartTag, EndTag, "content", required: true));
    }

    [Test]
    public void InjectRegion_EmptyContent_InjectsNothing()
    {
        const string prompt = "Data: <data></data> End.";

        var result = ContextService.InjectRegion(prompt, StartTag, EndTag, string.Empty, required: true);

        Assert.That(result, Does.Contain("<data>"));
        Assert.That(result, Does.Contain("</data>"));
    }
}
