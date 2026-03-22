using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.Services.Tests;

[TestFixture]
public sealed class ContextServiceConstructorTests
{
    private static SystemSettings ValidSystemSettings() => new()
    {
        MaximumDatasetRecordCount = 10,
        AiServiceSettings = new AiServiceSettings
        {
            SystemName = "openai",
            DeploymentsUrl = "deployments",
            ChatUrl = "chat",
            CompletionsUrl = "completions",
            Instances = [new InstanceSettings { Name = "default" }]
        }
    };

    private static ContextSettings ValidContextSettings() => new()
    {
        PromptPath = "prompts",
        DatasetPath = "dataset.csv",
        OutputDirectory = "output",
        ReActSequence = ["v1.json"]
    };

    [Test]
    public void Constructor_NullSystemSettings_Throws()
    {
        var contextOptions = Options.Create(ValidContextSettings());
        var aiService = Substitute.For<IAiService>();

        Assert.Throws<ArgumentNullException>(() =>
            _ = new ContextService(null!, contextOptions, aiService));
    }

    [Test]
    public void Constructor_NullContextSettings_Throws()
    {
        var systemOptions = Options.Create(ValidSystemSettings());
        var aiService = Substitute.For<IAiService>();

        Assert.Throws<ArgumentNullException>(() =>
            _ = new ContextService(systemOptions, null!, aiService));
    }

    [Test]
    public void Constructor_NullAiService_Throws()
    {
        var systemOptions = Options.Create(ValidSystemSettings());
        var contextOptions = Options.Create(ValidContextSettings());

        Assert.Throws<ArgumentNullException>(() =>
            _ = new ContextService(systemOptions, contextOptions, null!));
    }

    [Test]
    public void Constructor_EmptyInstances_Throws()
    {
        var settings = ValidSystemSettings() with
        {
            AiServiceSettings = new AiServiceSettings
            {
                SystemName = "openai",
                DeploymentsUrl = "deployments",
                ChatUrl = "chat",
                CompletionsUrl = "completions",
                Instances = []
            }
        };

        var systemOptions = Options.Create(settings);
        var contextOptions = Options.Create(ValidContextSettings());
        var aiService = Substitute.For<IAiService>();

        Assert.Throws<ArgumentException>(() =>
            _ = new ContextService(systemOptions, contextOptions, aiService));
    }

    [Test]
    public void Constructor_ValidArguments_DoesNotThrow()
    {
        var systemOptions = Options.Create(ValidSystemSettings());
        var contextOptions = Options.Create(ValidContextSettings());
        var aiService = Substitute.For<IAiService>();

        Assert.DoesNotThrow(() =>
            _ = new ContextService(systemOptions, contextOptions, aiService));
    }
}
