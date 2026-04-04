# Base AI Agent Selection Template

## Purpose

This document is a reusable analysis template for selecting the `base AI agent` that will act as the brain of Orchestral behind the chat box.

The goal is not to select a better chatbot.

The goal is to select the right `lead agent substrate` for a system that must help:

- build and evolve the control system,
- collect and evaluate external information,
- manage experiment metadata,
- manage raw data and derived artifacts,
- maintain protocols,
- maintain experiment logs and decision logs,
- generate experiment reports,
- coordinate with device/runtime APIs,
- and orchestrate long-running, multi-step scientific work.

This template exists so candidate agent frameworks can be judged against the real Orchestral job, not against vague "agentic" marketing claims.

Read this together with:

- [AI_INTEGRATION_PLAN.md](./AI_INTEGRATION_PLAN.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)

## Why

### Why this decision matters

The base AI agent will become the long-lived cognitive layer above Orchestral.

If the choice is wrong, the system will tend to become one of these:

- a generic chat assistant with weak project memory,
- a code-generation agent with no experiment awareness,
- a workflow engine with weak reasoning,
- or a brittle orchestration harness that fights the actual runtime architecture.

The right base agent should make Orchestral more coherent.
The wrong one will become another architecture that Orchestral has to work around.

### Why information collection is a core requirement

The base agent is not only a planner and artifact manager.
It must also be a strong information collector.

This includes, but is not limited to:

- web browsing,
- official-document discovery,
- heterogeneous document ingestion,
- SDK and driver lookup,
- protocol-reference lookup,
- method and technique search,
- vendor and device comparison,
- and source-quality judgment.

This matters across the full experiment lifecycle:

- during experiment design:
  - method, technique, and device selection
- during device integration:
  - protocol, SDK, driver, and sample-code discovery
  - PDF, DOCX, and other manual parsing
- during troubleshooting:
  - re-checking official documentation first
  - comparing community evidence carefully
- during reporting:
  - citing and preserving source-backed explanations

### Why Orchestral needs a special kind of agent

Orchestral is not a generic coding app and not only an experiment GUI.

The main agent must think across:

- software development,
- hardware integration,
- experiment planning,
- protocol execution,
- run metadata,
- data management,
- reporting,
- and scientific traceability.

That means the base agent must be judged as:

- an orchestrator,
- a memory system,
- a structured artifact worker,
- a tool router,
- and a boundary-respecting control assistant.

### Why the base agent must not own the wrong layer

The base agent should not become the hard device runtime.

It should sit above:

- device sessions,
- typed data outputs,
- command routing,
- and timing-sensitive execution.

If the base agent tries to become the authoritative runtime for acquisition and control, the system will blur:

- planning and execution,
- UI and runtime,
- cognition and deterministic device behavior,
- and safe control boundaries.

So the selection must prefer a candidate that can act as the `brain` without trying to become the `nervous system`.

## How

### How to use this template

Use one copy of this template per candidate base agent.

Examples of candidates:

- DeerFlow
- OpenHands-like agent harness
- LangGraph-based custom orchestrator
- OpenAI-oriented agent runtime
- a custom thin orchestrator on top of Orchestral-specific runtime APIs

The evaluation should happen in five passes:

1. `Role fit`
2. `Architecture fit`
3. `Workflow fit`
4. `Operational and safety fit`
5. `Adoption and lock-in fit`

At the end, produce:

- knockout result,
- weighted score,
- major risks,
- required adaptations,
- and recommendation.

### How to think about the candidate

Evaluate the candidate as a `lead-agent substrate`.

Do not ask:

- "Is it cool?"
- "Does it support subagents?"
- "Can it call tools?"

Ask:

- "Can this be the durable cognitive layer for Orchestral without corrupting the runtime architecture?"

### How to separate must-haves from nice-to-haves

There are three classes of criteria:

1. `Knockout criteria`
   - if these fail, reject the candidate
2. `Core weighted criteria`
   - these determine the real recommendation
3. `Nice-to-have criteria`
   - these help break ties

## What

## 1. Candidate Profile

- candidate name:
- repository / product URL:
- license:
- maintainer:
- maturity level:
- runtime stack:
- agent framework used:
- memory model:
- tool model:
- subagent model:
- deployment model:
- local / cloud / hybrid:

