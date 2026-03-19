using PromptEngineering.Model;

namespace PromptEngineering.Services;

public interface IContextService
{
    Task<IReadOnlyCollection<Context>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Context?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<Context> UpsertAsync(Context context, CancellationToken cancellationToken = default);

    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
}
