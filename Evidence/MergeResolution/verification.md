# Long / develop merge verification

Verified on 14 October 2026 (Asia/Saigon).

Starting branches: Long `d44b8a775d159b11e62708b9c0063c56cab3b344`; develop `ba2b2474284514e60790e82c39d4489adaaedb1f`.
The local merge is recorded as `2829203`; the subsequent compatibility fixes complete that resolution.

## Resolution

- ProfileService keeps Long's multi-profile repository and develop's per-profile rewarded-question history. Failed purchases return SaveFailed after the repository rolls back the change.
- MissionController keeps Long's result-screen save retry and develop's save-failure flag, clearing both error indicators on a successful retry. Develop's drop tracking is retained.
- Existing develop saves in profiles/<id>/progress.json are imported only when neither the new profiles.json nor its backup exists. All profiles are read before a single atomic save. Corrupted primary files can use their backups; unreadable data fails visibly. Names, class, consent, balance, purchases, unlocked missions, and rewarded questions are preserved; old files remain intact.
- Develop's storage tests now use isolated temporary repositories and continue checking round trips, backup recovery, and rollback. Added checks cover separate reward histories, save-failure status, and importing two old profiles without duplicates or modifying the original files.
- Fixed imported compile errors in BalanceConfig, DroppedWeapon, and AcceptanceTestRunner. The latter reports the interrupted-quit scenario as requiring an actual check rather than treating the existence of a service as proof.
- Restored Long's existing W-MAUSER definition, still referenced by the scenes and scene builder, alongside develop's expanded weapon catalog. Catalog tests check required weapon identifiers and the catalog's existing rocket ammunition label.
- Assets/Settings/Mobile_RPAsset.asset was already modified before this work and is excluded from the resolution commit. No authored scenes were rewritten.

## Fresh verification

- Unity 6000.3.24f1: compilation succeeded with no reported errors.
- Full EditMode suite in the user's open Editor: **70/70 passed**.
- Full PlayMode suite in the user's open Editor: **14/14 passed**.
- Git whitespace check passed. No unresolved paths or conflict markers remain in project C# files.
- Develop is an ancestor of the resolved Long branch.

Local raw results: Logs/merge-editor-results-final.json and Logs/merge-editor-play-results-final.json.

The failed-purchase regression failed with an IOException before the compatibility fix; the save-status regression failed before flag synchronization; the old-save migration regression found zero profiles before migration was implemented. They pass in the final suites.

Earlier batch-mode runs exposed two menu failures that did not reproduce in the user's open Editor; both passed in the final full gameplay run. A later scene-reward failure identified the missing Mauser definition and passed after restoration.

## Boundaries

These checks verify compilation, automated gameplay flows, and the merge resolution. No new standalone build or full human mission traversal was performed. Release content approval remains separate: the restored Mauser definition retains its existing empty sourceId. The pre-existing MissionSupplyPoint rocket ammunition mismatch was noted during review but is outside these conflict changes.

No remote branch was pushed and develop was not directly updated by this work.
