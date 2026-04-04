# RAG sample (`Rag` project)

The **`Rag`** console demonstrates **retrieval-augmented generation** on local text files using the same **`IAiService`** as the Client: **embeddings** for indexing and **chat** for answers.

## Pipeline (high level)

1. **Discover** files: recursive scan under **`Rag:DocumentsPath`** (relative to the application base directory).
2. **Filter** extensions: **`.md`** and **`.txt`** only.
3. **Chunk** each file with **`ChunkSizeChars`** and **`ChunkOverlapChars`**.
4. **Embed** chunks in batches of **`EmbeddingBatchSize`** via **`CreateEmbeddingsAsync`**, using **`Rag:InstanceName`**.
5. **Store** vectors in an in-memory index with cosine similarity search.
6. **Answer**: embed the user question, take **TopK** chunks, build a single user message with labeled context blocks, call **`CompleteChatAsync`** on the **same** instance.

## Configuration

### `Rag` section

| Key | Role |
| --- | --- |
| `DocumentsPath` | Root folder under the build output (default `documents`) |
| `ChunkSizeChars` | Target chunk size |
| `ChunkOverlapChars` | Overlap between consecutive chunks (non-negative, smaller than chunk size) |
| `EmbeddingBatchSize` | Texts per embedding API call |
| `TopK` | How many chunks are concatenated into the prompt context |
| `InstanceName` | Must match `Instances[].Name` — used for **both** embeddings and chat |

### `SystemSettings.AiServiceSettings`

Same shape as the Client. For many providers, chat and embeddings use **different deployment names**; set **`EmbeddingDeployment`** on the instance when needed.

Example `Rag` block (illustrative):

```json
"Rag": {
  "DocumentsPath": "documents",
  "ChunkSizeChars": 600,
  "ChunkOverlapChars": 100,
  "TopK": 4,
  "EmbeddingBatchSize": 16,
  "InstanceName": "AI Architect.Rag.Low"
}
```

## Answering behavior

The orchestrator constructs a user message that:

- Lists retrieved chunks with source file names and separators.
- Instructs the model to use **only** that context.
- Requires admitting **“do not know”** when the context is insufficient.

Adjust system/user instructions in **`RagOrchestrator.AnswerAsync`** if you need stricter citation or different tone.

## Indexing limitations

| Situation | Behavior |
| --- | --- |
| **`.csv` in `documents/`** | **Not** indexed (extension filter). Convert to `.md`/`.txt` or preprocess if you need RAG over tabular exports. |
| Empty or non-text result after chunking | Startup fails with a clear error; add at least one `.md` or `.txt`. |

## Operational notes

- **Large corpora**: Everything is **in-memory** (vectors + text). Very large libraries may need a different store; this sample optimizes for clarity.
- **Secrets**: Use user secrets on the `Rag` project (`UserSecretsId` in `.csproj`) for API keys and base address.
- **One-shot CLI**: `dotnet run --project Rag -- "your question"` skips the interactive loop.

## Sample RAG evaluation questions (Spotify dictionary)

Use these to check that retrieval pulls **schema prose** (for example `spotify_data_dictionary.md`), not only play-history rows. Answers that need **URI shape**, **UTC on `ts`**, or **`ms_played` in milliseconds** should fail or go vague if the dictionary chunk is missing from context.

**Prerequisite:** Include a Markdown (or `.txt`) **data dictionary** in `documents/` alongside any optional history export. Remember: **`.csv` is not embedded** by this sample indexer.

### 1. Schema and coverage

| # | Question |
| --- | --- |
| S1 | Which columns does the Spotify streaming data dictionary define? |
| S2 | List each field in the streaming export and its meaning. |
| S3 | Which field records how long a stream was played, and what unit does it use? |

### 2. Specific fields

| # | Question |
| --- | --- |
| F1 | What does `ts` mean, and in what timezone is it expressed? |
| F2 | How should `spotify_track_uri` be interpreted or formatted? |
| F3 | How do `reason_start` and `reason_end` differ? |
| F4 | What do `shuffle` and `skipped` represent, and which values appear in the dictionary? |
| F5 | Which fields hold the track title, artist, and album? |

### 3. Natural language (same facts, different wording)

| # | Question |
| --- | --- |
| N1 | I need to detect skips—what field should I look at? |
| N2 | Durations look like large integers (e.g. tens of thousands)—are those seconds or another unit? |
| N3 | Which column indicates the client or surface used for playback (e.g. web vs app)? |

### 4. Edge cases and grounding (dictionary-only)

These test whether the model **sticks to the dictionary** and avoids inventing enum semantics.

| # | Question |
| --- | --- |
| E1 | Does the dictionary enumerate meanings for `reason_start` / `reason_end` values such as `clickrow` or `trackdone`, or only describe the fields in general? |
| E2 | The dictionary text says “TRUE of FALSE” for `skipped`—what is that field actually about? |

### Using the set

- Run after **re-indexing** so new or updated `.md` chunks are embedded.
- Compare answers when **only** `spotify_history.csv` (or other non-indexed files) is present versus when **`spotify_data_dictionary.md`** is indexed: correct responses for S3, F1–F2, and N2 should depend on the dictionary chunk.
- Optionally log retrieved **source file names** from the orchestrator to confirm the dictionary appears in **TopK** for these queries.

## See also

- [Getting started](getting-started.md) — run commands and secret setup
- [Repository structure](repository-structure.md) — where `documents/` lives in source vs output
