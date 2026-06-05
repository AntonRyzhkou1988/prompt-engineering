using System.Text.Json;
using NUnit.Framework;
using PromptEngineering.SpaceMissions;
using SpaceMissions.McpServer.Tools;

namespace SpaceMissions.McpServer.Tests;

[TestFixture]
public sealed class SpaceMissionToolsTests
{
    private SpaceMissionTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "space_missions_sample.csv");
        _tools = new SpaceMissionTools(new SpaceMissionQueryService(path));
    }

    [Test]
    public void GetSpaceMissionsSchema_ReturnsColumnsAndMetadata()
    {
        var json = _tools.GetSpaceMissionsSchema();
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("columns").GetArrayLength(), Is.EqualTo(9));
        Assert.That(doc.RootElement.GetProperty("datasetRowCount").GetInt32(), Is.EqualTo(20));
        Assert.That(doc.RootElement.GetProperty("knownMissionStatusValues").GetArrayLength(), Is.EqualTo(4));
    }

    [Test]
    public void GetSpaceMissionsSummary_ReturnsOutcomeBreakdown()
    {
        var json = _tools.GetSpaceMissionsSummary();
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("totalRows").GetInt32(), Is.EqualTo(20));
        Assert.That(doc.RootElement.GetProperty("missionStatusBreakdown").GetArrayLength(), Is.GreaterThan(0));
    }

    [Test]
    public void AggregateSpaceMissions_ByMissionStatus_ReturnsExpectedTotal()
    {
        var json = _tools.AggregateSpaceMissions("MissionStatus");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("totalRows").GetInt32(), Is.EqualTo(20));
    }

    [Test]
    public void AggregateSpaceMissions_InvalidColumn_ReturnsErrorJson()
    {
        var json = _tools.AggregateSpaceMissions("NotAColumn");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.TryGetProperty("error", out var error), Is.True);
        Assert.That(error.GetString(), Does.Contain("Invalid groupBy column"));
    }

    [Test]
    public void CountSpaceMissions_WithMissionStatusFilter_ReturnsExpectedCount()
    {
        var json = _tools.CountSpaceMissions(missionStatus: "Failure");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(4));
    }

    [Test]
    public void FilterSpaceMissions_ReturnsTotalMatching()
    {
        var json = _tools.FilterSpaceMissions(company: "SpaceX");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("returned").GetInt32(), Is.EqualTo(5));
        Assert.That(doc.RootElement.GetProperty("totalMatching").GetInt32(), Is.EqualTo(5));
    }

    [Test]
    public void FilterSpaceMissions_InvalidDateFrom_ReturnsWarning()
    {
        var json = _tools.FilterSpaceMissions(dateFrom: "not-a-date");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("warnings").GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public void ListSpaceMissionDistinctValues_ReturnsCompanies()
    {
        var json = _tools.ListSpaceMissionDistinctValues("Company");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("column").GetString(), Is.EqualTo("Company"));
        Assert.That(doc.RootElement.GetProperty("values").GetArrayLength(), Is.GreaterThan(0));
    }

    [Test]
    public void AggregateSpaceMissionsByLaunchCountry_ReturnsDerivationRule()
    {
        var json = _tools.AggregateSpaceMissionsByLaunchCountry();
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("derivationRule").GetString(), Does.Contain("comma"));
        Assert.That(doc.RootElement.GetProperty("totalRows").GetInt32(), Is.EqualTo(20));
    }

    [Test]
    public void ComputeSpaceMissionSuccessRate_ForFailures_ReturnsZeroPercent()
    {
        var json = _tools.ComputeSpaceMissionSuccessRate(missionStatus: "Failure");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("successCount").GetInt32(), Is.EqualTo(0));
        Assert.That(doc.RootElement.GetProperty("successRatePercent").GetDouble(), Is.EqualTo(0));
    }
}
