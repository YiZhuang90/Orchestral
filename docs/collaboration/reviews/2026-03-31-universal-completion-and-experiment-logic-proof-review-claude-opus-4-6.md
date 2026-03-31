**No blocking issues.** The three documents are internally consistent and the sequencing is sound. Here are the specifics:

## Sequencing: correct

- Universal hardening (4 units) before experiment-logic proof is the right call. Each of those 4 units is genuinely universal and none depends on experiment-specific meaning.
- Using Reynolds as the minimum proof target is correct ¡ª it already exists, already consumes runtime outputs, and already feeds controller/monitor/recorder. No new science is needed to prove the boundary.
- The execution track (V1_EXECUTION_TRACK.md ¡ì3) correctly routes through the new planning doc and doesn't skip ahead to broad experiment-logic work.

## Boundary definition: correct

- The "runtime infrastructure slot vs. experiment-specific policy plugged into that slot" distinction (EXPERIMENT_LOGIC_LAYER.md lines 132¨C148) is clean and avoids the common mistake of classifying the host as experiment logic.
- `FlowReynoldsDerivedStateSession` is correctly identified as experiment logic despite being already built inside the runtime branch ¡ª the plan acknowledges it needs to be *relocated*, not rebuilt.

## Residual low-severity risks

1. **Ordering dependency between units 1 and 2 is underspecified.** Control-center capability decomposition (unit 1) may reshape the session topology that cross-session validation (unit 2) needs to validate. The plan lists them in the right order but doesn't state whether unit 2 has a hard dependency on unit 1's output. If it does, call that out explicitly so a future implementer doesn't try to parallelize them.

2. **Replay/simulator harness scope is broad.** "Reusable hardening and offline-test infrastructure" could expand to cover recorded-stream playback, mock device injection, time-travel semantics, etc. Without a scope fence, this unit could delay the experiment-logic proof indefinitely. Consider adding a one-line scope bound (e.g., "replay harness V1 covers single-session recorded-stream playback only").

3. **Reynolds relocation path is implicit.** The proof plan says "create an explicit code-level home" and "use Reynolds as the first proof case," but doesn't say whether that means *moving* the existing `FlowReynoldsDerivedStateSession` out of the runtime namespace or *re-exporting* it from a new experiment-logic namespace. Either works, but leaving it ambiguous means the proof-slice implementer will have to make an architectural call mid-implementation. Worth a one-sentence preference in the plan.

4. **V1_EXECUTION_TRACK.md ¡ì1 status bullet (line 52) contradicts the new plan.** It says "the next missing work should not be framed as 'one more universal runtime unit'" ¡ª but the new plan defines exactly four more universal units. The intent is clear in context (those four are hardening, not new core), but the phrasing could confuse a future reader. A small edit to say "should not be framed as new first-generation runtime creation" would align the language.

None of these block execution. All are fixable with one-line clarifications.
