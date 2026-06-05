using NUnit.Framework;

namespace PromptEngineering.SpaceMissions.Tests;

[TestFixture]
public sealed class SpaceMissionQueryServiceFilterTests
{
    private ISpaceMissionQueryService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "space_missions_sample.csv");
        _service = new SpaceMissionQueryService(path);
    }

    [Test]
    public void Filter_ByMissionStatus_ReturnsMatchingRows()
    {
        var rows = _service.Filter(new SpaceMissionFilter { MissionStatus = "Success" }, 200);

        Assert.That(rows, Is.Not.Empty);
        Assert.That(rows.All(r => r.MissionStatus.Equals("Success", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Filter_ByCompany_ReturnsMatchingRows()
    {
        var rows = _service.Filter(new SpaceMissionFilter { Company = "SpaceX" }, 200);

        Assert.That(rows, Has.Count.EqualTo(5));
    }

    [Test]
    public void Filter_ByLocationContains_ReturnsMatchingRows()
    {
        var rows = _service.Filter(new SpaceMissionFilter { LocationContains = "Kennedy Space Center" }, 200);

        Assert.That(rows, Has.Count.EqualTo(5));
    }

    [Test]
    public void Filter_ByDateRange_ReturnsMatchingRows()
    {
        var rows = _service.Filter(new SpaceMissionFilter
        {
            DateFrom = new DateOnly(2020, 1, 1),
            DateTo = new DateOnly(2021, 12, 31)
        }, 200);

        Assert.That(rows, Has.Count.EqualTo(7));
    }

    [Test]
    public void Filter_LimitCapsAtMax200()
    {
        var rows = _service.Filter(null, 500);

        Assert.That(rows.Count, Is.LessThanOrEqualTo(SpaceMissionQueryService.MaxLimit));
    }

    [Test]
    public void Count_WithFilter_ReturnsTotalMatches()
    {
        var count = _service.Count(new SpaceMissionFilter { MissionStatus = "Failure" });

        Assert.That(count, Is.EqualTo(4));
    }
}
