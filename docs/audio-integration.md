# Audio integration — 21/10/2026

The user's choices are connected to the game: **MAT-49 = A (`cg1`)**, **Kiểu 50 = B (`Q009 minigun`)**, and the five music tracks remain assigned to M1–M5. Vietnamese field-speaker dialogue is excluded by the user. These are general game sound sources, not verified recordings of the historical weapons or vehicles.

## What is connected

| Game action | Sound |
|---|---|
| Mosin / scoped Mosin fires | Cycle through five cropped shot recordings without consuming gameplay randomness |
| MAT-49 fires | Approved A |
| Kiểu 50 fires | Approved B |
| Weapon switch / empty magazine | Prepared switch / dry sounds when no authored override exists |
| Walking, sprinting, crouching | Footsteps based on actual grounded distance, with quieter crouching; mud default and wood/stone/gravel/leaves selected from ground material or collider names |
| Distant artillery flash | Distant explosion synchronized to the existing flash |
| Mission background | Corresponding M1–M5 music and an airplane sound loop; no airplane model |
| Grenade / bunker / A1 explosion | Positional explosion fallback, independent of the destroyed object |
| Artillery strike / tank shell | General cannon fallback |
| Tank | Positional heavy-engine loop; stopped when destroyed; explosion fallback |
| Campaign buttons | Click sound |
| Correct field quiz / timeout | Reward / timeout sound |
| Successful mission-progress save | Saved sound after successful persistence |

The unapproved reload tails and rejected gun C were not imported. Existing authored reload sounds remain available, but this change does not invent a new weapon-specific reload sound. Other weapon IDs keep their existing authored fallback rather than borrowing the approved MAT-49/Kiểu 50 recording.

## Implementation and source records

- `AudioCatalog` maps weapon and scene IDs to the selected clips. `CampaignAudio` is created automatically per scene; authored mission scenes and prefabs are unchanged.
- World effects use 24 reusable voices. UI uses two separate voices that obey master volume and ignore the listener pause used for quiz freeze. Ordinary pause stops world sources; quiz freeze uses the existing listener pause/resume fade.
- There are **34 imported audio files** under `Assets/_Project/Resources/Audio`: 15 T28, 6 explosion/cannon/engine, 2 approved automatic-gun sounds, 2 switch/dry, 4 UI and 5 music. Variations are not automatically counted as separate accepted gameplay effects.
- Music imports as stereo streaming audio. Effects import as mono PCM. Imported sample levels are tested against prepared levels.
- [Imported source records and approved choices](../Assets/_Project/Audio/import-manifest.json), [authors, licenses and edits](../Assets/_Project/Audio/CREDITS.md). The CC BY-SA license for edited Q009 material is included alongside the credits; it is not applied to the other assets.

## Verification boundaries

The first three focused tests failed before implementation because the approved shots, music and imported sounds were missing. The six completed focused PlayMode checks cover approved gun routing, real playback, all five mission music assignments, scene cleanup, ordinary pause, quiz freeze/resume, UI pause/volume settings, imported clips, a bounded effect pool and grenade audio surviving grenade destruction. Three EditMode cases check imported peak levels and channels for a gun, footstep and UI sound.

Final Unity 6000.3.24f1 results: **103/103 EditMode and 29/29 PlayMode passed**, with no skipped tests. The first full PlayMode run passed 28/29; its mouse menu check failed. That check passed in isolation after focusing Unity, then the entire unchanged PlayMode suite passed 29/29 with Unity focused. Both the initial and final evidence are retained; no test was weakened or skipped.

Local results are stored under `Evidence/Audio/`; this directory is ignored by Git. See `focused-playmode.json`, `import-levels.json`, `editmode.json` and `playmode.json` for current run results. A separate code review found no confirmed actionable issues; it did not listen to audio or run Unity itself.

Automatic checks prove the tested routing and playback behavior. They do not prove that the mix sounds right across every combat situation, that every ground surface is named correctly, or that musical phrase changes are unobtrusive when tracks repeat. The selected music files retain their prepared entrance/exit fades; musical loops still need listening acceptance.

## Listening acceptance

Open `MainMenu` in Unity and press Play. Use a test profile, deploy to a mission, then listen to gunfire, movement and ambient sounds together. Check the volume slider, Escape pause/resume, a field question freeze/resume, and mission exit. On accessible later missions, check the selected music, cannon/tank and explosion events. Ground surfaces with unrecognized names deliberately use the mud default.

The user's approval settles the A/B and music choices. **T28/T49 and the sound portion of T69 remain pending whole-mission listening acceptance.** Reload choices remain unresolved; Vietnamese dialogue is excluded. The resolution checks in T69 are outside this audio integration and have not been newly verified here.

## Publication to Long — 21/10/2026

Prepared on the current develop base (`3d45690`) with the audio changes preserved. Fresh checks on Long passed 103/103 EditMode and 29/29 PlayMode with no failures or skipped tests. The local Unity Pipeline package upgrade is excluded from the audio commit. Listening acceptance and reload selection remain pending as described above.
