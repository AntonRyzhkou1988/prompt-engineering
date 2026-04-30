namespace Agent;

public interface IWeatherAgentService
{
    Task<IMcpBackendSession> ConnectAsync(CancellationToken cancellationToken = default);
}
