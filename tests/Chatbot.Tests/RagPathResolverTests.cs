using Chatbot;
using NUnit.Framework;
using Rag;

namespace Chatbot.Tests;

[TestFixture]
public sealed class RagPathResolverTests
{
    [Test]
    public void ApplyAbsolutePaths_SetsDocumentsFolderPathAndValidatesDataset()
    {
        var repoRoot = SpaceMissionsPathResolver.FindRepoRoot(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "Chatbot"));

        var options = new RagSettings
        {
            DocumentsFolderPath = "",
            DatasetPath = "dataset/space_missions.csv"
        };

        RagPathResolver.ApplyAbsolutePaths(options, Path.Combine(repoRoot, "src", "Chatbot"));

        Assert.That(Path.IsPathRooted(options.DocumentsFolderPath), Is.True);
        Assert.That(options.DocumentsFolderPath, Is.EqualTo(repoRoot));

        var datasetPath = options.ResolveDatasetPath(AppContext.BaseDirectory);
        Assert.That(File.Exists(datasetPath), Is.True);
    }
}
