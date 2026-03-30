# High-Rate Stream Buffering And Delivery Policy Plan

## Purpose

Implement the next experiment-plane slice above the current runtime sessions:

- a real buffered delivery path for high-rate streams,
- explicit consumer-side delivery policy instead of raw producer-thread fan-out,
- and migration of the highest-pressure UI consumers off direct per-frame callbacks.

This slice is intentionally narrow. It should not build the recorder, the processing pipeline, or cross-session validation yet.

## 1. Clarify The Slice

### Goal

Add a shared high-rate delivery policy for runtime stream ports so Orchestral can:

- keep a full ordered stream available for downstream consumers,
- coalesce high-rate streams for UI-style latest-only consumers,
- rate-limit or bound delivery when needed,
- and stop treating the panel thread as the first buffering layer.

### In Scope

- extend `IStreamOutputPort<T>` with a buffered subscription path
- add shared delivery-policy contracts for:
  - latest-only delivery
  - ordered delivery
  - bounded buffering
  - overflow/drop behavior
  - optional rate limiting
- keep the raw `Produced` event for compatibility, but mark it as the low-level path
- migrate high-rate panel consumers off `LatestFrame.Changed` and onto buffered frame subscriptions:
  - integrated camera
  - HuaTeng camera
  - integrated microphone
- migrate `MicrophoneFrameJournal` onto the ordered buffered path as the first non-UI consumer example
- add runtime tests that prove:
  - ordered delivery preserves sequence
  - latest-only delivery coalesces bursts
  - bounded buffers apply the configured drop policy

### Out Of Scope

- recorder or artifact-writer implementation
- run-monitor surface
- camera-pair processing logic
- output-routing orchestration
- cross-session validation
- full replacement of panel-level `IntegrationPanelOutputPublisher`

### Success Criteria

- high-rate runtime streams no longer rely only on synchronous producer-thread event fan-out
- high-rate UI consumers no longer subscribe directly to every camera or microphone frame snapshot change
- runtime tests prove the new buffering semantics under burst/load conditions
- camera and microphone panels still show live data truthfully after migration
- the architecture docs describe buffered delivery as the preferred high-rate path

### Landing Target

- branch `codex/runtime-io-microphone`

## 1.1 Required Foundation Docs

### Required Foundation

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [TIMING_AND_SYNCHRONIZATION_STRATEGY.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/TIMING_AND_SYNCHRONIZATION_STRATEGY.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)
- [2026-03-28-functional-unit-roadmap.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/2026-03-28-functional-unit-roadmap.md)

### Supporting Reference

- [TURBULENCE_EXPERIMENT_FLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/TURBULENCE_EXPERIMENT_FLOW.md)
- [DATA_OUTPUT_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DATA_OUTPUT_INVENTORY.md)
- [DEVICE_INVENTORY.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/legacy-knowledge/DEVICE_INVENTORY.md)

### Conflict Winner

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)

## 2. Current Reality

- `StreamOutputPort<T>` currently invokes all subscribers synchronously on the producer thread.
- `IStreamOutputPort<T>` exposes only `event Action<T> Produced`.
- camera and microphone panels currently bind `LatestFrame.Changed` and marshal every frame to the WPF dispatcher.
- panel-level `IntegrationPanelOutputPublisher` only shapes output after the frame has already crossed into the panel layer.
- current tests only prove raw event notification and do not cover overload, coalescing, or bounded delivery.

## 3. Ordered Tasks

1. Add failing runtime tests for:
   - ordered buffered delivery
   - latest-only coalescing under a slow consumer
   - bounded ordered delivery with explicit drop policy
2. Implement the shared runtime delivery contract:
   - delivery mode
   - overflow policy
   - delivery policy record
   - buffered subscription interface
   - `IStreamOutputPort<T>.Subscribe(...)`
3. Upgrade `StreamOutputPort<T>` to maintain buffered subscribers while keeping raw event compatibility
4. Migrate the highest-pressure consumers:
   - integrated camera panel
   - HuaTeng camera panel
   - integrated microphone panel
   - microphone frame journal
5. Run full verification
6. Run external review
7. Address findings, reverify, and sync shared docs

## 4. Owned Files

Primary:

- `platform/src/ExperimentalControlPlatform.Runtime/IStreamOutputPort.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamOutputPort.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamDeliveryMode.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamOverflowPolicy.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/StreamDeliveryPolicy.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/IStreamDeliverySubscription.cs`
- `platform/src/ExperimentalControlPlatform.Runtime/MicrophoneFrameJournal.cs`
- `platform/tests/ExperimentalControlPlatform.Runtime.Tests/StreamBufferedDeliveryTests.cs`

Likely secondary:

- `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- `platform/src/ExperimentalControlPlatform.App/DevicePanels/Microphone/IntegratedMicrophonePanelViewModel.cs`
- `docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md`
- `docs/development/V1_EXECUTION_TRACK.md`

## 5. Review Gate

External review is required because this slice is:

- shared runtime foundation,
- architecture-affecting,
- runtime/lifecycle adjacent,
- and hard to validate from low-rate tests alone.

Preferred reviewer channel:

- Claude Code CLI with `claude-opus-4-6` when available
- otherwise a bounded fallback review artifact through an available reviewer CLI

## 6. Verification Gate

Minimum:

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`

Behavior checks:

- targeted high-rate stream buffering tests in `ExperimentalControlPlatform.Runtime.Tests`
- one local panel smoke launch for a camera or microphone panel after the consumer migration

## Result

- shared runtime buffering contract added through:
  - `StreamDeliveryPolicy`
  - `StreamDeliveryMode`
  - `StreamOverflowPolicy`
  - `IStreamDeliverySubscription`
  - `IStreamOutputPort<T>.Subscribe(...)`
- `StreamOutputPort<T>` now supports:
  - ordered buffered delivery
  - latest-only coalescing
  - bounded buffering with overflow policy
  - optional max delivery rate
- high-rate panel consumers for:
  - integrated camera
  - HuaTeng camera
  - integrated microphone
  now use buffered frame subscriptions instead of direct `LatestFrame.Changed` delivery
- `MicrophoneFrameJournal` now uses ordered buffered delivery as the first non-UI runtime consumer example
- architecture and execution docs were synced in:
  - [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
  - [V1_EXECUTION_TRACK.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/development/V1_EXECUTION_TRACK.md)
- verification passed:
  - `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
  - `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`
  - `dotnet run --project .\\temp\\panel-window-launcher\\panel-window-launcher.csproj`
    - launcher stayed alive until timeout, which is the expected smoke result
- review artifacts:
  - [2026-03-30-high-rate-stream-buffering-and-delivery-policy-review-gemini-3-flash-preview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-high-rate-stream-buffering-and-delivery-policy-review-gemini-3-flash-preview.md)
  - [2026-03-30-high-rate-stream-buffering-and-delivery-policy-response.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-high-rate-stream-buffering-and-delivery-policy-response.md)
  - [2026-03-30-high-rate-stream-buffering-and-delivery-policy-rereview-gemini-3-flash-preview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-30-high-rate-stream-buffering-and-delivery-policy-rereview-gemini-3-flash-preview.md)
