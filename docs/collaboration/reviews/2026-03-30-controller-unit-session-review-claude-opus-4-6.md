---

## ControllerUnitSession Slice Review

### Blocking Issues

**None found.** The slice is architecturally coherent with the existing runtime/session model and honestly scoped.

---

### Semantic Mismatches

#### 1. `ControlOutputValue` equals raw error in closed-loop mode ¡ª misleading name

- **Severity:** Medium
- **File:** `ControllerUnitSession.cs:96-98`
- **Issue:** In closed-loop mode, `ControlOutputValue = errorValue` (i.e., just the signed error). The name "ControlOutputValue" implies an actuator-ready command, but this is actually just the proportional error term. In open-loop mode, `ControlOutputValue = targetValue`, which is a setpoint pass-through ¡ª also not really a "control output" in the classical sense.
- **Why it matters:** A downstream consumer (the future `ExperimentMonitorSession`, alarm surface, or any actuator adapter) will see `ControlOutputValue` and reasonably assume it can be forwarded to hardware. In closed-loop mode it's just `target - measured`, which is only useful as a P-term input, not as a command. If someone wires this to an actuator without noticing, the output has no gain, no units, and no saturation. The plan explicitly calls this out of scope ("PID tuning or advanced controller algorithms"), but the field name doesn't signal that.
- **Recommended fix:** Either (a) rename to `RawControlSignal` or `ProportionalError` in the closed-loop path to make it obvious this is a placeholder, or (b) add a `ControlOutputInterpretation` string field on `ControllerDecision` (e.g., `"proportional_error"` / `"setpoint_passthrough"`) so consumers can programmatically detect what kind of value they're getting. Option (b) is better for the monitor/alarm surface that's next.

#### 2. `MeasuredValue` fallback to previous state on null input

- **Severity:** Medium
- **File:** `ControllerUnitSession.cs:94`
- **Issue:** `var currentMeasuredValue = measuredValue ?? State.Current?.MeasuredValue;` ¡ª when `SampleAsync` is called without a measured value, it silently carries forward the last known measurement. The error is then computed against a stale value with no indication that it's stale.
- **Why it matters:** A monitor/alarm surface consuming `ControllerDecision` has no way to distinguish "measured at this timestamp" from "carried forward from an earlier sample." For a real-time control loop, stale-data-as-current is a safety-relevant silent failure mode. The architecture doc explicitly says the monitor should evaluate "stale-stream conditions."
- **Recommended fix:** Add a `bool MeasuredValueIsStale` or `DateTimeOffset? MeasuredValueObservedAtUtc` field to `ControllerDecision` and `ControllerUnitState` so downstream consumers can distinguish fresh from carried-forward values.

#### 3. `ControlTargetDefinition.MeasuredSourceId` refers to streams OR monitors ¡ª ambiguous type

