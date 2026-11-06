# SafeShelter — Final Hackathon Project Plan

**Tagline:** Plan from above. Inspect from within. Improve together.

**Event:** Beyond Shared Reality — 3D User Interfaces for Shared Spatial Interaction  
**Hackathon deadline:** Monday, November 9, 2026, at 12:00 PM (confirm the organizer's time zone)  
**Team:** 1–2 developers  
**Devices:** Meta Quest headset + Windows/macOS laptop  
**Engine:** Unity 6, VR template, OpenXR, XR Interaction Toolkit, Netcode for GameObjects  
**Submission:** Public/reviewable project repository and 3–5 minute demo video, including a brief code explanation

---

## 1. Project overview

SafeShelter is a **collaborative 3D planning prototype** in which two people convert an empty school hall into a temporary disaster shelter.

- **Planner (laptop):** Sees a miniature, orbitable 3D overview. Moves and rotates beds, a supply table, and optional partitions.
- **Inspector (Meta Quest):** Experiences the exact same layout at human scale. Inspects circulation routes and marks places that should remain clear.
- **Shared feedback:** A VR-marked keep-clear region appears on both devices. Furniture overlapping the region causes a visible conflict; moving it away resolves the conflict.
- **Optional route test after the human interaction works:** Three simple simulated occupants travel between fixed start points, supplies, and an exit to demonstrate reachability before and after a layout change.

**Pitch:** “SafeShelter lets a planner and an inspector design and test a temporary shelter together before moving real equipment.”

**Safety positioning:** This is an exploratory planning and training prototype—not a certified disaster-safety, accessibility, or evacuation assessment system. A clear region and successful bot paths do not establish real-world code compliance or evacuation safety.

## 2. Why it fits the hackathon

| Prompt requirement | SafeShelter implementation |
|---|---|
| Two or more people collaborate on shared 3D content | Planner and inspector work in the same networked hall. |
| Different devices are encouraged | Laptop overview + Quest immersive room-scale view. |
| Users manipulate, inspect, annotate, or organize content | Furniture arrangement, immersive inspection, and shared floor constraints. |
| Interaction technique comes first | **Cross-Perspective Spatial Negotiation**: immersive observations become actionable geometry the overview collaborator can resolve. |

**Core contribution:** Not simply furniture dragging, multiplayer, or annotations. The contribution is the end-to-end interaction where one collaborator experiences the layout inside VR, marks a geometric spatial requirement, and the other collaborator uses immediate overlap feedback to improve the shared design.

We should **not** claim this is proven to be an unprecedented research technique without reviewing prior work.

## 3. Main user experience: one complete collaborative loop

1. **Plan:** The laptop planner arranges several beds and a supply station in the hall; one bed intentionally obstructs a route.
2. **Inspect:** The Quest inspector moves through the actual-size hall and recognizes the narrow or blocked passage.
3. **Constrain:** Using a controller ray, the inspector selects two floor corners defining a rectangular **keep-clear zone**.
4. **Share:** The zone appears immediately on both devices; it is **red** while furniture intersects it.
5. **Resolve:** The laptop planner drags the obstructing bed outside the zone. The zone turns **green** when the geometric conflict is gone.
6. **Test (secondary feature):** The planner runs three simple occupant routes, then compares the same test before and after the change.

**Hero moment for judges:** The inspector marks a corridor *from inside the VR room* and the planner sees and resolves that precise spatial constraint *from above*, live.

### Key interaction states

- **Preview:** Inspector has selected the first corner and is aiming at the second.
- **Conflict:** Finalized zone overlaps one or more furniture footprints (red).
- **Clear:** No tested furniture footprint overlaps the zone (green).
- **Removed:** Inspector clears an erroneous or outdated zone.

Colors indicate **user-defined geometric overlap only**, not safety certification.

## 4. Locked MVP scope

### Must-have (finish in this order)

1. **One shared hall:** Floor, four walls, designated exit, and identical starting layout on both devices.
2. **Laptop furniture editor:** Select, drag, rotate, and reset four beds plus one supply table.
3. **Quest inspection:** Human-scale XR view, controller rays, and teleportation/locomotion.
4. **Shared spatial constraint:** Two controller-ray floor points create a persistent keep-clear rectangle, visible on both clients.
5. **Overlap feedback:** Rectangle becomes red when furniture overlaps it and green when cleared.

### Add only after the human collaboration works

6. **Simple occupants:** Three agents on repeatable start/destination pairs; show routes completed and unreachable destinations.

### Explicitly out of scope

No earthquakes, weather, city-scale environment, realistic medical simulation, real-world scans, advanced crowd behavior, evacuation predictions, AI layout designer, automatic layout optimization, voice chat, multiplayer accounts, or elaborate character animations.

Do **not** implement ghost-proposal or accept/reject workflows for this MVP; live synchronized movement plus spatial constraints is the core.

## 5. Scene design

**Environment:** One simple school hall, approximately **12 m × 8 m** in Unity world units (1 unit = 1 meter).

| Asset | MVP quantity | Notes |
|---|---:|---|
| Floor + walls | 1 hall | Simple box geometry, neutral materials. |
| Beds | 4 | Low-poly boxes with pillows, movable on the floor. |
| Supply table | 1 | Movable, clearly distinguishable. |
| Partitions | 0–2 | Optional; use static ones initially. |
| Exit | 1 | Clearly labeled route-test destination. |
| Keep-clear region | At least 1 | Translucent floor rectangle. |
| Occupant bots | 3 (later) | Capsules are sufficient. |

Prepare an intentionally problematic starting layout so a bed blocks an important passage. Keep a **Reset Layout** control for reproducibility.

## 6. Controls

### Laptop planner

| Input | Action |
|---|---|
| Left click | Select furniture. |
| Left drag | Move selected furniture along the ground plane. |
| `R` | Rotate selected furniture by 90°. |
| Right drag | Orbit camera. |
| Mouse wheel | Zoom. |
| `Reset Layout` button | Restore original object placements. |
| `Run Test` button (later) | Reset and run the fixed bot routes. |

Use raycasting from the desktop camera to select furniture. Project drag rays onto the hall floor plane so objects remain on the ground.

### Quest inspector

| Input | Action |
|---|---|
| Headset movement | Inspect at human scale. |
| Controller teleportation | Navigate around the hall. |
| Controller ray | Aim at the floor. |
| Mark action, first press | Record first corner of keep-clear rectangle. |
| Mark action, second press | Record opposite corner and submit zone. |
| Clear/undo action | Remove a mistaken or old constraint. |

Keep the marking gesture easy to understand, with a visible preview between the two clicks. Pick an explicit controller binding and display it on an in-world help panel.

## 7. Unity setup and architecture

### Template and packages

- **Unity template:** VR (one project for both devices).
- **Rendering:** Universal Render Pipeline (URP).
- **XR:** OpenXR + XR Interaction Toolkit.
- **Multiplayer:** Netcode for GameObjects (NGO) + Unity Transport.
- **Bots (later):** Unity AI Navigation / NavMeshAgent.
- **Quest build:** Android.
- **Desktop build:** Windows or macOS, depending on available laptop.

Use one Unity scene and platform-specific input rigs: Quest activates the XR rig; desktop activates the orbit/overview camera and mouse UI. Make sure desktop builds **do not try to initialize an XR headset**.

### Networking design

**Laptop is the session host and the authoritative source of shared state. Quest is a client connected over a reachable local network.** Verify client-to-client connectivity early; venue Wi-Fi may isolate devices, in which case use a suitable private hotspot/router.

| Shared data | Direction and authority |
|---|---|
| Furniture positions/rotations | Desktop submits edits; host authoritatively updates synchronized furniture transforms. |
| Keep-clear region corners | Quest sends two floor coordinates to host; host creates persistent region state. |
| Zone conflict/clear state | Host evaluates furniture overlap and publishes result. |
| Bot positions/results (later) | Host runs navigation simulation and replicates bot positions/statuses. |
| Walls/floor | Identical static scene content on both clients; no per-frame synchronization required. |

Use `NetworkObject`/`NetworkTransform` for furniture and bots, and NGO RPCs plus replicated state for actions and persistent zone data. Ensure objects created at runtime are properly network-spawned, or use a fixed pre-spawned zone object for the first MVP.

**Important:** Synchronize the *meaningful state* (object poses, two corner coordinates, conflict status), not mesh vertex arrays or thousands of marker updates.

### Recommended scene hierarchy

```text
SafeShelterScene
├── NetworkManager
├── SessionController
├── Environment
│   ├── HallFloor
│   ├── Walls
│   ├── Exit
│   └── Partitions
├── Furniture
│   ├── Bed_01
│   ├── Bed_02
│   ├── Bed_03
│   ├── Bed_04
│   └── SupplyTable
├── SpatialConstraints
│   └── ConstraintManager
├── Navigation                   # Phase 4 only
│   ├── NavMeshSurface
│   ├── BotSpawnPoints
│   └── BotManager
├── DesktopRig
│   ├── OrbitCamera
│   └── PlannerUI
├── VRRig
│   ├── XROrigin
│   └── InspectorUI
└── Lighting
```

### Suggested core scripts

| Script | Purpose |
|---|---|
| `SessionController.cs` | Start host/client, connection controls, roles. |
| `PlatformRigSelector.cs` | Enable correct camera/input rig on each device. |
| `DesktopPlanner.cs` | Mouse raycast selection, dragging, rotation, reset. |
| `SharedFurniture.cs` | Network identity and synchronized furniture pose. |
| `VRInspector.cs` | Controller-ray floor marking and undo. |
| `ConstraintManager.cs` | Host-owned region creation/removal and replication. |
| `ConstraintValidator.cs` | Detect furniture overlap on the floor plane. |
| `FeedbackController.cs` | Preview/conflict/clear colors and status text. |
| `BotManager.cs` | Repeatable route testing (later). |

## 8. Keep-clear zone implementation

**Simplest version:** The inspector marks two opposite points on the ground, `A = (x1,z1)` and `B = (x2,z2)`. Build a floor-aligned, axis-aligned rectangle:

```text
minX = min(x1, x2)
maxX = max(x1, x2)
minZ = min(z1, z2)
maxZ = max(z1, z2)
```

Draw a translucent rectangular mesh or thin quad slightly above the floor to avoid z-fighting.

On the laptop host, test each movable furniture footprint against the region. For the MVP, axis-aligned bounding rectangles are acceptable; after furniture rotates, recalculate the world-space bounds. More precise rotated footprint intersection is a stretch improvement.

```text
conflict = any(furniture_footprint overlaps keep_clear_region)
zone_color = conflict ? RED : GREEN
```

Re-evaluate after furniture moves, resets, rotates, or the zone changes.

**Required behavior:** If the inspector creates a region under a bed, both views show red. Once the planner drags the bed clear, both views show green, without the inspector needing to press a confirmation button.

## 9. Simple bots (add after MVP)

Implement three **repeatable route tests**, not a crowd simulation:

| Bot | Fixed test route |
|---|---|
| Bot A | Sleeping area → supply station. |
| Bot B | Sleeping area → exit. |
| Bot C | Another designated starting point → exit. |

- Use a baked `NavMeshSurface` for the floor and `NavMeshAgent` on each bot.
- Add suitable `NavMeshObstacle` components to stationary furniture; carving may be used to adjust navigable routes after objects settle.
- If obstacle updates are unreliable during dragging, disable route testing while editing and trigger a refresh/repath after furniture is released.
- **Reset bots to exactly the same positions and targets for each test.**
- Run agent behavior on the host, with synchronized results and visuals on Quest.
- Show actual results only: routes completed/attempted and unreachable destinations. Optionally compare calculated path lengths.

**Demo design check:** Ensure the baseline layout really produces a detectable blockage or path failure; otherwise the before/after test won't demonstrate anything. Use a fixed, deterministic layout and route setup for the video.

**Limitations:** These toy agents do not model realistic occupant motion, congestion, evacuation timing, disability access, or disaster response requirements.

## 10. Implementation order and milestones

### Friday, Nov 6 — Connectivity and shared object

- [ ] Confirm Unity project and Git repository work on both machines.
- [ ] Build and run the app on Quest.
- [ ] Verify XR view/controller rays/teleportation.
- [ ] Add NGO host/client startup over LAN.
- [ ] Sync **one bed** from desktop to Quest.
- [ ] Commit working networking prototype.

**Done when:** Move a bed on the laptop and watch it move inside Quest.

### Saturday, Nov 7 — Signature collaboration

- [ ] Build the hall and four movable beds plus supply table.
- [ ] Complete laptop selection, dragging, rotation, and reset.
- [ ] Complete Quest floor-point selection and rectangle preview.
- [ ] Replicate keep-clear zone through the host.
- [ ] Implement overlap detection and red/green feedback.
- [ ] Run a full two-person scenario multiple times.
- [ ] Commit a stable playable version.

**Done when:** VR marks an obstructed area and desktop resolves it live.

### Sunday, Nov 8 — Test, polish, record

- [ ] Add three simple bots **only if** core interaction is stable.
- [ ] Confirm deterministic before/after test layout.
- [ ] Add short in-app control instructions, status labels, and a reset button.
- [ ] Test reconnection, Quest build, and fresh launch.
- [ ] Capture side-by-side desktop + Quest footage.
- [ ] Record team explanation of the key networking/constraint code.
- [ ] Finish README and repository housekeeping.
- [ ] Export and verify the 3–5 minute demo video.

**Done when:** A clean launch reproduces the full demo and the video is ready.

### Monday, Nov 9 — Submission before noon

- [ ] Verify repository link is accessible.
- [ ] Verify final video plays correctly and is 3–5 minutes.
- [ ] Confirm all required content, including the code walkthrough.
- [ ] Submit via the organizer's form before 12 PM in the specified time zone.

## 11. Division of labor for two developers

| Developer A — laptop / host | Developer B — Quest / inspector |
|---|---|
| Set up NGO host and synchronization. | Ensure Quest APK launches and XR input works. |
| Build hall, furniture prefabs, and desktop camera. | Set up teleportation and controller rays. |
| Implement furniture selection/drag/rotate. | Implement two-point floor-marking interaction. |
| Implement authoritative constraint overlap calculation. | Create marker preview and feedback visualization. |
| Implement host bot routing if time permits. | Assist bot visualization, testing, and video. |

**Integration contract:** Agree early on world coordinates, furniture IDs, the shared rectangle data structure, and which side owns network updates. Merge and test together at least once on Friday and repeatedly on Saturday.

**If working solo:** Prioritize Quest connection → one networked bed → one shared zone → live conflict resolution. Treat bots and visual polish as optional.

## 12. Definition of done

### Required for a strong core demo

- [ ] Laptop and Quest connect to the same session.
- [ ] Both render the same hall and furniture locations.
- [ ] Planner moves a bed; inspector sees it move live.
- [ ] Inspector teleports and points at the floor.
- [ ] Inspector creates a keep-clear region; planner sees it live.
- [ ] A bed crossing the region changes its conflict indicator.
- [ ] Planner resolves the overlap and both views show a clear region.
- [ ] The experience can be reset and repeated from a clean launch.

### Additional polish if time permits

- [ ] Three bots replay identical routes before/after.
- [ ] Route reachability results appear to both users.
- [ ] Instructor/help text is clearly readable in both views.
- [ ] Demo captures both users' simultaneous perspectives.

## 13. 3–5 minute demo video storyboard

**Recommended runtime: about 4 minutes.**

| Time | Content |
|---|---|
| 0:00–0:35 | Motivation: why planning from above misses human-scale passage issues. |
| 0:35–1:20 | Show the laptop layout editor and simultaneous Meta Quest walkthrough. |
| 1:20–2:35 | **Main innovation:** VR inspector creates a corridor constraint; desktop planner sees the conflict and resolves it. |
| 2:35–3:10 | Run the same optional bot routes before/after and show actual observed results; if bots are incomplete, instead show a second live constraint resolution. |
| 3:10–4:00 | Team explains shared coordinate state, network messages, and overlap detection in the code. |

Record the same sequence from both devices, ideally displayed side by side. Avoid any claims that simulated bot movement proves real emergency safety.

## 14. Risk management and cut order

| Risk | Mitigation |
|---|---|
| Quest and laptop cannot connect on venue Wi-Fi | Test LAN connectivity immediately; use a reachable private hotspot/router. |
| Quest build/controller setup consumes time | Use the existing VR template and test APK before adding gameplay. |
| Furniture drags jitter across devices | Let host own state; throttle updates or sync final poses as fallback. |
| XR user cannot place precise floor points | Use raycast hit indicators, previews, and undo. |
| Furniture conflict detection fails on rotation | Start with simple world-space bounds; clearly constrain or simplify rotation. |
| Navigation bots take too long | Cut bots entirely; core human collaboration is enough for the prompt. |
| No visible before/after difference | Design one intentionally blocked passage and test it from clean launch. |

**Cut order:** Fancy furniture assets → bot animation/metrics → bots entirely → extra furniture/partitions.  
**Never cut:** Two-device shared state, VR spatial constraint creation, and desktop conflict resolution.

## 15. Repository outline

```text
SafeShelter/
├── Assets/
│   ├── Scenes/SafeShelterScene.unity
│   ├── Scripts/
│   │   ├── Networking/
│   │   ├── Desktop/
│   │   ├── XR/
│   │   ├── Constraints/
│   │   ├── Furniture/
│   │   └── Navigation/
│   ├── Prefabs/
│   └── Materials/
├── Packages/
├── ProjectSettings/
├── README.md
└── .gitignore
```

Keep the `Assets`, `Packages`, and `ProjectSettings` folders tracked. Ignore Unity-generated `Library`, `Temp`, `Logs`, and build artifacts as appropriate. Commit throughout the event, not only at the end.

## 16. Submission-ready description

> **SafeShelter — Plan from above. Inspect from within. Improve together.**
>
> SafeShelter is a collaborative 3D planning prototype that helps a desktop planner and a Meta Quest inspector design a temporary disaster shelter inside a shared virtual school hall. The planner arranges beds and supplies from an overview, while the inspector experiences the space at human scale and marks keep-clear regions where circulation is obstructed. Those VR-created spatial constraints appear on both devices and provide live feedback as the planner revises the layout. Optional simulated occupants test repeatable routes before and after the changes.
>
> Our interaction concept, **Cross-Perspective Spatial Negotiation**, turns immersive spatial observations into actionable geometric constraints in a shared 3D design interface. SafeShelter is an exploratory training and planning prototype, not a certified evacuation-safety assessment tool.

---

## Final priority

> **Laptop moves a bed → Quest sees it → Quest marks an obstructed passage → laptop sees the keep-clear zone → planner moves the bed → both views show the conflict resolved.**

Build that sequence reliably before adding anything else.
