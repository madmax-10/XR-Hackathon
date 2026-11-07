# SafeShelter — Developer B: Friday Implementation Guide

**Date:** Friday, November 6, 2026  
**Deadline:** Monday, November 9, 2026, at 12:00 PM  
**Developer B:** VR headset + powerful Mac/desktop  
**Stack:** Unity 6, existing VR template, OpenXR, XR Interaction Toolkit, Meta Quest, GitHub

> **Your responsibility today:** Deliver a working Quest build of a correctly scaled school hall, with four beds, a supply table, partitions, an exit, headset/controller tracking, and teleportation. Then integrate with Developer A to prove that one networked bed moves on both devices.
>
> **Do not spend Friday on:** simulated occupants, full keep-clear marker creation, networking authority/host implementation, high-fidelity assets, or visual polish.

## 0. Friday definition of done

- [ ] Existing Unity project opens with **zero compiler errors**.
- [ ] Correct Android/Quest build modules and OpenXR configuration are installed.
- [ ] Quest is detected and authorized for USB debugging.
- [ ] Existing VR template builds and runs as a **standalone Quest app**.
- [ ] `SafeShelter.unity` contains a **12 m × 8 m hall** with walls, exit, and two partitions.
- [ ] Four beds and one supply table are correctly positioned; one bed blocks the central passage.
- [ ] XR Origin is properly positioned; head and controller tracking work inside the hall.
- [ ] Controller ray and **teleportation** work on the real headset.
- [ ] Your scene/configuration changes are committed and pushed to GitHub.
- [ ] **Team integration milestone:** Developer A moves a bed on Windows; its movement appears in Quest.

**Suggested time budget:** Quest deployment (60 min), hall/furniture (90 min), VR navigation/testing (45 min), networking integration (45 min). These are planning allocations, not promises.

---

## 1. Coordinate with Developer A before modifying shared files

Agree on this ownership structure:

| You — Developer B | Developer A |
|---|---|
| `Assets/Scenes/SafeShelter.unity` (sole scene editor) | Networking host/client setup |
| Hall, walls, floor, partitions, exit | Networked furniture logic/prefabs |
| Quest/OpenXR configuration and APK build | Desktop camera/UI and movement |
| XR Origin, locomotion, teleportation | Constraint manager/validator |
| Later: controller marking and zone visuals | Later: bots/navmesh tests |

**Avoid Unity merge conflicts:** Only you edit `SafeShelter.unity`. Developer A delivers prefabs/scripts you can place into the scene. Agree on a shared Unity Editor version and package lockfiles. Do not both change the same `ProjectSettings` or `Packages` files simultaneously.

**Agree on room coordinates:** X = left/right; Y = height; Z = forward/back. The top of the floor is at **Y = 0**, the hall is centered at X/Z = (0,0), and 1 Unity unit = 1 meter.

### Message you can send Developer A

> I'm handling the shared SafeShelter scene, Quest build, room geometry, VR rig, controller rays, and teleportation today. I'll create a 12×8 m hall with four beds, a supply table, partitions, and an exit. One bed initially blocks the central passage. Please handle NGO host/client networking and one networked movable bed. I'll own `SafeShelter.unity` and integrate your prefab, so we avoid scene merge conflicts. Tonight we'll test that moving a bed on Windows updates it inside Quest. Tomorrow I'll implement VR keep-clear marking against your constraint-manager interface.

---

## 2. Verify Unity and Android/Quest prerequisites

### 2.1 Confirm the exact Unity version

1. Open the existing GitHub project in **Unity Hub** (do not create another project).
2. Inspect `ProjectSettings/ProjectVersion.txt` to see the expected Editor version.
3. Install/open that **exact** Unity version (for example, `6000.3.27f1` if that is what the repository specifies).
4. Wait for import, then open **Console** and resolve all compile errors before proceeding.

### 2.2 Verify Unity Hub Android modules

Unity Hub → **Installs** → your Unity version → **Manage / Add modules**. Ensure:

- Android Build Support
- Android SDK & NDK Tools
- OpenJDK

You normally do not need Android Studio for basic Quest APK deployment using Unity's bundled tools.

### 2.3 Verify the XR packages

Unity → **Window → Package Management → Package Manager**. Confirm the template includes:

