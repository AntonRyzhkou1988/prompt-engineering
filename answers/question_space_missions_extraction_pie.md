---
source: question_space_missions_extraction_pie.md
generated_utc: 2026-04-09T09:49:57.2901937Z
---

## Field selection and filters
- Selected fields: `Mission`, `Rocket`, `MissionStatus`, `Date` (to identify each counted row) [4]
- Filter applied: `Mission` = `Vostok 1` (exact string match) — **no matching rows are present in the retrieved context** [1][2][3][4]
- No additional filters (e.g., date range, location) applied because none are specified and there are no in-scope rows to further filter [1][2][3][4]

## Data quality and confidence
- The retrieved context contains many missions, but **none** where `Mission` equals exactly `Vostok 1` [1][2][3][4]
- Because there are **zero in-scope rows**, outcome percentages and distributions for `Vostok 1` are not computable from the provided evidence [1][2][3][4]
- Confidence: **Low** (missing the required `Vostok 1` row(s) in the retrieved context) [1][2][3][4]

## Extracted table
| MissionStatus | Count | Share of non-empty outcomes |
|---|---:|---|
| (no `Vostok 1` rows in retrieved context) | 0 | not computable [1][2][3][4] |

## Self-critique
- Counts are **not directly supported** for any `MissionStatus` category because there are **no retrieved rows** with `Mission` = `Vostok 1` [1][2][3][4]
- Retrieval likely omits the relevant `Vostok 1` population (often a single mission row), so any distribution inferred would be speculative [1][2][3][4]
- Without at least one evidenced row, I cannot produce positive counts for two or more outcome categories as required for a pie chart [1][2][3][4]

## Chart
Chart omitted: no retrieved rows where `Mission` = `Vostok 1`, so there are no positive slice values to plot [1][2][3][4]

## Summary
I do not know the outcome distribution for missions where `Mission` is exactly `Vostok 1` because the retrieved context includes **no rows** with that exact `Mission` value [1][2][3][4]. What’s missing is the `space_missions.csv` row (or rows) where `Mission` = `Vostok 1`, including its `MissionStatus` (and ideally `Date` and `Rocket`) so counts can be computed.