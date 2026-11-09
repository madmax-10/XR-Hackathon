# SafeShelter Locked Scene Spec (Blind Integration Contract)

**Both parallel agents must treat this document as immutable.**  
Agent A and Agent B work without seeing each other’s mid-work. Morning merge only works if both build to these exact names, sizes, poses, counts, layers, and Inspector fields.

- Authoritative demo scene: `Assets/Scenes/SafeShelter_Integration.unity`
- Namespace: `SafeShelter`
- Units: **1 Unity unit = 1 meter**
- Do not invent alternate APIs, object names, sizes, or resting heights

---

## 1. Hall envelope (exact)

| Item | Value |
|---|---|
| Footprint | **24 m (X) × 16 m (Z)** |
| X range | **−12 … +12** |
| Z range | **−8 … +8** |
| Floor top Y | **0** |
| Wall height | **3** |
| Exit gap | North wall, X **−1.5 … +1.5** (3 m wide), Z = +8 |
| Origin | Hall center `(0,0,0)` |

### Environment object transforms (exact)

Parent under a static root named `Environment` (create if missing). All rotations ` (0,0,0)` unless noted. Scales assume Unity default cube = 1 m.

| GameObject name | Position | Scale | Notes |
|---|---|---|---|
| `HallFloor` | `(0, -0.1, 0)` | `(24, 0.2, 16)` | Top at Y=0. **Layer = Floor (index 8)**. Must have a Collider (BoxCollider preferred). Used by placement, teleport, ConstraintManager |
| `HallCenter` | `(0, 0, 0)` | `(1,1,1)` | Empty transform. DesktopOrbitCamera **Focus** |
| `Wall_East` | `(12, 1.5, 0)` | `(0.2, 3, 16)` | Layer Walls (9) if used |
| `Wall_West` | `(-12, 1.5, 0)` | `(0.2, 3, 16)` | |
| `Wall_South` | `(0, 1.5, -8)` | `(24, 3, 0.2)` | |
| `Wall_North_Left` | `(-7.5, 1.5, 8)` | `(9, 3, 0.2)` | Leaves exit gap |
| `Wall_North_Right` | `(7.5, 1.5, 8)` | `(9, 3, 0.2)` | Leaves exit gap |
| `ExitMarker` | `(0, 0.03, 7.2)` | `(2.5, 0.06, 1.0)` | Bright/green; not networked |
| `Partition_AisleWest` | `(-1.25, 1.0, -0.5)` | `(0.2, 2, 11)` | Static; **NOT** NetworkObject. Central aisle clear ≈ 2.5 m |
| `Partition_AisleEast` | `(1.25, 1.0, -0.5)` | `(0.2, 2, 11)` | Static; **NOT** NetworkObject |
| `Partition_SupplyNorth` | `(-10, 1.0, 3)` | `(4, 2, 0.2)` | Forms west supply annex |
| `Partition_SupplySouth` | `(-10, 1.0, -3)` | `(4, 2, 0.2)` | Opening toward hall at roughly X=−8 |
| `Partition_MedicalNorth` | `(10, 1.0, 3)` | `(4, 2, 0.2)` | Forms east medical annex |
| `Partition_MedicalSouth` | `(10, 1.0, -3)` | `(4, 2, 0.2)` | Opening toward hall at roughly X=+8 |

Replace old `Partition_Left` / `Partition_Right` with the four partition names above (delete or rename; do not leave duplicates that shrink aisles).

Annexes share the same `HallFloor` (no separate floor mesh). Supply annex ≈ X −12…−8, Z −3…+3. Medical annex ≈ X +8…+12, Z −3…+3.

### Layers (project already defines)

| Layer name | Index |
|---|---|
| Default | 0 |
| Floor | **8** |
| Walls | 9 |
| Furniture | 10 |

`HallFloor` **must** be Floor (8). Furniture roots may stay Default or Furniture (10); colliders must still be found by `GetComponentsInChildren<Collider>` for zone overlap.

---

## 2. Baseline furniture inventory (exact names + poses)

**Total baseline networked objects: 22** (11 beds + 2 supply tables + 4 crates + 1 medical table + 4 chairs).

