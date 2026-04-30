# RAG sample (`Rag` project)

The **`Rag`** console demonstrates **retrieval-augmented generation**: one **corpus file** is chunked, embedded into an **in-memory** vector index, and used as the **only** context for chat answers via the same **`IAiService`** as the other samples.

## Corpus and paths (repository root)

| Concept | Role |
| --- | --- |
| **`Rag:DocumentsFolderPath`** | Shared parent for resolving **`DatasetPath`**, **`QuestionsPath`**, and **`AnswersPath`**. Committed default: absolute **repository root**; may instead be relative to `AppContext.BaseDirectory` (the folder containing `Rag.dll`). |
| **`Rag:DatasetPath`** | **Single indexed file** (`.md`, `.txt`, or `.csv`), either absolute or **relative to `DocumentsFolderPath`**. Committed default: **`dataset/space_missions.csv`** — only that file is embedded; **`dataset/attacks.csv`** is **not** in the index until you point **`DatasetPath`** at it or another file. |
| **`Rag:QuestionsPath`** | Subfolder under **`DocumentsFolderPath`** with prefilled **`*.md`** questions (default **`questions`**). |
| **`Rag:AnswersPath`** | Subfolder under **`DocumentsFolderPath`** for saved answers (default **`answers`**). |
| **`dataset/`**, **`documents/`** | Repo folders that typically hold CSV / prose corpora; choose one **file** via **`DatasetPath`**. |
| **`metrics/`**, **`docs/applications/rag/`** | Scoring specs and eval tables — **not** loaded by Rag unless you copy them into a file you set as **`DatasetPath`**. |

On startup, the app logs the resolved **dataset file** path so you can confirm indexing targets the intended file.

## Pipeline (high level)

1. **Resolve** **`Rag:DatasetPath`** to an absolute path (combine with **`DocumentsFolderPath`** when the value is not rooted).
2. **Load** that **one** file. If it is **`.csv`**, parse rows (quoted fields, `""` escapes, newlines inside quotes), format rows as `column: value` text, and pack up to **`Rag:Csv:BatchSize`** data rows per chunk. Prose budget per CSV chunk is **`max(ChunkSizeChars, BatchSize × 512)`** (512 is an internal rows-to-chars estimate); overlap uses **whole trailing rows** within **`ChunkOverlapChars`**. If it is **`.md`** or **`.txt`**, read text and split with **`ChunkSizeChars`** / **`ChunkOverlapChars`**.
3. **Embed** chunks in batches of **`EmbeddingBatchSize`** with **`CreateEmbeddingsAsync`**, using **`Rag:InstanceName`**.
4. **Store** vectors in memory; retrieve with cosine similarity.
5. **Answer**: embed the question, take **`TopK`** chunks via **`SearchTopKWithProseReserve`** (reserve up to **`MinProseChunks`** slots for the best **`.md`/`.txt`** chunks when those exist in the index), then **`CompleteChatAsync`** on the **same** instance with context-only instructions and **[n] citations**.

> **Note:** `RagSettings` describes a possible future **directory** corpus; the current **`RagOrchestrator.BuildIndexAsync`** indexes **one file only**.

## Configuration

### `Rag` section (see `src/Rag/RagSettings.cs` and `appsettings.json`)

| Key | Role |
| --- | --- |
| `DocumentsFolderPath` | Content root for relative **`DatasetPath`**, **`QuestionsPath`**, **`AnswersPath`**. |
| `DatasetPath` | **One** corpus file (`.md` / `.txt` / `.csv`), relative to `DocumentsFolderPath` or absolute. |
| `QuestionsPath` | Prefilled question folder under `DocumentsFolderPath`. |
| `AnswersPath` | Answer output folder under `DocumentsFolderPath`. |
| `ChunkSizeChars` | Prose chunk size; base budget for CSV chunk sizing. |
| `ChunkOverlapChars` | Overlap for prose chunks; CSV overlap is row-based within this budget. |
| `EmbeddingBatchSize` | Texts per embedding API request. |
| `TopK` | Chunks passed into the answer prompt. |
| `MinProseChunks` | Prefer up to this many **`.md`/`.txt`** chunks in the top‑K when available. |
| `InstanceName` | Must match `SystemSettings.AiServiceSettings.Instances[].Name` for **both** embeddings and chat. |

