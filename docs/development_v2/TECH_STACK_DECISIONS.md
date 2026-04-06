# Tech Stack Decisions

## 1. Purpose

This document records the current technical direction for V1 of the platform.

These are not eternal decisions, but they define the default implementation path unless a strong reason appears to change them.

## 2. Main System Language

### Decision

Use **C#/.NET** as the main implementation language of the platform itself.

### Why

The platform needs:

- stronger concurrency and async control than Python is comfortable with,
- stronger process and runtime structure,
- better long-running supervision behavior,
- stronger typing for a growing system,
- practical desktop application support,
- a realistic path to a maintainable runtime and safety layer.

### What C# should own

- runtime engine,
- device lifecycle manager,
- protocol/session manager,
- safety and automatic stop layer,
- central application state,
- plugin loading,
- main operator UI,
- logging and run orchestration.

## 3. Secondary System Language

### Decision

Use **Python** as a secondary language, not the system core.

### Why

Python still has real value for this project:

- existing turbulence and pipe-flow code already works,
- scientific and analysis libraries are strong,
- algorithm migration is easier,
- AI-assisted code generation and experimentation are fast,
- legacy knowledge can be preserved without shaping the whole architecture.

### What Python should own

- legacy algorithm reuse,
- scientific signal-processing modules,
- analysis tools,
- temporary prototype adapters,
- helper scripts,
- AI-generated experimental tooling,
- migration bridges from the old pipe-flow system.

## 4. UI Technology

### Decision

Use a .NET desktop UI stack.

Current preferred options:

- **WPF** if the system is explicitly Windows-first,
- **Avalonia** if cross-platform desktop is a serious near-term goal.

### Current recommendation

Because the target environment is a lab machine and the existing system is Windows-based, the default recommendation is:

- **WPF for V1**

This can be revisited if cross-platform deployment becomes important earlier than expected.

## 5. Data and Storage

### Decision

Use a layered local data approach:

- **SQLite** for manifests, metadata, run records, and event logs
- **Parquet** for structured scalar or tabular outputs
- **Zarr** for large multidimensional arrays and image-like outputs

### Why

The platform needs:

- structured run history,
- queryable metadata,
- efficient numerical output storage,
- a format path that scales better than ad hoc text dumps.

## 6. Artifact Format

### Decision

Use **human-readable YAML artifacts** backed by strongly typed internal models.

Examples:

- `experiment.yaml`
- `device.yaml`
- `protocol.yaml`
- `runtime.yaml`
- `manifest.yaml`

### Why

YAML is suitable for:

- human inspection,
- AI editing,
- diff review,
- documentation generation,
- structured validation.

The real language of the system is still the underlying schema and semantics, not the surface syntax alone.

## 7. AI Integration Technology Direction

### Decision

Start with a **structured AI Agent model** implemented via Microsoft Semantic Kernel, not AI embedded inside the runtime core.

### Implementation

The agent is implemented as a Semantic Kernel-based sidecar with four capability layers:

- **A1 Observation**: read-only tools that query platform state (run status, device state, manifests, calibration)
- **A2 Guidance**: suggestion tools for experiment design, device integration, troubleshooting
- **A2R Research**: web search, vendor doc discovery, literature search (core capability, not optional)
- **A3 Generation**: artifact drafting (experiment definitions, reports, device configs) — ALL gated by human approval

Safety is enforced through SK filters: SafetyFilter (blocks inappropriate tool use during runs), ApprovalFilter (gates all generative output), AuditFilter (logs everything for compliance).

Knowledge is managed through a Karpathy LLM Wiki pattern with three layers (Raw sources -> Evolving wiki -> Governance schema) and 10 knowledge categories (K1-K10).

For the full detailed design, see [AGENT_SIDECAR_BLUEPRINT.md](./AGENT_SIDECAR_BLUEPRINT.md).

### Why

This is the lowest-risk useful integration path:

- it adds value early,
- it avoids unsafe direct hardware control,
- it keeps the runtime deterministic,
- native C#/.NET matches the platform stack,
- SK's plugin/filter model maps cleanly to the A1-A3 capability layers.

## 7.5 Agent Framework

### Decision

Use **Microsoft Semantic Kernel** (C#/.NET) as the agent framework.

### Why

- Native C#/.NET — matches the platform stack
- Plugin/function model maps cleanly to agent capability layers (A1-A3)
- Filter chain provides the "AI proposes, human approves" safety boundary
- Provider abstraction allows switching between Claude, GPT, and local models
- ChatHistory management handles session persistence

### What Semantic Kernel should own

- agent loop (receive -> prompt -> LLM -> parse -> tools -> loop),
- plugin registration and tool dispatch,
- filter chain for safety, approval, and audit,
- provider abstraction for LLM calls,
- chat history and session management.

## 8. Concurrency and Execution Model

### Decision

Use a supervised runtime model rather than ad hoc notebook threads.

The system should support:

- device workers,
- runtime orchestration,
- event propagation,
- health monitoring,
- ordered startup and shutdown,
- hard stop propagation.

### Why

The project needs better structure than Python thread-heavy glue code if it is to become a general platform.

## 9. Testing Direction

### Decision

Testing should include both software tests and experiment-system tests.

V1 should eventually include:

- unit tests for schemas and transformations,
- integration tests for protocol adapters,
- simulated devices,
- replay tests from recorded data,
- runtime failure-path tests,
- stop-condition tests.

## 10. Documentation Direction

### Decision

Documentation should be generated alongside development and run artifacts, not added only at the end.

Required documentation streams:

- high-level project docs,
- architecture docs,
- device docs,
- protocol docs,
- experiment docs,
- run and data docs,
- AI-generated change explanations where appropriate.

## 11. Explicit Non-Decisions for Now

The following are intentionally not locked yet:

- final plugin packaging mechanism,
- exact inter-process communication mechanism between C# and Python,
- final UI component library beyond the desktop stack choice,
- reusable device-testing UI templates,
- distributed orchestration.

These should be decided later, once the core runtime and artifact model are working.
