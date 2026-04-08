# RAG output metric: Grounded Response Pass Rate (GRPR)

## Purpose

This metric scores **generative answers** from the `Rag` sample pipeline (retrieval + chat) on how well they stay **tied to retrieved context** and avoid **fabricated specifics**. It is an **offline** checklist suitable for a **small gold set** and light-weight automation (substring checks) plus optional human review.

**Scope:** Questions are written against the indexed corpus under `documents/` (for example `space_missions.csv` and `metrics/space_missions_data_dictionary.md` when copied into the app `documents/` tree for a run). **Do not** treat this file as indexed unless it is under `DocumentsPath`.

## Definitions

### Criterion 1 — Grounding and non-fabrication (primary)

For each evaluation item, the model output **passes** if **all** of the following hold:

1. **Mode match**
   - If `expected_answer_mode` = `must_ground`: the answer must address the question using facts supportable from the corpus (evaluated via **required** substrings in the gold file—see below).
   - If `expected_answer_mode` = `must_abstain`: the answer must **not** invent missing details. **Automated pass:** (a) no hit on `forbidden_substrings`, and (b) at least one **abstention cue** from the allow-list appears, **unless** `required_substrings` is non-empty—then apply the same AND rule as `must_ground`. If `required_substrings` is empty and no abstention cue is present, flag for **human** scoring (model may still pass if it hedges without forbidden hits).

2. **Required cues (when non-empty)**  
   Split `required_substrings` on `|` (pipe). **Every** segment must appear as a substring in the model answer (**AND** semantics). Matching is **case-insensitive** unless `case_sensitive=1` on the row.

3. **Forbidden fabrications**  
   No token in `forbidden_substrings` may appear in the answer (case-insensitive), **except** when the row’s notes explicitly allow quoting a forbidden token inside a negation (rare—prefer restructuring gold).

**Abstention cue allow-list (for `must_abstain`):**  
`cannot`, `do not know`, `don't know`, `insufficient`, `not in the context`, `not provided`, `missing`, `unclear`, `not enough information`

### Criterion 2 — Qualitative fabrication risk (secondary)

Assign per response:

| Label | When |
| --- | --- |
| **Low** | Pass on Criterion 1; no forbidden hits; no precise numbers or names beyond gold. |
| **Medium** | Pass with weak hedging, or minor wording drift but no forbidden hit. |
| **High** | Any forbidden hit, or confident specifics with no corpus support (human judgment). |

This is **qualitative** and complements GRPR for **safety** review (high-risk outputs should not drive operational decisions).

### Aggregate metric: GRPR

For an evaluation run over \(N\) items with automated binary pass scores \(p_i \in \{0,1\}\):

\[
\text{GRPR} = \frac{1}{N} \sum_{i=1}^{N} p_i
\]

Report **count passed / N** and GRPR as a proportion. When some rows need human adjudication, report **automation coverage** (how many scored automatically) separately.

## Evaluation protocol (minimal)

1. Build the vector index from the same `DocumentsPath` and settings as production.
2. For each row in `rag_eval_space_missions_gold.csv`, send `question` as the user query (fixed `TopK` and temperature if you compare runs).
3. Store raw model output text.
4. Score with substring rules; escalate ambiguous `must_abstain` rows to a human rater.
5. Compute GRPR and optional risk histogram.

## Demonstration (worked example, \(N=5\))

Synthetic outputs for illustration only—the **gold file** is the source of truth for your real runs.

| item_id | Verdict (Criterion 1) | Risk | Rationale |
| --- | --- | --- | --- |
| eval-001 | Pass | Low | Contains required rocket name substring. |
| eval-002 | Pass | Low | Contains `Failure` and `Vanguard TV3`. |
| eval-003 | Fail | High | Answer claims a concrete **Price** value for Sputnik-1 (for example hits forbidden numeric pattern in the gold file) or omits both `Sputnik-1` and `Price` when denying the value. |
| eval-004 | Pass | Low | Contains `AMBA` and `Explorer 1`. |
| eval-005 | Pass | Medium | Abstains with “not in the provided context” and gives no crew number. |

For this demo slice: **4 / 5 passed** → **GRPR = 0.80** on \(N=5\).

## Limits and honesty

- **Substring checks** are brittle: they reward surface form, not deep entailment. Use them as a **regression guard**, not a full truth lab.
- **Retrieval errors** (wrong chunks) can produce “grounded” but **wrong** answers relative to the full CSV; extend the protocol with **retrieval labels** if you need to separate generation from retrieval quality.
- **Safety:** GRPR measures adherence to the gold checklist, not universal harm avoidance; pair with policy-specific tests for PII, medical, or compliance domains.

## Files

| File | Role |
| --- | --- |
| `metrics/rag_eval_space_missions_gold.csv` | Small evaluation set (questions + expected modes + substrings). |
| `metrics/rag_grounded_response_metric.md` | This specification and demo. |
