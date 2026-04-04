# Prompt chain (shark attacks / ReAct)

This document describes how **`PromptEngineering.Client`** runs the JSON prompts under `prompts/` against **`dataset/attacks.csv`**, how outputs are shaped, and how quality is judged.

## End-to-end flow

`RunReActAsync` walks **`ContextSettings.ReActSequence`** in order. After each completion, the host injects that text into the next prompt’s **`<prior_run>...</prior_run>`** region (if present).

```text
initial.json   →  completion_initial_<timestamp>.md
       │ <prior_run>
       ▼
v1.json        →  completion_v1_<timestamp>.md
       │ <prior_run>
       ▼
v2.json        →  completion_v2_<timestamp>.md
       │ <prior_run>
       ▼
v3.json        →  completion_v3_<timestamp>.md
       │ <prior_run>
       ▼
answer.json    →  completion_answer_<timestamp>.md
```

Prompts **without** `<prior_run>` ignore injected content.

## What each run does (data path)

1. Load up to **`MaximumDatasetRecordCount`** rows from the CSV.
2. Replace the **`<data></data>`** placeholder in the user prompt with one **`<record>`** per row (XML).
3. Call the LLM using the prompt’s **`InstanceName`** and **`Temperature`**.
4. Persist the assistant content under **`OutputDirectory`**.

## Prompt JSON schema

Each file is a JSON object with **four** required properties:

| Field | Type | Meaning |
| --- | --- | --- |
| `InstanceName` | string | Must match `SystemSettings.AiServiceSettings.Instances[].Name` |
| `Temperature` | number | Sampling temperature for that call |
| `DefaultAssistantRole` | string[] | Lines joined as the system message |
| `DefaultUserPrompt` | string[] | Lines joined as the user message; must include `<data></data>` |

Example skeleton:

```json
{
  "InstanceName": "AIArchitect.PromptEngineering.Low",
  "Temperature": 0.3,
  "DefaultAssistantRole": ["System line 1", "line 2"],
  "DefaultUserPrompt": ["User line 1", "<data></data>", "line 3"]
}
```

### Injected XML (`<record>` fields)

| XML tag | CSV column |
| --- | --- |
| `Year` | Year |
| `Country` | Country |
| `Area` | Area |
| `Type` | Type |
| `Activity` | Activity |
| `Injury` | Injury |
| `FatalYN` | Fatal (Y/N) |
| `Sex` | Sex |
| `Age` | Age |
| `Time` | Time |
| `Species` | Species |
| `InvestigatorSource` | Investigator or Source |

Empty tags mean missing values. Values are XML-escaped where needed.

## Prompt versions (committed set)

| Version | File | Instance tier | Temp | Focus |
| --- | --- | --- | ---: | --- |
| **initial** | `initial.json` | Low | 0.3 | Thought / Action / Observation, `<prior_run>`, Sections A–D |
| **v1** | `v1.json` | Low | 0.3 | Broad geographic-hotspot baseline |
| **v2** | `v2.json` | Low | 0.3 | Numbered goals, headings, confidence labels |
| **v3** | `v3.json` | Medium | 0.2 | Strict ReAct loop + five-step self-reflection, Sections A–D |
| **answer** | `answer.json` | High | 0.2 | Final synthesis using prior runs |

### Default instance names and models (reference)

| Tier | `InstanceName` | Model (as in repo config) |
| --- | --- | --- |
| Low | `AIArchitect.PromptEngineering.Low` | `gpt-4o-2024-05-13` |
| Medium | `AIArchitect.PromptEngineering.Medium` | `anthropic.claude-opus-4-20250514-v1:0-with-thinking` |
| High | `AIArchitect.PromptEngineering.High` | `gemini-2.5-pro` |

**Tip:** Start new work from `initial.json`; use `v3.json` as a strict single-turn reference.

## Research question (shark dataset)

> Are there geographic hotspots whose shark attack frequency is rising or falling, and what might drive those trends?

| Role | Fields |
| --- | --- |
| Primary | `Country`, `Area`, `Year` |
| Supporting | `Type`, `Activity`, `FatalYN`, `Injury`, `Species` |

## Output schema: Sections A–D

Used by **`initial.json`** and **`v3.json`** (visible output only; internal chain-of-thought is not shown).

| Section | Intent | Bullet limit |
| --- | --- | ---: |
| **A — Findings** | Trend insight with Country / Area / Year evidence; candidate drivers; `Confidence: High / Medium / Low` | 3–5 |
| **B — Data quality caveats** | Risks (missing Year, naming noise, bias, small *n*) tied to how they affect geographic trends | 3–5 |
| **C — Next analyses** | Feasible follow-ups on the same fields | ≤ 3 |
| **D — Executive summary** | Short; **no** new unsupported claims | ≤ 3 |

## Internal reasoning pattern (ReAct + self-reflection)

Before Sections A–D, prompts expect an internal loop:

```text
Thought      → plan scope
Action       → analytical moves on injected <record> data only
Observation  → what the records support or withhold
```

Then a **five-step** pass: select fields → data-quality risks → evidence-based findings → self-critique → revise weak claims.

## Quality checklist

Score each dimension **1** (weak) through **5** (strong):

| Criterion | Ask |
| --- | --- |
| Clarity | Easy to follow? |
| Specificity | Claims tied to fields or records? |
| Grounding | Supported by injected data? |
| Hallucination resistance | No fabricated metrics or tool output? |
| Consistency | Matches Sections A–D schema? |
| Actionability | Next steps concrete? |

**Accept** when the **average ≥ 4.0**, with no fabricated metrics and no unlabeled speculation.

If scores stall, use **meta-prompting**: have an LLM revise the prompt (preserve intent, tighten grounding and schema), then re-run and re-score.

## Related rules

Repository Cursor rules expand on ReAct steps, evidence rules, and aggregation behavior: [project-rules.mdc](../.cursor/rules/project-rules.mdc).
