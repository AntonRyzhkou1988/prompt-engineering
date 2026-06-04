# Space missions dataset — data dictionary

**[Repository README — Documentation](../../../README.md#documentation)** · [RAG guide](rag.md)

## Purpose

This document summarizes the column definitions for `dataset/space_missions.csv`, as specified in `dataset/space_missions_data_dictionary.csv` when that file exists alongside the CSV. Use it to interpret fields consistently before filtering, grouping, or computing metrics (for example success-rate or outcome-mix ratios derived from **`MissionStatus`** as documented below, or holistic scoring with **`metrics/answer_correctness_score.md`** for saved answers).

## Dataset shape (logical)

| Field | Description |
|-------|-------------|
| `Company` | Company responsible for the space mission. |
| `Location` | Location of the launch. |
| `Date` | Date of the launch. |
| `Time` | Time of the launch (UTC). |
| `Rocket` | Name of the rocket used for the mission. |
| `Mission` | Name of the space mission (or missions). |
| `RocketStatus` | Status of the rocket **as of August 2022** (`Active` or `Inactive`). |
| `Price` | Cost of the rocket in **millions of US dollars**. |
| `MissionStatus` | Status of the mission: `Success`, `Failure`, `Partial Failure`, or `Prelaunch Failure`. |

## Field groups (for analysis)

### Who and where

- **`Company`** — Organizational dimension; suitable for grouping or filtering by launch provider.
- **`Location`** — Geographic / site dimension; suitable for grouping launches by pad or region (exact granularity follows the raw values in the CSV).

### When

- **`Date`** — Calendar date of launch.
- **`Time`** — UTC time of launch.

Together, `Date` and `Time` define launch timing; validate parsing (time zones, missing components) against the raw file before trend analysis.

### Vehicle and mission identity

- **`Rocket`** — Vehicle identifier or family name as recorded in the source.
- **`Mission`** — Mission label; the dictionary notes it may represent **one or more** missions in a single row’s value—treat as a display/name field unless the pipeline normalizes multi-mission rows.

### Cost

- **`Price`** — Stated in **millions USD**, not raw dollars. Do not mix units with other currency fields without an explicit conversion rule. Missing or sparse `Price` affects any cost-based metrics; report exclusions.

### Status fields (categorical)

**`RocketStatus`** (snapshot)

- Meaning: rocket operational status **as of August 2022**.
- Documented values: **`Active`**, **`Inactive`**.
- Note: This is a **point-in-time** attribute, not the status on launch day unless the source aligns them; use for fleet-style questions, not implicit historical status unless verified in data.

**`MissionStatus`** (outcome)

- Meaning: outcome of the mission attempt.
- Documented values:
  - `Success`
  - `Failure`
  - `Partial Failure`
  - `Prelaunch Failure`

Use these literals for denominators/numerators in success-rate or outcome breakdowns; align any metric definition with exact string equality unless the pipeline maps aliases.

## Analysis notes

1. **Evidence scope** — Interpretations should tie claims to the columns above; do not assume extra columns or derived flags unless computed and documented.
2. **Missing data** — The dictionary does not specify null sentinels; inspect the CSV for empty fields, placeholders, or inconsistent time formats before computing aggregates.
3. **Temporal vs. snapshot** — `Date`/`Time` describe the launch event; `RocketStatus` describes a **2022-August** snapshot—avoid conflating them in causal language.
4. **Multi-mission rows** — If `Mission` encodes multiple missions in one cell, stratification by mission name may under- or over-count unless rows are split or flagged.

## Source

Canonical field list: `dataset/space_missions_data_dictionary.csv` (when checked in next to `dataset/space_missions.csv`).

## See also

- [RAG guide](rag.md) · [Space Missions MCP server](../space-missions-mcp/space-missions-mcp.md) (tool-based access to the same CSV) · [MCP tool reference](../space-missions-mcp/space-missions-mcp-tools.md) · [Repository README — Documentation](../../../README.md#documentation) · [Answer Correctness Score](../../../metrics/answer_correctness_score.md)
