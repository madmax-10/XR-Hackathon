# SafeShelter final demo · Dev B

Own the final integration scene, shelter wayfinding, humanoid visuals, and Quest walkthrough. This brief owns **world geometry, object names, and Inspector assignments**. [Dev A's brief](lastPartA.md) owns **runtime APIs, networking, and traffic behavior**. The two briefs extend the [locked scene spec](Parallel_LockedSceneSpec.md) for this phase. The aisle openings and the new demo parking position below are intentional changes.

## 1. Start from a common base

- Use Unity **6000.3.27f1**. The existing integration base is `new-new-main` at `10961d2`, which combined `dev-A` at `ba3d899` and `agentB` at `c879978`. Review newer commits and give both developers the same agreed base before branching for the final phase.
- Verify that the merged scene uses Dev A's prefabs for the supply tables, crates, medical table, and chairs. Keep all locked names and poses and the `FurnitureLayout.initialFurniture` references in the original order. Verify Quest keep-clear wiring. Run **Tools → SafeShelter → Validate Integration Scene** before expanding the world.
- Dev B alone edits `Assets/Scenes/SafeShelter_Integration.unity` and owns `Assets/SafeShelter/CrowdArt/`. Dev A owns scripts, network prefab assets, editor tooling, and tests. Save all new art assets with their Unity `.meta` files. Keep unrelated Unity import and project-setting changes out of both branches.
- Prepare the world and art on your branch while A builds against a proof fixture. Use the named empty objects below while A's scripts are unavailable. Once both branches are merged, Dev B opens the merged scene in Unity and performs the final Inspector wiring. Let Unity generate scene network IDs on save.

## 2. Build the final shelter floor plan

All positions below are **world coordinates in meters**. Preserve the 24 × 16 m hall, floor top at Y = 0, Floor layer 8, north exit, sleeping beds, and medical annex. Keep exactly 22 baseline networked furniture items. New partitions, fixtures, and signs are static environment objects.

### Aisle connections and comparison layout

- Keep the center aisle between the existing east and west partitions at X = ±1.25. Each partition is 0.2 m thick and 2 m high. Their original Z span is −6 to +5.
- On the west side, leave a **2.4 m opening** from Z = 1.45 to 3.85. Keep `Partition_AisleWest` for the lower segment (center Z = −2.275, length 7.45). Name the upper segment `Partition_AisleWest_North` (center Z = 4.425, length 1.15).
- On the east side, leave a **2.4 m opening** from Z = 2.40 to 4.80. Keep `Partition_AisleEast` for the lower segment (center Z = −1.8, length 8.4). Name the upper segment `Partition_AisleEast_North` (center Z = 4.9, length 0.2).
- Keep `Bed_01_Blocker` at **(0, 0.225, 0)** with yaw 0 for the blocked layout. Mark a visible, collider-free **BED PARKING** outline centered at **(4, 0, −2)** for the clear layout. Keep that bay empty. Its target marker is `ClearBedPose` at **(4, 0.225, −2)** with yaw 0.
- Keep the demo keep-clear corners at **(−1.0, 0, −0.65)** and **(1.0, 0, 0.65)**. Small floor ticks can identify the corners from Quest. Use decals or collider-free geometry so marks still hit `HallFloor`.

### Food pantry

- Convert the existing **west supply annex** (approximately X = −12 to −8, Z = −3 to +3) into a clearly recognizable **FOOD PANTRY**. Keep the two supply tables and four crates in their locked positions. Add simple food props on the tables or shelves, a counter sign, and floor markings for a single-file line.
- Keep the approach from the hall open. Place `PantryService` at **(−9.05, 0, 0.9)** on the customer side. Keep the line at Z = 0.9. The first waiting point is **(−8.25, 0, 0.9)**. Subsequent points step **+0.8 m on X** toward the hall. Give the service point and queue floor space clear of decoration colliders.

### Toilets

- Add a dedicated room northeast of the medical annex, approximately **X = 9.15 to 11.75, Z = 4.55 to 7.55**. Keep the medical annex in place. Add two recognizable toilet stalls, a privacy divider, a **TOILETS** sign visible from the aisle, and an exterior queue line.
- Put the entrance on the **west face**, open from **Z = 6.15 to 7.55**. Use a clear threshold at `ToiletEntry` **(9.15, 0, 6.85)**. Place `ToiletService` at **(10.35, 0, 6.85)** in the stall approach. Keep the walkable entry and service point clear of stall and fixture colliders.
- Keep the line on Z = 6.85. The first waiting point is **(8.35, 0, 6.85)**. Subsequent points step **−0.8 m on X** toward the hall. Keep the line clear of Bed_11 and the north exit.
- Give walls, partitions, and floor-standing fixtures simple `BoxCollider` components. Preserve the existing floor collider as the sole navigation and teleport floor. Keep signs, lane paint, and small decorative objects collider-free.

## 3. Place and name the shared scene anchors

Create `CrowdDemo` as a scene root at identity. Place the navigation anchors below under `CrowdDemo/Waypoints`, with unit scale. Station queue arrays are always ordered **front to back**. Use zero-padded numbers and populate serialized arrays explicitly.

| Object | World position or placement |
| --- | --- |
| `AisleGateSouth` | (0, 0, −1.15) |
| `AisleGateNorth` | (0, 0, 1.15) |
| `PantryPortal` | (−1.25, 0, 2.65) |
| `PantryService` | (−9.05, 0, 0.9) |
| `ToiletPortal` | (1.25, 0, 3.6) |
| `ToiletEntry` | (9.15, 0, 6.85) |
| `ToiletService` | (10.35, 0, 6.85) |
| `ClearBedPose` | (4, 0.225, −2), yaw 0 |
| `PantryQueue_01…08` | (−8.25 + 0.8 × i, 0, 0.9), i = 0…7 |
| `ToiletQueue_01…04` | (8.35 − 0.8 × i, 0, 6.85), i = 0…3 |
| `TransitQueue_01…12` | Pairs at X = −0.6, +0.6 from front to back. Row Z = −1.7 − 0.88 × row, row = 0…5. Within each row west comes first. |

Create `ResidentStart_01…12` under the same parent. The order below is the authoritative array order. Starts 01–06 cover both sides of the sleeping area in Normal activity. Rush adds 07–12.

| Start | (X, Z) | Start | (X, Z) |
| --- | --- | --- | --- |
| 01 | (−2.2, −7.15) | 07 | (−4.6, −7.15) |
| 02 | (2.2, −7.15) | 08 | (4.6, −7.15) |
| 03 | (−3.4, −7.15) | 09 | (−2.2, −6.15) |
| 04 | (3.4, −7.15) | 10 | (2.2, −6.15) |
| 05 | (−3.4, −6.15) | 11 | (−4.6, −6.15) |
| 06 | (3.4, −6.15) | 12 | (4.6, −6.15) |

All start Y values are 0. Test that the southern approach, each side opening, and each service line can be reached at floor level. In Play mode, confirm the approach markers stay clear of the 22 furniture colliders. Fix any geometry collision in this scene before handoff. Coordinate any anchor contract change with A and update both briefs in the same handoff.

## 4. Make people and the walkthrough believable in Quest

- Deliver `Assets/SafeShelter/CrowdArt/HumanVisual_A.prefab` and `HumanVisual_B.prefab`. Use humanoid silhouettes with clothing variation and distinguishable colors. Simple stylized people are fine. Use assets you have permission to include and record their provenance alongside the assets if sourced externally.
- Each visual root has unit scale, feet on local Y = 0, forward +Z, and a root `Animator`. Aim for **1.65–1.85 m** standing height. Animator root motion is off. Both controllers expose the float `MoveSpeed` in meters per second and blend a grounded idle into a forward walk. Tune the walk near 1.15 m/s and verify idle feet stay planted in a queue.
- Visuals are local children of A's network occupant prefab at runtime. Leave visual prefabs free of colliders, navigation agents, network components, and camera scripts. Use Quest-friendly meshes and materials that stay legible with 12 avatars visible at once.
- Preserve tracked camera height and Quest join, teleport, and keep-clear controls. Use the existing XRI rig to provide optional comfortable continuous locomotion for moving through the aisle. Keep teleport available. Verify joystick locomotion, turning, and floor targeting still work with the final scene's walls and fixtures.
- Place two clearly labeled, collider-free viewing marks for the guided comparison. Use **SOUTH AISLE VIEW** around **(0, 0, −2.5)** and **NORTH CROSSING VIEW** around **(0, 0, 3.0)**. These are visual guides. Existing teleport uses the floor and must still let the reviewer approach the views and walk past people at normal head height.

## 5. Wire the merged scene

Dev A defines the fields in [lastPartA.md §2](lastPartA.md#2-locked-component-and-inspector-contract). Assign scene references in Unity after merging A's runtime work. Use the same names and types.

- On `CrowdDemo`, add one `NetworkObject`, one `SafeShelter.CrowdSceneBindings`, and one `SafeShelter.CrowdDirector`. Assign the director's `bindings` to the component on the same object and `occupantPrefab` to A's `NetworkOccupant.prefab`. Bind the hall floor, existing layout, blocker, all named anchors and ordered arrays, both visuals in A/B order, and the explicit static obstacle array. The static array includes hall walls, all aisle and annex partitions, and blocking toilet fixtures. The runtime gathers moving furniture separately.
- On `DesktopRig`, add `SafeShelter.CrowdDesktopControls` and assign `director`. Check that its controls are visible alongside the planner and do not capture clicks meant for furniture or orbit controls.
- Dev A owns the [Windows desktop app](lastPartA.md#4-windows-desktop-app). Verify `DesktopOrbitCamera.focus` still points to `HallCenter` and the initial camera frames the full layout. Check that the pantry, toilets, exit, and bed parking labels are readable from the desktop overview as well as at Quest eye level.
- Under the existing XR Origin, create `CrowdViewerPresence`, attach `SafeShelter.CrowdViewerPresence`, and assign `director` and the tracked XR camera transform as `viewHead`.
- Recheck `QuestKeepClearMarker` on XR Origin. Assign the same scene ConstraintManager, two active aiming rays whose raycast masks include Floor, and Floor-only `floorMask`. Confirm the previous red/green presentation still works. Recheck the scene's single session, transport, and network manager.

## 6. Deliver the scene and run the demo

1. Report the shared base, final B commit, art provenance, and any wiring differences. Run **Tools → SafeShelter → Validate Integration Scene** and A's desktop proof on the merged scene. Resolve missing references, IDs, and blocked spawn or service anchors before builds.
2. Build both players from the same merged commit via `SafeShelter.Editor.IntegrationSetup.BuildWindows` and `BuildAndroid`. Keep the Windows executable, `SafeShelter_Data/`, and accompanying runtime files together at `Builds/Integration/`. Keep the Quest APK at `Builds/Integration/SafeShelter.apk`. Run the merged Windows smoke described in A's brief and record the exact commit and result. Rebuild both players after any scene or runtime fix.
3. Launch the standalone Windows planner at `Builds/Integration/SafeShelter.exe`. Confirm it shows the live hall, host status, and LAN address. Connect the Quest and Windows machine to the same reachable LAN. On first setup, use a Quest with developer mode and USB debugging authorized. Confirm it appears in `adb devices` and install with `adb install -r Builds/Integration/SafeShelter.apk`. Open SafeShelter on Quest. Enter the IPv4 address shown by the planner and port **7777** in the Quest session panel. Verify the Windows planner shows the connection. Check tracking, teleport, optional smooth movement, and controller selection before inviting the reviewer.
4. On desktop select **Clear layout** and **Normal activity**. Start a run. In Quest, walk from the south view toward the crossing. See people at human scale passing, turning, waiting at the pantry or toilet line, and coming back.
5. Select **Blocked layout** and **Meal rush**. Restart the run. Watch the aisle queue build. Mark the demo floor corners on Quest from positions that give both rays a clear floor hit. Verify red on both devices. Move out of traffic and let the 90-second run finish. Use **Clear layout** to move the blocker to the marked parking bay on desktop and verify green. A manual drag to the same bay can demonstrate the same red-to-green interaction.
6. Replay **Meal rush** with the same start schedule and measurement window. Observe from just north of the central aisle, around **(0, 0, 5.65)**, for both measured passes. See people walk through the crossing and reach the services. In the Windows app, show the completed **Blocked** and **Clear** results together and compare completed trips, peak queue, and mean aisle wait. Use the marked views and step into the lane in a separate walkthrough to experience the crowd and check that nearby people yield.
7. Repeat the entire sequence after a clean launch. Verify the **Reset Layout** restores the blocker and all 22 props, and the next run starts from zero. Record headset model, build commit, smooth movement and teleport results, visible queue behavior, and any failure. Leave the combined build, smoke logs, and short operator instructions together. If a Quest is unavailable, record the exact headset checks still open.
