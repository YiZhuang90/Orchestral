# Agent Architecture Sketch

Status: Sketch -- detailed design deferred to EF-05.

---

## Purpose

High-level sketch of the Orchestral agent. What it observes, what it can do, what contracts it follows.

This document is intentionally incomplete. It establishes the conceptual boundaries and capability categories so that detailed design work in EF-05 has a stable starting point. Nothing here is a commitment to a specific implementation.

---

## What The Agent Is

The Orchestral agent is the AI half of Orchestral. It helps users build, operate, and learn from their experimental systems.

It is NOT the platform runtime. It does not own the run state machine, the device lifecycle, or the data pipeline. It works alongside the platform -- observing its outputs, proposing changes to its inputs, and accumulating knowledge that makes the next experiment better than the last.

The agent's authority is bounded by the core contract: AI proposes, human approves. The agent never acts unilaterally on anything that affects runtime state, data integrity, or published outputs.

---

## Current Reality

The agent is already active. During development, Claude Code with Superpowers skills serves as the development-time agent -- reviewing code, writing plans, running tests, brainstorming architecture decisions, and maintaining project knowledge through structured documents.

What is missing is formalization. The current agent is effective but ad-hoc. There are no explicit capability definitions, no knowledge storage contracts, and no observation interfaces that make the agent a first-class system citizen rather than a tool invoked through a CLI.

The existing Superpowers skill set (brainstorming, writing-plans, TDD, systematic-debugging, code-review, etc.) is the development-time agent. The production agent will share many of the same principles -- structured reasoning, human review gates, knowledge accumulation -- but will serve the experiment lifecycle rather than the development lifecycle.

---

## Agent Capabilities (Sketch)

### A1 -- Observation and Knowledge

- Observe runtime events, session state, and run manifests as they are produced by the platform.
- Capture decisions from user interactions (the daily dream process -- distilling what was learned from each session into durable knowledge).
- Track calibration state per device, linking calibration records to specific run manifests.
- Maintain a lab knowledge base: protocols, best practices, device-specific experience, failure patterns.
- Auto-collect metadata during experiments (environmental conditions, operator notes, deviations from protocol).

### A2 -- Guidance

- Guide device integration via structured contracts (reference: `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md`). Walk users through the interface-service-client-panel pipeline for new hardware.
- Assist experiment design top-down: from research question to required capabilities to hardware configuration.
- Assist troubleshooting through text-based, AI-native diagnostic workflows. No manual-style flowcharts -- the agent asks questions, narrows hypotheses, and suggests concrete next steps.
- Provide device protocol knowledge and integration patterns accumulated from previous integrations.

### A3 -- Generation

- Draft experiment definitions (YAML artifacts) from high-level natural-language descriptions.
- Generate daily/weekly experiment reports from recorded data, with a mandatory human review gate before publication.
- Generate analysis outputs from recorded data (summary statistics, trend detection, anomaly flagging).
- Generate compliance and audit documentation from run manifests and calibration records.

---

## Core Contract -- AI Proposes, Human Approves

The agent NEVER takes unilateral action on:

- Runtime start/stop
- Data deletion
- Configuration changes to device or experiment definitions
- Report publication or external data sharing

All agent-generated artifacts go through a human review gate. The agent may draft, suggest, and recommend. The human decides.

This contract is not a limitation to be worked around. It is a design principle. The agent's value comes from reducing cognitive load and accelerating iteration, not from autonomous operation.

---

## Knowledge Storage (Sketch)

The agent accumulates knowledge across five categories:

- **Decision logs**: Per-project, date-indexed. Distilled from interaction history during the daily dream process. Captures why choices were made, not just what was chosen.
- **Calibration records**: Per-device, linked to run manifests. Tracks calibration history, drift patterns, and recalibration triggers.
- **Protocol library**: Per-device-type, versioned. Shareable across lab members. Captures validated procedures for device operation, maintenance, and troubleshooting.
- **Best practices**: Lab-wide, accumulated from reviewed experiment reports. Emergent knowledge about what works and what does not in specific experimental contexts.
- **Device experience**: Per-device-model. Failure patterns, workarounds, integration notes, known quirks. The institutional memory that currently lives in researchers' heads.

Storage format and persistence model are open questions for EF-05. The sketch assumption is that knowledge is stored as structured files (YAML or Markdown) that are version-controlled alongside the project, but this may change based on multi-user and cross-session requirements.

---

## How The Agent Connects To The Platform

The agent interacts with the platform through four directional relationships:

1. **Agent OBSERVES platform data foundation.** Runtime outputs, run manifests, device telemetry, metadata -- the agent reads these but does not own them. The platform produces; the agent consumes.

2. **Agent WRITES TO knowledge foundation.** Decision logs, protocol entries, best practices, device experience notes -- the agent produces these from its observations and interactions. The knowledge foundation is the agent's primary output.

3. **Platform CONSUMES agent-generated artifacts.** Experiment definitions, device configurations, analysis parameters -- the agent drafts these as YAML artifacts. After human approval, the platform loads and executes them.

4. **Agent never directly controls the platform runtime.** No API calls from the agent to start runs, stop devices, or modify live configuration. All runtime-affecting actions require human approval and are executed through the platform's own control surfaces.

---

## Open Questions For EF-05 (Detailed Design)

These questions are explicitly deferred. They require implementation experience and architectural decisions that cannot be made at sketch level.

- **Build vs. extend vs. hybrid.** Build a custom agent framework, extend Claude Code/Codex capabilities, or compose both approaches for different lifecycle phases?
- **Agent persistence model.** How does knowledge survive sessions? File-based, database-backed, or a combination? How does the agent resume context after being inactive?
- **Multi-user architecture.** How does the agent serve a lab with multiple researchers? Shared knowledge base with per-user interaction history? Role-based access to agent capabilities?
- **Observation interface design.** What platform surfaces does the agent subscribe to? Push (event-driven) vs. pull (query-based)? What granularity of runtime events is useful vs. noisy?
- **Artifact handoff format.** How does the agent hand off generated experiment definitions to the platform? Direct file write, staging area with diff review, or artifact proposal queue?
- **Development-to-production agent relationship.** The development-time agent (Superpowers skills) and the production agent share principles but serve different lifecycles. Are they the same system with different skill sets, or separate systems with shared knowledge?
