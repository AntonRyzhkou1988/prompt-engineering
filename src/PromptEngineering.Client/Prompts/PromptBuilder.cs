using PromptEngineering.LLM.Extensions;
using PromptEngineering.LLM.Models;
using PromptEngineering.Model;
using PromptEngineering.Services;

namespace PromptEngineering.Client.Prompts;

internal static class PromptBuilder
{
    private const string AssistantRoleKey = "assistant.role";
    private const string DefaultAssistantRole = "You are software developer assistant.";
    private const string UserPrompt = "What is a GC in .NET?";

    public static async Task<ChatRequest> BuildAsync(
        IContextService contextService,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextService);

        await contextService.UpsertAsync(
            new Context(AssistantRoleKey, DefaultAssistantRole),
            cancellationToken);

        var assistantRole = await contextService.GetByKeyAsync(AssistantRoleKey, cancellationToken);

        var chatRequest = new ChatRequest
        {
            Temperature = 0.3f
        };
        chatRequest.AddSystemMessage(assistantRole?.Value ?? DefaultAssistantRole);
        chatRequest.AddUserMessage(UserPrompt);

        return chatRequest;
    }
}
