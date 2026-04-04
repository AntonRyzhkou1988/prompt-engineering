# Prompt Engineering Practice

![.NET](https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet)
![LLM](https://img.shields.io/badge/LLM-GPT--4o%20%7C%20Claude%20%7C%20Gemini-10a37f)
![Data](https://img.shields.io/badge/Data-attacks.csv-blue)

**.NET 8** solution for experimenting with **prompt engineering** (structured, versioned prompts and multi-step refinement) and **RAG** (retrieval-augmented answers over your own documents).

---

## Prompt engineering (in this repo)

**Prompt engineering** here means designing **explicit roles, tasks, constraints, and output shapes** so model outputs stay grounded, comparable across runs, and easy to review.

This track is implemented by **`PromptEngineering.Client`**:

- Prompts are **JSON files** (`prompts/`) with system/user text, temperature, and **per-prompt model routing** (`InstanceName`).
- **Shark attack** rows from `dataset/attacks.csv` are injected as XML `<record>` elements where the template has `<data></data>`.
- Runs follow a **ReAct-style refinement chain**: each completion can be passed into the next file as **`<prior_run>`**, so later prompts build on earlier analysis instead of starting cold.
- Completions are written as timestamped Markdown under `output/` for diffing and scoring.

For the full flow, schema (Sections A–D), and quality checklist, see **[docs/prompt-chain.md](docs/prompt-chain.md)** and **[project-rules.mdc](.cursor/rules/project-rules.mdc)**.

---

## RAG (in this repo)

**RAG (retrieval-augmented generation)** means: turn your documents into **vector embeddings**, **retrieve** the pieces most similar to a user question, and **generate** an answer using only (or primarily) that retrieved text as context—reducing reliance on the model’s parametric memory.

This track is implemented by **`Rag`**:

- **Index**: `.md` and `.txt` under `src/Rag/documents` (copied to the build output) are **chunked**, **embedded** in batches, and stored in an **in-memory** vector index.
- **Query**: the question is embedded; **top‑K** chunks are selected by **cosine similarity** and passed into a **chat** completion with instructions to stay grounded in that context.
- **Configuration**: chunk size, overlap, `TopK`, batch size, and which configured **instance** performs embeddings and chat (see `appsettings.json`).

CSV files are **not** indexed as-is; put tabular or dictionary content in Markdown or text if you want it retrieved. Details: **[docs/rag.md](docs/rag.md)**.

---

## Shared LLM layer

**`PromptEngineering.LLM`** provides **chat** completions, optional **SSE** streaming (aggregated to one completion object), **Polly** retries, timeouts, and **embeddings** (`CreateEmbeddingsAsync`), including optional **`EmbeddingDeployment`** when embedding and chat models differ.

---

## Documentation

| Doc | Topics |
| --- | --- |
| [docs/README.md](docs/README.md) | Index of all guides |
| [docs/overview.md](docs/overview.md) | Big-picture summary and capabilities |
| [docs/getting-started.md](docs/getting-started.md) | Prerequisites, secrets, run commands |
| [docs/repository-structure.md](docs/repository-structure.md) | Folders, projects, diagram |
| [docs/prompt-chain.md](docs/prompt-chain.md) | ReAct sequence, JSON/XML, versions, Sections A–D, quality bar |
| [docs/rag.md](docs/rag.md) | RAG pipeline, settings, limitations |

---

## Quick start

```powershell
cd src
dotnet run --project PromptEngineering.Client
```

```powershell
cd src
dotnet run --project Rag
```

Configure `appsettings.json` and set API keys via **user secrets** ([Getting started](docs/getting-started.md)).
