# Final crowd runtime and Windows planner — Dev A

Implements [lastPartA.md](lastPartA.md). Unity version: **6000.3.27f1**. Dev B owns the integration scene and humanoid art; A owns the runtime, occupant prefab, proof scene, validators, and smoke runner. The two chats used one working tree; there is no additional scene merge to perform in this checkout.

## Implemented

- `CrowdSceneBindings`, `CrowdDirector`, `CrowdDesktopControls`, `CrowdViewerPresence`, and `NetworkOccupant` use the exact shared Inspector contract. B's scene is wired to those types.
- Host builds and disposes runtime navigation from the hall, explicit static boxes, and present furniture. Mandatory floor anchors are sampled within 0.35 m with height checks. A route around the aisle partitions does not qualify as an open crossing.
- Normal activity uses 6 residents and 4-second departures; Meal rush uses 12 and 0.6-second departures. Deterministic walking speeds, 90-second windows, pantry/toilet dwell, return trips, and rest periods match the brief.
- Blocked transit slots are reserved on arrival at the aisle entrance. Pantry and toilet lines use independent FIFO reservations. Visits count only at the service point; trips count only on return home.
- Mean aisle wait is stopped time at the aisle divided by released residents. It includes unfinished waiting at cutoff. The clear route also counts stops below 0.15 m/s within the crossing and its approaches; walking and service dwell are excluded. These are simulation measurements, not a calibrated capacity forecast.
- Occupant transforms, appearance, movement speed, and live results replicate from the host. Clients do not run navigation. Late join resolves the live scene director and local humanoid visuals.
- Tracked Quest head positions arrive over rate-limited unreliable RPCs. Host rejects nonfinite/out-of-hall X/Z and disconnected senders. Nearby residents stop within 0.8 m and resume when space clears; the proxy expires after 0.5 seconds. No camera motion is forced.
- Host-only preset/run controls, live metrics, and completed results per profile/preset are in the desktop panel. Manual layouts and interrupted runs cannot replace preset results. Layout edits and resets clean up running or completed resident populations. Restart begins with fresh navigation and counters.
- Furniture selection highlighting and panel input exclusion preserve the planner controls. The camera reserves the two control columns and fits the hall in the remaining viewport, with relative zoom preserved on resize.
- Windows builds open at 1366 × 768 in a resizable window, show LAN adapters/port/session state, and retain the existing automatic host and session-reload behavior.

## Build and test

Close Unity and existing player processes before replacing a package. Run only one Unity process for this working tree.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.27f1\Editor\Unity.exe' `
  -batchmode -nographics -quit -projectPath "$PWD" `
  -executeMethod SafeShelter.Editor.IntegrationSetup.BuildWindows `
  -logFile "$PWD/Logs/CrowdIntegration-ReleaseBuild.log"

./Tools/Run-NetworkSmokeTest.ps1 -Executable Builds/Integration/SafeShelter.exe -Port 17957
./Tools/Run-NetworkSmokeTest.ps1 -Executable Builds/Integration/SafeShelter.exe -Port 17967 -Crowd
```

The crowd probe runs two full 90-second simulations, checks late join, visible replicated movement, both service destinations, client authority rejection, cleanup, Normal restart, and viewer-proxy yielding/expiry. The default probe retains furniture, zones, disconnect and reload checks. Results are written under `Logs/NetworkingSmoke-<id>` so Unity's temporary-directory cleanup cannot erase them.

For the isolated fixture, `SafeShelter.Editor.CrowdSetup.PrepareAndBuildProof` creates the crowd wiring in `Assets/SafeShelter/Scenes/DesktopNetworkTest.unity`, registers the occupant prefab, and calls the existing desktop test builder. Supply `--safeshelter-build` to choose a separate output directory. The fixture deliberately uses a simple capsule visual; the final integration scene uses B's two animated humanoids.

## Evidence and acceptance

Final revision/build and clean test evidence will be recorded below after validation. Earlier integrated two-player evidence: `Logs/NetworkingSmoke-d8938f8b04994ae980fdbe147dd7de1f` passed with blocked 0 round trips / 81.86 seconds mean wait, and clear 15 round trips / 12 pantry visits / 6 toilet visits. This predates final queue, wait, viewport and viewer-test refinements and is not the final release record.

Baseline furniture and simulated XR red/green proof passed in `DesktopNetworkTest`: `Logs/NetworkingSmoke-d62b1ecc5b8d48a1a6d63b62aa68e618`.

A concurrent B crowd test (`Logs/NetworkingSmoke-4f998f772ef943cab3255a41e28b0536`) timed out on client movement while builds/other players overlapped. The clean serial final run is authoritative; the movement probe now observes all residents instead of one arbitrary waiting resident.

Computer Use denied A access to the packaged application. B is reviewing the desktop view and input behavior separately. Do not infer manual UI acceptance from headless tests.

Quest release acceptance remains pending: the installed Unity editor has no Android Build Support module and ADB reports no attached headset. Build the APK from the same combined source revision once the toolchain is available, then follow [B's operator checklist](DevB/Operator.md) for LAN join, scale, comfort, tracking, personal-space behavior and clean replay. Desktop network simulation does not establish headset readiness.

Ship the **entire `Builds/Integration/` Windows player directory**, including the executable, data directory and runtime libraries. A lone executable is insufficient. Build outputs and logs are ignored by Git; retain/share them separately.
