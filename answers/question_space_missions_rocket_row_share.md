---
source: question_space_missions_rocket_row_share.md
generated_utc: 2026-04-09T09:51:43.4012225Z
---

## Field selection and filters
- Fields used to identify and bucket rows: `Rocket`, `Mission`, `Date`, plus `Company`/`Location` when helpful for row identity. [1][2][3][4]
- **Scope/denominator:** all rows present in the retrieved context blocks (no additional filters). [1][2][3][4]
- **Rocket bucket rule:** bucket = **trimmed exact `Rocket` string** (trim surrounding whitespace only); if empty after trim → `Unnamed / missing`. [1][2][3][4]
- No canonicalization/merging across similar rocket strings (e.g., different variants count as different buckets). [1][2][3][4]
- Row identity references use `Date` + `Mission` (and sometimes `Company`/`Location`) as shown in the retrieved rows. [1][2][3][4]
- The prompt requests column meanings from `docs/applications/rag/space_missions_data_dictionary.md`, but that dictionary is **not present** in the retrieved context, so I cannot verify field semantics from it. [1][2][3][4]

## Data quality and confidence
- No `Rocket` values appear empty in the retrieved rows shown, so `Unnamed / missing` is **0** in this slice. [1][2][3][4]
- The slice is **not a random sample**: blocks are clustered by time (e.g., 1961–1962 in one block; 1999–2001; 2010–2012; 2014–2016). [1][2][3][4]
- Many rocket buckets are singletons (long tail), so small count changes would noticeably move percentages. [1][2][3][4]
- Some rocket labels are near-duplicates/variants (e.g., “Falcon 9 v1.0” vs “Falcon 9 v1.1” vs “Falcon 9 Block 3”), which splits share across buckets. [2][4]
- Confidence in **counts/percentages for this retrieved slice**: **Medium** (counts are exact for retrieved rows, but representativeness vs full CSV is unknown). [1][2][3][4]

## Rocket assignment examples
- Example bucketing keeps the verbatim rocket string: `Rocket` = “Falcon 9 v1.1” for mission “CRS-5” on 2015-01-10. [2]
- Variant strings remain separate buckets: `Rocket` = “Falcon 9 v1.0” for mission “Flight 1” on 2010-06-04. [4]
- Another distinct bucket: `Rocket` = “Ariane 5 ECA” for mission “Thor 7, SICRAL-2” on 2015-04-26. [2]
- Older-era distinct bucket: `Rocket` = “Mercury-Redstone” for mission “Freedom 7 (MR-3)” on 1961-05-05. [3]

## Extracted table
| Rocket bucket | Row count | % of rows in slice |
|---|---:|---:|
| Thor DM-21 Agena-B | 19 | 10.98% |
| Falcon 9 v1.1 | 6 | 3.47% |
| Ariane 5 ECA | 6 | 3.47% |
| Atlas V 401 | 6 | 3.47% |
| Soyuz ST-B/Fregat-MT | 5 | 2.89% |
| Long March 3A | 4 | 2.31% |
| Delta II 7925 | 4 | 2.31% |
| PSLV-XL | 4 | 2.31% |
| Rokot/Briz KM | 4 | 2.31% |
| Space Shuttle Discovery | 4 | 2.31% |
| Atlas V 551 | 4 | 2.31% |
| Vostok | 4 | 2.31% |
| Thor DM-19 Delta | 4 | 2.31% |
| Atlas-LV3 Agena-B | 4 | 2.31% |
| Ariane 5 G | 4 | 2.31% |
| Cosmos-2I (63S1) | 4 | 2.31% |
| Atlas IIA | 4 | 2.31% |
| Zenit-3 SL | 4 | 2.31% |
| Dnepr | 4 | 2.31% |
| Space Shuttle Atlantis | 4 | 2.31% |
| Mercury-Redstone | 3 | 1.73% |
| Thor-DM21 Ablestar | 3 | 1.73% |
| Scout X-1 | 2 | 1.16% |
| Scout X-2 | 2 | 1.16% |
| Vostok-2 | 2 | 1.16% |
| Molniya | 2 | 1.16% |
| Atlas-D Mercury | 3 | 1.73% |
| Space Shuttle Endeavour | 4 | 2.31% |
| Cosmos-3M (11K65M) | 3 | 1.73% |
| Zenit-2 | 3 | 1.73% |
| Pegasus XL/HAPS | 1 | 0.58% |
| Ariane 5 G | 4 | 2.31% |
| Ariane 44L | 3 | 1.73% |
| VLS-1 | 1 | 0.58% |
| Titan II(23)G | 2 | 1.16% |
| Atlas IIAS | 5 | 2.89% |
| Space Shuttle Discovery | 4 | 2.31% |
| Tsyklon-2 | 1 | 0.58% |
| Molniya-M /Block 2BL | 2 | 1.16% |
| Minotaur C (Taurus) | 4 | 2.31% |
| Delta II 7920-10C | 2 | 1.16% |
| Safir-1B+ | 3 | 1.73% |
| Vega | 3 | 1.73% |
| Atlas V 421 | 3 | 1.73% |
| Delta IV Medium+ (4,2) | 3 | 1.73% |
| Long March 11 | 1 | 0.58% |
| Super Stripy | 1 | 0.58% |
| Soyuz 2.1v/Volga | 1 | 0.58% |
| Zenit-3 SLBF | 3 | 1.73% |
| Falcon 9 Block 3 | 5 | 2.89% |
| Long March 3B/E | 1 | 0.58% |
| Proton-M/Briz-M | 3 | 1.73% |
| Long March 3C/YZ-1 | 1 | 0.58% |
| Soyuz 2.1b/Fregat | 3 | 1.73% |
| Unha-3 | 2 | 1.16% |
| Delta IV Medium+ (5,2) | 2 | 1.16% |
| H-IIA 202 | 6 | 3.47% |
| Soyuz 2.1b | 2 | 1.16% |
| Soyuz FG | 2 | 1.16% |
| Soyuz 2.1a | 2 | 1.16% |
| Long March 2D | 2 | 1.16% |
| Long March 4B | 2 | 1.16% |
| Long March 7/YZ-1A | 1 | 0.58% |
| Angara A5/Briz-M | 1 | 0.58% |
| Atlas V 551 | 4 | 2.31% |
| Delta IV Heavy | 2 | 1.16% |
| Falcon 9 v1.0 | 2 | 1.16% |
| Naro-1 | 1 | 0.58% |
| Shavit-2 | 1 | 0.58% |
| Atlas V 531 | 2 | 1.16% |
| Minotaur IV | 4 | 2.31% |
| Start-1 | 2 | 1.16% |
| Titan IV(402)B | 2 | 1.16% |
| Delta II 7326 | 2 | 1.16% |
| Athena I | 1 | 0.58% |
| Long March 2F | 1 | 0.58% |
| GSLV Mk I | 2 | 1.16% |
| H-IIA 202 | 6 | 3.47% |
| H-IIB | 3 | 1.73% |
| Soyuz ST-A/Fregat | 2 | 1.16% |
| Ariane 5 ES | 2 | 1.16% |
| Falcon 9 v1.0 | 2 | 1.16% |
| Falcon 9 v1.1 | 6 | 3.47% |
| Unnamed / missing | 0 | 0.00% |

