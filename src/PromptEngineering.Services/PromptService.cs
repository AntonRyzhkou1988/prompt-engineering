using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;
using PromptEngineering.Model;

namespace PromptEngineering.Services;

public sealed class PromptService : IPromptService
{
    private const string AssistantRoleKey = "assistant.role";
    private const string DefaultAssistantRole = "You are software developer assistant.";
    private const string UserPrompt = "What is a GC in .NET?";

    private readonly IContextService _contextService;

    public PromptService(IContextService contextService)
    {
        ArgumentNullException.ThrowIfNull(contextService);
        _contextService = contextService;
    }

    public async Task<ChatRequest> BuildAsync(CancellationToken cancellationToken = default)
    {
        await _contextService.UpsertAsync(
            new Context(AssistantRoleKey, DefaultAssistantRole),
            cancellationToken);

        var assistantRole = await _contextService.GetByKeyAsync(AssistantRoleKey, cancellationToken);

        var chatRequest = new ChatRequest
        {
            Temperature = 0.3f
        };
        chatRequest.AddSystemMessage(assistantRole?.Value ?? DefaultAssistantRole);
        chatRequest.AddUserMessage(UserPrompt);

        return chatRequest;
    }
}
