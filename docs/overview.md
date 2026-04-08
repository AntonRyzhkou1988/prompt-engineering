# Overview

Documentation index: see the repository **[README.md](../README.md)**.

## What this repository is

A **.NET 8** solution for experimenting with **LLM-backed workflows** in two complementary ways:

| Track | Entry point | Purpose |
| --- | --- | --- |
| **ReAct prompt chain** | `PromptEngineering.Client` | Run a ordered sequence of JSON prompts against **shark attack** rows from `dataset/attacks.csv`. Each step can receive the previous model output as `<prior_run>`. Results are saved as timestamped Markdown under `output/`. |
| **RAG sample** | `Rag` | Chunk **`.md` / `.txt` / `.csv`** under repository-root **`dataset/`** by default (via **`Rag:DocumentsFolderPath`** + **`Rag:DocumentsPath`** = **`dataset`**), embed them, retrieve **top‑K** chunks (with optional **prose reservation** for dictionary-style `.md`/`.txt`), then answer with a **context-only** chat prompt and **[n] citations**. Prefilled questions live in **`questions/`**; answers are written under **`answers/`** (see [RAG](rag.md)). |

Both tracks use the same **`PromptEngineering.LLM`** layer (HTTP to a DIAL- or OpenAI-compatible API).

## Shared platform capabilities

- **Per-call routing**: Each prompt JSON specifies `InstanceName` and `Temperature`; the host picks the matching configured model.
- **Resilience**: Retries with backoff, timeouts, handler lifetime (see `AiServiceSettings` in appsettings).
- **Streaming**: Server-sent events are folded into a single completion object so callers do not branch on stream vs non-stream.
- **Embeddings**: `IAiService.CreateEmbeddingsAsync` supports batch input. Optional **`EmbeddingDeployment`** per instance when the embedding model name differs from the chat **`Deployment`** (typical for RAG).

## Data and safety notes

- **ReAct track**: Evidence is **injected CSV rows** as XML `<record>` elements. Do not treat saved completions as a substitute for structured sections when aggregating runs (see project rules).
- **RAG track**: **`.md`**, **`.txt`**, and **`.csv`** under the resolved corpus folder (**`Rag:DocumentsFolderPath`** + **`Rag:DocumentsPath`**, default **`dataset`**) are indexed (CSV is split into row batches for embedding). Specs and offline eval gold under repository-root **`metrics/`** are not indexed unless you also place them under **`dataset/`** / **`documents/`** (or point **`DocumentsPath`** at a tree that includes them).

## Where to go next

1. [Getting started](getting-started.md) — run the apps locally.
2. [Repository structure](repository-structure.md) — map folders to projects.
3. [Prompt chain](prompt-chain.md) or [RAG](rag.md) — depth for the track you care about.
