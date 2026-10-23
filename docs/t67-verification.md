# T67 lightweight effects — 23 October 2026

The approved project-made effects are implemented and tested. Final acceptance in the authored later encounters remains dependent on their scene work: tank/bunker/A1 components are not placed in M2–M5 in this checkout. Representative component checks are recorded separately from a full-mission playthrough.

## Implemented behavior

- Player/enemy muzzle flashes, ordinary-hit dust, metal-hit sparks, explosion bursts and smoke. Scoped player flashes are suppressed. Existing mission fog is retained.
- Fallbacks connected to grenades, bunker/tank destruction, tank shells, artillery and A1. Authored effect overrides remain usable. The old field-phone sphere flash now uses the same particle effects.
- A fixed scene-owned pool of 36 particle systems (12 muzzle, 8 dust, 8 sparks, 4 blast, 4 smoke), with one owned feathered texture/material. Smoke outlives its emitter, expires within 2.5 seconds, freezes with the world and is removed with its scene. Visual seeds do not consume gameplay randomness.
- Explicit URP shader under Resources; no external artwork pack or new runtime package. No particle collision or shadow effects.
- Deployment, menu return, restart and next mission fade through black: 0.12 seconds each way, unscaled time, asynchronous load, duplicate-request rejection and owned input lock cleanup.
- Independent review found Sandbox pause/result lock leaks and an unlocked cursor after a later-mission fade. Regression tests reproduced all three paths before the fixes. Tutorial locks remain intact.

No damage, ammunition, scoring or mission-completion rules were changed. Existing UI assertions remain; wait helpers now await the fade instead of assuming synchronous loading.

## Fresh verification

Unity **6000.3.24f1**, running Editor:

| Suite | Passed | Failed | Skipped |
|---|---:|---:|---:|
| EditMode | 103 | 0 | 0 |
| PlayMode | 38 | 0 | 0 |

Nine new PlayMode checks cover actual firing/impact, bounded objects, gameplay randomness, grenade smoke lifetime/freeze, scene cleanup, paused fades, duplicate/invalid requests, Sandbox paused/result restarts and the later-mission cursor. Initial effect/fade checks failed before implementation. Review regressions failed before their fixes.

Real bunker, tank and A1 component fixtures each emitted smoke through their own destruction/event paths. M2–M5 each load with fog and the shared pool. These fixtures are necessary because the corresponding components are not authored into the current later mission scenes.

Local ignored `Evidence/T67/` contains `red-effects.json`, `red-transitions.json`, `review-red.json`, `review-green.json`, `focused-effects.json`, `focused-transitions.json`, `editmode-final.json`, `playmode-final.json`, `later-missions.json`, captures and reproduction scripts.

## Visual and performance acceptance

Inspected actual rendered firing/impacts and blast/smoke in Sandbox/M1, additional M5 effect captures and a screen capture during the fade. Materials render with soft edges, without missing-material pink or opaque rectangular particle backgrounds. The visuals deliberately remain simple.

Matched runs used the real MAT-49 firing method at 10 shots/second, static camera and 1366×768 camera render target. AI was disabled equally in both conditions. Each run had 1 second warmup and 3 seconds sampling, with three alternating off/on pairs per scene. The idle pool exists in both conditions; the switch disables emission. VSync was off; target frame rate was **60 FPS**. Hardware: RTX 4060 Laptop GPU, Ryzen 7 8845H, Windows 11.

| Scene | Pair | Effects off FPS | Effects on FPS | Drop |
|---|---:|---:|---:|---:|
| Sandbox | 1 | 59.98 | 60.00 | -0.03 |
| Sandbox | 2 | 60.00 | 59.95 | 0.04 |
| Sandbox | 3 | 59.79 | 59.95 | -0.16 |
| M1 | 1 | 60.02 | 59.92 | 0.10 |
| M1 | 2 | 60.04 | 59.92 | 0.11 |
| M1 | 3 | 59.99 | 59.98 | 0.01 |

Largest measured decrease: **less than 0.12 FPS**, within T67's **5 FPS** limit in this controlled 60 FPS Editor test. Differences use unrounded values. This does not establish uncapped overhead, performance on other computers, a standalone build result or future fully authored encounter performance. Raw settings/runs: `performance.json`; paired comparisons: `performance-summary.json`.

## Remaining dependency and handoff

T59 belongs to **Bùi Nguyễn Thành Đạt**, and T60 to **Lê Nguyễn Đình Khôi** in the source tracker. Their later encounter objects still need placement/integration in this checkout. Once that work lands, verify those actual encounters with the already-connected fallbacks. Long's T67 checkbox remains open for that final scene acceptance; implementation, automatic checks and the current performance result are recorded separately.

Temporary Unity automation package settings are restored after verification. The user subsequently requested committing and pushing T67/T69 to Long; merging into other branches remains with the user. Publication does not close the outstanding scene-acceptance dependency.
