using System.Collections.Concurrent;
using PromptEngineering.Model;

namespace PromptEngineering.Services;

public sealed class ContextService : IContextService
{
    private readonly ConcurrentDictionary<string, Context> _contexts = new(StringComparer.OrdinalIgnoreCase);

    public Task<IReadOnlyCollection<Context>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<Context>>(_contexts.Values.ToArray());
    }

    public Task<Context?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _contexts.TryGetValue(key, out var context);
        return Task.FromResult(context);
    }

    public Task<Context> UpsertAsync(Context context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(context.Key);

        var storedContext = _contexts.AddOrUpdate(context.Key, context, (_, _) => context);
        return Task.FromResult(storedContext);
    }

    public Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return Task.FromResult(_contexts.TryRemove(key, out _));
    }
}
