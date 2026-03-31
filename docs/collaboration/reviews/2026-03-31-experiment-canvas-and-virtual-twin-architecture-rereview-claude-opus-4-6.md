# Rereview: Experiment Canvas And Virtual Twin Architecture

## 1. Previous Blocking Findings

**B1. Phantom document references — RESOLVED.**
Both `EXPERIMENT_CANVAS_ARCHITECTURE.md` and `VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md` are present in the staged diff as new files with full content.

**B2. Device vs Concrete Implementation overlap — RESOLVED.**
`SYSTEM_LANGUAGE_SPEC.md` now has three distinct nouns:
- §3.4 `Concrete Implementation` — run-bound realization of a role (may be real, virtual, replay, synthetic)
- §3.5 `Device` — identifiable hardware endpoint or device-shaped twin
- Explicit statement: "replay and synthetic implementations are not themselves devices"

The boundary is clear enough for an implementer to decide which noun applies.

**B3. Experiment Role vs Device Role — RESOLVED.**
`SYSTEM_LANGUAGE_SPEC.md` now uses `Experiment Role` throughout (§3.3). The design-rules section, the graph section, and the canvas-order guidance all use the same term. `Device Role` no longer appears in the touched files.

## 2. Remaining Findings

### Non-blocking

**N1 (carried). Stage renumbering still undocumented.**
Old Stage I (guided escalation) is now Stage J in `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md`. No changelog note. Low risk, same as before.

**N2 (carried, improved). Source-mode ownership.**
Now consistently stated across three docs: selection belongs to experiment-building/binding surfaces, becomes a property of the resolved concrete implementation. The `DEVICE_RUNTIME_IO_ARCHITECTURE.md` line "should belong to experiment building or canvas-level system design" and the language spec's "selected by experiment-building or binding surfaces" are aligned. No remaining conflict.

**N3 (carried, improved). Canvas flow diagram direction.**
`EXPERIMENT_LOGIC_LAYER.md` now has a revised diagram where `Universal Runtime` feeds into `Experiment Logic Units` and `Concrete Implementations` feed into `Universal Runtime`. The arrow direction matches the stated layering. Resolved.

**N4 (carried, improved). V1 scope fence.**
Both `VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md` ("preferred closure target rather than a first-pass blocker") and `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md` ("read as a preferred closure target rather than as a first-pass blocker") now contain explicit V1 fences. The done criteria add scalar synthetic streams but keep broader simulation out of scope. Resolved.

**N5 (new). Worktree paths leaked into committed docs.**
Several cross-references use `C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/...` rather than repo-relative paths. These will break for any other reader or after the worktree is cleaned up. Examples in `DEVICE_RUNTIME_IO_ARCHITECTURE.md`, `V1_EXECUTION_TRACK.md`, and the plan file. Not blocking for architecture correctness but should be normalized before merge.

**N6 (new). `Experiment Function` is defined but never given a formal relationship to `Experiment` in the graph.**
§3.2 defines Experiment Function. The graph in §5 says "an experiment may be described in terms of one or more experiment functions" — this is good. But the graph also says "an experiment requires one or more experiment roles" directly, bypassing functions. This implies functions are optional groupings rather than mandatory structure. That's probably intentional but worth confirming, since the canvas doc says top-level blocks "should" map to functions.

## 3. Verdict

**Ready to land.** All three previous blocking findings (B1, B2, B3) are resolved. The remaining items are non-blocking cleanup:

- **N5** (worktree paths) should be fixed before or shortly after merge.
- **N6** (function optionality) is a design clarification, not a consistency bug.
