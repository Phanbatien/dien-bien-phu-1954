# Supplied enemy characters

Goal: Replace mission enemy and officer visuals with the user's matching supplied models and five animations. Preserve current allies, mission logic, combat settings, cover, climbing and command actions.

- [x] Inspect assets and verify humanoid imports. Source-only FBXs are unrigged; use rigged firing FBXs for visuals.
- [x] Add checks that fail until both usable textured character prefabs and animation connections exist.
- [x] Build reusable visual prefabs/controllers; preserve legacy actions and disable the legacy extra fall only for the new death clips.
- [x] Replace visuals in all seven gameplay scenes without disturbing the open MainMenu.
- [x] Verify both prefabs, every mission, animation playback and rendering in Unity; run relevant existing tests.

Done: correct enemy/officer selection, valid rigs and textures, all supplied clips connected, attached rifles, grounded scale, retained combat/navigation, natural death playback, passing checks and visual review. Reload is an available controller action; no new ammunition rules are introduced.