- XR Interaction Toolkit
- OpenXR Plugin
- XR Plug-in Management
- Input System
- Meta Quest/OpenXR platform support where required by the installed version

Keep the existing VR template's working XR rig and interaction stack. **Do not install an additional full VR framework unless essential.** Developer A handles adding Netcode for GameObjects and Unity Transport.

### 2.4 Configure OpenXR / Android target

1. **Edit → Project Settings → XR Plug-in Management**.
2. Select the Android/Quest target and enable **OpenXR**.
3. In OpenXR settings, enable the Meta Quest support feature group and appropriate Touch controller interaction profile for your headset.
4. Review **Project Validation** and fix blocking Android XR issues.
5. **File → Build Profiles** → select the Meta Quest / Android profile and switch or enable the platform.
6. In Player Settings verify the platform requirements; typically **IL2CPP** scripting backend and **ARM64** target architecture. For graphics APIs, prefer the already validated Quest template/profile configuration rather than switching blindly.

**Pass condition:** Android/Quest build profile is active and no blocking XR project validation errors remain.

---

## 3. First, deploy the existing VR template to Quest

**Do this before building the hall** so APK/USB problems cannot surprise you at the end.

### 3.1 Enable headset debugging

1. Confirm your Quest is in Developer Mode through its paired Meta Horizon mobile app/developer setup.
2. Connect Quest to the development computer using a USB-C **data** cable.
3. Put on the headset and approve any **Allow USB debugging** prompt; select *Always allow* if appropriate.

### 3.2 Check `adb devices`

If `adb` is available in your shell:

```bash
adb devices
```

If not, run the platform-tools binary packaged with the installed Unity version. A typical macOS location is:

```bash
"/Applications/Unity/Hub/Editor/<VERSION>/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb" devices
```

Expected pattern:

```text
List of devices attached
<device-serial>    device
```

- `unauthorized`: approve the prompt **inside Quest**.
- No devices: check the cable, Developer Mode, USB permissions, and the exact SDK/platform-tools location.

### 3.3 Build the template APK

1. Open the working VR template scene.
2. **File → Build Profiles** → Quest/Android profile.
3. Add/open the active test scene in the **Scene List** and make sure it is checked.
4. Choose the connected Quest under **Run Device**.
5. Click **Build and Run**. Save an APK such as `SafeShelter-Test.apk` outside the Assets folder.
6. On Quest, verify the scene renders and the head and controllers track correctly.

**Pass condition:** You can launch a standalone Unity APK on Quest and freely look around with correctly tracked controllers.

If this is blocked, **do not start complicated scene/networking code**. First fix the deployment foundation.

---

## 4. Create `SafeShelter.unity` from the working template

1. Open the working template scene.
2. **File → Save As** → `Assets/Scenes/SafeShelter.unity`.
3. Remove only unnecessary template demonstration objects (grab cubes, demos, sample props).
4. Keep the existing **XR Origin, tracking camera, controllers, input actions, locomotion system, XR Interaction Manager**, and other required XR components.
5. Create empty parent objects `Environment`, `Furniture`, and `Lighting` for organization.
6. Save the scene.

### 4.1 Coordinate and geometry rules

- 1 Unity unit = 1 meter.
- Hall footprint = X **-6 to +6** (12 m), Z **-4 to +4** (8 m).
- Hall floor walking surface = **Y = 0**.
- Room walls = **3 m high**.
- Doorway/exit opening = north center, 2 m wide.

### 4.2 Create the floor

GameObject → **3D Object → Cube**; name `HallFloor`.

| Transform | X | Y | Z |
|---|---:|---:|---:|
| Position | 0 | -0.1 | 0 |
| Rotation | 0 | 0 | 0 |
| Scale | 12 | 0.2 | 8 |

The top of this 0.2-m-thick cube is **Y = 0**. Retain its `BoxCollider`.

Create a Unity **GameObject Layer** named `Floor`, assign it to `HallFloor`, and later use it to filter controller physics raycasts for marker placement. This is different from XR Interaction Toolkit's **Interaction Layer Mask**.

### 4.3 Create the exterior walls

Create cubes with these exact names and transforms:

