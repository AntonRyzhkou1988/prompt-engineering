namespace Agent;

public interface INewsAgentService
{
    Task<IMcpBackendSession> ConnectAsync(CancellationToken cancellationToken = default);
}
