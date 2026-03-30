# High-Rate Stream Buffering And Delivery Policy Review

Reviewer channel:

- Gemini CLI `gemini-3-flash-preview`

Review scope:

- `platform/src/ExperimentalControlPlatform.Runtime/IStreamOutputPort.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/IStreamDeliverySubscription.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamDeliveryMode.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamOverflowPolicy.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamDeliveryPolicy.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamOutputPort.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/MicrophoneFrameJournal.cs`
- `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophonePanelViewModel.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/StreamBufferedDeliveryTests.cs`
- `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `docs/development/V1_EXECUTION_TRACK.md`

Gemini summary:

This review covers the implementation of high-rate stream buffering and delivery policy. The slice successfully introduces a decoupled delivery mechanism that significantly improves the reliability of the runtime-to-UI boundary.

## Blocking Issues

### HRSB-001

- Severity: `High`
- File: `platform/src/ExperimentalControlPlatform.Runtime/StreamOutputPort.cs`
- Issue: `Publish(...)` calls `_bufferedSubscriptions?.ToArray()` for every item.
- Why it matters: for high-rate streams this adds avoidable allocation pressure and GC churn directly in the publication hot path.
- Recommended fix: cache the subscriber snapshot and rebuild it only when the subscription list changes.

### HRSB-002

- Severity: `Medium`
- File: `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- File: `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
- Issue: the previous explicit `20 Hz` preview throttle was removed when the panels moved to buffered `LatestOnly` subscriptions.
- Why it matters: camera panels can still overdrive the UI if the dispatcher keeps up with every delivered frame.
- Recommended fix: initialize the camera preview subscriptions with `StreamDeliveryPolicy.LatestOnly(maxDeliveryRateHz: 20.0)`.

## Non-Blocking Improvements

### HRSB-003

- Severity: `Low`
- File: `platform/src/ExperimentalControlPlatform.Runtime/StreamOutputPort.cs`
- Issue: callback failure inside `PumpAsync()` is still silent from a consumer-observability perspective.
- Why it matters: if a consumer callback throws during shutdown or late-bind churn, the subscription can stop without surfacing a structured fault.
- Recommended fix: add a future fault-reporting hook or observable error surface on the subscription object.

### HRSB-004

- Severity: `Low`
- File: `platform/src/ExperimentalControlPlatform.Runtime/StreamDeliveryPolicy.cs`
- Issue: the initial `LatestOnly` factory policy encoded overflow semantics in a confusing way.
- Why it matters: it makes the policy harder to reason about when reading code or tests.
- Recommended fix: align the factory default with replacement semantics.

### HRSB-005

- Severity: `Low`
- File: `platform/tests/ExperimentalControlPlatform.Runtime.Tests/StreamBufferedDeliveryTests.cs`
- Issue: the disposal responsiveness test used a short delay-based assertion.
- Why it matters: small timeout-based tests can become flaky in slower CI environments.
- Recommended fix: switch to a more explicit completion wait.

## Verdict

Not merge-ready at review time.

Reason:

- the hot-path allocation and lost explicit camera preview cap should be fixed before merge
- the remaining notes are non-blocking improvements