| # | Exact GameObject name | Type / prefab | Position | Rotation Y | Root scale |
|---|---|---|---|---|---|
| 1 | `Bed_01_Blocker` | NetworkBed | `(0, 0.225, 0)` | `0` | `(1,1,1)` |
| 2 | `Bed_02` | NetworkBed | `(-8, 0.225, -5.5)` | `0` | `(1,1,1)` |
| 3 | `Bed_03` | NetworkBed | `(-8, 0.225, -3.5)` | `0` | `(1,1,1)` |
| 4 | `Bed_04` | NetworkBed | `(-6, 0.225, -5.5)` | `0` | `(1,1,1)` |
| 5 | `Bed_05` | NetworkBed | `(-6, 0.225, -3.5)` | `0` | `(1,1,1)` |
| 6 | `Bed_06` | NetworkBed | `(6, 0.225, -5.5)` | `0` | `(1,1,1)` |
| 7 | `Bed_07` | NetworkBed | `(6, 0.225, -3.5)` | `0` | `(1,1,1)` |
| 8 | `Bed_08` | NetworkBed | `(8, 0.225, -5.5)` | `0` | `(1,1,1)` |
| 9 | `Bed_09` | NetworkBed | `(8, 0.225, -3.5)` | `0` | `(1,1,1)` |
| 10 | `Bed_10` | NetworkBed | `(-8, 0.225, 5.0)` | `90` | `(1,1,1)` |
| 11 | `Bed_11` | NetworkBed | `(8, 0.225, 5.0)` | `90` | `(1,1,1)` |
| 12 | `SupplyTable_01` | NetworkSupplyTable | `(-10.0, 0.4, 0.0)` | `0` | `(1,1,1)` |
| 13 | `SupplyTable_02` | NetworkSupplyTable | `(-10.0, 0.4, 1.8)` | `0` | `(1,1,1)` |
| 14 | `Crate_01` | NetworkCrate | `(-10.8, 0.3, -1.6)` | `0` | `(1,1,1)` |
| 15 | `Crate_02` | NetworkCrate | `(-9.4, 0.3, -1.6)` | `0` | `(1,1,1)` |
| 16 | `Crate_03` | NetworkCrate | `(-10.8, 0.3, 2.6)` | `0` | `(1,1,1)` |
| 17 | `Crate_04` | NetworkCrate | `(-9.4, 0.3, 2.6)` | `0` | `(1,1,1)` |
| 18 | `MedicalTable_01` | NetworkMedicalTable | `(10.0, 0.4, 0.0)` | `0` | `(1,1,1)` |
| 19 | `Chair_01` | NetworkChair | `(10.0, 0.45, -1.6)` | `180` | `(1,1,1)` |
| 20 | `Chair_02` | NetworkChair | `(10.0, 0.45, 1.6)` | `0` | `(1,1,1)` |
| 21 | `Chair_03` | NetworkChair | `(8.8, 0.45, 0.0)` | `90` | `(1,1,1)` |
| 22 | `Chair_04` | NetworkChair | `(-4.0, 0.45, 6.0)` | `0` | `(1,1,1)` |

**Demo blocker:** `Bed_01_Blocker` at `(0, 0.225, 0)` sits in the central aisle between the aisle partitions. Keep-clear under this bed must start **red**.

**Side aisles** west of X=−1.25 and east of X=+1.25 stay mostly clear of the blocker.

Parent furniture under a root named `Furniture` (create if missing).

---

## 3. Prefab asset contract (Agent A builds; Agent B instances)

All paths under `Assets/SafeShelter/Prefabs/`.

| Prefab asset path | Resting root Y | Approx visual size (X,Y,Z) | Mesh color (approx RGB) | In `SafeShelterNetworkPrefabs.asset`? |
|---|---|---|---|---|
| `NetworkBed.prefab` (existing) | **0.225** | 1.8 × 0.45 × 0.9 | keep existing | **Yes** (Add Bed spawn) |
| `NetworkSupplyTable.prefab` | **0.4** | 1.2 × 0.8 × 0.8 | (0.55, 0.35, 0.20) | Yes if Add can spawn it; **required as prefab asset** even if scene-only |
| `NetworkCrate.prefab` | **0.3** | 0.6 × 0.6 × 0.6 | (0.20, 0.45, 0.75) | Yes if spawnable; required asset |
| `NetworkMedicalTable.prefab` | **0.4** | 1.4 × 0.8 × 0.7 | (0.90, 0.90, 0.90) + red accent OK | required asset |
| `NetworkChair.prefab` | **0.45** | 0.5 × 0.9 × 0.5 | (0.35, 0.35, 0.35) | required asset |

### Required components on every networked furniture ROOT (exact stack)

Order does not matter; all must exist on the **root**:

