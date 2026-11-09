# Crowd visual and wayfinding provenance

HumanVisual_A and HumanVisual_B, their clothing/material palettes, idle/walk animation curves, toilet fixtures, pantry food cartons and wayfinding geometry were authored procedurally for this project. No downloaded character, animation, texture, or third-party art pack is included.

Geometry uses Unity's built-in cube mesh. Each character is approximately 1.80 m tall with feet at local Y=0, unit root scale and face toward +Z. Teal shirt/stripe and ochre vest variants share an Animator controller and an idle/walk blend driven by float `MoveSpeed` in meters per second. Root motion is disabled. The in-place walk has a 0.92 s cycle with a 0.529 m stance over 0.46 s, tuned for 1.15 m/s. Idle leg poses are grounded; walk swing lifts each foot. Animation is authored on transforms, so no external Humanoid Avatar or retargeting asset is needed.

The prefabs have no colliders, network behaviors, navigation agents, cameras or runtime scripts. All materials use the project's existing Universal Render Pipeline Lit shader. Meshes and materials are shared; the visuals use simple rigid pieces without skinning or texture maps.

Signs use the existing TextMesh Pro LiberationSans SDF asset already in the repository. Its existing license is retained under `Assets/TextMesh Pro/Fonts/`. No new font is redistributed. Scene furniture remains sourced from Dev A's existing network prefabs.

Reproducible authoring source and validation: `Docs/DevB/FinalWorldAuthoring.cs`, invoked through `Docs/DevB/Run-Authoring.ps1`. Physical headset performance and animation appearance remain part of Quest acceptance; desktop renders are not a headset performance measurement.
