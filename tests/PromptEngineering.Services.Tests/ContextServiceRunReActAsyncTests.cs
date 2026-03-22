using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.Services.Tests;

[TestFixture]
public sealed class ContextServiceRunReActAsyncTests
{
    private string _tempDir = null!;
    private string _datasetPath = null!;
    private string _outputDir = null!;
    private string _promptsDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"pe_tests_{Guid.NewGuid():N}");
        _datasetPath = Path.Combine(_tempDir, "attacks.csv");
        _outputDir = Path.Combine(_tempDir, "output");
        _promptsDir = Path.Combine(_tempDir, "prompts");

        Directory.CreateDirectory(_outputDir);
        Directory.CreateDirectory(_promptsDir);

        File.WriteAllText(_datasetPath, CsvContent);
        File.WriteAllText(Path.Combine(_promptsDir, "v1.json"), PromptJson);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private const string CsvContent =
        "Year,Country,Area,Type,Activity,Injury,Fatal (Y/N),Sex,Age,Time,Species,Investigator or Source\n" +
        "2020,USA,Florida,Unprovoked,Surfing,Laceration,N,M,25,1400,Blacktip,ISAF\n";

    private const string PromptJson = """
        {
          "InstanceName": "default",
          "DefaultAssistantRole": ["You are a data analyst."],
          "DefaultUserPrompt": ["Analyze the following data: <data></data>"]
        }
        """;

    private ContextService BuildService(IAiService aiService, List<string> sequence)
    {
        var systemSettings = new SystemSettings
        {
            MaximumDatasetRecordCount = 100,
            AiServiceSettings = new AiServiceSettings
            {
                SystemName = "openai",
                DeploymentsUrl = "deployments",
                ChatUrl = "chat",
                CompletionsUrl = "completions",
                Instances = [new InstanceSettings { Name = "default" }]
            }
        };

        var contextSettings = new ContextSettings
        {
            PromptPath = _promptsDir,
            DatasetPath = _datasetPath,
            OutputDirectory = _outputDir,
            ReActSequence = sequence
        };

        return new ContextService(
            Options.Create(systemSettings),
            Options.Create(contextSettings),
            aiService);
    }

    [Test]
    public void RunReActAsync_EmptySequence_Throws()
    {
        var aiService = Substitute.For<IAiService>();
        var sut = BuildService(aiService, []);

        Assert.ThrowsAsync<ArgumentException>(() => sut.RunReActAsync());
    }

    [Test]
    public async Task RunReActAsync_SinglePrompt_ReturnsOneResult()
    {
        var completion = new ChatCompletion
        {
            Id = "test-id",
            Choices =
            [
                new ChatCompletionChoice
                {
                    Message = new ChatMessage { Content = "Analysis complete." }
                }
            ]
        };

        var aiService = Substitute.For<IAiService>();
        aiService
            .CompleteChatAsync(
                Arg.Any<string>(),
                Arg.Any<ChatRequest>(),
                Arg.Any<MediaTypeHeaderValue>(),
                Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(completion);

        var sut = BuildService(aiService, ["v1.json"]);

        var results = await sut.RunReActAsync();

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].PromptStem, Is.EqualTo("v1"));
        Assert.That(results[0].Completion, Is.SameAs(completion));
        Assert.That(File.Exists(results[0].OutputPath), Is.True);
    }

    [Test]
    public async Task RunReActAsync_SinglePrompt_WritesCompletionContentToFile()
    {
        const string expectedContent = "## Key Insights\n- High activity in Florida.";

        var completion = new ChatCompletion
        {
            Choices =
            [
                new ChatCompletionChoice
                {
                    Message = new ChatMessage { Content = expectedContent }
                }
            ]
        };

        var aiService = Substitute.For<IAiService>();
        aiService
            .CompleteChatAsync(
                Arg.Any<string>(),
                Arg.Any<ChatRequest>(),
                Arg.Any<MediaTypeHeaderValue>(),
                Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(completion);

        var sut = BuildService(aiService, ["v1.json"]);

        var results = await sut.RunReActAsync();

        var fileContent = await File.ReadAllTextAsync(results[0].OutputPath);
        Assert.That(fileContent, Is.EqualTo(expectedContent));
    }

    [Test]
    public async Task RunReActAsync_MultiplePrompts_PassesPriorCompletionToNextRun()
    {
        File.WriteAllText(Path.Combine(_promptsDir, "v2.json"), """
            {
              "InstanceName": "default",
              "DefaultAssistantRole": ["You are a data analyst."],
              "DefaultUserPrompt": ["Refine: <data></data> <prior_run></prior_run>"]
            }
            """);

        const string v1Content = "v1 analysis result";

        var v1Completion = new ChatCompletion
        {
            Choices = [new ChatCompletionChoice { Message = new ChatMessage { Content = v1Content } }]
        };
        var v2Completion = new ChatCompletion
        {
            Choices = [new ChatCompletionChoice { Message = new ChatMessage { Content = "v2 refined" } }]
        };

        var aiService = Substitute.For<IAiService>();
        aiService
            .CompleteChatAsync(
                Arg.Any<string>(), Arg.Any<ChatRequest>(),
                Arg.Any<MediaTypeHeaderValue>(), Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(v1Completion, v2Completion);

        var sut = BuildService(aiService, ["v1.json", "v2.json"]);

        var results = await sut.RunReActAsync();

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results[0].PromptStem, Is.EqualTo("v1"));
        Assert.That(results[1].PromptStem, Is.EqualTo("v2"));

        var v2FileContent = await File.ReadAllTextAsync(results[1].OutputPath);
        Assert.That(v2FileContent, Does.Contain("v2 refined"));
    }

    [Test]
    public void RunReActAsync_AiServiceThrows_WrapsException()
    {
        var aiService = Substitute.For<IAiService>();
        aiService
            .CompleteChatAsync(
                Arg.Any<string>(), Arg.Any<ChatRequest>(),
                Arg.Any<MediaTypeHeaderValue>(), Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var sut = BuildService(aiService, ["v1.json"]);

        var ex = Assert.ThrowsAsync<Exception>(() => sut.RunReActAsync());
        Assert.That(ex!.Message, Does.Contain("Execution failed with system exception."));
        Assert.That(ex.InnerException, Is.InstanceOf<HttpRequestException>());
    }

    [Test]
    public void RunReActAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var aiService = Substitute.For<IAiService>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = BuildService(aiService, ["v1.json"]);

        Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.RunReActAsync(cts.Token));
    }

    [Test]
    public void RunReActAsync_MissingDatasetFile_Throws()
    {
        File.Delete(_datasetPath);

        var aiService = Substitute.For<IAiService>();
        var sut = BuildService(aiService, ["v1.json"]);

        Assert.CatchAsync<Exception>(() => sut.RunReActAsync());
    }

    [Test]
    public void RunReActAsync_NullCompletionFromAiService_Throws()
    {
        var aiService = Substitute.For<IAiService>();
        aiService
            .CompleteChatAsync(
                Arg.Any<string>(), Arg.Any<ChatRequest>(),
                Arg.Any<MediaTypeHeaderValue>(), Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .Returns((ChatCompletion?)null);

        var sut = BuildService(aiService, ["v1.json"]);

        var ex = Assert.ThrowsAsync<Exception>(() => sut.RunReActAsync());
        Assert.That(ex!.InnerException, Is.InstanceOf<InvalidOperationException>());
    }
}
