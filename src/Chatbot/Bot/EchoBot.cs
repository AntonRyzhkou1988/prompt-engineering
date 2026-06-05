using Chatbot.Services;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
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
            await turnContext.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
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
}
