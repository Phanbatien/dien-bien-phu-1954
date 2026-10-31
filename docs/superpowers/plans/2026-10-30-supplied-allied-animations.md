# Supplied allied animations

Goal: Use all six newly supplied allied animations with the existing allied model, materials and weapons. Preserve cover, climbing, melee, navigation and the prior enemy integration.

- [x] Verify a failing check for all six connections, correct rig import and grounded death settings.
- [x] Import the six FBXs under Art/Animations/Allies/Supplied. Bake root movement into poses, loop Run Forward/running fire, preserve non-looping actions.
- [x] Update the existing allied controller in place: supplied run/death, two stationary firing variants plus moving fire, and reload.
- [x] Pause allied fire after five shots for the supplied reload duration. Apply authored death to prefab and all existing mission allies; preserve the open scene.
- [x] Check real playback, reload interruption/death, grounding, rifle attachment, all mission ally instances, existing action tests, and relevant broader suites.

Done: all six clips used, current model/materials/weapons retained, no firing during reload, grounded death without double rotation, successful tests and visual review. Work remains local; no commit or push requested.

