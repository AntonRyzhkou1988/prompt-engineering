# RAG evaluation gold set — space missions

**[Repository README — Documentation](../../../README.md#documentation)** · [Data dictionary](space_missions_data_dictionary.md)

Structured gold items for scoring RAG answers against `dataset/space_missions.csv`. Each item lists required and forbidden substring checks used by automated evaluators. Gold rows mirror the user-facing questions in `questions/question_space_missions_*.md`.

| item_id | question | expected_answer_mode | required_substrings | forbidden_substrings | case_sensitive | notes |
| --- | --- | --- | --- | --- | --- | --- |
| eval-001 | Among space missions in the **retrieved context** where **`Mission` is exactly `Vostok 1`**, what is the **distribution of mission outcomes** (`MissionStatus`), and how does that mix look in a **single Mermaid pie chart** with counts or shares clearly derived from those rows? | must_ground | `Vostok 1` \| `Success` \| `Vostok` \| `MissionStatus` \| `mermaid` \| `pie` | — | no | Full CSV has a single row with `Mission` = `Vostok 1` (`Rocket` = `Vostok`, `MissionStatus` = `Success`, `Date` = `1961-04-12`); slice mixes depend on retrieval. |
| eval-002 | Using only **retrieved** rows from the space missions data, what **share of missions (rows)** does each **company name** (`Company` column) represent in the slice? Return **counts and percentages in a table** and the **same** distribution in a **single Mermaid pie chart**. | must_ground | `Company` \| `Extracted table` \| `%` \| `mermaid` \| `pie` | `4630` \| `4631` \| `4629` \| `5000` \| `10000` | no | Percentages and top operators depend on retrieved slice; do not assert a full-CSV row total as if it were the slice denominator. |
| eval-003 | Using only **retrieved** rows from the space missions data, what **share of missions (rows)** uses each **rocket name** (`Rocket` column) in the slice? Return **counts and percentages in a table** and the **same** distribution in a **single Mermaid pie chart**. | must_ground | `Rocket` \| `Extracted table` \| `%` \| `mermaid` \| `pie` | `4630` \| `4631` \| `4629` \| `5000` \| `10000` | no | Same slice caveat as eval-002. |
| eval-004 | Using only **retrieved** rows from the space missions data, and interpreting geography via **`Location`** as defined in **`docs/applications/rag/space_missions_data_dictionary.md`**, what **percentage of missions (rows)** fall under each **derived launch country** when you apply the **last comma-separated segment** rule? Return **counts and percentages in a table** and show the **same** distribution in a **single Mermaid pie chart**. | must_ground | `Location` \| `Extracted table` \| `%` \| `mermaid` \| `pie` \| `country` | `4630` \| `4631` \| `4629` \| `5000` \| `10000` | no | Derived-country buckets follow the prompt’s parser; shares depend on retrieval. |
| eval-005 | Using only **retrieved** rows from the space missions data, what **share of missions (rows)** falls into each **mission outcome** (`MissionStatus`)? Return **counts and percentages in a table** and the **same** distribution in a **single Mermaid pie chart**. | must_ground | `MissionStatus` \| `Extracted table` \| `%` \| `mermaid` \| `pie` | `4630` \| `4631` \| `4629` \| `5000` \| `10000` | no | Outcome mix depends on retrieved slice; do not treat as guaranteed full-dataset statistics. |

## Column semantics

- **expected_answer_mode**: `must_ground` — answer must be supported by retrieved/tabular evidence; `must_abstain` — answer must not assert unsupported facts (often defer or explain limits).
- **required_substrings**: Any listed alternative may satisfy the check (OR across `\|`-separated tokens), matching the original CSV pipe convention.
- **forbidden_substrings**: Hitting any listed substring typically fails the item (OR across tokens).
- **case_sensitive**: `no` corresponds to CSV value `0`.

## See also

- [RAG guide](rag.md) · [Getting started](../../getting-started.md) · [Answer Correctness Score](../../../metrics/answer_correctness_score.md)
