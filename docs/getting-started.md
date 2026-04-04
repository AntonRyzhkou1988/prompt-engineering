# Getting started

## Prerequisites

- **.NET 8** SDK or newer
- Network access to an **EPAM DIAL** deployment or another **OpenAI-compatible** HTTP API
- API keys (stored in **user secrets**, not committed)

## Secrets (both apps)

Never commit `BaseAddress` or `ApiKey`. Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for the `SystemSettings:AiServiceSettings` subtree.

Example (adjust indices to match your `Instances` array):

```powershell
# Client
cd src/PromptEngineering.Client
dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..."
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "your-key"
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:1:ApiKey" "your-key"
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:2:ApiKey" "your-key"
```

```powershell
# Rag (match instance index to Rag:InstanceName)
cd src/Rag
dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..."
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "your-key"
```

## Run: `PromptEngineering.Client` (ReAct chain)

1. Edit `src/PromptEngineering.Client/appsettings.json`:
   - `ContextSettings.PromptPath` — folder containing `initial.json`, `v1.json`, …
   - `ContextSettings.DatasetPath` — path to `dataset/attacks.csv`
   - `ContextSettings.OutputDirectory` — where completions are written
   - `SystemSettings.MaximumDatasetRecordCount` — cap on loaded rows
2. Ensure `SystemSettings.AiServiceSettings.Instances` includes the `Name` values referenced by your prompt JSON files.

```powershell
cd src
dotnet run --project PromptEngineering.Client
```

The committed Client config lists **three** instances (Low / Medium / High). **`EmbeddingsUrl`** is optional and defaults to `embeddings` in code if omitted. Add **`EmbeddingDeployment`** on an instance only if embeddings use a different deployment than chat.

## Run: `Rag`

1. Edit `src/Rag/appsettings.json`:
   - `Rag.InstanceName` must equal one of `SystemSettings.AiServiceSettings.Instances[].Name`
   - Tune `DocumentsPath`, chunk sizes, `TopK`, and `EmbeddingBatchSize` as needed
2. If your provider uses a **separate** embedding model, set **`EmbeddingDeployment`** on that instance (see committed Rag `appsettings.json`).

```powershell
cd src
dotnet run --project Rag
```

Interactive mode: type questions; **empty line** exits.

One-shot question:

```powershell
dotnet run --project Rag -- "What does the ts field mean in the Spotify export?"
```

## Configuration reference (abbreviated)

### Client: `ContextSettings`

| Key | Role |
| --- | --- |
| `PromptPath` | Directory of prompt `.json` files |
| `DatasetPath` | `attacks.csv` (or compatible path) |
| `OutputDirectory` | Timestamped `completion_*.md` files |
| `ReActSequence` | Ordered list of prompt filenames |

### Rag: `Rag` section

| Key | Role |
| --- | --- |
| `DocumentsPath` | Folder under the app base directory to scan for `.md` / `.txt` |
| `ChunkSizeChars` / `ChunkOverlapChars` | Chunking for embedding |
| `EmbeddingBatchSize` | Batch size for `CreateEmbeddingsAsync` |
| `TopK` | Chunks injected into the answer prompt |
| `InstanceName` | Instance used for **both** embeddings and chat completion |

See [RAG sample](rag.md) for behavior details.
