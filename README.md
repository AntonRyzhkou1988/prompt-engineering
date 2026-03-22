# Prompt Engineering Practice

This repository practices **ReAct-style prompt design** for reliable analysis of `dataset/attacks.csv`. The goal is **evidence-grounded** answers with **explicit uncertainty**, **confidence labels**, and a **stable output shape** so runs are comparable.

---

## How prompts run

- Prompts live in [`prompts/`](prompts/) as JSON files (processed in **file-name order** when using `RunReActAsync`, or individually by name when using `RunIterativeAsync`).
- Each file defines two string arrays: `DefaultAssistantRole` (system) and `DefaultUserPrompt` (user). The client **joins array entries with newlines** into the messages sent to the model.
- The user prompt must contain a `<data>...</data>` region. At runtime, the app **replaces the inner part** with one `<record>...</record>` per loaded CSV row (see [Injected XML](#injected-xml-one-record-per-row)).
- Paths for prompts, dataset, and output are configured under `ContextSettings` in [`src/PromptEngineering.Client/appsettings.json`](src/PromptEngineering.Client/appsettings.json).
- When all iterations have finished, [`IContextService.SummarizeAsync`](src/PromptEngineering.Services/IContextService.cs) writes **`summarize.txt`** inside `ContextSettings.OutputDirectory`: for each run, a compact header (`## Run: {prompt stem}` and `Output: {path}`) followed by verbatim first-choice assistant text; runs are separated by a `---` block. Empty completions use an explicit placeholder referencing the output path. This is **host-side formatting only** — no second LLM call — so you can compare iterations side by side.

---

## Iterative execution (`RunIterativeAsync`)

The primary execution mode runs a **single prompt file through N chained ReAct cycles**. Each iteration injects the previous completion into the prompt's `<prior_run>...</prior_run>` region, enabling the model to build on and refine its analysis rather than starting fresh.

```
Iteration 1: initial.json + <record> elements (no prior run)
             → completion saved as completion_initial_run1_<timestamp>.md

Iteration 2: initial.json + <record> elements + <prior_run> = run 1 output
             → completion saved as completion_initial_run2_<timestamp>.md

Iteration 3: initial.json + <record> elements + <prior_run> = run 2 output
             → completion saved as completion_initial_run3_<timestamp>.md
```

`Program.cs` runs this with `iterations: 3` against `initial.json` by default. Adjust `iterations` in the call site or set a different prompt file name as needed.

---

## Injected XML (one record per row)

The pipeline maps selected CSV columns into XML tags (names are fixed; empty tags mean missing values in the source row).

| XML element | Source column (conceptual) |
|-------------|----------------------------|
| `Year` | Year |
| `Country` | Country |
| `Area` | Area |
| `Type` | Type |
| `Activity` | Activity |
| `Injury` | Injury |
| `FatalYN` | Fatal (Y/N) |
| `Sex` | Sex (CSV header may include a trailing space) |
| `Age` | Age |
| `Time` | Time |
| `Species` | Species (CSV header may include a trailing space) |
| `InvestigatorSource` | Investigator or Source |

The raw CSV has additional columns (e.g. Case Number, Date, links). They are **not** injected unless the loader is extended; analyses should rely on the elements above.

---

## ReAct prompt standard (repository rule)

### Canonical ReAct (Reasoning + Acting)

In the general pattern, the model repeats a loop until it can answer:

1. **Thought** — Analyze the situation, decompose the problem, plan the next step.
2. **Action** — Choose and execute a tool (for example `Search[query]`, `Calculator[expression]`, or an API call).
3. **Observation** — The environment returns tool output; the model uses it in the next Thought.

**Prompt ingredients** for agentic ReAct: **system instructions** that require Thought/Action/Observation formatting, **defined tools**, and often **few-shot** examples of correct tool use.

Compared with linear chain-of-thought, ReAct is **adaptive** (the path changes from observations), encourages **stepwise** answers, and can ground claims in **external** knowledge when real tools are wired in.

### ReAct in this repository

The client sends **one** system message plus **one** user message (with injected `<data>`) and receives **one** completion ([`ContextService`](src/PromptEngineering.Services/ContextService.cs)). There is **no** host-driven multi-turn tool loop within a single call.

Here, **Observation** means what the model **reads** from `<record>` elements (patterns, gaps, contradictions) — not a separate HTTP round-trip. **Action** means deliberate **analytical moves** on that evidence (for example stratifying by `Country` and `FatalYN`, scanning missingness) — carried out in the model's **internal** reasoning. The `<prior_run>` injection in `initial.json` extends this into a **multi-iteration chain**: each call's output becomes the next call's Observation baseline.

**Illustrative single internal cycle** (format only; not a second API call):

- **Thought:** I need Activity and Type vs outcomes before writing Section A bullets.
- **Action:** `ReviewElements[Activity, Type, FatalYN, Injury]` — scan `<record>` values and missing tags.
- **Observation:** Several activities recur; `Type` mixes Unprovoked, Provoked, Invalid; `Injury` is heterogeneous text — claims should be qualified.

Production-ready prompts here should also enforce:

1. **Role first** — domain-relevant analyst, not a generic assistant.
2. **Explicit scope** — which fields and the research question (see below).
3. **ReAct flow** — field selection → data-quality check → evidence-based findings → **self-critique** → **claim revision** before the final answer (mapped to Thought/Action/Observation internally; see [.cursor/rules/project-rules.mdc](.cursor/rules/project-rules.mdc)).
4. **Strict response schema** — section headings and **bullet limits**.
5. **Safety** — no fabricated metrics; disclose partial evidence; **High / Medium / Low** confidence on substantive claims.

---

## Research question (shared across all prompts)

**Question:** In the provided `<record>` rows from `dataset/attacks.csv`, how do **Activity** and encounter **Type** relate to harm outcomes (**FatalYN** and **Injury**), and which **data-quality** issues most limit how strong those conclusions can be?

**Primary elements:** Type, Activity, Injury, FatalYN. **Context as needed:** Year, Country, Area (and other injected elements only for supporting detail).

---

## Prompt progression

| Version | File | Intent | ReAct | Iterative |
|---------|------|--------|--------|-----------|
| **initial** | [`prompts/initial.json`](prompts/initial.json) | **Canonical entry point**: mandatory internal Thought/Action/Observation, `<prior_run>` chain support, strict Sections A–D, confidence labels | **Mandatory** (internal ToA + 5-step self-reflection; visible output is A–D only) | Yes — designed for 3-iteration chained execution |
| **v1** | [`prompts/v1.json`](prompts/v1.json) | Baseline: same research question, minimal scaffolding; internal analytical flow | Implicit only (no mandatory Thought/Action/Observation or 5-step loop; still cite `<record>` evidence) | No |
| **v2** | [`prompts/v2.json`](prompts/v2.json) | **Numbered outcomes**, field focus, fixed **section headings**, confidence on substantive bullets | Implicit structured reasoning (quality + follow-ups explicit; no mandatory ToA vocabulary or 5-step loop) | No |
| **v3** | [`prompts/v3.json`](prompts/v3.json) | **Canonical standalone**: mandatory internal Thought/Action/Observation on `<record>` data plus 5-step self-reflection; strict Sections A–D and bullet caps | **Mandatory** (internal ToA + steps; visible output remains A–D only) | No |

**Key difference between `initial` and `v3`:** `initial.json` adds the `<prior_run>` region and is designed to run iteratively; `v3.json` is a self-contained single-turn prompt with no prior-run feedback mechanism.

---

## Output schema (Sections A–D)

Used by both `initial.json` and `v3.json`:

- **Section A — Findings:** 3–5 bullets (insight; supporting elements; Confidence: High | Medium | Low).
- **Section B — Data quality caveats:** 3–5 bullets (each ties a risk to interpretation).
- **Section C — Next analyses:** at most 3 bullets (feasible on the same fields).
- **Section D — Executive summary:** at most 3 short bullets; no new unsupported claims.

---

## Workflow

```mermaid
flowchart TD
    A["Load dataset/attacks.csv"] --> B["Iteration 1: initial.json + records"]
    B --> C["CompleteChatAsync"]
    C --> D["Save completion_initial_run1_*.md"]
    D --> E["Iteration 2: initial.json + records + prior_run"]
    E --> F["CompleteChatAsync"]
    F --> G["Save completion_initial_run2_*.md"]
    G --> H["Iteration 3: initial.json + records + prior_run"]
    H --> I["CompleteChatAsync"]
    I --> J["Save completion_initial_run3_*.md"]
    J --> K["SummarizeAsync -> summarize.txt"]
    K --> L{Pass acceptance gate?}
    L -- No --> M["Revise initial.json or meta-prompt"]
    M --> B
    L -- Yes --> N["Promote as standard"]
```

---

## Quality checklist and acceptance gate

Score 1 (weak) to 5 (strong):

- Clarity  
- Specificity  
- Grounding  
- Hallucination resistance  
- Consistency  
- Actionability  

**Accept** only if average ≥ **4.0**, with **no fabricated metrics** and **no unlabeled speculation** passed off as fact.

---

## Minimal runbook

1. Point `ContextSettings:PromptPath` at the `prompts` folder and `DatasetPath` at `dataset/attacks.csv`.
2. Run the client — it calls `RunIterativeAsync("initial.json", iterations: 3)`, executing three chained ReAct cycles. Each cycle's output is saved as a timestamped `.md` file under `ContextSettings:OutputDirectory`.
3. The client also writes **`summarize.txt`** (inside `OutputDirectory`) via `SummarizeAsync` as a single-file rollup of all iteration outputs for side-by-side comparison.
4. Evaluate each iteration's Markdown against the quality checklist above. Iteration 3 should show the most refined, self-critiqued claims.
5. If quality stalls, **meta-prompt** `initial.json` (preserve intent, tighten grounding and schema) and re-run.
6. Treat **`initial.json`** as the default template for new incident-style prompts in this repo; use `v3.json` as a reference for single-turn standalone runs.
