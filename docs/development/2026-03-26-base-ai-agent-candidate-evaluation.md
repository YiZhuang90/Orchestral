# Base AI Agent Candidate Evaluation

## Purpose

This document evaluates three candidate base agents against the Orchestral brain role using:

- [BASE_AI_AGENT_SELECTION_TEMPLATE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/BASE_AI_AGENT_SELECTION_TEMPLATE.md)

Candidates:

- DeerFlow 2.0
- PI coding-agent
- embedded Codex session

The goal is not to choose the most impressive agent demo.

The goal is to choose the best base for an agent that can act as the Orchestral brain behind the chat box while respecting:

- runtime boundaries,
- timing boundaries,
- safety boundaries,
- and structured experiment/data/reporting workflows.

## Orchestral Brain Role

The target agent should help with:

- control-system design and evolution
- information collection and source-grounded research
- multi-format document reading and normalization
- hardware integration guidance
- experiment design and modification
- experiment metadata creation and tracking
- raw-data and artifact bookkeeping
- protocol authoring and revision
- experiment log and decision log maintenance
- troubleshooting support
- report generation
- long-horizon project memory

The target agent should not become:

- the hard device runtime
- the safety stop layer
- the deterministic timing engine
- the hidden direct-control path

## Candidate 1: Embedded Codex Session

## Basic facts

- repo / product:
  - OpenAI Codex developer/runtime surface
