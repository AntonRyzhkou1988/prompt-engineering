# RAG evaluation gold set — space missions

Structured gold items for scoring RAG answers against `dataset/space_missions.csv`. Each item lists required and forbidden substring checks used by automated evaluators.

| item_id | question | expected_answer_mode | required_substrings | forbidden_substrings | case_sensitive | notes |
| --- | --- | --- | --- | --- | --- | --- |
| eval-001 | According to the indexed space missions data, which rocket launched the mission named Sputnik-1? | must_ground | `Sputnik 8K71PS` \| `Sputnik-1` | — | no | Requires vehicle and mission labels as in the CSV row for Sputnik-1. |
| eval-002 | What was the MissionStatus value for the Vanguard TV3 mission in the dataset? | must_ground | `Failure` \| `Vanguard TV3` | — | no | Exact status label from MissionStatus. |
| eval-003 | For the Sputnik-1 row in the dataset, is there a non-empty Price value recorded? | must_ground | `Sputnik-1` \| `Price` | `1160` \| `1,160` | no | Price is empty for that row; answer should indicate absence without borrowing Apollo 11's price (forbidden substrings are common hallucinated numerics for this row). |
| eval-004 | Which Company value is recorded for the Explorer 1 mission? | must_ground | `AMBA` \| `Explorer 1` | — | no | — |
| eval-005 | How many astronauts flew on Apollo 11 according to the space missions table? | must_abstain | — | `Neil` \| `Buzz` \| `Michael` \| `Armstrong` \| `Aldrin` \| `Collins` \| `three astronauts` \| `3 astronauts` | no | Crew count and names are not columns; astronaut names or explicit crew counts are treated as unsupported fabrications for automated scoring. |
| eval-006 | What Date is recorded for the Apollo 11 mission in the dataset? | must_ground | `1969` \| `Apollo 11` | — | no | Require year and mission label; full ISO date is in CSV as 1969-07-16—allow manual pass if formatting differs but year is correct. |
| eval-007 | What MissionStatus is recorded for the Explorer 2 mission in the dataset? | must_ground | `Explorer 2` \| `Failure` | — | no | Row matches Juno I / Explorer 2 with MissionStatus Failure. |
| eval-008 | What Location string is recorded for the first row with Mission Sputnik-2? | must_ground | `Sputnik-2` \| `Baikonur` | — | no | — |
| eval-009 | What was the first year a human landed on the Moon, based solely on this dataset? | must_ground | `1969` \| `Apollo 11` | — | no | Evidence row exists with Date 1969-07-16; answer must not claim a different year as dataset-grounded fact. |
| eval-010 | What is the total worldwide count of launches represented in this CSV? | must_abstain | — | `4630` \| `4631` \| `4629` \| `5000` \| `10000` | no | Gold does not embed row counts; invented totals fail. Pass if answer defers or proposes how to compute without asserting a false global count. |

## Column semantics

- **expected_answer_mode**: `must_ground` — answer must be supported by retrieved/tabular evidence; `must_abstain` — answer must not assert unsupported facts (often defer or explain limits).
- **required_substrings**: Any listed alternative may satisfy the check (OR across `\|`-separated tokens), matching the original CSV pipe convention.
- **forbidden_substrings**: Hitting any listed substring typically fails the item (OR across tokens).
- **case_sensitive**: `no` corresponds to CSV value `0`.
