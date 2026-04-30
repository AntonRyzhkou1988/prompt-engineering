# RAG sample (`Rag` project)

The **`Rag`** console demonstrates **retrieval-augmented generation** on local text files using the same **`IAiService`** as the Client: **embeddings** for indexing and **chat** for answers.

## Where files live (repository root)

| Folder | Role |
| --- | --- |
| **`dataset/`** | Default indexed corpus (`.md`, `.txt`, `.csv`) — **`Rag:DocumentsFolderPath`** + **`Rag:DocumentsPath`** (committed default **`dataset`**). Holds **`space_missions.csv`** and **`attacks.csv`**. |
| **`documents/`** | Optional corpus folder (empty by default); set **`Rag:DocumentsPath`** to **`documents`** if you add files here instead of **`dataset`**. |
| **`questions/`** | Prefilled prompt `.md` files — **`Rag:DocumentsFolderPath`** + **`Rag:QuestionsPath`** |
| **`answers/`** | Saved prefilled/manual outputs — **`Rag:DocumentsFolderPath`** + **`Rag:AnswersPath`** |
| **`metrics/`** | Metric specs and gold CSV for docs / offline scoring — **not** read by Rag unless also under the resolved corpus folder (**`DocumentsFolderPath`** + **`DocumentsPath`**) |

These sit at the **repository root** (next to `src/`, `docs/`, …). On startup, the app prints the resolved corpus directory so you can confirm it points at the intended folder (default **`…/dataset`**).

## Pipeline (high level)

1. **Discover** files: recursive scan under **`Rag:DocumentsFolderPath`** + **`Rag:DocumentsPath`** (resolved with `Path.GetFullPath`; rooted **`DocumentsFolderPath`** ignores `AppContext.BaseDirectory`).
2. **Filter** extensions: **`.md`**, **`.txt`**, and **`.csv`**.
3. **Chunk** each file: **`.md`/`.txt`** use a character window; **`.csv`** is parsed into logical rows (quoted fields, `""` escapes, newlines inside quotes), each data row is formatted as `column: value` lines, then up to **`Rag:Csv:BatchSize`** new rows are packed per chunk (fewer chunks than char-only packing). The CSV text budget per chunk is **`max(ChunkSizeChars, BatchSize × 512)`** characters; overlap is still **whole trailing rows** up to **`ChunkOverlapChars`**.
4. **Embed** chunks in batches of **`EmbeddingBatchSize`** via **`CreateEmbeddingsAsync`**, using **`Rag:InstanceName`**.
5. **Store** vectors in an in-memory index with cosine similarity search.
6. **Answer**: embed the user question, retrieve **`TopK`** chunks with **`SearchTopKWithProseReserve`**: up to **`MinProseChunks`** slots are filled from the **best-matching `.md` / `.txt`** chunks (by cosine similarity), then remaining slots are filled from the global ranking without duplicates. Chunks from **`.csv`** can still appear in the tail of context when they score highly. Build a single user message with labeled context blocks and call **`CompleteChatAsync`** on the **same** instance.

## Configuration

### Path resolution

**`Rag:DocumentsFolderPath`** may be an **absolute** directory or a path **relative to `AppContext.BaseDirectory`** (the folder containing `Rag.dll`, typically `src/Rag/bin/{Configuration}/net8.0/`). The committed default is the absolute repository root **`C:\Work\learn\ai-architect-practice\prompt-engineering`**. **`Rag:DocumentsPath`**, **`Rag:QuestionsPath`**, and **`Rag:AnswersPath`** are **subfolders under that parent** (defaults **`dataset`**, **`questions`**, **`answers`**). The `.csproj` does **not** copy those trees into `bin`; edit **`DocumentsFolderPath`** on other machines or when you clone to a different path, or use a relative climb such as **`../../../../../`** from the output folder instead.

### `Rag` section

| Key | Role |
| --- | --- |
| `DocumentsFolderPath` | Shared parent directory: absolute path or relative to `AppContext.BaseDirectory` (committed default: **`C:\Work\learn\ai-architect-practice\prompt-engineering`**) |
| `DocumentsPath` | Corpus folder name under `DocumentsFolderPath` (default **`dataset`**) |
| `QuestionsPath` | Prefilled question `.md` folder under `DocumentsFolderPath` (default **`questions`**) |
| `AnswersPath` | Answer output folder under `DocumentsFolderPath` (default **`answers`**; created at runtime if missing) |
| `ChunkSizeChars` | Target chunk size for prose and base budget for CSV packing |
| `ChunkOverlapChars` | Overlap between consecutive chunks (non-negative, smaller than chunk size) |
| `EmbeddingBatchSize` | Texts per embedding API call |
| `TopK` | How many chunks are concatenated into the prompt context |
| `MinProseChunks` | Minimum count (capped by `TopK`) of retrieved chunks that must come from **`.md` / `.txt`** sources, by best cosine score among prose—helps pull dictionary or narrative docs even when raw rows dominate similarity |
| `InstanceName` | Must match `Instances[].Name` — used for **both** embeddings and chat |

### `Rag:Csv` (CSV indexing)

| Key | Role |
| --- | --- |
| `Delimiter` | Single character separating fields (default `,`) |
| `Quote` | Single character for quoted fields (default `"`) |
| `HasHeader` | When `true`, the first logical row supplies column names for formatted text |
| `BatchSize` | Max number of **new** data rows per chunk; larger values reduce chunk count and embedding volume |

