using System.Net.Http.Headers;
using System.Text.Json;
using Chatbot;
using Chatbot.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Models;
using PromptEngineering.Mcp;
using Rag;
using LlmRole = PromptEngineering.LLM.Models.Role;

namespace Chatbot.Tests;

[TestFixture]
public sealed class SpaceMissionsAgentServiceTests
{
    private IAiService _ai = null!;
    private ISpaceMissionsMcpAgentService _mcpAgent = null!;
    private RagIndexStore _ragIndexStore = null!;
    private RagOrchestrator _ragOrchestrator = null!;
    private SpaceMissionsAgentService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _ai = Substitute.For<IAiService>();
        _mcpAgent = Substitute.For<ISpaceMissionsMcpAgentService>();

        var agentOptions = Options.Create(new SpaceMissionsAgentOptions
        {
            InstanceName = "test-instance",
            Temperature = 0.2f,
            MaxFunctionIterations = 3
        });

        var aiSettings = Options.Create(new AiServiceSettings
        {
            SystemName = "openai",
            BaseAddress = "https://example.test/",
            ChatUrl = "chat",
            CompletionsUrl = "completions",
            DeploymentsUrl = "deployments",
            Instances =
            [
                new InstanceSettings
                {
                    Name = "test-instance",
                    ApiKey = "key",
                    Deployment = "test-deployment"
                }
            ]
        });

        var ragSettings = Options.Create(new RagSettings
        {
            DocumentsFolderPath = AppContext.BaseDirectory,
            DatasetPath = "dataset/space_missions.csv",
            InstanceName = "test-instance",
            TopK = 1,
            MinProseChunks = 0
        });

        _ragOrchestrator = new RagOrchestrator(_ai, ragSettings, NullLogger<RagOrchestrator>.Instance);
        _ragIndexStore = new RagIndexStore();
        SeedRagIndex("Company: SpaceX, Mission: Starlink");

        StubQueryEmbedding(new float[] { 1f, 0f });

