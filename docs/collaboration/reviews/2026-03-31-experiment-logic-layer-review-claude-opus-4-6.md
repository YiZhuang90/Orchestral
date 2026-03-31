## Review Findings

**No blocking issues.** The slice achieves its stated goal: experiment-specific units are reclassified, the layer boundary is defined, and the three affected docs (architecture, language spec, execution track) are updated consistently.

### Findings by severity

---

**1. Medium ！ ControllerUnitSession / ExperimentMonitorSession straddle the boundary without explicit classification**

The runtime architecture doc (`DEVICE_RUNTIME_IO_ARCHITECTURE.md:282-371`) describes both `ControllerUnitSession` and `ExperimentMonitorSession` as "experiment-plane runtime units" with detailed responsibilities. The experiment-logic doc (`EXPERIMENT_LOGIC_LAYER.md:78`) lists "experiment-specific control-target wiring" and "experiment-specific monitor rules" as experiment logic. The runtime doc also says "generic controller and monitor infrastructure" belongs in runtime (`EXPERIMENT_LOGIC_LAYER.md:55`).

The problem: neither doc explicitly says where ControllerUnitSession *itself* lives. It has generic infrastructure (resolve a target, evaluate profiles) and experiment-specific behavior (the specific wiring for a turbulence experiment). A reader implementing the next experiment's controller could reasonably place it in either layer.

**Suggested fix:** Add one sentence to `EXPERIMENT_LOGIC_LAYER.md` section "Relationship To Runtime" clarifying that ControllerUnitSession and ExperimentMonitorSession are *runtime infrastructure* ！ they are the generic slots. Experiment-specific *policies, rules, and projections* plugged into those slots are experiment logic.

---

**2. Low ！ "experiment-plane" vs "experiment-logic" terminology overlap**

The runtime doc uses "experiment-plane" broadly in at least 6 places (e.g., "experiment-plane artifact" at line 118, "experiment-plane measured state" at line 159). The new experiment-logic doc introduces "experiment logic" / "experiment-logic layer" as a distinct concept. These terms overlap without being defined relative to each other. "Experiment plane" appears to be a superset (anything above device sessions), while "experiment logic" is a specific layer within it.

**Residual risk:** Future contributors may treat the terms as synonyms and misclassify units. Low severity because the intent is clear from context, but it would be cleaner to either retire "experiment-plane" in favor of the new layered terms or define it explicitly as the union of runtime-for-experiments + experiment-logic.

---

**3. Low ！ Roadmap shows FlowReynoldsDerivedStateSession as "Missing" (stale)**

The functional-unit-roadmap (`2026-03-28-functional-unit-roadmap.md:315`) still lists `Flow/Reynolds Derived-State Unit` as `Missing`. It was built in the current branch. The execution track (`V1_EXECUTION_TRACK.md:46-51`) correctly reflects it as done. The roadmap predates this slice, but the plan's step 4 says "Update execution/roadmap docs so experiment-specific units move under the new layer" ！ the execution track was updated, but the roadmap was not.

**Residual risk:** The roadmap is the broader planning artifact. Leaving it stale while the execution track is current means readers could get contradictory status depending on which doc they check first. Low severity because the execution track is clearly marked as the active operational index.

---

**4. Info ！ FlowReynoldsDerivedStateSession described in both docs**

The runtime doc describes it at length (lines 129-156) as a current-status artifact. The experiment-logic doc also describes it (lines 122-127, 193) as the proof case for the layer. Not contradictory, but the authoritative behavioral description lives in the runtime doc while the classification lives in the experiment-logic doc. This is fine for now; just note that future experiment-logic units should have their primary description in experiment-logic docs, not in the runtime architecture.

---

### Summary

The slice is clean. The boundary is well-motivated and the three-layer model (runtime ★ experiment logic ★ AI) is consistent across all four docs. The main residual risk is the ControllerUnitSession/ExperimentMonitorSession ambiguity (finding 1), which is worth a one-sentence clarification before merging.