The CSV reader is a small in-repo parser (no extra packages): delimiter and quote must differ and cannot be newlines. Malformed rows or field counts that do not match the header row throw with file name and row index.

### `SystemSettings.AiServiceSettings`

Same shape as the Client. For many providers, chat and embeddings use **different deployment names**; set **`EmbeddingDeployment`** on the instance when needed.

Example `Rag` block (aligned with current defaults in `appsettings.json`):

```json
"Rag": {
  "DocumentsFolderPath": "C:\\Work\\learn\\ai-architect-practice\\prompt-engineering",
  "DocumentsPath": "dataset",
  "QuestionsPath": "questions",
  "AnswersPath": "answers",
  "ChunkSizeChars": 600,
  "ChunkOverlapChars": 100,
  "TopK": 4,
  "MinProseChunks": 1,
  "EmbeddingBatchSize": 16,
  "InstanceName": "AI Architect.Rag.Low",
  "Csv": {
    "Delimiter": ",",
    "Quote": "\"",
    "HasHeader": true,
    "BatchSize": 100
  }
}
```

## Running the console

- **`dotnet run --project src/Rag/Rag.csproj`** (or `--project Rag` from the solution folder) starts indexing, then prompts:
  - **`[1] Prefilled`** — reads every **`*.md`** in **`DocumentsFolderPath`/`QuestionsPath`** (sorted by file name), runs each full file as the user question, writes one answer per question under **`DocumentsFolderPath`/`AnswersPath`** with YAML front matter (`source`, `generated_utc`).
  - **`[2] Manual`** — read single-line questions from stdin; each answer is saved as `manual_<utc-stamp>.md` with the question embedded in the file.
- **One-shot CLI**: `dotnet run --project src/Rag/Rag.csproj -- "your question"` skips the interactive menu, prints the answer to stdout, and does **not** write an answers file.

## Answering behavior

The orchestrator constructs a user message that:

- Lists retrieved chunks with **1-based indices**, source file names, and separators.
- Instructs the model to use **only** that context, to say it **does not know** when context is insufficient, and to add **bracket citations** (`[1]`, `[2]`, …) for non-obvious factual claims tied to those blocks.
- Uses a system message that forbids inventing policies, numbers, units, or contacts.

Adjust system/user instructions in **`RagOrchestrator.AnswerAsync`** if you need stricter citation or different tone.

## Supported document formats

Only these extensions are indexed (recursive scan under the resolved corpus folder, **`DocumentsFolderPath`** + **`DocumentsPath`**):

| Extension | Role |
| --- | --- |
| **`.md`** | Markdown |
| **`.txt`** | Plain text |
| **`.csv`** | Parsed rows, formatted for retrieval (see **`Rag:Csv`**) |

## Operational notes

- **Large corpora**: Everything is **in-memory** (vectors + text). Very large libraries may need a different store; this sample optimizes for clarity.
- **Large CSV files**: Chunk count scales roughly with row count ÷ **`Csv:BatchSize`** (plus overlap). Raise **`BatchSize`** to reduce embedding calls if your provider allows larger inputs per request.
- **`metrics/`** and **`docs/applications/rag/`**: Metric definitions (`*.md` under `metrics/`, currently **`metrics/answer_correctness_score.md`**) and offline RAG eval gold (**`docs/applications/rag/rag_eval_space_missions_gold.md`**) live here for documentation and checklist scoring. They are **not** indexed unless you also copy or place them under the corpus folder (or point **`DocumentsFolderPath`** / **`DocumentsPath`** at a tree that includes them).
- **Secrets**: Use user secrets on **`Rag`** (same **`SystemSettings:AiServiceSettings`** keys as other executables); see **[Getting started — User secrets](getting-started.md#user-secrets)**.

## Sample corpora and questions

### Space missions (default sample in repo)

The checked-in **`dataset/`** folder includes **`space_missions.csv`** (and **`attacks.csv`**, which is also indexed when **`DocumentsPath`** is **`dataset`**). For answers that need **stable field definitions** (for example **`MissionStatus`** literals or **`Location`** parsing rules), add prose under **`dataset/`** (or **`documents/`**) as well—for example copy **`docs/applications/rag/space_missions_data_dictionary.md`** next to the CSV or into the corpus folder. Otherwise retrieval may return mostly row chunks, and `MinProseChunks` only reserves slots for `.md`/`.txt` that actually exist in the index.

**Human-judged answer quality:** **`metrics/answer_correctness_score.md`** defines **Answer Correctness Score (ACS)** for outputs under **`answers/`**.

**Automated checklist:** substring and mode checks paired with the prefilled questions live in **`docs/applications/rag/rag_eval_space_missions_gold.md`**.

**Example prefilled prompt:** `questions/question_space_missions_extraction_pie.md` — asks for extraction from context plus a **Mermaid pie** chart; run via prefilled mode after indexing.

### Spotify-style playback dictionary (optional pattern)

If you maintain a **Markdown data dictionary** (and optionally a history **`.csv`**) under **`dataset/`** or **`documents/`**, you can ask schema-grounded questions the same way: URI shape, **UTC** on `ts`, **`ms_played`** in milliseconds, **`TLT`** (total listening time = sum of `ms_played` over a scope), and platform or content breakdowns. This is **not** required for the space-missions sample above.

## See also

- [Getting started](getting-started.md) — run commands and secret setup
- [Repository structure](repository-structure.md) — where `dataset/`, `documents/`, `questions/`, `metrics/`, and `answers/` live at the repository root
