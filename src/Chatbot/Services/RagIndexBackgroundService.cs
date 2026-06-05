using Rag;

namespace Chatbot.Services;

public sealed class RagIndexBackgroundService : BackgroundService
{
    private readonly RagOrchestrator _orchestrator;
    private readonly RagIndexStore _store;
    private readonly ILogger<RagIndexBackgroundService> _logger;

    public RagIndexBackgroundService(
        RagOrchestrator orchestrator,
        RagIndexStore store,
        ILogger<RagIndexBackgroundService> logger)
    {
        _orchestrator = orchestrator;
        _store = store;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _store.IsBuilding = true;
        _store.BuildError = null;

        try
        {
            _logger.LogInformation("Building RAG index in background...");
            _store.Index = await _orchestrator.BuildIndexAsync(stoppingToken).ConfigureAwait(false);
            _logger.LogInformation("RAG index ready: {ChunkCount} chunk(s)", _store.Index.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _store.BuildError = ex;
            _logger.LogError(ex, "RAG index build failed.");
        }
        finally
        {
            _store.IsBuilding = false;
        }
    }
}
