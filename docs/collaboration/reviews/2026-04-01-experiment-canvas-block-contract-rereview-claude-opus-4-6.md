## Re-review: Experiment Canvas Block Contract

### 1. Remaining Findings

**Previous blocking findings**

| # | Status | Notes |
|---|--------|-------|
| B1 | **Resolved.** | Summary rule 3 (BLOCK_CONTRACT L220) now reads `one role = exactly one active concrete implementation (V1 constraint)`. The V1 qualifier is present both in the body (L86, L104) and the summary. No ambiguity remains about this being a permanent structural rule. |
| B2 | **Resolved.** | Ownership split is stated explicitly in BLOCK_CONTRACT L88-95, CANVAS_ARCH L112-115, and LANGUAGE_SPEC L141-144. All three docs agree: canvas block = design-time intended binding, resolved run manifest = run-time actual binding. |

**Previous non-blocking findings**

| # | Status | Notes |
|---|--------|-------|
| N1 | **Resolved.** | BLOCK_CONTRACT L135-145 maps canonical names (`Ready`/`Caution`/`Blocked`) to colors (`Green`/`Yellow`/`Red`). LANGUAGE_SPEC 3.19 uses the same canonical names and visual encoding. Aligned. |
| N2 | **Resolved.** | BLOCK_CONTRACT L156 now reads "structurally complete and ready to be submitted to a run with intended fidelity". No longer implies runtime judgement. |
| N3 | **Resolved.** | CANVAS_ARCH L151-161 lists the readiness decomposition and links to BLOCK_CONTRACT for exact semantics. |
| N4 | **Partially resolved.** | BLOCK_CONTRACT L171 says "intended source mode should be recorded in the design-time block binding". LANGUAGE_SPEC 3.19 L391 says "recorded by the experiment-building surface". Both state *that* it should be recorded, but neither defines the schema field or metadata key where it lives. Acceptable for an architecture doc — the field name is an implementation detail — but worth noting for the implementer. |
| N5 | **Not addressed.** | Plan doc landing target (L46-47) still says `codex/runtime-io-microphone`. This is a branch-naming nit and has no implementation impact, but it remains confusing for anyone reading the plan as a standalone artifact. |

**New findings**

| # | Issue | Location |
|---|-------|----------|
| N6 | **CANVAS_ARCH cross-reference is one-directional.** CANVAS_ARCH links to BLOCK_CONTRACT three times (L13, L46, L161). BLOCK_CONTRACT never links back to CANVAS_ARCH or LANGUAGE_SPEC. A "Read together with" section at the top of BLOCK_CONTRACT would help discoverability symmetrically. | BLOCK_CONTRACT top |

### 2. Verdict

**Approved.**

Both blocking findings (B1, B2) are cleanly resolved. The three docs are now consistent on the V1 implementation constraint, binding ownership, and readiness terminology.

Remaining items (N4 partial, N5, N6) are minor housekeeping — none will mislead an implementer.
