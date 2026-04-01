## Review: Orchestral Documentation Diff

### 1. Findings

**Non-blocking: Definitions are repeated three times verbatim.**
The four function-class definitions (`condition_control`, `core_experiment_function`, etc.) and their one-line meanings appear identically in `EXPERIMENT_CANVAS_ARCHITECTURE.md`, `EXPERIMENT_CANVAS_BLOCK_CONTRACT.md`, and `SYSTEM_LANGUAGE_SPEC.md`. This creates a maintenance trap — the three copies will drift. Consider defining once in the language spec and referencing from the other two docs.

**Non-blocking: The turbulence example is well-fenced but is the only example.**
The "Turbulence Example Mapping" section has good disclaimers ("only a mapping example"). However, being the sole example still anchors the reader's mental model. A second one-paragraph sketch (even hypothetical — e.g., a generic "reaction kinetics" or "optical measurement" experiment) would prove generality more convincingly than disclaimers alone.

**Non-blocking: `core_experiment_function` is the vaguest class.**
`condition_control`, `order_parameter`, and `data_recording` have clear semantic boundaries. `core_experiment_function` is defined as "the main active scientific function" — which could absorb almost anything not covered by the other three. This isn't blocking for V1 (four classes is fine as a starting taxonomy), but expect classification debates here first.

**Non-blocking: Exclusion list is sound but calibration guidance is soft.**
"Calibration … should usually remain nested" appears in all three docs. The word "usually" is appropriate, but no criterion is given for when promotion *is* justified. A single sentence (e.g., "promote only when the calibration procedure has its own independent schedule and data output") would reduce future ambiguity.

**Non-blocking: Block contract now has rule 2 (`function_class`) but the field is not in the "conceptual parts" template.**
The contract adds `function class` to the identity section and adds rule 2, which is consistent. Good — no misalignment here.

**No blocking findings.** The top-level classes are experiment-domain concepts, not turbulence-specific. The block contract, canvas architecture, and language spec all agree on the four classes, the exclusion list, and the nesting guidance. No terminology drift detected between the three files.

### 2. Open Questions

1. Should the four function classes be an **open** or **closed** enum in V1? The language spec says "first canonical" (implying open), but the block contract says "should be one of" (implying closed). Worth an explicit stance.
2. Is there a planned validation rule that every canvas must have at least one `data_recording` block, or is "not every experiment must use all four" truly unconstrained?
3. Where does **feedback/closed-loop control** land? A PID loop that reads `order_parameter` output and adjusts `condition_control` sits across two classes. Is the loop logic nested inside one, or is it a wiring concern at the canvas level?

### 3. Verdict

**Accept.** The diff achieves its goal: the four function classes are domain-general, the turbulence mapping is clearly marked as an example, and the three documents stay aligned. The exclusion of monitor/lifecycle from top-level classes is coherent and well-motivated. The findings above are all improvable-later items, none blocking.
