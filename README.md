# SafeShelter

> **Plan from above. Inspect from within. Improve together.**

SafeShelter is a collaborative XR prototype for planning a temporary disaster shelter before real equipment is moved. A laptop-based planner arranges furniture from an overhead view while a Meta Quest inspector experiences the same hall at human scale. The inspector can mark a keep-clear area directly on the floor; the shared area turns red when furniture overlaps it and green when the planner resolves the obstruction.

SafeShelter is an exploratory planning and training prototype. It is **not** a certified evacuation, accessibility, building-code, or disaster-safety assessment tool.

## The collaboration loop

1. The laptop planner arranges beds and supplies in a shared hall.
2. The Quest inspector walks or teleports through that same layout at human scale.
3. The inspector marks two floor corners for a keep-clear region.
4. Both devices see the region turn **red** when furniture overlaps it.
5. The planner moves the obstructing furniture; the region turns **green** for both collaborators.

The core interaction is **cross-perspective spatial negotiation**: an observation made inside an immersive space becomes shared geometry that another collaborator can resolve from an overview.

## Features

- One shared school-hall scene with beds, a supply table, walls, floor, and exit.
- Desktop orbit camera and mouse-based furniture selection, dragging, rotation, adding, removal, and reset.
- Quest XR rig with controller-ray floor marking and in-headset LAN connection UI.
- Host-authoritative furniture synchronization with Netcode for GameObjects.
- A replicated keep-clear zone with host-side point validation and overlap checks.
- Red/green shared visual feedback for furniture overlap.
- Late-join-aware replicated scene furniture and zone state.
- Host-run resident simulation with Normal activity and Meal rush profiles, blocked/clear layouts, pantry/toilet queues, and 90-second comparison metrics.

## Tech stack

- **Unity:** `6000.3.27f1`
- **Rendering:** Universal Render Pipeline (URP)
- **XR:** OpenXR and XR Interaction Toolkit `3.5.1`
- **Networking:** Netcode for GameObjects `2.7.0` and Unity Transport `2.6.0`
- **Input:** Unity Input System `1.20.1`
- **Targets:** Windows desktop host and Meta Quest / Android client

## Quick start

### Prerequisites

- Unity Hub with **Unity `6000.3.27f1`** installed.
- A Windows machine for the desktop host.
- For the cross-device demo: a Meta Quest in developer mode, USB debugging authorized, and both devices on the same reachable LAN or private hotspot.
- A network that allows UDP traffic on port `7777`; guest Wi-Fi networks commonly isolate devices.

### Open the project

1. Clone or download this repository.
2. In Unity Hub, add the repository root as a project.
3. Open it in Unity `6000.3.27f1` and allow package import to finish.
4. Open [`Assets/Scenes/SafeShelter_Integration.unity`](Assets/Scenes/SafeShelter_Integration.unity).
5. Check the Unity Console for errors before entering Play mode.

For an editor desktop check, press **Play**, focus the Game view, and use the controls below. In the editor, the desktop role is used even when Android is selected as the build target.

### Desktop controls

| Input | Action |
|---|---|
| Left-click furniture | Select it |
| Left-drag selected furniture | Move it on the floor |
| `R` | Rotate selected furniture 90° |
| Right-drag | Orbit the planner camera |
| Mouse wheel | Zoom |
| **Add Bed** | Enter placement mode; click the floor to add a bed |
| **Remove Selected** | Remove the currently selected furniture |
| **Reset Layout** | Restore the starting furniture layout |
| **Mark Keep-Clear Zone (debug)** | Select two floor corners from the desktop |
| `Escape` | Cancel bed placement or zone marking |

The desktop is the authoritative furniture editor. Quest clients intentionally receive a read-only furniture view.

The right-hand **People and traffic** panel offers Blocked/Clear layout presets, Normal activity and Meal rush profiles, Start/Restart and Stop controls, and live and completed 90-second results. Compare completed round trips, pantry/toilet visits, peak aisle queue, and mean aisle wait using the same profile for both layouts. Keep the Quest viewer out of the lane during a measured run; viewer proximity can cause residents to yield.

## Run a shared Windows + Quest session

1. Build and run the Windows application from the integration scene. On Windows, it starts the Netcode host on UDP port `7777` by default.
2. Find the Windows LAN IPv4 address with `ipconfig`. Use the address of the adapter shared with the Quest—not `127.0.0.1`, a disconnected adapter, or an unrelated VPN.
3. Build and install the Android application on Quest.
4. In Quest, use the in-headset LAN panel to enter the Windows IPv4 address and port `7777`, then select **Connect**.
5. Drag or rotate furniture on Windows and confirm that it updates in Quest.
6. In Quest, point a controller ray at the floor and make two selections to define a keep-clear region. Move an overlapping bed on Windows and confirm the region changes from red to green in both views.