- reference:
  - [Codex](https://developers.openai.com/codex/)
- type:
  - hosted agent runtime with local tooling and embedded session behavior in this environment

## Initial thesis

- why this candidate is being considered:
  - strongest currently available practical agent environment already being used for Orchestral work
- expected role in Orchestral:
  - initial main agent brain
- non-goals:
  - not the hardware runtime
  - not the timing/safety authority

## Knockout result

- pass / fail:
  - `Pass`
- notes:
  - no knockout failure observed for the Orchestral brain role

## Weighted scorecard

- A. Lead-agent capability:
  - `5/5`
- B. Delegation and subagent orchestration:
  - `4.5/5`
- C. Long-horizon task continuity:
  - `4/5`
- D. Runtime-boundary compatibility:
  - `4.5/5`
- E. Structured artifact affinity:
  - `5/5`
- F. API-first integration posture:
  - `4/5`
- F2. Information-collection capability:
  - `4.5/5`
- F3. Source-discipline and evidence handling:
  - `4.5/5`
- F4. Multi-format document ingestion:
  - `3.5/5`
- G. Experiment-planning support:
  - `4.5/5`
- G2. Design-phase research support:
  - `4.5/5`
- H. Metadata and run-record support:
  - `4/5`
- H2. Structured storage compatibility:
  - `3.5/5`
- I. Log and decision-trace support:
  - `4/5`
- J. Reporting support:
  - `4.5/5`
- K. Tooling and execution environment:
  - `5/5`
- L. Memory transparency and inspectability:
  - `3.5/5`
- M. Local deployment and data control:
  - `3/5`
- N. Extensibility:
  - `4.5/5`
- O. Lock-in risk:
  - `2.5/5`
- P. Maintenance risk:
  - `4.5/5`

## Summary judgment

### Strengths

- best immediate fit for control-system evolution, hardware-integration guidance, docs, and report generation
- strongest practical artifact grounding
- strongest current coding-and-architecture assistance
- strong information collection when paired with browser/search

### Risks

- not fully owned by Orchestral
- memory/store model is not natively an Orchestral-specific structured experiment memory system
- multi-format document ingestion depends on the surrounding tool environment rather than one unified native stack

### Best role

- initial Orchestral brain
- planning, research, artifact work, reporting, and system evolution

### Required adaptations

- explicit Orchestral runtime API boundary
- structured store for runs, protocols, decision logs, and artifact references
- document ingestion adapters for awkward formats like CHM
- explicit provenance and audit policy

## Candidate 2: DeerFlow 2.0

## Basic facts

- repo / product:
  - [bytedance/deer-flow](https://github.com/bytedance/deer-flow)
- type:
  - open-source super agent harness
- advertised core features:
  - sub-agents
  - memory
  - sandboxes
  - skills
  - context engineering
  - embedded Python client
  - search/crawling support through InfoQuest

## Initial thesis

- why this candidate is being considered:
  - strongest open-source candidate for a lead-agent substrate
- expected role in Orchestral:
  - potential self-hosted brain/orchestration harness
- non-goals:
  - not the device runtime
  - not the deterministic timing layer

## Knockout result

- pass / fail:
  - `Pass`
- notes:
  - no knockout failure, but several adaptation requirements are non-trivial

## Weighted scorecard

- A. Lead-agent capability:
  - `4.5/5`
- B. Delegation and subagent orchestration:
  - `5/5`
- C. Long-horizon task continuity:
  - `4.5/5`
- D. Runtime-boundary compatibility:
  - `3.5/5`
- E. Structured artifact affinity:
  - `4/5`
- F. API-first integration posture:
  - `3.5/5`
- F2. Information-collection capability:
  - `4.5/5`
- F3. Source-discipline and evidence handling:
  - `3.5/5`
- F4. Multi-format document ingestion:
  - `3/5`
- G. Experiment-planning support:
  - `4/5`
- G2. Design-phase research support:
  - `4.5/5`
- H. Metadata and run-record support:
  - `3.5/5`
- H2. Structured storage compatibility:
  - `3.5/5`
- I. Log and decision-trace support:
  - `3.5/5`
- J. Reporting support:
  - `4/5`
- K. Tooling and execution environment:
  - `4.5/5`
- L. Memory transparency and inspectability:
  - `3.5/5`
- M. Local deployment and data control:
  - `4.5/5`
- N. Extensibility:
  - `4/5`
- O. Lock-in risk:
  - `4/5`
- P. Maintenance risk:
  - `3.5/5`

## Summary judgment

### Strengths

- best open-source lead-agent harness candidate
- strong built-in subagent model
- strong research/search orientation
- local deployment story is plausible
- good fit if Orchestral wants a self-hosted orchestration layer

### Risks

- heavier and more opinionated than necessary
- can easily become a competing architecture rather than a thin brain layer
- memory and runtime coupling would need careful discipline
- document-ingestion and structured-store fit are not yet clearly first-class for the Orchestral use case

### Best role

- open-source orchestration and memory harness above the Orchestral runtime

### Required adaptations

- strict runtime/session boundary
- Orchestral-specific structured store
- provenance and source-discipline policy
- clearer experiment-record/log/report handling
- document-ingestion path for lab/vendor formats

## Candidate 3: PI coding-agent

## Basic facts

- repo / product:
  - [badlogic/pi-mono/tree/main/packages/coding-agent](https://github.com/badlogic/pi-mono/tree/main/packages/coding-agent)
- type:
  - minimal terminal coding harness with SDK/RPC/extensibility
- notable philosophy:
  - intentionally minimal
  - explicitly omits built-in subagents and plan mode
  - favors extensions/skills/packages instead

## Initial thesis

- why this candidate is being considered:
  - attractive as a thin embeddable programmable shell
- expected role in Orchestral:
  - possible custom low-level substrate
- non-goals:
  - not a ready-made Orchestral brain

## Knockout result

- pass / fail:
  - `Pass, but weak fit`
- notes:
  - it does not fail a hard knockout, but it underdelivers for the full Orchestral brain role without substantial custom buildout

## Weighted scorecard

- A. Lead-agent capability:
  - `3/5`
- B. Delegation and subagent orchestration:
  - `1.5/5`
- C. Long-horizon task continuity:
  - `3.5/5`
- D. Runtime-boundary compatibility:
  - `4.5/5`
- E. Structured artifact affinity:
  - `4/5`
- F. API-first integration posture:
  - `4.5/5`
- F2. Information-collection capability:
  - `2.5/5`
- F3. Source-discipline and evidence handling:
  - `2.5/5`
- F4. Multi-format document ingestion:
  - `2.5/5`
- G. Experiment-planning support:
  - `2.5/5`
- G2. Design-phase research support:
  - `2.5/5`
- H. Metadata and run-record support:
  - `2.5/5`
- H2. Structured storage compatibility:
  - `3.5/5`
- I. Log and decision-trace support:
  - `2.5/5`
- J. Reporting support:
  - `2.5/5`
- K. Tooling and execution environment:
  - `4/5`
- L. Memory transparency and inspectability:
  - `4/5`
- M. Local deployment and data control:
  - `4.5/5`
- N. Extensibility:
  - `5/5`
- O. Lock-in risk:
  - `4.5/5`
- P. Maintenance risk:
  - `4/5`

## Summary judgment

### Strengths

- highly extensible
- clean embedding story through SDK and RPC
- lightweight and controllable
- good if Orchestral wants to build almost everything itself

### Risks

- too minimal for the full Orchestral brain role
- weak built-in orchestration
- weak built-in research and source-collection posture
- too much of the actual brain would still need to be built around it

### Best role

- thin programmable shell if Orchestral later decides to build a custom agent platform largely from scratch

### Required adaptations

- build subagent orchestration
- build stronger memory layer
- build stronger information-collection layer
- build structured experiment-record support
- build reporting and decision-log workflows

## Comparative Ranking

### Immediate practical fit for Orchestral brain

1. embedded Codex session
2. DeerFlow 2.0
3. PI coding-agent

### Best open-source ownership story

1. DeerFlow 2.0
2. PI coding-agent
3. embedded Codex session

### Best research and information-collection fit

1. embedded Codex session
2. DeerFlow 2.0
3. PI coding-agent

### Best thin embeddable shell

1. PI coding-agent
2. embedded Codex session
3. DeerFlow 2.0

## Final Recommendation

### Recommended now

Use **embedded Codex session** as the initial Orchestral brain.

Why:

- strongest immediate fit
- strongest practical support for design, integration, artifact work, and reporting
- best current balance of reasoning, coding, research, and orchestration

### Recommended open-source benchmark

Keep **DeerFlow 2.0** as the main open-source reference candidate.

Why:

- strongest open-source lead-agent harness of the three
- plausible future self-hosted brain if Orchestral later needs more ownership

### Recommended non-selection

Do not choose **PI coding-agent** as the main base for Orchestral right now.

Why:

- it is better as a customizable shell than as a ready brain for this project

## Most important architectural reminder

Whichever base agent is chosen, it should sit above:

- the Orchestral runtime session layer
- the typed data plane
- the typed command plane
- and the timing/safety boundary

The base agent should be:

- planner
- researcher
- memory layer
- artifact and report orchestrator

It should not become:

- the authoritative device runtime
- the deterministic timing layer
- or the hidden direct-control path.
