# Getting started

**[Repository README — Documentation](../README.md#documentation)** · [Overview](overview.md) · [Repository structure](repository-structure.md)

## Prerequisites

| Requirement | Client | Rag | Agent |
| --- | --- | --- | --- |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer | Yes | Yes | Yes |
| Network + OpenAI-compatible API (e.g. EPAM DIAL) | Yes | Yes | Yes |
| API keys via **user secrets** (not committed) | Yes | Yes | Yes |
| **Node.js** + **`npx`** on `PATH` (MCP servers) | No | No | Yes |

## User secrets

Never commit **`SystemSettings:AiServiceSettings:BaseAddress`** or **`Instances[*]:ApiKey`**. Each **executable** has its own [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) store, keyed By **`UserSecretsId`** in that project’s `.csproj`.

Overrides use the **same** subtree everywhere: **`SystemSettings:AiServiceSettings`** (same keys as **`appsettings.json`**).

### Executable projects

| Project | `--project` (from repository root) | `UserSecretsId` |
| --- | --- | --- |
| **PromptEngineering.Client** | `src/PromptEngineering.Client/PromptEngineering.Client.csproj` | `PromptEngineering.Client` |
| **Rag** | `src/Rag/Rag.csproj` | `Rag.Demo` |
| **Agent** | `src/Agent/Agent.csproj` | `154595eb-806c-479f-a229-4d363d9b9730` |

Use **`dotnet user-secrets`** with **`--project`** so commands work from the repository root (adjust paths if your clone location differs):

```powershell
# PromptEngineering.Client — set BaseAddress and one ApiKey per instance index your prompts reference.
dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "your-key" `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:1:ApiKey" "your-key" `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:2:ApiKey" "your-key" `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
```

```powershell
# Rag — match Instances[n]:ApiKey to Rag:InstanceName in appsettings.json.
dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/Rag/Rag.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "your-key" `
  --project src/Rag/Rag.csproj
```

```powershell
# Agent — match Instances[n]:ApiKey to Agent:InstanceName in appsettings.json.
dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/Agent/Agent.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "your-key" `
  --project src/Agent/Agent.csproj
```

You may **`cd src/<Project>`** and run **`dotnet user-secrets set`** without **`--project`**; the store is always scoped to that **`.csproj`**.

Configuration load order: **`appsettings.json`**, then **user secrets** when present (same for Client, Rag, Agent).

## Run: `PromptEngineering.Client` (ReAct chain)

1. Edit **`src/PromptEngineering.Client/appsettings.json`**:
   - **`ContextSettings.PromptPath`** — folder with **`initial.json`**, **`v1.json`**, …
   - **`ContextSettings.DatasetPath`** — e.g. **`dataset/attacks.csv`**
   - **`ContextSettings.OutputDirectory`** — timestamped completions
   - **`SystemSettings.MaximumDatasetRecordCount`** — row cap
2. Ensure **`Instances[].Name`** values match each prompt’s **`InstanceName`**.

```powershell
cd src
dotnet run --project PromptEngineering.Client
```

The committed Client config lists **three** instances. **`EmbeddingsUrl`** is optional in appsettings. Add **`EmbeddingDeployment`** only when embeddings use a different deployment than chat.

## Run: `Rag`

**`Rag`** indexes **one file** given by **`Rag:DatasetPath`** (relative to **`Rag:DocumentsFolderPath`** unless rooted). The committed default is **`dataset/space_missions.csv`**. Questions and answers resolve under **`DocumentsFolderPath`** + **`QuestionsPath`** / **`AnswersPath`**. Nothing is copied into **`bin`**; on a new machine, update **`DocumentsFolderPath`** or use a path relative to the **`Rag`** output directory.

- **`metrics/`** · **`docs/applications/rag/`** — not indexed unless that text lives inside your **`DatasetPath`** file.

1. Edit **`src/Rag/appsettings.json`**:
   - **`Rag.InstanceName`** must match an **`Instances[].Name`**
   - Tune **`DocumentsFolderPath`**, **`DatasetPath`**, **`QuestionsPath`**, **`AnswersPath`**, chunking, **`TopK`**, **`MinProseChunks`**, **`Csv:BatchSize`**, **`EmbeddingBatchSize`**
2. If chat and embeddings use different model names, set **`EmbeddingDeployment`** on the instance.

```powershell
cd src
dotnet run --project Rag
```

After indexing:

- **`[1] Prefilled`** — all **`*.md`** in **`QuestionsPath`** → answers in **`AnswersPath`** (YAML front matter).
- **`[2] Manual`** — stdin questions; **empty line** exits.

One-shot (stdout only; no **`answers/`** file):

```powershell
dotnet run --project Rag -- "What MissionStatus values appear in the space missions context?"
```

Full behavior: **[RAG guide](applications/rag/rag.md)**.

## Run: `Agent` (MCP)

Requires **Node.js** and **`npx`**. Configure secrets like **Rag** (see above).

```powershell
cd src
dotnet run --project Agent -- "What is the weather and the latest news in Paris?"
```

MCP sessions, **`Agent:InstanceName`**, and the TRA benchmark: **[README — Agent](../README.md#agent-mcp-tools)**, **[applications/agent/agent-weather-news.md](applications/agent/agent-weather-news.md)**, **[metrics/agent_tool_routing_accuracy.md](../metrics/agent_tool_routing_accuracy.md)**.

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
| `DocumentsFolderPath` | Root for relative **`DatasetPath`**, **`QuestionsPath`**, **`AnswersPath`** |
| `DatasetPath` | **Single** corpus file (`.md` / `.txt` / `.csv`) |
| `QuestionsPath` | Prefilled **`.md`** folder |
| `AnswersPath` | Answer output folder |
| `ChunkSizeChars` / `ChunkOverlapChars` | Chunking (see [RAG guide](applications/rag/rag.md)) |
| `Csv` | Delimiter, quote, header, **`BatchSize`** |
| `EmbeddingBatchSize` | Batch size for `CreateEmbeddingsAsync` |
| `TopK` | Chunks in the answer prompt |
| `MinProseChunks` | Reserved slots for best **`.md`/`.txt`** matches |
| `InstanceName` | Instance used for **both** embeddings and chat |

See **[RAG guide](applications/rag/rag.md)** for the full pipeline and notes.
