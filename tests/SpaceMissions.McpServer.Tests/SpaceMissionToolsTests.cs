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
    public void GetSpaceMissionsSchema_ReturnsNineColumns()
    {
        var json = _tools.GetSpaceMissionsSchema();
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetArrayLength(), Is.EqualTo(9));
    }

    [Test]
    public void AggregateSpaceMissions_ByMissionStatus_ReturnsExpectedTotal()
    {
        var json = _tools.AggregateSpaceMissions("MissionStatus");
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("TotalRows").GetInt32(), Is.EqualTo(20));
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
}
