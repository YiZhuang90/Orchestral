# PT-104 Validation Note

## Validation Context

This note records the first live PT-104 validation performed during AI-guided hardware integration.

## User-Provided Starting Information

- sensing element: `PT100 RTD`
- logger: `PT-104`
- active channel: `4`
- connection to PC: through the `PT-104`

No legacy pipe-flow integration code was used as prior knowledge for the inference step.

## Search and Inference Path

### Source priority followed

1. official/vendor sources first
2. local installed vendor SDK assets
3. no broader fallback sources were needed

### Key inferred facts confirmed

- the PT-104 is the PC-facing integration target
- the integration path is SDK-based, not serial text based
- the local machine already has the required vendor DLL installed
- channel configuration is mandatory before reading
- PT100 temperature values are returned scaled by `1/1000 C`

## Generated Test Artifact

- probe script: [pt104_probe.py](../probes/pt104_probe.py)

## Live Probe Result

The first successful live run produced:

- enumerate status: `0`
- detected device: `USB:HS337/004`
- open status: `0`
- mains set status: `0`
- channel set status: `0`
- first read status: `37`
- second read status: `0`
- second read raw value: `21501`
- interpreted temperature: `21.501 C`
- close status: `0`

## Revalidation After Repo Migration

After moving the probe into the Orchestral repo and rerunning it from the repo copy, the device still validated successfully:

- enumerate status: `0`
- detected device: `USB:HS337/004`
- open status: `0`
- mains set status: `0`
- channel set status: `0`
- first read status: `37`
- second read status: `0`
- second read raw value: `21275`
- interpreted temperature: `21.275 C`
- close status: `0`

## Interpretation

- The integration attempt succeeded on the first round.
- The device was discoverable and openable.
- Channel 4 could be configured as a 4-wire PT100 channel.
- A valid live temperature sample was obtained.
- The first read after configuration may not yet have a fresh usable sample.

## Current Confidence

- device identity: high confidence
- SDK path: high confidence
- initialization path: high confidence
- read scaling for PT100 temperature: high confidence
- parameter sensitivity around channel/type/wire count: high confidence

## Remaining Unknowns

The following are not yet validated in this first pass:

- behavior on channels other than 4
- 2-wire and 3-wire PT100 modes
- PT1000 mode
- unfiltered read behavior
- long-running acquisition behavior
- disconnection and recovery behavior
- Ethernet path

## Recommended Next Steps

1. promote this probe into a real Orchestral device test harness
2. add a minimal PT-104 test panel in the app
3. convert the validated settings into device/protocol artifacts
4. add a short acquisition mode with logged samples
