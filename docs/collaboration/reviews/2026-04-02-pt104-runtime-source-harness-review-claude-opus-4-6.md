This compiles because `Pt104Session` now implements `IPt104RuntimeSource`, so passing it to `AttachRuntimeSourcesAsync(…, IPt104RuntimeSource?)` is valid. Good.

I have all the information needed for the review.

---

## 1. Verdict

**No blocking issues.** The slice is clean, narrow, and correctly scoped. Ship it.

## 2. Findings

**All clear — no bugs or regressions found.**

- **Interface contract is well-shaped.** `IPt104RuntimeSource` exposes exactly the four output ports (`State`, `Diagnostics`, `LatestReading`, `Readings`) that `FlowReynoldsDerivedStateSession` actually consumes. No leaky abstractions.

- **`Pt104Session` backward compatibility is preserved.** Adding `IPt104RuntimeSource` to `Pt104Session`'s interface list is additive-only. The existing app-shell call site (`MainViewModel.cs:306`) passes `Pt104Session` directly and compiles without change because it satisfies the widened parameter type. No behavioral regression.

- **`Pt104RuntimeSourceBase` factoring is clean.** `PublishReading` updates all four ports atomically (state, diagnostics, latest reading, stream) — mirrors what `Pt104Session` does internally. The channel-existence guard (`InvalidOperationException` on undeclared channel) prevents silent data loss.

- **`ReplayAsync` is synchronous-in-async.** `Pt104ReplaySource.ReplayAsync` publishes all readings synchronously and returns `Task.CompletedTask`. This is intentional for test use — no real timing needed. Acceptable for the stated scope.

- **Dispose pattern is adequate.** Base class is `IDisposable` (not `IAsyncDisposable`), which is fine since replay/synthetic sources own no async-disposable resources. The test correctly uses `using var` for these sources.

- **Test coverage exercises the contract properly.** Both new tests (replay and synthetic) verify `UsesFallbackTemperature == false`, mean temperature, temperature delta, and Reynolds number presence — confirming the full derived-state pipeline runs without hardware.

## 3. Residual Risks / Missing Tests

These are **non-blocking** observations for future slices:

| Item | Risk | Severity |
|------|------|----------|
| **No detach/reattach test for virtual sources** | `DetachRuntimeSourcesAsync` unsubscribes `Readings.Produced`. No test verifies that a replay source can be detached without dangling subscriptions, then a new source reattached. The real `Pt104Session` path has the same gap. | Low |
| **No cancellation test for `ReplayAsync`** | `ReplayAsync` checks `cancellationToken.ThrowIfCancellationRequested()` per reading, but no test exercises mid-replay cancellation. Fine for V1 internal use. | Low |
| **Synthetic source noise bound is statistical, not deterministic** | The synthetic test uses `Assert.InRange` with bounds that could theoretically fail if the seeded random produces extreme values. The fixed seed (1234) makes this reproducible, so it's stable in practice, but the assertion is technically probabilistic. | Negligible |
| **`SeedTemperatureChannels` with base-class initial state** | `Pt104RuntimeSourceBase` initializes all 4 channels as `Available = false`. When `AttachRuntimeSourcesAsync` calls `SeedTemperatureChannels(pt104Source.State.Current?.Channels)`, it will see no available channels — so no seeding occurs. This is benign because readings arrive immediately after via `PublishReading`, but it means the seeding path is a no-op for virtual sources. Worth noting if seeding logic ever becomes load-bearing. | Low |

## 4. Recommendation

**Merge as-is.** The implementation is well-scoped, the contract is narrow and correct, backward compatibility is preserved, and tests cover the stated proof targets. The residual items above are all follow-up quality, not pre-merge blockers.
