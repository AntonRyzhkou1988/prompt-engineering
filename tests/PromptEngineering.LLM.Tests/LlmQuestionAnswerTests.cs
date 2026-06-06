using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using PromptEngineering.LLM;
using PromptEngineering.LLM.Configurations;
using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;

namespace PromptEngineering.LLM.Tests;

[TestFixture]
public sealed class LlmQuestionAnswerTests
{
    private const string InstanceName = "PromptEngineering.LLM.Tests";

    [Test]
    public async Task CompleteChatAsync_UserQuestion_ReturnsExpectedAnswer()
    {
        // 0. setup: initialize api-key (string)
        const string apiKey = "";
        const string expectedAnswer = "4";

        var handler = new StubHttpMessageHandler(
            (_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""
                    {
                      "id": "chatcmpl-test",
                      "object": "chat.completion",
                      "choices": [
                        {
                          "index": 0,
                          "message": { "role": "assistant", "content": "{{expectedAnswer}}" },
                          "finish_reason": "stop"
                        }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });

        var configuration = BuildConfiguration(apiKey);
        var services = new ServiceCollection();
        services.AddGenAi(configuration);
        services.AddHttpClient(InstanceName)
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        await using var provider = services.BuildServiceProvider();
        var aiService = provider.GetRequiredService<IAiService>();

        // 1. input: user question (string)
        const string userQuestion = "What is 2+2? Reply with the number only.";

        var request = new ChatRequest();
        request
            .SetTemperature(0f)
            .AddUserMessage(userQuestion);

        // 2. execute LLM
        var completion = await aiService.CompleteChatAsync(
            InstanceName,
            request,
            new MediaTypeHeaderValue("application/json"),
            new JsonSerializerOptions(JsonSerializerDefaults.General),
            CancellationToken.None);

        // 3. assert expected and actual answer
        var actualAnswer = completion?.Choices?.FirstOrDefault()?.Message?.Content;

        Assert.That(actualAnswer, Is.EqualTo(expectedAnswer));
        Assert.That(handler.LastRequest, Is.Not.Null);
        Assert.That(handler.LastRequest!.Headers.GetValues("api-key").First(), Is.EqualTo(apiKey));
    }

    private static IConfiguration BuildConfiguration(string apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SystemSettings:AiServiceSettings:BaseAddress"] = "https://ai-proxy.lab.epam.com/",
                ["SystemSettings:AiServiceSettings:SystemName"] = "openai",
                ["SystemSettings:AiServiceSettings:DeploymentsUrl"] = "deployments",
                ["SystemSettings:AiServiceSettings:ChatUrl"] = "chat",
                ["SystemSettings:AiServiceSettings:CompletionsUrl"] = "completions",
                ["SystemSettings:AiServiceSettings:Retry:RetryCount"] = "3",
                ["SystemSettings:AiServiceSettings:Retry:BackoffBase"] = "2",
                ["SystemSettings:AiServiceSettings:Instances:0:Name"] = InstanceName,
                ["SystemSettings:AiServiceSettings:Instances:0:ApiKey"] = apiKey,
                ["SystemSettings:AiServiceSettings:Instances:0:Deployment"] = "gpt-4o"
            })
            .Build();

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responder(request, cancellationToken));
        }
    }
}
