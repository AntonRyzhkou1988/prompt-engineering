# Repository structure

## Top-level layout

| Path | Role |
| --- | --- |
| `dataset/attacks.csv` | Source data for the ReAct client (shark attacks) |
| `prompts/*.json` | Prompt definitions and `ReActSequence` order (referenced by Client config) |
| `output/` | Default target for saved LLM completions from the Client |
| `src/` | .NET projects (see below) |
| `tests/` | Unit tests (e.g. `ContextService`) |
| `docs/` | This documentation set |

## Solution projects

| Project | Role |
| --- | --- |
| `PromptEngineering.Client` | Console host: load CSV, run `ReActSequence`, write outputs |
| `PromptEngineering.Services` | Orchestration (`ContextService`, pipeline) |
| `PromptEngineering.LLM` | HTTP integration: chat, embeddings, shared settings models |
| `PromptEngineering.Model` | Shared domain types |
| `Rag` | Console host: chunk documents, embed, retrieve, answer |
| `PromptEngineering.Services.Tests` | Tests for services |

## RAG assets (`Rag` project)

| Path (source) | Build / runtime |
| --- | --- |
| **`src/Rag/documents/`** | Copied to output as **`documents/**`** — indexed corpus (`.md`, `.txt`, `.csv`). |
| **`src/Rag/questions/`** | Copied to output as **`questions/**`** — prefilled prompts (`*.md`) for batch runs. |
| **`src/Rag/answers/`** | Created at runtime next to the executable (default **`answers/`**) — model outputs from prefilled or manual mode. |
| **`src/Rag/metrics/`** | Specs and gold eval CSV only; **not** in `Content` copy items — add files to **`documents/`** if they must be retrieved. |

## Diagram

```mermaid
graph TD
    ROOT["prompt-engineering/"]

    ROOT --> DATASET["dataset/"]
    ROOT --> PROMPTS["prompts/"]
    ROOT --> OUTPUT["output/"]
    ROOT --> SRC["src/"]
    ROOT --> TESTS["tests/"]
    ROOT --> DOCS["docs/"]

    DATASET --> DS_FILE["attacks.csv"]

    PROMPTS --> P_SEQ["initial.json → v1 → v2 → v3 → answer.json"]

    OUTPUT --> OUT_FILE["completion_<stem>_<timestamp>.md"]

    SRC --> CLIENT["PromptEngineering.Client"]
    SRC --> SERVICES["PromptEngineering.Services"]
    SRC --> LLM["PromptEngineering.LLM"]
    SRC --> MODEL["PromptEngineering.Model"]
    SRC --> RAG["Rag"]
    RAG --> RAG_DOCS["documents/ (.md, .txt, .csv)"]
    RAG --> RAG_Q["questions/ (*.md)"]

    TESTS --> TEST_PROJ["PromptEngineering.Services.Tests"]

    style ROOT fill:#1e1e2e,color:#cdd6f4,stroke:#89b4fa
    style DATASET fill:#1e1e2e,color:#cdd6f4,stroke:#a6e3a1
    style PROMPTS fill:#1e1e2e,color:#cdd6f4,stroke:#f9e2af
    style OUTPUT fill:#1e1e2e,color:#cdd6f4,stroke:#cba6f7
    style SRC fill:#1e1e2e,color:#cdd6f4,stroke:#89b4fa
    style TESTS fill:#1e1e2e,color:#cdd6f4,stroke:#f38ba8
    style DOCS fill:#1e1e2e,color:#cdd6f4,stroke:#94e2d5
```
