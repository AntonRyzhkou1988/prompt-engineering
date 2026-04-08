# Metric: scoped mission outcome distribution (SMOD)

## Purpose

**Scoped mission outcome distribution (SMOD)** describes how mission outcomes are **split across `MissionStatus` categories** for an explicitly defined row population (for example a single rocket label). It is the quantitative backbone for extraction-and-visualization prompts such as **`questions/question_space_missions_extraction_pie.md`**, which ask for counts or shares plus a **Mermaid pie** chart grounded in evidence.

**Contrast with MSR:** **`metrics/space_missions_mission_success_rate.md`** defines **mission success rate (MSR)** as a single ratio—`Success` over all rows with non-empty `MissionStatus` in the chosen scope. **SMOD** reports the **full** categorical mix (`Success`, `Failure`, `Partial Failure`, `Prelaunch Failure`) so stakeholders see outcome balance, not only the success fraction.

## Source schema

Defined using columns documented in **`docs/applications/space_missions_data_dictionary.md`** (and the CSV companion when present):

| Field | Role in this metric |
| --- | --- |
| `MissionStatus` | **Primary.** Category label for each counted row; empty values are excluded from the outcome denominator unless you define a separate “unknown” bucket (not part of the canonical four-label mix). |
| `Rocket` | **Scope key (canonical prompt).** Exact string match on the dataset value; broader families are **not** implied (for example `Vostok` and `Vostok-2` are distinct labels). |
| `Mission`, `Date` | **Traceability.** Use in RAG answers to show which rows support each count; optional for batch SQL-style runs over the full file. |

Other fields (`Company`, `Location`, `Price`, …) may define **alternate** scopes; the same counting rules apply once the population is fixed.

## Definition

Let **P** be the set of rows that satisfy the stated **scope predicate** (for the reference prompt, **`Rocket` equals exactly `Vostok`**). Let **P′ ⊆ P** be rows where **`MissionStatus` is non-empty** after trimming.

For each canonical label **L** ∈ {`Success`, `Failure`, `Partial Failure`, `Prelaunch Failure`}:

- **Count(L)** = number of rows in **P′** whose `MissionStatus` equals **L** (exact string match).

**Total outcomes** = Σ_L **Count(L)** (equivalently |**P′**| if every non-empty status is one of the four labels).

**Share(L)** = **Count(L)** / **Total outcomes**, when **Total outcomes** > 0. Use proportions or percentages consistently in a given deliverable.

Rows in **P** with missing `MissionStatus` are **excluded** from **Total outcomes**; report how many were excluded if that number matters for interpretation.

## RAG and retrieval scope

When the metric is computed **from retrieved context only** (as in the reference question), **Count(L)** and **Share(L)** describe the **evidence-visible slice**, not necessarily the full CSV. The analyst must state **confidence** (`High` / `Medium` / `Low`) and whether retrieval might omit part of **P**. Do not assert full-dataset totals unless every required row appears in context.

## Visualization conformance (Mermaid pie)

For outputs that include a chart matching **`questions/question_space_missions_extraction_pie.md`**:

- Exactly **one** fenced **`mermaid`** **pie** block.
- Slice values are **positive** counts (or positive percentages derived from the same counts); categories with **zero** count are **omitted** from the pie and noted in prose.
- Labels should align with **`MissionStatus`** strings as in the data.

These rules are **presentation constraints** for that prompt; they do not change the mathematical definition of **Count(L)** and **Share(L)**.

## Data quality and caveats

- **Label semantics:** Same as MSR—the dictionary does not define operational criteria per mission.
- **Sparse evidence:** If fewer than two categories have positive **Count(L)** in context, the reference prompt asks to **omit** the pie and explain why.
- **Scope drift:** Mixing in rows outside **P** (for example wrong `Rocket` substring matches) invalidates the metric for the intended question.

## Minimal computation checklist

1. State the **scope predicate** explicitly (for the default teaching prompt: **`Rocket` = `Vostok`** exact match).
2. Restrict to rows in scope; drop or separately report rows with empty `MissionStatus` for the denominator.
3. Compute **Count(L)** for each of the four canonical `MissionStatus` values.
4. Compute **Total outcomes** and **Share(L)** if reporting percentages.
5. If using RAG, tie counts to **cited** rows or chunks and label retrieval uncertainty.
6. If a pie chart is required, verify Mermaid syntax, positive slices only, and consistency between table and chart.

## Files

| File | Role |
| --- | --- |
| `questions/question_space_missions_extraction_pie.md` | Reference user prompt (Vostok slice + pie). |
| `metrics/space_missions_mission_success_rate.md` | MSR definition (success-only ratio in the same scope). |
| `docs/applications/space_missions_data_dictionary.md` | Column meanings and `MissionStatus` vocabulary. |