1. `Transform`
2. `SafeShelter.SharedFurniture`
3. `SafeShelter.NetworkFurniture`
4. `Unity.Netcode.NetworkObject`
5. `Unity.Netcode.Components.NetworkTransform` with:
   - `AuthorityMode` = **Server** (0)
   - `Interpolate` = true
   - Sync Position X/Y/Z = true
   - Sync RotAngle X/Y/Z = true
   - Sync Scale X/Y/Z = true
   - `InLocalSpace` = false
   - PositionThreshold = 0.001
6. At least one `Collider` on root or children (BoxCollider preferred) defining X/Z footprint
7. Visible mesh child(ren); **no** `NetworkManager` on prefab

`NetworkObject` scene instances: keep scene objects (Do Not Destroy / player prefab = false). Let Unity assign unique scene IDs on save.

### Add Bed only

`FurnitureLayout.bedPrefab` → **only** `NetworkBed` asset. Do not require multi-type Add for MVP.

---

## 4. Hierarchy names that must exist after Agent B’s work

```
SafeShelter_Integration (scene)
├── Environment
│   ├── HallFloor
│   ├── HallCenter
│   ├── Wall_East / Wall_West / Wall_South / Wall_North_Left / Wall_North_Right
│   ├── ExitMarker
│   ├── Partition_AisleWest / Partition_AisleEast
│   ├── Partition_SupplyNorth / Partition_SupplySouth
│   └── Partition_MedicalNorth / Partition_MedicalSouth
├── Furniture
│   ├── Bed_01_Blocker … Bed_11
│   ├── SupplyTable_01 / SupplyTable_02
│   ├── Crate_01 … Crate_04
│   ├── MedicalTable_01
│   └── Chair_01 … Chair_04
├── NetworkSession          (KEEP — NetworkManager, UnityTransport, SessionController)
├── DesktopRig              (KEEP — DesktopPlanner, FurnitureLayout, PlannerCamera, DesktopOrbitCamera)
├── PlatformRigSelector     (KEEP)
├── XR Origin …             (KEEP existing Quest rig / teleport / QuestSessionPanel)
└── Constraints             (NEW)
    ├── ConstraintManager
    ├── ZoneDebugView
    └── NetworkObject
```

Do **not** rename `NetworkSession`, `DesktopRig`, `HallFloor`, `HallCenter`.

---

## 5. Inspector field wiring matrix (exact)

Serialized field names are C# names as shown in scripts. Unity Inspector labels may be nicified (“Hall Floor”).

### 5.1 `NetworkSession` / `SessionController`

| Field | Must be |
|---|---|
| `networkManager` | Same GameObject’s `NetworkManager` |
| `transport` | Same GameObject’s `UnityTransport` |
| `address` | default OK (`127.0.0.1`); Quest overrides via panel |
| `port` | `"7777"` |
| `autoStartHost` | **true** (Windows/Editor host) |

NetworkManager: prefab list includes `SafeShelterNetworkPrefabs.asset`; no player prefab; scene management enabled (keep current).

### 5.2 `DesktopRig` / `FurnitureLayout`

| Field | Must be |
|---|---|
| `bedPrefab` | Prefab asset `Assets/SafeShelter/Prefabs/NetworkBed.prefab` |
| `session` | `NetworkSession` → `SessionController` |
| `initialFurniture` | **Size = 22**, in this exact order: `Bed_01_Blocker`, `Bed_02` … `Bed_11`, `SupplyTable_01`, `SupplyTable_02`, `Crate_01` … `Crate_04`, `MedicalTable_01`, `Chair_01` … `Chair_04` |

### 5.3 `DesktopRig` / `DesktopPlanner`

| Field | Must be |
|---|---|
| `plannerCamera` | Desktop planner camera |
| `floorPlane` | `HallFloor` transform (or same as today) |
| `placementSurface` | `HallFloor` **Collider** |
| `layout` | Same DesktopRig `FurnitureLayout` |
| `session` | `NetworkSession` → `SessionController` |
| `constraints` | `Constraints` → `ConstraintManager` |
| `showControls` | true |

### 5.4 `DesktopRig` / `DesktopOrbitCamera`

| Field | Must be |
|---|---|
| `focus` | `HallCenter` |
| `distance` | **28** |
| `pitch` | **55** |
| `distanceLimits` | **(6, 45)** — required so 24×16 hall is viewable |

### 5.5 `Constraints` / `ConstraintManager`

| Field | Must be |
|---|---|
| `hallFloor` | `HallFloor` Collider |
| `layout` | DesktopRig `FurnitureLayout` |

