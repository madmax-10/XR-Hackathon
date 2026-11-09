# SafeShelter final demo · Dev A

Build the shared occupant simulation for a Windows planner and a Quest reviewer walking through the shelter at eye level. Residents travel between beds, the food pantry, and the toilets. They pass, yield, wait in lines, and return to the sleeping area. The demo compares the same meal rush with the aisle blocked and clear. [Dev B's brief](lastPastB.md) owns **world geometry, anchors, and art**. This brief owns **runtime APIs, networking, and traffic behavior**.

## 1. Ownership and shared base

- Use Unity **6000.3.27f1**. The existing integration base is `new-new-main` at `10961d2`, which combined `dev-A` at `ba3d899` and `agentB` at `c879978`. Review newer commits and agree on one base for both final-phase branches. Dev B verifies the baseline scene wiring and prefab swaps on that base. Keep the [locked scene spec](Parallel_LockedSceneSpec.md) for the original network, furniture, and zone contracts. The final demo geometry and parking position are in [Dev B §2](lastPastB.md#2-build-the-final-shelter-floor-plan).
- Own `Assets/SafeShelter/Scripts/`, `Prefabs/`, `Editor/`, `Tests/`, and any required changes to `Tools/`. Use `Assets/SafeShelter/Scenes/DesktopNetworkTest.unity` for any persisted proof wiring. Dev B owns `Assets/Scenes/SafeShelter_Integration.unity` and `Assets/SafeShelter/CrowdArt/`. Dev B does the final Inspector wiring after merge.
- Build against a small test fixture with the same field names and actor interface. Use a temporary test visual until B's humanoids are available. Keep the existing scene usable when it has no crowd objects. Use the project's existing Unity navigation module, Netcode, URP, and XRI settings.

## 2. Locked component and Inspector contract

Use namespace `SafeShelter`. Each type is in a file with the same name under `Assets/SafeShelter/Scripts/`. The exact anchor transforms and array order are in [Dev B §3](lastPastB.md#3-place-and-name-the-shared-scene-anchors). Serialize the fields below with these exact names and types.

### `CrowdSceneBindings` on the `CrowdDemo` root

| Serialized field | Type | Dev B assignment |
| --- | --- | --- |
| `hallFloor` | `BoxCollider` | Existing `HallFloor` |
| `layout` | `FurnitureLayout` | Existing DesktopRig layout |
| `blocker` | `SharedFurniture` | `Bed_01_Blocker` |
| `clearBedPose` | `Transform` | `ClearBedPose` |
| `aisleGateSouth`, `aisleGateNorth` | `Transform` each | Matching `AisleGateSouth` and `AisleGateNorth` |
| `pantryPortal`, `pantryService` | `Transform` each | Matching `PantryPortal` and `PantryService` |
| `toiletPortal`, `toiletEntry`, `toiletService` | `Transform` each | Matching `ToiletPortal`, `ToiletEntry`, and `ToiletService` |
| `residentStarts` | `Transform[]` | `ResidentStart_01…12`, ascending |
| `transitQueue` | `Transform[]` | `TransitQueue_01…12`, front to back |
| `pantryQueue` | `Transform[]` | `PantryQueue_01…08`, front to back |
| `toiletQueue` | `Transform[]` | `ToiletQueue_01…04`, front to back |
| `staticObstacles` | `BoxCollider[]` | Hall walls, partitions, and blocking fixtures. Gather movable furniture separately. |
| `occupantVisuals` | `GameObject[]` | Two prefabs in order `HumanVisual_A`, `HumanVisual_B` |

### Runtime and controls

| Component | Where | Serialized fields and assignment |
| --- | --- | --- |
| `CrowdDirector : NetworkBehaviour` | `CrowdDemo` with one `NetworkObject` | `bindings : CrowdSceneBindings` on the same object. `occupantPrefab : GameObject` set to `NetworkOccupant.prefab`. |
| `CrowdDesktopControls : MonoBehaviour` | Existing `DesktopRig` | `director : CrowdDirector` on `CrowdDemo`. |
| `CrowdViewerPresence : MonoBehaviour` | `CrowdViewerPresence` under the existing XR Origin | `director : CrowdDirector` on `CrowdDemo`. `viewHead : Transform` set to the tracked XR camera. |
| `NetworkOccupant : NetworkBehaviour` | `Assets/SafeShelter/Prefabs/NetworkOccupant.prefab` | A owns all root components. Resolve the two local visuals through the assigned scene bindings. |

Use the existing `SafeShelterNetworkPrefabs.asset` to register `NetworkOccupant`. Keep `FurnitureLayout.bedPrefab` pointed at `NetworkBed` and keep occupants out of `initialFurniture`. `NetworkOccupant` has one root `NetworkObject`, an interpolated server-authoritative `NetworkTransform`, and a `NavMeshAgent` enabled only on the host after the navigation surface is available. It has no furniture or player ownership. Use NGO scene synchronization and a persistent scene director reference for resolving visuals on join. Replicate each occupant's look index and movement state. Instantiate its assigned visual once per peer after bindings are ready. Its Animator uses the float `MoveSpeed` in m/s, with root motion disabled. Settle at grounded idle in queues. Suppress animation spikes on spawn and reset.

### Host control surface

Expose these entry points on `CrowdDirector` and use them from both controls and tests.

```csharp
public enum CrowdProfile { Normal, MealRush }

// On CrowdDirector
public void StartRun(CrowdProfile profile);
public void StopRun();
public void ApplyBlockedLayout();
public void ApplyClearLayout();
```

Keep all four methods host-only and safe before session spawn. Desktop controls show **Normal activity**, **Meal rush**, **Start / Restart run**, **Stop**, **Blocked layout**, and **Clear layout**. Place controls beside the planner without intercepting its placement or orbit actions. Integrate UI hit testing with existing desktop controls.

- **Blocked layout** stops the crowd and returns the blocker to its baseline pose. **Clear layout** stops the crowd and moves the blocker to `clearBedPose` using the existing furniture API. The two controls change the blocker only. Keep ordinary drag, rotate, add, remove, and Reset Layout. Use Reset Layout first if the blocker was removed or other furniture edits should be cleared.
- Stop a measured run before a furniture edit takes effect, or end it visibly with a **Layout changed · restart run** status when the edit is detected. Preserve results only for completed comparison runs. Run navigation from the current snapshot at the next start, including when the planner moves the bed manually. Reset Layout removes added beds and restores all 22 baseline props. It also stops any active crowd run. Keep the existing keep-clear zone and its clear action independent.
- Display selected profile, active resident count, elapsed time, service visits by destination, completed round trips, current and peak aisle queue, and mean aisle wait. Distinguish an incomplete run from a finished one. Preserve the last completed result for each layout preset and profile in the running session. Label manually edited layouts separately if their geometry differs from both presets. Never count a waiting or unreachable bot as having completed a trip.

## 3. Navigation and repeatable traffic

### Build navigation from the current layout

- On the host at run start, sync transforms and build a runtime navigation surface from the explicit `hallFloor`, `staticObstacles`, and active colliders on present furniture from the existing layout, including added beds. Exclude preview objects, people, signage, the live viewer proxy, and removed furniture. Make the hall floor walkable and mark obstacle geometry non-walkable. Use box center, orientation, and scaled dimensions for the sources. Keep the build bounded to the hall. Dispose of old navigation data after stopping and despawning the previous run. [Unity runtime navigation API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshBuilder.BuildNavMeshData.html)
- Start with about **0.28 m** agent radius, **1.75 m** height, and **0.15 m** maximum climb for a level hall. Use the same agent type for the bake and the agents. Apply these values in the runtime build settings. Sample every mandatory anchor within **0.35 m** and verify its projected height remains near the floor. Fail the run with a named diagnostic if an anchor is invalid. A blocked crossing is a valid result.
- Residents taking the demo route use the **south gate → north gate** crossing before branching to the pantry or toilets. Confirm an actual complete path through the section between the two gates. A path routed around a partition via the rest of the hall does not count as this crossing. Use the aisle corridor (inner X bounds about ±1.15 and Z between the gates with a small sampling tolerance) to validate the route segment. If it is blocked, residents approach and wait in the transit queue. Never push them through a collider or teleport them past it.
- Continue via `PantryPortal` to pantry queue/service, or via `ToiletPortal` and `ToiletEntry` to the toilet queue/service. Return through the same side opening and crossing to the assigned start. Give both directions a way to yield and pass when clear. Keep waiting slots south of the blocked section and service lines outside their access points. Reserve queue slots by arrival order and release them on service, return, stop, or disconnect. Feed both destinations without allowing a blocked line to permanently stall the other.

### Reuse the same traffic schedule before and after

| Profile | Starts | First departure | Trip destination |
| --- | --- | --- | --- |
| Normal activity | Residents 01–06 | One resident every 4 seconds, starting at 0 | Every third resident goes to toilets. The others go to the pantry. |
| Meal rush | Residents 01–12 | One resident every 0.6 seconds, starting at 0 | Same destination rule. |

- Use a **90-second measurement window**, the same ordered start list, and deterministic variation by resident index. Walk around **1.0–1.3 m/s** with acceleration and turn smoothing. At a service point dwell **2 seconds** for pantry and **3 seconds** for toilets. After returning to the start, pause **3 seconds** and repeat. Count the outbound visit once on arrival at service and the round trip once on return. Mean aisle wait is total time spent waiting at the aisle divided by the number of released residents, including seconds accumulated by residents still waiting at cutoff. At the end of the window retain the result and let the operator restart or stop.
- The host owns all paths, queue slots, schedule, and metrics. Quest interpolates the same people rather than simulating another crowd. Late join shows the active residents in their current places and current run state. Stop, reset, scene unload, and reload clean up occupants, navigation, and transient state. Repeated restarts start from the same seed and zero counters. Reject stale packets or callbacks from an earlier run.

### Make the Quest wearer part of the walkthrough

- While a Quest client is connected and tracking, send its projected head X/Z to the host about **8 times per second**. Use an unreliable, rate-limited update. Accept only finite coordinates inside the hall from the connected viewer. Expire the proxy within **0.5 seconds** of missing updates and clear it on disconnect. Treat an inspector teleport as a new proxy position.
- The host uses the proxy only for nearby avoidance and yielding. A small non-carving obstacle can influence local avoidance without changing the permanent path. For a person directly ahead inside arm's reach, slow or stop the approaching occupant until there is space, then let it resume. Preserve camera tracking and movement under the wearer's control. Never move the XR camera in response to a bot. [Unity obstacle behavior](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AI.NavMeshObstacle.html)
- For the measured before/after pass, the wearer observes from the same marks and stays off the main lane. Run a separate pass through the lane to demonstrate personal space. The comparison describes this simulated demand and geometry. Record simulated counts and wait times as such.

## 4. Windows desktop app

Dev A owns the standalone **SafeShelter Windows planner**. Extend the existing Unity player, DesktopRig, and session controls. The Windows app is the LAN host and the operator's control surface. It opens the same integration scene and shows the same people, furniture, and zone state as Quest.

- **Session.** Open in a resizable window with the hall visible. Auto-start the host on UDP **7777** using the existing session behavior. Show host status, the LAN IPv4/port the Quest user should enter, and whether a Quest client is connected. If multiple network adapters are active, show the available LAN addresses with their adapter names. For a host failure, show the reason and keep the existing session reload path available.
- **3D planner.** Fit the hall in the initial overhead view and keep the pantry, toilets, exit, and aisle easy to identify. Left-click to select furniture, left-drag to move, **R** to rotate, right-drag to orbit, and mouse wheel to zoom. Keep **Add Bed**, **Remove Selected**, **Reset Layout**, and keep-clear mark and clear. Highlight the selected prop and show the zone's blocked or clear state in text alongside its color.
- **Crowd controls.** Keep **Normal activity**, **Meal rush**, **Blocked layout**, **Clear layout**, **Start / Restart run**, and **Stop** visible and grouped. Show the active profile, layout, run state, and 90-second progress. Keep completed results visible when switching layouts so the operator can compare them. Disable or explain unavailable actions before the host is ready. If furniture moves during a run, show the layout-change status and explain how to restart.
- **Traffic comparison.** Display live active residents, completed round trips, pantry and toilet visits, current and peak aisle queue, and mean aisle wait in seconds. Put the completed **Blocked** and **Clear** results for the selected profile side by side or in stacked rows. Mark an unrun result as pending. Keep manually edited and interrupted runs out of the preset comparison.
- **Input and layout.** Keep session and furniture controls on one side and crowd controls on the other, with a clear view of the shelter in between. Verify at **1366 × 768** and after resizing. Panel clicks, dragging, and scrolling must stay in the panel. They must not move furniture, place a zone corner, or orbit the camera. Keep the layout usable with only a mouse and keyboard.
- **Packaging.** Build the merged integration scene using `SafeShelter.Editor.IntegrationSetup.BuildWindows`. Hand off the full `Builds/Integration/` Windows player directory together, including `SafeShelter.exe`, `SafeShelter_Data/`, and the accompanying runtime files. The operator can launch the executable on Windows without opening Unity. Ship it with the Quest APK from the same commit and the [Dev B run instructions](lastPastB.md#6-deliver-the-scene-and-run-the-demo).

## 5. Validation and release evidence

1. Extend **Tools → SafeShelter → Validate Integration Scene** for the final merge. Require one scene director, bindings, and network identity for final integration builds. Check the two control components are on their correct rigs, the occupant prefab is registered with a unique nonzero network hash, anchor and array references match the shared contract, the static obstacles have colliders, and both visual prefabs meet the Animator contract. Preserve the existing 22-furniture, floor, and zone validations. The desktop proof scene uses its existing separate validator and can still run without the crowd.
2. Add an opt-in proof path to the existing smoke runner, exposed as **`-Crowd`**, for the merged integration build. Preserve the default furniture and zone smoke. Test the same Meal rush schedule with the blocker at its original pose and at the clear bay. Assert blocked gives a visible, nonzero transit queue, clear produces completed trips including service at both destinations, and the measured crossing wait improves. Confirm the client sees residents move, observes queueing, and catches up when joining a live run. Exercise stop, restart, Reset Layout, cleanup, and a second run. Use real navigation and collect logs on both peers. Set timeouts to cover the two 90-second runs and connection startup.
3. Build Windows from the merged commit and run `Tools/Run-NetworkSmokeTest.ps1 -Executable Builds/Integration/SafeShelter.exe` both with and without `-Crowd`. Confirm the earlier simulated XR red/green and clear proof still passes on `DesktopNetworkTest`. Record the executable, merged commit, logs, profile, and comparison numbers. If the blocked and clear passes look alike, fix the physical route or queue flow before claiming the traffic demo is ready.
4. Launch the packaged Windows app outside the Editor. Verify host status, LAN join information, furniture editing, panel input isolation, both crowd profiles, and comparison results at the target window sizes. Send B the exact merged commit, Windows package, validator and smoke results, control labels, and any deviations. Follow the headset acceptance steps in [Dev B §6](lastPastB.md#6-deliver-the-scene-and-run-the-demo). Final readiness requires a fresh Windows host and Quest APK from the same merge, a LAN join, a comfortable walk among life-size moving people, visible queueing, and a clean replay of the blocked and clear comparison.
