# High-Rate Stream Buffering And Delivery Policy Response

## HRSB-001

- Decision: `Accepted`
- Fix summary: `StreamOutputPort<T>` now keeps a cached subscriber snapshot array and rebuilds it only when subscriptions are added or removed, removing per-publish `ToArray()` allocation from the hot path.
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/StreamOutputPort.cs`
- Retest status:
  - `dotnet test .\\platform\\tests\\ExperimentalControlPlatform.Runtime.Tests\\ExperimentalControlPlatform.Runtime.Tests.csproj --no-restore --filter StreamBufferedDeliveryTests -nodeReuse:false`
  - passed

## HRSB-002

- Decision: `Accepted`
- Fix summary: integrated camera and HuaTeng camera preview subscriptions now use `StreamDeliveryPolicy.LatestOnly(maxDeliveryRateHz: 20.0)` so preview delivery remains explicitly capped after the move away from panel-local frame throttling.
- Changed files:
  - `platform/src/ExperimentalControlPlatform.App/DevicePanels/Integrated/IntegratedCameraPanelViewModel.cs`
  - `platform/src/ExperimentalControlPlatform.App/DevicePanels/HuaTeng/HuaTengPanelViewModel.cs`
- Retest status:
  - `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
  - `dotnet test .\\platform\\ExperimentalControlPlatform.sln --no-build -nodeReuse:false`
  - passed

## HRSB-003

- Decision: `Partially accepted`
- Fix summary: the current slice still leaves subscription callback fault reporting as a future improvement. The hot path now safely observes faulted pump tasks so faults do not go fully unobserved, but there is still no structured fault-reporting hook on `IStreamDeliverySubscription`.
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/StreamOutputPort.cs`
- Retest status:
  - build and runtime tests passed
- Follow-up note:
  - add explicit subscription fault reporting only when a downstream diagnostics surface or monitor is ready to consume it

## HRSB-004

- Decision: `Accepted`
- Fix summary: `StreamDeliveryPolicy.LatestOnly(...)` now uses overflow semantics that match replacement behavior.
- Changed files:
  - `platform/src/ExperimentalControlPlatform.Runtime/StreamDeliveryPolicy.cs`
- Retest status:
  - runtime buffering tests passed

## HRSB-005

- Decision: `Accepted`
- Fix summary: the disposal responsiveness test now uses `WaitAsync(...)` instead of a short manual delay race.
- Changed files:
  - `platform/tests/ExperimentalControlPlatform.Runtime.Tests/StreamBufferedDeliveryTests.cs`
- Retest status:
  - runtime buffering tests passed
