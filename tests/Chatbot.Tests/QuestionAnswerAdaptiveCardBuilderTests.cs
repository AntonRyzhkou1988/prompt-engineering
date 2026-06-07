using System.Text.Json;
using System.Text.Json.Nodes;
using Chatbot.Cards;
using NUnit.Framework;

namespace Chatbot.Tests;

[TestFixture]
public sealed class QuestionAnswerAdaptiveCardBuilderTests
{
    [Test]
    public void CreateAttachment_UsesAdaptiveCardContentType()
    {
        var attachment = QuestionAnswerAdaptiveCardBuilder.CreateAttachment("How many missions?", "42 missions.");

        Assert.That(attachment.ContentType, Is.EqualTo(QuestionAnswerAdaptiveCardBuilder.AdaptiveCardContentType));
    }

    [Test]
    public void CreateAttachment_IncludesUserQuestion()
    {
        const string userQuestion = "Which company launched Sputnik-1?";

        var attachment = QuestionAnswerAdaptiveCardBuilder.CreateAttachment(userQuestion, "RVSN USSR.");
        var cardJson = GetCardJson(attachment);

        Assert.That(cardJson, Does.Contain(userQuestion));
        Assert.That(cardJson, Does.Contain("Your question"));
    }

    [Test]
    public void CreateAttachment_IncludesAnswer()
    {
        const string answer = "There are 42 matching missions in the dataset.";

        var attachment = QuestionAnswerAdaptiveCardBuilder.CreateAttachment("How many missions?", answer);
        var cardJson = GetCardJson(attachment);

        Assert.That(cardJson, Does.Contain(answer));
        Assert.That(cardJson, Does.Contain("Answer"));
    }

    [Test]
    public void CreateAttachment_HasAcknowledgeButton()
    {
        var attachment = QuestionAnswerAdaptiveCardBuilder.CreateAttachment("Question?", "Answer.");
        var actions = GetSubmitActions(attachment);

        Assert.That(actions.Any(a => a?["title"]?.GetValue<string>() == "Acknowledge"), Is.True);
        Assert.That(actions.Any(a =>
            a?["data"]?["verb"]?.GetValue<string>() == "acknowledge"), Is.True);
    }

    [Test]
    public void CreateAttachment_HasLikeAndDislikeButtons()
    {
        var attachment = QuestionAnswerAdaptiveCardBuilder.CreateAttachment("Question?", "Answer.");
        var actions = GetSubmitActions(attachment);

        Assert.That(actions.Any(a => a?["title"]?.GetValue<string>() == "Like"), Is.True);
        Assert.That(actions.Any(a => a?["title"]?.GetValue<string>() == "Dislike"), Is.True);

        Assert.That(actions.Any(a =>
            a?["data"]?["verb"]?.GetValue<string>() == "feedback" &&
            a?["data"]?["rating"]?.GetValue<string>() == "like"), Is.True);

        Assert.That(actions.Any(a =>
            a?["data"]?["verb"]?.GetValue<string>() == "feedback" &&
            a?["data"]?["rating"]?.GetValue<string>() == "dislike"), Is.True);
    }

    [Test]
    public void CreateAttachment_IncludesCorrelationIdInSubmitData()
    {
        const string correlationId = "abc123correlation";

        var attachment = QuestionAnswerAdaptiveCardBuilder.CreateAttachment(
            "Question?",
            "Answer.",
            correlationId);
        var actions = GetSubmitActions(attachment);

        Assert.That(actions, Is.Not.Empty);
        Assert.That(actions.All(a => a?["data"]?["correlationId"]?.GetValue<string>() == correlationId), Is.True);
    }

    private static string GetCardJson(Microsoft.Agents.Core.Models.Attachment attachment)
    {
        Assert.That(attachment.Content, Is.Not.Null);
        return JsonSerializer.Serialize(attachment.Content);
    }

    private static JsonArray GetSubmitActions(Microsoft.Agents.Core.Models.Attachment attachment)
    {
        var cardJson = GetCardJson(attachment);
        var root = JsonNode.Parse(cardJson)!.AsObject();
        var actions = new JsonArray();

        if (root["body"] is JsonArray body)
        {
            foreach (var block in body)
            {
                if (block?["type"]?.GetValue<string>() == "ActionSet" && block["actions"] is JsonArray blockActions)
                {
                    foreach (var action in blockActions)
                        actions.Add(action?.DeepClone());
                }
            }
        }

        return actions;
    }
}
