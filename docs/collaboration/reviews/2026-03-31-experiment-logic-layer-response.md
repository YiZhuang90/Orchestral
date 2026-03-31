# Review Response: Experiment Logic Layer

## Review Basis

- Review artifact: `2026-03-31-experiment-logic-layer-review-claude-opus-4-6.md`
- Reviewer verdict: no blocking issues

## Response By Finding

### 1. ControllerUnitSession / ExperimentMonitorSession boundary ambiguity

Status: addressed

Change:
- updated `EXPERIMENT_LOGIC_LAYER.md` to state explicitly that `ControllerUnitSession` and `ExperimentMonitorSession` are runtime-infrastructure slots
- updated `DEVICE_RUNTIME_IO_ARCHITECTURE.md` to say the same thing from the runtime-side document
- clarified that experiment-specific policies, projections, and monitor rules sit on top of those slots as experiment logic
- clarified that `experiment plane` is the umbrella term and `experiment logic` is the experiment-specific layer within it

### 2. "experiment-plane" vs "experiment-logic" overlap

Status: addressed

Change:
- folded the terminology clarification into `EXPERIMENT_LOGIC_LAYER.md`
- the docs now define `experiment plane` as the broader above-session area and `experiment logic` as the experiment-specific layer within it

### 3. Flow/Reynolds unit still shown as missing in the roadmap

Status: addressed

Change:
- updated `2026-03-28-functional-unit-roadmap.md`
- `Flow/Reynolds derived-state unit` is now marked `Built`
- added a note that the first real `FlowReynoldsDerivedStateSession` already exists and what remains is broader experiment-logic composition around it

### 4. FlowReynoldsDerivedStateSession described in both docs

Status: accepted

Reason:
- this is now intentional
- the runtime architecture doc describes where it executes and how it relates to runtime ownership
- the experiment-logic doc explains why it is classified as experiment logic rather than universal runtime

## Verification

Docs-only slice.

Verification run:
- `git diff --check`

No code build or test step was required because the slice only changes documentation and architecture guidance.