| Object | Position `(x, y, z)` | Scale `(x, y, z)` |
|---|---|---|
| `Wall_South` | `(0, 1.5, -4)` | `(12, 3, 0.2)` |
| `Wall_West` | `(-6, 1.5, 0)` | `(0.2, 3, 8)` |
| `Wall_East` | `(6, 1.5, 0)` | `(0.2, 3, 8)` |
| `Wall_North_Left` | `(-3.5, 1.5, 4)` | `(5, 3, 0.2)` |
| `Wall_North_Right` | `(3.5, 1.5, 4)` | `(5, 3, 0.2)` |

There is a 2-meter gap between the two north wall segments, representing the shelter exit.

### 4.4 Create two partitions

| Object | Position `(x, y, z)` | Scale `(x, y, z)` |
|---|---|---|
| `Partition_Left` | `(-3.5, 1, 0)` | `(5, 2, 0.2)` |
| `Partition_Right` | `(3.5, 1, 0)` | `(5, 2, 0.2)` |

These leave a central ~2 m passage running north/south. Keep partitions static and collidable; they are **not** movable furniture and do not need `NetworkObject` components.

### 4.5 Create four beds

Use cubes first; realistic furniture models are unnecessary. A single bed prototype is **1.8 × 0.45 × 0.9 m** (`x, y, z`) with its center at Y = 0.225.

| Bed | Position `(x, y, z)` | Scale `(x, y, z)` |
|---|---|---|
| `Bed_01` | `(0, 0.225, 0)` | `(1.8, 0.45, 0.9)` |
| `Bed_02` | `(-3.8, 0.225, -2.3)` | `(1.8, 0.45, 0.9)` |
| `Bed_03` | `(3.8, 0.225, -2.3)` | `(1.8, 0.45, 0.9)` |
| `Bed_04` | `(-3.8, 0.225, 2.2)` | `(1.8, 0.45, 0.9)` |

`Bed_01` intentionally obstructs the central partition passage. The planner will later move it out of the way. Use blue materials to make all beds easy to identify in Quest.

**Important:** For networking, Developer A owns the final network-ready furniture prefab. Coordinate before adding any network components yourself, and do not keep duplicate copies of `Bed_01` in the final scene.

### 4.6 Supply table and exit marker

Create a cube named `SupplyTable`:

| Transform | X | Y | Z |
|---|---:|---:|---:|
| Position | 3.5 | 0.4 | 2.2 |
| Scale | 1.2 | 0.8 | 0.8 |

Color it brown/tan. For a designated exit, create a thin green marker named `ExitMarker` near `(0, 0.03, 3.4)`. If the marker is only decorative, remove/disable its blocking collider. Developer A can create a separate exact route-destination Transform later.

### 4.7 Simple material palette

- Beds: blue
- Supply table: tan/brown
- Walls and partitions: light gray
- Floor: light neutral gray
- Exit: green
- Obstruction highlight (optional): orange

Use low-cost URP-compatible materials and minimal lighting; Quest performance and clarity matter more than photo-realism.

### 4.8 Target hierarchy

```text
SafeShelter
├── Environment
│   ├── HallFloor
│   ├── Walls
│   │   ├── Wall_South
│   │   ├── Wall_West
│   │   ├── Wall_East
│   │   ├── Wall_North_Left
│   │   └── Wall_North_Right
│   ├── Partitions
│   │   ├── Partition_Left
│   │   └── Partition_Right
│   └── ExitMarker
├── Furniture
│   ├── Bed_01
│   ├── Bed_02
│   ├── Bed_03
│   ├── Bed_04
│   └── SupplyTable
├── VRRig
│   └── XR Origin (reuse existing template rig)
├── Lighting
│   └── Directional Light
└── Networking (added during integration with Developer A)
```

Group objects carefully: **avoid accidental parent transforms with non-unit scale** that would alter the above dimensions and world coordinates.

---

## 5. Make VR navigation work

### 5.1 Verify the existing XR Origin

Find your template's XR Origin. Confirm that its components/configuration provide:

- Tracked HMD camera
- Left/right tracked controller objects
- Enabled Input Actions
- XR interaction system
- Locomotion mediator/provider (name depends on XR Interaction Toolkit version)