        _service = new SpaceMissionsAgentService(
            agentOptions,
            aiSettings,
            _ai,
            _mcpAgent,
            _ragOrchestrator,
            _ragIndexStore,
            NullLogger<SpaceMissionsAgentService>.Instance);
    }

    [Test]
    public async Task RunAsync_WithToolCallLoop_ReturnsFinalAnswerAndRecordsToolName()
    {
        var session = new FakeMcpBackendSession(
            [new ChatToolDefinition { Function = new ChatToolFunctionDefinition { Name = "count_space_missions" } }],
            "count_space_missions",
            new CallToolResult { Content = [new TextContentBlock { Text = "{\"count\":42}" }] });

        _mcpAgent.ConnectAsync(Arg.Any<CancellationToken>()).Returns(session);

        _ai.CompleteChatAsync(
                Arg.Any<string>(),
                Arg.Any<ChatRequest>(),
                Arg.Any<MediaTypeHeaderValue>(),
                Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(
                CompletionWithToolCall("count_space_missions", "call-1"),
                CompletionWithText("There are 42 matching missions."));

        var result = await _service.RunAsync("How many missions?");

        Assert.That(result.AnswerText, Is.EqualTo("There are 42 matching missions."));
        Assert.That(result.ToolNamesInvoked, Is.EqualTo(["count_space_missions"]));
    }

    [Test]
    public async Task RunAsync_InjectsRetrievedContextAndToolHintsIntoSystemPrompt()
    {
        var session = new FakeMcpBackendSession(
            [new ChatToolDefinition { Function = new ChatToolFunctionDefinition { Name = "count_space_missions" } }],
            "count_space_missions",
            new CallToolResult { Content = [new TextContentBlock { Text = "{\"count\":1}" }] });

        _mcpAgent.ConnectAsync(Arg.Any<CancellationToken>()).Returns(session);

        ChatRequest? capturedRequest = null;
        _ai.CompleteChatAsync(
                Arg.Any<string>(),
                Arg.Do<ChatRequest>(r => capturedRequest ??= r),
                Arg.Any<MediaTypeHeaderValue>(),
                Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(CompletionWithText("Answer from hybrid agent."));

        await _service.RunAsync("What rocket names contain \"Falcon\"?");

        Assert.That(capturedRequest, Is.Not.Null);
        var systemMessage = capturedRequest!.Messages.First(m => m.Role == LlmRole.System);
        Assert.That(systemMessage.Content, Does.Contain("## Retrieved context"));
        Assert.That(systemMessage.Content, Does.Contain("Company: SpaceX, Mission: Starlink"));
        Assert.That(systemMessage.Content, Does.Contain("## Tool routing hints"));
        Assert.That(systemMessage.Content, Does.Contain("list_space_mission_distinct_values"));
        Assert.That(systemMessage.Content, Does.Contain("search=\"Falcon\""));
    }

    [Test]
    public void RunAsync_WhenMcpConnectFails_Throws()
    {
        _mcpAgent.ConnectAsync(Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("MCP unavailable"));

        Assert.ThrowsAsync<InvalidOperationException>(() => _service.RunAsync("How many missions?"));
    }

    [Test]
    public void RunAsync_WhenRagIndexNotReady_ThrowsRagIndexNotReadyException()
    {
        _ragIndexStore.Index = null;
        _ragIndexStore.IsBuilding = false;
        _ragIndexStore.BuildError = null;

        Assert.ThrowsAsync<RagIndexNotReadyException>(() => _service.RunAsync("How many missions?"));
    }

    [Test]
    public void RunAsync_WhenRagIndexIsBuilding_ThrowsFriendlyMessage()
    {
        _ragIndexStore.Index = null;
        _ragIndexStore.IsBuilding = true;

        var ex = Assert.ThrowsAsync<RagIndexNotReadyException>(() => _service.RunAsync("How many missions?"));
        Assert.That(ex!.Message, Does.Contain("still building"));
    }

    private void SeedRagIndex(string chunkText)
    {
        var store = new InMemoryVectorStore();
        store.Add(new VectorRecord("space_missions.csv", chunkText, new float[] { 1f, 0f }));
        _ragIndexStore.Index = store;
    }

    private void StubQueryEmbedding(float[] vector)
    {
        _ai.CreateEmbeddingsAsync(
                Arg.Any<string>(),
                Arg.Any<EmbeddingRequest>(),
                Arg.Any<MediaTypeHeaderValue>(),
                Arg.Any<JsonSerializerOptions>(),
                Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResponse
            {
                Data =
                [
                    new EmbeddingDataItem { Index = 0, Embedding = vector }
                ]
            });
    }

    private static ChatCompletion CompletionWithText(string content) => new()
    {
        Choices =
        [
            new ChatCompletionChoice
            {
                Message = new ChatMessage { Role = LlmRole.Assistant, Content = content }
            }
        ]
    };

    private static ChatCompletion CompletionWithToolCall(string toolName, string callId) => new()
    {
        Choices =
        [
            new ChatCompletionChoice
            {
                Message = new ChatMessage
                {
                    Role = LlmRole.Assistant,
                    ToolCalls =
                    [
                        new ChatToolCall
                        {
                            Id = callId,
                            Type = "function",
                            Function = new ChatToolCallFunction
                            {
                                Name = toolName,
                                Arguments = "{}"
                            }
                        }
                    ]
                }
            }
        ]
    };

    private sealed class FakeMcpBackendSession : IMcpBackendSession
    {
        private readonly string _toolName;
        private readonly CallToolResult _result;

        public FakeMcpBackendSession(
            IReadOnlyList<ChatToolDefinition> definitions,
            string toolName,
            CallToolResult result)
        {
            ToolDefinitions = definitions;
            Tools = [];
            _toolName = toolName;
            _result = result;
        }

        public IReadOnlyList<McpClientTool> Tools { get; }
        public IReadOnlyList<ChatToolDefinition> ToolDefinitions { get; }

        public Task<CallToolResult> CallToolAsync(string toolName, string argumentsJson, CancellationToken cancellationToken = default)
        {
            if (!toolName.Equals(_toolName, StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected tool '{toolName}'.");

            return Task.FromResult(_result);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
