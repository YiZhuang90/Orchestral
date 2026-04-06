# SL-002: Experiment Data Skeleton — Slice Plan

## Goal

Define the standard file/folder structure for Orchestral experiment data and update the existing RunArtifactWriter to follow it. This produces both a written specification and a working implementation.

## In Scope

1. **Specification document** (`docs/architecture/EXPERIMENT_DATA_SKELETON.md`):
   - Standard folder hierarchy for one experiment project
   - Standard folder hierarchy for one run within a project
   - File naming conventions
   - Storage format mapping (which data → YAML, SQLite, Parquet, Zarr)
   - Metadata location and format
   - Where calibration records, decision logs, and reports will eventually go (placeholders for future slices)
   - AI data access contract: what paths and formats the agent can expect to find

2. **Recorder update** (code):
   - Update `RunArtifactWriter` to follow the new folder/naming standard
   - Update `RunRecorder` if its output path logic needs alignment
   - Ensure manifest.yaml, runtime events, panel snapshots, and monitor snapshots land in the correct locations per the spec
   - Update tests to verify the new structure

## Out Of Scope

- Calibration record implementation (placeholder in spec only)
- Decision log implementation (placeholder in spec only)
- Auto-report generation (placeholder in spec only)
- Parquet or Zarr writing (spec defines where they go; implementation comes later)
- SQLite metadata store (spec defines the intent; implementation comes later)

## Governing Docs

- Required foundation:
  - [ROADMAP_V2.md](./ROADMAP_V2.md) — Section 4, EF-02
  - [TECH_STACK_DECISIONS.md](./TECH_STACK_DECISIONS.md) — Section 5 (SQLite/Parquet/Zarr)
  - [SYSTEM_LANGUAGE_SPEC.md](../architecture/SYSTEM_LANGUAGE_SPEC.md) — Run Manifest, Output definitions
- Supporting reference:
  - [DEVICE_RUNTIME_IO_ARCHITECTURE.md](../architecture/DEVICE_RUNTIME_IO_ARCHITECTURE.md) — Output families
  - [PROJECT_VISION.md](./PROJECT_VISION.md) — Principles 3, 4, 8 (data management, metadata, reproducibility)

## Proposed Folder Structure (Starting Point)

```
<project_root>/
  experiments/
    <experiment_id>/
      experiment.yaml              # experiment definition
      devices/                     # device definitions and bindings
      runs/
        <run_id>/
          manifest.yaml            # run manifest
          events/                  # runtime events (YAML or JSON lines)
          snapshots/               # panel and monitor snapshots
          data/
            raw/                   # raw device outputs (future: Parquet, Zarr)
            derived/               # derived state outputs (future: Parquet)
          metadata/                # run metadata, applied settings
          artifacts/               # any additional run artifacts
      calibration/                 # per-device calibration records (placeholder)
      reports/                     # experiment reports (placeholder)
      decisions/                   # decision logs (placeholder)
```

This is a starting point for discussion during the brainstorming phase of the coding session. The exact structure should be finalized in the spec document.

## Success Criteria

1. `EXPERIMENT_DATA_SKELETON.md` exists and defines the standard
2. `RunArtifactWriter` produces output following the standard
3. Tests verify the new folder structure and file locations
4. The spec includes placeholders for calibration, decisions, and reports
5. The spec includes an AI data access contract section

## Estimated Complexity

Medium. The spec is the main design work. The recorder update is bounded — it already writes to a run directory, the change is primarily about path restructuring and naming.

## Dependencies

- LD-001 landed (provides the run recorder and artifact writer in the shared codebase) ✓

## Superpowers Skill Chain

`$using-git-worktrees` → `$brainstorming` (for spec) → `$writing-plans` (for recorder tasks) → `$subagent-driven-development` → `$requesting-code-review` → `$finishing-a-development-branch`
