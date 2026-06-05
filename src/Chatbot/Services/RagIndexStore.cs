using Rag;

namespace Chatbot.Services;

public sealed class RagIndexStore
{
    public InMemoryVectorStore? Index { get; set; }

    public bool IsBuilding { get; set; }

    public Exception? BuildError { get; set; }

    public bool IsReady => Index is not null;
}
