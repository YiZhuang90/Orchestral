# AI Integration Plan

## 1. Purpose

This document defines how AI should be integrated into V1 of the platform.

The principles and boundaries defined here are now operationalized in the detailed agent blueprint:

- [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md) — concrete implementation using Microsoft Semantic Kernel, with SK plugins mapping to each AI capability layer (A1/A2/A2R/A3), and SK filters enforcing the "AI proposes, human approves" boundary.

For evaluating candidate lead-agent frameworks, use:

- [BASE_AI_AGENT_SELECTION_TEMPLATE.md](./BASE_AI_AGENT_SELECTION_TEMPLATE.md)

The goal is to add useful AI assistance early, while keeping live experiment control deterministic and safe.

## 2. Position of AI in the System

AI should begin as a **sidecar assistant**, not as the direct owner of the runtime.

Its role is to help build, modify, explain, and document the system around structured artifacts.

The AI layer should sit above:

- experiment definitions,
- device definitions,
- protocol descriptions,
- runtime specifications,
- documentation,
- logs and run summaries.

It should not initially sit inside the hard real-time or safety-critical execution path.

## 3. Initial AI Form

The preferred initial form is a **Semantic Kernel-based ExperimentAssistant** integrated into the WPF application. This is now fully designed in [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md) with a 6-phase build-use-refine strategy (P0 Skeleton through P6 Hardening).

This assistant is able to:

- observe runtime state and device status (A1),
- search for hardware docs, SDKs, and literature (A2R),
- guide experiment design and device integration (A2),
- draft experiment definitions, reports, and analysis (A3),
- capture decisions and maintain the lab knowledge wiki (A1/A3).

The build-use-refine strategy starts light (P0: just a chat panel) and adds capabilities through real use.

## 4. What AI Should Do in V1

### 4.1 During experiment design

AI should help:

- turn user intent into structured experiment definitions,
- identify required device roles,
- propose monitors and outputs,
- draft high-level schematics and docs.

### 4.2 During device preparation

AI should help:

- structure device information,
- gather protocol details from provided materials or external sources,
- draft driver scaffolds,
- generate device test code,
- generate device documentation,
- define expected output contracts.

### 4.3 During orchestration design

AI should help:

- translate experiment logic into runtime artifacts,
- suggest processing and monitoring flows,
- draft central-control configuration,
- document the control flow and key system variables.

### 4.4 During operation and maintenance

AI should help:

- explain configuration changes,
- summarize run outcomes,
- inspect logs,
- help trace issues,
- generate documentation updates,
- propose safe parameter changes for review.

## 5. What AI Should Not Do in Early V1

AI should not initially:

- send live hardware commands directly on its own authority,
- bypass validation,
- own the stop system,
- rewrite runtime behavior during execution without explicit review,
- make silent changes to critical experiment logic,
- act as the final arbiter of whether a run is safe.

## 6. AI Control Boundary

The operating rule should be:

- **AI can suggest**
- **humans can approve**
- **runtime can execute**
- **safety layer can stop**

This boundary should remain strict until the platform has much stronger validation and operational maturity.

This boundary is enforced architecturally through three SK filters: SafetyFilter (blocks inappropriate tool use during active runs), ApprovalFilter (gates all generative output for human review), and AuditFilter (logs all tool calls for compliance traceability). See [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md) for details.

## 7. AI Interaction Model

The AI workflow should be based on artifacts and diffs.

The expected pattern is:

1. user describes intent in natural language,
2. AI translates that intent into proposed artifact changes,
3. system validates the proposal,
4. user reviews the diff,
5. approved changes become part of the project state.

This keeps AI useful without making it opaque.

## 8. AI Memory and Context

The assistant should gradually build project memory around:

- the experiment domain,
- available device roles,
- known concrete devices,
- protocol facts,
- known operating assumptions,
- prior validated experiment definitions,
- data and documentation conventions.

This memory should be grounded in project artifacts and docs rather than in hidden chat context alone.

## 9. AI and Legacy Knowledge

The current pipe-flow code should be used as an early AI knowledge source.

AI should help extract from it:

- device information,
- protocol information,
- control heuristics,
- monitoring logic,
- signal-processing logic,
- logging conventions,
- safety assumptions.

This makes the old system useful to the new one without turning the old architecture into the new architecture.

## 10. AI and Safety

AI should be aware of safety policy, but it should not own safety enforcement.

The runtime must always retain authority over:

- automatic stops,
- invalid state rejection,
- disconnect handling,
- stale-data rejection,
- experiment-defined stop conditions.

AI may propose changes to these rules, but the runtime enforces them.

## 11. Near-Term AI Deliverables

The first practical AI deliverables should be:

- artifact drafting,
- legacy knowledge summarization,
- protocol and device doc generation,
- experiment-doc generation,
- change explanation,
- operator support for setup and modification.

This is enough to make AI genuinely useful early without overreaching.

## 12. Success Criteria

The AI layer is successful in early V1 if it can:

- reduce the effort needed to define experiments,
- reduce the effort needed to add devices,
- reduce the effort needed to generate docs,
- help translate old knowledge into structured form,
- help operators modify the system safely,
- and do all of this without becoming a hidden unsafe control path.
