# Legacy Knowledge Extraction Plan

## 1. Purpose

This document defines how the current pipe-flow control system should be used during development of the new platform.

The old system should not be treated as the software foundation. It should be treated as structured legacy knowledge.

## 2. Why This Stage Exists

The existing system already contains valuable information:

- real hardware integrations,
- real protocol usage,
- real signal-processing logic,
- real monitoring logic,
- real control heuristics,
- real output formats,
- real operating habits from successful experiments.

Throwing that knowledge away would be wasteful. Copying its structure into the new platform would also be a mistake.

So the project needs a deliberate extraction step.

## 3. Extraction Goals

The extraction process should identify and document:

- all devices currently involved,
- how each device communicates,
- what settings matter for each device,
- what data each device produces,
- what commands each device accepts,
- what the central control logic actually does,
- what variables are monitored,
- what stop or failure conditions already exist implicitly,
- what data products are saved,
- what experiment assumptions are embedded in the current workflow.

## 4. Primary Source Files

The main sources are:

- [exp_control_functions.py](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/legacy/pipe-flow-reference/python/exp_control_functions.py)
- [Looping_exp_main.ipynb](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/legacy/pipe-flow-reference/notebooks/Looping_exp_main.ipynb)
- [PipeFlowComputer.py](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/legacy/pipe-flow-reference/python/PipeFlowComputer.py)
- [pufffuncs.py](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/legacy/pipe-flow-reference/python/pufffuncs.py)
- [Statistics.py](C:/Users/Yi%20Zhuang/OneDrive/Codes/Projects/Orchestral/legacy/pipe-flow-reference/python/Statistics.py)

Secondary source material may include:

- hardware manuals,
- vendor SDK docs,
- serial command notes,
- old run outputs,
- user knowledge that was never written down in the code.

## 5. Extraction Categories

### 5.1 Device inventory

For each device, capture:

- role in experiment,
- vendor/model,
- connection method,
- protocol family,
- control parameters,
- output data type,
- health expectations,
- known quirks.

### 5.2 Protocol inventory

For each device protocol, capture:

- transport type,
- command style,
- expected responses,
- timing assumptions,
- polling or push mode,
- failure behaviors,
- reconnect expectations,
- protocol constraints that matter to runtime design.

### 5.3 Experiment logic inventory

Capture:

- major control loops,
- trigger logic,
- monitoring logic,
- parameter dependencies,
- startup sequence,
- shutdown sequence,
- operator actions required for a successful run.

### 5.4 Signal and analysis inventory

Capture:

- raw signals,
- derived signals,
- detection logic,
- synchronization logic,
- important thresholds,
- analysis outputs that matter operationally.

### 5.5 Data and logging inventory

Capture:

- files produced,
- file meaning,
- metadata currently implied but not formalized,
- run-to-run comparability assumptions,
- data structures worth preserving.

### 5.6 Safety and stop inventory

Capture:

- manual stop paths,
- implicit stop conditions,
- obvious missing stop conditions,
- device-failure effects,
- stale-data risks,
- operations that should trigger automatic stop in the new system.

## 6. Desired Output of the Extraction

The extraction stage should produce structured knowledge artifacts such as:

- device notes,
- protocol notes,
- experiment operation notes,
- signal-processing notes,
- data schema notes,
- safety notes,
- migration candidates list.

This output should feed directly into:

- schema design,
- protocol model design,
- runtime model design,
- stop-policy design,
- migration planning for the turbulence experiment package.

## 7. Rules for Using Legacy Code

- Reuse knowledge before reusing structure.
- Reuse algorithms where sensible.
- Do not preserve notebook-centered orchestration.
- Do not preserve uncontrolled global state as an architectural pattern.
- Do not preserve ad hoc thread design as the new runtime model.
- Preserve experimentally validated behavior where it is scientifically important.

## 8. Success Criteria

This stage is successful when the new platform team can answer:

- what hardware exists,
- how each device communicates,
- what the experiment actually needs,
- what outputs matter,
- what must be monitored,
- what should cause a stop,
- and which parts of the old code are worth migrating as logic rather than as structure.
