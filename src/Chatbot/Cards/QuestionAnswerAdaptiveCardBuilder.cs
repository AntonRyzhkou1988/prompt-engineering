using System.Text.Json;
using AdaptiveCards;
using Microsoft.Agents.Core.Models;

namespace Chatbot.Cards;

public static class QuestionAnswerAdaptiveCardBuilder
{
    public const string AdaptiveCardContentType = "application/vnd.microsoft.card.adaptive";
    private const int MaxTextLength = 4000;

    public static Attachment CreateAttachment(string userQuestion, string answerText, string? correlationId = null)
    {
        var correlation = string.IsNullOrWhiteSpace(correlationId)
            ? Guid.NewGuid().ToString("N")
            : correlationId;

        var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 5))
        {
            Body =
            [
                new AdaptiveTextBlock
                {
                    Text = "Your question",
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                },
                new AdaptiveTextBlock
                {
                    Text = Truncate(Normalize(userQuestion)),
                    Wrap = true,
                    IsSubtle = true,
                    Spacing = AdaptiveSpacing.Small,
                },
                new AdaptiveTextBlock
                {
                    Text = "Answer",
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                    Separator = true,
                },
                new AdaptiveTextBlock
                {
                    Text = Truncate(Normalize(answerText)),
                    Wrap = true,
                },
                new AdaptiveActionSet
                {
                    Actions =
                    [
                        CreateSubmitAction("Acknowledge", new { verb = "acknowledge", correlationId = correlation }),
                        CreateSubmitAction("Like", new { verb = "feedback", rating = "like", correlationId = correlation }),
                        CreateSubmitAction("Dislike", new { verb = "feedback", rating = "dislike", correlationId = correlation }),
                    ],
                },
            ],
        };

        return new Attachment
        {
            ContentType = AdaptiveCardContentType,
            Content = JsonSerializer.Deserialize<JsonElement>(card.ToJson()),
        };
    }

    private static AdaptiveSubmitAction CreateSubmitAction(string title, object data) =>
        new()
        {
            Title = title,
            Data = data,
        };

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(empty)" : value.Trim();

    private static string Truncate(string value) =>
        value.Length <= MaxTextLength ? value : value[..(MaxTextLength - 1)] + "…";
}
