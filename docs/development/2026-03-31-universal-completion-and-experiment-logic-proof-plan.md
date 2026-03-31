# Universal Completion And Experiment-Logic Proof Plan

## Goal

Finish the remaining universal Orchestral units before expanding further experiment-specific logic, then run one minimum proof that the new `Experiment Logic` layer works as a real code boundary rather than only a documentation boundary.

## In Scope

- identify which universal units are still truly open
- define the order to finish those universal units
- define a minimum experiment-logic proof after universal completion
- update execution routing so future slices follow that order

## Out Of Scope

- implementing one of the remaining universal units in this slice
- implementing the minimum experiment-logic proof in this slice
- rewriting already-landed runtime foundation slices
- broad experiment-package implementation

## Governing Docs

Required foundation:

- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [REVIEW_PROTOCOL.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/REVIEW_PROTOCOL.md)
- [SUBAGENT_DEVELOPMENT_CONTRACT.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/collaboration/SUBAGENT_DEVELOPMENT_CONTRACT.md)
- [V1_EXECUTION_TRACK.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/V1_EXECUTION_TRACK.md)
- [EXPERIMENT_LOGIC_LAYER.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_LOGIC_LAYER.md)
- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

Conflict winner:

- branch-local execution and architecture docs define the current universal/runtime boundary
- the experiment-logic layer doc defines what must wait until after universal completion

## Current Reality

Universal runtime is now substantial and no longer missing its first-generation core:

- session layer
- registry
- panel session-client pattern
- operator controls
- session-local validation
- runtime coordinator and stop authority
- experiment definition and role binding
- high-rate stream delivery policy
- run context and metadata
- run recorder and artifact writer
- controller-unit infrastructure
- run monitor and alarm surface

The remaining universal work is now mostly hardening and reusable composition work rather than first-generation runtime creation.

## Remaining Universal Units

The remaining universal units should be treated as:

1. `Cross-session validation`
   - reusable validation over interacting sessions and experiment bindings
   - should follow control-center capability decomposition because the validation model needs the cleaned-up session topology and capability boundaries

2. `Experiment-definition linting`
   - reusable static checking before a run starts

3. `Replay / simulator harness`
   - reusable hardening and offline-test infrastructure
   - V1 scope should stay narrow:
     - single-session recorded-stream playback first
     - not full time-travel or broad multi-device simulation on the first pass

These should be completed before the first deliberate code-level proof of the experiment-logic layer.

`Control-center capability decomposition` is now complete in branch code with:

- explicit `LaserControl`, `PuffActuation`, and `FlowTelemetry` runtime capability surfaces,
- capability-oriented control-center command helpers above the shared serial protocol,
- panel and artifact semantics that no longer record the control center as one undifferentiated command surface,
- and downstream Reynolds wiring through the explicit flow-telemetry capability seam.

## Units Considered Closed Enough

These universal units are not perfect, but they are complete enough that they should not block the next sequence:

- device session foundation
- operator runtime controls
- session-local validation
- runtime coordinator and stop authority
- experiment definition and role binding
- high-rate stream buffering
- run context and metadata
- run recorder and artifact writer
- controller-unit infrastructure
- run monitor and alarm surface

## Recommended Universal Completion Order

1. `Cross-session validation`
2. `Experiment-definition linting`
3. `Replay / simulator harness`

## Done Criteria For The Remaining Universal Units

### 1. Control-center capability decomposition

Done when:

- the control-center session has explicit capability or endpoint separation for:
  - puff actuation
  - laser control
  - flow telemetry
- monitor, recorder, and command surfaces no longer treat those concerns as one vague lump
- tests prove the decomposed state/command semantics

### 2. Cross-session validation

Done when:

- the runtime can validate relationships across more than one session or role binding
- the validation result is structured and surfaced before run start
- at least one real multi-session contradiction is covered by tests

### 3. Experiment-definition linting

Done when:

- the platform can run static checks on an experiment package before initialization
- missing roles, invalid bindings, and broken parameter references are surfaced without starting hardware
- lint results are available as a reusable artifact or validation object

### 4. Replay / simulator harness

Done when:

- one recorded stream can be replayed through the runtime-facing consumer path without real hardware
- the harness is usable in automated tests
- V1 stays limited to single-session recorded-stream playback
- broader multi-device simulation remains out of scope for this pass

## Minimum Experiment-Logic Proof

After the remaining universal units are finished, the minimum proof should be:

1. create an explicit code-level home for experiment logic
   - preferred direction:
     - dedicated namespace and folder
     - or a dedicated project if the separation is clean enough

2. use the already-built Reynolds logic as the first proof case
   - `FlowReynoldsDerivedStateSession` is the best candidate
   - it already proves the right pattern:
     - consumes runtime outputs
     - derives experiment-specific state
     - feeds controller/monitor/recorder
   - preferred relocation path:
     - move it into the experiment-logic home
     - keep the runtime-facing interfaces stable
     - avoid a redundant re-export layer unless a compatibility seam is genuinely needed

3. run one integration proof that checks:
   - experiment-logic code consumes runtime sessions rather than raw adapters
   - derived state reaches controller and monitor
   - run artifacts still capture the result

## Success Criteria

- the team has one explicit list of the remaining universal units
- the next build order is unambiguous
- the first experiment-logic proof target is named explicitly
- future work stops bouncing back and forth between universal runtime and experiment-specific logic without a boundary

## Outcome

This plan now defines:

- the remaining universal-hardening set
- the order to finish those universal units
- done-criteria for each of those units
- the minimum experiment-logic proof target:
  - use Reynolds logic first
  - give experiment logic an explicit code-level home
  - prove the boundary through one runtime-to-experiment-logic integration path

Review trail:

- `2026-03-31-universal-completion-and-experiment-logic-proof-review-claude-opus-4-6.md`
- `2026-03-31-universal-completion-and-experiment-logic-proof-response.md`
- `2026-03-31-universal-completion-and-experiment-logic-proof-rereview-claude-opus-4-6.md`

Verification:

- `git diff --check`

This slice is docs-only.
