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

Corpus layout (defaults assume you run from a normal `dotnet build` of **`Rag`**, so `AppContext.BaseDirectory` is `src/Rag/bin/{Configuration}/net8.0/`):

- **`dataset/`** (default **`Rag:DocumentsPath`**), **`questions/`**, **`answers/`** — **`space_missions.csv`** lives under **`dataset/`** with **`attacks.csv`**. `appsettings.json` sets **`Rag:DocumentsFolderPath`** to the absolute repository path (committed default: **`C:\Work\learn\ai-architect-practice\prompt-engineering`**) and **`Rag:DocumentsPath`** to **`dataset`**; **`questions`** and **`answers`** name the other subfolders. Nothing is copied into `bin`. You can use a **relative** **`DocumentsFolderPath`** if you prefer (for example **`../../../../../`** from `bin/.../net8.0/`).
- **`documents/`** — optional extra corpus folder (empty by default); set **`Rag:DocumentsPath`** to **`documents`** if you add files there instead of **`dataset`**.
- **`metrics/`** — also at the **repository root**; used for specs and offline eval CSV. It is **not** indexed unless you copy files into **`dataset/`** / **`documents/`** or point **`DocumentsPath`** at a tree that includes them.

1. Edit `src/Rag/appsettings.json`:
   - `Rag.InstanceName` must equal one of `SystemSettings.AiServiceSettings.Instances[].Name`
   - Tune `DocumentsFolderPath`, `DocumentsPath`, `QuestionsPath`, `AnswersPath`, chunk sizes, `TopK`, `MinProseChunks`, `Csv:BatchSize`, and `EmbeddingBatchSize` as needed
2. If your provider uses a **separate** embedding model, set **`EmbeddingDeployment`** on that instance (see committed Rag `appsettings.json`).

```powershell
cd src
dotnet run --project Rag
```

After indexing, the console offers:

- **`[1] Prefilled`** — runs every **`*.md`** in **`Rag:QuestionsPath`** and saves answers under **`Rag:AnswersPath`** (YAML front matter + body).
- **`[2] Manual`** — type one question per line; **empty line** exits.

One-shot question (prints the answer only; does not write **`answers/`**):

```powershell
dotnet run --project Rag -- "What MissionStatus values appear in the space missions context?"
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
| `DocumentsFolderPath` | Shared parent for corpus/questions/answers: **absolute** path or relative to `AppContext.BaseDirectory` (committed default: **`C:\Work\learn\ai-architect-practice\prompt-engineering`**) |
| `DocumentsPath` | Corpus subfolder under `DocumentsFolderPath` (default **`dataset`** — indexes **`space_missions.csv`** and **`attacks.csv`**) |
| `QuestionsPath` | Prefilled question `.md` subfolder (default **`questions`**) |
| `AnswersPath` | Answer output subfolder (default **`answers`**) |
| `ChunkSizeChars` / `ChunkOverlapChars` | Chunking for prose; CSV chunks use a larger effective budget (see [RAG](rag.md)) |
| `Csv` | Delimiter, quote, header, **`BatchSize`** (rows per CSV chunk) |
| `EmbeddingBatchSize` | Batch size for `CreateEmbeddingsAsync` |
| `TopK` | Chunks injected into the answer prompt |
| `MinProseChunks` | Reserve up to this many slots for best `.md`/`.txt` matches before filling from CSV-heavy results |
| `InstanceName` | Instance used for **both** embeddings and chat completion |

See [RAG sample](rag.md) for behavior details.
