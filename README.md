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

- **Layout**: **`dataset/`** (default RAG corpus, includes **`space_missions.csv`** next to **`attacks.csv`**), **`documents/`** (optional extra corpus), **`questions/`**, **`answers/`**, and **`metrics/`** (reference / eval) live at or under the **repository root**. The **`Rag`** project under **`src/Rag/`** only hosts code and `appsettings.json`; it does not own those folders.
- **Index**: `.md`, `.txt`, and `.csv` under **`Rag:DocumentsFolderPath`** + **`Rag:DocumentsPath`** (committed default: repo root + **`dataset`**) are **chunked** (CSV as row batches), **embedded** in batches, and stored in an **in-memory** vector index. With the default, **`attacks.csv`** is indexed too; point **`DocumentsPath`** at **`documents`** if you want a separate corpus folder. The `.csproj` does **not** copy data into `bin`.
- **Query**: the question is embedded; **top‑K** chunks are selected by **cosine similarity**, with **`MinProseChunks`** reserving slots for the best‑matching dictionary-style `.md`/`.txt` chunks when configured. Answers use **context-only** instructions and **[n] citations** to context blocks.
- **Batch Q&A**: prefilled prompts in **`questions/`** can be run from the console; answers are written under **`answers/`** (see **[docs/rag.md](docs/rag.md)**).
- **Configuration**: chunk size, overlap, `TopK`, `MinProseChunks`, `Csv:BatchSize`, batch size, and which **instance** performs embeddings and chat (see `src/Rag/appsettings.json`).

Specs and eval gold under **`metrics/`** are for documentation and offline scoring; copy anything you need retrieved into **`dataset/`** or **`documents/`** (or adjust **`DocumentsFolderPath`** / **`DocumentsPath`**). Full behavior: **[docs/rag.md](docs/rag.md)**.

---

## Shared LLM layer

**`PromptEngineering.LLM`** provides **chat** completions, optional **SSE** streaming (aggregated to one completion object), **Polly** retries, timeouts, and **embeddings** (`CreateEmbeddingsAsync`), including optional **`EmbeddingDeployment`** when embedding and chat models differ.

---

## Documentation

Hands-on guides for this repository. Start with **[Overview](docs/overview.md)**, then **[Getting started](docs/getting-started.md)**.

| Guide | Topics |
| --- | --- |
| [Overview](docs/overview.md) | What the solution does, two tracks (ReAct vs RAG), shared LLM capabilities |
| [Getting started](docs/getting-started.md) | Prerequisites, configuration, user secrets, run commands for Client and Rag |
| [Repository structure](docs/repository-structure.md) | Folders, projects, where data and outputs live (includes diagram) |
| [Prompt chain (shark / ReAct)](docs/prompt-chain.md) | End-to-end ReAct flow, JSON prompt format, prompt versions, research question, output schema, reasoning cycle, quality bar |
| [RAG sample](docs/rag.md) | Indexing rules (including CSV rows), `Rag` settings, prose reservation, prefilled questions, retrieval and answering with citations |

Related: [Project rules for prompt authoring](.cursor/rules/project-rules.mdc) (ReAct standard used when editing prompts).

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
