# Experiment Data Skeleton

## 1. Purpose

This document defines the standard file and folder structure for Orchestral experiment data. It is the normative reference for:

- how experiment projects are organized on disk,
- how individual run directories are structured,
- what storage formats are used for each data category,
- what is guaranteed to exist versus what is optional,
- and how any consumer (AI agent, analysis script, human) discovers and navigates run data.

This standard is required before the first reference experiment package (EF-04) can be built, because the runtime recorder, the agent stack, and manual analysis tools all need to agree on where data lives and how to find it.

References:

- [ROADMAP_V2.md](../development_v2/ROADMAP_V2.md) — Section 4, EF-02
- [PROJECT_VISION.md](../development_v2/PROJECT_VISION.md) — Principles 3 (data management), 4 (metadata and provenance), 8 (reproducibility)
- [TECH_STACK_DECISIONS.md](../development_v2/TECH_STACK_DECISIONS.md) — Section 5 (SQLite/Parquet/Zarr)

---

## 2. Two-Level Data Model

Orchestral experiment data is organized at two levels:

### Experiment Level (Shared Structure)

The **experiment definition** (`experiment.yaml`) declares the shared structure for all runs of that experiment: roles, parameters, streams, data schemas, monitors, stop conditions, and outputs. All runs of the same experiment share this structure definition.

This level answers: *what data should I expect from runs of this experiment?*

### Run Level (Per-Execution Record)

The **run manifest** (`manifest.yaml`) records what happened in one specific execution: actual parameter values used, timestamps, stop reason, which artifacts were produced, and where they are located. Each run is unique.

This level answers: *what actually happened in this specific run, and where is the data?*

The experiment definition defines the contract. The manifest records the fulfillment of that contract for one execution.

---

## 3. Experiment Project Hierarchy

```
<project_root>/
  experiments/
    <experiment_id>/
      experiment.yaml                    # experiment definition (shared structure)
      devices/                           # device definitions and role bindings
        <device_id>.yaml
      runs/
        <run_id>/                        # one directory per execution (see Section 4)
          manifest.yaml
          ...
      calibration/                       # (placeholder) per-device calibration records
      reports/                           # (placeholder) experiment-level reports
      decisions/                         # (placeholder) decision logs
```

### Node Descriptions

| Node | What It Holds | Format | Who Writes | When |
|------|--------------|--------|-----------|------|
| `experiment.yaml` | Experiment definition: roles, parameters, streams, monitors, stop conditions, outputs | YAML | Human or AI agent (with human approval) | Before first run |
| `devices/` | Device definitions and capability declarations | YAML | Human or AI agent | Before first run |
| `runs/` | All run directories for this experiment | — | RunArtifactWriter | At run completion |
| `calibration/` | Per-device calibration records | YAML | Future slice | Before or between runs |
| `reports/` | Experiment-level summary reports | Markdown/YAML | Future slice (AI-generated) | After runs |
| `decisions/` | Decision logs with rationale | YAML | Future slice (AI-captured) | During experiment lifecycle |

`calibration/`, `reports/`, and `decisions/` are defined here for completeness. They are not implemented in this slice.

---

## 4. Run Directory Hierarchy

Each run directory is created by `RunArtifactWriter` inside the `runs/` parent directory. The run directory name uses the format `{yyyyMMdd-HHmmss}_{RunId:N}` where the timestamp is UTC start time and the RunId is a Guid in N-format (no hyphens, 32 hex characters).

```
<run_id>/
  manifest.yaml                          # ALWAYS — single entry point for all consumers
  events/                                # ALWAYS — every run has runtime events
    runtime-events.yaml                  #   indexed event log
    warnings-or-faults.yaml              #   collected warnings and fault conditions
  snapshots/                             # ALWAYS — device panel state is always captured
    panels/                              #   one file per active device panel
      <panel_slug>.yaml                  #   panel state snapshot (data, settings, status, diagnostics)
    monitor-snapshot.yaml                # CONDITIONAL — only if experiment monitor was active
  metadata/                              # ALWAYS — every run has parameters and settings
    applied-parameters.yaml              #   experiment + role-binding parameter values
  data/                                  # CONDITIONAL — only when data artifacts exist
    raw/                                 # CONDITIONAL — raw device outputs (future: Parquet, Zarr)
    derived/                             # CONDITIONAL — derived/processed outputs
      flow-reynolds-snapshot.yaml        #   point-in-time derived state (if captured)
      flow-reynolds-record.yaml          #   time-series derived samples (if recorded)
```

### Directory Creation Rules

Directories are created based on content, not speculatively:

- `manifest.yaml`, `events/`, `snapshots/`, `metadata/` — **always created** for every run.
- `data/` — **only created** when data artifacts (raw or derived) exist.
- `data/raw/` — **only created** when raw device data files are written (future: Parquet/Zarr).
- `data/derived/` — **only created** when derived/processed data files are written.

Processed/derived data is always optional. Raw data can often be re-processed easily, so the absence of a `data/derived/` directory is a normal condition, not an error.

---

## 5. Guaranteed vs. Optional Content

| Content | Presence | Condition |
|---------|----------|-----------|
| `manifest.yaml` | **Always** | Every run produces a manifest |
| `events/runtime-events.yaml` | **Always** | Every run logs events |
| `events/warnings-or-faults.yaml` | **Always** | Every run collects warnings (may be empty list) |
| `snapshots/panels/` | **Always** | Panel snapshots are always captured (directory may contain zero files if no panels) |
| `metadata/applied-parameters.yaml` | **Always** | Every run has experiment parameters |
| `snapshots/monitor-snapshot.yaml` | Conditional | Only when experiment monitor was active during the run |
| `data/derived/flow-reynolds-snapshot.yaml` | Conditional | Only when derived state session was active |
| `data/derived/flow-reynolds-record.yaml` | Conditional | Only when derived state samples were collected |
| `data/raw/*` | Conditional | Only when raw device data recording is implemented (future) |

