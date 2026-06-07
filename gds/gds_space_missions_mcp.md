# Golden Data Set — Space Missions MCP (Chatbot)

**[Repository README — Documentation](../README.md#documentation)** · [MCP questions](../docs/applications/space-missions-mcp/mcp-questions.md) · [Tool reference](../docs/applications/space-missions-mcp/space-missions-mcp-tools.md)

Ten curated natural-language questions for evaluating the **Chatbot** hybrid agent (`SpaceMissionsAgentService`: RAG + MCP tool loop). Each item includes expected MCP tools, verification criteria, and MCP-derived ground truth under `ground-truth/`.

Machine-readable source: [`manifest.json`](manifest.json).

| item_id | source # | question | expected_tools | ground_truth_ref |
| --- | --- | --- | --- | --- |
| gds-001 | 1 | What columns are in the space missions dataset? | `get_space_missions_schema` | `ground-truth/gds-001.json` |
| gds-002 | 7 | Give me a high-level overview of the space missions dataset. | `get_space_missions_summary` | `ground-truth/gds-002.json` |
| gds-003 | 14 | What rocket names contain "Falcon"? | `list_space_mission_distinct_values` | `ground-truth/gds-003.json` |
| gds-004 | 19 | Show me SpaceX launches from 2020 onward. | `filter_space_missions` | `ground-truth/gds-004.json` |
| gds-005 | 29 | How many SpaceX launches are in the dataset? | `count_space_missions` | `ground-truth/gds-005.json` |
| gds-006 | 35 | Break down all missions by MissionStatus with counts and percentages. | `aggregate_space_missions` or `get_space_missions_summary` | `ground-truth/gds-006.json` |
| gds-007 | 43 | What percentage of launches are from the USA? | `aggregate_space_missions_by_launch_country` | `ground-truth/gds-007.json` |
| gds-008 | 51 | What is SpaceX's mission success rate? | `compute_space_mission_success_rate` | `ground-truth/gds-008.json` |
| gds-009 | 58 | What is SpaceX's success rate since 2020, and show a few example rows? | `compute_space_mission_success_rate`, `filter_space_missions` | `ground-truth/gds-009.json` |
| gds-010 | 66 | Show me all 5,000 SpaceX launches. | `count_space_missions` or `filter_space_missions` (any) | `ground-truth/gds-010.json` |

Items **gds-006** and **gds-010** use `expectedToolsMode: "any"` in [`manifest.json`](manifest.json) — at least one listed tool must be invoked.

## Key ground-truth anchors (full CSV)

| Fact | Value |
| --- | --- |
| Dataset rows | 4630 |
| SpaceX launch count | 182 |
| SpaceX launches from 2020+ | 96 |
| USA launch share | 31.68% (1467 / 4630) |
| Filter row cap | 200 per call |

Ground truth last regenerated: **2026-06-07** (MCP tool calls, no LLM).

## Last agent + judge run

Integration test artifacts under `answers/` and `judge/` from **2026-06-07** (all items pass ACS ≥ 0.5 and tool routing):

| item_id | tools invoked | ACS | routing | judge |
| --- | --- | --- | --- | --- |
| gds-001 | `get_space_missions_schema` | 0.5 | pass | pass |
| gds-002 | `get_space_missions_summary` | 1 | pass | pass |
| gds-003 | `list_space_mission_distinct_values` | 1 | pass | pass |
| gds-004 | `filter_space_missions` | 1 | pass | pass |
| gds-005 | `count_space_missions` | 1 | pass | pass |
| gds-006 | `get_space_missions_summary` | 1 | pass | pass |
| gds-007 | `aggregate_space_missions_by_launch_country` | 1 | pass | pass |
| gds-008 | `compute_space_mission_success_rate` | 1 | pass | pass |
| gds-009 | `compute_space_mission_success_rate`, `filter_space_missions` | 1 | pass | pass |
| gds-010 | `count_space_missions` | 1 | pass | pass |

Regenerate ground truth after dataset or MCP tool changes:

```powershell
dotnet test tests/Chatbot.Tests --filter "FullyQualifiedName~GdsGroundTruth"
```

## Agent + LLM judge run

Explicit integration test (requires LLM API key and built MCP server):

```powershell
dotnet test tests/Chatbot.Tests --filter "FullyQualifiedName~SpaceMissionsGdsIntegration" -- NUnit.ExplicitMode=Explicit
```

Outputs:

- `answers/{item_id}.md` — agent `RunAsync` answers (EchoBot path)
- `judge/{item_id}.json` — LLM-as-judge score (0 / 0.5 / 1), pass/fail, reasoning

## See also

- [RAG eval gold](../docs/applications/rag/rag_eval_space_missions_gold.md)
- [Answer Correctness Score](../metrics/answer_correctness_score.md)
- [Space Missions MCP server](../docs/applications/space-missions-mcp/space-missions-mcp.md)
