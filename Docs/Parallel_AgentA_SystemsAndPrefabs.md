# Agent A — Systems, Prefabs, Keep-Clear Code

**Blind parallel track.** You will **not** see Agent B’s scene edits until morning integration.  
This file is **self-contained**. Also mirrored in [`Parallel_LockedSceneSpec.md`](Parallel_LockedSceneSpec.md) — if anything conflicts, **LockedSceneSpec wins**, but the numbers below match it.

| Item | Value |
|---|---|
| Your role | Scripts, prefabs, editor validation, tests |
| Demo scene (you do NOT edit) | `Assets/Scenes/SafeShelter_Integration.unity` |
| Partner brief (read-only at start) | `Docs/Parallel_AgentB_IntegrationWorld.md` |
| Deferred | Physical Quest headset test (morning, last) |

---

## 0. Goal

Ship everything Agent B can wire by name/field without guessing:

1. Prefabs: `NetworkSupplyTable`, `NetworkCrate`, `NetworkMedicalTable`, `NetworkChair` (+ keep `NetworkBed`)
2. `QuestKeepClearMarker.cs` with **locked field names** in §5.7
3. Zone presentation ready (`ZoneDebugView`)
4. Prefab list + any FurnitureLayout/smoke/validate updates
5. Prove mark→red→green on `DesktopNetworkTest` (not integration scene)

Hero loop: desktop moves furniture → Quest marks keep-clear → both red → desktop clears → both green → Reset restores **22** baseline props.

---

## 1. Ownership fence

### Allowed
- `Assets/SafeShelter/Scripts/**`
- `Assets/SafeShelter/Prefabs/**`
- `Assets/SafeShelter/Editor/**`
- `Assets/SafeShelter/Tests/**`
- `Assets/SafeShelter/Scenes/DesktopNetworkTest.unity` (proof only)
- `Tools/**` if smoke needs it

### Forbidden
- **`Assets/Scenes/SafeShelter_Integration.unity`** (Agent B only)
- `Assets/Scenes/SafeShelter.unity`
- Second ConstraintManager API / alternate zone methods
- Renaming locked serialized fields in §5.7 after B may have started wiring notes

If integration needs a scene change: document it under §9; do **not** edit the scene.

---

## 2. LOCKED WORLD + FURNITURE CONTRACT (identical for both agents)

### 2.1 Hall envelope

| Item | Value |
|---|---|
| Footprint | **24 × 16 m** |
| X | **−12 … +12** |
| Z | **−8 … +8** |
| Floor top Y | **0** |
| Wall height | **3** |
| Exit gap | North, X **−1.5 … +1.5**, Z = +8 |
| Floor layer | **Floor = layer index 8** |
| Namespace | `SafeShelter` |
| Units | 1 unit = 1 m |

### 2.2 Environment transforms Agent B will build (you must not assume old 12×8)

| Name | Position | Scale |
|---|---|---|
| `HallFloor` | `(0, -0.1, 0)` | `(24, 0.2, 16)` — Layer Floor (8), has Collider |
| `HallCenter` | `(0, 0, 0)` | empty |
| `Wall_East` | `(12, 1.5, 0)` | `(0.2, 3, 16)` |
| `Wall_West` | `(-12, 1.5, 0)` | `(0.2, 3, 16)` |
| `Wall_South` | `(0, 1.5, -8)` | `(24, 3, 0.2)` |
| `Wall_North_Left` | `(-7.5, 1.5, 8)` | `(9, 3, 0.2)` |
| `Wall_North_Right` | `(7.5, 1.5, 8)` | `(9, 3, 0.2)` |
| `ExitMarker` | `(0, 0.03, 7.2)` | `(2.5, 0.06, 1.0)` |
| `Partition_AisleWest` | `(-1.25, 1.0, -0.5)` | `(0.2, 2, 11)` — static, no NetworkObject |
| `Partition_AisleEast` | `(1.25, 1.0, -0.5)` | `(0.2, 2, 11)` — static |
| `Partition_SupplyNorth` | `(-10, 1.0, 3)` | `(4, 2, 0.2)` |
| `Partition_SupplySouth` | `(-10, 1.0, -3)` | `(4, 2, 0.2)` |
| `Partition_MedicalNorth` | `(10, 1.0, 3)` | `(4, 2, 0.2)` |
| `Partition_MedicalSouth` | `(10, 1.0, -3)` | `(4, 2, 0.2)` |

`ConstraintManager` must keep using **`hallFloor` collider bounds** (not hardcoded 12×8). Larger floor automatically expands valid zone area.

### 2.3 Baseline furniture — 22 objects (exact names / poses B will place)

| # | Name | Prefab | Position | Rot Y |
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