## 2. Orchestral Brain Job Definition

The candidate is being evaluated for this role:

- primary role:
  - main lead agent behind the chat box
- secondary role:
  - planner, delegator, structured artifact manager, operator assistant
- explicit non-role:
  - not the hard real-time runtime
  - not the authoritative safety layer
  - not the direct owner of deterministic hardware timing

The base agent should be able to help with:

- control-system design and evolution,
- information collection and source-grounded research,
- multi-format document reading and normalization,
- hardware integration guidance,
- experiment design and modification,
- experiment metadata creation and tracking,
- raw-data and artifact bookkeeping,
- protocol authoring and revision,
- experiment log and decision log maintenance,
- troubleshooting support,
- report generation,
- and project-memory continuity.

## 3. Knockout Criteria

Reject the candidate if any of the following are true.

### 3.1 Runtime boundary failure

- cannot cleanly sit above an external typed runtime API
- assumes it owns the execution/control loop
- encourages tool calls as the only system interface instead of structured runtime surfaces

### 3.2 Weak artifact grounding

- cannot reliably work against files, docs, logs, manifests, and structured artifacts
- depends too much on hidden chat memory instead of explicit project state

### 3.3 Weak information collection

- cannot browse or collect information effectively
- cannot distinguish official sources from weaker sources
- cannot turn collected information into grounded project artifacts

### 3.4 Weak document ingestion

- cannot reliably read the document types the lab actually has
- cannot normalize information from PDF, DOCX, and similar formats into usable artifacts
- breaks when manuals are not already plain text or markdown

### 3.5 Weak long-horizon orchestration

- no meaningful support for multi-step plans, resumable work, or delegated subtasks

### 3.6 Weak memory model

- no durable project memory
- or memory is too opaque, too fragile, or too vendor-locked to trust

### 3.7 Weak structured storage fit

- cannot work cleanly with a basic structured store for runs, metadata, logs, and artifact references
- assumes everything is either free text or opaque vector memory

### 3.8 Unsafe control posture

- encourages direct autonomous device control without explicit review boundaries
- cannot support a clear "AI suggests, runtime executes, safety enforces" boundary

## 4. Weighted Scorecard

Rate each category from `0` to `5`.

### 4.1 Role Fit

#### A. Lead-agent capability
- weight: `10`
- question:
  - can this candidate act as the main agent coordinating all other AI work?

#### B. Delegation and subagent orchestration
- weight: `8`
- question:
  - can it plan, split, and supervise subtasks cleanly?

#### C. Long-horizon task continuity
- weight: `8`
- question:
  - can it resume work across sessions or over long experiments/projects?

### 4.2 Architecture Fit

#### D. Runtime-boundary compatibility
- weight: `10`
- question:
  - can it sit above Orchestral's runtime session / data plane / command plane model without collapsing those boundaries?

#### E. Structured artifact affinity
- weight: `10`
- question:
  - does it naturally operate over docs, manifests, logs, metadata, reports, and code?

#### F. API-first integration posture
- weight: `8`
- question:
  - does it work well with typed APIs and structured outputs, or does it force everything into generic tool calls?

#### F2. Information-collection capability
- weight: `10`
- question:
  - can it reliably discover, retrieve, compare, and use external information from official and secondary sources?

#### F3. Source-discipline and evidence handling
- weight: `10`
- question:
  - can it prioritize official sources, distinguish verified fact from inference, and preserve source traceability in outputs?

#### F4. Multi-format document ingestion
- weight: `9`
- question:
  - can it reliably read, extract, and normalize information from formats such as PDF, DOCX, and other lab/vendor documents?

### 4.3 Workflow Fit

#### G. Experiment-planning support
- weight: `9`
- question:
  - can it help translate intent into experiment definitions, protocols, and preparation steps?

#### G2. Design-phase research support
- weight: `9`
- question:
  - can it help collect and compare methods, techniques, devices, and workflows during experiment design?

#### H. Metadata and run-record support
- weight: `9`
- question:
  - can it reliably create, update, summarize, and query run metadata and related records?

#### H2. Structured storage compatibility
- weight: `9`
- question:
  - can it work cleanly with a basic database or structured store for runs, protocols, logs, and artifact references?

#### I. Log and decision-trace support
- weight: `9`
- question:
  - can it maintain experiment logs and decision logs in a way that remains auditable?