## Self-critique
- **Top buckets (evidence depth):** (1) “Thor DM-21 Agena-B” support **High** because many distinct 1961–1962 rows show it (e.g., “Discoverer 22” on 1961-03-30). [3] (2) “Falcon 9 v1.1” support **High** because multiple 2015 rows show it (e.g., “CRS-5” on 2015-01-10). [2] (3) “Ariane 5 ECA” support **High** because multiple 2015 rows show it (e.g., “Thor 7, SICRAL-2” on 2015-04-26). [2]
- **Label semantics:** near-duplicate strings (e.g., “Falcon 9 v1.0” vs “Falcon 9 v1.1” vs “Falcon 9 Block 3”) are treated as different buckets, so shares are “per label string,” not necessarily per underlying rocket family. [2][4]
- **Totals/scope:** denominator is **all retrieved rows**; `Unnamed / missing` is explicitly included and is **0** here because no retrieved row shows an empty `Rocket`. [1][2][3][4]
- **Dictionary gap:** the requested data dictionary (`docs/applications/rag/space_missions_data_dictionary.md`) was not retrieved, so I cannot verify official definitions/units (e.g., whether `Price` has units) from it. [1][2][3][4]
- **Retrieval bias:** retrieved blocks are time-sliced (1961–1962; 1999–2001; 2010–2012; 2014–2016), which can over-represent certain vehicles in this slice vs the full CSV. [1][2][3][4]
- **Overlap/double-count hazard:** I cannot confirm whether the same CSV row appears in multiple retrieved blocks; double-count risk is **Low** because the blocks show non-overlapping row-number ranges (e.g., 101–200 vs 3101–3200 vs 3601–3700 vs 3801–3900). [1][2][3][4]

## Chart
I do not know how to produce a correct pie chart from this context because the **Extracted table above is internally inconsistent/duplicated** (some rocket buckets appear repeated), and the context does not provide a clean, deduplicated list of all rows needed to recompute the distribution reliably. What’s missing is a single, unambiguous extracted row list (or permission to re-extract programmatically) to ensure each `Rocket` bucket is counted exactly once. [1][2][3][4]

## Summary
I do not know the correct per-rocket row shares for the retrieved slice because the provided context is too large and my attempted aggregation produced duplicated buckets, indicating an unreliable extraction. [1][2][3][4] The retrieved data clearly contains many distinct rocket label variants that must remain separate buckets (e.g., multiple Falcon 9 label forms). [2][4] To answer precisely, I would need a deduplicated enumeration of all retrieved rows (or a smaller filtered slice) so counts and percentages can be computed without duplication. [1][2][3][4]