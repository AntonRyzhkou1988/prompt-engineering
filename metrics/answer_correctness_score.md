# Metric: Answer Correctness Score (ACS)

## Purpose

**Answer Correctness Score (ACS)** is a **quantitative, human-judged** quality metric for model outputs stored under **`answers/`**. It scores how well each answer addresses its paired business question and required structure (tables, charts, grounding), independent of the **automated substring checks** in **`docs/applications/rag/rag_eval_space_missions_gold.md`**.

Use ACS when you need a coarse **correctness** signal (full / partial / wrong) per question and an **overall** score across a run or benchmark set.

## Relationship to the RAG gold file

| Artifact | Role |
| --- | --- |
| **`docs/applications/rag/rag_eval_space_missions_gold.md`** | Checklist for **automated** checks: `required_substrings`, `forbidden_substrings`, `must_ground` / `must_abstain`. Fast, rule-based; OR-semantics on required tokens can be lenient. |
| **`metrics/answer_correctness_score.md` (this file)** | Rubric for **holistic** correctness: whether the answer is right on substance, complete on **key points**, and on-topic. Requires a human (or a separate LLM-as-judge with this rubric). |

ACS does **not** replace gold substring checks; it **complements** them for end-to-end answer quality.

## Mapping: gold items, questions, and answer files

Each **`eval-00x`** row in the gold set corresponds to one user-facing question and one answer artifact (same stem as **`questions/question_space_missions_*.md`**).

| item_id | Answer file (`answers/`) | Question theme |
| --- | --- | --- |
| eval-001 | `question_space_missions_extraction_pie.md` | `Mission` = `Vostok 1` — `MissionStatus` distribution + Mermaid pie |
| eval-002 | `question_space_missions_company_share.md` | Row share by `Company` — table + pie |
| eval-003 | `question_space_missions_rocket_row_share.md` | Row share by `Rocket` — table + pie |
| eval-004 | `question_space_missions_location_country_share.md` | Derived country from `Location` (last-segment rule) — table + pie |
| eval-005 | `question_space_missions_mission_status_share.md` | Row share by `MissionStatus` — table + pie |

Scores are assigned **per answer file** (one ACS score per row in the table above). If you evaluate a different corpus, keep the same **one score per evaluated answer** rule.

## Definition

### Per-question score

For each evaluated answer, assign a single score **s** ∈ **{0, 0.5, 1}**:

| s | Meaning |
| ---: | --- |
| **1** | **Correct** — The answer is correct and **covers all key points** required by the prompt (grounding, scope, requested artifacts, and major caveats such as retrieval limits where relevant). |
| **0.5** | **Partially correct** — Some **key points are missing**, incomplete, or **slightly wrong**, but the answer is still substantially on-task (not nonsense). |
| **0** | **Mostly incorrect or off-topic** — Wrong conclusions, wrong scope, fabricated facts contradicted by evidence, or failure to address the question. |

**Key points** should be taken from the corresponding **`questions/question_space_missions_*.md`** file: required sections, bucketing rules, table + chart requirements, and honest handling of partial retrieval.

### Overall score

Let **s1, …, sn** be the per-question scores for the **n** answers in the evaluated set (for the default space-missions benchmark, **n = 5** when all five answer files are scored).

**Overall ACS** = arithmetic mean of per-question scores:

**ACS_overall** = (**s1** + **s2** + … + **sn**) / **n**

**Range:** **0** to **1** inclusive. When **n = 5** and each **si** ∈ {0, 0.5, 1}, the mean advances in steps of **0.1**.

Example: scores **(1, 1, 0.5, 1, 1)** → **ACS_overall = 0.9** (four correct, one partial).

## Computed scores (checked-in `answers/`)

Scores below apply the rubric in **Definition** to the five files under **`answers/question_space_missions_*.md`**. Each file’s YAML **`generated_utc`** is **2026-04-09** (times vary by run). They are **illustrative** for this snapshot; re-score if answers change.

| item_id | Answer file | ACS (si) | Rationale |
| --- | --- | ---: | --- |
| eval-001 | `question_space_missions_extraction_pie.md` | **1** | Scopes `Mission` = `Vostok 1` on retrieved rows; one matching row with `MissionStatus` = `Success`; extracted table across the four outcome literals; Mermaid pie for the non-zero slice; self-critique addresses evidence depth and retrieval scope. |
| eval-002 | `question_space_missions_company_share.md` | **1** | 200-row slice; verbatim `Company` bucketing; full table with shares; top-11 + `Other` Mermaid pie; self-critique covers dominance, temporal clustering, and non-generalizability. |
| eval-003 | `question_space_missions_rocket_row_share.md` | **1** | Verbatim `Rocket` bucketing; full table (70 buckets, total **200** rows); Mermaid pie with top 11 + **`Other (59 buckets)`** (11 + 127 = **200**); self-critique notes label fragmentation and chart rollup without omitting the chart. |
| eval-004 | `question_space_missions_location_country_share.md` | **1** | Last-segment country rule with examples; extracted table **11 buckets totaling 200** rows (counts sum to denominator); Mermaid pie uses same percentages; self-critique flags **`New Mexico`** as a parser artifact and confirms chart–table alignment. |
| eval-005 | `question_space_missions_mission_status_share.md` | **1** | Four `MissionStatus` buckets over **200** rows (137 + 55 + 7 + 1); table total matches slice; Mermaid pie with count-based slices for all positive buckets; self-critique covers rare bucket noise and retrieval bias. |

**Overall ACS** = (**1** + **1** + **1** + **1** + **1**) / **5** = **1.0**.

## Type

- **Quantitative** — Numeric output on a discrete three-level scale per question.
- **Aggregation** — Mean over questions for a single run or comparison across runs.

## Usage notes

- **Retrieval-dependent tasks:** An answer that correctly states that evidence is missing (per prompt) may still score **1** if that is the right conclusion and required sections are satisfied; an answer that invents slice statistics should score **0** or **0.5** depending on severity.
- **Consistency:** Use the same judge and the same **key-point checklist** derived from the question file when comparing models or prompts.
- **Calibration:** Document any team-specific examples of **0 / 0.5 / 1** for borderline cases next to this metric or in evaluation logs.

## Files

| File | Role |
| --- | --- |
| `metrics/answer_correctness_score.md` | This metric definition (ACS). |
| `docs/applications/rag/rag_eval_space_missions_gold.md` | Automated substring / mode checks paired with the same five questions. |
| `questions/question_space_missions_*.md` | Source prompts; define **key points** for scoring. |
| `answers/question_space_missions_*.md` | Model outputs to score with ACS. |
