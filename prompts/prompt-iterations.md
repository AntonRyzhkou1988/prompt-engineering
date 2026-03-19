# ReAct Prompt Iterations (`v1` -> `v2` -> `v3`)

This file documents the iterative optimization chain required by the project standard.

## `v1` - Initial structured attempt

```text
Act as a data analyst.
Analyze dataset/attacks.csv and provide useful insights.
Return key findings in bullet points.
```

What this fixes:
- Improves over an unscoped baseline by introducing a role and explicit dataset path.

Remaining risks:
- Scope is too broad.
- No required evidence format.
- No uncertainty controls.
- No self-critique loop.

## `v2` - Clearer scope and response structure

```text
Act as a senior incident data analyst.
Analyze dataset/attacks.csv using Year, Country, Type, Activity, Injury, and Fatal (Y/N).

Return:
1) Key trends
2) Fatality-related patterns
3) Data quality caveats
4) Suggested next analyses

Use bullet points and assign confidence levels (High/Medium/Low).
Do not invent exact metrics.
```

What this fixes:
- Narrows analysis to known high-signal columns.
- Introduces a stable section structure.
- Adds confidence labels and anti-fabrication rule.

Remaining risks:
- Confidence can still be inconsistent without self-check criteria.
- Weak claims may pass through without explicit revision logic.

## `v3` - ReAct + self-reflection with strict schema

Canonical prompt:
- See `prompts/react-self-reflection-v3.txt`.

What this fixes:
- Adds explicit reasoning sequence: field selection -> data quality -> findings -> self-critique -> claim revision.
- Enforces evidence grounding for each key insight.
- Forces uncertainty disclosure and confidence calibration.
- Standardizes output with bullet-count limits for comparability.

Residual risk:
- Output quality still depends on how much data context is actually provided to the model at run time.
