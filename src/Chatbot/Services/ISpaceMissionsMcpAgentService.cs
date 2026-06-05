using PromptEngineering.Mcp;

namespace Chatbot.Services;

public interface ISpaceMissionsMcpAgentService
{
    Task<IMcpBackendSession> ConnectAsync(CancellationToken cancellationToken = default);
}
