# Overview

**[Repository README — Documentation](../README.md#documentation)** · [Getting started](getting-started.md) · [Repository structure](repository-structure.md)

## What this repository is

A **.NET 8** solution ([`src/PromptEngineering.sln`](../src/PromptEngineering.sln)) for four complementary tracks:

| Track | Entry point | Purpose |
| --- | --- | --- |
| **ReAct prompt chain** | `PromptEngineering.Client` | Run **an** ordered sequence of JSON prompts against **shark attack** rows from `dataset/attacks.csv`. Each step can receive the previous model output as `<prior_run>`. Results are saved as timestamped Markdown under `output/`. |
| **RAG sample** | `Rag` | Index **one** corpus file (`.md`, `.txt`, or `.csv`) resolved from **`Rag:DocumentsFolderPath`** + **`Rag:DatasetPath`** (committed default: **`dataset/space_missions.csv`**), embed chunks, retrieve **top‑K**, answer with **context-only** instructions and **`[n]` citations**. Prefilled questions live in **`questions/`**; answers under **`answers/`** (see [RAG guide](applications/rag/rag.md)). |
| **Agent (MCP tools)** | `Agent` | Interactive chat with **tool calling**: weather (Open-Meteo MCP) and news / web search (DuckDuckGo MCP). Configure stdio MCP sessions in **`src/Agent/appsettings.json`**; requires **Node.js** and **`npx`**. |
| **Security samples** | `Security` | Console **chat-only** demos: **prompt injection** and **sensitive information disclosure**, each shown **without** then **with** mitigations ([Security samples](applications/security/security-samples.md)). Uses **`Security:InstanceName`**; no MCP. |

All four tracks use **`PromptEngineering.LLM`** (HTTP to a DIAL- or OpenAI-compatible API).

## Shared platform capabilities

- **Per-call routing**: **`PromptEngineering.Client`** reads **`InstanceName`** and **`Temperature`** from each prompt JSON; **`Rag`**, **`Agent`**, and **`Security`** resolve **`Rag:InstanceName`**, **`Agent:InstanceName`**, or **`Security:InstanceName`** against the same **`Instances`** list in **`appsettings.json`** / user secrets.
- **Resilience**: Retries with backoff, timeouts, handler lifetime (see **`AiServiceSettings`** in appsettings).
- **Streaming**: Server-sent events are folded into a single completion object so callers do not branch on stream vs non-stream.
- **Embeddings**: `IAiService.CreateEmbeddingsAsync` supports batch input. Optional **`EmbeddingDeployment`** per instance when the embedding model differs from chat **`Deployment`** (typical for RAG).

## Data and safety notes

- **ReAct track**: Evidence is **injected CSV rows** as XML `<record>` elements inside `<data>…</data>`. When comparing runs, do not treat ad-hoc aggregates as a substitute for the model’s structured sections (see [.cursor/rules/project-rules.mdc](../.cursor/rules/project-rules.mdc)).
- **RAG track**: Only the **`Rag:DatasetPath`** **file** is indexed. **`metrics/`** and **`docs/applications/rag/`** are not in the vector store unless their text is inside that file or you switch **`DatasetPath`** to a file that contains them.
- **Agent track**: Answers depend on **live tool responses**. Treat tool traces as runtime evidence; do not fabricate tool output in prompts (same rules file as above).
- **Security track**: Scenarios are **synthetic** (fake instance name, fake CRM XML). Outputs are **console logs**—treat as teaching aids, not production controls. See [Security samples](applications/security/security-samples.md) and [`risk-assessment/`](../risk-assessment/).

## Application-specific docs

- **Prompt engineering (Client):** [Prompt chain](applications/prompt-engineering/prompt-chain.md)
- **RAG:** [RAG guide](applications/rag/rag.md), [data dictionary](applications/rag/space_missions_data_dictionary.md), [eval gold](applications/rag/rag_eval_space_missions_gold.md)
- **Agent:** [TRA benchmark](applications/agent/agent-weather-news.md) with [Tool Routing Accuracy](../metrics/agent_tool_routing_accuracy.md)
- **Security:** [Security samples](applications/security/security-samples.md); narratives under [`risk-assessment/`](../risk-assessment/)

## Where to go next

1. [Getting started](getting-started.md) — run the apps locally.
2. [Repository structure](repository-structure.md) — map folders to projects.
3. [Prompt chain](applications/prompt-engineering/prompt-chain.md) · [RAG guide](applications/rag/rag.md) · for **Agent**: [README — Agent](../README.md#agent-mcp-tools), [`src/Agent/appsettings.json`](../src/Agent/appsettings.json), [agent benchmark](applications/agent/agent-weather-news.md) · for **Security**: [README — Security samples](../README.md#4-security-samples), [Security samples](applications/security/security-samples.md).
