---
source: question_space_missions_extraction_pie.md
generated_utc: 2026-04-08T21:58:10.5275098Z
---

## Field selection and filters
- Selected fields: `Mission`, `Rocket`, `MissionStatus`, `Date` (to identify each counted row) [4]  
- Filter applied: `Mission` = `Vostok 1` (exact string match on the `Mission` column) [4]  
- No additional filters (e.g., date range, location) were applied because none were requested and none are justified by the retrieved context [4]  

## Data quality and confidence
- In the retrieved context, there is exactly one row where `Mission` is `Vostok 1`, which makes the outcome distribution extremely small-sample and not robust [4]  
- `MissionStatus` is present (non-empty) for the `Vostok 1` row in the retrieved context [4]  
- Confidence: **Low**, because the retrieved context may not include all rows from `space_missions.csv` that match `Mission` = `Vostok 1` (we cannot verify completeness from the provided excerpts) [4]  

## Extracted table

| MissionStatus | Count | Share of non-empty outcomes |
|---|---:|---:|
| Success | 1 [4] | 100% (1/1) [4] |

## Self-critique
- The count of `Success = 1` is directly supported by the single retrieved row with `Mission: Voskhod 1` and `MissionStatus: Success` [4]  
- The total non-empty outcomes (1) is directly supported because that same row has a non-empty `MissionStatus` [4]  
- I cannot confirm whether additional `Vostok 1` rows exist elsewhere in `space_missions.csv` because only excerpts of the CSV were provided (possible retrieval omission) [4]  
- Because only one `MissionStatus` category has positive evidence in-scope, I cannot produce positive counts for at least two categories as required for a pie chart [4]  

## Chart
Chart omitted because the retrieved context supports a positive count for only one outcome category (`Success`), not at least two categories. [4]

## Summary
Within the retrieved context, the only mission with `Mission` exactly `Vostok 1` has `MissionStatus` = `Success` on `1964-10-12`. [4]  
This yields an observed outcome mix of 100% Success (1 of 1 non-empty outcomes) for the retrieved slice. [4]  
What remains unknown is whether the retrieved context includes all `Vostok 1` rows from the full CSV (additional matching rows, if any, are missing from the provided excerpts). [4]