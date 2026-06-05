using Chatbot.Services;
using NUnit.Framework;

namespace Chatbot.Tests;

[TestFixture]
public sealed class SpaceMissionToolRoutingHintsTests
{
    [Test]
    public void BuildHints_FalconRocketQuestion_SuggestsDistinctValuesWithSearch()
    {
        var hints = SpaceMissionToolRoutingHints.BuildHints("What rocket names contain \"Falcon\"?");

        Assert.That(hints, Does.Contain("list_space_mission_distinct_values"));
        Assert.That(hints, Does.Contain("Rocket"));
        Assert.That(hints, Does.Contain("search=\"Falcon\""));
    }

    [Test]
    public void BuildHints_CountQuestion_SuggestsCountTool()
    {
        var hints = SpaceMissionToolRoutingHints.BuildHints("How many SpaceX launches are there?");

        Assert.That(hints, Does.Contain("count_space_missions"));
    }

    [Test]
    public void BuildHints_SchemaQuestion_SuggestsSchemaTool()
    {
        var hints = SpaceMissionToolRoutingHints.BuildHints("What columns are in the dataset?");

        Assert.That(hints, Does.Contain("get_space_missions_schema"));
    }

    [Test]
    public void BuildHints_GenericQuestion_ReturnsEmpty()
    {
        var hints = SpaceMissionToolRoutingHints.BuildHints("Tell me about space exploration history.");

        Assert.That(hints, Is.Empty);
    }
}