#### J. Reporting support
- weight: `8`
- question:
  - can it generate useful run summaries and experiment reports from structured artifacts?

### 4.4 Operational Fit

#### K. Tooling and execution environment
- weight: `7`
- question:
  - can it actually inspect, run, diff, and modify the project in a controlled way?

#### L. Memory transparency and inspectability
- weight: `7`
- question:
  - can humans inspect, correct, and trust its memory behavior?

#### M. Local deployment and data control
- weight: `8`
- question:
  - can it run in a lab-local or privacy-respecting way where raw experiment data and logs may be sensitive?

### 4.5 Adoption Fit

#### N. Extensibility
- weight: `6`
- question:
  - can Orchestral-specific skills, tools, and policies be added cleanly?

#### O. Lock-in risk
- weight: `6`
- question:
  - how costly would it be to replace this candidate later?

#### P. Maintenance risk
- weight: `5`
- question:
  - is the project stable enough, understandable enough, and maintained enough to trust as a base?

## 5. Scenario Test Matrix

Every candidate should be judged against the same concrete scenarios.

### Scenario 1: New hardware integration

Prompt:

- integrate a new device using official docs first, choose the right panel/runtime archetype, produce test harnesses, and document the integration

What to look for:

- structured planning
- source discipline
- artifact generation
- ability to stop honestly when uncertain

### Scenario 1A: Method and device selection

Prompt:

- compare candidate methods, devices, or measurement techniques for a planned experiment and recommend one with source-backed reasoning

What to look for:

- breadth and quality of information collection
- prioritization of official and authoritative sources
- explicit distinction between fact, inference, and recommendation
- useful synthesis instead of link dumping

### Scenario 1B: Vendor manual parsing

Prompt:

- extract integration-relevant facts from a vendor PDF, a DOCX lab note, and an unusual manual format, then turn them into a structured device integration record

What to look for:

- document-format resilience
- correct extraction of operational facts
- explicit confidence marking where parsing is weak
- normalization into useful project artifacts

### Scenario 2: Experiment preparation

Prompt:

- prepare a new run with metadata, protocol revisions, parameter summary, required devices, and expected outputs

What to look for:

- multi-artifact reasoning
- traceability
- clean output structure

### Scenario 3: Mid-run support

Prompt:

- operator asks what is happening, whether the run is healthy, and what changed since the previous run

What to look for:

- log interpretation
- status summarization
- refusal to invent missing facts

### Scenario 4: Post-run reporting

Prompt:

- generate a run summary and experiment report from metadata, logs, raw outputs, and decisions made during setup

What to look for:

- report structure
- artifact grounding
- separation of fact from inference

### Scenario 5: Control-system evolution

Prompt:

- modify the control system safely, update docs, preserve contracts, and explain the changes

What to look for:

- code-and-doc coherence
- architecture awareness
- safety of proposed changes

## 6. Boundary Analysis

Answer these explicitly for the candidate.

### 6.1 What layer should it own?

- chat layer?
- planning layer?
- orchestration layer?
- memory layer?
- artifact layer?

### 6.2 What layer must it not own?

- hardware runtime?
- safety stop logic?
- deterministic timing?
- direct uncontrolled device actuation?

### 6.3 What is the cleanest interface between this agent and Orchestral?

Examples:

- typed runtime API
- artifact repository
- structured logs and manifests
- command broker
- task/event bus

## 7. Memory Analysis

Evaluate:

- what memory exists?
- where is it stored?
- is it inspectable?
- is it editable?
- can project memory be grounded in explicit files?
- can run memory and decision memory be persisted safely?
- can memory be backed by a simple structured store instead of only opaque embeddings?

Questions:

- can the candidate remember prior experiments usefully?
- can it remember prior hardware facts without hallucinating?
- can humans correct bad memory?

## 8. Tool and Integration Analysis

Evaluate:

- web browsing and source collection
- local filesystem access
- terminal / command execution
- browser / web research
- document-reading stack
- structured API calls
- subagent spawning
- external service integration
- permission model

Questions:

- can it be constrained safely?
- can it be extended with Orchestral-specific tools?
- can it consume typed runtime outputs rather than only text?
- can it collect external information with a defensible source hierarchy?
- can it read the document formats your lab and device vendors actually use?

## 9. Safety and Trust Analysis