Start the inspector near the southern half of the room, e.g. set the XR Origin transform position to `(0, 0, -2.5)` while respecting the template's tracking-origin/floor-offset configuration. **Do not hard-code the HMD camera's local height** if floor tracking already supplies it.

### 5.2 Configure teleportation

First test any teleport functionality already supplied by the template. If missing:

1. Select `HallFloor` and add **Teleportation Area**.
2. Keep a valid floor collider.
3. On XR Origin, configure the existing **Locomotion Mediator** and **Teleportation Provider** if required by your XR Interaction Toolkit version.
4. Ensure a controller has an **XR Ray Interactor** that can target teleportable areas.
5. Include the teleport area in the controller's XR **Interaction Layer Mask**.
6. Ensure input actions for teleport activation and select are mapped/enabled.
7. Ensure the ray or teleport arc visual appears on the controller.

Use one controller for teleportation now; reserve the other controller for precise keep-clear marking on Saturday.

### 5.3 Check teleportation inside Quest

Build and run the new `SafeShelter.unity` scene on Quest. Verify:

- [ ] Headset eye level is natural (not underground or abnormally high).
- [ ] Hall appears human-scale.
- [ ] Controllers track when moved.
- [ ] Teleport ray/arc is visible on aiming.
- [ ] Aiming at floor allows teleportation.
- [ ] Teleportation lands on top of the floor, not inside it.
- [ ] You can inspect the obstruction and then teleport elsewhere.
- [ ] You cannot teleport to invalid walls/props.

**Don't add smooth locomotion** if the teleportation interaction works.

### Common VR problems

| Symptom | Check |
|---|---|
| No ray visible | Controller tracking, XR Ray Interactor, line visual, Input Actions |
| Ray visible but floor not selectable | Teleportation Area, collision, XR Interaction Layer Mask |
| Teleport puts player below floor | Tracking origin mode, floor Y = 0, rig offsets |
| Headset view is black | OpenXR Android configuration, active XR camera, build validation, rendering compatibility |
| Two camera views/audio warnings | Duplicate active rigs/cameras/audio listeners |

---

## 6. Prepare for Saturday's keep-clear zone (do not fully implement today)

Your Saturday files will likely be:

```text
Assets/Scripts/VR/VRInspector.cs
Assets/Scripts/Constraints/VRZoneInput.cs
Assets/Scripts/Constraints/ZoneRenderer.cs
```

Today's preparation checklist:

- [ ] Floor top at `Y = 0`.
- [ ] Floor has a collider and dedicated `Floor` GameObject layer.
- [ ] Furniture has fitting colliders and unique names.
- [ ] All users/builds share the same world-coordinate convention.
- [ ] Developer A knows your plan to send **two world-space floor positions**.

Tomorrow's intended interaction:

1. Quest controller ray hits the floor.
2. First trigger selects corner A.
3. Second trigger selects corner B.
4. B sends both world-space positions to A's authoritative `ConstraintManager`.
5. Both clients draw the same rectangle.
6. Host checks whether furniture footprints intersect the rectangle.
7. Rectangle displays **red** for overlap and **green** when clear.

Agree on an API boundary with A, for example `RequestZone(Vector3 cornerA, Vector3 cornerB)` and `RequestClearZone()`. These are **conceptual names**, not built-in Unity APIs; A can expose an RPC-backed implementation.

---

## 7. Integrate with Developer A's networking

**Target:** Windows is the host; Quest is the client; a bed moved on Windows visibly moves on Quest.

### 7.1 Request the networking deliverable

Ask A for:

- A tested `NetworkManager`/Unity Transport setup (or prefab).
- A tested `SharedFurniture` script/prefab for `Bed_01`.
- The host/client startup controls/instructions.
- Agreed UDP port (e.g. `7777`).
- Any build/scene dependencies you need to include.

### 7.2 Integrate into your scene

1. Pull/merge tested code from A; resolve Unity package changes cautiously.
2. Add the supplied network manager to `SafeShelter.unity` (only one manager).
3. Replace or upgrade `Bed_01` using A's agreed networked prefab/components.
4. Keep walls/floor static in both builds; they do not need network transforms.
5. Ensure both builds start the correct camera/rig for their platforms (desktop camera on Windows, XR rig on Quest).
6. Verify the Quest uses client startup, not a second host.
7. Verify host-authoritative movement and synchronization of the furniture transform.

