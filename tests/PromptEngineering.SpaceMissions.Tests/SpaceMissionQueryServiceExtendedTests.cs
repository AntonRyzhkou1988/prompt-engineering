using NUnit.Framework;

namespace PromptEngineering.SpaceMissions.Tests;

[TestFixture]
public sealed class SpaceMissionQueryServiceExtendedTests
{
    private ISpaceMissionQueryService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "space_missions_sample.csv");
        _service = new SpaceMissionQueryService(path);
    }

    [Test]
    public void GetSummary_ReturnsTwentyRowsAndDateRange()
    {
        var summary = _service.GetSummary();

        Assert.That(summary.TotalRows, Is.EqualTo(20));
        Assert.That(summary.DateMin, Is.EqualTo("1957-10-04"));
        Assert.That(summary.DateMax, Is.EqualTo("2022-04-27"));
        Assert.That(summary.MissionStatusBreakdown, Is.Not.Empty);
    }

    [Test]
    public void Filter_CompanyContains_ReturnsMatchingRows()
    {
        var rows = _service.Filter(new SpaceMissionFilter { CompanyContains = "Space" }, 200);

        Assert.That(rows, Has.Count.EqualTo(5));
        Assert.That(rows.All(r => r.Company.Contains("Space", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void Filter_OffsetSkipsEarlierRows()
    {
        var all = _service.Filter(null, 200);
        var page = _service.Filter(null, 5, offset: 5);

        Assert.That(page, Has.Count.EqualTo(5));
        Assert.That(page[0].Date, Is.EqualTo(all[5].Date));
        Assert.That(page[0].Mission, Is.EqualTo(all[5].Mission));
    }

    [Test]
    public void DistinctValues_ByCompany_ReturnsSortedDistinct()
    {
        var result = _service.DistinctValues("Company", null, null, 100);

        Assert.That(result.Column, Is.EqualTo("Company"));
        Assert.That(result.TotalDistinct, Is.GreaterThan(5));
        Assert.That(result.Values, Does.Contain("SpaceX"));
    }

    [Test]
    public void DistinctValues_WithSearch_FiltersValues()
    {
        var result = _service.DistinctValues("Company", null, "Space", 100);

        Assert.That(result.Values.All(v => v.Contains("Space", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public void AggregateByLaunchCountry_GroupsByLastSegment()
    {
        var result = _service.AggregateByLaunchCountry(null, SpaceMissionQueryService.DefaultMaxBuckets);

        Assert.That(result.GroupByColumn, Is.EqualTo("LaunchCountry"));
        Assert.That(result.TotalRows, Is.EqualTo(20));
        Assert.That(result.Buckets.Any(b => b.Bucket == "USA"), Is.True);
    }

    [Test]
    public void ComputeSuccessRate_ForSpaceX_ReturnsExpectedRate()
    {
        var result = _service.ComputeSuccessRate(new SpaceMissionFilter { Company = "SpaceX" });

        Assert.That(result.TotalMatching, Is.EqualTo(5));
        Assert.That(result.SuccessCount, Is.EqualTo(5));
        Assert.That(result.Denominator, Is.EqualTo(5));
        Assert.That(result.SuccessRate, Is.EqualTo(100));
    }

    [Test]
    public void Aggregate_MaxBuckets_RollsRemainderIntoOther()
    {
        var result = _service.Aggregate("Company", null, maxBuckets: 3);

        Assert.That(result.Buckets, Has.Count.EqualTo(3));
        Assert.That(result.Other, Is.Not.Null);
        Assert.That(result.Other!.Count, Is.GreaterThan(0));
        Assert.That(result.Buckets.Sum(b => b.Count) + result.Other.Count, Is.EqualTo(result.TotalRows));
    }
}
