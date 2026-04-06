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

## Supported document formats

Only these extensions are indexed (recursive scan under **`Rag:DocumentsPath`**):

| Extension | Role |
| --- | --- |
| **`.md`** | Markdown |
| **`.txt`** | Plain text |

## Operational notes

- **Large corpora**: Everything is **in-memory** (vectors + text). Very large libraries may need a different store; this sample optimizes for clarity.
- **Secrets**: Use user secrets on the `Rag` project (`UserSecretsId` in `.csproj`) for API keys and base address.
- **One-shot CLI**: `dotnet run --project Rag -- "your question"` skips the interactive loop.

## Sample RAG evaluation questions (Spotify dictionary)

Use these to check that retrieval pulls **schema prose** (for example `spotify_data_dictionary.md`), not only play-history rows. Answers that need **URI shape**, **UTC on `ts`**, or **`ms_played` in milliseconds** should fail or go vague if the dictionary chunk is missing from context.

**Prerequisite:** Include a Markdown (or `.txt`) **data dictionary** in `documents/` alongside any optional history export. Remember: **`.csv` is not embedded** by this sample indexer.

### Dictionary analysis, metric, and business questions

Source: `src/Rag/documents/spotify_data_dictionary.md` (indexed when copied into the app `documents/` folder as `.md`).

#### Analysis

The dictionary describes **per-stream playback events** (one row per play). Each row ties a **track** (`spotify_track_uri`, `track_name`, `artist_name`, `album_name`) to **when it ended** (`ts` in UTC), **how long it ran** (`ms_played`), **where it was played** (`platform`), **how it started/ended** (`reason_start`, `reason_end`), and **mode/behavior** (`shuffle`, `skipped`). That supports engagement, channel, and content analytics when aggregating over rows; interpret with care (for example, very short `ms_played` may indicate partial listens; `skipped` marks user-initiated skips).

#### Metric: Total listening time (TLT)

For a chosen scope (date range, user, artist, album, platform, etc.), **TLT** is the **sum of `ms_played`** over all events in that scope. Use **milliseconds** as stored, or convert to minutes/hours for reporting. Optionally refine by filtering events—for example, exclude rows where `skipped` is TRUE if the question is time listened before skipping.

#### Business questions (TLT)

1. **Trend and seasonality** — How does **TLT** change week over week or month over month, and do spikes or drops align with releases, marketing, or product changes (using `ts` to place events in time)?
2. **Platform and product mix** — How is **TLT** split and trending by **`platform`**, and should we prioritize fixes or features on the surfaces that carry the most listening time?
3. **Content and discovery** — Which **`artist_name`** / **`album_name`** (or tracks by URI) drive the most **TLT** in a period, and how does that compare to **`skipped`** and **`shuffle`** to judge depth of engagement versus exploratory listening?

## See also

- [Getting started](getting-started.md) — run commands and secret setup
- [Repository structure](repository-structure.md) — where `documents/` lives in source vs output
