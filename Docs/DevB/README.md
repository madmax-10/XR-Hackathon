# Final demo B authoring

Source brief: `Docs/lastPastB.md` (the filename contains `Past`). Shared starting commit: `10961d2`, Unity 6000.3.27f1. B owns the integration scene and CrowdArt. A's runtime files are being developed in the same checkout; do not reset or stage them as B's work.

`FinalWorldAuthoring.cs` is reproducible scene/art authoring source kept outside Assets so it does not add a permanent editor dependency or overlap A's editor tooling. With this project closed in Unity:

```powershell
./Docs/DevB/Run-Authoring.ps1 -Action Build
./Docs/DevB/Run-Authoring.ps1 -Action Validate
./Docs/DevB/Run-Authoring.ps1 -Action RenderReview
./Docs/DevB/Run-Authoring.ps1 -Action BuildAndPackage
```

Build first runs the existing integration validator, preserves the 22 baseline poses and array order, replaces any remaining scene clones with A's prefabs, creates the scene and art, and explicitly assigns all available A components. It generates IDs through Unity scene serialization. Re-running Build reconstructs B's named generated objects; do not run it over intentional manual edits without reviewing those edits first.

BuildAndPackage also calls A's `IntegrationSetup.BuildWindows` after saving, validating and rendering the final scene. Android requires Unity Android Build Support and the corresponding SDK/NDK/JDK; run `IntegrationSetup.BuildAndroid` from the same finalized source revision once that module is available.

If A's runtime is absent, Build leaves named empty objects, with no fabricated script references. Once A's four components and NetworkOccupant prefab exist, run `-Action WireMerged` to populate the exact serialized contract and validate. The scene remains the authoritative deliverable; the source is for reproducibility and handoff.

Validation checks the existing reference/network/furniture contract, 44 anchors, 0.28 m clearance around the 43 floor anchors, art grounding/height/Animator contract, and a temporary clear-layout navigation bake with actual furniture and static box colliders. That geometry probe does not replace A's real multiplayer crowd proof. It restores the blocker and removes navigation data afterward.

The rig retains tracked camera transforms. The left controller enables smooth movement at 1.15 m/s; the right keeps teleport and snap turning. Physical comfort, tracking, floor targeting and controller behavior require headset acceptance.
