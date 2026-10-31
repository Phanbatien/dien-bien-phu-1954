# Supplied allied animations

All six files from `model folder/animation_model_allies/animation` are imported under `Assets/_Project/Art/Animations/Allies/Supplied` and connected to the existing allied character.

- Run Forward replaces the running animation.
- Firing Rifle Standing and Firing Rifle provide alternating stationary firing variations; Speed blends into Firing Rifle Running while moving.
- Reload plays after five shots. Allies pause movement and combat for the 4.1-second clip, then resume. The last shot has a short lead-in before reload. Death cancels reload behavior.
- Dying replaces the former generated fall. Vertical descent stays in the pose, upper-body actions stop on death, and the legacy extra rotation is disabled.

The original allied model, textures, rifle and player arms remain. A two-hand rifle grip keeps the weapon attached across the different supplied wrist poses. The original rifle-fitting clip is loaded directly during rebuilds so it does not have to remain in the new controller.

All 60 mission allies (12 each in M1–M5) have the updated animation and death settings. M6 and Sandbox have no allied soldiers. The BoDoi prefab is also updated. Controller rebuilding reapplies the supplied clips, and character rebuilding retains the new settings.

Menu: **DBP > Allies > Apply supplied six animations**. Stop Play Mode and save open scene edits first. The operation preserves the user's open scene.

Verification includes all six clip references and valid rigs, grounded death settings, rifle attachment at three times per supplied clip, five-shot reload, blocked fire during reload, resuming fire, death during reload, all mission allies, and rendered poses. Evidence is in the ignored `Evidence/character-import` directory.

Final checks: 121 Editor tests passed, all 3 character playback tests passed, and all 5 campaign gameplay checks passed. The six poses were also reviewed during actual Play Mode in allied-live-poses.png.

