# Repository structure

**[Repository README — Documentation](../README.md#documentation)** · [Overview](overview.md)

## Top-level layout

| Path | Role |
| --- | --- |
| `dataset/attacks.csv` | Source data for **PromptEngineering.Client** (shark attacks) |
| `dataset/space_missions.csv` | Tabular sample for **Rag** (`Rag:DatasetPath`) and **SpaceMissions.McpServer** / **Chatbot** MCP tools |
| `prompts/*.json` | Prompt definitions and **`ReActSequence`** (Client) |
| `output/` | Default Client completion output (`completion_<stem>_<timestamp>.md`) |
| `documents/` | Optional extra files; use as **`Rag:DatasetPath`** target if you keep a corpus here |
| `questions/` | RAG prefilled prompts (`*.md`) — **`DocumentsFolderPath`** + **`QuestionsPath`** |
| `answers/` | RAG saved answers — **`DocumentsFolderPath`** + **`AnswersPath`** |
| `gds/` | Chatbot MCP golden data set — **`manifest.json`**, **`ground-truth/`**, agent **`answers/`**, LLM **`judge/`** results |
| `metrics/` | Offline scoring specs (**`answer_correctness_score.md`**, **`agent_tool_routing_accuracy.md`**, …); **not** indexed by Rag unless copied into the **`DatasetPath`** file |
| `risk-assessment/` | Narrative risk notes (**prompt injection**, **sensitive information disclosure**) aligned with the **`Security`** console demos |
| `src/` | .NET projects ([solution](../src/PromptEngineering.sln)) |
| `tests/` | Unit tests (e.g. **`PromptEngineering.Services.Tests`**) |
| **`docs/`** | Common guides plus **`docs/applications/`** (**prompt-engineering**, **rag**, **agent**, **space-missions-mcp**, **security**) — full index: [README.md](../README.md#documentation) |

## Solution projects

| Project | Role |
| --- | --- |
| `PromptEngineering.Client` | Console: load CSV, run **`ReActSequence`**, write outputs |
| `PromptEngineering.Services` | Orchestration (**`ContextService`**, pipeline) |
| `PromptEngineering.LLM` | HTTP integration: chat, embeddings, settings models |
| `PromptEngineering.Model` | Shared domain types |
| `Rag` | Console: single-file index, retrieve, answer |
| `Agent` | Console: MCP tool-using chat — **`src/Agent/appsettings.json`** |
| `SpaceMissions.McpServer` | Stdio MCP server: eight tools over **`dataset/space_missions.csv`** — [Space Missions MCP](applications/space-missions-mcp/space-missions-mcp.md) |
| `PromptEngineering.SpaceMissions` | CSV load, filter, aggregate, launch-country and success-rate queries (library for MCP server) |
| `Chatbot` | ASP.NET bot host; **RAG index** + **SpaceMissions.McpServer** MCP tool loop — **`src/Chatbot/appsettings.json`** |
| `Security` | Console: paired **vulnerable / mitigated** chat demos (**prompt injection**, **sensitive disclosure**) — **`src/Security/`**, [Security samples](applications/security/security-samples.md) |
| `PromptEngineering.Services.Tests` | Service tests |

## RAG paths (mental model)

| Item | Role |
| --- | --- |
| **`Rag:DocumentsFolderPath`** | Anchor for **`DatasetPath`**, **`QuestionsPath`**, **`AnswersPath`** when those values are relative |
| **`Rag:DatasetPath`** | **One** indexed **`.md`**, **`.txt`**, or **`.csv`** (committed default: **`dataset/space_missions.csv`**) |
| **`questions/`**, **`answers/`** | Sibling folders under repo root by default; resolved via **`DocumentsFolderPath`** |

Offline RAG eval: **[`applications/rag/rag_eval_space_missions_gold.md`](applications/rag/rag_eval_space_missions_gold.md)**. Chatbot MCP GDS: **[`../gds/gds_space_missions_mcp.md`](../gds/gds_space_missions_mcp.md)**. Agent TRA sample: **[`applications/agent/agent-weather-news.md`](applications/agent/agent-weather-news.md)**. Space missions MCP: **[`applications/space-missions-mcp/space-missions-mcp.md`](applications/space-missions-mcp/space-missions-mcp.md)**. Security demos: **[`applications/security/security-samples.md`](applications/security/security-samples.md)**.

## Diagram

```mermaid
graph TD
    ROOT["prompt-engineering/"]

    ROOT --> DATASET["dataset/"]
    ROOT --> PROMPTS["prompts/"]
    ROOT --> OUTPUT["output/"]
    ROOT --> EXTRA_CORPUS["documents/"]
    ROOT --> QUESTIONS["questions/"]
    ROOT --> ANSWERS["answers/"]
    ROOT --> GDS["gds/"]
    ROOT --> METRICS["metrics/"]
    ROOT --> SRC["src/"]
    ROOT --> TESTS["tests/"]
    ROOT --> DOCSROOT["docs/"]

    DATASET --> DS_ATTACK["attacks.csv"]
    DATASET --> DS_SPACE["space_missions.csv"]

    PROMPTS --> P_SEQ["ReAct JSON chain"]

    OUTPUT --> OUT_FILE["completion_*.md"]

    SRC --> CLIENT["PromptEngineering.Client"]
    SRC --> SERVICES["PromptEngineering.Services"]
    SRC --> LLM["PromptEngineering.LLM"]
    SRC --> MODEL["PromptEngineering.Model"]
    SRC --> RAG["Rag"]
    SRC --> AGENT["Agent"]
    SRC --> MCP_SM["SpaceMissions.McpServer"]
    SRC --> LIB_SM["PromptEngineering.SpaceMissions"]
    SRC --> CHATBOT["Chatbot"]
    SRC --> SECURITY["Security"]

    MCP_SM --> LIB_SM
    CHATBOT -->|stdio MCP| MCP_SM
    MCP_SM -->|"SPACE_MISSIONS_DATASET_PATH"| DS_SPACE

    RAG -->|"DatasetPath file"| DATASET
    RAG -->|"questions"| QUESTIONS
    RAG -->|"answers"| ANSWERS

    TESTS --> TEST_PROJ["PromptEngineering.Services.Tests"]
```
