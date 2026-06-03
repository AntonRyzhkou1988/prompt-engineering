using Chatbot.Services;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;

namespace Chatbot.Bot;

public class EchoBot : AgentApplication
{
    private readonly SpaceMissionsAgentService _agentService;
    private readonly ILogger<EchoBot> _logger;

    public EchoBot(
        AgentApplicationOptions options,
        SpaceMissionsAgentService agentService,
        ILogger<EchoBot> logger) : base(options)
    {
        _agentService = agentService;
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
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("Hello! Ask me about space missions from dataset/space_missions.csv."),
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
            var result = await _agentService.RunAsync(question, cancellationToken);
            var answer = string.IsNullOrWhiteSpace(result.AnswerText)
                ? "I could not produce an answer from the space missions data."
                : result.AnswerText;
            await turnContext.SendActivityAsync(MessageFactory.Text(answer), cancellationToken);
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
