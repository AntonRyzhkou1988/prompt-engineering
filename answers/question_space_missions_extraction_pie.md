---
source: question_space_missions_extraction_pie.md
generated_utc: 2026-04-08T21:42:22.1104643Z
---

## Field selection and filters
- Selected fields: `Mission`, `Rocket`, `MissionStatus`, `Date` (to identify each counted row) [4]  
- Filter applied: `Mission` = `Vostok 1` (exact string match on the `Mission` column)  
- No additional filters (e.g., date range, location, rocket status) were applied because none are justified by the question or provided context [4]

## Data quality and confidence
- In the retrieved context, there is exactly one row where `Mission` is `Vostok 1`, so the outcome distribution is based on a single observation [4]
- `MissionStatus` is present (non-empty) for that row (`Success`) [4]
- Confidence: **Medium** (counts are clear for the retrieved row, but the context may not include all `Vostok 1` rows if more exist elsewhere in the full CSV) [4]

## Extracted table

| MissionStatus | Count | Share of non-empty outcomes |
|---|---:|---:|
| Success | 1 [4] | 100% (1/1) [4] |

## Self-critique
- The `Success` count is directly supported by the single retrieved row with `Mission` = `Vostok 1` and `MissionStatus` = `Success` [4]
- No evidence in the retrieved context supports any `Failure`, `Partial Failure`, or `Prelaunch Failure` outcomes for `Vostok 1` [4]
- The retrieved context may omit additional `Vostok 1` entries (if they exist elsewhere in `space_missions.csv`), so the distribution may not reflect the full dataset [4]
- The request references a data dictionary and metric definitions, but those files are not included in the provided context, so their formal definitions cannot be verified here (missing: `metrics/space_missions_data_dictionary.md`, `metrics/space_missions_scoped_outcome_distribution.md`, `metrics/space_missions_mission_success_rate.md`)

## Chart
Omitted: the retrieved evidence supports a positive count for only one outcome category (`Success`), not at least two categories as required. The other categories have zero evidence-supported counts in the retrieved context [4].

## Summary
Within the retrieved rows, there is one mission with `Mission` exactly `Vostok 1`, and its `MissionStatus` is `Success` [4]. That yields a 100% success share among non-empty outcomes in this retrieved slice [4]. It is unknown whether additional `Vostok 1` rows exist in the full CSV because only a subset of rows was provided (missing: complete set of rows where `Mission` = `Vostok 1`).