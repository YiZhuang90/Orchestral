# Response: Top-Level Experiment Function Classes

## Review Context

Review artifact:

- [2026-04-01-top-level-experiment-function-classes-review-claude-opus-4-6.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-04-01-top-level-experiment-function-classes-review-claude-opus-4-6.md)

## Response To Findings

The review returned no blocking findings. Two non-blocking clarifications were still worth taking immediately.

### V1 class-set openness

Clarified in:

- [EXPERIMENT_CANVAS_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_ARCHITECTURE.md)
- [EXPERIMENT_CANVAS_BLOCK_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/EXPERIMENT_CANVAS_BLOCK_CONTRACT.md)
- [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md)

The docs now state explicitly that the four top-level experiment-function classes are a **closed set in V1**.

### Calibration/reference promotion criterion

Clarified in the same three docs.

The docs now state that calibration/reference logic should stay nested by default and should only be promoted when it has:

- its own independent schedule,
- its own outputs,
- or its own operator-visible identity in the experiment design.

### Repeated definitions

Reduced slightly by making the architecture and block-contract docs point back to [SYSTEM_LANGUAGE_SPEC.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/SYSTEM_LANGUAGE_SPEC.md) for canonical meanings.

The list of classes still appears in more than one doc because:

- the canvas architecture doc needs the top-level pattern in place,
- the block contract needs the allowed field values in place,
- and the language spec remains the semantic source of truth.

## Rereview Target

Please verify:

1. the V1 class set is now explicit enough,
2. the calibration/reference promotion rule is now concrete enough,
3. the three docs still remain aligned after the clarification pass.
