# SafeShelter integration contract

The user confirmed that the two project plans are the agreed contract and delegated routine
engineering decisions. This document makes their conceptual interface concrete. These are
implementation decisions for the next increments, not a claim that networking or zones exist yet.

## Ownership and coordinates

- A owns desktop input, authoritative host logic, shared furniture, and constraint state.
  B owns the main scene, Quest input, and zone presentation. B should not create a second
  SharedFurniture or ConstraintManager implementation.
- Use the `SafeShelter` namespace. Components receive Inspector references; no scene-name or
  GameObject-name lookup is part of the interface.
- One Unity unit is one meter. Points use the same Unity world coordinates on both devices.
  Y is vertical; furniture translates in X/Z and rotates around world Y. No miniature-world
  scaling: the desktop camera views the same full-size geometry from above.
- The hall floor is horizontal. Its height and allowed X/Z bounds come from serialized scene
  configuration supplied by B. DesktopTest uses a centered 12 by 8 meter floor at Y=0.
  Both builds must use identical hall transforms and bounds.

## Furniture and networking

- Keep `void SharedFurniture.MoveTo(Vector3 position)`, `void Rotate90()`, and
  `void ResetPose()`. Preserve the initial root height. Reset restores the host's starting
  pose for that scene instance. `PoseChanged` is a local notification for authoritative edits.
- The Windows process explicitly starts as host; Quest explicitly joins as client. A failed
  connection never silently grants local write authority.
- In the networked increment, all furniture mutation methods enforce server authority;
  clients never move furniture locally. Desktop controls run only on the host.
- Each furniture root gets NetworkObject and a server-authoritative NetworkTransform.
  Use NGO's session-scoped NetworkObjectId when network identity is needed; do not invent
  additional persistent furniture IDs for this MVP or use object names as IDs.
- Prove synchronization with one fixed scene-placed bed first. The user clarified that adding
  and removing furniture is also core planner work: next, FurnitureLayout will instantiate
  and network-spawn the registered bed prefab on the host, and network-despawn added furniture
  on removal. Clients receive those spawns; they never instantiate a duplicate locally.
  Register each dynamically spawned furniture prefab with the one NetworkManager. Avoid
  network ownership transfer. Reset removes added furniture and restores the scene baseline.
- Exactly one NetworkManager/transport pair belongs to the running session. SessionController
  receives a reference to it. Reusable desktop/furniture prefabs do not contain managers.
- On disconnect, editing stays disabled until the host session is valid again. Restart the
  scene/session for a clean reset; seamless reconnect/persistence is outside the initial MVP.
- Resolve compatible NGO and Unity Transport releases through the matching Unity editor,
  verify their APIs, then commit manifest and lockfile together. Both developers use those
  exact resolved versions; no independent package upgrades.

## Zone input and presentation

The following public interface is the target for the constraints increment:

```csharp
// All types belong to namespace SafeShelter.
void ConstraintManager.RequestZone(Vector3 cornerA, Vector3 cornerB);
void ConstraintManager.RequestClearZone();
bool ConstraintManager.GetConflict();
ZoneState ConstraintManager.CurrentZone { get; }
event Action<ZoneState> ConstraintManager.ZoneChanged;

// Read-only value snapshot exposed to presentation code:
// ZoneState.IsActive: bool
// ZoneState.CornerA: Vector3 (normalized minimum X/Z, configured floor Y)
// ZoneState.CornerB: Vector3 (normalized maximum X/Z, configured floor Y)
// ZoneState.HasConflict: bool
```

- B's VR component calls RequestZone after its second floor hit and RequestClearZone for clear.
  The mouse debug marker will call these same methods. Neither input component sets colors,
  network variables, or furniture poses.
- Request methods are asynchronous submissions, not proof of acceptance. The host validates
  and publishes a single coherent ZoneState snapshot. Any connected client may request a zone
  or clear for this two-person MVP; the host remains the only writer. Last valid request wins
  in host processing order. There is one zone, with no per-player ownership.
- Reject nonfinite coordinates, X/Z points outside the configured bounds, and rectangles with
  either side shorter than 0.1 meter. Normalize X/Z and project Y onto the configured floor.
  Invalid requests leave the previous state unchanged. Validate on the host even if input
  already validates locally. Log rejected requests on the host during development.
- Clear publishes an inactive, nonconflicting state. GetConflict returns false when inactive;
  presentation must check IsActive before displaying anything green.
- Replicate persistent state, including to late joiners; an event-only RPC is insufficient.
  ZoneChanged fires locally after the synchronized snapshot changes. B reads CurrentZone once
  when attaching/enabling the renderer, then subscribes and unsubscribes with its lifecycle.
- B owns first-corner preview locally. Finalized rendering uses synchronized state only:
  hidden when inactive, red when conflicting, green otherwise. Render slightly above floor Y.

## Overlap and acceptance

- The host compares the active zone with registered furniture collider world-space X/Z AABBs,
  ignoring vertical separation. Child colliders count toward the furniture footprint.
  AABB edge contact counts as conflict. This is deliberately conservative for rotated beds.
- Recalculate after an accepted zone request, furniture move, rotation, or reset. Publish the
  resulting corners/active/conflict fields as one snapshot, avoiding inconsistent partial UI.
- Test in order: local desktop controls; host plus second desktop client moving one bed;
  Quest viewing that same bed; Quest marking red and desktop moving it to green; mouse debug
  marking through the same request path. Include late joining and reset in network tests.
- Green means only that no registered furniture footprint overlaps the marked rectangle.
  Bots and route checks remain deferred until this collaboration loop works.