GameObject `Constraints` also has `NetworkObject` (scene object, not player).

### 5.6 `Constraints` / `ZoneDebugView`

| Field | Must be |
|---|---|
| `constraints` | Same `ConstraintManager` |
| `material` | Any URP Lit/Unlit material (assign a project material; translucent OK) |
| `heightAboveFloor` | `0.01` |
| `blockedColor` | `(0.9, 0.15, 0.1)` |
| `clearColor` | `(0.15, 0.8, 0.25)` |

### 5.7 `QuestKeepClearMarker` (Agent A creates; Agent B attaches)

**Exact script path:** `Assets/SafeShelter/Scripts/QuestKeepClearMarker.cs`  
**Exact component type:** `SafeShelter.QuestKeepClearMarker`  
**Attach to:** new empty child of the Quest XR Origin named `QuestKeepClearMarker` (NOT under DesktopRig).

**Locked serialized fields Agent A must use (do not rename):**

| Field name | Type | B assigns |
|---|---|---|
| `constraints` | `ConstraintManager` | `Constraints` object |
| `leftRay` | `UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor` | Left controller ray |
| `rightRay` | `UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor` | Right controller ray |
| `floorMask` | `LayerMask` | **Floor only** = layer 8 |
| `maxDistance` | `float` | `30` |
| `previewHeight` | `float` | `0.02` |

Behavior (locked):

1. Trigger / select on ray floor hit #1 → local preview only  
2. Hit #2 → `constraints.RequestZone(a,b)` then clear preview  
3. Public method `ClearZone()` → `constraints.RequestClearZone()` (B may hook a world-space button OnClick to this)  
4. Never writes NetworkVariables / colors / furniture  

### 5.8 `PlatformRigSelector` (keep)

| Field | Must be |
|---|---|
| `desktopRig` | `DesktopRig` |
| `xrRig` | Existing XR Origin root |
| `questOnlyObjects` | Keep QuestSessionPanel etc. as currently configured |

### 5.9 `QuestSessionPanel` (keep)

| Field | Must be |
|---|---|
| `session` | `NetworkSession` → `SessionController` |
| `xrCamera` | XR camera |

---

## 6. Zone / networking API (do not change)

```csharp
namespace SafeShelter {
  void ConstraintManager.RequestZone(Vector3 cornerA, Vector3 cornerB);
  void ConstraintManager.RequestClearZone();
  bool ConstraintManager.GetConflict();
  ZoneState ConstraintManager.CurrentZone { get; }
  event Action<ZoneState> ZoneChanged;
}
```

- Min zone side: **0.1 m**
- Host validates against `hallFloor.bounds` (so larger floor automatically expands allowed area)
- Overlap: furniture collider world X/Z AABB vs zone; edge contact = conflict
- One zone only; last valid request wins
- Late join must receive furniture + zone snapshot

---

## 7. Counts summary

| Category | Count |
|---|---|
| Hall size | 24 × 16 m |
| Baseline networked props | **22** |
| Beds | 11 |
| Supply tables | 2 |
| Crates | 4 |
| Medical tables | 1 |
| Chairs | 4 |
| Static partitions | 6 named |
| ConstraintManager | exactly 1 |
| NetworkManager | exactly 1 |
| Keep-clear zones | exactly 1 active max |

---

## 8. Acceptance probes that must pass after morning merge

1. HallFloor collider bounds size ≈ `(24, 0.2, 16)` (center near origin).  
2. `FurnitureLayout.initialFurniture.Length == 22` with no nulls.  
3. Desktop host: Mark zone under `Bed_01_Blocker` → red; move blocker to `(0, 0.225, -4)` → green; Reset → blocker back to `(0,0.225,0)`.  
4. Add Bed spawns NetworkBed; Reset removes adds and restores all 22.  
5. Smoke test: zone checks **enabled** (ConstraintManager present).  
6. Quest physical test **last** (deferred).

---

## 9. What each agent must NOT wait for

| Agent | Can finish without the other |
|---|---|
| A | All prefabs, QuestKeepClearMarker, ZoneDebugView, validators, DesktopNetworkTest proof |
| B | Full Environment geometry + all 22 named objects (beds from NetworkBed; other types from A’s prefabs **or** temporary scene clones using same resting Y and names from §2) |

If A’s non-bed prefabs are missing when B places props: B creates scene objects with the **exact names and poses in §2**, component stack in §3, and resting Y values — morning swap to A’s prefab assets if needed **without renaming**.