If the Quest cannot connect, verify that both devices are on the same non-isolated LAN, the IPv4 address is correct, UDP `7777` is available through the Windows firewall, and both builds use the same project revision.

### Optional launch arguments

The session controller supports explicit roles for desktop testing:

```text
SafeShelter.exe --safeshelter-role host --safeshelter-port 7777
SafeShelter.exe --safeshelter-role client --safeshelter-address 192.168.1.10 --safeshelter-port 7777
```

The Quest client rejects loopback addresses because `127.0.0.1` refers to the headset itself.

## Architecture

```text
Windows Host                         Quest Client
────────────                         ────────────
DesktopPlanner ── furniture requests ─┐
                                      ├─ NetworkManager + Unity Transport
QuestKeepClearMarker ─ zone requests ─┘
                                             │
                                  Host-authoritative shared state
                                  ├─ Furniture poses / presence
                                  └─ Zone corners / conflict state
                                             │
                         Desktop + Quest render the same result
```

### Key scripts

| Script | Responsibility |
|---|---|
| `SessionController.cs` | Configures the one NetworkManager/transport, starts host/client sessions, and handles reconnecting. |
| `PlatformRigSelector.cs` | Activates the desktop planner rig on desktop and the XR rig on Android. |
| `DesktopPlanner.cs` | Handles mouse selection, floor-plane dragging, rotation, layout controls, and desktop zone-marking debug input. |
| `FurnitureLayout.cs` | Owns registered starting furniture, runtime beds, removal, and deterministic reset behavior. |
| `SharedFurniture.cs` | Provides shared move, rotate, and reset behavior. |
| `NetworkFurniture.cs` | Adds server-authoritative network transform and replicated presence behavior to furniture. |
| `ConstraintManager.cs` | Accepts zone requests, validates them on the host, calculates overlap, and replicates one shared zone snapshot. |
| `ZoneState.cs` | Defines the compact synchronized keep-clear state: active flag, two corners, and conflict flag. |
| `QuestKeepClearMarker.cs` | Collects two valid Quest controller-ray hits on the floor and submits the zone request. |
| `ZoneDebugView.cs` | Renders the replicated zone red for conflict and green when clear. |

### Networking model

The Windows desktop is the session host and the authority for shared state.

- Furniture poses are synchronized with NGO `NetworkObject` / `NetworkTransform` components.
- The host owns movement, rotation, removal, reset, and runtime spawning.
- Quest sends keep-clear requests via RPC; the host validates the floor points before changing the shared zone.
- The keep-clear state is a single replicated `NetworkVariable`: two normalized floor corners, active state, and conflict state.
- The host tests present furniture colliders in X/Z against the zone. A touching edge counts as an overlap.
- The host runs the resident simulation and replicates occupant transforms, visuals, and results; Quest clients render the shared result.

This sends meaningful spatial state rather than meshes or high-frequency marker data.

## Scenes and testing

| Scene / tool | Purpose |
|---|---|
| `Assets/Scenes/SafeShelter_Integration.unity` | Main integrated scene for the Windows-and-Quest demo. |
| `Assets/SafeShelter/Scenes/DesktopNetworkTest.unity` | Desktop-focused networking test scene. |
| `Tools/Run-NetworkSmokeTest.ps1` | Two-process Windows smoke test for network synchronization. |

To run the Windows network smoke test against a built executable:

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Run-NetworkSmokeTest.ps1 -Executable Builds/Integration/SafeShelter.exe
```

The smoke test is useful for networking behavior, but it does not replace a real headset test for tracking, controller input, teleportation, rendering, LAN connectivity, or Quest UI.

## Demo video

The timed, YC-style demo plan—including the code walkthrough and recording checklist—is in [Docs/Demo_Video_Script.md](Docs/Demo_Video_Script.md).

For the strongest recording, capture the Windows host and Quest view simultaneously and show the red-to-green keep-clear transition in both views. If the crowd run is verified on the exact recorded build, show a short blocked/clear comparison as a second product moment. Do not present the overlap color or crowd metrics as a real-world safety determination.

## Limitations and scope

- The keep-clear feedback checks geometric furniture overlap only; it does not prove safe egress, accessibility, code compliance, or evacuation readiness.
- Resident movement and queue metrics are a repeatable prototype simulation, not a calibrated prediction of real-world evacuation or shelter capacity. There is no emergency prediction, AI layout optimizer, voice chat, user accounts, or real-world spatial scanning.
- Actual Quest behavior and LAN conditions must be verified on the target hardware and network used for the demo.

## Documentation

- [Project plan](Docs/SafeShelter_Project_Plan.md)
- [Integration handoff and device test instructions](Docs/IntegrationHandoff.md)
- [Networking milestone](Docs/NetworkingMilestone.md)
- [Demo video script](Docs/Demo_Video_Script.md)

