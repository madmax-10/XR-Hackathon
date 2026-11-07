# Handoff notes for Dev B

Dev A adds one section here for each piece of work that Dev B needs to wire into
`Assets/Scenes/SafeShelter.unity` or call from Quest code. Newest section goes at the top.
Each section says what was built, what to wire, what to call, and one test to run.

---

## 2. Keep-clear zones (ConstraintManager)

**Status:** Compiles. Not committed yet. Tested only by compiling; run the test below first.

**What it is:** One shared keep-clear rectangle on the floor. The Windows host decides if it is
valid and whether any furniture overlaps it, then both devices get the same result.
Red means a bed overlaps; green means clear.

**New files**
- `Assets/SafeShelter/Scripts/ConstraintManager.cs` — host-owned zone logic and sync.
- `Assets/SafeShelter/Scripts/ZoneState.cs` — the shared snapshot your renderer reads.
- `Assets/SafeShelter/Scripts/ZoneDebugView.cs` — simple red/green slab for the desktop test.
  Optional; replace with your own zone renderer.

**Wire into `SafeShelter.unity`**
1. Create a GameObject `Constraints`. Add `NetworkObject` and `ConstraintManager`.
   Keep exactly one in the scene.
2. On `ConstraintManager`, set **Hall Floor** to the hall floor's collider and **Layout** to the
   scene's `FurnitureLayout`. The floor collider's top and edges define the allowed zone area.
3. On `DesktopPlanner`, set **Constraints** to the same `ConstraintManager`. This enables the
   desktop debug buttons.
4. Optional: add `ZoneDebugView`, set **Constraints** and any URP material.
   `DesktopNetworkTest` shows the finished wiring.

**Call from Quest code**
```csharp
constraints.RequestZone(cornerA, cornerB); // after the second floor hit, world-space points
constraints.RequestClearZone();            // clear action
```
- Requests are only submissions. The host may reject them (outside the floor, a side under
  0.1 m, invalid numbers); the previous zone then stays.
- Only works while connected. Quest must have joined the host first.
- Keep the first-corner preview local in your code; don't send it.

**Render the zone**
- Read `constraints.CurrentZone` once when your renderer enables, then subscribe to
  `constraints.ZoneChanged` and unsubscribe on disable.
- `IsActive == false` means hide. Otherwise use `HasConflict`: red if true, green if false.
- `CornerA` is the min X/Z corner, `CornerB` the max, both at floor height. `Center` and
  `SizeXZ` are there for convenience. Draw slightly above the floor.
- Don't set colors from input code; only react to `ZoneChanged`.

**Test**
1. In `DesktopNetworkTest`, press Play, then Start Host.
2. Click **Mark Keep-Clear Zone (debug)**, then click two floor points under the bed. It turns red.
3. Drag the bed out of the rectangle. It turns green. Drag it back: red.
4. In your scene with Quest joined: mark under a bed in VR, both devices show red;
   Dev A moves the bed on Windows, both turn green.

---

## 1. Desktop planner and furniture networking

**Status:** Merged into `main` (commit `48e9cae` and earlier).

**What it is:** The Windows host moves, rotates, adds, removes, and resets beds with the mouse.
Quest joins as a client and sees every change but cannot edit furniture.

**Assets**
- `Assets/SafeShelter/Prefabs/NetworkBed.prefab` — networked bed.
- `Assets/SafeShelter/Prefabs/SafeShelterNetworkPrefabs.asset` — prefab list for Netcode.
- `DesktopRig` and `NetworkSession` in `Assets/SafeShelter/Scenes/DesktopNetworkTest.unity` —
  working reference setup to copy.

**Wire into `SafeShelter.unity`**
1. Add one `NetworkManager` with one `UnityTransport`. Add
   `SafeShelterNetworkPrefabs.asset` to its prefab lists. Don't add a second manager.
2. Replace `Bed_01` with a `NetworkBed` instance at the same position. Keep one visible copy.
3. Copy `DesktopRig`. Set:
   - `SessionController`: **Network Manager** and **Transport**.
   - `FurnitureLayout`: **Bed Prefab** = `NetworkBed` asset, **Initial Furniture** = the scene
     bed(s), **Session** = the `SessionController`.
   - `DesktopPlanner`: **Planner Camera**, **Floor Plane** (transform at floor height),
     **Placement Surface** (hall floor collider), **Layout**, **Session**.
4. Enable `DesktopRig` only on Windows and the XR rig only on Quest.

**Connect**
- Windows clicks **Start Host**. Quest enters Dev A's LAN IPv4 (from `ipconfig`), port `7777`,
  then **Join Client**. Same Wi-Fi; `127.0.0.1` does not work from Quest.

**Test**
- Dev A drags, rotates, and resets the bed on Windows; Quest sees each change.
- Close and reopen the Quest app, then rejoin; it gets the current layout.
