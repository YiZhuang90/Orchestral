# Panel Close And Apply-Exit Plan

## Goal

Finish the operator-facing panel lifecycle so that:

- the title-bar `X` discards unapplied edits but disconnects safely before the panel closes,
- acquisition panels expose `Apply and Exit` in the main panel for consistency with controlled-device panels,
- detailed-settings `Apply` remains the explicit save action,
- and the saved applied-settings snapshot remains the current device-metadata artifact that later experiment orchestration can quote.

## In Scope

- window-close policy for device panels
- acquisition-panel `Apply and Exit`
- main-panel lifecycle consistency
- explicit metadata-save semantics through applied-settings output
- verification and doc sync for the new behavior

## Out Of Scope

- new persistent metadata database
- coordinator/orchestration layer
- redesign of diagnostics surfaces
- changing `LatestOnly` / `OnChange` output modes

## Required Foundation

- [DEVICE_RUNTIME_IO_ARCHITECTURE.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md)
- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)
- [DEVICE_PANEL_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/DEVICE_PANEL_CONTRACT.md)
- [MODULE_DEVELOPMENT_WORKFLOW.md](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/docs/development/MODULE_DEVELOPMENT_WORKFLOW.md)

## Conflict Winner

- [INTEGRATION_PANEL_IO_CONTRACT.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/architecture/INTEGRATION_PANEL_IO_CONTRACT.md)

## Tasks

- [x] Add a panel-close policy seam so `X` means `disconnect and close without apply`
- [x] Add `Apply and Exit` lifecycle support to acquisition panels
- [x] Ensure `Apply` from detailed settings updates the applied-settings snapshot as the current device metadata artifact
- [x] Keep `Diagnostics` in the main panel
- [x] Verify build/test and review behavior

## Verification

- `dotnet build .\\platform\\ExperimentalControlPlatform.sln -nodeReuse:false`
- `dotnet test .\\platform\\ExperimentalControlPlatform.sln`

Result:

- build passed
- tests passed: `78/78`

## Review

- review:
  - [2026-03-28-panel-close-and-apply-exit-review.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-28-panel-close-and-apply-exit-review.md)
- response:
  - [2026-03-28-panel-close-and-apply-exit-response.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-28-panel-close-and-apply-exit-response.md)
- re-review:
  - [2026-03-28-panel-close-and-apply-exit-rereview.md](C:/Users/Yi%20Zhuang/.config/superpowers/worktrees/Orchestral/runtime-io-microphone/docs/collaboration/reviews/2026-03-28-panel-close-and-apply-exit-rereview.md)

Outcome:

- initial Opus review found one `P2` thread-affinity issue on `CloseRequested`
- follow-up fix landed and Opus re-review marked the slice merge-ready
