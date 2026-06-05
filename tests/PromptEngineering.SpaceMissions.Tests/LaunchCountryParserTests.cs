using NUnit.Framework;

namespace PromptEngineering.SpaceMissions.Tests;

[TestFixture]
public sealed class LaunchCountryParserTests
{
    [Test]
    public void DeriveCountry_LastCommaSegment_ReturnsCountry()
    {
        Assert.That(
            LaunchCountryParser.DeriveCountry("LC-39A, Kennedy Space Center, Florida, USA"),
            Is.EqualTo("USA"));
        Assert.That(
            LaunchCountryParser.DeriveCountry("Site 1/5, Baikonur Cosmodrome, Kazakhstan"),
            Is.EqualTo("Kazakhstan"));
    }

    [Test]
    public void DeriveCountry_EmptyOrNoComma_ReturnsUnparseable()
    {
        Assert.That(LaunchCountryParser.DeriveCountry(""), Is.EqualTo(LaunchCountryParser.UnparseableBucket));
        Assert.That(LaunchCountryParser.DeriveCountry("   "), Is.EqualTo(LaunchCountryParser.UnparseableBucket));
        Assert.That(LaunchCountryParser.DeriveCountry("Single site name"), Is.EqualTo(LaunchCountryParser.UnparseableBucket));
    }
}
