## Re-Review: Post-Clarification Pass

### 1. Remaining Findings

**None blocking.**

**Non-blocking: `core_experiment_function` remains the vaguest class.** Unchanged from original review. Still fine for V1 — just expect it to be the first classification debate when onboarding a new experiment type.

**Non-blocking: Single example.** The turbulence mapping is still the only concrete example. The docs are clearer now about generality, so this is less of a concern, but a second sketch would still strengthen the case.

### 2. Verification of Clarifications

**V1 class set openness — sufficient.** All three docs now contain the identical sentence: *"In V1, this should be treated as a closed set of canonical top-level function classes."*
- Architecture: line 47
- Block contract: line 56
- Language spec: line 76

Consistent. No ambiguity remains between "first canonical" and "should be one of."

**Calibration/reference promotion criterion — sufficient.** All three docs now state the same three-part test: *independent schedule, outputs, or operator-visible identity in the experiment design.*
- Architecture: line 63
- Block contract: lines 103–104
- Language spec: lines 96–97

The wording is near-identical across all three, which is good. The criterion is concrete enough to resolve the "usually" ambiguity without over-constraining.

**Cross-document alignment — intact.** Checked:
- The four classes appear identically in all three docs (same names, same order).
- The exclusion list (monitor, lifecycle, orchestration, calibration) appears consistently.
- Architecture and block contract both point to `SYSTEM_LANGUAGE_SPEC.md` as the canonical source for class meanings (architecture line 53, block contract line 64).
- The block contract V1 summary now includes rule 2 (`function_class`) and renumbers correctly (7 rules total).
- The language spec relationship graph at line 432 includes the new function-class edge.

No terminology drift detected.

### 3. Verdict

**Accept.** The three clarifications (closed enum, promotion criterion, canonical-meaning delegation) are all sufficient, consistently applied, and do not introduce new misalignment. The original open questions 1 and part of open question 2 from the first review are now resolved. Open question 3 (feedback/closed-loop wiring across classes) remains deferred, which is fine for V1.
