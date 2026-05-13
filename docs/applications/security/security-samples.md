# Security samples (`Security`)

**[Repository README — Security](../../../README.md#4-security-samples)** · [Getting started](../../getting-started.md) · [Overview](../../overview.md)

The **`Security`** console app ([`src/Security/`](../../../src/Security/)) runs **four sequential demos** against a single chat instance. Each pair shows a **vulnerable pattern** followed by a **mitigation** aligned with this host. It uses **`PromptEngineering.LLM`** (`IAiService`) only—**no MCP tools** and **no** host-fabricated tool results.

Narrative risk write-ups (tables and mitigations) live under **[`risk-assessment/`](../../../risk-assessment/)**: [prompt injection](../../../risk-assessment/prompt-injection.md), [sensitive information disclosure](../../../risk-assessment/sensitive-information-disclosure.md).

## What runs (in order)

| Step | Method (in code) | Idea |
| --- | --- | --- |
| 1 | `PromptInjection` | System prompt embeds a fake **instance name** secret; adversarial user text (“ignore prior instructions…”) can elicit it. |
| 2 | `PromptInjectionSafe` | Stronger system guardrail, **heuristic** `IsInjectionAttempt` (banned substrings), **abort** before completion if flagged. |
| 3 | `SensitiveInformationDisclosure` | Fake **CRM row** (balance, internal note) merged into the **system** message; a **benign** user ask can still produce paste-ready sensitive text. |
| 4 | `SensitiveInformationDisclosureSafe` | CRM fields **redacted** before model ingress, explicit reply policy in system text, **post-check** logs a warning if the raw balance string appears in output. |

Demos are **educational**: substring guards and post-checks are **not** sufficient for production; they illustrate layers you might combine with architecture, policy, and monitoring.

## Configuration

- **[`src/Security/appsettings.json`](../../../src/Security/appsettings.json)** — **`SystemSettings:AiServiceSettings`** (same shape as Client, Rag, Agent): **`BaseAddress`**, **`Instances`** with **`Name`**, **`ApiKey`**, **`Deployment`**, optional **`EmbeddingDeployment`**.
- **`Security`** section — **`InstanceName`**: must match **`Instances[n].Name`**. **`Temperature`**: passed into each **`ChatRequest`** (default **0.2** in committed config).

Override secrets with user secrets on **`src/Security/Security.csproj`** (see [Getting started — User secrets](../../getting-started.md#executable-projects)).

## Related code

- Entry: [`Program.cs`](../../../src/Security/Program.cs) — demo flow and `IsInjectionAttempt`.
- Options: [`SecurityOptions.cs`](../../../src/Security/SecurityOptions.cs) — **`Security`** section binding.
