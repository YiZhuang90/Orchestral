Review updated at `docs/collaboration/reviews/2026-03-28-runtime-coordinator-and-stop-authority-review.md`.

**4 findings** (1 blocking, 3 non-blocking):

| ID | Sev | Area | Summary |
|----|-----|------|---------|
| **F-01** | **P1** | App-shell shutdown | `OnExit` bypasses the coordinator and calls the registry directly. Coordinator state stays `Running` during shutdown. Must fix. |
| F-02 | P2 | UI/async | `StopRuntime()` blocks the UI thread with `GetResult()`, so the `Stopping` state is never rendered -- the three-state model is invisible to the operator. |
| F-03 | P2 | App-exit race | If the user clicks Stop then closes the window, `OnExit` either double-stops the registry or throws on the duplicate `RequestStopAsync`. |
| F-04 | P3 | Test coverage | No tests for restart-after-failed-stop or cancellation-during-stop recovery. |

The coordinator core (state machine, lock discipline, `finally`-based recovery, registry delegation, duplicate-start/stop rejection) and the test suite are solid. F-01 is the gap the plan set out to close and should be fixed before landing. F-02 and F-03 share the same fix surface and are worth addressing alongside F-01.
