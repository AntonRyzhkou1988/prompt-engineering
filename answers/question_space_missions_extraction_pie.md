---
source: question_space_missions_extraction_pie.md
generated_utc: 2026-04-08T21:37:09.3337256Z
---

## Field selection and filters
- Filter: `Mission` equals exactly `Vostok 1` (string match on the `Mission` column) — **no matching rows found in retrieved context** [4].
- Selected fields intended for identification and counting: `Mission`, `Rocket`, `MissionStatus`, `Date` (but none are available for `Vostok 1` in the retrieved rows) [4].
- No additional filters (date range, location, rocket) applied because the required `Mission = Vostok 1` rows are missing from the retrieved context [4].

## Data quality and confidence
- The retrieved context contains a row for `Voskhod 1` (note spelling difference) but **not** `Vostok 1`, so the required slice is empty [4].
- Because there are zero retrieved rows matching `Mission = Vostok 1`, outcome counts and percentages for that mission are not computable from the provided evidence [4].
- Confidence: **Low** (missing the key row(s) needed for the requested filter) [4].

## Extracted table

| MissionStatus | Count | Share of non-empty outcomes |
|---|---:|---|
| Success | 0 | not computable (no `Mission = Vostok 1` rows retrieved) [4] |
| Failure | 0 | not computable (no `Mission = Vostok 1` rows retrieved) [4] |
| Partial Failure | 0 | not computable (no `Mission = Vostok 1` rows retrieved) [4] |
| Prelaunch Failure | 0 | not computable (no `Mission = Vostok 1` rows retrieved) [4] |

## Self-critique
- I cannot cite any row where `Mission` is exactly `Vostok 1`; therefore, all category counts for that slice are unsupported beyond stating “no matches in retrieved context” [4].
- The retrieved rows may omit the `Vostok 1` record(s), so this result should not be interpreted as evidence that `Vostok 1` is absent from the full CSV [4].
- To answer, the missing piece is at least one retrieved row where `Mission` equals exactly `Vostok 1`, including its `MissionStatus` [4].

## Chart
Chart omitted because there are **no positive counts** from retrieved rows matching `Mission = Vostok 1`, and Mermaid pie slices require positive numbers [4].

## Summary
No missions with `Mission` exactly `Vostok 1` appear in the retrieved context, so the mission outcome distribution cannot be computed from the provided rows [4]. The context does include `Voskhod 1`, which is a different `Mission` value and out of scope for this request [4]. What remains unknown is the `MissionStatus` (and even presence) of `Vostok 1` in the dataset because the relevant row(s) were not retrieved [4].