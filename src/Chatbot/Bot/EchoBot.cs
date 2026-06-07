using System.Text.Json;
using Chatbot.Cards;
using Chatbot.Services;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.App.AdaptiveCards;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;

namespace Chatbot.Bot;

public class EchoBot : AgentApplication
{
    private readonly SpaceMissionsAgentService _agentService;
    private readonly RagIndexStore _ragIndexStore;
    private readonly ILogger<EchoBot> _logger;

    public EchoBot(
        AgentApplicationOptions options,
        SpaceMissionsAgentService agentService,
        RagIndexStore ragIndexStore,
        ILogger<EchoBot> logger) : base(options)
    {
        _agentService = agentService;
        _ragIndexStore = ragIndexStore;
        _logger = logger;

        OnConversationUpdate(ConversationUpdateEvents.MembersAdded, WelcomeMessageAsync);
        OnActivity(ActivityTypes.Message, OnMessageAsync);
        AdaptiveCards.OnActionSubmit("acknowledge", OnAcknowledgeAsync);
        AdaptiveCards.OnActionSubmit("feedback", OnFeedbackAsync);
    }

    protected async Task WelcomeMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        foreach (ChannelAccount member in turnContext.Activity.MembersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                var readiness = _ragIndexStore.IsReady
                    ? "The dataset index is ready."
                    : _ragIndexStore.IsBuilding
                        ? "The dataset index is still building — first answers may take a moment."
                        : "The dataset index is not ready yet.";

                await turnContext.SendActivityAsync(
                    MessageFactory.Text(
                        $"Hello! Ask about space missions — I combine retrieved dataset context with MCP tools for precise counts, distinct values, and filters. {readiness}"),
                    cancellationToken);
            }
        }
    }

    protected async Task OnMessageAsync(ITurnContext turnContext, ITurnState turnState, CancellationToken cancellationToken)
    {
        var question = turnContext.Activity.Text?.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Please send a question about space missions."),
                cancellationToken);
            return;
        }

        try
        {
            await turnContext.SendActivityAsync(new Activity { Type = ActivityTypes.Typing }, cancellationToken);

            var result = await _agentService.RunAsync(question, cancellationToken);
            var answer = string.IsNullOrWhiteSpace(result.AnswerText)
                ? "I could not produce an answer from the space missions data."
                : result.AnswerText;

            var correlationId = Guid.NewGuid().ToString("N");
            var attachment = QuestionAnswerAdaptiveCardBuilder.CreateAttachment(question, answer, correlationId);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(attachment), cancellationToken);
        }
        catch (RagIndexNotReadyException ex)
        {
            _logger.LogWarning(ex, "RAG index not ready for question.");
            await turnContext.SendActivityAsync(
                MessageFactory.Text(ex.Message),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to answer space missions question.");
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Sorry, I could not answer that question right now. Please try again later."),
                cancellationToken);
        }
    }

    protected async Task OnAcknowledgeAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        object data,
        CancellationToken cancellationToken)
    {
        var correlationId = TryGetCorrelationId(data);
        _logger.LogInformation("User acknowledged answer. CorrelationId={CorrelationId}", correlationId);

        await turnContext.SendActivityAsync(
            MessageFactory.Text("Thanks — marked as read."),
            cancellationToken);
    }

    protected async Task OnFeedbackAsync(
        ITurnContext turnContext,
        ITurnState turnState,
        object data,
        CancellationToken cancellationToken)
    {
        var rating = TryGetStringProperty(data, "rating") ?? "unknown";
        var correlationId = TryGetCorrelationId(data);
        _logger.LogInformation(
            "User submitted feedback. Rating={Rating}, CorrelationId={CorrelationId}",
            rating,
            correlationId);

        await turnContext.SendActivityAsync(
            MessageFactory.Text("Thanks for your feedback!"),
            cancellationToken);
    }

    private static string? TryGetCorrelationId(object data) =>
        TryGetStringProperty(data, "correlationId");

    private static string? TryGetStringProperty(object data, string propertyName)
    {
        if (data is JsonElement element)
            return element.TryGetProperty(propertyName, out var value) ? value.GetString() : null;

        if (data is JsonDocument document && document.RootElement.TryGetProperty(propertyName, out var docValue))
            return docValue.GetString();

        return null;
    }
}
