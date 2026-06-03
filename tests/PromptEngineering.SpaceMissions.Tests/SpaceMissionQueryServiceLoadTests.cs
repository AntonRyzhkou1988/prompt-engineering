using NUnit.Framework;

namespace PromptEngineering.SpaceMissions.Tests;

[TestFixture]
public sealed class SpaceMissionQueryServiceLoadTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "space_missions_sample.csv");

    [Test]
    public void LoadMissions_ReadsQuotedLocationFields()
    {
        var missions = SpaceMissionQueryService.LoadMissions(FixturePath);

        Assert.That(missions, Has.Count.EqualTo(20));
        Assert.That(missions[0].Location, Does.Contain("Baikonur"));
        Assert.That(missions[1].Location, Does.Contain("Florida, USA"));
    }

    [Test]
    public void LoadMissions_AllowsEmptyPriceAndTime()
    {
        var missions = SpaceMissionQueryService.LoadMissions(FixturePath);

        Assert.That(missions.Any(m => string.IsNullOrEmpty(m.Price)), Is.True);
        Assert.That(missions.Any(m => string.IsNullOrEmpty(m.Time)), Is.False);
    }

    [Test]
    public void LoadMissions_MissingFile_ThrowsFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => SpaceMissionQueryService.LoadMissions("missing.csv"));
    }
}
