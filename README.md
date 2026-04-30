# Prompt Engineering Practice

![.NET](https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet)
![LLM](https://img.shields.io/badge/LLM-GPT--4o%20%7C%20Claude%20%7C%20Gemini-10a37f)
![Data](https://img.shields.io/badge/Data-attacks.csv-blue)
![MCP](https://img.shields.io/badge/MCP-stdio-6366f1)

**.NET 8** solution for experimenting with three complementary samples: **prompt engineering** (structured, versioned prompts and multi-step refinement via **`PromptEngineering.Client`**), **RAG** (retrieval-augmented answers over your own documents via **`Rag`**), and a **tool-using agent** (**`Agent`**)—chat completions plus MCP-hosted weather and news tools.

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

- **Layout**: **`dataset/`** (default RAG corpus, includes **`space_missions.csv`** next to **`attacks.csv`**), **`documents/`** (optional extra corpus), **`questions/`**, **`answers/`**, and **`metrics/`** (offline scoring specs for RAG and Agent—see **Documentation** below) live at or under the **repository root**. The **`Rag`** project under **`src/Rag/`** only hosts code and `appsettings.json`; it does not own those folders.
- **Index**: `.md`, `.txt`, and `.csv` under **`Rag:DocumentsFolderPath`** + **`Rag:DocumentsPath`** (committed default: repo root + **`dataset`**) are **chunked** (CSV as row batches), **embedded** in batches, and stored in an **in-memory** vector index. With the default, **`attacks.csv`** is indexed too; point **`DocumentsPath`** at **`documents`** if you want a separate corpus folder. The `.csproj` does **not** copy data into `bin`.
- **Query**: the question is embedded; **top‑K** chunks are selected by **cosine similarity**, with **`MinProseChunks`** reserving slots for the best‑matching dictionary-style `.md`/`.txt` chunks when configured. Answers use **context-only** instructions and **[n] citations** to context blocks.
- **Batch Q&A**: prefilled prompts in **`questions/`** can be run from the console; answers are written under **`answers/`** (see **[docs/rag.md](docs/rag.md)**).
- **Configuration**: chunk size, overlap, `TopK`, `MinProseChunks`, `Csv:BatchSize`, batch size, and which **instance** performs embeddings and chat (see `src/Rag/appsettings.json`).

Specs under **`metrics/`** (for example RAG **`answer_correctness_score.md`**) and eval gold under **`docs/applications/rag/`** are for documentation and offline scoring; copy anything you need retrieved into **`dataset/`** or **`documents/`** (or adjust **`DocumentsFolderPath`** / **`DocumentsPath`**). Full behavior: **[docs/rag.md](docs/rag.md)**.

---

## Agent (MCP tools)

**Agent** runs **interactive Q&A**: the model may call **weather** tools (Open-Meteo MCP) and **news / web search** tools (DuckDuckGo MCP). The host prints the final answer and the **tools invoked** list.

This track is implemented by **`Agent`** (`src/Agent/`):

- **Runtime**: **`PromptEngineering.LLM`** (`IAiService`) chat completions with **function / tool calling**; tool schemas are loaded from MCP **stdio** sessions configured in **`Agent:OpenMeteo`** and **`Agent:DuckDuckGo`** in [`src/Agent/appsettings.json`](src/Agent/appsettings.json).
- **Prerequisites**: **Node.js** with **`npx`** on `PATH` (committed defaults spawn MCP servers via `npx`).
- **Configuration**: **`SystemSettings:AiServiceSettings`** (same pattern as Client/Rag); **`Agent:InstanceName`**, **`Temperature`**, **`MaxFunctionIterations`**; optional **`Agent:ToolRouting`** maps invoked tool names to domains for the **[Tool Routing Accuracy](metrics/agent_tool_routing_accuracy.md)** metric.
- **Secrets**: Same **`SystemSettings:AiServiceSettings`** pattern as Client/Rag—step-by-step commands under **[How to run the samples](#how-to-run-the-samples)**; **`UserSecretsId`** reference in **[Getting started](docs/getting-started.md#user-secrets)**.

Benchmark prompt and TRA definition: **[src/Agent/Documents/qa.md](src/Agent/Documents/qa.md)** and **[metrics/agent_tool_routing_accuracy.md](metrics/agent_tool_routing_accuracy.md)**.

---

## Shared LLM layer

**`PromptEngineering.LLM`** provides **chat** completions (used by Client, Rag, and **Agent**), optional **SSE** streaming (aggregated to one completion object), **Polly** retries, timeouts, and **embeddings** (`CreateEmbeddingsAsync`), including optional **`EmbeddingDeployment`** when embedding and chat models differ. **Agent** selects an instance by **`Agent:InstanceName`** the same way prompts reference **`InstanceName`** in JSON.

---

## How to run the samples

Use a shell at the **repository root** (paths below assume your clone is `prompt-engineering/`). Swap **`https://...`** and API keys for your deployment.

### Prerequisites

| Requirement | Client | Rag | Agent |
| --- | --- | --- | --- |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Yes | Yes | Yes |
| LLM API (**OpenAI-compatible** base URL + keys) | Yes | Yes | Yes |
| **Node.js** + **`npx`** on `PATH` (MCP servers) | No | No | Yes |

Tune paths and **`appsettings.json`** as needed before running (dataset folders, **`Rag:*`** paths, **`Agent:InstanceName`**, and matching **`Instances`** entries). More detail: **[Getting started](docs/getting-started.md)**.

### User secrets

Each executable has its **own** user-secrets store (see **`UserSecretsId`** in each `.csproj`). Running **`dotnet user-secrets init`** below is **optional**—these projects already declare an id—but it is safe if you want an explicit first-time step.

Use the same configuration keys everywhere under **`SystemSettings:AiServiceSettings`**:

- Set **`BaseAddress`** to your API root URL.
- Set **`Instances:n:ApiKey`** for every index **`n`** you rely on. The **`Name`** of **`Instances[n]`** must match **`InstanceName`** in prompts (**Client**), **`Rag:InstanceName`**, or **`Agent:InstanceName`**.

### 1. PromptEngineering.Client (ReAct chain)

```powershell
dotnet user-secrets init --project src/PromptEngineering.Client/PromptEngineering.Client.csproj

dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "..." `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:1:ApiKey" "..." `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:2:ApiKey" "..." `
  --project src/PromptEngineering.Client/PromptEngineering.Client.csproj

dotnet run --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
```

Writes completions under **`output/`** per **`ContextSettings`** in **`appsettings.json`**. See **[docs/prompt-chain.md](docs/prompt-chain.md)**.

### 2. Rag

```powershell
dotnet user-secrets init --project src/Rag/Rag.csproj

dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/Rag/Rag.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "..." `
  --project src/Rag/Rag.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:1:ApiKey" "..." `
  --project src/Rag/Rag.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:2:ApiKey" "..." `
  --project src/Rag/Rag.csproj

dotnet run --project src/Rag/Rag.csproj
```

After indexing, choose prefilled or manual mode in the console; optional one-shot argument:  
`dotnet run --project src/Rag/Rag.csproj -- "Your question"`. See **[docs/rag.md](docs/rag.md)**.

### 3. Agent (MCP weather + news)

Ensure **`npx`** works (MCP packages are pulled on demand). Optional: **`dotnet user-secrets list --project src/Agent/Agent.csproj`** to verify keys.

```powershell
dotnet user-secrets init --project src/Agent/Agent.csproj

dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/Agent/Agent.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "..." `
  --project src/Agent/Agent.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:1:ApiKey" "..." `
  --project src/Agent/Agent.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:2:ApiKey" "..." `
  --project src/Agent/Agent.csproj

dotnet run --project src/Agent/Agent.csproj -- "What is the weather and the latest news in Paris?"
```

Without the trailing **`-- "..."`** argument, **Agent** reads a question from stdin. If you only use **one** configured instance, setting **`Instances:0:ApiKey`** alone is enough **provided** **`Agent:InstanceName`** matches **`Instances[0].Name`**.

### Verify secrets

```powershell
dotnet user-secrets list --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets list --project src/Rag/Rag.csproj
dotnet user-secrets list --project src/Agent/Agent.csproj
```

---

## Documentation

Hands-on guides for this repository. **Run steps:** **[How to run the samples](#how-to-run-the-samples)**. Then read **[Overview](docs/overview.md)** and **[Getting started](docs/getting-started.md)** for deeper configuration.

**Metrics:** Root **`metrics/`** holds offline scoring specs—for example **[answer_correctness_score.md](metrics/answer_correctness_score.md)** (RAG) and **[agent_tool_routing_accuracy.md](metrics/agent_tool_routing_accuracy.md)** (Agent tool traces). The Agent benchmark prompt lives in **[src/Agent/Documents/qa.md](src/Agent/Documents/qa.md)**.

| Guide | Topics |
| --- | --- |
| [Overview](docs/overview.md) | What the solution does, two tracks (ReAct vs RAG), shared LLM capabilities |
| [Getting started](docs/getting-started.md) | Prerequisites, configuration, unified user secrets for Client / Rag / Agent, run commands |
| [Repository structure](docs/repository-structure.md) | Folders, projects, where data and outputs live (includes diagram) |
| [Prompt chain (shark / ReAct)](docs/prompt-chain.md) | End-to-end ReAct flow, JSON prompt format, prompt versions, research question, output schema, reasoning cycle, quality bar |
| [RAG sample](docs/rag.md) | Indexing rules (including CSV rows), `Rag` settings, prose reservation, prefilled questions, retrieval and answering with citations |
| Agent (this README + config) | MCP weather/news tools, routing metric: [`src/Agent/appsettings.json`](src/Agent/appsettings.json), [`metrics/agent_tool_routing_accuracy.md`](metrics/agent_tool_routing_accuracy.md), [`src/Agent/Documents/qa.md`](src/Agent/Documents/qa.md) |

Related: [Project rules for prompt authoring](.cursor/rules/project-rules.mdc) (ReAct standard used when editing prompts).
