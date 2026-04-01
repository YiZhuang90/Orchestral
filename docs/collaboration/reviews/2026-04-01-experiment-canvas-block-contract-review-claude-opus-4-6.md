## Review: Experiment Canvas Block Contract Diff

### 1. Findings

**Blocking**

| # | Issue | Location | Why blocking |
|---|-------|----------|--------------|
| B1 | **"one role = exactly one concrete implementation" is stated as absolute, but the architecture doc says "one role may be fulfilled by different concrete implementations"**. The block contract (line "V1 should not allow multiple prepared implementations on one role at the same time") scopes this to V1, but the summary table at the bottom of BLOCK_CONTRACT drops the V1 qualifier: `3. one role = exactly one concrete implementation`. An implementer reading only the summary will build a data model that structurally forbids multiple implementations per role, making the V2 relaxation a breaking change. | BLOCK_CONTRACT §4 vs summary rule 3; CANVAS_ARCH L101-103 vs L112 | Likely to lock the schema too early. |
| B2 | **Block ownership is still ambiguous across docs**. CANVAS_ARCH says blocks represent *functions*, but never explicitly says a block **owns** its roles/implementations. BLOCK_CONTRACT says a block "contains" roles. SYSTEM_LANGUAGE_SPEC §3.7 defines role binding as part of the *run manifest*, not the block. Who is the source of truth for the binding — the canvas block or the runtime manifest? If both, the sync rule is undefined. | BLOCK_CONTRACT §4, SYSTEM_LANGUAGE_SPEC §3.7 + new §3.7 addition | Implementers will disagree on whether the block or the manifest is authoritative for "current binding". |

**Non-blocking**

| # | Issue | Location |
|---|-------|----------|
| N1 | **Readiness terminology drifts.** BLOCK_CONTRACT uses `Green/Yellow/Red`. SYSTEM_LANGUAGE_SPEC §3.19 introduces `Ready/Caution/Blocked` as the canonical names with green/yellow/red as "visual encoding". The block contract never references the canonical names — it only uses colours. These should align or cross-reference. | BLOCK_CONTRACT §Readiness vs LANGUAGE_SPEC §3.19 |
| N2 | **"Ready to run" wording bleeds into runtime.** BLOCK_CONTRACT Green definition includes "ready to run with intended fidelity". The same doc says the block "is not the runtime control surface" and readiness is "not live runtime health". "Ready to run" implies runtime judgement. Consider "ready to be submitted to a run" or "structurally complete". | BLOCK_CONTRACT §Green |
| N3 | **CANVAS_ARCH new readiness bullet list** ("structural readiness, binding completeness, source-mode trust level") is a different decomposition than the traffic-light definitions in BLOCK_CONTRACT. Neither references the other. | CANVAS_ARCH L148-152 vs BLOCK_CONTRACT §Readiness |
| N4 | **SYSTEM_LANGUAGE_SPEC §3.19 Caution example** ("runnable only through virtual, replay, or synthetic source modes") assumes the *intended* mode is real hardware. The doc never defines "intended" source mode or where that intent is recorded. | LANGUAGE_SPEC §3.19 |
| N5 | **Plan doc landing target is `codex/runtime-io-microphone`**, which is unrelated to canvas block contracts. Looks like a leftover from a previous task. Minor, but confusing for anyone reading the plan. | Plan doc, Landing Target |

### 2. Open Questions

1. **Who owns the binding at rest?** Is the canvas block the single source of truth for role→implementation bindings, or does it merely *propose* bindings that the runtime manifest *resolves*? The docs need an explicit statement.
2. **Where is "intended source mode" recorded?** The Caution state depends on comparing actual vs intended mode, but no doc defines where intent lives (block metadata? run config? protocol?).
3. **Will V2 allow multiple prepared-but-inactive implementations per role?** If yes, the V1 data model should use a list with an `active` flag rather than a singular field, to avoid a migration. The contract should state this explicitly either way.
4. **Should BLOCK_CONTRACT reference SYSTEM_LANGUAGE_SPEC §3.19 for canonical readiness names?** Currently there is no cross-link.

### 3. Verdict

**Approve with required fixes for B1 and B2.**

- **B1**: Add `(V1 rule)` qualifier to the summary table entries, or better, make rule 3 read: `one role → exactly one **active** concrete implementation (V1 constraint)`.
- **B2**: Add one sentence to BLOCK_CONTRACT (or CANVAS_ARCH) stating whether the block is authoritative for bindings at design time and the manifest is authoritative at run time, or define the handoff explicitly.

The rest (N1–N5) are clean-up items that won't mislead implementation but should be addressed before the next round of docs lands.
