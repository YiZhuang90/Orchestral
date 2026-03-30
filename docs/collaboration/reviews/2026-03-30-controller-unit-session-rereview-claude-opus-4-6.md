# ControllerUnitSession Slice Re-Review

**Reviewer:** Claude Opus 4.6 (re-review)  
**Date:** 2026-03-30  
**Branch:** `codex/runtime-io-microphone`  
**Original review:** `2026-03-30-controller-unit-session-review-claude-opus-4-6.md`  
**Response artifact:** `2026-03-30-controller-unit-session-response.md`

## 1. Verification Summary

| # | Finding | Severity | Claimed Status | Verified Status | Notes |
|---|---------|----------|---------------|-----------------|-------|
| 1 | `ControlOutputValue` misleading name in closed-loop mode | Medium | Accepted | **Resolved** | `ControlOutputInterpretation` added to both `ControllerDecision` and `ControllerUnitState`. The session publishes `"proportional_error"` and `"setpoint_passthrough"` and the runtime tests assert both values explicitly. |
| 2 | `MeasuredValue` stale fallback undetectable | Medium | Accepted | **Resolved** | `MeasuredValueObservedAtUtc` and `MeasuredValueIsStale` were added to both records. The session preserves the original observation timestamp on carry-forward and marks stale measurements truthfully. Dedicated stale-path tests cover the timestamp, flag, and status-message behavior. |
| 3 | `MeasuredSourceId` accepts monitor IDs | Low-Medium | Accepted | **Resolved** | Validation now builds measured-source IDs from experiment streams only. The architecture doc and system language spec now explicitly say monitor identities are not valid measured sources. A dedicated validation test proves a monitor ID is rejected. |
| 4 | Schedule format validated only at runtime | Low | Accepted | **Resolved** | `ControlTargetScheduleParser` was extracted and is now used both by resolve-time validation and runtime construction. Invalid schedule strings now fail with `invalid_control_target_schedule_format` before session construction. |
| 5 | No common session interface | Low | Accepted with defer | **Resolved (deferred)** | The defer is explicit and appropriate for this slice. No code change was needed yet; the response records the design boundary that should be resolved in the monitor/alarm slice. |
| 6 | `StopAsync` uses internal `UtcNow` | Low | Accepted | **Resolved** | `StopAsync` now accepts an optional caller-supplied timestamp and falls back to `UtcNow` only when none is supplied. A dedicated test verifies the supplied timestamp is preserved in `SessionEnd`. |
| 7 | Thread-safety gap with `lock` + async apply | Low | Accepted | **Resolved** | The controller unit now uses `SemaphoreSlim` across the entire sample-publish-apply cycle, and `StopAsync` uses the same gate. A concurrency test verifies overlapping `SampleAsync` calls do not overlap the apply callback. |

**All 7 findings: resolved.**

## 2. Code Review Notes

- The fixes are narrow and honest. They address the original issues without introducing a larger control-framework abstraction prematurely.
- `ControlTargetScheduleParser` is the right extraction point. It lets validation fail early while keeping runtime parsing semantics aligned with the resolved experiment contract.
- The stale-measurement fix is semantically correct because it preserves the original measurement timestamp instead of restamping the carried-forward value as if it were fresh.
- The `SemaphoreSlim` change is structurally sound: the gate now protects `SampleAsync` and `StopAsync` symmetrically, which prevents interleaving of state mutation, publication, and async apply callbacks.
- The architecture/language docs now match the code: a derived value must be surfaced as a named stream before a control target can use it, and the controller unit consumes streams rather than monitor identities.

## 3. Build and Test

Per the response artifact:

- Core tests: `42/42` passed
- Device tests: `14/14` passed
- App tests: `14/14` passed
- Runtime tests: `67/67` passed
- Full build: `0 warnings, 0 errors`

This re-review was read-only, so the reviewer did not rerun the suite directly.

## 4. New Findings

None blocking.

Minor observation:

- `BuildStatusMessage(...)` still formats status as `Target +/- Error` even when `RegulationMode = open_loop`. That is cosmetically arguable, but it is not a correctness or architecture problem for this slice.

## 5. Verdict

**Pass.** All seven original findings are resolved. The fixes are well-scoped, correctly implemented, and backed by targeted tests plus a clean full-solution verification pass.
