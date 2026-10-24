# T69 polish — 23 October 2026

## Implemented

The technical audio mix and visible feedback are implemented. Existing approved audio sources and the profile master-volume slider are retained. Final subjective whole-mission listening is still a separate acceptance step.

- Relative gains: music 0.22, aircraft ambience 0.12, tank engine 0.28, steps 0.25, approved gunfire 0.62, ordinary world effects 0.55, weapon mechanics 0.35, click/save cues 0.40 and reward/timeout cues 0.55. These multiply existing per-event gains; they do not rewrite the source files or master volume. Existing authored overrides outside CampaignAudio retain their behavior.
- Gunfire, cannon and explosions briefly reduce music to 55% of its background level, then restore it using real time. This never changes AudioListener master volume. UI retains two independent voices, ignores listener pause and obeys master volume.
- Non-interactive dark/gold notices show correct-answer knowledge points, actually granted/leftover crate rewards, question timeout and successful persisted mission progress. No invented Resolve reward is displayed.
- At most three notices (current plus two pending), with 0.15-second fades and 2.5-second holds. They expire during pause/freeze, clear on scene changes, and do not intercept mouse clicks. Placement changes outside gameplay so results navigation and leaderboard context remain visible.
- Final missions with no next mission now perform a real profile write before reporting successful persistence. Guest-only completion and failed writes do not produce a saved-success notice. Empty/unknown/duplicate-zero item rewards do not claim inventory items were granted.

## Screen verification

The actual Editor Game view was set to **1366×768** and **1920×1080**, with width/height assertions before capture. This uses screen captures including overlay UI, not camera-only renders. The original Game-view selection is restored and task-created temporary sizes are removed.

Captured and visually inspected at both sizes: menu, profile form, preparation/shop, tutorial, HUD, notice, pause, results and leaderboard — **18 screens**. Button bounds and button-label height checks report **0 errors**. Virtual mouse press/release on Mission 1 reaches preparation at both sizes. Profiles include 40-character wide ASCII and Vietnamese names; stored names remain complete while menu labels shorten for display.

Visual inspection caught a notice covering results navigation and part of the leaderboard metadata. A regression test reproduced the overlap; notices now move to clear space on those screens. No unrelated screen redesign was needed. `Evidence/T69/resolution-checks.json` and the PNGs retain the checks.

## Audio output verification and preview

Used Unity's built-in AudioRenderer to capture the actual mix with each M1–M5 music track, 10 gunshot cues/second, repeated explosion/footstep cues and reward/timeout/save UI cues. AI was disabled equally for the representative captures. Master volume was 0.8; stereo 48 kHz; approximately 4.992 seconds per track. Temporary capture frame settings, background mode and master volume were restored.

| Mix | Peak amplitude | RMS amplitude | Samples at/above 0.999 |
|---|---:|---:|---:|
| M1 | 0.350 | 0.100 | 0 |
| M2 | 0.402 | 0.116 | 0 |
| M3 | 0.332 | 0.100 | 0 |
| M4 | 0.403 | 0.116 | 0 |
| M5 | 0.333 | 0.100 | 0 |

The exported PCM WAVs were independently decoded and measured using Python's standard wave reader. All five contain nonzero audio and valid stereo/48 kHz/16-bit data. The combined preview orders **M1, M2, M3, M4, M5**, with half-second silence between excerpts: `Evidence/T69/t69-mix-preview.wav`.

These measurements establish headroom for this representative combination, not every possible simultaneous encounter or subjective listening quality. No human listening acceptance is claimed. The first callback-based capture returned no samples; it was rejected and replaced by the valid AudioRenderer captures above. The preview uses direct audio cue playback; it is not a recorded full-mission playthrough, and synthetic save/reward cues in the preview do not persist or grant game data.

## Automated verification and review

Initial focused checks: **0/5 passed** before implementation, failing on absent notices/ducking and full-volume music. Independent review identified final-mission no-op persistence and empty item reward success claims; both were reproduced with failing regression tests before fixes. A screen-overlap regression also failed before its placement fix. The timeout test initially exhausted Sandbox's only level-1 question; it now draws a remaining question without changing the no-repeat rule. Unknown weapon IDs intentionally log their existing catalog error, which the regression test explicitly expects.

Final Unity 6000.3.24f1 running-Editor results: **103/103 EditMode and 47/47 PlayMode passed**, with **0 failures and 0 skipped tests**. Nine new T69 PlayMode tests cover mix priorities, duck/recovery, correct/timeout notices, real/failed/final saves, empty and real item rewards, paused queue expiry/cleanup and unobstructed results navigation. Local evidence and reproduction scripts are under ignored `Evidence/T69/`, including `editmode-final.json` and `playmode-final.json`.

## Acceptance boundary

T69 implementation, screen-size verification and measured output are ready for review. The task's overall checkbox remains open until the user accepts the in-game sound balance after listening; T28/T49 listening and reload selection are not silently closed. Temporary automation package settings are restored at finish. Existing T67 work is preserved. The user subsequently requested committing and pushing T67/T69 to Long, leaving integration into other branches to them; publication does not imply listening acceptance.
