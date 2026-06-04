# User questions mapped to Space Missions MCP tools

**[Repository README — Documentation](../../../README.md#documentation)** · [Tool reference](space-missions-mcp-tools.md) · [Server guide](space-missions-mcp.md)

Natural-language questions a user might ask in **Chatbot** (or when testing the MCP host), grouped by the tool(s) that should answer them. Combined tool chains are noted where one question may need multiple calls.

---

## `get_space_missions_schema`

Questions about **what data exists** and **how to interpret columns** before querying.

| # | User question |
|---|----------------|
| 1 | What columns are in the space missions dataset? |
| 2 | How many rows are in the full space missions CSV? |
| 3 | What is the earliest and latest launch date in the dataset? |
| 4 | What are the valid values for mission outcome (`MissionStatus`)? |
| 5 | What does `RocketStatus` mean in this dataset? |
| 6 | Which fields can I use to filter launches? |

---

## `get_space_missions_summary`

Questions about **whole-dataset** statistics (no filters).

| # | User question |
|---|----------------|
| 7 | Give me a high-level overview of the space missions dataset. |
| 8 | How are missions distributed by outcome across the entire dataset? |
| 9 | What percentage of all missions were successful? (outcome mix, not MSR) |
| 10 | How many launches are recorded from 1957 through 2022 in total? |
| 11 | What is the overall success vs failure breakdown for all rows? |

---

## `list_space_mission_distinct_values`

Questions about **discovering values** before exact filters.

| # | User question |
|---|----------------|
| 12 | Which companies appear in the dataset? |
| 13 | List all distinct mission status values. |
| 14 | What rocket names contain "Falcon"? |
| 15 | Which companies have names containing "Space"? |
| 16 | What are the distinct launch locations that mention "Baikonur"? |
| 17 | What mission names exist for NASA launches since 2010? |
| 18 | How many unique rockets are there in the full dataset? |

---

## `filter_space_missions`

Questions needing **specific rows** as evidence (dates, missions, sites).

| # | User question |
|---|----------------|
| 19 | Show me SpaceX launches from 2020 onward. |
| 20 | List failed missions at Kennedy Space Center. |
| 21 | What missions used the Falcon 9 rocket? |
| 22 | Give me the first 50 launches in the dataset by date. |
| 23 | Show NASA missions between 1960 and 1970. |
| 24 | Find missions where the mission name contains "Starlink". |
| 25 | What are the details of the Demo-2 mission? |
| 26 | Show me the next page of SpaceX results after the first 200. |
| 27 | List all prelaunch failures in the dataset. |
| 28 | Which launches from French Guiana used Ariane rockets? |

---

## `count_space_missions`

Questions that only need a **number**, not row details.

| # | User question |
|---|----------------|
| 29 | How many SpaceX launches are in the dataset? |
| 30 | How many missions failed in the 1960s? |
| 31 | Count launches from Russia or the USSR (company contains "Roscosmos" or "USSR"). |
| 32 | How many active rockets (`RocketStatus`) are associated with at least one launch? |
| 33 | How many missions launched from locations containing "Florida"? |
| 34 | How many partial failures occurred after 2000? |

---

## `aggregate_space_missions`

Questions about **distributions by a column** (company, rocket, status, etc.).

| # | User question |
|---|----------------|
| 35 | Break down all missions by `MissionStatus` with counts and percentages. |
| 36 | Which companies launched the most missions? |
| 37 | What is the share of launches by rocket name? |
| 38 | How are launches distributed by `RocketStatus`? |
| 39 | Group SpaceX missions by mission status since 2015. |
| 40 | What are the top launch sites by row count? |
| 41 | Show mission outcome mix for launches in the 2010s only. |
| 42 | Aggregate by company for missions at Cape Canaveral. |

---

## `aggregate_space_missions_by_launch_country`

Questions about **geography** using the last comma segment of `Location`.

| # | User question |
|---|----------------|
| 43 | What percentage of launches are from the USA? |
| 44 | Show launch count by country derived from location. |
| 45 | Which countries have the most launches in the dataset? |
| 46 | What share of missions launch from Kazakhstan? |
| 47 | Break down launches by country for SpaceX only. |
| 48 | How many rows have an unparseable or missing country from location? |
| 49 | Compare country distribution for missions after 2010. |
| 50 | Give me the top 10 launch countries and roll the rest into Other. |

---

## `compute_space_mission_success_rate`

Questions about **success rate** for a filtered slice (MSR-style).

| # | User question |
|---|----------------|
| 51 | What is SpaceX's mission success rate? |
| 52 | What is the success rate for NASA missions since 2000? |
| 53 | How successful were launches from 1957–1965? |
| 54 | What is the failure rate for missions using Vanguard rockets? |
| 55 | Compute success rate for launches from the USA in the last decade of the dataset. |
| 56 | What percentage of ISRO missions succeeded? |
| 57 | Success rate for crew-related missions (mission name contains "Crew"). |

---

## Multi-tool workflows (typical agent chains)

| # | User question | Suggested tool sequence |
|---|----------------|-------------------------|
| 58 | What is SpaceX's success rate since 2020, and show a few example rows? | `compute_space_mission_success_rate` → `filter_space_missions` |
| 59 | How many companies are there, and who has the most launches? | `list_space_mission_distinct_values` (Company) → `aggregate_space_missions` (`groupBy`: Company) |
| 60 | Before filtering by country, what does Location look like in the data? | `get_space_missions_schema` → `list_space_mission_distinct_values` (Location) |
| 61 | Overall dataset health, then breakdown by country | `get_space_missions_summary` → `aggregate_space_missions_by_launch_country` |
| 62 | How many Falcon 9 launches failed, and list them? | `count_space_missions` → `filter_space_missions` |
| 63 | Is "Partial Failure" spelled exactly that way in the CSV? | `list_space_mission_distinct_values` (`column`: MissionStatus) |
| 64 | Mission status mix for USA vs rest of world | `aggregate_space_missions_by_launch_country` + filtered `aggregate_space_missions` (two calls with location/derived filters) |

---

## Edge-case and disclosure prompts (test agent behavior)

| # | User question | What to verify in the answer |
|---|----------------|------------------------------|
| 65 | Filter launches from `dateFrom` "2020-13-01" — did the date filter apply? | Mentions `warnings` if date ignored |
| 66 | Show me all 5,000 SpaceX launches. | States `totalMatching` vs `returned` cap (200) |
| 67 | List every unique rocket (there are hundreds). | Distinct `limit` / pagination or `other` in aggregates |
| 68 | What country is "Florida, USA" under your parser? | Explains last-segment rule; not treated as sovereign state |
| 69 | Success rate including rows with blank mission status. | Uses formula: empty status excluded from denominator |

---

## Quick reference: one question per tool

| Tool | Example user question |
|------|------------------------|
| `get_space_missions_schema` | What columns and date range does the dataset have? |
| `get_space_missions_summary` | Summarize the whole dataset by mission outcome. |
| `list_space_mission_distinct_values` | Which companies are in the data? |
| `filter_space_missions` | List SpaceX launches after 2019 with mission details. |
| `count_space_missions` | How many failures are recorded for US Navy? |
| `aggregate_space_missions` | Distribution of missions by company. |
| `aggregate_space_missions_by_launch_country` | Launch share by country from location text. |
| `compute_space_mission_success_rate` | What is ESA's success rate? |

---

## See also

- [Tool reference](space-missions-mcp-tools.md) — parameters, limits, JSON shapes
- [Server guide](space-missions-mcp.md) — run Chatbot, configuration, evidence rules
- [Space missions data dictionary](../rag/space_missions_data_dictionary.md) — column semantics
- [RAG eval gold](../rag/rag_eval_space_missions_gold.md) — related prefilled RAG questions

Use this list as **Chatbot smoke prompts**, eval seeds, or templates when authoring questions under **`questions/`**.
