# Metric: mission success rate (MSR)

## Source schema

This metric is defined only from fields documented in `documents/space_missions_data_dictionary.csv`:

| Field           | Role in this metric |
|----------------|----------------------|
| `MissionStatus` | **Primary.** Determines whether a row counts as a successful mission. |

Optional scope dimensions (same dictionary): `Company`, `Location`, `Date`, `Rocket`, `Mission`, `RocketStatus`, `Price`, `Time`—use these only to filter or group the population before computing MSR; they do not change the core definition.

## Definition

For a chosen **mission population** (all rows meeting any stated filters, e.g. date range, company, or rocket family), **mission success rate (MSR)** is the share of missions whose **mission outcome** is classified as **Success** according to `MissionStatus`.

## Formula

\[
\text{MSR} = \frac{\text{count of rows where } \texttt{MissionStatus} = \texttt{Success}}{\text{count of rows with non-empty } \texttt{MissionStatus}} 
\]

Express as a proportion (0–1) or a percentage (0–100%). Use one convention consistently in a given report.

## Classification rule (canonical)

Per the data dictionary, `MissionStatus` takes these values:

- `Success`
- `Failure`
- `Partial Failure`
- `Prelaunch Failure`

For **MSR** as defined here:

- **Success numerator:** only rows with `MissionStatus` exactly equal to `Success`.
- **Excluded from denominator:** rows where `MissionStatus` is missing or empty (if any appear in the raw file)—do not impute; report the exclusion count if material.
- **Non-success outcomes:** `Failure`, `Partial Failure`, and `Prelaunch Failure` all count toward the denominator but not the numerator.

If a stakeholder needs a **lenient** success definition (e.g. treating `Partial Failure` as success), that is a **different metric** and must be named and specified separately.

## Interpretation

MSR answers: *“Among missions with a recorded outcome, what fraction completed as full successes under the dataset’s labels?”* It does not, by itself, measure cost efficiency, rocket reliability, or operator skill; those require combining other fields (e.g. `Price`, `Company`, `Rocket`) in separate metrics.

## Data quality and caveats

- **Label semantics:** Success vs partial vs prelaunch failure is whatever the source encoded; the dictionary does not define operational criteria per mission.
- **`RocketStatus`** reflects status **as of August 2022**; it is unrelated to MSR unless used as a grouping dimension.
- **`Price`** is optional in the dataset (many rows may lack a value); absence of price does not invalidate `MissionStatus` for MSR but may matter for joint cost-outcome analyses.

## Minimal computation checklist

1. Restrict to the intended population with explicit filters (if none, state “all missions with non-empty `MissionStatus`”).
2. Denominator: count rows with non-empty `MissionStatus`.
3. Numerator: count rows where `MissionStatus == Success`.
4. Report MSR and, if useful, counts for each `MissionStatus` value for transparency.
