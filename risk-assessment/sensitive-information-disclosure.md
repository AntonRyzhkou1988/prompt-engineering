# Sensitive information disclosure (LLM)

## 📊 Sensitive Information Disclosure Risk Assessment (Before Mitigation)

| Aspect | Assessment (Vulnerable Code) |
| --- | --- |
| Threat scenario | Secrets, PII, or internal-only data are placed in the model context (for example system prompts, retrieved chunks, or tool output). A **benign** user question can still cause the model to **repeat or summarize** that data in the reply, in logs, or in downstream systems. |
| Likelihood | **High** — Any flow that over-shares context with the LLM makes accidental disclosure easy; the user does not need a crafted injection string. |
| Impact | **High** — Leaked balances, credentials, health or identity data, or proprietary business facts can violate policy, regulation, and customer trust. |
| Overall risk | **Severe** — Without controls, routine prompts plus rich internal context are enough to expose sensitive fields. |
| Residual risk | **N/A** — No mitigation has been applied yet. |

## 🛡️ Mitigation: Defense Techniques and Safer Code

### Key mitigations for sensitive information disclosure (LLM)

- **Least-privilege context** — Send only the minimum data the model needs for the task; avoid dumping full records or admin-only fields into the same channel as end-user chat.
- **Redaction and tokenization** — Replace raw values (balances, account numbers, tokens) with placeholders before the prompt is built; resolve real values outside the model when strictly necessary.
- **System and product policy** — Instruct the model not to surface internal markers, exact amounts, or INTERNAL_USE_ONLY fields; pair with non-LLM enforcement where possible.
- **Output filtering and monitoring** — Scan model output for known secret patterns or format checks; log alerts and block or sanitize before showing the user.
- **Separation for RAG and agents** — Use user-scoped retrieval, strip metadata from chunks, and avoid mixing other users’ or environments’ data into one completion request.
- **Logging hygiene** — Do not log full prompts or completions that may contain secrets; redact in telemetry and support tools.

## 🔒 Revised Risk Assessment (After Mitigation)

Even with mitigations, sensitive information disclosure is not fully eliminated—models can infer, hallucinate, or leak in unexpected ways. Defenses materially reduce exposure:

| Aspect | Assessment (After Mitigation) |
| --- | --- |
| Threat scenario | Internal data may still reach the model in edge cases, but context is redacted, scoped, and governed by explicit policies. |
| Likelihood | **Moderate** — Ordinary misuse paths are blocked; misconfiguration or novel UI paths could still attach too much context. |
| Impact | **Moderate** — If a leak occurs, blast radius is smaller due to minimized payloads and output checks. |
| Residual risk | **Reduced but not eliminated** — Ongoing reviews of prompts, retrieval pipelines, and logging are required as features and models change. |
