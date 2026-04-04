# Data Output Inventory

## Purpose

This document captures the data products produced or expected by the preserved pipe-flow system.

## Reference Sources

- [exp_control_functions.py](../../legacy/pipe-flow-reference/python/exp_control_functions.py)
- [Looping_exp_main.ipynb](../../legacy/pipe-flow-reference/notebooks/Looping_exp_main.ipynb)

## Output Families

## 1. Two-point tracking / main helper output family

The active `data_log_at_end(raw_data_path)` path in the preserved helper module always writes the Plotly HTML summary and conditionally writes the text/CSV data files when `log_data` is enabled.

### Temperature record

- Filename pattern:
  - `..._Temp_record.txt`
- Source:
  - `temp_array`
- Meaning:
  - time-series temperature data with channel values, mean temperature, delta temperature, and elapsed time.

### Reynolds-number record

- Filename pattern:
  - `..._Re_record.txt`
- Source:
  - `Re_record`
- Meaning:
  - time-series Reynolds number and flowrate state derived from pulse-count telemetry and temperature.

### Signal records

- Filename patterns:
  - `..._data_p1.txt`
  - `..._data_p2.txt`
- Meaning:
  - reduced signal traces for point 1 and point 2 after the legacy processing pipeline.

### Puff classification table

- Filename pattern:
  - `..._{puff_count}puffs.csv`
- Source:
  - `puff_log`
- Meaning:
  - per-puff tracking/classification table including:
    - Reynolds number,
    - radius ratio,
    - puff index,
    - point-1/point-2 classification,
    - head/tail/center positions,
    - puff widths,
    - bug marker field.

### Plotly summary

- Filename pattern:
  - `..._{puff_count}puffs.html`
- Meaning:
  - HTML summary plot combining signals and related experiment statistics.
- Write behavior:
  - written unconditionally by `data_log_at_end(...)`.

## 2. Flow-control and controller tuning side outputs

### Flowrate/Re quick record

- Filename pattern:
  - `#{exp_index}_Re_record.cvs`
- Source:
  - `acquire_flowrate_control_center_version(...)`
- Meaning:
  - direct control-center Reynolds-number record.
- Important note:
  - the extension is `.cvs` in code, which appears to be a typo rather than a deliberate format choice.

### Reynolds control debug record

- Filename pattern:
  - `#{exp_index}_WT{iteration_time},Kp{Kp},ReAVe{average_count}.csv`
- Source:
  - `Re_control_center(...)`
- Meaning:
  - controller debug or tuning trace for Reynolds-number regulation.

## 3. Looping-analysis output family used by the notebook

The preserved notebook's analysis cells load files such as:

- `*_data_detection.txt`
- `*_data_verification.txt`
- `*_Re_record.txt`

These are clearly part of the real historical workflow because later notebook cells load and analyze them. However, the currently preserved helper module no longer contains an active matching `data_log_at_end_looping_version(...)` implementation.

### Implication

For the looping path, the preserved notebook and the preserved helper module have drifted apart. So the file expectations below are strong historical evidence, but not fully reconstructible from the helper module alone:

- detection signal trace,
- verification signal trace,
- Reynolds-number trace,
- and the notebook later derives and writes `*_time-turbulence_fraction.txt` as an analysis output rather than loading it as an input.

In Orchestral, these should become explicit run outputs rather than notebook-dependent assumptions.

## 4. Metadata embedded in filenames

The legacy system encodes important metadata directly into filenames, including:

- experiment index,
- radius ratio (`RR`),
- Reynolds number,
- exposure times,
- concentration,
- gain settings,
- puff count.

This is useful evidence of what the operator cared about, but it is also a sign that metadata was not yet fully separated from file naming.

## Migration Notes

The new system should preserve the scientific content of these outputs while moving toward:

- structured run manifests,
- explicit typed outputs,
- decoupled metadata,
- stable schema for signal traces,
- stable schema for per-event / per-puff tables.
