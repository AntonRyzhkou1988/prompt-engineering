# Prompt Engineering Practice

![.NET](https://img.shields.io/badge/.NET-8%2B-512BD4?logo=dotnet)
![LLM](https://img.shields.io/badge/LLM-GPT--4o%20%7C%20Claude%20%7C%20Gemini-10a37f)
![Data](https://img.shields.io/badge/Data-attacks.csv-blue)
![MCP](https://img.shields.io/badge/MCP-stdio-6366f1)

**.NET 8** solution for experimenting with complementary samples: **prompt engineering** (structured, versioned prompts via **`PromptEngineering.Client`**), **RAG** (retrieval-augmented answers via **`Rag`**), **tool-using agents** (**`Agent`** for weather/news MCP; **`Chatbot`** + **`SpaceMissions.McpServer`** for grounded space-launch CSV queries), and **`Security`** (paired **vulnerable** vs **mitigated** chat flows for **prompt injection** and **sensitive information disclosure**).

---

## Prompt engineering (in this repo)

**Prompt engineering** here means designing **explicit roles, tasks, constraints, and output shapes** so model outputs stay grounded, comparable across runs, and easy to review.

This track is implemented by **`PromptEngineering.Client`**:

- Prompts are **JSON files** (`prompts/`) with system/user text, temperature, and **per-prompt model routing** (`InstanceName`).
- **Shark attack** rows from `dataset/attacks.csv` are injected as XML `<record>` elements where the template has `<data></data>`.
- Runs follow a **ReAct-style refinement chain**: each completion can be passed into the next file as **`<prior_run>`**, so later prompts build on earlier analysis instead of starting cold.
- Completions are written as timestamped Markdown under `output/` for diffing and scoring.

For the full flow, schema (Sections A–D), and quality checklist, see **[docs/applications/prompt-engineering/prompt-chain.md](docs/applications/prompt-engineering/prompt-chain.md)** and **[project-rules.mdc](.cursor/rules/project-rules.mdc)**.

---

## RAG (in this repo)

**RAG (retrieval-augmented generation)** means: embed a **single** local corpus file, **retrieve** the chunks most similar to a question, and **generate** an answer using that retrieved text as context.

This track is implemented by **`Rag`**:

- **Corpus**: **`Rag:DatasetPath`** names **one** file (`.md`, `.txt`, or `.csv`), resolved under **`Rag:DocumentsFolderPath`** unless rooted. Committed default: **`dataset/space_missions.csv`**. To use **`attacks.csv`**, point **`DatasetPath`** at that file instead.
- **Layout**: **`questions/`** and **`answers/`** sit at the **repository root** (resolved via **`DocumentsFolderPath`**). The **`Rag`** project only hosts code and **`appsettings.json`**.
- **Index**: that file is **chunked** (CSV as row batches per **`Rag:Csv:BatchSize`**), **embedded**, and stored in an **in-memory** vector index.
- **Query**: embed the question; **top‑K** chunks with **`MinProseChunks`** prose reservation; **context-only** answer with **`[n]` citations**.
- **Batch Q&A**: prefilled **`questions/*.md`** from the console; answers under **`answers/`** (see **[docs/applications/rag/rag.md](docs/applications/rag/rag.md)**).
- **Configuration**: chunking, **`TopK`**, **`MinProseChunks`**, CSV batching, **`InstanceName`** (see **`src/Rag/appsettings.json`**).

Specs under **`metrics/`** and **[`docs/applications/rag/`](docs/applications/rag/)** are for documentation and offline scoring only; they are **not** retrieved unless their text is inside your **`DatasetPath`** file. Full behavior: **[docs/applications/rag/rag.md](docs/applications/rag/rag.md)**.

---

## Agent (MCP tools)

**Agent** runs **interactive Q&A**: the model may call **weather** tools (Open-Meteo MCP) and **news / web search** tools (DuckDuckGo MCP). The host prints the final answer and the **tools invoked** list.

This track is implemented by **`Agent`** (`src/Agent/`):

- **Runtime**: **`PromptEngineering.LLM`** (`IAiService`) chat completions with **function / tool calling**; tool schemas are loaded from MCP **stdio** sessions configured in **`Agent:OpenMeteo`** and **`Agent:DuckDuckGo`** in [`src/Agent/appsettings.json`](src/Agent/appsettings.json).
- **Prerequisites**: **Node.js** with **`npx`** on `PATH` (committed defaults spawn MCP servers via `npx`).
- **Configuration**: **`SystemSettings:AiServiceSettings`** (same pattern as Client/Rag); **`Agent:InstanceName`**, **`Temperature`**, **`MaxFunctionIterations`**; optional **`Agent:ToolRouting`** maps invoked tool names to domains for the **[Tool Routing Accuracy](metrics/agent_tool_routing_accuracy.md)** metric.
- **Secrets**: Same **`SystemSettings:AiServiceSettings`** pattern as Client/Rag—step-by-step commands under **[How to run the samples](#how-to-run-the-samples)**; **`UserSecretsId`** reference in **[Getting started](docs/getting-started.md#user-secrets)**.

Benchmark prompt and TRA definition: **[docs/applications/agent/agent-weather-news.md](docs/applications/agent/agent-weather-news.md)** and **[metrics/agent_tool_routing_accuracy.md](metrics/agent_tool_routing_accuracy.md)**.

---

## Space Missions MCP (`SpaceMissions.McpServer` + `Chatbot`)

**`SpaceMissions.McpServer`** is a **stdio MCP** process that exposes **eight** tools over **`dataset/space_missions.csv`** (filter, aggregate, launch-country share, success rate, distinct values, schema/summary). **`Chatbot`** hosts the LLM tool loop for bot channels (Teams / M365 Agents Playground).

- **Runtime:** In-memory CSV via **`PromptEngineering.SpaceMissions`**; child process receives **`SPACE_MISSIONS_DATASET_PATH`** from **`Chatbot`** path resolution.
- **Configuration:** **`SpaceMissionsAgent`** in [`src/Chatbot/appsettings.json`](src/Chatbot/appsettings.json); LLM keys like other samples.
- **Prerequisites:** **.NET 8** only for this MCP (unlike **Agent**, no **Node.js** / **`npx`**).
- **Documentation:** **[docs/applications/space-missions-mcp/space-missions-mcp.md](docs/applications/space-missions-mcp/space-missions-mcp.md)** (architecture, run, Chatbot wiring) · **[tool reference](docs/applications/space-missions-mcp/space-missions-mcp-tools.md)**.

---

## Shared LLM layer

**`PromptEngineering.LLM`** provides **chat** completions (used by Client, Rag, **Agent**, and **Security**), optional **SSE** streaming (aggregated to one completion object), **Polly** retries, timeouts, and **embeddings** (`CreateEmbeddingsAsync`), including optional **`EmbeddingDeployment`** when embedding and chat models differ. **Agent** and **Security** select an instance by **`Agent:InstanceName`** or **`Security:InstanceName`** the same way prompts reference **`InstanceName`** in JSON.

---

## How to run the samples

Use a shell at the **repository root** (paths below assume your clone is `prompt-engineering/`). Swap **`https://...`** and API keys for your deployment.

### Prerequisites

| Requirement | Client | Rag | Agent | Chatbot | Security |
| --- | --- | --- | --- | --- | --- |
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | Yes | Yes | Yes | Yes | Yes |
| LLM API (**OpenAI-compatible** base URL + keys) | Yes | Yes | Yes | Yes | Yes |
| **Node.js** + **`npx`** on `PATH` (external MCP) | No | No | Yes | No | No |
| Bot registration (Teams / playground) | No | No | No | Yes | No |

Tune paths and **`appsettings.json`** as needed before running (dataset folders, **`Rag:*`** paths, **`Agent:InstanceName`** / **`Security:InstanceName`**, and matching **`Instances`** entries). More detail: **[Getting started](docs/getting-started.md)**.

### User secrets

Each executable has its **own** user-secrets store (see **`UserSecretsId`** in each `.csproj`). Running **`dotnet user-secrets init`** below is **optional**—these projects already declare an id—but it is safe if you want an explicit first-time step.

Use the same configuration keys everywhere under **`SystemSettings:AiServiceSettings`**:

- Set **`BaseAddress`** to your API root URL.
- Set **`Instances:n:ApiKey`** for every index **`n`** you rely on. The **`Name`** of **`Instances[n]`** must match **`InstanceName`** in prompts (**Client**), **`Rag:InstanceName`**, **`Agent:InstanceName`**, or **`Security:InstanceName`**.

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

Writes completions under **`output/`** per **`ContextSettings`** in **`appsettings.json`**. See **[docs/applications/prompt-engineering/prompt-chain.md](docs/applications/prompt-engineering/prompt-chain.md)**.

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
`dotnet run --project src/Rag/Rag.csproj -- "Your question"`. See **[docs/applications/rag/rag.md](docs/applications/rag/rag.md)**.

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

### 4. Chatbot (Space Missions MCP)

```powershell
dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/Chatbot/Chatbot.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:2:ApiKey" "..." `
  --project src/Chatbot/Chatbot.csproj

dotnet build src/SpaceMissions.McpServer/SpaceMissions.McpServer.csproj
dotnet run --project src/Chatbot/Chatbot.csproj
```

Set bot **`ClientId`** / **`ClientSecret`** via user secrets when using the Bot Framework (see **[Getting started](docs/getting-started.md#user-secrets)**). Full guide: **[docs/applications/space-missions-mcp/space-missions-mcp.md](docs/applications/space-missions-mcp/space-missions-mcp.md)**.

### 5. Security samples

Console demos for **prompt injection** and **sensitive information disclosure** (vulnerable pattern, then mitigation). Uses **`Security:InstanceName`** and **`Security:Temperature`** in [`src/Security/appsettings.json`](src/Security/appsettings.json). No MCP; chat only.

```powershell
dotnet user-secrets init --project src/Security/Security.csproj

dotnet user-secrets set "SystemSettings:AiServiceSettings:BaseAddress" "https://..." `
  --project src/Security/Security.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:0:ApiKey" "..." `
  --project src/Security/Security.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:1:ApiKey" "..." `
  --project src/Security/Security.csproj
dotnet user-secrets set "SystemSettings:AiServiceSettings:Instances:2:ApiKey" "..." `
  --project src/Security/Security.csproj

dotnet run --project src/Security/Security.csproj
```

Behavior and scenario table: **[docs/applications/security/security-samples.md](docs/applications/security/security-samples.md)**. Risk narratives: **[risk-assessment/prompt-injection.md](risk-assessment/prompt-injection.md)**, **[risk-assessment/sensitive-information-disclosure.md](risk-assessment/sensitive-information-disclosure.md)**.

### Verify secrets

```powershell
dotnet user-secrets list --project src/PromptEngineering.Client/PromptEngineering.Client.csproj
dotnet user-secrets list --project src/Rag/Rag.csproj
dotnet user-secrets list --project src/Agent/Agent.csproj
dotnet user-secrets list --project src/Chatbot/Chatbot.csproj
dotnet user-secrets list --project src/Security/Security.csproj
```

---

## Documentation

This section is the **single index** for repository guides. **Run steps:** [How to run the samples](#how-to-run-the-samples). Shared narrative: [Overview](docs/overview.md), [Getting started](docs/getting-started.md), [Repository structure](docs/repository-structure.md).

### Common guides (`docs/`)

| Document | Topics |
| --- | --- |
| [Overview](docs/overview.md) | Sample tracks, shared LLM behavior, safety notes |
| [Getting started](docs/getting-started.md) | Prerequisites, user secrets, run commands |
| [Repository structure](docs/repository-structure.md) | Folders, projects, diagram |

### Prompt engineering — `PromptEngineering.Client` (`docs/applications/prompt-engineering/`)

Versioned JSON prompts, shark **`dataset/attacks.csv`**, `<data>` / `<prior_run>`, completions under **`output/`**.

| Document | Topics |
| --- | --- |
| [Prompt chain (ReAct)](docs/applications/prompt-engineering/prompt-chain.md) | Flow, JSON schema, Sections A–D, quality checklist |

### RAG — `Rag` (`docs/applications/rag/`)

Single-file corpus indexing, embeddings, retrieval, context-only answers with **`[n]` citations`.

| Document | Topics |
| --- | --- |
| [RAG guide](docs/applications/rag/rag.md) | `DatasetPath`, chunking, `TopK`, console behavior |
| [Space missions data dictionary](docs/applications/rag/space_missions_data_dictionary.md) | Column semantics for **`dataset/space_missions.csv`** |
| [RAG eval gold (space missions)](docs/applications/rag/rag_eval_space_missions_gold.md) | Automated substring / mode checks for prefilled questions |

### Agent — MCP tools (`docs/applications/agent/`)

Chat with **Open-Meteo** and **DuckDuckGo** via stdio; configure [`src/Agent/appsettings.json`](src/Agent/appsettings.json); requires **Node.js** / **`npx`**.

| Document | Topics |
| --- | --- |
| [TRA benchmark — Paris weather & news](docs/applications/agent/agent-weather-news.md) | Canonical benchmark + illustrative answer for [Tool Routing Accuracy](metrics/agent_tool_routing_accuracy.md) |

### Space missions MCP — `SpaceMissions.McpServer` + `Chatbot` (`docs/applications/space-missions-mcp/`)

Eight stdio tools over **`dataset/space_missions.csv`**; **`Chatbot`** runs the LLM tool loop. **.NET only** (no Node for this MCP).

| Document | Topics |
| --- | --- |
| [Space Missions MCP guide](docs/applications/space-missions-mcp/space-missions-mcp.md) | Architecture, dataset path, Chatbot configuration, evidence rules |
| [Tool reference](docs/applications/space-missions-mcp/space-missions-mcp-tools.md) | All eight tools, filters, limits, JSON shapes |

### Security — `Security` (`docs/applications/security/`)

Paired **vulnerable / mitigated** chat completions for **prompt injection** and **sensitive information disclosure**; links to **`risk-assessment/`** narratives.

| Document | Topics |
| --- | --- |
| [Security samples](docs/applications/security/security-samples.md) | Demo order, configuration, relation to [`src/Security/`](src/Security/) |

### Metrics and prompt rules

| Resource | Role |
| --- | --- |
| [metrics/](metrics/) | Offline specs (e.g. [answer_correctness_score.md](metrics/answer_correctness_score.md), [agent_tool_routing_accuracy.md](metrics/agent_tool_routing_accuracy.md)); not ingested by **`Rag`** unless that text is inside **`Rag:DatasetPath`** |
| [project-rules.mdc](.cursor/rules/project-rules.mdc) | ReAct / evidence standard for prompt authoring |
