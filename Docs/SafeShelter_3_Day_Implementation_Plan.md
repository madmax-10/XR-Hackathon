# SafeShelter — 3-Day Hackathon Implementation Plan

**Project:** SafeShelter — *Plan from above. Inspect from within. Improve together.*  
**Hackathon:** Friday, November 6 – Monday, November 9, 2026  
**Submission deadline:** Monday, November 9, 12:00 PM (confirm organizers' time zone)  
**Team:** 2 developers  
**Platform:** Unity 6, Windows desktop, Meta Quest VR  
**Already completed:** Unity VR template project created; GitHub version control configured.

---

## 1. Goal and scope

Build a collaborative disaster-shelter layout prototype in one shared virtual school hall:

1. **Shared hall:** The desktop and Quest display the same hall, beds, supply table, and markers.
2. **Desktop layout editor:** The planner can select, move, and rotate furniture from above.
3. **VR inspection:** The inspector can look around, teleport, and point at locations at human scale.
4. **Shared spatial constraints:** The inspector marks a two-corner *keep-clear zone*. Both devices see the zone, which is **red** when furniture overlaps and **green** when clear.
5. **Occupant route testing:** Three simple bots, or at minimum three navigation reachability checks, test fixed paths before and after furniture is moved.

**Core hackathon interaction:** The inspector identifies an obstructed passage in VR, marks it with geometry, and the desktop planner resolves the obstruction in real time. This cross-perspective collaboration is more important than bot animations or visual polish.

### Deliberate non-goals

No AI agents, disaster physics, floor-plan generation, real-world mapping, room scanning, complex crowd simulation, photorealistic assets, or medical/emergency certification.

### Initial scene

| Element | Initial target |
| --- | --- |
| Hall | Approximately 12 m × 8 m |
| Beds | 4 movable |
| Supply station | 1 movable table |
| Partitions | 2 simple static sections |
| Exit | 1 designated destination |
| Bots | 3 capsules/simplified agents |
| Keep-clear zone | 1 synchronized rectangle for MVP |

Make the initial layout intentionally contain a blocked bottleneck so the before-and-after improvement is visible.

---

## 2. Developer assignments

### Developer A — Windows laptop, no headset

**Owns desktop controls and shared game logic.** All of this work is testable in the Unity Editor without Quest hardware.

- Multiplayer host startup and network configuration.
- Network-authoritative furniture movement, rotation, and reset.
- Desktop overhead camera, mouse selection, dragging, rotation, and UI.
- Network state for the keep-clear zone; handling zone-creation requests.
- X/Z furniture footprint versus zone overlap testing.
- Bot pathfinding, route start/reset, and result display.
- Windows build and desktop recording for the final demo.
- Mouse-driven **debug marker** tool so zone logic can be tested before VR integration.

### Developer B — VR headset + powerful Mac/desktop

**Owns the physical scene and Quest experience.** This work requires actual headset testing.

- Shared hall geometry, lighting, scale, exit, and scene assembly.
- Quest/OpenXR/Android build configuration and deployment.
- VR rig, controller rays, headset viewing, and teleportation.
- Two-click floor-point selection for keep-clear zone creation.
- Translucent rectangle rendering and red/green feedback on both devices.
- Clear/undo marker interaction and minimal VR instructions.
- Quest recording, visual polish, and assembly of the final demo video.

### Shared responsibilities

- Friday and Saturday cross-device integration.
- Verify network connection on the actual event network.
- Test the full demonstration from a clean launch.
- README, demo script, and submission package.

**Ownership rule:** B edits the main `SafeShelter.unity` scene; A supplies reusable scripts/prefabs. This reduces Unity scene merge conflicts.

---

## 3. Recommended technical architecture

**One Unity project; two builds; one authoritative host.**

```text
Windows desktop (HOST)
  - Planner controls + UI
  - Authoritative furniture transforms
  - Keep-clear state + overlap validation
  - Bot route/pathfinding simulation
              |
              | Unity Transport on reachable LAN
              | Netcode for GameObjects
              v
Meta Quest (CLIENT)
  - VR inspection + teleportation
  - Controller floor raycast
  - Two-point marking request
  - Displays synchronized furniture, zone, bots/results
```

### Stack

| Layer | Tool |
| --- | --- |
| Engine | Existing Unity 6 VR project |
| VR | OpenXR + XR Interaction Toolkit |
| Desktop build | Windows standalone |
| Quest build | Android |
| Multiplayer | Netcode for GameObjects (NGO) + Unity Transport |
| Navigation | Unity AI Navigation |
| Rendering | URP / existing VR template pipeline |
| Source control | GitHub |

### Networking rules

- Windows hosts the session (`NetworkManager.StartHost()`); Quest joins as a client (`NetworkManager.StartClient()`).
- Use the host's **LAN IPv4 address**, a matching UDP port (for example `7777`), and a reachable Wi-Fi/LAN. Watch out for firewall or guest-network client isolation.
- Both builds share identical static room geometry: do **not** network the floor or walls every frame.
- Movable furniture has a `NetworkObject` and `NetworkTransform`. The host applies requested changes and replicates the transforms.
- The Quest sends two world-space floor positions to the host using an NGO server RPC (`[Rpc(SendTo.Server)]` for compatible NGO versions).
- The host validates and stores the zone's corners, active state, and conflict state in synchronized state such as `NetworkVariable`s.
- Both devices render the same rectangle from the synchronized positions and status.
- The host runs bot navigation and transmits movement/test results.
- Only one zone is required for the MVP. Avoid dynamic network spawning of marker objects unless necessary.

### Integration API contract

Agree on interface names and coordinate conventions before implementing. These are **conceptual interfaces**, not drop-in code:

```csharp
// Developer A: authoritative logic
SharedFurniture.MoveTo(position);
SharedFurniture.Rotate90();
ConstraintManager.RequestZone(cornerA, cornerB);
ConstraintManager.RequestClearZone();
ConstraintManager.GetConflict();
BotManager.RunTest();
BotManager.ResetTest();

// Developer B: Quest input and presentation
VRInspector.SelectFirstCorner();
VRInspector.SelectSecondCorner();
VRInspector.ClearMarker();
ZoneRenderer.UpdateZone();
ZoneRenderer.UpdateConflictColor();
```

**Main integration point:** B's VR input calls A's `ConstraintManager.RequestZone(cornerA, cornerB)` after the second controller trigger. The host checks conflicts and syncs the result.

---

## 4. Day-by-day schedule

### Friday, Nov 6 — Foundation and first network proof

**Developer A**

1. Confirm a matching Unity version and required packages.
2. Add/check NGO and Unity Transport; set up host/client startup.
3. Implement a `SharedFurniture` component for one bed.
4. Make the bed movable from the host and visible across the network.
5. Test in the Editor; prepare a Windows standalone build if possible.

**Developer B**

1. Configure Android/OpenXR and build the existing VR template to Quest.
2. Confirm headset pose, controller ray, and teleportation work on-device.
3. Build the simple 12 × 8 m hall, walls, exit, partitions, four beds, and supply table.
4. Use basic colored materials and check room scale/eye height.
5. Merge/import the shared bed prefab without changing A's underlying networking scripts.

**Friday definition of done:** **One bed moved on the Windows host visibly moves inside Quest**, with both clients displaying the same hall.

**If blocked:** Pause nonessential scene polish. Resolve the Quest build and LAN connection first.

### Saturday, Nov 7 — Main interaction

**Developer A**

1. Implement mouse raycast selection and floor-plane movement.
2. Add `R` for 90° rotation; add Reset Layout.
3. Implement networked keep-clear zone state and zone request handling.
4. Compare furniture footprints with the marked floor rectangle.
5. Update conflict status whenever a relevant furniture transform changes.
6. Add Windows mouse-based debug marking to test independently.

**Developer B**

1. Finish VR floor-targeting with a controller ray.
2. First trigger chooses corner A; second trigger chooses corner B.
3. Render a thin translucent floor rectangle with a visible outline.
4. Display **red** when obstructed and **green** when unobstructed.
5. Add a Clear Marker action and simple VR control instructions.
6. Test complete interaction on the actual Quest build.

**Saturday definition of done:** **Quest marks an obstructed zone → Windows sees the zone → planner moves the bed → both devices show green.**

This is the **non-negotiable milestone**. If it works, the project's central idea is demoable.

### Sunday, Nov 8 — Bots, reliability, and presentation

**Developer A**

1. Configure Unity AI Navigation with a walkable floor/navmesh.
2. Add three fixed bot spawn/destination pairs.
3. Make furniture influence paths through navigation obstacles/carving; let carving settle before measuring routes.
4. Add Run Test, Reset Test, and routes reachable/completed UI.
5. Run the same start/destination pairs before and after layout changes.
6. Fix networking, state-reset, and navigation bugs.

**Developer B**

1. Polish zone readability, room lighting, and basic HUD instructions.
2. Test a fresh Quest install/build.
3. Capture headset footage showing the blocked zone and resolution.
4. Help verify bot results match the actual scene.
5. Assemble the demo video and polish the README with A.

**Sunday definition of done:** A fresh launch reliably completes the full before/after story. The repository, README, and demo recording are submission-ready **Sunday evening**.

**If bots take too long:** Replace animated agents with three genuine path-reachability checks. Do not fake results or sacrifice the shared VR marking feature.

### Monday, Nov 9 — Final validation and submit before noon

- Fresh-launch check: host starts, Quest connects, furniture syncs, VR marker works, conflicts update, results display.
- Confirm final commit, repository access, README, and video link.
- Submit by **11:30 AM** to keep a buffer before the **12:00 PM** deadline.
- Do not introduce new features Monday morning.

---

## 5. Critical dependency path

```text
VR template & Quest build  ──┐
                            ├─> Shared hall + first connected Quest session
Host networking ────────────┘                     |
                                                 v
                        One synchronized movable bed
                                    |
                 ┌──────────────────┴──────────────────┐
                 v                                     v
       Desktop furniture editing              Quest floor ray/marking
                 └──────────────────┬──────────────────┘
                                    v
                    Networked keep-clear rectangle
                                    |
                                    v
                    Live red/green overlap feedback
                                    |
                                    v
                         Bot routes / results
                                    |
                                    v
                      Recorded reliable demo
```

Do not start bot polish before the red/green end-to-end interaction works.

---

## 6. File ownership and Git workflow

```text
Assets/
├── Scenes/
│   └── SafeShelter.unity             [B]
├── Scripts/
│   ├── Networking/                  [A]
│   │   ├── SessionController.cs
│   │   └── SharedFurniture.cs
│   ├── Desktop/                     [A]
│   │   └── DesktopPlanner.cs
│   ├── Constraints/
│   │   ├── ConstraintManager.cs      [A]
│   │   ├── ConstraintValidator.cs    [A]
│   │   ├── VRZoneInput.cs            [B]
│   │   └── ZoneRenderer.cs           [B]
│   ├── VR/                          [B]
│   │   └── VRInspector.cs
│   └── Navigation/                  [A]
│       └── BotManager.cs
├── Prefabs/
│   ├── Furniture/                   [A]
│   ├── VR/                          [B]
│   └── Bots/                        [A]
└── Materials/                        [B]
```

### Working agreement

- Branches: `dev/desktop-networking` and `dev/vr-inspector`; merge tested changes frequently into `main`.
- B owns scene integration; A owns furniture/network/bot prefab internals.
- Never independently make conflicting changes to the same `.unity` scene or prefab.
- Enable **Visible Meta Files** and **Force Text** serialization in Unity.
- Commit all needed `.meta` files, `Packages/manifest.json`, `Packages/packages-lock.json`, and `ProjectSettings`.
- Use the same Unity Editor version; pull changes before modifying shared files.
- Integrate once on Friday and repeatedly on Saturday; do not postpone integration until Sunday.

---

## 7. Key implementation guidance

### Desktop controls

| Input | Result |
| --- | --- |
| Left click | Select furniture |
| Left drag | Move selected furniture over floor |
| `R` | Rotate by 90° |
| Right drag | Orbit overhead camera |
| Mouse wheel | Zoom |
| Run Test | Start or reset route checks |
| Reset Layout | Restore initial furniture positions |

If smooth dragging is difficult, fall back to **click furniture → click destination on floor**. Prioritize reliability.

### VR controls

| Input | Result |
| --- | --- |
| Head movement | Inspect from human scale |
| Controller teleport | Relocate around the hall |
| Controller ray + first trigger | Choose first floor corner |
| Controller ray + second trigger | Choose opposite floor corner; create zone |
| Clear/undo control | Remove accidental marker |

### Keep-clear validation

For the two floor-hit points `A` and `B`, use X/Z coordinates:

```csharp
minX = Mathf.Min(A.x, B.x);
maxX = Mathf.Max(A.x, B.x);
minZ = Mathf.Min(A.z, B.z);
maxZ = Mathf.Max(A.z, B.z);
```

Compare this rectangle with each movable furniture footprint. For a rapid MVP, a world-space collider bounding box is a conservative approximation; for more accurate handling of rotated beds, compare oriented footprint corners.

**Important:** Green means the user-marked region has no furniture overlap. It does **not** certify an accessible or emergency-safe layout.

### Bot test

- Bot 1: bed area → supply station.
- Bot 2: bed area → exit.
- Bot 3: another fixed spawn → exit.
- Give bots fixed start/destination coordinates and reuse them for the before/after test.
- On the host, use `NavMeshAgent` path queries and `NavMeshObstacle` carving on stationary furniture.
- Report actual `pathComplete`/reachability results; do not hardcode success counts.
- Start with **routes reachable / 3**. Animated capsules are optional if real movement proves too costly.

---

## 8. Troubleshooting and fallback rules

| Risk | Immediate fallback |
| --- | --- |
| Windows–Quest connection fails | Fix transport IP/port, firewall, and LAN isolation before adding features |
| Quest controller interactions are complex | Basic floor raycast and two trigger presses |
| Desktop dragging feels unreliable | Select object, click destination |
| Multiple markers are complex | Support just one persistent synchronized zone |
| Bot navigation/carving is unreliable | Three real path queries, with results but no animated agents |
| Running out of time | Cut assets and animation polish, **not** shared VR constraints |
| Merge conflicts | Keep scene ownership with B, prefab/script ownership with A |

---

## 9. Acceptance checklist

### Core — must be done

- [ ] Windows host starts successfully.
- [ ] Quest connects to Windows over the local network.
- [ ] Both devices show the same hall and furniture.
- [ ] Desktop can move/rotate furniture.
- [ ] Quest can look around, teleport, and point at floor locations.
- [ ] VR can select two floor corners to mark a region.
- [ ] Both devices show the same marked rectangle.
- [ ] Overlap turns the marker red.
- [ ] Moving the obstructing bed makes the marker green on both devices.
- [ ] Resetting the scene makes the demonstration repeatable.

### Supporting feature — after core works

- [ ] Three identical routes are tested before and after.
- [ ] Route-reachability results reflect actual navigation queries.
- [ ] Optional bot animations are consistent on both devices.

### Submission

- [ ] Demo works from a fresh launch.
- [ ] GitHub repository and setup instructions are ready.
- [ ] 3–5 minute demo/code walkthrough is recorded (if required by organizers).
- [ ] Both viewpoints appear in the demo video.
- [ ] Submission is completed before Monday noon.

---

## 10. Demo video outline (approximately 4 minutes)

| Time | Show |
| --- | --- |
| 0:00–0:35 | Disaster shelter layout problem and project goal |
| 0:35–1:20 | Desktop overview editing and VR human-scale inspection |
| 1:20–2:35 | VR marks a blocked passage; desktop moves the bed; red → green |
| 2:35–3:10 | Rerun the same three paths and show genuine results |
| 3:10–4:00 | Explain host authority, RPC/zone synchronization, and overlap validation |

**Developer A:** Record desktop perspective and explain networking, constraints, and bots.  
**Developer B:** Record Quest perspective and explain VR input and why the spatial marking interaction matters.

### Final demo script

1. Planner adds or moves beds into a layout that blocks a passage.
2. Planner runs the initial navigation test.
3. VR inspector teleports near the bottleneck and points out the problem.
4. VR inspector selects two floor corners to mark a keep-clear rectangle.
5. The zone appears on Windows and turns red because furniture overlaps it.
6. Planner moves the bed away; both devices see the zone turn green.
7. Planner reruns exactly the same paths and compares actual results.

---

## Final recommendation

**Friday night:** One synchronized bed visible from Windows and Quest.  
**Saturday night:** Complete working collaboration: VR mark → desktop sees it → desktop clears it → both views turn green.  
**Sunday:** Add honest route tests, improve reliability, record footage, finish submission.  
**Monday morning:** Validate and submit; no new feature work.

A polished, repeatable two-device spatial interaction is the deliverable. Everything else supports that moment.
