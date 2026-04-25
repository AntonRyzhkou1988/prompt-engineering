# Metric: Tool Routing Accuracy (TRA)

## Purpose

**Tool Routing Accuracy (TRA)** measures whether the agent invokes MCP tools from the **correct domains** (weather via Open-Meteo vs. news/search via DuckDuckGo MCP) when answering benchmark questions. It is a **quantitative, automatic** signal derived from **function/tool names** recorded during a run.

TRA complements prose-quality metrics: a model can write fluent text but call the wrong tools; TRA catches that failure mode.

## Definition

### Domains

Each MCP tool name is mapped to one or more domains using substring rules (configurable under `Agent:ToolRouting` in [`src/Agent/appsettings.json`](../src/Agent/appsettings.json)). LLM calls use [`PromptEngineering.LLM`](../src/PromptEngineering.LLM) (`IAiService`) with `SystemSettings:AiServiceSettings` and `Agent:InstanceName` selecting the deployment.

| Domain  | Typical tool name patterns (examples) |
|--------|-----------------------------------------|
| Weather | Substrings such as `forecast`, `weather`, `meteo`, `ensemble`, `air_quality`, `geocoding`, etc. |
| News    | Substrings such as `duckduckgo`, `ddg`, `web_search` |

### Per-item score

For benchmark item *i*, let **E** be the ordered set of expected domains (`weather`, `news`, or both as two entries). Let **T** be the multiset of tool names invoked during the run (from the chat response’s function-call contents).

Item *i* scores **1** if for every domain *d* in **E** there exists at least one tool name *t* in **T** such that *t* maps to *d* under the rules above. Otherwise the score is **0**.

### Overall TRA

For *n* items with scores *s₁ … sₙ* ∈ {0, 1}:

**TRA_overall** = (s₁ + … + sₙ) / *n*

Range: **0** to **1**.

## Dataset

Benchmark rows live in [`src/Agent/data/eval_items.json`](../src/Agent/data/eval_items.json) (`id`, `question`, `expected_domains`).

## Demonstration

From the repository root:

```bash
dotnet run --project src/Agent -- --eval
```

Requires valid `SystemSettings:AiServiceSettings` (and `Agent:InstanceName`), Node.js/npx on `PATH` so the MCP servers can start, and optional DuckDuckGo env overrides under `Agent:DuckDuckGo:Environment` (or matching process env vars). The command prints per-item pass/fail and **Overall TRA**.

## Limitations

- TRA does **not** judge factual correctness of tool outputs or final natural-language answers.
- Misleading tool names or ambiguous substrings can skew mapping; tune `WeatherToolNameSubstrings` / `NewsToolNameSubstrings` if a server exposes unexpected names.
- If the model completes without calling tools, TRA is typically **0** for that item even if the text looks plausible.

## Type

- **Quantitative** — Binary per item, mean over the benchmark set.
- **Safety-related (coarse)** — Items that demand “weather only” or “news only” help detect inappropriate cross-domain tool use when paired with strict expected domains.