### Rule

A consumer should never assume a conditional directory or file exists. Use the manifest to discover what was produced.

---

## 6. File Naming Conventions

| Convention | Format | Example |
|-----------|--------|---------|
| YAML artifact filenames | kebab-case | `runtime-events.yaml`, `warnings-or-faults.yaml` |
| Panel snapshot filenames | slugified panel title | `integrated_camera.yaml`, `pt_104.yaml` |
| Run directory name | `{yyyyMMdd-HHmmss}_{RunIdN}` | `20260330-140000_11111111111111111111111111111111` |
| RunId in artifact IDs | first 8 hex characters of Guid N-format | `11111111` |
| Artifact ID format | `artifact.{kind}.{key}` | `artifact.runtime_events.11111111` |

### Slug Rules

Panel titles are slugified by: lowercase, replace non-alphanumeric with underscore, collapse double underscores, trim edge underscores. If two panels produce the same slug, a numeric suffix is appended (`_2`, `_3`, etc.).

---

## 7. Storage Format Mapping

| Data Category | Current Format | Future Format | Notes |
|--------------|---------------|---------------|-------|
| Manifests, events, snapshots, panel state | YAML | YAML | Human-readable, AI-parseable |
| Run metadata, applied settings, event indices | YAML | SQLite | Queryable metadata store |
| Scalar time-series (flow rate, Reynolds, temperature) | YAML | Parquet | Columnar, efficient for analysis |
| Multidimensional arrays, image sequences | Not yet recorded | Zarr | Chunked, cloud-friendly |
| Experiment definitions, device definitions | YAML | YAML | Artifact-first, always YAML |

Reference: [TECH_STACK_DECISIONS.md](../development_v2/TECH_STACK_DECISIONS.md), Section 5.

The transition from YAML to SQLite/Parquet/Zarr for applicable categories is a future slice concern. This spec defines where each format lives in the directory hierarchy; the format transition does not change the directory structure.

---

## 8. Consumer Contract

This section defines how any consumer — AI agent, analysis script, or human — discovers and reads data from a run.

### Entry Point

**`manifest.yaml` at the run directory root is the single entry point.** Every consumer starts here.

The manifest contains:

- `experimentId` and `experimentVersion` — link to the experiment definition
- `startedAt`, `stoppedAt`, `stopReason` — execution timeline
- `roleBindings`, `protocolBindings`, `parameterValues` — what was configured
- `outputIds` — list of all artifact IDs produced
- `artifacts` — dictionary mapping each artifact ID to its relative file path
- `runtimeEvents` — inline event log
- `warningsOrFaults` — inline warning summary

### Discovery Protocol

1. Read `manifest.yaml` at the run root.
2. Use the `artifacts` dictionary to locate any file by its artifact ID.
3. The artifact path is relative to the run directory, using forward slashes.
4. Read the target file. All YAML files use camelCase key naming.

### Rules for Consumers

- **Never hardcode paths** beyond `manifest.yaml` at the run root. Always use the manifest's `artifacts` dict.
- **Check before assuming.** Conditional content (monitor snapshots, derived data) may not exist. The manifest's `outputIds` tells you what was produced.
- **Derived data is optional.** The absence of `data/derived/` is normal. Raw data can often be re-processed from source.
- **Expect YAML with camelCase.** All structured artifacts are serialized as YAML with camelCase naming convention, null values omitted.
- **Forward slashes in paths.** Artifact paths in the manifest are normalized to forward slashes for cross-platform compatibility.

### Example: Finding a Panel Snapshot

```yaml
# In manifest.yaml:
artifacts:
  artifact.panel_snapshot.integrated_camera.11111111: snapshots/panels/integrated_camera.yaml
  artifact.panel_snapshot.pt_104.11111111: snapshots/panels/pt_104.yaml
```

A consumer looking for the camera panel reads `snapshots/panels/integrated_camera.yaml` relative to the run directory.

### Example: Checking for Derived Data

```yaml
# In manifest.yaml:
outputIds:
  - artifact.flow_reynolds_snapshot.11111111
  - artifact.flow_reynolds_record.11111111
artifacts:
  artifact.flow_reynolds_snapshot.11111111: data/derived/flow-reynolds-snapshot.yaml
  artifact.flow_reynolds_record.11111111: data/derived/flow-reynolds-record.yaml
```

If `outputIds` does not contain `artifact.flow_reynolds_snapshot.*`, no derived data was produced.

---

## 9. Placeholders (Future Slices)

### Calibration Records

- **Location:** `experiments/<experiment_id>/calibration/`
- **Purpose:** Per-device calibration records with timestamps, methods, and results.
- **Format:** YAML, one file per device per calibration event.
- **Status:** Location defined. Not implemented in this slice.

### Decision Logs

- **Location:** `experiments/<experiment_id>/decisions/`
- **Purpose:** Record experimental decisions with rationale, context, and outcomes.
- **Format:** YAML, chronologically ordered entries.
- **Status:** Location defined. Not implemented in this slice.

### Reports

- **Location:** `experiments/<experiment_id>/reports/`
- **Purpose:** Experiment-level summary reports (daily, weekly, or milestone).
- **Format:** Markdown or YAML, AI-generated with human review gate.
- **Status:** Location defined. Not implemented in this slice.