`Bed_01_Blocker` is the demo obstruction in the central aisle.

### 2.4 Prefab specs you must create

Paths under `Assets/SafeShelter/Prefabs/`:

| Prefab | Resting root Y | Approx size XYZ | Color RGB | Register in SafeShelterNetworkPrefabs? |
|---|---|---|---|---|
| `NetworkBed.prefab` | **0.225** | 1.8×0.45×0.9 | keep | **Yes** (Add Bed) |
| `NetworkSupplyTable.prefab` | **0.4** | 1.2×0.8×0.8 | (0.55, 0.35, 0.20) | Yes (spawn list; scene uses instances) |
| `NetworkCrate.prefab` | **0.3** | 0.6×0.6×0.6 | (0.20, 0.45, 0.75) | Yes |
| `NetworkMedicalTable.prefab` | **0.4** | 1.4×0.8×0.7 | (0.90, 0.90, 0.90) | Yes |
| `NetworkChair.prefab` | **0.45** | 0.5×0.9×0.5 | (0.35, 0.35, 0.35) | Yes |

### 2.5 Required components on every networked furniture ROOT

1. `SharedFurniture`
2. `NetworkFurniture`
3. `NetworkObject`
4. `NetworkTransform` with:
   - AuthorityMode = **Server (0)**
   - Interpolate = true
   - Sync pos/rot/scale XYZ = true
   - InLocalSpace = false
   - PositionThreshold = 0.001
5. Collider(s) for X/Z footprint
6. Visible meshes; **no** NetworkManager on prefab

`FurnitureLayout.bedPrefab` remains **NetworkBed only** for Add Bed.

---

## 3. Already done — reuse

| Piece | File |
|---|---|
| Move/rotate/reset | `SharedFurniture.cs` |
| Presence hide | `NetworkFurniture.cs` |
| Add/remove/reset | `FurnitureLayout.cs` |
| Desktop zone debug | `DesktopPlanner.cs` |
| Host zone | `ConstraintManager.cs`, `ZoneState.cs` |
| Red/green slab | `ZoneDebugView.cs` |
| Network bed | `Prefabs/NetworkBed.prefab` |
| Prefab list | `Prefabs/SafeShelterNetworkPrefabs.asset` |
| Reference wiring | `Scenes/DesktopNetworkTest.unity` |

---

## 4. Task A1 — Prefabs

Create the four new prefabs per §2.4–2.5. Register all Add-spawnable (at least NetworkBed; register the new ones too so scene/NGO is happy if anything spawns them).

Do not change resting Y values — B’s scene poses depend on them.

---

## 5. Task A2 — `QuestKeepClearMarker` (locked API for blind wiring)

**File:** `Assets/SafeShelter/Scripts/QuestKeepClearMarker.cs`  
**Type:** `SafeShelter.QuestKeepClearMarker`

### Locked serialized fields — DO NOT RENAME

```csharp
[SerializeField] ConstraintManager constraints;
[SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor leftRay;
[SerializeField] UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor rightRay;
[SerializeField] LayerMask floorMask;   // B sets to Floor only (layer 8)
[SerializeField] float maxDistance = 30f;
[SerializeField] float previewHeight = 0.02f;
```

Also expose:

```csharp
public void ClearZone() // calls constraints.RequestClearZone()
```

### Behavior

1. On trigger/select with raycast hit on `floorMask` within `maxDistance`:  
   - 1st hit → store corner, show **local-only** preview at Y = hit.y + previewHeight  
   - 2nd hit → `constraints.RequestZone(first, second)`, clear preview  
2. `ClearZone()` → `RequestClearZone()`  
3. Optional cancel resets first corner  
4. No NetworkVariable writes, no color ownership, no furniture moves  
5. If `constraints` null or not spawned, no-op / warn  

B attaches this to GO `QuestKeepClearMarker` under XR Origin (not DesktopRig).

---

## 6. Task A3 — Zone presentation

Keep `ZoneDebugView` as shared desktop+Quest presenter.

Fields B will set:

| Field | Value |
|---|---|
| `constraints` | scene ConstraintManager |
| `material` | any URP Lit/Unlit |
| `heightAboveFloor` | 0.01 |
| `blockedColor` | (0.9, 0.15, 0.1) |
| `clearColor` | (0.15, 0.8, 0.25) |

Inactive hide; conflict red; else green; slab has **no collider**.

---

## 7. Task A4 — Validation helpers

Extend `IntegrationSetup` (or sibling) to fail if missing:

- Exactly one NetworkManager / SessionController / ConstraintManager
- ConstraintManager.hallFloor + layout assigned
- DesktopPlanner.constraints assigned
- ZoneDebugView.constraints assigned
- QuestKeepClearMarker present with constraints + rays
- FurnitureLayout.initialFurniture length **== 22**, no nulls, all have NetworkFurniture
- HallFloor collider size roughly 24×16 (warn if ~12×8)
- Unique nonzero NetworkObject IDs for Constraints + all 22 furniture

Menu path: document in §9 when done (e.g. `SafeShelter/Integration/Validate`).

---

## 8. Task A5 — Tests

- Keep zone assertions when ConstraintManager present
- Soft-skip if null remains OK until morning merge
- Prove on DesktopNetworkTest: mark under bed → red → move → green → clear → reset
- Two-process smoke still PASS on your Windows test build

---

## 9. Inspector wiring Agent B will perform (you do not do this)

B’s scene after merge must have:

### Constraints (new GO)

| Component field | Assignment |
|---|---|
| `ConstraintManager.hallFloor` | HallFloor Collider |
| `ConstraintManager.layout` | DesktopRig FurnitureLayout |
| `ZoneDebugView.constraints` | that ConstraintManager |
| `ZoneDebugView.material` | URP material |
| NetworkObject | on Constraints GO |

### DesktopPlanner

| Field | Assignment |
|---|---|
| `constraints` | Constraints.ConstraintManager |
| `placementSurface` | HallFloor Collider |
| `layout` | FurnitureLayout |
| `session` | NetworkSession.SessionController |

### FurnitureLayout

| Field | Assignment |
|---|---|
| `bedPrefab` | NetworkBed asset |
| `session` | NetworkSession.SessionController |
| `initialFurniture` | **22** refs in order: Bed_01_Blocker…Bed_11, SupplyTable_01/02, Crate_01…04, MedicalTable_01, Chair_01…04 |

### DesktopOrbitCamera

| Field | Value |
|---|---|
| `focus` | HallCenter |
| `distance` | 28 |
| `pitch` | 55 |
| `distanceLimits` | (6, 45) |

### QuestKeepClearMarker

| Field | Assignment |
|---|---|
| `constraints` | ConstraintManager |
| `leftRay` / `rightRay` | XR rays |
| `floorMask` | Floor (8) |
| `maxDistance` | 30 |
| `previewHeight` | 0.02 |

Assign the active aiming rays and include Floor (layer 8) in their raycast masks. The marker reads the configured select, activate, or UI press action on a floor hit and skips rays pointing at UI. The first hit shows a local corner preview. The second submits the rectangle to the host.

### Status block — fill when YOU finish

```
[x] NetworkSupplyTable.prefab ready
[x] NetworkCrate.prefab ready
[x] NetworkMedicalTable.prefab ready
[x] NetworkChair.prefab ready
[x] SafeShelterNetworkPrefabs.asset updated
[x] QuestKeepClearMarker.cs compiles with the exact §5 field names
[x] ZoneDebugView unchanged
[x] Validate menu path: Tools > SafeShelter > Validate Integration Scene
[x] DesktopNetworkTest red→green, clear, and reset verified on host and client
[x] Two-process Windows smoke PASS (Logs/AgentA-Smoke/host.log and client.log)
[x] Zero edits to SafeShelter_Integration.unity
```

All five furniture prefabs were opened and checked in Unity for dimensions, resting height, colliders, and server-authoritative transform settings. NetworkBed keeps its existing asset identity and materials with a centered root at Y = 0.225 m.

The Windows smoke run used simulated XR select and trigger input on `DesktopNetworkTest`. It verified the local first-corner preview, replicated red and green renderer colors, clear, and restoration of the desktop baseline bed while despawning an added bed. The 22-prop integration reset and physical Quest test are part of the morning merge.

---

## 10. Zone API (unchanged)

```csharp
void ConstraintManager.RequestZone(Vector3 cornerA, Vector3 cornerB);
void ConstraintManager.RequestClearZone();
bool ConstraintManager.GetConflict();
ZoneState ConstraintManager.CurrentZone { get; }
event Action<ZoneState> ZoneChanged;
```

Min side 0.1 m; host validates; one zone; last valid wins; late join gets snapshot.

---

## 11. Acceptance (Agent A done)

- [x] All prefabs open with server-authoritative NetworkTransform and correct resting Y
- [x] Prefab list updated
- [x] QuestKeepClearMarker field names match §5 exactly
- [x] DesktopNetworkTest mark→red→green→clear
- [x] Smoke PASS
- [x] §9 status block filled
- [x] **No** integration scene edits

---

## 12. Morning merge (you do not own scene)

1. Combine your branch with B’s integration scene  
2. B finishes any remaining wiring from §9  
3. Run Validate  
4. Rebuild Windows + APK  
5. Smoke with zones ON  
6. Quest headset test **last**