- **Severity:** Low-Medium
- **File:** `ResolvedExperimentDefinition.cs:202-205`, `ControlTargetDefinition.cs:24`
- **Issue:** Validation builds `measuredSourceIds` from `experiment.Streams ¡È experiment.Monitors`, meaning `MeasuredSourceId` can point at either a stream or a monitor. But streams and monitors have fundamentally different runtime semantics (monitors may be display-only or derived-state per the spec, not necessarily stream-producing).
- **Why it matters:** When the `ControllerUnitSession` eventually needs to *subscribe* to the measured source at runtime (rather than receiving values pushed through `SampleAsync`), it needs to know whether it's subscribing to a stream port or polling a monitor snapshot. The current union hides that distinction. The system language spec (¡ì3.10) explicitly says "if a monitor produces time-varying data, that output must be named as a stream rather than implied by the monitor name alone."
- **Recommended fix:** Either (a) restrict `MeasuredSourceId` to stream IDs only (consistent with the spec's intent that monitors producing streams must declare them separately), or (b) add a `MeasuredSourceKind` discriminator (`"stream"` / `"monitor"`) to `ControlTargetDefinition`.

---

### Non-Blocking Improvements

#### 4. Schedule format is a raw semicolon-delimited string in parameter values

- **Severity:** Low
- **File:** `ControllerUnitSession.cs:257-277`
- **Issue:** Scheduled setpoint profiles are encoded as `"0:1600;10:1700;20:1800"` inside a string parameter value. The parsing is local to `ControllerUnitSession` and invisible to validation ¡ª the resolved experiment validates that the parameter *exists* and has a value, but not that the value is parseable as a schedule.
- **Why it matters:** A user can pass `"hello"` as the schedule parameter and validation will pass; the error only surfaces at `ControllerUnitSession` construction time. For the monitor/alarm surface, this means an experiment that looks valid at resolve time can fail at runtime startup.
- **Recommended fix:** Add schedule-format validation to `ResolvedExperimentDefinition.ValidateControlTargets` for control targets with `setpointProfile == "scheduled"`. This catches malformed schedules at binding validation time rather than session construction time.

#### 5. No `IDeviceSession` or common session interface implemented

- **Severity:** Low
- **File:** `ControllerUnitSession.cs:11`
- **Issue:** `ControllerUnitSession` implements `IAsyncDisposable` but does not implement `IDeviceSession` or any shared session interface. `ControlCenterSession` implements both `IDeviceSession` and `IAsyncDisposable`. The architecture doc says `IDeviceSessionRegistry` owns session lifecycle and `StopAll` goes through the registry.
- **Why it matters:** Without a common interface, the runtime coordinator and session registry cannot manage `ControllerUnitSession` through the same stop-authority path used for device sessions. The plan doc says this is an "experiment-plane runtime unit above device sessions," so a different interface may be intentional ¡ª but then the ownership boundary for who creates, stops, and disposes controller units needs to be explicit before the monitor surface consumes them.
- **Recommended fix:** This is a design decision for the next slice. Either (a) introduce `IExperimentPlaneUnit` as a sibling to `IDeviceSession` with at least `StopAsync` + output port discovery, or (b) document that the coordinator manages controller units through a separate path. The current slice is honest about this gap (plan ¡ì"Out Of Scope"), so no code change is needed now, but the monitor slice should not proceed without resolving this.

#### 6. `StopAsync` uses `DateTimeOffset.UtcNow` instead of a supplied timestamp

- **Severity:** Low
- **File:** `ControllerUnitSession.cs:195`
- **Issue:** `EndedAt = DateTimeOffset.UtcNow` ¡ª all other timestamps in the session flow through the caller (e.g., `observedAtUtc`, `runStartedAtUtc`), but the stop timestamp is captured internally. This creates a clock-source inconsistency if the runtime is ever tested with synthetic time or replayed from recorded data.
- **Why it matters:** Minor for V1, but the recorder and manifest will record this timestamp. If the stop was triggered 200ms before the session processes it, the manifest will show a stop time that doesn't align with the coordinator's stop event.
- **Recommended fix:** Accept an optional `DateTimeOffset? stoppedAtUtc` parameter in `StopAsync`, defaulting to `DateTimeOffset.UtcNow` if not supplied. Consistent with the pattern already used for `SampleAsync`.

#### 7. Thread-safety gap between `_syncRoot` lock and async `_applyDecisionAsync`

- **Severity:** Low
- **File:** `ControllerUnitSession.cs:77-91`, `ControllerUnitSession.cs:138-166`
- **Issue:** The `_syncRoot` lock protects `_stopped` and `_lastObservedAtUtc`, but is released before the async `_applyDecisionAsync` call. If two `SampleAsync` calls overlap (first releases lock, enters await on apply; second acquires lock, proceeds), both will publish state updates and decisions concurrently. The `PublishState` and `PublishDiagnostics` calls after the lock are also not protected.
- **Why it matters:** The architecture doc says "events are raised on the runtime producer thread or callback thread." If the caller guarantees single-threaded sample delivery, this is fine. But the session doesn't enforce or document that contract, and the lock on `_syncRoot` implies it's trying to be thread-safe.
- **Recommended fix:** Either (a) document that `SampleAsync` must be called sequentially (single caller, no overlapping calls), or (b) use `SemaphoreSlim(1,1)` instead of `lock` to hold exclusivity across the entire sample-publish-apply cycle. Option (a) is simpler and matches typical acquisition loop patterns.

---

### Summary

The slice is well-scoped and honestly bounded. The core modeling decisions ¡ª two-axis control target (setpoint profile ¡Á regulation mode), experiment-definition-level declaration, resolved-experiment validation, and runtime session with snapshot/stream output ports ¡ª are all consistent with the architecture and system language spec.

The three semantic mismatches (#1¨C#3) are worth resolving before the monitor/alarm surface consumes this unit, because they affect how downstream consumers interpret controller output. The non-blocking items (#4¨C#7) are lower priority and can be addressed as part of the monitor slice or later hardening.
