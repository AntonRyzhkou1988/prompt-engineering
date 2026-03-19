# Prompt Engineering Practice

This repository contains a prompt-engineering exercise based on the dataset at `dataset/attacks.csv`.
The goal is to design, debug, and improve prompts for extracting reliable insights from a real-world CSV file.

## Dataset

- File: `dataset/attacks.csv`
- Domain: historical shark attack incidents
- Example columns used in prompts:
  - `Year`
  - `Country`
  - `Type`
  - `Activity`
  - `Fatal (Y/N)`
  - `Injury`
  - `Age`

## 1) Baseline Prompt (Poor Performance)

The first prompt is intentionally vague and under-specified.

```text
Analyze the dataset and give me useful insights.
```

### Why this performs poorly

- No analyst role or perspective is defined.
- No scope boundaries (which columns or time range to prioritize).
- No required output structure, so responses vary widely.
- No data-quality handling instructions for missing or noisy fields.
- No requirement to separate evidence-based findings from assumptions.

## 2) Refined ReAct / Self-Reflection Prompt

This is a custom prompt designed for this dataset and topic.

```text
Act as a senior data scientist specializing in incident analytics.

You are given a CSV dataset of shark attack records with columns such as:
Case Number, Date, Year, Type, Country, Area, Location, Activity, Sex, Age, Injury, Fatal (Y/N), Time, Species.

Task:
1. Analyze the dataset to extract the most important risk and pattern insights.
2. Focus on practical insights for safety planning and public awareness.
3. Use evidence from the available fields and avoid unsupported claims.

Reasoning method (ReAct + self-reflection):
- Step 1: List the columns you will rely on and why they matter.
- Step 2: Identify data quality issues (missing values, inconsistent labels, duplicate columns, noisy text).
- Step 3: Derive trends (time, geography, activity, and severity/fatality patterns).
- Step 4: Self-check each trend:
  - Is it directly supported by the data fields?
  - Could missing data bias this conclusion?
  - What confidence level should be assigned (High/Medium/Low)?
- Step 5: Revise weak or speculative findings before finalizing.

Output format (strict):
- Section A: Key insights (5-8 bullet points), each with:
  - Insight statement
  - Supporting fields used
  - Confidence: High/Medium/Low
- Section B: Data quality findings (3-5 bullet points)
- Section C: Recommended next analyses (3 bullet points)
- Section D: Final concise summary (max 5 lines)

Constraints:
- Do not invent numeric values if they are not computed.
- Explicitly state uncertainty when evidence is partial.
- Keep language clear and non-technical for business stakeholders.
```

## 3) Debug and Optimization Log (v1 -> v2 -> v3)

### v1 (too broad)

```text
Act as a data scientist. Analyze the shark attacks dataset and provide key insights in bullet points.
```

Problems:
- Better than baseline, but still broad.
- No mandatory method for checking weak assumptions.
- No consistent section format, making outputs hard to compare.

### v2 (clearer task and structure)

```text
Act as a data scientist.
Analyze attacks.csv using Year, Country, Type, Activity, Injury, and Fatal (Y/N).
Return:
1) Top trends
2) Fatality-related patterns
3) Data quality issues
4) Suggested follow-up analyses
Use bullet points and mark confidence levels.
```

Improvements over v1:
- Defines specific columns.
- Introduces structured outputs.
- Adds confidence marking.

Remaining issues:
- Still no explicit self-correction loop.
- Confidence labels can still be inconsistent without evaluation criteria.

### v3 (optimized ReAct + self-reflection)

v3 is the refined prompt shown in section 2.

What v3 fixes:
- Adds explicit reasoning flow and self-check gates.
- Forces uncertainty disclosure and anti-hallucination constraints.
- Standardizes output for reliable comparison between model runs.

## 4) Meta-Prompting Experiment

In this step, an LLM is used to improve an existing prompt (`v2`).

### Meta-prompt used

```text
You are a prompt optimization expert.
Improve the following prompt to maximize clarity, specificity, factual grounding, and output consistency for CSV analysis.

Requirements:
- Keep the same intent (analyze shark attacks dataset).
- Add explicit reasoning and self-reflection steps.
- Add hallucination controls and confidence calibration.
- Enforce a strict response schema.
- Keep the final prompt concise but robust.

Prompt to improve:
[PASTE v2 HERE]

Return:
1) Improved prompt
2) Short explanation of what was changed and why
3) A checklist to evaluate output quality
```

### Example improved prompt produced by meta-prompting

```text
Act as a senior incident data analyst.
Analyze attacks.csv using Year, Country, Type, Activity, Injury, Fatal (Y/N), Age, and Time.

Process:
1) Select relevant fields and justify usage.
2) Detect data quality risks that may affect conclusions.
3) Extract trends by time, location, activity, and severity.
4) Self-critique each finding for evidence strength and potential bias.
5) Downgrade confidence or remove claims that are weakly supported.

Output:
- Insights: 5-8 bullets with evidence fields + confidence (High/Medium/Low)
- Data quality caveats: 3-5 bullets
- Recommended next analyses: 3 bullets
- Executive summary: <= 5 lines

Rules:
- No fabricated statistics.
- Explicitly mark uncertain findings.
- Keep wording concise and stakeholder-friendly.
```

### Test result checklist

Use this checklist to compare `v2` vs improved prompt outputs:

- Clarity: Is the task objective unambiguous?
- Specificity: Are required fields and sections explicit?
- Grounding: Are claims tied to dataset columns?
- Hallucination resistance: Does the model avoid fabricated numbers?
- Consistency: Is output format stable across multiple runs?
- Actionability: Are recommendations practical and prioritized?

## 5) How to Run the Experiment

1. Open `dataset/attacks.csv` and copy a representative sample (or provide full file context if your LLM supports it).
2. Run baseline prompt and save output as `baseline_output`.
3. Run `v1`, `v2`, and `v3` prompts; save each result.
4. Run the meta-prompt on `v2` and capture the LLM-generated improved prompt.
5. Re-run analysis with the improved prompt.
6. Evaluate all outputs with the checklist above.
7. Select the prompt that gives the best balance of correctness, clarity, and consistency.

## Expected Outcome

By following this workflow, you should observe that:

- Prompt specificity strongly improves output quality.
- Self-reflection steps reduce weak or unsupported claims.
- Meta-prompting accelerates prompt refinement and standardization.