### `Rag:Csv`

| Key | Role |
| --- | --- |
| `Delimiter` | Field separator (default `,`). |
| `Quote` | Quote character (default `"`). |
| `HasHeader` | First row supplies column names for formatted chunks. |
| `BatchSize` | Max **new** data rows per CSV chunk. |

### `SystemSettings.AiServiceSettings`

Same shape as **PromptEngineering.Client**. Set **`EmbeddingDeployment`** on an instance when chat and embedding models differ.

Example fragment (trimmed from committed `src/Rag/appsettings.json`; adjust paths and **`InstanceName`** on your machine):

```json
"Rag": {
  "DocumentsFolderPath": "C:\\Work\\learn\\ai-architect-practice\\prompt-engineering",
  "DatasetPath": "dataset/space_missions.csv",
  "QuestionsPath": "questions",
  "AnswersPath": "answers",
  "ChunkSizeChars": 600,
  "ChunkOverlapChars": 100,
  "TopK": 4,
  "MinProseChunks": 1,
  "EmbeddingBatchSize": 16,
  "InstanceName": "AIArchitect.Rag.High",
  "Csv": {
    "Delimiter": ",",
    "Quote": "\"",
    "HasHeader": true,
    "BatchSize": 50
  }
}
```

## Running the console

- **`dotnet run --project src/Rag/Rag.csproj`** (from repo root) or **`cd src`**, then **`dotnet run --project Rag`**, builds the index, then offers:
  - **`[1] Prefilled`** — every **`*.md`** under **`QuestionsPath`**, answers written under **`AnswersPath`** (YAML front matter: `source`, `generated_utc`).
  - **`[2] Manual`** — stdin questions; saves **`manual_<utc>.md`** under **`AnswersPath`**.
- **One-shot:** `dotnet run --project src/Rag/Rag.csproj -- "Your question"` prints the answer only (no file under **`answers/`**).

## Answering behavior

`RagOrchestrator.AnswerAsync` builds context blocks labeled **`[1]`**, **`[2]`**, … and instructs the model to stay within that context, cite non-obvious claims with **`[n]`**, and admit ignorance when context is insufficient. Adjust prompts in code if you need stricter behavior.

## Supported corpus formats

The **indexed file** must end with **`.md`**, **`.txt`**, or **`.csv`**.

## Operational notes

- **Scope**: One file → one retrieval universe. To experiment on **`attacks.csv`**, set **`DatasetPath`** to `dataset/attacks.csv` (or an absolute path). There is no multi-file index in the current orchestrator.
- **Memory**: Vectors and text stay **in memory**; huge files may need a different backend.
- **Docs under `metrics/` and `docs/applications/rag/`**: Useful for humans and offline scoring; they are **not** in the index unless their content is inside the **`DatasetPath`** file or you switch to a file that includes them.

## Sample corpora and questions

### Space missions

The repo ships **`dataset/space_missions.csv`**. For stable field meanings (**`MissionStatus`**, **`Location`** parsing), keep or copy **[`space_missions_data_dictionary.md`](space_missions_data_dictionary.md)** beside the CSV or merge dictionary prose into a **`.md`** you index alongside (same folder pattern), so **`MinProseChunks`** can retrieve prose and not only row batches.

- **Human-judged quality:** [`metrics/answer_correctness_score.md`](../../../metrics/answer_correctness_score.md) (outputs in **`answers/`**).
- **Automated check table:** [`rag_eval_space_missions_gold.md`](rag_eval_space_missions_gold.md).

### Optional pattern (other domains)

You can point **`DatasetPath`** at any single large **`.csv`** or narrative **`.md`** under **`dataset/`** or **`documents/`** and author matching questions under **`questions/`**.

## See also

- [Repository README — Documentation](../../../README.md#documentation)
- [Overview](../../overview.md) · [Getting started](../../getting-started.md) · [Repository structure](../../repository-structure.md)
- [Prompt chain](../prompt-engineering/prompt-chain.md) — contrasts **injected** `<record>` rows (Client) with **retrieved** chunks (Rag)
