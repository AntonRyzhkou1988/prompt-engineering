using Chatbot.Tests.Gds;
using NUnit.Framework;

namespace Chatbot.Tests;

[TestFixture]
public sealed class GdsGroundTruthTests
{
    [Test]
    public async Task BuildAll_WritesGroundTruthFilesForEachManifestItem()
    {
        try
        {
            await using var session = await GdsGroundTruthBuilder.ConnectMcpSessionAsync().ConfigureAwait(false);
            await GdsGroundTruthBuilder.BuildAllAsync(session, GdsPaths.GroundTruthDirectory).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Build SpaceMissions.McpServer", StringComparison.Ordinal))
        {
            Assert.Ignore(ex.Message);
        }

        var manifest = GdsManifest.Load(GdsPaths.ManifestPath);
        foreach (var item in manifest.Items)
        {
            var path = GdsPaths.ResolveGroundTruthPath(item.GroundTruthRef);
            Assert.That(File.Exists(path), Is.True, $"Missing ground truth file: {path}");

            var document = GdsGroundTruthDocument.Load(path);
            Assert.That(document.ItemId, Is.EqualTo(item.ItemId));
            Assert.That(document.McpCalls, Is.Not.Empty);
            Assert.That(document.KeyFacts, Is.Not.Empty);
        }
    }

    [Test]
    public async Task BuildAll_KeyFactsMatchManifestExpectations()
    {
        try
        {
            await using var session = await GdsGroundTruthBuilder.ConnectMcpSessionAsync().ConfigureAwait(false);
            await GdsGroundTruthBuilder.BuildAllAsync(session, GdsPaths.GroundTruthDirectory).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Build SpaceMissions.McpServer", StringComparison.Ordinal))
        {
            Assert.Ignore(ex.Message);
        }

        var gds001 = GdsGroundTruthDocument.Load(GdsPaths.ResolveGroundTruthPath("ground-truth/gds-001.json"));
        Assert.That(gds001.KeyFacts["datasetRowCount"].GetInt32(), Is.EqualTo(4630));

        var gds005 = GdsGroundTruthDocument.Load(GdsPaths.ResolveGroundTruthPath("ground-truth/gds-005.json"));
        Assert.That(gds005.KeyFacts["spacexLaunchCount"].GetInt32(), Is.GreaterThan(0));

        var gds010 = GdsGroundTruthDocument.Load(GdsPaths.ResolveGroundTruthPath("ground-truth/gds-010.json"));
        Assert.That(gds010.KeyFacts["actualSpacexCount"].GetInt32(), Is.LessThan(5000));
        Assert.That(gds010.KeyFacts["maxFilterLimit"].GetInt32(), Is.EqualTo(200));
    }
}
