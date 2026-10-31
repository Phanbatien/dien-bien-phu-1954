# Supplied enemy characters

The supplied `animation_model_ennemies` and `animation_model_enemy leader` folders are imported under `Assets/_Project/Art/Characters/Supplied/Enemies` and `EnemyLeader`. The ZIP archives contain the same files and were not duplicated in Assets. The supplied allies folder was empty, so current allies remain in place.

The rigged model inside each Firing Rifle FBX supplies the character visual. The separate source FBXs are unrigged reference models. All ten animation FBXs use valid Humanoid avatars. Root movement is baked so navigation remains controlled by the game. Regular-soldier mesh normals are calculated because the source has none.

Each set has a textured `Soldier.prefab` and a separate `Soldier.controller`, retaining the existing cover, ladder, melee, hit and officer actions. `Shoot` blends the supplied stationary/running firing clips by Speed. `StartWalking` plays when movement starts, then returns to the existing walking/running loop. `Dead` plays the complete 4.4-second supplied fall without the legacy extra body rotation, and fading waits until the clip ends. `Reload` is connected as a controller trigger and tested; the current AI has no magazine/reloading rules, so no new ammunition behavior was added.

Mission enemy visual replacements: M1 12 (1 officer), M2 18 (2), M3 12 (1), M4 16 (2), M5 14 (2), M6 6 (1), Sandbox 5 (1). Existing combat scripts, navigation, eye/muzzle references and allies are retained. Enemy animator references are reconnected to each new rig.

To rebuild the supplied visuals and reapply them, stop Play Mode, save open scene edits, and choose **DBP > Enemies > Apply supplied models and animations**. Open scenes are preserved. Future system wiring also chooses the supplied visuals when creating a soldier or officer.

Verification: both prefab checks and live animation checks passed, including completed action transitions and grounded final body positions and no double rotation/root shift on death. The existing character-action test passed. Full Edit Mode suite: 119 passed. Pose previews and local test evidence are under the ignored `Evidence/character-import` folder.

