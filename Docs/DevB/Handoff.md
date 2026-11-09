# Final demo B handoff

Shared starting revision: `10961d2` on `new-new-main`. B working branch: `codex/final-demo-b`. Unity: **6000.3.27f1**. A and B ended up working concurrently in the same checkout; B did not change A's runtime, prefab assets, test code or editor tooling. Do not treat the initial shared revision as the final built revision.

## Delivered

- `Assets/Scenes/SafeShelter_Integration.unity`: west/east aisle openings at the specified coordinates; two new north segments; food pantry with table-top food cartons, signage and eight queue marks; northeast toilet room with two fixtures, privacy divider, west entry and four queue marks; empty bed parking bay, keep-clear ticks, and both viewing marks. All marks and signs are collider-free. Dense inherited template fog is disabled for desktop visibility.
- Exactly 22 baseline furniture items retain their names, poses and original `initialFurniture` order. Remaining scene clones were replaced with the corresponding A prefabs. Table-top food props are children of the supply tables and follow editing/reset.
- `CrowdDemo/Waypoints` has all **44** exact anchors in the brief. The final bindings serialize **24 active static BoxColliders** explicitly; inactive template floors/walls are excluded. A's runtime gathers moving furniture separately.
- `CrowdDemo` has A's director, bindings and one network identity. All scalar/array references and A/B visuals are assigned. Desktop controls and tracked viewer presence are wired to the director. Unity generated the scene network IDs on save.
- `HumanVisual_A.prefab` and `HumanVisual_B.prefab`: original stylized 1.80 m people with teal/ochre clothing, unit roots, feet at Y=0, forward +Z, root Animator, no root motion, `MoveSpeed` float, grounded idle and forward walk tuned to 1.15 m/s. No collider, agent, network behavior or camera. Provenance is beside the assets; every new asset has a `.meta` file.
- Left-controller smooth locomotion is configured at 1.15 m/s; right-controller teleport and snap turning remain available. Tracked camera transforms and the existing keep-clear setup are preserved. Both aiming ray masks include Floor.
- Reproducible authoring source is outside Assets under `Docs/DevB`; its wrapper temporarily imports the editor source and cleans up afterward. No permanent dependency on this authoring helper is shipped.

## Evidence collected on November 9, 2026

- Existing integration validator: **PASS**, 22 furniture, references and unique nonzero network identities.
- A's crowd integration validator: **PASS**, scene bindings, controls, network occupant prefab and Animator contract.
- B scene/art checks: **PASS**, 44 anchors, 0.28 m collider clearance around all 43 floor-level anchors, 24 active static obstacles, visual height/grounding.
- Independent real NavMesh bake with the bed parked: **PASS**, all 43 floor anchors sampled within 0.35 m near the floor; southern approach, gate crossing, pantry and toilet routes complete. This is geometry validation, not the multiplayer proof.
- Desktop renders reviewed: `Logs/FinalDemoB/overview.png`, `pantry.png`, `toilets.png`, `people-idle.png`, `people-walk.png`. Review caught and corrected inherited fog and floor-label overflow. Renders show the saved scene and the actual prefabs.
- `FinalWorldAuthoring.BuildAndPackage` called A's `IntegrationSetup.BuildWindows`: **PASS**, `Logs/FinalDemoB/BuildAndPackage.log`. Full Windows player is under `Builds/Integration/`.
- Baseline smoke against that package: **PASS**, `Logs/NetworkingSmoke-52cba3093c8640dd94c1e654eaf10c63`, port 18877.
- B's overlapping crowd smoke attempt: **FAILED / not acceptance**, `Logs/NetworkingSmoke-4f998f772ef943cab3255a41e28b0536`. Client timed out on live movement in the clear crossing; host subsequently timed out. This attempt overlapped additional players and A's rebuild. It must not be reported as a pass.
- A's earlier integrated crowd run passed both peers in `Logs/NetworkingSmoke-d8938f8b04994ae980fdbe147dd7de1f`. A owns the clean final serial rerun after its latest runtime refinements and camera viewport fix; use that later result for final acceptance.
- Live packaged UI opened and showed the hall, host-ready status, LAN addresses, furniture controls and crowd controls. Initial panels partially overlapped the hall; A is adding a responsive camera viewport. Final resize/input-isolation review remains open until that package is ready.

## Release blockers and coordination

No Android build was produced: this Unity installation only has Windows Standalone support; `Editor/Data/PlaybackEngines/AndroidPlayer` is absent. Android SDK ADB exists at `C:/Users/adhik/AppData/Local/Android/Sdk/platform-tools/adb.exe`, but `adb devices` returned an empty device list (`Logs/FinalDemoB/adb-devices.txt`).

Still required: install Unity 6000.3.27f1 Android Build Support including its SDK/NDK/JDK, build `IntegrationSetup.BuildAndroid` from the final combined revision, connect an authorized Quest, install the APK, join the Windows host on the same LAN, and complete every headset/replay check in `Operator.md`. In particular, physical tracking, comfort, controller floor targeting, teleport, turning, human scale, visible queue animation, personal-space yielding, red/green sync and clean replay have **not** been verified on a headset. No headset model or headset performance result is claimed.

A and B have explicit user authorization to coordinate. A owns the final rebuild and serial smoke runs after the viewport/runtime refinements. B will commit only the integration scene, CrowdArt and `Docs/DevB`; A should commit its scopes afterward. Obtain the B scene/art commit with `git log -1 --format=%H -- Assets/Scenes/SafeShelter_Integration.unity Assets/SafeShelter/CrowdArt`. Build both platforms from the resulting combined revision; preserve the exact revision and both-peer smoke logs beside the player. The current working-tree package is not a final matched Windows/Quest release.

Operator instructions: `Docs/DevB/Operator.md`. Authoring/revalidation instructions: `Docs/DevB/README.md`.
