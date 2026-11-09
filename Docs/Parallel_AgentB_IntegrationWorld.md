# Agent B — Integration World, Layout, Scene Wiring

**Blind parallel track.** You will **not** see Agent A’s prefab/script commits until morning integration.  
This file is **self-contained**. Also mirrored in [`Parallel_LockedSceneSpec.md`](Parallel_LockedSceneSpec.md) — if anything conflicts, **LockedSceneSpec wins**, but the numbers below match it.

| Item | Value |
|---|---|
| Your role | Expand + fill + wire `SafeShelter_Integration` only |
| Scene you own | `Assets/Scenes/SafeShelter_Integration.unity` |
| Partner brief (read-only at start) | `Docs/Parallel_AgentA_SystemsAndPrefabs.md` |
| Deferred | Physical Quest headset test (morning, last) |

---

## 0. Goal

Make the integration scene a **large playable shelter** and wire keep-clear by **exact names/fields** so morning merge has no guesswork.

Deliver:

1. Hall **24×16 m**, multi-aisle partitions, supply + medical annexes, clear exit  
2. **22** named networked props at locked poses (11 beds, 2 supply, 4 crates, 1 medical, 4 chairs)  
3. Wire Constraints / ZoneDebugView / DesktopPlanner / QuestKeepClearMarker per §5  
4. FurnitureLayout `initialFurniture` length **22** in locked order  
5. Desktop Editor proof: mark under blocker → red → move → green → Reset  

Do **not** reimplement networking or ConstraintManager logic.

---

## 1. Ownership fence

### Allowed
- `Assets/Scenes/SafeShelter_Integration.unity` **exclusively**
- Assign Inspector refs on components already in that scene
- Add Agent A components onto scene objects when available
- Scene-local materials if needed for prop colors

