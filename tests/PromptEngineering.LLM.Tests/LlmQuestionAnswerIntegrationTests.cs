using System.Net.Http.Headers;
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
[Category("Integration")]
public sealed class LlmQuestionAnswerIntegrationTests
{
    private const string InstanceConfigPath = "SystemSettings:AiServiceSettings:Instances:0";

    [Test]
    [Explicit("Requires LLM API key, BaseAddress, and network access.")]
    public async Task CompleteChatAsync_UserQuestion_ReturnsExpectedAnswer_Live()
    {
        // 0. setup: initialize api-key (string)
        var configuration = BuildLiveConfiguration();
        var instanceName = configuration[$"{InstanceConfigPath}:Name"]
            ?? throw new InvalidOperationException("Missing instance name in configuration.");
        var apiKey = configuration[$"{InstanceConfigPath}:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Assert.Ignore(
                "Set SystemSettings:AiServiceSettings:Instances:0:ApiKey via user secrets, " +
                "environment variables, or appsettings.json before running this test.");
        }

        var services = new ServiceCollection();
        services.AddGenAi(configuration);

        await using var provider = services.BuildServiceProvider();
        var aiService = provider.GetRequiredService<IAiService>();

        // 1. input: user question (string)
        const string userQuestion = "What is 2+2? Reply with the number only.";
        const string expectedAnswer = "4";

        var request = new ChatRequest();
        request
            .SetTemperature(0f)
            .AddUserMessage(userQuestion);

        // 2. execute LLM (real HTTP)
        var completion = await aiService.CompleteChatAsync(
            instanceName,
            request,
            new MediaTypeHeaderValue("application/json"),
            new JsonSerializerOptions(JsonSerializerDefaults.General),
            CancellationToken.None);

        // 3. assert expected and actual answer
        var actualAnswer = completion?.Choices?.FirstOrDefault()?.Message?.Content;

        Assert.That(actualAnswer, Is.Not.Null.And.Not.Empty, "LLM returned no assistant content.");
        Assert.That(actualAnswer!.Trim(), Is.EqualTo(expectedAnswer));
    }

    private static IConfiguration BuildLiveConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(TestContext.CurrentContext.TestDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<LlmQuestionAnswerIntegrationTests>(optional: true)
            .AddEnvironmentVariables()
            .Build();
}
