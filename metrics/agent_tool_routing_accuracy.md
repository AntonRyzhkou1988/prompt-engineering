# Metric: Tool Routing Accuracy (TRA)

## Summary

**Tool Routing Accuracy (TRA)** scores whether the agent invoked tools from the **expected domains** (weather vs news) during a run. It uses **only** the tool names recorded by the host—not prose quality or factual correctness.

In this repository, the **documented benchmark prompt** and an **illustrative reference reply** live in [`docs/applications/agent/agent-weather-news.md`](../docs/applications/agent/agent-weather-news.md). TRA is defined against that question’s **expected domains**; the **Answer** block in that document is for human or qualitative checks (correctness, tone), not for TRA.

---

## Why this metric exists

A model can answer fluently yet call the wrong MCP tools or skip a domain. TRA is a **binary, tool-trace** signal that complements answer-quality metrics (for example [`answer_correctness_score.md`](answer_correctness_score.md) if you score natural language separately).

---

## Definition

### Domains

| Domain   | Typical MCP / config | Routing config |
|----------|----------------------|----------------|
| **weather** | Open-Meteo (stdio), `Agent:OpenMeteo` in [`appsettings.json`](../src/Agent/appsettings.json) | `Agent:ToolRouting:WeatherToolNameSubstrings` |
| **news**    | DuckDuckGo web search, `Agent:DuckDuckGo` | `Agent:ToolRouting:NewsToolNameSubstrings` |

[`ToolDomainMapper`](../src/Agent/ToolDomainMapper.cs) maps each invoked tool name **t** to zero or more domains with **case-insensitive substring** matching (ordinal). If **t** contains configured substring **s**, **t** counts toward that domain. One name can match both domains if both substring lists hit.

Substring lists are bound to [`ToolRoutingMapOptions`](../src/Agent/AgentOptions.cs) from **`Agent:ToolRouting`**.

| Source | Weather substrings | News substrings |
|--------|-------------------|-----------------|
| **Committed** [`appsettings.json`](../src/Agent/appsettings.json) | `openmeteo` | `duckduckgo_web_search` |
| **Code defaults** (if arrays omitted in config) | `openmeteo`, `open_meteo`, `forecast`, `weather`, `meteo`, `ensemble`, `climate`, `air_quality`, `geocoding` | `duckduckgo`, `ddg`, `web_search` |

For scores reported **for this repo**, state whether you used the **committed** `appsettings.json` or overrides.

LLM usage goes through [`PromptEngineering.LLM`](../src/PromptEngineering.LLM) (`IAiService`); instance selection uses `Agent:InstanceName` and `SystemSettings:AiServiceSettings:Instances`.

### Tool trace (what you score)

[`WeatherNewsAgentService.RunAsync`](../src/Agent/Services/WeatherNewsAgentService.cs) collects assistant **function** tool names into `AgentRunResult.ToolNamesInvoked`. [`Program.cs`](../src/Agent/Program.cs) prints them after each run under `--- Tools invoked ---`.

Let **T** be that list (order preserved; duplicates allowed).

### Per-item score

For benchmark item *i*:

- **E** = set of expected domains (`weather`, `news`, or both).
- **T** = multiset of invoked tool names from the run.

**Score = 1** if for **every** domain *d* ∈ **E** there is at least one *t* ∈ **T** such that `ToolDomainMapper` maps *t* to *d* under the active config. **Otherwise score = 0.**

### Overall TRA

For *n* items with scores *s₁ … sₙ* ∈ {0, 1}:

**TRA_overall** = (*s₁* + … + *sₙ*) / *n*  
Range: **0** … **1**.

**Quantitative:** binary per item, mean over the set.

---

## Benchmark document (`docs/applications/agent/agent-weather-news.md`)

Canonical path: **[`docs/applications/agent/agent-weather-news.md`](../docs/applications/agent/agent-weather-news.md)**.

### Content mapping

| Part of benchmark file | Role in evaluation |
|-----------------|--------------------|
| **`## Question:`** block | Defines the **user prompt** for the benchmark item. |
| **`## Answer:`** block | **Illustrative** completion (weather sentence + news bullets). Use for human review or separate answer-quality scoring; **TRA does not read answer text**. |
| Expected domains (**E**) | Derived from the **question**: it asks for **both** weather and news → **E** = `{ weather, news }`. |

### Benchmark item (verbatim from benchmark document)

**Question** (the string to pass through the Agent; same idea as [`Program.cs`](../src/Agent/Program.cs) `Usage` / `Example` line after the executable name):

```text
What is the weather and the latest news in Paris?
```

**Expected domains E:** `weather` **and** `news` (both required for TRA = 1 on this item).

**Reference answer** (under **`## Answer:`** in the benchmark file): weather details (e.g. temperature, conditions, wind) plus a bulleted “latest news” list. That content is **time-sensitive** and **not** the TRA gold standard—only the tool trace **T** vs **E** defines TRA for this item.

### How to run the benchmark item

There is no `dotnet run -- --eval` or benchmark JSON in-repo; you apply the definition manually:

```powershell
dotnet run --project src/Agent -- "What is the weather and the latest news in Paris?"
```

Compare printed **--- Tools invoked ---** against **E** using the substring rules above.

---

## Limitations

- TRA does **not** judge factual correctness of tool outputs or of the final answer.
- Narrow substrings (committed config) can miss renamed tools until `WeatherToolNameSubstrings` / `NewsToolNameSubstrings` are updated.
- No tool calls ⇒ TRA is usually **0** for that item even if the text looks plausible.
- Ambiguous or misleading server tool names can skew domain mapping; document the config used when reporting TRA.

### Optional qualitative cross-check

After running TRA, you may compare the model’s reply to the **`Answer:`** section in [`docs/applications/agent/agent-weather-news.md`](../docs/applications/agent/agent-weather-news.md) for structure (weather + news bullets)—knowing that headlines and numbers will drift over time.

---

## Related artifacts

- Benchmark Q&A: [`docs/applications/agent/agent-weather-news.md`](../docs/applications/agent/agent-weather-news.md)
- Agent entrypoint and tool listing: [`src/Agent/Program.cs`](../src/Agent/Program.cs)
- Routing implementation: [`src/Agent/ToolDomainMapper.cs`](../src/Agent/ToolDomainMapper.cs), [`src/Agent/appsettings.json`](../src/Agent/appsettings.json)