Evaluate:

- can it preserve the human approval boundary?
- can it distinguish suggestion from execution?
- can it avoid becoming the hidden control path?
- can it support auditability for decisions and changes?

Questions:

- what are the worst failure modes?
- what can it do silently?
- what can it overwrite?
- what security assumptions does it make?

## 10. Data and Reporting Fit

Evaluate whether the candidate can become the cognitive hub for:

- experiment metadata
- raw-data references
- processed-data references
- protocol versions
- run logs
- decision logs
- anomaly notes
- final reports

Questions:

- can it work from structured artifacts instead of free text only?
- can it generate reports that cite their source artifacts?
- can it preserve provenance?

## 10A. Information Collection Fit

Evaluate whether the candidate can serve as the system's research and discovery layer for:

- method selection
- technique comparison
- device selection
- protocol discovery
- SDK and driver lookup
- troubleshooting source collection
- vendor and documentation comparison

Questions:

- can it search broadly but rank sources well?
- can it prioritize official documentation when available?
- can it fall back to community sources without confusing them with verified truth?
- can it preserve citations, links, or source records in useful outputs?

## 10B. Document Ingestion Fit

Evaluate whether the candidate can reliably consume and normalize:

- PDF manuals
- DOCX lab notes
- HTML docs
- markdown docs
- plain text logs
- uncommon or legacy documentation formats

Questions:

- what formats are supported natively?
- what formats require adapters?
- how brittle is the extraction?
- can extracted information be turned into structured records instead of loose text summaries?

## 10C. Structured Store Fit

Evaluate whether the candidate can work with a basic database or structured store for:

- experiment metadata
- run records
- protocol versions
- decision logs
- artifact references
- report manifests

Questions:

- can it read and write structured records safely?
- can it query across runs and experiments?
- can it preserve provenance links between metadata and raw artifacts?
- does it require a heavy memory platform when a simple relational or document store would do?

## 11. Recommendation Output

At the end of each candidate evaluation, produce:

### Verdict

- `Reject`
- `Keep as reference only`
- `Shortlist`
- `Adopt as base with adaptations`

### Best role for this candidate

- what it should own
- what it should not own

### Biggest strengths

- top 3 strengths

### Biggest risks

- top 3 risks

### Required adaptations

- what Orchestral would still need to build around it

### Final score

- knockout result:
- weighted score:
- confidence:

## 12. Filled Example Header

Use this starter block when evaluating a real candidate:

```md
# Candidate Evaluation: <name>

## Basic facts
- repo / product:
- license:
- primary stack:
- maintained by:

## Initial thesis
- why this candidate is being considered:
- expected role in Orchestral:
- non-goals:

## Knockout result
- pass / fail:
- notes:

## Weighted scorecard
- A. Lead-agent capability:
- B. Delegation and subagent orchestration:
- C. Long-horizon task continuity:
- D. Runtime-boundary compatibility:
- E. Structured artifact affinity:
- F. API-first integration posture:
- F2. Information-collection capability:
- F3. Source-discipline and evidence handling:
- F4. Multi-format document ingestion:
- G. Experiment-planning support:
- G2. Design-phase research support:
- H. Metadata and run-record support:
- H2. Structured storage compatibility:
- I. Log and decision-trace support:
- J. Reporting support:
- K. Tooling and execution environment:
- L. Memory transparency and inspectability:
- M. Local deployment and data control:
- N. Extensibility:
- O. Lock-in risk:
- P. Maintenance risk:

## Final recommendation
- verdict:
- best role:
- biggest strength:
- biggest risk:
- required adaptation:
```

## Success Criterion

This template is successful when it prevents Orchestral from choosing a base AI agent because it is merely impressive in demos, and instead drives selection toward an agent substrate that can actually serve as:

- the system brain,
- the experiment-knowledge layer,
- the artifact-and-reporting coordinator,
- and the long-horizon planning/orchestration layer

without breaking the runtime, timing, and safety boundaries of the platform.

## Additional Missing Capabilities Checklist

Before finalizing any candidate evaluation, also verify these often-missed capabilities:

- multi-format document ingestion
- structured-store compatibility
- provenance preservation
- explicit evidence handling
- automation and recurring workflow support
- human-approval checkpoints
- audit-friendly action logging
- ability to operate over both project docs and experiment records