### 7.3 Network test

1. Both devices join the **same reachable LAN/Wi-Fi** (avoid client-isolated guest networks).
2. A runs Windows host.
3. Record Windows LAN IPv4 address (e.g. from `ipconfig`).
4. Set Quest's connection destination to **that LAN IP**, using the agreed UDP port.
5. Quest starts the client.
6. Both see the same hall.
7. A moves `Bed_01`; you see it move in Quest.
8. A rotates the bed; you see matching rotation.
9. Disconnect/reconnect Quest to verify reproducibility.

**Important:** `127.0.0.1` inside Quest refers to the Quest itself, **not** the Windows host. If the devices are on different networks, plain LAN IP connection may not work; use a reachable network before adding Relay/internet networking.

If Quest fails to connect, first check port, LAN IP, firewall, Unity Transport configuration, and that both builds come from compatible project versions.

---

## 8. GitHub workflow and commit

Use the **existing** repository. Suggested branch: `dev/vr-inspector`.

```bash
git status
git add Assets Packages ProjectSettings
git commit -m "feat: create SafeShelter hall and Quest VR navigation"
git push origin dev/vr-inspector
```

If the branch does not exist yet, create it intentionally (coordinate with A). Do **not** commit Unity-generated `Library/`, `Temp/`, `obj/`, or APK/build outputs. Do commit `.meta` files, `Packages/manifest.json`, `Packages/packages-lock.json`, and relevant `ProjectSettings`.

Suggested final division:

```text
Assets/
├── Scenes/SafeShelter.unity           [B owns]
├── Scripts/VR/                       [B]
├── Scripts/Constraints/
│   ├── ConstraintManager.cs          [A]
│   ├── ConstraintValidator.cs        [A]
│   ├── VRZoneInput.cs                [B, Saturday]
│   └── ZoneRenderer.cs               [B, Saturday]
├── Scripts/Networking/              [A]
├── Scripts/Desktop/                 [A]
├── Scripts/Navigation/              [A, Sunday]
├── Prefabs/VR/                       [B]
├── Prefabs/Furniture/                [A]
└── Materials/                        [B]
```

---

## 9. End-of-Friday acceptance test

Perform these in order, checking the actual headset whenever applicable.

### Device/build
- [ ] Same exact Unity version as A.
- [ ] Zero red Console errors.
- [ ] Quest appears as an authorized Android device.
- [ ] Standalone `SafeShelter` APK launches and renders correctly.

### Environment
- [ ] Floor is 12 × 8 m, top at Y = 0.
- [ ] Walls, north exit opening, and two partitions exist.
- [ ] Four beds and one supply table exist.
- [ ] `Bed_01` obstructs the central passage.
- [ ] Geometry scale looks believable in VR.

### VR
- [ ] HMD movement/rotation are tracked.
- [ ] Controllers/ray work.
- [ ] Teleportation works inside the hall.
- [ ] Can examine the obstructed path at human scale.

### Collaboration
- [ ] A's networked bed integrated into your scene without duplicates.
- [ ] Windows host and Quest client connect on LAN.
- [ ] Moving/rotating bed on Windows updates the Quest view.
- [ ] All changed project files pushed to GitHub.

### If behind schedule, cut in this order

1. Decorative details/lighting polish.
2. Optional pillow geometry and labels.
3. Extra controller interactions beyond teleportation.
4. Additional movable furniture/networked props (keep **one networked bed** for Friday).

**Never cut the working Quest build, the common hall, or the one-bed cross-device test** if you can still make them work.

---

## 10. Saturday preview

After Friday's checkpoint, your next tasks are:

- Implement a straight controller ray filtered to the `Floor` GameObject layer.
- Capture two floor corners with two trigger presses.
- Send corners to A's networked constraint manager.
- Render a thin, translucent floor rectangle on both devices.
- Update marker colors from authoritative conflict state.
- Add a Clear Marker/Undo action.

**Today's single most important milestone:** Stand inside Quest and watch a bed move when your teammate changes it from the Windows desktop planner. That proves the shared-world architecture and protects the rest of the weekend.
