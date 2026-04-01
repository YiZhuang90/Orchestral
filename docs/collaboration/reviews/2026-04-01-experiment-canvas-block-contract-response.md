# Response: Experiment Canvas Block Contract

## Review Context

Review artifact:

- [2026-04-01-experiment-canvas-block-contract-review-claude-opus-4-6.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-04-01-experiment-canvas-block-contract-review-claude-opus-4-6.md)

## Response To Blocking Findings

### B1. V1-only implementation constraint stated too absolutely

Resolved in [EXPERIMENT_CANVAS_BLOCK_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_BLOCK_CONTRACT.md).

The summary rule now reads:

- `one role = exactly one active concrete implementation (V1 constraint)`

This keeps the current contract explicit without prematurely freezing the long-term data model.

### B2. Binding ownership unclear across block and manifest docs

Resolved across:

- [EXPERIMENT_CANVAS_BLOCK_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_BLOCK_CONTRACT.md)
- [EXPERIMENT_CANVAS_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_ARCHITECTURE.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

The ownership split is now explicit:

- canvas block = design-time intended binding
- resolved run manifest = run-time actual binding

This makes it clear that the canvas does not replace the manifest and the manifest does not replace the design surface.

## Response To Non-Blocking Notes

### N1. Readiness terminology drift

Resolved in [EXPERIMENT_CANVAS_BLOCK_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_BLOCK_CONTRACT.md).

The block contract now names the canonical states:

- `Ready`
- `Caution`
- `Blocked`

and maps them explicitly to:

- `Green`
- `Yellow`
- `Red`

### N2. "Ready to run" wording too runtime-shaped

Softened in [EXPERIMENT_CANVAS_BLOCK_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_BLOCK_CONTRACT.md) to:

- `structurally complete and ready to be submitted to a run with intended fidelity`

### N3. Readiness bullet list in canvas architecture not cross-referenced

Resolved in [EXPERIMENT_CANVAS_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_ARCHITECTURE.md) by linking the exact semantics back to the block-contract doc.

### N4. Intended source mode not defined

Clarified in:

- [EXPERIMENT_CANVAS_BLOCK_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_BLOCK_CONTRACT.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

The docs now state that intended source mode is recorded by the design-time experiment-building surface.

## Rereview Target

Please verify:

1. the V1-only active-implementation rule is no longer overstated,
2. binding ownership is explicit enough across canvas and manifest,
3. readiness terminology is now aligned with canonical names and visual encoding.