### Forbidden
- `Assets/SafeShelter/Scripts/**` (do not edit C#)
- `Assets/SafeShelter/Prefabs/**` source assets (you **instance** them; you do not edit prefab assets)
- `Assets/SafeShelter/Editor/**`, `Tests/**`
- Second SharedFurniture / ConstraintManager implementation
- Using `SafeShelter.unity` as the demo target

If a script is missing mid-track: place named empty hosts / use NetworkBed clones with locked names/poses; finish wiring in morning.

---

## 2. LOCKED WORLD CONTRACT (build exactly)

### 2.1 Hall envelope

| Item | Value |
|---|---|
| Footprint | **24 × 16 m** |
| X | **−12 … +12** |
| Z | **−8 … +8** |
| Floor top Y | **0** |
| Wall height | **3** |
| Exit gap | North, X **−1.5 … +1.5** (3 m), Z = +8 |
| Units | 1 unit = 1 m |
| Floor layer | **Floor = index 8** |

### 2.2 Environment transforms (exact)

Parent under `Environment`. Rotation all identity unless noted.

| Name | Position | Scale | Rules |
|---|---|---|---|
| `HallFloor` | `(0, -0.1, 0)` | `(24, 0.2, 16)` | Layer **Floor (8)**. BoxCollider required. Placement + teleport + ConstraintManager |
| `HallCenter` | `(0, 0, 0)` | — | Empty. Orbit focus |
| `Wall_East` | `(12, 1.5, 0)` | `(0.2, 3, 16)` | |
| `Wall_West` | `(-12, 1.5, 0)` | `(0.2, 3, 16)` | |
| `Wall_South` | `(0, 1.5, -8)` | `(24, 3, 0.2)` | |
| `Wall_North_Left` | `(-7.5, 1.5, 8)` | `(9, 3, 0.2)` | |
| `Wall_North_Right` | `(7.5, 1.5, 8)` | `(9, 3, 0.2)` | |
| `ExitMarker` | `(0, 0.03, 7.2)` | `(2.5, 0.06, 1.0)` | Not networked; bright/green |
| `Partition_AisleWest` | `(-1.25, 1.0, -0.5)` | `(0.2, 2, 11)` | Static; **no** NetworkObject |
| `Partition_AisleEast` | `(1.25, 1.0, -0.5)` | `(0.2, 2, 11)` | Static; central aisle ≈ 2.5 m clear |
| `Partition_SupplyNorth` | `(-10, 1.0, 3)` | `(4, 2, 0.2)` | West supply annex |
| `Partition_SupplySouth` | `(-10, 1.0, -3)` | `(4, 2, 0.2)` | |
| `Partition_MedicalNorth` | `(10, 1.0, 3)` | `(4, 2, 0.2)` | East medical annex |
| `Partition_MedicalSouth` | `(10, 1.0, -3)` | `(4, 2, 0.2)` | |

Delete or replace old `Partition_Left` / `Partition_Right` so they do not fight the new aisle widths.

Annex regions (same HallFloor, no second floor mesh):

- Supply ≈ X −12…−8, Z −3…+3  
- Medical ≈ X +8…+12, Z −3…+3  

Add lights if annexes are dark. Keep teleport area covering full HallFloor including annexes.

### 2.3 Preserve these existing systems (do not delete)

| Object | Components / role |
|---|---|
| `NetworkSession` | NetworkManager, UnityTransport, SessionController — **keep SessionController here**, not under DesktopRig |
| `DesktopRig` | DesktopPlanner, FurnitureLayout, PlannerCamera, DesktopOrbitCamera |
| `PlatformRigSelector` | desktop vs XR |
| XR Origin + teleport + `QuestSessionPanel` | Quest path |

Do not add a second NetworkManager. Port stays **7777**. Host auto-start stays on.

---

## 3. LOCKED FURNITURE LIST (22 objects — exact)

Parent under `Furniture`. Root scale `(1,1,1)` for all.

| # | Exact name | Prefab to instance | Position | Rot Y |
|---|---|---|---|---|
| 1 | `Bed_01_Blocker` | NetworkBed | `(0, 0.225, 0)` | 0 |
| 2 | `Bed_02` | NetworkBed | `(-8, 0.225, -5.5)` | 0 |
| 3 | `Bed_03` | NetworkBed | `(-8, 0.225, -3.5)` | 0 |
| 4 | `Bed_04` | NetworkBed | `(-6, 0.225, -5.5)` | 0 |
| 5 | `Bed_05` | NetworkBed | `(-6, 0.225, -3.5)` | 0 |
| 6 | `Bed_06` | NetworkBed | `(6, 0.225, -5.5)` | 0 |
| 7 | `Bed_07` | NetworkBed | `(6, 0.225, -3.5)` | 0 |
| 8 | `Bed_08` | NetworkBed | `(8, 0.225, -5.5)` | 0 |
| 9 | `Bed_09` | NetworkBed | `(8, 0.225, -3.5)` | 0 |
| 10 | `Bed_10` | NetworkBed | `(-8, 0.225, 5.0)` | 90 |
| 11 | `Bed_11` | NetworkBed | `(8, 0.225, 5.0)` | 90 |
| 12 | `SupplyTable_01` | NetworkSupplyTable | `(-10.0, 0.4, 0.0)` | 0 |
| 13 | `SupplyTable_02` | NetworkSupplyTable | `(-10.0, 0.4, 1.8)` | 0 |
| 14 | `Crate_01` | NetworkCrate | `(-10.8, 0.3, -1.6)` | 0 |
| 15 | `Crate_02` | NetworkCrate | `(-9.4, 0.3, -1.6)` | 0 |
| 16 | `Crate_03` | NetworkCrate | `(-10.8, 0.3, 2.6)` | 0 |
| 17 | `Crate_04` | NetworkCrate | `(-9.4, 0.3, 2.6)` | 0 |
| 18 | `MedicalTable_01` | NetworkMedicalTable | `(10.0, 0.4, 0.0)` | 0 |
| 19 | `Chair_01` | NetworkChair | `(10.0, 0.45, -1.6)` | 180 |
| 20 | `Chair_02` | NetworkChair | `(10.0, 0.45, 1.6)` | 0 |
| 21 | `Chair_03` | NetworkChair | `(8.8, 0.45, 0.0)` | 90 |
| 22 | `Chair_04` | NetworkChair | `(-4.0, 0.45, 6.0)` | 0 |

**Demo blocker:** `Bed_01_Blocker` at aisle center. Keep-clear under it must read **red**. Side aisles stay relatively clear.

### If Agent A prefabs are not visible yet

You may still place objects **now**:

- Beds: duplicate / instance existing `NetworkBed`  
- Other types: create roots with **exact names and poses**, resting Y from table, and this component stack copied from a working bed:
  1. `SharedFurniture`
  2. `NetworkFurniture`
  3. `NetworkObject`
  4. Server-auth `NetworkTransform` (AuthorityMode Server, Interpolate on, sync pos/rot/scale)
  5. Collider footprint + distinct colored mesh  

Morning: replace meshes with A’s prefab instances **without renaming** GameObjects.

### Prefab resting heights (do not drift)

| Prefab | Root Y |
|---|---|
| NetworkBed | 0.225 |
| NetworkSupplyTable | 0.4 |
| NetworkCrate | 0.3 |
| NetworkMedicalTable | 0.4 |
| NetworkChair | 0.45 |

Expected colors if using A’s prefs: supply brown, crate blue, medical white, chair gray.

---

## 4. Target hierarchy

```
SafeShelter_Integration
├── Environment
│   ├── HallFloor, HallCenter, Walls…, ExitMarker, Partitions…
├── Furniture
│   ├── Bed_01_Blocker … Bed_11
│   ├── SupplyTable_01, SupplyTable_02
│   ├── Crate_01 … Crate_04
│   ├── MedicalTable_01
│   └── Chair_01 … Chair_04
├── NetworkSession
├── DesktopRig
├── PlatformRigSelector
├── XR Origin… / QuestSessionPanel…
└── Constraints                    ← CREATE
    └── (ConstraintManager + ZoneDebugView + NetworkObject)
```

---

## 5. LOCKED INSPECTOR WIRING (every field)

### 5.1 Create `Constraints`

1. Empty GameObject named **`Constraints`**
2. Add `NetworkObject` (scene object, not player)
3. Add `SafeShelter.ConstraintManager`
4. Add `SafeShelter.ZoneDebugView`

| Component.field | Assign |
|---|---|
| `ConstraintManager.hallFloor` | `HallFloor` Collider |
| `ConstraintManager.layout` | DesktopRig → `FurnitureLayout` |
| `ZoneDebugView.constraints` | that ConstraintManager |
| `ZoneDebugView.material` | any URP Lit/Unlit |
| `ZoneDebugView.heightAboveFloor` | `0.01` |
| `ZoneDebugView.blockedColor` | `(0.9, 0.15, 0.1)` |
| `ZoneDebugView.clearColor` | `(0.15, 0.8, 0.25)` |

Exactly **one** ConstraintManager in the scene.

### 5.2 `FurnitureLayout` (on DesktopRig)

| Field | Assign |
|---|---|
| `bedPrefab` | `Assets/SafeShelter/Prefabs/NetworkBed.prefab` |
| `session` | NetworkSession → SessionController |
| `initialFurniture` | **Size 22**, exact order below |

**initialFurniture order (index 0…21):**  
`Bed_01_Blocker`, `Bed_02`, `Bed_03`, `Bed_04`, `Bed_05`, `Bed_06`, `Bed_07`, `Bed_08`, `Bed_09`, `Bed_10`, `Bed_11`, `SupplyTable_01`, `SupplyTable_02`, `Crate_01`, `Crate_02`, `Crate_03`, `Crate_04`, `MedicalTable_01`, `Chair_01`, `Chair_02`, `Chair_03`, `Chair_04`

Missing entries break Reset and zone overlap.

### 5.3 `DesktopPlanner` (on DesktopRig)

| Field | Assign |
|---|---|
| `plannerCamera` | planner camera |
| `floorPlane` | HallFloor transform |
| `placementSurface` | HallFloor **Collider** |
| `layout` | same FurnitureLayout |
| `session` | NetworkSession → SessionController |
| `constraints` | Constraints → ConstraintManager |
| `showControls` | true |
| `selectionMask` | keep ~0 / current |

### 5.4 `DesktopOrbitCamera`

| Field | Value |
|---|---|
| `focus` | HallCenter |
| `distance` | **28** |
| `pitch` | **55** |
| `distanceLimits` | **(6, 45)** |

### 5.5 `SessionController` (on NetworkSession) — verify, don’t break

| Field | Value |
|---|---|
| `networkManager` | same GO NetworkManager |
| `transport` | same GO UnityTransport |
| `port` | `"7777"` |
| `autoStartHost` | true |

NetworkManager prefab list still references `SafeShelterNetworkPrefabs.asset`.

### 5.6 `QuestKeepClearMarker` (when A’s script exists)

1. Create empty child under **XR Origin** named `QuestKeepClearMarker`  
2. Add `SafeShelter.QuestKeepClearMarker`  
3. **Do not** parent under DesktopRig (disabled on Quest)

| Field (A locked names) | Assign |
|---|---|
| `constraints` | Constraints → ConstraintManager |
| `leftRay` | Left XRRayInteractor |
| `rightRay` | Right XRRayInteractor |
| `floorMask` | **Floor only** (layer 8) |
| `maxDistance` | `30` |
| `previewHeight` | `0.02` |

Optional: world-space UI button OnClick → `QuestKeepClearMarker.ClearZone()`.

If script missing: still create the empty `QuestKeepClearMarker` GO and leave a note; wire in morning.

### 5.7 `PlatformRigSelector` / `QuestSessionPanel` — verify

| Object | Fields |
|---|---|
| PlatformRigSelector | `desktopRig`=DesktopRig, `xrRig`=XR Origin root |
| QuestSessionPanel | `session`=SessionController, `xrCamera`=XR camera |

---

## 6. Zone API you call (do not reimplement)

```csharp
constraints.RequestZone(cornerA, cornerB);
constraints.RequestClearZone();
// Read only:
constraints.CurrentZone; constraints.ZoneChanged; constraints.GetConflict();
```

Min side 0.1 m; host may reject; presentation only via ZoneDebugView.

---

## 7. Work order (can start immediately)

1. Resize/rebuild Environment per §2.2  
2. Place all 11 beds from NetworkBed at locked poses (blocker at origin)  
3. Place other 11 props (A prefabs or temporary networked clones with locked names/Y)  
4. Fill FurnitureLayout initialFurniture (22, exact order)  
5. Update orbit camera limits; confirm placementSurface = HallFloor  
6. Create Constraints + wire DesktopPlanner.constraints  
7. Desktop Play: Mark Keep-Clear under blocker → red; move to `(0, 0.225, -4)` → green; Reset  
8. Add QuestKeepClearMarker host + wire when script available  

---

## 8. Desktop acceptance (no headset)

- [ ] HallFloor world size ≈ 24×16, top Y=0, layer Floor  
- [ ] 6 named partitions + annexes + exit present  
- [ ] 22 furniture names/poses match §3  
- [ ] initialFurniture length 22, no nulls  
- [ ] Constraints + DesktopPlanner.constraints wired  
- [ ] Mark under Bed_01_Blocker → red; clear path → green; Clear Zone; Reset restores all 22  
- [ ] Add Bed / Remove / Rotate still work on new floor  
- [ ] Orbit can see whole hall (distanceLimits max 45)  
- [ ] No second NetworkManager / ConstraintManager  
- [ ] Did not edit Agent A script/prefab **assets**

---

## 9. Morning integration sequence

1. Pull/combine Agent A scripts+prefabs with your scene  
2. Replace any temporary non-bed meshes with A’s Network* prefabs **keeping names**  
3. Finish QuestKeepClearMarker field wiring (§5.6)  
4. Run Agent A’s Integration **Validate** menu — fix all errors  
5. Rebuild Windows exe + Quest APK from integration scene  
6. `Tools/Run-NetworkSmokeTest.ps1` — expect zone checks **ON**  
7. One more desktop red→green dry run  
8. **Last:** Quest headset LAN join, teleport, VR two-point mark → Windows resolve  

---

## 10. Counts checklist

| Item | Required |
|---|---|
| Hall | 24×16 |
| Baseline networked props | **22** |
| Beds | 11 |
| Supply tables | 2 |
| Crates | 4 |
| Medical tables | 1 |
| Chairs | 4 |
| Named static partitions | 6 |
| ConstraintManager | 1 |
| NetworkManager | 1 |
