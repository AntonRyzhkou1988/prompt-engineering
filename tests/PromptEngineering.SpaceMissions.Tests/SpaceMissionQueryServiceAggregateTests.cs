using NUnit.Framework;

namespace PromptEngineering.SpaceMissions.Tests;

[TestFixture]
public sealed class SpaceMissionQueryServiceAggregateTests
{
    private ISpaceMissionQueryService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "space_missions_sample.csv");
        _service = new SpaceMissionQueryService(path);
    }

    [Test]
    public void Aggregate_ByMissionStatus_ReturnsExpectedBuckets()
    {
        var result = _service.Aggregate("MissionStatus", null, SpaceMissionQueryService.DefaultMaxBuckets);

        Assert.That(result.GroupByColumn, Is.EqualTo("MissionStatus"));
        Assert.That(result.TotalRows, Is.EqualTo(20));
        Assert.That(result.Buckets.Single(b => b.Bucket == "Success").Count, Is.EqualTo(13));
        Assert.That(result.Buckets.Single(b => b.Bucket == "Failure").Count, Is.EqualTo(4));
        Assert.That(result.Buckets.Single(b => b.Bucket == "Partial Failure").Count, Is.EqualTo(2));
        Assert.That(result.Buckets.Single(b => b.Bucket == "Prelaunch Failure").Count, Is.EqualTo(1));
    }

    [Test]
    public void Aggregate_PercentagesSumToOneHundred()
    {
        var result = _service.Aggregate("Company", null, SpaceMissionQueryService.DefaultMaxBuckets);

        var sum = result.Buckets.Sum(b => b.Percentage);
        Assert.That(sum, Is.EqualTo(100).Within(0.01));
    }

    [Test]
    public void Aggregate_InvalidColumn_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _service.Aggregate("NotAColumn", null, SpaceMissionQueryService.DefaultMaxBuckets));
    }

    [Test]
    public void GetSchema_ReturnsNineColumns()
    {
        Assert.That(_service.GetSchema(), Has.Count.EqualTo(9));
    }
}
