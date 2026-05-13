# Prompt injection

Runnable **vulnerable / mitigated** pair in this repo: [`src/Security/Program.cs`](../src/Security/Program.cs) (`PromptInjection`, `PromptInjectionSafe`) — see **[Security samples](../docs/applications/security/security-samples.md)**.

## 📊 Prompt Injection Risk Assessment (Before Mitigation)

| Aspect | Assessment (Vulnerable Code) |
| --- | --- |
| Threat scenario | Malicious user input manipulates model instructions (direct prompt injection). |
| Likelihood | **High** — Attackers can attempt this with crafted prompts; no special access is required. |
| Impact | **High** — The model may ignore business logic or safety guidelines, which can lead to disclosure of sensitive data or harmful actions. |
| Overall risk | **Severe** — Without defenses, a simple crafted prompt can largely override the LLM’s intended behavior. |
| Residual risk | **N/A** — No mitigation has been applied yet. |

## 🛡️ Mitigation: Defense Techniques and Safer Code

### Key mitigations for prompt injection

- **Input validation and filtering** — Actively detect and reject suspicious prompts (for example phrases such as “ignore instructions” or other meta-commands).
- **Context separation** — Avoid blindly concatenating user input. Prefer structured prompting or separate channels for system versus user content so user text cannot override system directives.
- **Output validation** — Monitor the LLM’s output for policy violations (for example checks for secret or sensitive patterns) and block or sanitize disallowed content.
- **Constrained model behavior** — Use LLM settings or fine-tuning to narrow allowable actions—for example, instruct the model to follow the system prompt strictly and to resist attempts to circumvent it.

## 🔒 Revised Risk Assessment (After Mitigation)

Even with mitigations, prompt injection is not 100% solved—LLMs can be unpredictable. Our defenses significantly reduce the risk:

| Aspect | Assessment (After Mitigation) |
| --- | --- |
| Threat scenario | Attack remains possible, but input filters catch common malicious cues, and role separation limits prompt power. |
| Likelihood | **Moderate** — Basic attacks blocked; advanced attackers might still find novel injection techniques. |
| Impact | **Moderate** — If an injection slips through, impact could still be high, but additional checks (output filtering, human review) limit damage. |
| Residual risk | **Reduced but not eliminated** — Continual prompt hardening and model safety updates remain necessary. |
