# Orchestral Documentation Diff Review

## 1. Findings

### Blocking

**B1. Phantom document references — EXPERIMENT_CANVAS_ARCHITECTURE.md and VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md are not in the diff.**
Multiple files now link to these two documents as required foundations, but neither document's content appears in this diff. If they don't exist yet (or exist only as stubs), every "read together with" and "required foundations" pointer is a broken contract. Any implementer following `V1_EXECUTION_TRACK.md` or `DEVICE_RUNTIME_IO_ARCHITECTURE.md` will hit dead links.

**B2. "Device" noun now overlaps with "Concrete Implementation" in SYSTEM_LANGUAGE_SPEC.md.**
Section 3.5 (Device) still says *"A device is a concrete hardware or **simulated** instance that can be bound to a role."* The new section 3.4 (Concrete Implementation) also covers simulated/virtual/replay instances bound to roles. The two nouns share nearly the same definition space. An implementer building the binding layer won't know which noun to use for a virtual camera service — is it a Device or a Concrete Implementation? This needs an explicit disambiguation (e.g., Device = inventory entry, Concrete Implementation = runtime-bound instance with source mode).

**B3. Terminology drift: "Experiment Role" vs "Device Role".**
`EXPERIMENT_CANVAS_ARCHITECTURE.md` (referenced) and `V1_EXECUTION_TRACK.md` introduce the term **Experiment Role**. `SYSTEM_LANGUAGE_SPEC.md` still calls the same concept **Device Role** (section 3.3). The canvas flow diagram in `EXPERIMENT_LOGIC_LAYER.md` uses "Experiment Roles" while the language spec graph section still says "device roles." If these are intended to be the same noun, pick one; if they are different, define the boundary.

### Non-blocking

**N1. Stage numbering shift in AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md is undocumented.**
Old Stage I (guided escalation) becomes Stage J. Any external references to "Stage I" in plans, changelogs, or agent prompts will now point to the wrong stage. Low risk but worth a grep.

**N2. Source-mode ownership is stated twice with slightly different framing.**
`DEVICE_RUNTIME_IO_ARCHITECTURE.md` says the choice of source mode "should belong to experiment building or canvas-level system design rather than to panel-local lifecycle buttons." `SYSTEM_LANGUAGE_SPEC.md` defines source mode as a property of a concrete implementation. These aren't contradictory, but the ownership sentence in the runtime-IO doc could be read as saying the canvas *sets* the mode, while the language spec implies it's an intrinsic property of the implementation. Worth aligning the framing.

**N3. The EXPERIMENT_LOGIC_LAYER.md canvas flow diagram inverts the expected direction.**
The diagram flows `Experiment Function → Experiment Roles → Concrete Implementations → Universal Runtime → Experiment Logic Execution`. Having "Universal Runtime" feed *into* "Experiment Logic Execution" reverses the stated layering (universal runtime below, experiment logic above). The arrow should flow the other way, or the labels need clarification that this represents a build/resolution pipeline rather than a runtime call direction.

**N4. V1 scope creep for the replay/simulation harness.**
The done criteria now add "scalar synthetic streams such as mean-plus-noise sources" to V1. This is fine in isolation, but the virtual-twin stage in `AI_GUIDED_HARDWARE_INTEGRATION_SPEC.md` describes a much broader ambition (virtual device services implementing the full Orchestral contract). There is no explicit V1 fence in that spec, so an agent following the integration spec may build far more than the V1 plan intends.

## 2. Open Questions

1. **Do `EXPERIMENT_CANVAS_ARCHITECTURE.md` and `VIRTUAL_TWIN_AND_SIMULATION_STRATEGY.md` exist yet?** If not, should the references be marked as planned/pending?
2. **Is "Experiment Role" intended to replace "Device Role," or are they distinct nouns?** If distinct, what is a non-device experiment role?
3. **Where does source-mode selection actually live at runtime?** The diff describes it architecturally but doesn't name a responsible component (canvas? experiment definition? run manifest? binding resolver?).
4. **Should the virtual-twin stage (Stage I in the integration spec) be explicitly marked as optional or V1-deferred?** Currently it reads as a standard workflow stage, which could cause agents to block on it.

## 3. Verdict

**Not ready to land as-is.** Two blocking issues need resolution first:

- **B2** (Device vs Concrete Implementation overlap) will cause real confusion in binding-layer implementation. Add one sentence to section 3.5 clarifying the boundary.
- **B3** (Experiment Role vs Device Role) is a terminology split that will propagate inconsistently through code. Pick one canonical name or define both explicitly.

**B1** (phantom docs) is blocking if the referenced files don't exist; if they do exist and simply aren't in this diff, it's fine — just confirm.

Everything else is non-blocking and can be cleaned up in a follow-up pass.
