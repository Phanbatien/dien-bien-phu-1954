# Long UI verification

Scope: T35, T36, T19, T48 and T27. Implementation began 10 October 2026; final verification continues on 11 October 2026 (Asia/Saigon).

The game now has a profile menu, mission selection, bunker preparation and upgrade purchases, an M1 controls tutorial, a live HUD, pause controls and results/replay screens. The missing temporary single-profile storage was replaced with independent profiles and atomic snapshot saves.

## Evidence and boundaries

- Scoring tests cover BR-25's 360, 300 and 125 examples, 0/12 seconds, clamped timing, halfway-up rounding per answer, wrong/timeout answers and invalid data.
- Storage tests use separate temporary folders. Duplicate names retain different IDs, independent progress and balances. Purchases survive reload and cannot deduct twice. Interrupted temporary saves and corrupted primary files recover from a backup. Failed saves roll back purchases.
- PlayMode checks run real project scenes. They cover profiles, consent, volume, locked missions, shop refusals, repeated button events, applied upgrade values, tutorial input locks, live damage/ammo/stamina/score display, loss/results, replay and active-profile mission unlocks.
- An additional test queues mouse and keyboard input through the Input System. Mission selection, Escape pause and Resume work through this input path. Long accepted profile names retain their full stored values and fit the list display.
- A locked save file reproduced a victory that previously failed before displaying results. The fix keeps results and exit controls available, shows that progress is unsaved, and offers a retry. The regression verifies that retry persists the unlock without firing victory twice. A second review found no remaining major issues in this fix.
- Captures were inspected at actual Game view resolutions, rather than resizing an unusually wide Game view. UI raycasts and pointer-click events reached mission, buy, deploy, tutorial and result-menu controls.
- The existing Sandbox quiz timeout/freeze regression also passed. Automated victory testing invokes the real mission completion logic with a controlled objective list; it is not evidence of a full human traversal of M1.
- A person outside the group has not yet played M1 without help. T36's implementation is ready, but that acceptance checkbox must remain pending.
- The checkout has no bunker-question/Resolve reward settlement flow. Results show the existing saved Resolve balance; these five tasks do not grant invented points or implement the separately assigned reward tasks. Shop verification uses temporary test balances.
- Screenshots use disposable demonstration profiles. Real player profiles were not changed by verification.

## Build troubleshooting

The initial Windows builds failed in the Input System linker hook while it inspected temporary PipelineEval/PipelineRunScript assemblies. A per-assembly diagnostic reproduced the same native GetName() string-conversion exception only on those temporary assemblies. The output-folder retry alone did not help. A clean script/domain reload clears the temporary assemblies; build immediately after that, before using eval/run_script again.

Final verification on 11 October 2026: **37/37 EditMode and 10/10 PlayMode checks passed**. The Windows build succeeded with zero errors. Its sole warning says the editor-control Pipeline is disabled in the standalone player; it does not disable gameplay. UI captures were inspected at 1366×768 and 1920×1080. The final player remained running after startup, with no exceptions or startup errors in [its log](player-smoke.log); only that temporary player was stopped. This headless launch checks player startup only, not standalone visual rendering. The Editor was left outside Play mode with MainMenu open.

Final test and build result files are saved alongside the screenshots: [EditMode](editmode-tests.json), [PlayMode](playmode-tests.json), [Windows build](build-final.json), [HUD](hud-1920.png), [menu](menu-1366.png), [shop](shop-1366.png), [tutorial](tutorial-1366.png), [results](results-1920.png).

## Startup correction — 12 October 2026

The user reproduced a blank MainMenu when pressing Play directly from the open scene. The earlier scene-flow tests explicitly loaded MainMenu after entering Play and missed this startup path. Reproduction showed no CampaignUI/buttons on direct Play, while reloading MainMenu created them. A new EditMode regression enters and exits Play from the open MainMenu without loading another scene: it failed before the fix and now passes. CampaignUI also checks the active scene after initial scene loading, while the existing object lookup prevents duplicate menus. All 38 EditMode checks passed after this correction.

## Reproduce

From the Unity project folder, with its Editor running:

1. `unity recompile --json`
2. `unity command run_tests --mode editor --async_tests true --json`, then `unity command test_status --json`.
3. `unity command run_tests --mode playmode --async_tests true --json`, then `unity command test_status --json`.
4. Open `Assets/_Project/Scenes/MainMenu.unity` and press Play for the normal profile/menu flow.
5. If the Input System build hook reports the temporary-assembly issue, request a clean script compilation, await completion and then build without another eval/run_script call.

`AgentScripts/PrepareCampaignUI.cs` can recreate the menu scene if absent and preserve existing mission scenes. `AgentScripts/CampaignEvidence.cs` is verification-only tooling using disposable profiles; it is not part of the game.

12 October final checks: 38/38 EditMode and 10/10 PlayMode passed; Windows rebuild succeeded with zero errors. Direct Editor Play now shows exactly one CampaignUI and six initial menu buttons. The rendered [startup menu](menu-startup-12.png) was inspected. Unity is left in Play mode at the menu, with no real profiles modified.

## Pre-push check — 12 October 2026

Fresh compilation and all 38 EditMode checks passed. The first full PlayMode run found a timing-sensitive mouse test (9/10 passed); the same mouse test passed in isolation. The test now waits for the rebuilt menu Canvas to settle and forces its layout update before calculating the pointer position. Gameplay code is unchanged by this test correction. Recompilation after this correction timed out, and the final gameplay rerun could not complete because the Editor stopped responding. The user closed the Editor and explicitly requested that the prepared changes be pushed to Long. The test timing correction has not been verified by a completed rerun; the earlier build and full-suite results above remain historical evidence.
